using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Consultations.Commands;
using VetDesk.Application.Consultations.Queries;
using VetDesk.Application.Invoices.Commands;
using VetDesk.Domain.Entities.Consultations;
using VetDesk.Domain.Entities.Dispensing;
using VetDesk.Domain.Entities.Patients;
using VetDesk.Domain.Entities.Products;
using VetDesk.Domain.Entities.Staff;

namespace VetDesk.Application.UnitTests.Handlers;

[TestFixture]
public class ConsultationHandlerTests
{
    private TestDatabase _database = null!;
    private StaffMember _vet = null!;
    private StaffMember _reception = null!;
    private Patient _biscuit = null!;
    private Product _consult = null!;
    private Product _amoxicillin = null!;
    private Product _vaccine = null!;

    [SetUp]
    public async Task SetUp()
    {
        _database = await TestDatabase.CreateAsync();
        _vet = await _database.AddStaffAsync("Asha", "Patel", StaffRole.Vet);
        _reception = await _database.AddStaffAsync("Jack", "Bennett", StaffRole.Reception);
        _biscuit = await _database.AddPatientAsync("Biscuit", allergy: "amoxicillin", allergyClass: "Penicillin");
        _consult = await _database.AddProductAsync("Consultation", ProductCategory.Service, 52m);
        _amoxicillin = await _database.AddProductAsync("Amoxicillin 250 mg", ProductCategory.Medicine, 0.95m, stock: 100, drugClass: "Penicillin");
        _vaccine = await _database.AddProductAsync("Lepto 4", ProductCategory.Vaccine, 45m, stock: 10, boosterMonths: 12);
        await _database.SignInAsync(_vet);
    }

    [TearDown]
    public void TearDown() => _database.Dispose();

