using VetDesk.UI.Services;

namespace VetDesk.UI.UnitTests;

public class OpenPatientsTests
{
    [Test]
    public void Show_AddsEachPatientOnceInTheOrderOpened()
    {
        var open = new OpenPatients();

        open.Show(1, "Biscuit", "Hannah Clarke", "patients/1");
        open.Show(2, "Bramble", "Dan Moore", "patients/2");
        open.Show(1, "Biscuit", "Hannah Clarke", "patients/1");

        open.Tabs.Select(t => t.PatientId).ShouldBe([1, 2]);
    }

    [Test]
    public void Show_AgainKeepsTheTabButUpdatesItsScreenAndName()
    {
        var open = new OpenPatients();
        open.Show(1, "Biscuit", "Hannah Clarke", "patients/1");

        open.Show(1, "Biscuit Jr", "Hannah Clarke", "consultations/40");

        var tab = open.Tabs.ShouldHaveSingleItem();
        tab.Name.ShouldBe("Biscuit Jr");
        tab.Path.ShouldBe("consultations/40");
    }

    [Test]
    public void Changed_IsRaisedOnlyWhenSomethingChanges()
    {
        var open = new OpenPatients();
        var raised = 0;
        open.Changed += () => raised++;

        open.Show(1, "Biscuit", "Hannah Clarke", "patients/1");
        open.Show(1, "Biscuit", "Hannah Clarke", "patients/1");
        open.Close(99);

        raised.ShouldBe(1);
    }

    [TestCase("patients/2", 2)]
    [TestCase("/patients/2", 2)]
    [TestCase("patients/2?tab=history", 2)]
    [TestCase("Patients/2", 2)]
    [TestCase("consultations/40", 3)]
    public void ActiveFor_IsTheTabShowingTheCurrentScreen(string currentPath, int patientId)
    {
        var open = ThreeOpen();

        open.ActiveFor(currentPath)!.PatientId.ShouldBe(patientId);
    }

    [TestCase("")]
    [TestCase("billing")]
    [TestCase("patients")]
    [TestCase("patients/20")]
    [TestCase("patients/3")]
    public void ActiveFor_IsNoneOffAnOpenPatientsScreen(string currentPath)
    {
        // Patient 3's tab is on its consultation, so its record is not that tab's screen.
        ThreeOpen().ActiveFor(currentPath).ShouldBeNull();
    }

    [TestCase(1, 2)]
    [TestCase(2, 3)]
    [TestCase(3, 2)]
    public void Close_ReturnsTheTabToTheRightElseTheLeft(int closed, int next)
    {
        var open = ThreeOpen();

        open.Close(closed)!.PatientId.ShouldBe(next);
        open.Find(closed).ShouldBeNull();
    }

    [Test]
    public void Close_TheLastTabReturnsNone()
    {
        var open = new OpenPatients();
        open.Show(1, "Biscuit", "Hannah Clarke", "patients/1");

        open.Close(1).ShouldBeNull();
        open.Tabs.ShouldBeEmpty();
    }

    [Test]
    public void Clear_ClosesEveryTab()
    {
        var open = ThreeOpen();

        open.Clear();

        open.Tabs.ShouldBeEmpty();
    }

    private static OpenPatients ThreeOpen()
    {
        var open = new OpenPatients();
        open.Show(1, "Biscuit", "Hannah Clarke", "patients/1");
        open.Show(2, "Bramble", "Dan Moore", "patients/2");
        open.Show(3, "Atlas", "Priya Shah", "consultations/40");
        return open;
    }
}
