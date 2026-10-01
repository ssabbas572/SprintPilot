using System.Security.Claims;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using SprintPilot.Api;
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(x => x.Limits.MaxRequestBodySize = 1024 * 1024);
builder.Services.AddDbContext<SprintDb>(o => o.UseSqlite(builder.Configuration.GetConnectionString("SprintPilot") ?? "Data Source=sprintpilot.db"));
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
{
    o.Cookie.Name = "SprintPilot.Session";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.Events.OnRedirectToLogin = c => { c.Response.StatusCode = 401; return Task.CompletedTask; };
});
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();
builder.Services.AddRateLimiter(o => { o.RejectionStatusCode = 429; o.AddPolicy("write", c => RateLimitPartition.GetFixedWindowLimiter(c.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? c.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) })); });
string provider = builder.Configuration["AI:Provider"] ?? "Mock";
if (provider is not ("Mock" or "OpenAI")) throw new InvalidOperationException("Provider must be Mock or OpenAI.");
if (provider == "OpenAI" && string.IsNullOrWhiteSpace(builder.Configuration["OpenAI:ApiKey"])) throw new InvalidOperationException("Set OpenAI:ApiKey in user secrets.");
builder.Services.AddHttpClient<OpenAiPlanner>(x => x.Timeout = TimeSpan.FromSeconds(90));
builder.Services.AddScoped<IPlanner>(s => provider == "Mock" ? new MockPlanner() : s.GetRequiredService<OpenAiPlanner>());
if (builder.Configuration.GetValue("Worker:Enabled", true)) builder.Services.AddHostedService<ProposalWorker>();
WebApplication app = builder.Build();
app.UseExceptionHandler();
app.Use(async (c, next) =>
{
    c.Response.Headers["X-Content-Type-Options"] = "nosniff";
    if (c.Request.Method is "POST" or "PUT" or "DELETE" && c.Request.Headers["X-SprintPilot"] != "1")
    {
        c.Response.StatusCode = 400;
        await c.Response.WriteAsJsonAsync(new
        {
            error = "Required request header missing."
        });
        return;
    }
    try
    {
        await next();
    }
    catch (InputError e) { c.Response.StatusCode = 400; await c.Response.WriteAsJsonAsync(new { error = e.Message }); }
    catch (DbUpdateConcurrencyException) { c.Response.StatusCode = 409; await c.Response.WriteAsJsonAsync(new { error = "This record changed. Refresh and try again." }); }
});
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
using (IServiceScope s = app.Services.CreateScope()) { SprintDb db = s.ServiceProvider.GetRequiredService<SprintDb>(); await db.Database.EnsureCreatedAsync(); if (app.Environment.IsDevelopment() && app.Configuration.GetValue("Demo:Seed", true) && !await db.Users.AnyAsync()) await Seed.Run(db, s.ServiceProvider.GetRequiredService<IPasswordHasher<User>>()); }
app.MapGet("/", () => new { message = "SprintPilot API. Open the frontend at http://localhost:5176" });
app.MapGet("/health", () => new { status = "healthy" });
app.MapPost("/api/auth/login", async (LoginRequest q, SprintDb db, IPasswordHasher<User> hasher, HttpContext c) =>
{
    if (string.IsNullOrWhiteSpace(q.Email) || string.IsNullOrEmpty(q.Password) || q.Email.Length > 254 || q.Password.Length > 256)
        return Results.BadRequest();
    User? u = await db.Users.SingleOrDefaultAsync(x => x.Email == q.Email.Trim().ToLowerInvariant());
    if (u is null || hasher.VerifyHashedPassword(u, u.PasswordHash, q.Password) == PasswordVerificationResult.Failed)
        return Results.Unauthorized();
    await c.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()), new Claim(ClaimTypes.Name, u.Email) }, CookieAuthenticationDefaults.AuthenticationScheme)));
    return Results.Ok(new
    {
        u.Email
    });
}).RequireRateLimiting("write");
app.MapGet("/api/auth/me", (ClaimsPrincipal u, IPlanner ai) => new { email = u.Identity!.Name, provider = ai.Name }).RequireAuthorization();
app.MapPost("/api/auth/logout", async (HttpContext c) => { await c.SignOutAsync(); return Results.NoContent(); }).RequireAuthorization();
Api.Map(app);
app.Run();
public partial class Program
{
}
