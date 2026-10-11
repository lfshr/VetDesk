using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Invoices;
using VetDesk.Application.Invoices.Commands;
using VetDesk.Application.Invoices.Queries;
using VetDesk.Application.Settings.Commands;
using VetDesk.Domain.Entities.Patients;
using VetDesk.Domain.Entities.Practice;
using VetDesk.Domain.Entities.Products;
using VetDesk.Domain.Entities.Staff;

namespace VetDesk.Application.UnitTests.Handlers;

[TestFixture]
public class InvoicePrintoutTests
{
    private TestDatabase _database = null!;
    private Patient _patient = null!;

    [SetUp]
    public async Task SetUp()
    {
        _database = await TestDatabase.CreateAsync();
        await _database.SignInAsync(await _database.AddStaffAsync("Liam", "Price", StaffRole.Admin));
        _patient = await _database.AddPatientAsync();
    }

    [TearDown]
    public void TearDown() => _database.Dispose();

    private async Task<int> InvoiceAsync(params NewInvoiceLine[] lines)
    {
        var clientId = (await _database.Db.Patients.Include(p => p.Client).FirstAsync(p => p.Id == _patient.Id)).Client.Id;
        var result = await new CreateInvoiceCommandHandler(_database.Db, _database.User, _database.Clock)
            .Handle(new CreateInvoiceCommand { ClientId = clientId, PatientId = _patient.Id, Lines = lines }, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue(result.ToString());
        return result.Value.Id;
    }

    private Task<SimpleResult.Result<InvoicePrintout>> PrintoutAsync(int id) =>
        new GetInvoicePrintoutQueryHandler(_database.NewContext(), _database.User, _database.Clock)
            .Handle(new GetInvoicePrintoutQuery { Id = id }, CancellationToken.None);

    /// <summary>Saves the current settings unchanged apart from the company and bank details given.</summary>
    private async Task<SimpleResult.Result> SaveSettingsAsync(string? companyName = null, string? companyNumber = null,
        CompanyRegistration? registeredIn = null, string? registeredOffice = null, string? bankName = null, string? sortCode = null, string? account = null)
    {
        var s = await _database.NewContext().ClinicSettings.AsNoTracking().FirstAsync();
        var command = new UpdateClinicSettingsCommand
        {
            Name = s.Name, Address = s.Address, Phone = s.Phone, Email = s.Email, VatNumber = s.VatNumber,
            WeekdayOpen = s.WeekdayOpen, WeekdayClose = s.WeekdayClose, SaturdayOpen = s.SaturdayOpen, SaturdayClose = s.SaturdayClose,
            LunchStart = s.LunchStart, LunchEnd = s.LunchEnd, PaymentTermsDays = s.PaymentTermsDays, OpeningFloat = s.OpeningFloat,
            AutoLockMinutes = s.AutoLockMinutes, BackupFolder = s.BackupFolder, BackupTime = s.BackupTime, Version = s.Version,
            CompanyName = companyName, CompanyNumber = companyNumber, CompanyRegisteredIn = registeredIn, RegisteredOffice = registeredOffice,
            BankAccountName = bankName, BankSortCode = sortCode, BankAccountNumber = account
        };
        return await new UpdateClinicSettingsCommandHandler(_database.NewContext(), _database.User).Handle(command, CancellationToken.None);
    }

    [Test]
    public async Task Printout_MixedRates_ShowsVatByRate_ThatAddsUpToTheInvoiceTotals()
    {
        var consult = await _database.AddProductAsync("Consultation", ProductCategory.Service, 52m);
        var food = await _database.AddProductAsync("Renal diet", ProductCategory.Retail, 24m, stock: 5, vat: VatRate.Zero);
        var id = await InvoiceAsync(new NewInvoiceLine(consult.Id, 1), new NewInvoiceLine(food.Id, 1));

        var printout = (await PrintoutAsync(id)).Value;

        printout.VatByRate.Select(r => (r.Rate, r.Net, r.Vat)).ShouldBe([(VatRate.Standard, 52m, 10.40m), (VatRate.Zero, 24m, 0m)]);
        printout.VatByRate.Sum(r => r.Net).ShouldBe(printout.Invoice.Net);
        printout.VatByRate.Sum(r => r.Vat).ShouldBe(printout.Invoice.Vat);
        printout.Patient.ShouldNotBeNull().Name.ShouldBe("Biscuit");
    }

    [Test]
    public async Task Printout_LeavesCompanyAndBankDetailsOff_UntilTheyAreSet()
    {
        var consult = await _database.AddProductAsync("Consultation", ProductCategory.Service, 52m);
        var id = await InvoiceAsync(new NewInvoiceLine(consult.Id, 1));

        var before = (await PrintoutAsync(id)).Value.Practice;
        (await SaveSettingsAsync("Northfield Veterinary Clinic Ltd", "sc 123456", CompanyRegistration.Scotland, "14 Gloucester Road, Bristol",
            "Northfield Veterinary Clinic Ltd", "12 34 56", "1234 5678")).IsSuccess.ShouldBeTrue();
        var after = (await PrintoutAsync(id)).Value.Practice;

        before.HasCompanyDetails.ShouldBeFalse();
        before.HasBankDetails.ShouldBeFalse();
        after.HasCompanyDetails.ShouldBeTrue();
        after.CompanyNumber.ShouldBe("SC123456");
        after.HasBankDetails.ShouldBeTrue();
        after.BankSortCode.ShouldBe("12-34-56");
        after.BankAccountNumber.ShouldBe("12345678");
    }

    [TestCase("Northfield Ltd", "01234567", null, "14 Gloucester Road", "where it's registered")]
    [TestCase("Northfield Ltd", "1234", CompanyRegistration.EnglandAndWales, "14 Gloucester Road", "8 characters")]
    [TestCase("Northfield Ltd", null, null, null, "company number")]
    public async Task CompanyDetails_Incomplete_AreRefused(string? name, string? number, CompanyRegistration? registeredIn, string? office, string message)
    {
        var result = await SaveSettingsAsync(name, number, registeredIn, office);

        result.ErrorCode.ShouldBe(UpdateClinicSettingsError.InvalidRequest);
        result.ToString().ShouldContain(message);
    }

    [TestCase("Northfield Ltd", null, "12345678", "all three")]
    [TestCase("Northfield Ltd", "12-34-5", "12345678", "sort code")]
    [TestCase("Northfield Ltd", "12-34-56", "1234567", "8 digits")]
    public async Task BankDetails_IncompleteOrMistyped_AreRefused(string? name, string? sortCode, string? account, string message)
    {
        var result = await SaveSettingsAsync(bankName: name, sortCode: sortCode, account: account);

        result.ErrorCode.ShouldBe(UpdateClinicSettingsError.InvalidRequest);
        result.ToString().ShouldContain(message);
    }
}
