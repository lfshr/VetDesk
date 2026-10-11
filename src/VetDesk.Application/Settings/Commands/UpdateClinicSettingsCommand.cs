using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Practice;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Settings.Commands;

public enum UpdateClinicSettingsError { NotAllowed, InvalidRequest, ChangedElsewhere }

public class UpdateClinicSettingsCommand : IRequest<Result>
{
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? VatNumber { get; init; }
    public string? CompanyName { get; init; }
    public string? CompanyNumber { get; init; }
    public CompanyRegistration? CompanyRegisteredIn { get; init; }
    public string? RegisteredOffice { get; init; }
    public string? BankAccountName { get; init; }
    public string? BankSortCode { get; init; }
    public string? BankAccountNumber { get; init; }
    public TimeOnly WeekdayOpen { get; init; }
    public TimeOnly WeekdayClose { get; init; }
    public TimeOnly? SaturdayOpen { get; init; }
    public TimeOnly? SaturdayClose { get; init; }
    public TimeOnly? LunchStart { get; init; }
    public TimeOnly? LunchEnd { get; init; }
    public int PaymentTermsDays { get; init; }
    public decimal OpeningFloat { get; init; }
    public int AutoLockMinutes { get; init; }
    public string? BackupFolder { get; init; }
    public TimeOnly BackupTime { get; init; }
    public Guid Version { get; init; }
}

public class UpdateClinicSettingsCommandHandler : IRequestHandler<UpdateClinicSettingsCommand, Result>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public UpdateClinicSettingsCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result> Handle(UpdateClinicSettingsCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.ManageSettings))
            return Result.Failure(UpdateClinicSettingsError.NotAllowed, Access.Denied(Permission.ManageSettings));
        if (Validate(request) is { } problem)
            return Result.Failure(UpdateClinicSettingsError.InvalidRequest, problem);

        var s = await _dbContext.ClinicSettings.FirstAsync(cancellationToken);
        if (s.Version != request.Version)
            return Result.Failure(UpdateClinicSettingsError.ChangedElsewhere, Saving.ChangedElsewhere);

        s.Name = request.Name.Trim();
        s.Address = Text.Clean(request.Address);
        s.Phone = Text.Clean(request.Phone);
        s.Email = Text.Clean(request.Email);
        s.VatNumber = Text.Clean(request.VatNumber)?.ToUpperInvariant();
        s.CompanyName = Text.Clean(request.CompanyName);
        s.CompanyNumber = CompanyNumberOf(request.CompanyNumber);
        s.CompanyRegisteredIn = s.CompanyNumber is null ? null : request.CompanyRegisteredIn;
        s.RegisteredOffice = Text.Clean(request.RegisteredOffice);
        s.BankAccountName = Text.Clean(request.BankAccountName);
        s.BankSortCode = SortCodeOf(request.BankSortCode);
        s.BankAccountNumber = DigitsOf(request.BankAccountNumber);
        s.WeekdayOpen = request.WeekdayOpen;
        s.WeekdayClose = request.WeekdayClose;
        s.SaturdayOpen = request.SaturdayOpen;
        s.SaturdayClose = request.SaturdayClose;
        s.LunchStart = request.LunchStart;
        s.LunchEnd = request.LunchEnd;
        s.PaymentTermsDays = request.PaymentTermsDays;
        s.OpeningFloat = Money.Round(request.OpeningFloat);
        s.AutoLockMinutes = request.AutoLockMinutes;
        s.BackupFolder = Text.Clean(request.BackupFolder);
        s.BackupTime = request.BackupTime;

        if (!await _dbContext.TrySaveChangesAsync(cancellationToken))
            return Result.Failure(UpdateClinicSettingsError.ChangedElsewhere, Saving.ChangedElsewhere);
        return Result.Success();
    }

    internal static string? Validate(UpdateClinicSettingsCommand r)
    {
        if (!Text.Has(r.Name))
            return "Enter the practice name.";
        if (r.WeekdayClose <= r.WeekdayOpen)
            return "Weekday closing time must be after opening time.";
        if ((r.SaturdayOpen is null) != (r.SaturdayClose is null) || r.SaturdayClose <= r.SaturdayOpen)
            return "Set both Saturday times with closing after opening, or leave both empty.";
        if ((r.LunchStart is null) != (r.LunchEnd is null) || r.LunchEnd <= r.LunchStart)
            return "Set both lunch times with the end after the start, or leave both empty.";
        if (r.PaymentTermsDays is < 0 or > 120)
            return "Payment terms must be between 0 and 120 days.";
        if (r.OpeningFloat < 0)
            return "The float can't be negative.";
        if (r.AutoLockMinutes is < 1 or > 120)
            return "Auto-lock must be between 1 and 120 minutes.";
        if (Text.Has(r.VatNumber) && !System.Text.RegularExpressions.Regex.IsMatch(r.VatNumber!.Replace(" ", ""), "^(GB)?[0-9]{9}([0-9]{3})?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            return "A UK VAT number is GB followed by 9 digits.";
        if (CompanyNumberOf(r.CompanyNumber) is { } number)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(number, "^[A-Z0-9]{8}$"))
                return "A company number is 8 characters, such as 01234567 or SC123456.";
            if (!Text.Has(r.CompanyName) || r.CompanyRegisteredIn is null || !Text.Has(r.RegisteredOffice))
                return "Enter the company's registered name, where it's registered and its registered office, or clear the company number.";
        }
        else if (Text.Has(r.CompanyName) || Text.Has(r.RegisteredOffice))
            return "Enter the company number, or clear the company details.";
        var bank = new[] { r.BankAccountName, r.BankSortCode, r.BankAccountNumber };
        if (bank.Any(Text.Has))
        {
            if (!bank.All(Text.Has))
                return "Enter the account name, sort code and account number for bank transfers, or clear all three.";
            if (DigitsOf(r.BankSortCode)!.Length != 6 || !System.Text.RegularExpressions.Regex.IsMatch(r.BankSortCode!, "^[0-9 -]+$"))
                return "A sort code is 6 digits, such as 12-34-56.";
            if (DigitsOf(r.BankAccountNumber)!.Length != 8 || !System.Text.RegularExpressions.Regex.IsMatch(r.BankAccountNumber!, "^[0-9 ]+$"))
                return "A UK account number is 8 digits.";
        }
        return null;
    }

    /// <summary>Companies House numbers are 8 characters; people often type them with spaces or in lower case.</summary>
    private static string? CompanyNumberOf(string? value) => Text.Clean(value)?.Replace(" ", "").ToUpperInvariant();

    private static string? DigitsOf(string? value) => Text.Clean(value) is { } v ? new string(v.Where(char.IsAsciiDigit).ToArray()) : null;

    /// <summary>Stored as 12-34-56, the way it's printed, whatever separators were typed.</summary>
    private static string? SortCodeOf(string? value) => DigitsOf(value) is { Length: 6 } d ? $"{d[..2]}-{d[2..4]}-{d[4..]}" : null;
}
