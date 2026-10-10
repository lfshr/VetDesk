using VetDesk.Domain.Entities.Staff;

namespace VetDesk.Domain.UnitTests.Entities;

[TestFixture]
public class StaffMemberTests
{
    private static readonly DateTime Now = new(2026, 10, 9, 9, 0, 0, DateTimeKind.Utc);

    [Test]
    public void WrongPins_LockAfterTheLastAttempt_ForTheLockoutPeriod()
    {
        var staff = new StaffMember { FirstName = "Janet", Role = StaffRole.Nurse };

        for (var i = 1; i < StaffMember.MaxPinAttempts; i++)
            staff.RecordWrongPin(Now);
        staff.IsPinLocked(Now).ShouldBeFalse();

        staff.RecordWrongPin(Now);

        staff.IsPinLocked(Now).ShouldBeTrue();
        staff.IsPinLocked(Now + StaffMember.PinLockout).ShouldBeFalse();
    }

    [Test]
    public void RightPin_ClearsTheCount()
    {
        var staff = new StaffMember { FirstName = "Janet", Role = StaffRole.Nurse };
        staff.RecordWrongPin(Now);
        staff.RecordWrongPin(Now);

        staff.RecordRightPin();

        staff.FailedPinAttempts.ShouldBe(0);
        staff.PinLockedUntilUtc.ShouldBeNull();
    }

    [TestCase(StaffRole.Vet, "", "Douglas")]
    [TestCase(StaffRole.Vet, "Patel", "Douglas")]
    [TestCase(StaffRole.Nurse, "Reid", "Douglas")]
    public void ShortName_IsTheFirstName_WithNoTitle(StaffRole role, string surname, string expected)
    {
        new StaffMember { FirstName = "Douglas", LastName = surname, Role = role }.ShortName.ShouldBe(expected);
    }
}
