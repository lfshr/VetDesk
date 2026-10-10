using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Appointments.Commands;
using VetDesk.Application.Appointments.Queries;
using VetDesk.Domain.Entities.Appointments;
using VetDesk.Domain.Entities.Patients;
using VetDesk.Domain.Entities.Staff;

namespace VetDesk.Application.UnitTests.Handlers;

[TestFixture]
public class AppointmentHandlerTests
{
    // 10:00 local (BST) on the test clock's day.
    private static readonly DateTime TenAm = new(2026, 10, 9, 9, 0, 0, DateTimeKind.Utc);

    private TestDatabase _database = null!;
    private StaffMember _vet = null!;
    private Patient _patient = null!;
    private AppointmentType _consultation = null!;

    [SetUp]
    public async Task SetUp()
    {
        _database = await TestDatabase.CreateAsync();
        _vet = await _database.AddStaffAsync("Asha", "Patel", StaffRole.Vet);
        await _database.SignInAsync(await _database.AddStaffAsync("Jack", "Bennett", StaffRole.Reception));
        _patient = await _database.AddPatientAsync();
        _consultation = await _database.Db.AppointmentTypes.FirstAsync(t => t.Name == "Consultation");
    }

    [TearDown]
    public void TearDown() => _database.Dispose();

    private Task<SimpleResult.Result<int>> BookAsync(DateTime startUtc, int? patientId = null, int? vetId = null, int minutes = 15) =>
        new SaveAppointmentCommandHandler(_database.Db, _database.User).Handle(new SaveAppointmentCommand
        {
            PatientId = patientId ?? _patient.Id, TypeId = _consultation.Id, ClinicianId = vetId, StartUtc = startUtc, Minutes = minutes, Reason = "Lameness",
        }, CancellationToken.None);

    [Test]
    public async Task SameVet_Overlapping_IsAClash()
    {
        var other = await _database.AddPatientAsync("Mabel");
        await BookAsync(TenAm, vetId: _vet.Id, minutes: 30);

        var result = await BookAsync(TenAm.AddMinutes(15), patientId: other.Id, vetId: _vet.Id);

        result.ErrorCode.ShouldBe(SaveAppointmentError.Clash);
    }

    [Test]
    public async Task DifferentPatients_NoVet_CanShareASlot()
    {
        var other = await _database.AddPatientAsync("Mabel");
        await BookAsync(TenAm);

        var result = await BookAsync(TenAm, patientId: other.Id);

        result.IsSuccess.ShouldBeTrue(result.ToString());
    }

    [Test]
    public async Task SamePatient_Overlapping_IsAClash()
    {
        await BookAsync(TenAm);

        var result = await BookAsync(TenAm.AddMinutes(5));

        result.ErrorCode.ShouldBe(SaveAppointmentError.Clash);
    }

    [Test]
    public async Task CancelledAppointment_FreesTheSlot()
    {
        var first = await BookAsync(TenAm, vetId: _vet.Id);
        await new ChangeAppointmentStatusCommandHandler(_database.Db, _database.User, _database.Clock)
            .Handle(new ChangeAppointmentStatusCommand { Id = first.Value, Status = AppointmentStatus.Cancelled }, CancellationToken.None);

        var result = await BookAsync(TenAm, vetId: _vet.Id);

        result.IsSuccess.ShouldBeTrue(result.ToString());
    }

    [Test]
    public async Task CheckIn_RaisesNotification()
    {
        var booked = await BookAsync(TenAm);

        var result = await new ChangeAppointmentStatusCommandHandler(_database.Db, _database.User, _database.Clock)
            .Handle(new ChangeAppointmentStatusCommand { Id = booked.Value, Status = AppointmentStatus.CheckedIn }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        (await _database.Db.Notifications.SingleAsync()).Text.ShouldContain("checked in");
    }

    [Test]
    public async Task Slots_MarkLunchPastAndBooked_InLocalTime()
    {
        _database.Clock.UtcNow = new DateTime(2026, 10, 9, 8, 40, 0, DateTimeKind.Utc); // 09:40 local
        await BookAsync(TenAm);

        var day = await new GetBookingSlotsQueryHandler(_database.Db, _database.User, _database.Clock)
            .Handle(new GetBookingSlotsQuery { Date = new DateOnly(2026, 10, 9), Minutes = 15 }, CancellationToken.None);

        var slots = day.Value.Slots.ToDictionary(s => s.Time);
        slots[new TimeOnly(9, 30)].State.ShouldBe(SlotState.Past);
        slots[new TimeOnly(10, 0)].State.ShouldBe(SlotState.Booked);
        slots[new TimeOnly(10, 15)].State.ShouldBe(SlotState.Free);
        slots[new TimeOnly(13, 15)].State.ShouldBe(SlotState.Lunch);
        day.Value.Slots[^1].Time.ShouldBe(new TimeOnly(18, 45));
    }

    [Test]
    public async Task Slots_Sunday_IsClosed_ButOffersTheWeekdayHoursMarkedClosed()
    {
        var day = await new GetBookingSlotsQueryHandler(_database.Db, _database.User, _database.Clock)
            .Handle(new GetBookingSlotsQuery { Date = new DateOnly(2026, 10, 11) }, CancellationToken.None);

        day.Value.IsClosed.ShouldBeTrue();
        day.Value.Slots[0].Time.ShouldBe(new TimeOnly(8, 0));
        day.Value.Slots[^1].Time.ShouldBe(new TimeOnly(18, 45));
        day.Value.Slots.ShouldAllBe(s => s.State == SlotState.Closed);
    }

    [Test]
    public async Task Slots_Saturday_MarksTimesOutsideItsHoursClosed()
    {
        var day = await new GetBookingSlotsQueryHandler(_database.Db, _database.User, _database.Clock)
            .Handle(new GetBookingSlotsQuery { Date = new DateOnly(2026, 10, 10) }, CancellationToken.None);

        var slots = day.Value.Slots.ToDictionary(s => s.Time);
        day.Value.IsClosed.ShouldBeFalse();
        slots[new TimeOnly(8, 45)].State.ShouldBe(SlotState.Closed);
        slots[new TimeOnly(9, 0)].State.ShouldBe(SlotState.Free);
        slots[new TimeOnly(12, 45)].State.ShouldBe(SlotState.Free);
        slots[new TimeOnly(13, 0)].State.ShouldBe(SlotState.Closed);
        slots.ShouldContainKey(new TimeOnly(18, 45));
    }
}
