using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Services
{
    public class BackupService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<BackupService> _logger;

        public BackupService(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            ILogger<BackupService> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        public string GetBackupsFolder()
        {
            return Path.Combine(
                _env.WebRootPath,
                "uploads",
                "reports",
                "backups");
        }

        public async Task<BackupRecord> CreateSnapshotAsync(
            string? note,
            bool isAutomatic,
            CancellationToken cancellationToken = default)
        {
            var createdAt = GetSastNow();

            var normalizedNote =
                string.IsNullOrWhiteSpace(note)
                    ? null
                    : note.Trim();

            if (normalizedNote?.Length > 200)
            {
                throw new ArgumentException(
                    "Snapshot notes cannot exceed 200 characters.",
                    nameof(note));
            }

            var backupRecord = new BackupRecord
            {
                CreatedAt = createdAt,
                SizeBytes = 0,
                Success = false,
                Note = normalizedNote,
                IsAutomatic = isAutomatic
            };

            _context.BackupRecords.Add(backupRecord);
            await _context.SaveChangesAsync(cancellationToken);

            var backupsFolder = GetBackupsFolder();
            Directory.CreateDirectory(backupsFolder);

            var fileName =
                $"backup_{backupRecord.Id}_{createdAt:yyyyMMdd_HHmmss}.json";

            var backupPath =
                Path.Combine(backupsFolder, fileName);

            backupRecord.FileName = fileName;

            try
            {
                // Load all datasets sequentially. Npgsql permits only one
                // active command per connection.
                var players = await _context.Players
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var coaches = await _context.Coaches
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var events = await _context.Events
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var matches = await _context.Matches
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var announcements = await _context.Announcements
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var attendances = await _context.Attendances
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var eventRegistrations = await _context.EventRegistrations
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var sponsors = await _context.Sponsors
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var reports = await _context.Reports
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var snapshot = new
                {
                    FormatVersion = 2,
                    GeneratedAtSast = createdAt,
                    Note = normalizedNote,
                    IsAutomatic = isAutomatic,
                    Players = players,
                    Coaches = coaches,
                    Events = events,
                    Matches = matches,
                    Announcements = announcements,
                    Attendances = attendances,
                    EventRegistrations = eventRegistrations,
                    Sponsors = sponsors,
                    Reports = reports
                };

                await using (var stream = new FileStream(
                    backupPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 64 * 1024,
                    useAsync: true))
                {
                    await JsonSerializer.SerializeAsync(
                        stream,
                        snapshot,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true
                        },
                        cancellationToken);

                    await stream.FlushAsync(cancellationToken);
                }

                var fileInfo = new FileInfo(backupPath);

                backupRecord.SizeBytes = fileInfo.Length;
                backupRecord.Sha256 =
                    await CalculateSha256Async(
                        backupPath,
                        cancellationToken);
                backupRecord.Success = true;

                await _context.SaveChangesAsync(cancellationToken);

                return backupRecord;
            }
            catch
            {
                if (System.IO.File.Exists(backupPath))
                {
                    System.IO.File.Delete(backupPath);
                }

                backupRecord.SizeBytes = 0;
                backupRecord.Success = false;
                backupRecord.Sha256 = null;

                try
                {
                    await _context.SaveChangesAsync(CancellationToken.None);
                }
                catch (Exception saveException)
                {
                    _logger.LogError(
                        saveException,
                        "Could not save failed backup record {BackupId}.",
                        backupRecord.Id);
                }

                throw;
            }
        }

        public string? FindSnapshotPath(BackupRecord backup)
        {
            var backupsFolder = GetBackupsFolder();

            if (!Directory.Exists(backupsFolder))
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(backup.FileName))
            {
                var safeName = Path.GetFileName(backup.FileName);
                var directPath = Path.Combine(backupsFolder, safeName);

                if (System.IO.File.Exists(directPath))
                {
                    return directPath;
                }
            }

            return Directory.GetFiles(
                    backupsFolder,
                    $"backup_{backup.Id}_*.json")
                .OrderByDescending(
                    System.IO.File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }

        public async Task<string> CalculateSha256Async(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            await using var stream =
                new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 64 * 1024,
                    useAsync: true);

            var hash =
                await SHA256.HashDataAsync(
                    stream,
                    cancellationToken);

            return Convert.ToHexString(hash);
        }

        public async Task<bool?> VerifyIntegrityAsync(
            BackupRecord backup,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(backup.Sha256))
            {
                return null;
            }

            var path = FindSnapshotPath(backup);

            if (path == null)
            {
                return false;
            }

            var currentHash =
                await CalculateSha256Async(
                    path,
                    cancellationToken);

            return string.Equals(
                currentHash,
                backup.Sha256,
                StringComparison.OrdinalIgnoreCase);
        }

        public async Task<Dictionary<string, int>> GetRecordCountsAsync(
            BackupRecord backup,
            CancellationToken cancellationToken = default)
        {
            var counts = new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);

            var path = FindSnapshotPath(backup);

            if (path == null)
            {
                return counts;
            }

            await using var stream =
                System.IO.File.OpenRead(path);

            using var document =
                await JsonDocument.ParseAsync(
                    stream,
                    cancellationToken:
                        cancellationToken);

            var root = document.RootElement;

            foreach (var propertyName in new[]
            {
                "Players",
                "Coaches",
                "Events",
                "Matches",
                "Announcements",
                "Attendances",
                "EventRegistrations",
                "Sponsors",
                "Reports"
            })
            {
                counts[propertyName] =
                    root.TryGetProperty(
                        propertyName,
                        out var value) &&
                    value.ValueKind == JsonValueKind.Array
                        ? value.GetArrayLength()
                        : 0;
            }

            return counts;
        }

        public async Task DeleteBackupAsync(
            BackupRecord backup,
            CancellationToken cancellationToken = default)
        {
            DeleteSnapshotFiles(backup);

            _context.BackupRecords.Remove(backup);

            await _context.SaveChangesAsync(
                cancellationToken);
        }

        public async Task<int> CleanFailedRecordsAsync(
            CancellationToken cancellationToken = default)
        {
            var failedRecords =
                await _context.BackupRecords
                    .Where(record => !record.Success)
                    .ToListAsync(cancellationToken);

            foreach (var record in failedRecords)
            {
                DeleteSnapshotFiles(record);
            }

            _context.BackupRecords.RemoveRange(
                failedRecords);

            if (failedRecords.Count > 0)
            {
                await _context.SaveChangesAsync(
                    cancellationToken);
            }

            return failedRecords.Count;
        }

        public async Task<int> ApplyRetentionAsync(
            int retentionCount,
            CancellationToken cancellationToken = default)
        {
            retentionCount =
                Math.Clamp(
                    retentionCount,
                    5,
                    100);

            var successfulRecords =
                await _context.BackupRecords
                    .Where(record => record.Success)
                    .OrderByDescending(
                        record => record.CreatedAt)
                    .ToListAsync(cancellationToken);

            var recordsToDelete =
                successfulRecords
                    .Skip(retentionCount)
                    .ToList();

            foreach (var record in recordsToDelete)
            {
                DeleteSnapshotFiles(record);
            }

            _context.BackupRecords.RemoveRange(
                recordsToDelete);

            if (recordsToDelete.Count > 0)
            {
                await _context.SaveChangesAsync(
                    cancellationToken);
            }

            return recordsToDelete.Count;
        }

        private void DeleteSnapshotFiles(
            BackupRecord backup)
        {
            var backupsFolder =
                GetBackupsFolder();

            if (!Directory.Exists(backupsFolder))
            {
                return;
            }

            var candidates =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(
                    backup.FileName))
            {
                candidates.Add(
                    Path.Combine(
                        backupsFolder,
                        Path.GetFileName(
                            backup.FileName)));
            }

            foreach (var path in Directory.GetFiles(
                         backupsFolder,
                         $"backup_{backup.Id}_*.json"))
            {
                candidates.Add(path);
            }

            foreach (var path in candidates)
            {
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }
        }

        public static DateTime GetSastNow()
        {
            return DateTime.SpecifyKind(
                DateTime.UtcNow.AddHours(2),
                DateTimeKind.Unspecified);
        }

        public static DateTime? GetNextScheduledRun(
            BackupSettings settings,
            DateTime? nowOverride = null)
        {
            if (!settings.AutomaticEnabled)
            {
                return null;
            }

            var now =
                nowOverride ??
                GetSastNow();

            var mostRecent =
                GetMostRecentScheduledRun(
                    settings,
                    now);

            if (!settings.LastAutomaticRunAt.HasValue ||
                settings.LastAutomaticRunAt.Value <
                    mostRecent)
            {
                // A scheduled run is already due. The background
                // service will create it on its next check.
                return now;
            }

            var todayScheduled =
                now.Date.AddHours(
                    settings.HourSast);

            if (settings.Frequency ==
                BackupScheduleFrequency.Daily)
            {
                return now < todayScheduled
                    ? todayScheduled
                    : todayScheduled.AddDays(1);
            }

            var daysUntil =
                ((int)settings.WeeklyDay -
                 (int)now.DayOfWeek +
                 7) % 7;

            var candidate =
                now.Date
                    .AddDays(daysUntil)
                    .AddHours(settings.HourSast);

            if (candidate <= now)
            {
                candidate =
                    candidate.AddDays(7);
            }

            return candidate;
        }

        public static DateTime GetMostRecentScheduledRun(
            BackupSettings settings,
            DateTime now)
        {
            if (settings.Frequency ==
                BackupScheduleFrequency.Daily)
            {
                var candidate =
                    now.Date.AddHours(
                        settings.HourSast);

                return candidate <= now
                    ? candidate
                    : candidate.AddDays(-1);
            }

            var daysSince =
                ((int)now.DayOfWeek -
                 (int)settings.WeeklyDay +
                 7) % 7;

            var weeklyCandidate =
                now.Date
                    .AddDays(-daysSince)
                    .AddHours(settings.HourSast);

            if (weeklyCandidate > now)
            {
                weeklyCandidate =
                    weeklyCandidate.AddDays(-7);
            }

            return weeklyCandidate;
        }
    }
}
