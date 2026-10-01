using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
namespace SprintPilot.Api;
public static class Api
{
    public static Guid Owner(this ClaimsPrincipal u) => Guid.Parse(u.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public static object View(Story s) => new { s.Id, code = $"SP-{s.Id:000}", s.ProposalId, content = Json.Read<StoryContent>(s.ContentJson), s.Status, s.Assignee, s.Blocked, s.Blocker, s.Version, s.CreatedAt, s.UpdatedAt };
    public static object View(Proposal p) => new { p.Id, p.Title, p.Source, p.State, p.Version, p.Provider, p.Error, p.CreatedAt, plan = p.PlanJson == "" ? null : Json.Read<Plan>(p.PlanJson) };
    public static void Map(WebApplication app)
    {
        RouteGroupBuilder api = app.MapGroup("/api").RequireAuthorization();
        api.MapGet("/board", async (SprintDb db, ClaimsPrincipal u) => Results.Ok((await db.Stories.AsNoTracking().Where(x => x.OwnerId == u.Owner()).OrderBy(x => x.Id).ToListAsync()).Select(View)));
        api.MapGet("/stories/{id:int}", async (int id, SprintDb db, ClaimsPrincipal u) => { Story? s = await db.Stories.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == u.Owner()); return s is null ? Results.NotFound() : Results.Ok(new { story = View(s), activity = await db.Activities.Where(x => x.StoryId == id).OrderByDescending(x => x.At).ToListAsync() }); });
        api.MapPut("/stories/{id:int}", async (int id, StoryEdit q, SprintDb db, ClaimsPrincipal u) =>
        {
            Story? s = await db.Stories.SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == u.Owner());
            if (s is null)
                return Results.NotFound();
            if (s.Version != q.Version)
                return Results.Conflict(new
                {
                    error = "This story changed. Close and reopen it before saving."
                });
            Validation.Content(q.Content);
            Validation.Workflow(q.Status, q.Assignee, q.Blocked, q.Blocker);
            string previous = s.Status;
            s.ContentJson = Json.Write(q.Content);
            s.Status = q.Status;
            s.Assignee = q.Assignee.Trim();
            s.Blocked = q.Blocked;
            s.Blocker = q.Blocked ? q.Blocker.Trim() : "";
            s.Version++;
            s.UpdatedAt = DateTime.UtcNow;
            db.Activities.Add(new Activity { StoryId = id, Text = $"{u.Identity!.Name} updated story details. Status: {previous} → {s.Status}." });
            await db.SaveChangesAsync();
            return Results.Ok(View(s));
        }).RequireRateLimiting("write");
        api.MapPost("/stories/{id:int}/move", async (int id, MoveRequest q, SprintDb db, ClaimsPrincipal u) =>
        {
            Story? s = await db.Stories.SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == u.Owner());
            if (s is null)
                return Results.NotFound();
            if (s.Version != q.Version)
                return Results.Conflict(new
                {
                    error = "Story moved or changed elsewhere. Refresh the board."
                });
            Validation.Workflow(q.Status, s.Assignee, s.Blocked, s.Blocker);
            if (s.Status == q.Status)
                return Results.Ok(View(s));
            string previous = s.Status;
            s.Status = q.Status;
            s.Version++;
            s.UpdatedAt = DateTime.UtcNow;
            db.Activities.Add(new Activity { StoryId = id, Text = $"{u.Identity!.Name} moved {previous} → {s.Status}." });
            await db.SaveChangesAsync();
            return Results.Ok(View(s));
        }).RequireRateLimiting("write");
        api.MapGet("/proposals", async (SprintDb db, ClaimsPrincipal u) => Results.Ok((await db.Proposals.AsNoTracking().Where(x => x.OwnerId == u.Owner()).OrderByDescending(x => x.CreatedAt).ToListAsync()).Select(View)));
        api.MapPost("/proposals", async (ImportRequest q, SprintDb db, ClaimsPrincipal u) =>
        {
            Validation.Text(q.Title, 3, 100, "Brief title");
            Validation.Text(q.Source, 20, 20000, "Requirements");
            await using var tx = await db.Database.BeginTransactionAsync();
            if (await db.Proposals.CountAsync(x => x.OwnerId == u.Owner()) >= 30)
                throw new InputError("Demo limit: 30 briefs per account.");
            Proposal p = new()
            {
                OwnerId = u.Owner(),
                Title = q.Title.Trim(),
                Source = q.Source.Trim()
            };
            db.Proposals.Add(p);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return Results.Created($"/api/proposals/{p.Id}", View(p));
        }).RequireRateLimiting("write");
        api.MapGet("/proposals/{id:guid}", async (Guid id, SprintDb db, ClaimsPrincipal u) => { Proposal? p = await db.Proposals.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == u.Owner()); return p is null ? Results.NotFound() : Results.Ok(View(p)); });
        api.MapPut("/proposals/{id:guid}", async (Guid id, ProposalEdit q, SprintDb db, ClaimsPrincipal u) =>
        {
            Proposal? p = await db.Proposals.SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == u.Owner());
            if (p is null)
                return Results.NotFound();
            if (p.State != "Ready" || p.Version != q.Version)
                return Results.Conflict(new
                {
                    error = "Only the current Ready proposal can be edited."
                });
            Validation.Plan(q.Plan);
            p.PlanJson = Json.Write(q.Plan);
            p.Version++;
            await db.SaveChangesAsync();
            return Results.Ok(View(p));
        }).RequireRateLimiting("write");
        api.MapPost("/proposals/{id:guid}/approve", async (Guid id, VersionRequest q, SprintDb db, ClaimsPrincipal u) =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            Proposal? p = await db.Proposals.SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == u.Owner());
            if (p is null)
                return Results.NotFound();
            if (p.State == "Applied")
            {
                int[] existing = await db.Stories.Where(x => x.ProposalId == id && x.OwnerId == u.Owner()).Select(x => x.Id).ToArrayAsync();
                return Results.Ok(new
                {
                    storyIds = existing,
                    alreadyApplied = true
                });
            }
            if (p.State != "Ready" || p.Version != q.Version)
                return Results.Conflict(new
                {
                    error = "Proposal changed or is not Ready. Refresh before approval."
                });
            Plan plan = Json.Read<Plan>(p.PlanJson);
            Validation.Plan(plan);
            if (await db.Stories.CountAsync(x => x.OwnerId == u.Owner()) + plan.Stories.Length > 100)
                throw new InputError("Demo limit: 100 stories per account.");
            List<Story> stories = [];
            for (int n = 0; n < plan.Stories.Length; n++)
            {
                Story s = new()
                {
                    OwnerId = u.Owner(),
                    ProposalId = p.Id,
                    ProposalIndex = n,
                    ContentJson = Json.Write(plan.Stories[n])
                };
                s.Activity.Add(new Activity { Text = $"Approved from brief '{p.Title}' by {u.Identity!.Name}." });
                stories.Add(s);
                db.Stories.Add(s);
            }
            p.State = "Applied";
            p.Version++;
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return Results.Ok(new
            {
                storyIds = stories.Select(x => x.Id),
                alreadyApplied = false
            });
        }).RequireRateLimiting("write");
        api.MapPost("/proposals/{id:guid}/retry", async (Guid id, VersionRequest q, SprintDb db, ClaimsPrincipal u) =>
        {
            Proposal? p = await db.Proposals.SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == u.Owner());
            if (p is null)
                return Results.NotFound();
            if (p.State != "Failed" || p.Version != q.Version)
                return Results.Conflict();
            p.State = "Queued";
            p.Attempts = 0;
            p.Error = null;
            p.RetryAt = DateTime.UtcNow;
            p.Version++;
            await db.SaveChangesAsync();
            return Results.Ok(View(p));
        }).RequireRateLimiting("write");
        api.MapGet("/summary", async (SprintDb db, ClaimsPrincipal u) =>
        {
            List<Story> stories = await db.Stories.AsNoTracking().Where(x => x.OwnerId == u.Owner()).OrderBy(x => x.Id).ToListAsync();
            List<TestCase> tests = stories.SelectMany(x => Json.Read<StoryContent>(x.ContentJson).Tests).ToList();
            return Results.Ok(new
            {
                generatedAt = DateTime.UtcNow,
                total = stories.Count,
                done = stories.Count(x => x.Status == "Done"),
                inProgress = stories.Count(x => x.Status == "In progress"),
                inReview = stories.Count(x => x.Status == "In review"),
                backlog = stories.Count(x => x.Status == "Backlog"),
                points = stories.Sum(x => Json.Read<StoryContent>(x.ContentJson).Points),
                completedPoints = stories.Where(x => x.Status == "Done").Sum(x => Json.Read<StoryContent>(x.ContentJson).Points),
                passed = tests.Count(x => x.Result == "Passed"),
                failed = tests.Count(x => x.Result == "Failed"),
                notRun = tests.Count(x => x.Result == "Not run"),
                completed = stories.Where(x => x.Status == "Done").Select(View),
                blockers = stories.Where(x => x.Blocked).Select(View)
            });
        });
        api.MapGet("/export", async (string? format, SprintDb db, ClaimsPrincipal u) =>
        {
            List<Story> stories = await db.Stories.AsNoTracking().Where(x => x.OwnerId == u.Owner()).OrderBy(x => x.Id).ToListAsync();
            if (format == "json")
                return Results.File(Encoding.UTF8.GetBytes(Json.Write(new
                {
                    exportedAt = DateTime.UtcNow,
                    stories = stories.Select(View)
                })), "application/json", "sprintpilot-backlog.json");
            if (format is not (null or "md"))
                throw new InputError("Export format must be md or json.");
            StringBuilder md = new("# SprintPilot backlog & test plan\n\nGenerated from recorded board data. Test results are manually recorded, not automated execution.\n\n");
            foreach (Story s in stories)
            {
                StoryContent c = Json.Read<StoryContent>(s.ContentJson);
                md.AppendLine($"## SP-{s.Id:000} — {ExportText(c.Title)}\n\nStatus: {s.Status} | Priority: {c.Priority} | Estimate: {c.Points} points\n\n{ExportText(c.Description)}\n\n### Acceptance criteria\n");
                foreach (string a in c.Acceptance)
                    md.AppendLine("- " + ExportText(a));
                md.AppendLine("\n### Implementation tasks\n");
                foreach (string a in c.Tasks)
                    md.AppendLine("- " + ExportText(a));
                md.AppendLine("\n### Test cases\n");
                foreach (TestCase t in c.Tests)
                    md.AppendLine($"- **{t.Kind} / {t.Result}:** {ExportText(t.Scenario)} Expected: {ExportText(t.Expected)}");
                if (s.Blocked)
                    md.AppendLine("\nBlocker: " + ExportText(s.Blocker));
                md.AppendLine();
            }
            return Results.File(Encoding.UTF8.GetBytes(md.ToString()), "text/markdown", "sprintpilot-backlog.md");
        });
    }
    public static string ExportText(string s) => System.Net.WebUtility.HtmlEncode(s).Replace("\\", "\\\\").Replace("*", "\\*").Replace("_", "\\_").Replace("[", "\\[").Replace("]", "\\]").Replace("`", "\\`").Replace("#", "\\#").Replace("\r", "").Replace("\n", " ");
}
