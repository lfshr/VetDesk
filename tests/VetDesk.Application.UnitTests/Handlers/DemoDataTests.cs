using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Patients.Queries;
using VetDesk.Application.Reports;
using VetDesk.Application.Reports.Queries;
using VetDesk.Application.Today.Queries;

namespace VetDesk.Application.UnitTests.Handlers;

/// <summary>The demo data is what the screens are first seen with, so it must load and read back cleanly.</summary>
[TestFixture]
public class DemoDataTests
{
    private TestDatabase _database = null!;

    [SetUp]
    public async Task SetUp()
    {
        _database = await TestDatabase.CreateAsync(demoData: true);
        await _database.SignInAsync(await _database.Db.StaffMembers.FirstAsync(s => s.LastName == "Patel"));
    }

    [TearDown]
    public void TearDown() => _database.Dispose();

    [Test]
    public async Task Today_ShowsTheDaysAppointments()
    {
        var result = await new GetTodayQueryHandler(_database.Db, _database.User, _database.Clock).Handle(new GetTodayQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue(result.ToString());
        result.Value.Appointments.Count.ShouldBe(12);
        result.Value.WaitingNow.ShouldBe(1);
        result.Value.FollowUps.ShouldNotBeEmpty();
    }

    [Test]
    public async Task PatientRecord_LoadsHistoryAndAllergy()
    {
        var biscuit = await _database.Db.Patients.FirstAsync(p => p.Name == "Biscuit");

        var result = await new GetPatientQueryHandler(_database.Db, _database.User, _database.Clock).Handle(new GetPatientQuery { Id = biscuit.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue(result.ToString());
        result.Value.Allergies.Single().Label.ShouldBe("Allergy: amoxicillin");
        result.Value.History.Count(h => h.ConsultationId is not null).ShouldBe(4);
        result.Value.OpenConsultationId.ShouldNotBeNull();
    }

    [Test]
    public async Task Search_FindsByPhoneDigitsAndPostcode()
    {
        var handler = new SearchPatientsQueryHandler(_database.Db, _database.User, _database.Clock);

        var byPhone = await handler.Handle(new SearchPatientsQuery { Text = "07700900412" }, CancellationToken.None);
        var byPostcode = await handler.Handle(new SearchPatientsQuery { Text = "bs6 5qt" }, CancellationToken.None);
        var byName = await handler.Handle(new SearchPatientsQuery { Text = "BISC" }, CancellationToken.None);

        byPhone.Value.Select(p => p.Name).ShouldBe(["Biscuit", "Mabel"], ignoreOrder: true);
        byPostcode.Value.Count.ShouldBe(2);
        byName.Value.Single().Name.ShouldBe("Biscuit");
    }

    [TestCase(StandardReport.DaySheet)]
    [TestCase(StandardReport.VatSummary)]
    [TestCase(StandardReport.AgedDebtors)]
    [TestCase(StandardReport.ControlledDrugs)]
    [TestCase(StandardReport.ClinicianProductivity)]
    [TestCase(StandardReport.StockValuation)]
    public async Task StandardReports_Run(StandardReport report)
    {
        var today = new DateOnly(2026, 10, 9);
        var result = await new GetStandardReportQueryHandler(_database.Db, _database.User, _database.Clock)
            .Handle(new GetStandardReportQuery { Report = report, From = today.AddDays(-400), To = today }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue(result.ToString());
        result.Value.Columns.ShouldNotBeEmpty();
    }

    [TestCase(ReportPeriod.Week)]
    [TestCase(ReportPeriod.Month)]
    [TestCase(ReportPeriod.Quarter)]
    public async Task ReportSummary_Runs(ReportPeriod period)
    {
        var result = await new GetReportSummaryQueryHandler(_database.Db, _database.User, _database.Clock)
            .Handle(new GetReportSummaryQuery { Period = period }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue(result.ToString());
        result.Value.RevenueBars.ShouldNotBeEmpty();
    }
}
