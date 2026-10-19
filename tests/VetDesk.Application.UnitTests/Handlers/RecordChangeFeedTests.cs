using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Live;
using VetDesk.Application.Consultations.Commands;
using VetDesk.Application.Patients.Commands;
using VetDesk.Domain.Entities.Patients;
using VetDesk.Domain.Entities.Staff;
using VetDesk.Infrastructure.Persistence;

namespace VetDesk.Application.UnitTests.Handlers;

/// <summary>Every save tells the open screens what changed, so other desks see it live.</summary>
[TestFixture]
public class RecordChangeFeedTests
{
    private TestDatabase _database = null!;
    private RecordChangeFeed _feed = null!;
    private TestDesk _desk = null!;
    private VetDeskDbContext _db = null!;
    private readonly List<RecordChange> _heard = [];
    private Patient _biscuit = null!;

    [SetUp]
    public async Task SetUp()
    {
        _heard.Clear();
        _database = await TestDatabase.CreateAsync();
        var vet = await _database.AddStaffAsync("Heather", "Moss", StaffRole.Vet);
        _biscuit = await _database.AddPatientAsync("Biscuit");
        await _database.SignInAsync(vet);
        _feed = new RecordChangeFeed(NullLogger<RecordChangeFeed>.Instance);
        _feed.Changed += _heard.Add;
        _desk = new TestDesk();
        _db = _database.NewContext(_feed, _desk);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
        _database.Dispose();
    }

    [Test]
    public async Task ANewNote_IsHeardByScreensShowingItsPatient_WithWhoAndWhichDesk()
    {
        var result = await new AddClinicalNoteCommandHandler(_db, _database.User, _database.Clock)
            .Handle(new AddClinicalNoteCommand { PatientId = _biscuit.Id, Title = "Phone call", Body = "Owner says eating well" }, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue(result.ToString());

        var change = _heard.ShouldHaveSingleItem();
        change.Topics.ShouldContain("ClinicalNote");
        change.Topics.ShouldContain(RecordChange.Topic("Patient", _biscuit.Id));
        change.ByName.ShouldBe("Heather");
        change.DeskId.ShouldBe(_desk.DeskId);
    }

    [Test]
    public async Task ChangedNotes_AreHeardByTheConsultationAndItsPatient()
    {
        var started = await new StartConsultationCommandHandler(_db, _database.User, _database.Clock)
            .Handle(new StartConsultationCommand { PatientId = _biscuit.Id }, CancellationToken.None);
        var id = started.Value;
        var version = (await _db.Consultations.AsNoTracking().FirstAsync(c => c.Id == id)).Version;
        _heard.Clear();

        var saved = await new SaveConsultationNotesCommandHandler(_db, _database.User)
            .Handle(new SaveConsultationNotesCommand { Id = id, OwnerAdvice = "Recheck in a week", Version = version }, CancellationToken.None);

        saved.IsSuccess.ShouldBeTrue(saved.ToString());
        var change = _heard.ShouldHaveSingleItem();
        change.Topics.ShouldContain(RecordChange.Topic("Consultation", id));
        change.Topics.ShouldContain(RecordChange.Topic("Patient", _biscuit.Id));
    }

    [Test]
    public async Task ARefusedSave_IsNotHeard()
    {
        // Another desk saved the patient first, so this desk's copy is out of date and its save is refused.
        var stale = await _db.Patients.FirstAsync(p => p.Id == _biscuit.Id);
        await using (var otherDesk = _database.NewContext())
        {
            var fresh = await otherDesk.Patients.FirstAsync(p => p.Id == _biscuit.Id);
            fresh.Breed = "Labrador";
            await otherDesk.SaveChangesAsync();
        }

        stale.Breed = "Spaniel";
        (await _db.TrySaveChangesAsync(CancellationToken.None)).ShouldBeFalse();

        _heard.ShouldBeEmpty();
    }

    [Test]
    public async Task ARemovedAlert_IsStillHeardByItsPatient()
    {
        var alert = new PatientAlert { Patient = await _db.Patients.FirstAsync(p => p.Id == _biscuit.Id), Text = "Bites", Severity = AlertSeverity.Alert };
        _db.PatientAlerts.Add(alert);
        await _db.SaveChangesAsync();
        _heard.Clear();

        _db.PatientAlerts.Remove(alert);
        await _db.SaveChangesAsync();

        _heard.ShouldHaveSingleItem().Topics.ShouldContain(RecordChange.Topic("Patient", _biscuit.Id));
    }
}
