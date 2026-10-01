using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SprintPilot.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace SprintPilot.Tests;
public sealed class Factory : WebApplicationFactory<Program>
{
    private readonly string path = Path.Combine(Path.GetTempPath(), "sprintpilot-test-" + Guid.NewGuid() + ".db"); public IPlanner Planner { get; set; } = new MockPlanner();
    protected override void ConfigureWebHost(IWebHostBuilder b)
    {
        b.UseEnvironment("Development");
        b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:SprintPilot"] = "Data Source=" + path, ["Worker:Enabled"] = "false", ["AI:Provider"] = "Mock" }));
        b.ConfigureServices(s => s.AddScoped<IPlanner>(_ => Planner));
    }
    public async Task<HttpClient> Login(string email = "second@sprintpilot.local")
    {
        HttpClient c = CreateClient();
        c.DefaultRequestHeaders.Add("X-SprintPilot", "1");
        (await c.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "DemoPass123!"
        })).EnsureSuccessStatusCode();
        return c;
    }
    public Task<bool> Process() => new ProposalWorker(Services.GetRequiredService<IServiceScopeFactory>(), Services.GetRequiredService<ILogger<ProposalWorker>>()).Once(default);
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (string p in new[] { path, path + "-wal", path + "-shm" })
            if (File.Exists(p))
                File.Delete(p);
    }
}
public class WorkflowTests
{
    private static async Task<Guid> Import(HttpClient c)
    {
        HttpResponseMessage r = await c.PostAsJsonAsync("/api/proposals", new ImportRequest("Booking experience", "Customers should be able to reserve an available appointment slot."));
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
    private static async Task<JsonElement> Get(HttpClient c, Guid id) => (await c.GetFromJsonAsync<JsonElement[]>("/api/proposals"))!.Single(p => p.GetProperty("id").GetGuid() == id);
    private static async Task<int> Approve(HttpClient c, Guid id)
    {
        JsonElement p = await Get(c, id);
        HttpResponseMessage r = await c.PostAsJsonAsync($"/api/proposals/{id}/approve", new VersionRequest(p.GetProperty("version").GetInt32()));
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("storyIds")[0].GetInt32();
    }
    private static async Task<JsonElement> Story(HttpClient c, int id) => (await c.GetFromJsonAsync<JsonElement>($"/api/stories/{id}")).GetProperty("story");
    [Fact]
    public async Task Anonymous_access_rejected()
    {
        using Factory f = new();
        Assert.Equal(HttpStatusCode.Unauthorized, (await f.CreateClient().GetAsync("/api/board")).StatusCode);
    }
    [Fact]
    public async Task Mutation_header_required()
    {
        using Factory f = new();
        using HttpClient c = await f.Login();
        c.DefaultRequestHeaders.Remove("X-SprintPilot");
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/proposals", new ImportRequest("Title", "A valid source requirement to test."))).StatusCode);
    }
    [Fact]
    public async Task Generation_does_not_change_board_until_approval()
    {
        using Factory f = new();
        using HttpClient c = await f.Login();
        Guid id = await Import(c);
        Assert.Equal("Queued", (await c.GetFromJsonAsync<JsonElement>($"/api/proposals/{id}")).GetProperty("state").GetString());
        Assert.Empty((await c.GetFromJsonAsync<JsonElement[]>("/api/board"))!);
        await f.Process();
        Assert.Equal("Ready", (await Get(c, id)).GetProperty("state").GetString());
        Assert.Empty((await c.GetFromJsonAsync<JsonElement[]>("/api/board"))!);
        int story = await Approve(c, id);
        Assert.Equal("Backlog", (await Story(c, story)).GetProperty("status").GetString());
    }
    [Fact]
    public async Task Approval_is_idempotent_and_keeps_same_story_ids()
    {
        using Factory f = new();
        using HttpClient c = await f.Login();
        Guid id = await Import(c);
        await f.Process();
        int first = await Approve(c, id);
        HttpResponseMessage repeat = await c.PostAsJsonAsync($"/api/proposals/{id}/approve", new VersionRequest(0));
        repeat.EnsureSuccessStatusCode();
        JsonElement payload = await repeat.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(payload.GetProperty("alreadyApplied").GetBoolean());
        Assert.Equal(first, payload.GetProperty("storyIds")[0].GetInt32());
        Assert.Single((await c.GetFromJsonAsync<JsonElement[]>("/api/board"))!);
    }
    [Fact]
    public async Task Proposal_edit_requires_current_version_and_ready_state()
    {
        using Factory f = new();
        using HttpClient c = await f.Login();
        Guid id = await Import(c);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/proposals/{id}", new ProposalEdit(1, ValidationTests.Plan))).StatusCode);
        await f.Process();
        JsonElement p = await Get(c, id);
        int v = p.GetProperty("version").GetInt32();
        (await c.PutAsJsonAsync($"/api/proposals/{id}", new ProposalEdit(v, ValidationTests.Plan))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/proposals/{id}/approve", new VersionRequest(v))).StatusCode);
        await Approve(c, id);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PutAsJsonAsync($"/api/proposals/{id}", new ProposalEdit(v + 2, ValidationTests.Plan))).StatusCode);
    }
    [Fact]
    public async Task Accounts_are_isolated()
    {
        using Factory f = new();
        using HttpClient a = await f.Login(), b = await f.Login("demo@sprintpilot.local");
        Guid id = await Import(a);
        await f.Process();
        int story = await Approve(a, id);
        Assert.DoesNotContain((await b.GetFromJsonAsync<JsonElement[]>("/api/proposals"))!, p => p.GetProperty("id").GetGuid() == id);
        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"/api/stories/{story}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.PostAsJsonAsync($"/api/stories/{story}/move", new MoveRequest(1, "Done"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await b.PostAsJsonAsync($"/api/proposals/{id}/approve", new VersionRequest(3))).StatusCode);
        Assert.DoesNotContain("Booking experience", await b.GetStringAsync("/api/export?format=md"));
    }
    [Fact]
    public async Task Moving_stories_is_versioned_and_logged()
    {
        using Factory f = new();
        using HttpClient c = await f.Login();
        Guid id = await Import(c);
        await f.Process();
        int s = await Approve(c, id);
        (await c.PostAsJsonAsync($"/api/stories/{s}/move", new MoveRequest(1, "In progress"))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync($"/api/stories/{s}/move", new MoveRequest(1, "Done"))).StatusCode);
        JsonElement detail = await c.GetFromJsonAsync<JsonElement>($"/api/stories/{s}");
        Assert.Equal(2, detail.GetProperty("activity").GetArrayLength());
        Assert.Equal("In progress", detail.GetProperty("story").GetProperty("status").GetString());
    }
    [Fact]
    public async Task Blocker_prevents_done_until_resolved()
    {
        using Factory f = new();
        using HttpClient c = await f.Login();
        Guid id = await Import(c);
        await f.Process();
        int s = await Approve(c, id);
        (await c.PutAsJsonAsync($"/api/stories/{s}", new StoryEdit(1, ValidationTests.Good, "In progress", "Alex", true, "Waiting on policy"))).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync($"/api/stories/{s}/move", new MoveRequest(2, "Done"))).StatusCode);
        (await c.PutAsJsonAsync($"/api/stories/{s}", new StoryEdit(2, ValidationTests.Good, "Done", "Alex", false, ""))).EnsureSuccessStatusCode();
    }
    [Fact]
    public async Task Summary_and_exports_use_saved_manual_test_results()
    {
        using Factory f = new();
        using HttpClient c = await f.Login();
        Guid id = await Import(c);
        await f.Process();
        int s = await Approve(c, id);
        StoryContent content = ValidationTests.Good with
        {
            Tests = ValidationTests.Good.Tests.Select((t, n) => t with { Result = n == 0 ? "Passed" : n == 1 ? "Failed" : "Not run" }).ToArray()
        };
        (await c.PutAsJsonAsync($"/api/stories/{s}", new StoryEdit(1, content, "Done", "Alex", false, ""))).EnsureSuccessStatusCode();
        JsonElement summary = await c.GetFromJsonAsync<JsonElement>("/api/summary");
        Assert.Equal(1, summary.GetProperty("done").GetInt32());
        Assert.Equal(1, summary.GetProperty("passed").GetInt32());
        Assert.Equal(1, summary.GetProperty("failed").GetInt32());
        Assert.Contains("Negative / Failed", await c.GetStringAsync("/api/export?format=md"));
        JsonElement exported = await c.GetFromJsonAsync<JsonElement>("/api/export?format=json");
        Assert.Single(exported.GetProperty("stories").EnumerateArray());
    }
    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public async Task Invalid_brief_rejected(string text)
    {
        using Factory f = new();
        using HttpClient c = await f.Login();
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/proposals", new ImportRequest("Test brief", text))).StatusCode);
    }
    [Fact]
    public async Task Invalid_proposal_does_not_create_stories()
    {
        using Factory f = new();
        using HttpClient c = await f.Login();
        Guid id = await Import(c);
        await f.Process();
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PutAsJsonAsync($"/api/proposals/{id}", new ProposalEdit(3, ValidationTests.Plan with
        {
            Stories = []
        }))).StatusCode);
        Assert.Empty((await c.GetFromJsonAsync<JsonElement[]>("/api/board"))!);
    }
    private sealed class FailingPlanner : IPlanner
    {
        public string Name => "Test"; public Task<Plan> Generate(string title, string source, CancellationToken token) => throw new HttpRequestException("simulated");
    }
    [Fact]
    public async Task Failure_retries_are_bounded_and_manual_retry_recovers()
    {
        using Factory f = new();
        using HttpClient c = await f.Login();
        Guid id = await Import(c);
        f.Planner = new FailingPlanner();
        for (int n = 0; n < 3; n++)
        {
            using (IServiceScope scope = f.Services.CreateScope())
            {
                SprintDb db = scope.ServiceProvider.GetRequiredService<SprintDb>();
                Proposal p = await db.Proposals.SingleAsync(x => x.Id == id);
                p.RetryAt = DateTime.UtcNow.AddSeconds(-1);
                await db.SaveChangesAsync();
            }
            await f.Process();
        }
        JsonElement p2 = await Get(c, id);
        Assert.Equal("Failed", p2.GetProperty("state").GetString());
        Assert.False(await f.Process());
        (await c.PostAsJsonAsync($"/api/proposals/{id}/retry", new VersionRequest(p2.GetProperty("version").GetInt32()))).EnsureSuccessStatusCode();
        f.Planner = new MockPlanner();
        await f.Process();
        Assert.Equal("Ready", (await Get(c, id)).GetProperty("state").GetString());
    }
    [Fact]
    public async Task Expired_generation_lease_is_recovered()
    {
        using Factory f = new();
        using HttpClient c = await f.Login();
        Guid id = await Import(c);
        using (IServiceScope scope = f.Services.CreateScope())
        {
            SprintDb db = scope.ServiceProvider.GetRequiredService<SprintDb>();
            Proposal p = await db.Proposals.SingleAsync(x => x.Id == id);
            p.State = "Generating";
            p.Lease = "old";
            p.LeaseUntil = DateTime.UtcNow.AddMinutes(-4);
            p.Attempts = 1;
            await db.SaveChangesAsync();
        }
        Assert.True(await f.Process());
        Assert.Equal("Ready", (await Get(c, id)).GetProperty("state").GetString());
    }
}
