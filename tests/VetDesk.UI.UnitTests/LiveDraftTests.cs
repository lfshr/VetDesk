using Microsoft.Extensions.Logging.Abstractions;
using VetDesk.UI.Services.Live;

namespace VetDesk.UI.UnitTests;

public class LiveDraftTests
{
    private static readonly Guid Heather = Guid.NewGuid(), Douglas = Guid.NewGuid();

    private readonly ManualTime _time = new();
    private LiveDrafts _drafts = null!;

    [SetUp]
    public void SetUp() => _drafts = new LiveDrafts(NullLogger<LiveDrafts>.Instance, _time);

    private LiveDraft Open(string plan = "Recheck") =>
        _drafts.Join("Consultation:40", new Dictionary<string, string?> { ["Notes"] = plan, ["OwnerAdvice"] = null }, Guid.NewGuid());

    [Test]
    public void SecondDesk_JoinsTheSameDraft_AndSeesUnsavedTyping()
    {
        var first = Open();
        first.Type("Notes", "Recheck in a w", Heather, "Heather");

        var second = Open("something stale");

        second.ShouldBeSameAs(first);
        second["Notes"].ShouldBe("Recheck in a w");
    }

    [Test]
    public void Typing_IsSentToTheOtherDesks_AsItHappens()
    {
        var draft = Open();
        var heard = new List<LiveDraftEvent>();
        draft.Changed += heard.Add;

        draft.Type("Notes", "R", Heather, "Heather");

        heard.ShouldHaveSingleItem().ShouldBe(new LiveDraftEvent(LiveDraftEventKind.Typed, Heather, "Heather", "Notes"));
    }

    [Test]
    public void ABoxSomeoneIsTypingIn_IsReadOnlyElsewhere_UntilTheyMoveOn()
    {
        var draft = Open();
        draft.Type("Notes", "Heather's plan", Heather, "Heather");

        draft.TypingBy("Notes", Douglas).ShouldBe("Heather");
        draft.TypingBy("Notes", Heather).ShouldBeNull();
        draft.Type("Notes", "Douglas's plan", Douglas, "Douglas").ShouldBeFalse();
        draft["Notes"].ShouldBe("Heather's plan");

        // Other boxes stay free.
        draft.Type("OwnerAdvice", "Stable", Douglas, "Douglas").ShouldBeTrue();

        draft.Release("Notes", Heather);
        draft.Type("Notes", "Douglas's plan", Douglas, "Douglas").ShouldBeTrue();
    }

    [Test]
    public void ABox_IsFreedAfterAPauseInTyping()
    {
        var draft = Open();
        draft.Type("Notes", "Heather's plan", Heather, "Heather");

        _time.Advance(LiveDrafts.HoldFor + TimeSpan.FromSeconds(1));

        draft.TypingBy("Notes", Douglas).ShouldBeNull();
        draft.Type("Notes", "Douglas's plan", Douglas, "Douglas").ShouldBeTrue();
    }

    [Test]
    public void Saving_ClearsWhatWasSaved_ButNotWhatWasTypedWhileItSaved()
    {
        var draft = Open();
        draft.Type("Notes", "Recheck in a week", Heather, "Heather");
        var sent = draft.Values;
        draft.Type("OwnerAdvice", "Stable", Douglas, "Douglas"); // typed while the save was on its way
        var version = Guid.NewGuid();

        draft.MarkSaved(sent, version, Heather, "Heather");

        draft.Version.ShouldBe(version);
        draft.Unsaved.ShouldBe(["OwnerAdvice"]);
    }

    [Test]
    public void ClearingABox_ThatWasNeverFilledIn_IsNotAChange()
    {
        var draft = Open();
        draft.Type("OwnerAdvice", "", Heather, "Heather");

        draft.IsDirty.ShouldBeFalse();
    }

    [Test]
    public void ASaveMadeElsewhere_UpdatesUntouchedBoxes_AndKeepsUnsavedTyping()
    {
        var draft = Open();
        draft.Type("OwnerAdvice", "Stable", Heather, "Heather");
        var version = Guid.NewGuid();

        draft.Reset(new Dictionary<string, string?> { ["Notes"] = "Saved elsewhere", ["OwnerAdvice"] = "Saved elsewhere" }, version);

        draft["Notes"].ShouldBe("Saved elsewhere");
        draft["OwnerAdvice"].ShouldBe("Stable");
        draft.Version.ShouldBe(version);
    }

    [Test]
    public void WhileSomeoneCompletesTheRecord_NobodyElseCanType()
    {
        var draft = Open();
        draft.Freeze(Heather, "Heather").ShouldBeTrue();

        draft.Freeze(Douglas, "Douglas").ShouldBeFalse();
        draft.Type("OwnerAdvice", "Stable", Douglas, "Douglas").ShouldBeFalse();
        draft.FrozenBy(Douglas).ShouldBe("Heather");
        draft.TypingBy("OwnerAdvice", Douglas).ShouldBe("Heather");

        draft.Unfreeze(Heather);
        draft.Type("OwnerAdvice", "Stable", Douglas, "Douglas").ShouldBeTrue();
    }

    [Test]
    public void TheDraftGoes_WhenTheLastDeskLeaves()
    {
        var first = Open();
        var second = Open();
        first.Type("Notes", "Unsaved", Heather, "Heather");

        _drafts.Leave(first);
        Open().ShouldBeSameAs(first);
        _drafts.Leave(first);
        _drafts.Leave(second);

        Open()["Notes"].ShouldBe("Recheck");
    }

    private sealed class ManualTime : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 12, 9, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}