    private async Task<int> StartAsync()
    {
        var result = await new StartConsultationCommandHandler(_database.Db, _database.User, _database.Clock)
            .Handle(new StartConsultationCommand { PatientId = _biscuit.Id }, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue(result.ToString());
        return result.Value;
    }

    private Task<SimpleResult.Result<int>> AddAsync(int consultationId, Product product, string? overrideReason = null, bool prescription = false) =>
        new AddChargeCommandHandler(_database.Db, _database.User).Handle(new AddChargeCommand
        {
            ConsultationId = consultationId, ProductId = product.Id, Quantity = 1, AllergyOverrideReason = overrideReason, IsPrescription = prescription,
        }, CancellationToken.None);

    private async Task<Guid> WriteNotesAsync(int consultationId)
    {
        var version = (await _database.Db.Consultations.AsNoTracking().FirstAsync(c => c.Id == consultationId)).Version;
        var result = await new SaveConsultationNotesCommandHandler(_database.Db, _database.User).Handle(new SaveConsultationNotesCommand
        {
            Id = consultationId, Subjective = "Well", Objective = "Normal exam", Assessment = "Healthy", Plan = "Booster given", WeightKg = 31.2m, Version = version,
        }, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue(result.ToString());
        return result.Value;
    }

    private Task<SimpleResult.Result<CompletedConsultation>> CompleteAsync(int consultationId, Guid version) =>
        new CompleteConsultationCommandHandler(_database.Db, _database.User, _database.Clock)
            .Handle(new CompleteConsultationCommand { Id = consultationId, Version = version }, CancellationToken.None);

    [Test]
    public async Task AddCharge_ProductInAllergyClass_IsRefusedWithoutReason()
    {
        var id = await StartAsync();

        var result = await AddAsync(id, _amoxicillin);

        result.ErrorCode.ShouldBe(AddChargeError.AllergyConflict);
        _database.Db.ConsultationCharges.Count().ShouldBe(0);
    }

    [Test]
    public async Task AddCharge_AllergyOverriddenByVet_KeepsReasonOnLine()
    {
        var id = await StartAsync();

        var result = await AddAsync(id, _amoxicillin, overrideReason: "Sensitivity culture, no alternative");

        result.IsSuccess.ShouldBeTrue(result.ToString());
        var charge = await _database.Db.ConsultationCharges.SingleAsync();
        charge.AllergyOverrideReason.ShouldBe("Sensitivity culture, no alternative (Dr Patel)");
    }

    [Test]
    public async Task AddCharge_PrescriptionByReceptionist_IsNotAllowed()
    {
        var id = await StartAsync();
        await _database.SignInAsync(_reception);

        var result = await AddAsync(id, _consult, prescription: true);

        result.ErrorCode.ShouldBe(AddChargeError.NotAllowed);
    }

    [Test]
    public async Task Complete_WithoutAllFourSoapSections_IsRefused()
    {
        var id = await StartAsync();
        var version = (await _database.Db.Consultations.AsNoTracking().FirstAsync(c => c.Id == id)).Version;

        var result = await CompleteAsync(id, version);

        result.ErrorCode.ShouldBe(CompleteConsultationError.IncompleteNotes);
    }

    [Test]
    public async Task Complete_RaisesInvoiceWithVat_RecordsVaccineAndWeight_QueuesStock_AndLocks()
    {
        var id = await StartAsync();
        await AddAsync(id, _consult);
        await AddAsync(id, _vaccine);
        var version = await WriteNotesAsync(id);

        var result = await CompleteAsync(id, version);

        result.IsSuccess.ShouldBeTrue(result.ToString());
        result.Value.Total.ShouldBe(116.40m); // (52 + 45) × 1.2
        result.Value.InvoiceNumber.ShouldBe("INV-00001");

        var invoice = await _database.Db.Invoices.Include(i => i.Lines).SingleAsync();
        invoice.Net.ShouldBe(97m);
        invoice.Vat.ShouldBe(19.40m);
        invoice.ConsultationId.ShouldBe(id);
        invoice.DueDate.ShouldBe(new DateOnly(2026, 10, 23));

        var vaccination = await _database.Db.Vaccinations.SingleAsync();
        vaccination.DueDate.ShouldBe(new DateOnly(2027, 10, 9));
        (await _database.Db.WeightRecords.SingleAsync()).Kg.ShouldBe(31.2m);
        (await _database.Db.DispenseItems.SingleAsync()).Status.ShouldBe(DispenseStatus.Pending);

        var consultation = await _database.Db.Consultations.AsNoTracking().SingleAsync();
        consultation.Status.ShouldBe(ConsultationStatus.Completed);
        consultation.InvoiceId.ShouldBe(invoice.Id);
    }

    [Test]
    public async Task CompletedConsultation_IsLocked()
    {
        var id = await StartAsync();
        await AddAsync(id, _consult);
        var version = await WriteNotesAsync(id);
        await CompleteAsync(id, version);
        var lockedVersion = (await _database.Db.Consultations.AsNoTracking().FirstAsync(c => c.Id == id)).Version;

        var save = await new SaveConsultationNotesCommandHandler(_database.Db, _database.User)
            .Handle(new SaveConsultationNotesCommand { Id = id, Subjective = "Changed later", Version = lockedVersion }, CancellationToken.None);
        var add = await AddAsync(id, _consult);
        var charge = await _database.Db.ConsultationCharges.FirstAsync();
        var remove = await new RemoveChargeCommandHandler(_database.Db, _database.User).Handle(new RemoveChargeCommand { ChargeId = charge.Id }, CancellationToken.None);

        save.ErrorCode.ShouldBe(SaveConsultationNotesError.Locked);
        add.ErrorCode.ShouldBe(AddChargeError.Locked);
        remove.ErrorCode.ShouldBe(RemoveChargeError.Locked);
    }

    [Test]
    public async Task Complete_AllergyRecordedAfterItemWasAdded_IsRefused()
    {
        var patient = await _database.AddPatientAsync("Mabel");
        var start = await new StartConsultationCommandHandler(_database.Db, _database.User, _database.Clock)
            .Handle(new StartConsultationCommand { PatientId = patient.Id }, CancellationToken.None);
        (await AddAsync(start.Value, _amoxicillin)).IsSuccess.ShouldBeTrue();
        _database.Db.PatientAllergies.Add(new PatientAllergy { Patient = patient, Substance = "amoxicillin" });
        await _database.Db.SaveChangesAsync();
        var version = await WriteNotesAsync(start.Value);

        var result = await CompleteAsync(start.Value, version);

        result.ErrorCode.ShouldBe(CompleteConsultationError.AllergyConflict);
        _database.Db.Invoices.Count().ShouldBe(0);
    }

    [Test]
    public async Task SaveNotes_StaleVersion_ReportsChangedElsewhere()
    {
        var id = await StartAsync();
        var stale = (await _database.Db.Consultations.AsNoTracking().FirstAsync(c => c.Id == id)).Version;
        await WriteNotesAsync(id);

        var result = await new SaveConsultationNotesCommandHandler(_database.Db, _database.User)
            .Handle(new SaveConsultationNotesCommand { Id = id, Subjective = "Other desk", Version = stale }, CancellationToken.None);

        result.ErrorCode.ShouldBe(SaveConsultationNotesError.ChangedElsewhere);
    }

    [Test]
    public async Task ConsultationTotal_MatchesInvoiceTotal()
    {
        var id = await StartAsync();
        await AddAsync(id, _consult);
        await AddAsync(id, _vaccine);

        var detail = await new GetConsultationQueryHandler(_database.Db, _database.User, _database.Clock)
            .Handle(new GetConsultationQuery { Id = id }, CancellationToken.None);
        var version = await WriteNotesAsync(id);
        var completed = await CompleteAsync(id, version);

        detail.Value.Total.ShouldBe(completed.Value.Total);
    }

    [Test]
    public async Task CounterSale_ToAllergicPatient_IsRefusedWithoutVetOverride()
    {
        await _database.SignInAsync(_reception);

        var result = await new CreateInvoiceCommandHandler(_database.Db, _database.User, _database.Clock).Handle(new CreateInvoiceCommand
        {
            ClientId = (await _database.Db.Patients.Include(p => p.Client).FirstAsync(p => p.Id == _biscuit.Id)).Client.Id,
            PatientId = _biscuit.Id,
            Lines = [new NewInvoiceLine(_amoxicillin.Id, 10)],
            AllergyOverrideReason = "Owner insists",
        }, CancellationToken.None);

        result.ErrorCode.ShouldBe(CreateInvoiceError.NotAllowed);
    }
}
