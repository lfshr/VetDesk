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
        _drafts.Join("Consultation:40", new Dictionary<string, string?> { ["Plan"] = plan, ["Assessment"] = null }, Guid.NewGuid());

    [Test]
    public void SecondDesk_JoinsTheSameDraft_AndSeesUnsavedTyping()
    {
        var first = Open();
        first.Type("Plan", "Recheck in a w", Heather, "Heather");

        var second = Open("something stale");

        second.ShouldBeSameAs(first);
        second["Plan"].ShouldBe("Recheck in a w");
    }

    [Test]
    public void Typing_IsSentToTheOtherDesks_AsItHappens()
    {
        var draft = Open();
        var heard = new List<LiveDraftEvent>();
        draft.Changed += heard.Add;

        draft.Type("Plan", "R", Heather, "Heather");

        heard.ShouldHaveSingleItem().ShouldBe(new LiveDraftEvent(LiveDraftEventKind.Typed, Heather, "Heather", "Plan"));
    }

    [Test]
    public void ABoxSomeoneIsTypingIn_IsReadOnlyElsewhere_UntilTheyMoveOn()
    {
        var draft = Open();
        draft.Type("Plan", "Heather's plan", Heather, "Heather");

        draft.TypingBy("Plan", Douglas).ShouldBe("Heather");
        draft.TypingBy("Plan", Heather).ShouldBeNull();
        draft.Type("Plan", "Douglas's plan", Douglas, "Douglas").ShouldBeFalse();
        draft["Plan"].ShouldBe("Heather's plan");

        // Other boxes stay free.
        draft.Type("Assessment", "Stable", Douglas, "Douglas").ShouldBeTrue();

        draft.Release("Plan", Heather);
        draft.Type("Plan", "Douglas's plan", Douglas, "Douglas").ShouldBeTrue();
    }

    [Test]
    public void ABox_IsFreedAfterAPauseInTyping()
    {
        var draft = Open();
        draft.Type("Plan", "Heather's plan", Heather, "Heather");

        _time.Advance(LiveDrafts.HoldFor + TimeSpan.FromSeconds(1));

        draft.TypingBy("Plan", Douglas).ShouldBeNull();
        draft.Type("Plan", "Douglas's plan", Douglas, "Douglas").ShouldBeTrue();
    }

    [Test]
    public void Saving_ClearsWhatWasSaved_ButNotWhatWasTypedWhileItSaved()
    {
        var draft = Open();
        draft.Type("Plan", "Recheck in a week", Heather, "Heather");
        var sent = draft.Values;
        draft.Type("Assessment", "Stable", Douglas, "Douglas"); // typed while the save was on its way
        var version = Guid.NewGuid();

        draft.MarkSaved(sent, version, Heather, "Heather");

        draft.Version.ShouldBe(version);
        draft.Unsaved.ShouldBe(["Assessment"]);
    }

    [Test]
    public void ClearingABox_ThatWasNeverFilledIn_IsNotAChange()
    {
        var draft = Open();
        draft.Type("Assessment", "", Heather, "Heather");

        draft.IsDirty.ShouldBeFalse();
    }

    [Test]
    public void ASaveMadeElsewhere_UpdatesUntouchedBoxes_AndKeepsUnsavedTyping()
    {
        var draft = Open();
        draft.Type("Assessment", "Stable", Heather, "Heather");
        var version = Guid.NewGuid();

        draft.Reset(new Dictionary<string, string?> { ["Plan"] = "Saved elsewhere", ["Assessment"] = "Saved elsewhere" }, version);

        draft["Plan"].ShouldBe("Saved elsewhere");
        draft["Assessment"].ShouldBe("Stable");
        draft.Version.ShouldBe(version);
    }

    [Test]
    public void WhileSomeoneCompletesTheRecord_NobodyElseCanType()
    {
        var draft = Open();
        draft.Freeze(Heather, "Heather").ShouldBeTrue();

        draft.Freeze(Douglas, "Douglas").ShouldBeFalse();
        draft.Type("Assessment", "Stable", Douglas, "Douglas").ShouldBeFalse();
        draft.FrozenBy(Douglas).ShouldBe("Heather");
        draft.TypingBy("Assessment", Douglas).ShouldBe("Heather");

        draft.Unfreeze(Heather);
        draft.Type("Assessment", "Stable", Douglas, "Douglas").ShouldBeTrue();
    }

    [Test]
    public void Discard_PutsBackTheSavedValues_ExceptWhereSomeoneElseIsTyping()
    {
        var draft = Open();
        draft.Type("Plan", "Heather's plan", Heather, "Heather");
        draft.Type("Assessment", "Douglas's typo", Douglas, "Douglas");

        draft.Revert(Douglas, "Douglas");

        draft["Assessment"].ShouldBeNull();
        draft["Plan"].ShouldBe("Heather's plan");
    }

    [Test]
    public void TheDraftGoes_WhenTheLastDeskLeaves()
    {
        var first = Open();
        var second = Open();
        first.Type("Plan", "Unsaved", Heather, "Heather");

        _drafts.Leave(first);
        Open().ShouldBeSameAs(first);
        _drafts.Leave(first);
        _drafts.Leave(second);

        Open()["Plan"].ShouldBe("Recheck");
    }

    private sealed class ManualTime : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 12, 9, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}
