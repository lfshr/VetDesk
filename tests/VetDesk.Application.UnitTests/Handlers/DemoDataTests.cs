using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Clients.Queries;
using VetDesk.Application.Patients.Queries;
using VetDesk.Application.Reports;
using VetDesk.Application.Reports.Queries;
using VetDesk.Application.Today.Queries;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Appointments;
using VetDesk.Domain.Services;

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
        await _database.SignInAsync(await _database.Db.StaffMembers.FirstAsync(s => s.FirstName == "Douglas"));
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

    [TestCase(ReportPeriod.Week, "Fri")]
    [TestCase(ReportPeriod.Month, "8–14")]
    [TestCase(ReportPeriod.Quarter, "Oct")]
    public async Task ReportSummary_PicksOutTheBarTodayFallsIn(ReportPeriod period, string label)
    {
        var result = await new GetReportSummaryQueryHandler(_database.Db, _database.User, _database.Clock)
            .Handle(new GetReportSummaryQuery { Period = period }, CancellationToken.None);

        result.Value.RevenueBars.Where(b => b.IsCurrent).Select(b => b.Label).ShouldBe([label]);
    }

    [TestCase(null)]
    [TestCase("clarke")]
    public async Task PatientCounts_MatchTheListForEachSpeciesFilter(string? text)
    {
        var counts = await new CountPatientsQueryHandler(_database.Db, _database.User)
            .Handle(new CountPatientsQuery { Text = text }, CancellationToken.None);
        var search = new SearchPatientsQueryHandler(_database.Db, _database.User, _database.Clock);

        foreach (var filter in Enum.GetValues<SpeciesFilter>())
        {
            var rows = await search.Handle(new SearchPatientsQuery { Text = text, Species = filter }, CancellationToken.None);
            counts.Value[filter].ShouldBe(rows.Value.Count, filter.ToString());
        }
        counts.Value[SpeciesFilter.Dogs].ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task Appointments_AreInOpeningHoursAndNoClinicianIsDoubleBooked()
    {
        var settings = await _database.Db.ClinicSettings.SingleAsync();
        var appointments = (await _database.Db.Appointments.Include(a => a.Patient).Include(a => a.Clinician).ToListAsync())
            .Where(a => a.Status is not (AppointmentStatus.Cancelled or AppointmentStatus.NoShow))
            .ToList();

        var problems = new List<string>();
        foreach (var a in appointments)
        {
            var start = ClinicTime.ToLocal(a.StartUtc);
            var hours = settings.HoursOn(start.DayOfWeek);
            if (hours is null || TimeOnly.FromDateTime(start) < hours.Value.Open || TimeOnly.FromDateTime(ClinicTime.ToLocal(a.EndUtc)) > hours.Value.Close)
                problems.Add($"{a.Patient.Name} is booked out of hours, {start:ddd d MMM HH:mm}");
        }
        foreach (var day in appointments.Where(a => a.Clinician is not null).GroupBy(a => (a.Clinician!.Id, ClinicTime.ToLocal(a.StartUtc).Date)))
        {
            var ordered = day.OrderBy(a => a.StartUtc).ToList();
            for (var i = 1; i < ordered.Count; i++)
                if (ordered[i].StartUtc < ordered[i - 1].EndUtc)
                    problems.Add($"{ordered[i].Clinician!.FirstName} has {ordered[i - 1].Patient.Name} and {ordered[i].Patient.Name} at once, {ClinicTime.ToLocal(ordered[i].StartUtc):ddd d MMM HH:mm}");
        }

        problems.ShouldBeEmpty();
    }

    [Test]
    public async Task Invoices_MatchTheirLinesAndConsultations()
    {
        var invoices = await _database.Db.Invoices.Include(i => i.Lines).Include(i => i.Payments).ToListAsync();
        var consultations = await _database.Db.Consultations.Include(c => c.Charges).Where(c => c.InvoiceId != null).ToListAsync();

        invoices.Select(i => i.Number).ShouldBeUnique();
        foreach (var invoice in invoices)
        {
            invoice.Lines.ShouldNotBeEmpty(invoice.Number);
            invoice.Total.ShouldBe(VatCalculator.For(invoice.Lines).Total, invoice.Number);
            invoice.Paid.ShouldBeLessThanOrEqualTo(invoice.Total, invoice.Number);
            invoice.Paid.ShouldBeGreaterThanOrEqualTo(0m, invoice.Number);
        }
        foreach (var consultation in consultations)
            consultation.Charges.Sum(ch => ch.Net).ShouldBe(invoices.Single(i => i.Id == consultation.InvoiceId).Net);
    }

    [TestCase(null)]
    [TestCase("clarke")]
    public async Task ClientCounts_MatchTheListForEachFilter(string? text)
    {
        var counts = await new CountClientsQueryHandler(_database.Db, _database.User, _database.Clock)
            .Handle(new CountClientsQuery { Text = text }, CancellationToken.None);
        var search = new SearchClientsQueryHandler(_database.Db, _database.User, _database.Clock);

        foreach (var filter in Enum.GetValues<ClientFilter>())
        {
            var rows = await search.Handle(new SearchClientsQuery { Text = text, Filter = filter }, CancellationToken.None);
            counts.Value[filter].ShouldBe(rows.Value.Count, filter.ToString());
        }
    }
}
