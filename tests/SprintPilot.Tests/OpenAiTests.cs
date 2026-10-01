using System.Net;
using System.Text;
using System.Text.Json;
using SprintPilot.Api;
using Microsoft.Extensions.Configuration;
using Xunit;
namespace SprintPilot.Tests;
public class OpenAiTests
{
    private sealed class Handler(string json, HttpStatusCode code = HttpStatusCode.OK) : HttpMessageHandler
    {
        public string? Body; protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken t)
        {
            Body = await r.Content!.ReadAsStringAsync(t);
            return new HttpResponseMessage(code) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }
    private static OpenAiPlanner Create(Handler h) => new(new HttpClient(h), new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["OpenAI:ApiKey"] = "test-only", ["OpenAI:Model"] = "gpt-4o-mini" }).Build());
    [Fact]
    public async Task Strict_request_returns_valid_plan()
    {
        string json = JsonSerializer.Serialize(new
        {
            status = "completed",
            output = new[] { new { content = new[] { new { type = "output_text", text = Json.Write(ValidationTests.Plan) } } } }
        });
        Handler h = new(json);
        Plan p = await Create(h).Generate("Brief", "Requirement text", default);
        Assert.Single(p.Stories);
        using JsonDocument d = JsonDocument.Parse(h.Body!);
        Assert.False(d.RootElement.GetProperty("store").GetBoolean());
        Assert.True(d.RootElement.GetProperty("text").GetProperty("format").GetProperty("strict").GetBoolean());
    }
    [Theory][InlineData("incomplete")][InlineData("failed")] public async Task Incomplete_rejected(string status) => await Assert.ThrowsAsync<InputError>(() => Create(new Handler("{\"status\":\"" + status + "\"}")).Generate("title", "text", default));
    [Fact] public async Task Refusal_rejected() => await Assert.ThrowsAsync<InputError>(() => Create(new Handler("{\"status\":\"completed\",\"output\":[{\"content\":[{\"type\":\"refusal\"}]}]}")).Generate("title", "text", default));
    [Fact]
    public async Task Error_body_not_exposed()
    {
        HttpRequestException e = await Assert.ThrowsAsync<HttpRequestException>(() => Create(new Handler("PRIVATE BODY", HttpStatusCode.TooManyRequests)).Generate("title", "text", default));
        Assert.DoesNotContain("PRIVATE", e.Message);
    }
}
