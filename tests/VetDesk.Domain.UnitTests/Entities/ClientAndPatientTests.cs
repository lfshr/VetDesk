using VetDesk.Domain.Entities.Clients;
using VetDesk.Domain.Entities.Patients;

namespace VetDesk.Domain.UnitTests.Entities;

[TestFixture]
public class ClientAndPatientTests
{
    [TestCase("07700 900412", "07700900412")]
    [TestCase("+44 7700 900412", "07700900412")]
    [TestCase("(0117) 496-0321", "01174960321")]
    public void PhoneDigits_IgnoresFormatting(string typed, string expected)
    {
        new Client { LastName = "Clarke", Phone = typed }.PhoneDigits.ShouldBe(expected);
    }

    [TestCase("Hannah Clarke", "Hannah", "Clarke")]
    [TestCase("Mary Ann van Dyke", "Mary Ann van", "Dyke")]
    [TestCase("Cher", "", "Cher")]
    public void SplitName_TakesLastWordAsSurname(string typed, string first, string last)
    {
        Client.SplitName(typed).ShouldBe((first, last));
    }

    [Test]
    public void AgeLabel_UnderFourMonths_InWeeks()
    {
        var pet = new Patient { Name = "Miso", Species = "Cat", Client = null!, DateOfBirth = new DateOnly(2026, 9, 1) };

        pet.AgeLabel(new DateOnly(2026, 10, 9)).ShouldBe("5 wks");
    }

    [Test]
    public void AgeLabel_Adult_WithMonths()
    {
        var pet = new Patient { Name = "Biscuit", Species = "Dog", Client = null!, DateOfBirth = new DateOnly(2020, 8, 1) };

        pet.AgeLabel(new DateOnly(2026, 10, 9)).ShouldBe("6 yrs");
        pet.AgeLabel(new DateOnly(2026, 10, 9), withMonths: true).ShouldBe("6 yrs 2 mths");
    }

    [Test]
    public void AgeLabel_OneYear_IsSingular()
    {
        var pet = new Patient { Name = "Nala", Species = "Cat", Client = null!, DateOfBirth = new DateOnly(2025, 9, 1) };

        pet.AgeLabel(new DateOnly(2026, 10, 9)).ShouldBe("1 yr");
        pet.AgeLabel(new DateOnly(2026, 10, 9), withMonths: true).ShouldBe("1 yr 1 mth");
    }

    [Test]
    public void ClientNumber_IsPadded()
    {
        Client.FormatNumber(42).ShouldBe("CL-00042");
        Patient.FormatNumber(42).ShouldBe("PT-000042");
    }
}
