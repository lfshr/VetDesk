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

    /// <summary>
    /// A limited company must show its registered name, number, place of registration and registered office on its
    /// invoices (Companies Act 2006). Left empty for a practice that isn't a company, and then not printed.
    /// </summary>
    public string? CompanyName { get; set; }
    public string? CompanyNumber { get; set; }
    public CompanyRegistration? CompanyRegisteredIn { get; set; }
    public string? RegisteredOffice { get; set; }

    /// <summary>Printed on invoices so clients can pay by transfer. Left empty to leave the section off.</summary>
    public string? BankAccountName { get; set; }
    public string? BankSortCode { get; set; }
    public string? BankAccountNumber { get; set; }

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

/// <summary>The part of the UK a company is registered in, as Companies House records it.</summary>
public enum CompanyRegistration
{
    EnglandAndWales,
    Scotland,
    NorthernIreland
}
