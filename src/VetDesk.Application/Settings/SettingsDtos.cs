namespace VetDesk.Application.Settings;

public record ClinicSettingsDto(
    string Name,
    string? Address,
    string? Phone,
    string? Email,
    string? VatNumber,
    TimeOnly WeekdayOpen,
    TimeOnly WeekdayClose,
    TimeOnly? SaturdayOpen,
    TimeOnly? SaturdayClose,
    TimeOnly? LunchStart,
    TimeOnly? LunchEnd,
    int PaymentTermsDays,
    decimal OpeningFloat,
    int AutoLockMinutes,
    string? BackupFolder,
    TimeOnly BackupTime,
    DateTime? LastBackupUtc,
    string? LastBackupResult,
    Guid Version);

public record BackupDto(DateTime StartedUtc, bool Succeeded, string? FilePath, string? Error, string TriggeredBy);
