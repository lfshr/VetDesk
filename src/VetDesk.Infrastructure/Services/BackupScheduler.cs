using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Application.Settings.Commands;
using VetDesk.Domain.Common;
using VetDesk.Infrastructure.Persistence;

namespace VetDesk.Infrastructure.Services;

/// <summary>
/// Runs the nightly backup while VetDesk is open: every few minutes it checks whether today's backup time has passed
/// without a backup, and if so takes one. If several desks are open, the first to notice does it; the others then
/// see a recent backup and skip.
/// </summary>
public sealed class BackupScheduler : IAsyncDisposable
{
    internal static readonly TimeSpan CheckEvery = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopes;
    private readonly IClock _clock;
    private readonly ILogger<BackupScheduler> _logger;
    private PeriodicTimer? _timer;
    private Task? _loop;
    private readonly CancellationTokenSource _stop = new();

    public BackupScheduler(IServiceScopeFactory scopes, IClock clock, ILogger<BackupScheduler> logger)
    {
        _scopes = scopes;
        _clock = clock;
        _logger = logger;
    }

    public void Start()
    {
        if (_loop is not null)
            return;
        _timer = new PeriodicTimer(CheckEvery);
        _loop = RunAsync(_stop.Token);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            do
            {
                await CheckAsync(cancellationToken);
            } while (await _timer!.WaitForNextTickAsync(cancellationToken));
        }
        catch (OperationCanceledException)
        {
        }
    }

    internal async Task CheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<VetDeskDbContext>();
            var settings = await db.ClinicSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
            if (settings is null || string.IsNullOrWhiteSpace(settings.BackupFolder))
                return;

            var now = _clock.UtcNow;
            var dueUtc = ClinicTime.ToUtc(ClinicTime.Today(now), settings.BackupTime);
            if (now < dueUtc || settings.LastBackupUtc >= dueUtc)
                return;

            // A failed attempt today is not retried every five minutes; it is reported once and waits for tomorrow or "Back up now".
            var triedToday = await db.BackupRecords.AnyAsync(b => b.StartedUtc >= dueUtc, cancellationToken);
            if (triedToday)
                return;

            var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new RunBackupCommand { Scheduled = true }, cancellationToken);
            if (result.IsFailure)
                _logger.LogError("Nightly backup failed: {Error}", result.ToString());
            else
                _logger.LogInformation("Nightly backup written to {File}", result.Value);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Nightly backup check failed");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        if (_loop is not null)
            await _loop;
        _timer?.Dispose();
        _stop.Dispose();
    }
}
