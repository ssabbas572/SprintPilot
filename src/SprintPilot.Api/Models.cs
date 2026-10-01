using System.Text.Json;
using Microsoft.EntityFrameworkCore;
namespace SprintPilot.Api;
public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid(); public string Email { get; set; } = ""; public string PasswordHash { get; set; } = "";
}
public sealed class Proposal
{
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid OwnerId
    {
        get; set;
    }
    public string Title { get; set; } = ""; public string Source { get; set; } = "";
    public string State { get; set; } = "Queued"; public string PlanJson { get; set; } = ""; public int Version { get; set; } = 1; public string Provider { get; set; } = "";
    public int Attempts
    {
        get; set;
    }
    public string? Error
    {
        get; set;
    }
    public string? Lease
    {
        get; set;
    }
    public DateTime? LeaseUntil
    {
        get; set;
    }
    public DateTime RetryAt { get; set; } = DateTime.UtcNow; public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public sealed class Story
{
    public int Id
    {
        get; set;
    }
    public Guid OwnerId
    {
        get; set;
    }
    public Guid? ProposalId
    {
        get; set;
    }
    public int ProposalIndex
    {
        get; set;
    }
    public string ContentJson { get; set; } = ""; public string Status { get; set; } = "Backlog"; public string Assignee { get; set; } = ""; public bool Blocked
    {
        get; set;
    }
    public string Blocker { get; set; } = "";
    public int Version { get; set; } = 1; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<Activity> Activity { get; set; } = [];
}
public sealed class Activity
{
    public long Id
    {
        get; set;
    }
    public int StoryId
    {
        get; set;
    }
    public string Text { get; set; } = ""; public DateTime At { get; set; } = DateTime.UtcNow;
}
public record TestCase(string Kind, string Scenario, string Expected, string Result = "Not run");
public record StoryContent(string Title, string Description, string Priority, int Points, string[] Acceptance, string[] Tasks, TestCase[] Tests);
public record Plan(string Summary, string[] Questions, StoryContent[] Stories);
public record LoginRequest(string Email, string Password);
public record ImportRequest(string Title, string Source);
public record ProposalEdit(int Version, Plan Plan);
public record VersionRequest(int Version);
public record StoryEdit(int Version, StoryContent Content, string Status, string Assignee, bool Blocked, string Blocker);
public record MoveRequest(int Version, string Status);
public sealed class InputError(string message) : Exception(message);
public static class Json
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web); public static string Write<T>(T value) => JsonSerializer.Serialize(value, Options); public static T Read<T>(string text) => JsonSerializer.Deserialize<T>(text, Options) ?? throw new InputError("Empty JSON content.");
}
public sealed class SprintDb(DbContextOptions<SprintDb> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>(); public DbSet<Proposal> Proposals => Set<Proposal>(); public DbSet<Story> Stories => Set<Story>(); public DbSet<Activity> Activities => Set<Activity>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(x => x.Email).IsUnique();
        b.Entity<Proposal>().Property(x => x.Version).IsConcurrencyToken();
        b.Entity<Story>().Property(x => x.Version).IsConcurrencyToken();
        b.Entity<Proposal>().HasOne<User>().WithMany().HasForeignKey(x => x.OwnerId);
        b.Entity<Story>().HasOne<User>().WithMany().HasForeignKey(x => x.OwnerId);
        b.Entity<Story>().HasOne<Proposal>().WithMany().HasForeignKey(x => x.ProposalId);
        b.Entity<Story>().HasIndex(x => new { x.ProposalId, x.ProposalIndex }).IsUnique();
        b.Entity<Story>().HasMany(x => x.Activity).WithOne().HasForeignKey(x => x.StoryId).OnDelete(DeleteBehavior.Cascade);
    }
}
