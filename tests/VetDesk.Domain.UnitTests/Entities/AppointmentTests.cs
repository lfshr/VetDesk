using VetDesk.Domain.Entities.Appointments;
using VetDesk.Domain.Entities.Clients;
using VetDesk.Domain.Entities.Patients;

namespace VetDesk.Domain.UnitTests.Entities;

[TestFixture]
public class AppointmentTests
{
    private static readonly DateTime Now = new(2026, 10, 9, 9, 0, 0, DateTimeKind.Utc);

    private static Appointment NewAppointment() => new()
    {
        Patient = new Patient { Name = "Biscuit", Species = "Dog", Client = new Client { LastName = "Clarke", Phone = "07700900412" } },
        Type = new AppointmentType { Name = "Consultation", Minutes = 15 },
        Reason = "Lameness",
        StartUtc = Now,
        Minutes = 15,
    };

    [Test]
    public void CheckIn_FromBooked_RecordsTime()
    {
        var appointment = NewAppointment();

        appointment.ChangeStatus(AppointmentStatus.CheckedIn, Now).ShouldBeTrue();

        appointment.Status.ShouldBe(AppointmentStatus.CheckedIn);
        appointment.CheckedInUtc.ShouldBe(Now);
    }

    [Test]
    public void UndoCheckIn_ClearsTime()
    {
        var appointment = NewAppointment();
        appointment.ChangeStatus(AppointmentStatus.CheckedIn, Now);

        appointment.ChangeStatus(AppointmentStatus.Booked, Now).ShouldBeTrue();

        appointment.CheckedInUtc.ShouldBeNull();
    }

    [Test]
    public void Done_FromBooked_IsRefused()
    {
        var appointment = NewAppointment();

        appointment.ChangeStatus(AppointmentStatus.Done, Now).ShouldBeFalse();

        appointment.Status.ShouldBe(AppointmentStatus.Booked);
    }

    [Test]
    public void Cancel_AfterDone_IsRefused()
    {
        var appointment = NewAppointment();
        appointment.ChangeStatus(AppointmentStatus.InConsult, Now);
        appointment.ChangeStatus(AppointmentStatus.Done, Now);

        appointment.ChangeStatus(AppointmentStatus.Cancelled, Now).ShouldBeFalse();
    }

    [TestCase(0, 15, true)]
    [TestCase(14, 15, true)]
    [TestCase(15, 15, false)]
    [TestCase(-15, 15, false)]
    [TestCase(-10, 15, true)]
    public void Overlaps_TouchingEndsDoNotOverlap(int startOffsetMinutes, int minutes, bool expected)
    {
        var appointment = NewAppointment();
        var start = Now.AddMinutes(startOffsetMinutes);

        appointment.Overlaps(start, start.AddMinutes(minutes)).ShouldBe(expected);
    }
}
