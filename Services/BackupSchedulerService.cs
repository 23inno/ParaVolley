using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Services
{
    public class BackupSchedulerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BackupSchedulerService> _logger;

        public BackupSchedulerService(
            IServiceScopeFactory scopeFactory,
            ILogger<BackupSchedulerService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            // Give the application a short time to complete startup.
            await Task.Delay(
                TimeSpan.FromSeconds(20),
                stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunDueBackupAsync(
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Automatic backup check failed.");
                }

                await Task.Delay(
                    TimeSpan.FromMinutes(30),
                    stoppingToken);
            }
        }

        private async Task RunDueBackupAsync(
            CancellationToken cancellationToken)
        {
            using var scope =
                _scopeFactory.CreateScope();

            var context =
                scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

            var backupService =
                scope.ServiceProvider
                    .GetRequiredService<BackupService>();

            var settings =
                await context.BackupSettings
                    .FirstOrDefaultAsync(
                        cancellationToken);

            if (settings == null ||
                !settings.AutomaticEnabled)
            {
                return;
            }

            var now =
                BackupService.GetSastNow();

            var mostRecentScheduledRun =
                BackupService.GetMostRecentScheduledRun(
                    settings,
                    now);

            if (settings.LastAutomaticRunAt.HasValue &&
                settings.LastAutomaticRunAt.Value >=
                    mostRecentScheduledRun)
            {
                return;
            }

            var backup =
                await backupService.CreateSnapshotAsync(
                    "Automatic scheduled snapshot",
                    isAutomatic: true,
                    cancellationToken);

            settings.LastAutomaticRunAt =
                backup.CreatedAt;

            await context.SaveChangesAsync(
                cancellationToken);

            await backupService.ApplyRetentionAsync(
                settings.RetentionCount,
                cancellationToken);

            _logger.LogInformation(
                "Automatic snapshot {BackupId} created at {CreatedAt}.",
                backup.Id,
                backup.CreatedAt);
        }
    }
}
