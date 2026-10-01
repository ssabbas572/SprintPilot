using SprintPilot.Api;
using Xunit;
namespace SprintPilot.Tests;
public class ValidationTests
{
    public static StoryContent Good => MockPlanner.Template("Book an appointment", "Book an available provider slot.");
    public static Plan Plan => new("An appointment delivery plan.", ["Which provider roles are allowed?"], [Good]);
    [Fact] public void Valid_plan_is_accepted() => Validation.Plan(Plan);
    [Theory][InlineData(0)][InlineData(4)][InlineData(99)] public void Invalid_points_rejected(int points) => Assert.Throws<InputError>(() => Validation.Content(Good with { Points = points }));
    [Fact] public void Missing_negative_or_boundary_test_rejected() => Assert.Throws<InputError>(() => Validation.Content(Good with { Tests = [Good.Tests[0], Good.Tests[0], Good.Tests[0]] }));
    [Fact] public void Proposed_tests_cannot_claim_passed() => Assert.Throws<InputError>(() => Validation.Plan(Plan with { Stories = [Good with { Tests = Good.Tests.Select(t => t with { Result = "Passed" }).ToArray() }] }));
    [Fact] public void Empty_acceptance_rejected() => Assert.Throws<InputError>(() => Validation.Content(Good with { Acceptance = [] }));
    [Fact] public void Seven_stories_rejected() => Assert.Throws<InputError>(() => Validation.Plan(Plan with { Stories = Enumerable.Repeat(Good, 7).ToArray() }));
    [Fact] public void Blocker_requires_reason() => Assert.Throws<InputError>(() => Validation.Workflow("In progress", "", true, ""));
    [Fact] public void Blocked_story_cannot_be_done() => Assert.Throws<InputError>(() => Validation.Workflow("Done", "", true, "Awaiting a decision"));
    [Fact] public void Unknown_state_rejected() => Assert.Throws<InputError>(() => Validation.Workflow("Paid", "", false, ""));
    [Fact]
    public void Markdown_escapes_html_and_links()
    {
        string text = SprintPilot.Api.Api.ExportText("<script>x</script>[x](url)");
        Assert.DoesNotContain("<script>", text);
        Assert.Contains("\\[x\\]", text);
    }
    [Fact]
    public async Task Mock_uses_source_lines_and_not_run_tests()
    {
        Plan p = await new MockPlanner().Generate("Booking", "Book a slot.\nReschedule a booking.", default);
        Assert.Equal(2, p.Stories.Length);
        Assert.Contains("Book a slot", p.Stories[0].Description);
        Assert.All(p.Stories.SelectMany(x => x.Tests), t => Assert.Equal("Not run", t.Result));
        Validation.Plan(p);
    }
}
