using VetDesk.UI.Services.Live;

namespace VetDesk.UI.UnitTests;

public class PresenceTests
{
    private static readonly Guid Heather = Guid.NewGuid(), Douglas = Guid.NewGuid();

    [Test]
    public void Others_ListsEveryoneElseWithTheRecordOpen_AndWhoIsEditing()
    {
        var presence = new Presence();
        using var mine = presence.Join("Consultation:40", Heather, "Heather");
        using var theirs = presence.Join("Consultation:40", Douglas, "Douglas");
        using var elsewhere = presence.Join("Consultation:41", Guid.NewGuid(), "Lorna");

        theirs.SetEditing(true);

        presence.Others("Consultation:40", Heather).ShouldBe([new Presence.Viewer(Douglas, "Douglas", true)]);
    }

    [Test]
    public void Leaving_TellsTheOthers()
    {
        var presence = new Presence();
        var changed = new List<string>();
        var theirs = presence.Join("Patient:12", Douglas, "Douglas");
        presence.Changed += changed.Add;

        theirs.Dispose();

        changed.ShouldBe(["Patient:12"]);
        presence.Others("Patient:12", Heather).ShouldBeEmpty();
    }
}
