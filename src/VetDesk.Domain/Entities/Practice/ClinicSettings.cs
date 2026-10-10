using VetDesk.Domain.Common;

namespace VetDesk.Domain.Entities.Practice;

/// <summary>The practice's own details. There is exactly one row (single branch in v1).</summary>
public class ClinicSettings : BaseAuditableEntity, IHasVersion
{
    public string Name { get; set; } = "My veterinary practice";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? VatNumber { get; set; }

    public TimeOnly WeekdayOpen { get; set; } = new(8, 0);
    public TimeOnly WeekdayClose { get; set; } = new(19, 0);
    public TimeOnly? SaturdayOpen { get; set; } = new(9, 0);
    public TimeOnly? SaturdayClose { get; set; } = new(13, 0);
    public TimeOnly? LunchStart { get; set; } = new(13, 0);
    public TimeOnly? LunchEnd { get; set; } = new(14, 0);

    public int PaymentTermsDays { get; set; } = 14;
    public decimal OpeningFloat { get; set; } = 150m;
    public int AutoLockMinutes { get; set; } = 10;

    public string? BackupFolder { get; set; }
    public TimeOnly BackupTime { get; set; } = new(2, 0);
    public DateTime? LastBackupUtc { get; set; }
    public string? LastBackupResult { get; set; }

    public Guid Version { get; set; }

    /// <summary>Opening hours for a day, or null when closed (Sundays).</summary>
    public (TimeOnly Open, TimeOnly Close)? HoursOn(DayOfWeek day) => day switch
    {
        DayOfWeek.Sunday => null,
        DayOfWeek.Saturday => SaturdayOpen is { } o && SaturdayClose is { } c ? (o, c) : null,
        _ => (WeekdayOpen, WeekdayClose)
    };

    public (TimeOnly Start, TimeOnly End)? LunchOn(DayOfWeek day) =>
        day is DayOfWeek.Saturday or DayOfWeek.Sunday || LunchStart is null || LunchEnd is null
            ? null
            : (LunchStart.Value, LunchEnd.Value);
}
