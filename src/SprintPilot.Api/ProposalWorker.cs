using Microsoft.EntityFrameworkCore;
namespace SprintPilot.Api;
public sealed class ProposalWorker(IServiceScopeFactory scopes, ILogger<ProposalWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (!await Once(token))
                    await Task.Delay(1000, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
            catch (Exception e) { log.LogWarning("Planning failed: {Type}", e.GetType().Name); await Task.Delay(2000, token); }
        }
    }
    public async Task<bool> Once(CancellationToken token)
    {
        using IServiceScope scope = scopes.CreateScope();
        SprintDb db = scope.ServiceProvider.GetRequiredService<SprintDb>();
        IPlanner ai = scope.ServiceProvider.GetRequiredService<IPlanner>();
        DateTime now = DateTime.UtcNow;
        Proposal? p = await db.Proposals.AsNoTracking().OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(x => x.State == "Queued" && x.RetryAt <= now || x.State == "Generating" && x.LeaseUntil <= now, token);
        if (p is null)
            return false;
        string lease = Guid.NewGuid().ToString();
        int claimed = await db.Proposals.Where(x => x.Id == p.Id && x.Version == p.Version).ExecuteUpdateAsync(s => s.SetProperty(x => x.State, "Generating").SetProperty(x => x.Version, x => x.Version + 1).SetProperty(x => x.Attempts, x => x.Attempts + 1).SetProperty(x => x.Lease, lease).SetProperty(x => x.LeaseUntil, now.AddMinutes(3)), token);
        if (claimed == 0)
            return true;
        Plan? plan = null;
        string? error = null;
        bool permanent = false;
        try
        {
            if (p.Attempts >= 3)
                throw new InputError("Attempt limit reached after interruptions.");
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(TimeSpan.FromSeconds(100));
            plan = await ai.Generate(p.Title, p.Source, timeout.Token);
            Validation.Plan(plan);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (InputError e) { error = e.Message; permanent = true; }
        catch (Exception e) { error = "Unable to generate proposal. Check provider configuration and retry."; log.LogWarning("Provider failure: {Type}", e.GetType().Name); }
        Proposal? current = await db.Proposals.SingleOrDefaultAsync(x => x.Id == p.Id && x.Lease == lease, token);
        if (current is null)
            return true;
        if (plan is not null && error is null)
        {
            current.PlanJson = Json.Write(plan);
            current.Provider = ai.Name;
            current.State = "Ready";
        }
        else
        {
            current.State = permanent || current.Attempts >= 3 ? "Failed" : "Queued";
            current.RetryAt = DateTime.UtcNow.AddSeconds(Math.Pow(2, current.Attempts));
        }
        current.Error = error;
        current.Lease = null;
        current.LeaseUntil = null;
        current.Version++;
        await db.SaveChangesAsync(token);
        return true;
    }
}
