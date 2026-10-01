namespace SprintPilot.Api;
public static class Validation
{
    public static readonly string[] States = ["Backlog", "In progress", "In review", "Done"];
    public static readonly string[] Priorities = ["Low", "Medium", "High"];
    public static readonly string[] Kinds = ["Positive", "Negative", "Boundary"];
    public static readonly string[] Results = ["Not run", "Passed", "Failed"];
    public static void Text(string? text, int min, int max, string label)
    {
        if (text is null || text.Trim().Length < min || text.Length > max)
            throw new InputError($"{label} must contain {min}–{max} characters.");
    }
    public static void Lines(string[]? values, int min, string label)
    {
        if (values is null || values.Length < min || values.Length > 12)
            throw new InputError($"{label} must contain {min}–12 items.");
        foreach (string line in values)
            Text(line, 1, 700, label);
    }
    public static void Content(StoryContent? c, bool draft = false)
    {
        if (c is null)
            throw new InputError("Story content is required.");
        Text(c.Title, 3, 140, "Title");
        Text(c.Description, 10, 2000, "Description");
        if (!Priorities.Contains(c.Priority) || !new[] { 1, 2, 3, 5, 8, 13 }.Contains(c.Points))
            throw new InputError("Choose a valid priority and story-point estimate.");
        Lines(c.Acceptance, 1, "Acceptance criteria");
        Lines(c.Tasks, 1, "Implementation tasks");
        if (c.Tests is null || c.Tests.Length < 3 || c.Tests.Length > 12)
            throw new InputError("Provide 3–12 test cases, including Positive, Negative and Boundary.");
        foreach (TestCase? t in c.Tests)
        {
            if (t is null || !Kinds.Contains(t.Kind) || !Results.Contains(t.Result))
                throw new InputError("Invalid test kind or result.");
            Text(t.Scenario, 3, 700, "Test scenario");
            Text(t.Expected, 3, 700, "Expected result");
            if (draft && t.Result != "Not run")
                throw new InputError("Proposed tests must be Not run.");
        }
        if (Kinds.Any(k => !c.Tests.Any(t => t.Kind == k)))
            throw new InputError("Include Positive, Negative and Boundary test cases.");
    }
    public static void Plan(Plan? p)
    {
        if (p is null)
            throw new InputError("Plan is required.");
        Text(p.Summary, 5, 1500, "Summary");
        Lines(p.Questions, 0, "Clarification questions");
        if (p.Stories is null || p.Stories.Length is < 1 or > 6)
            throw new InputError("A proposal needs 1–6 stories.");
        foreach (StoryContent s in p.Stories)
            Content(s, true);
    }
    public static void Workflow(string status, string? assignee, bool blocked, string? blocker)
    {
        if (!States.Contains(status))
            throw new InputError("Unknown board column.");
        Text(assignee, 0, 80, "Assignee");
        Text(blocker, blocked ? 3 : 0, 500, "Blocker");
        if (blocked && status == "Done")
            throw new InputError("Resolve the blocker before moving to Done.");
    }
}
