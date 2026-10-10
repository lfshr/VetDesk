using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Pharmacy.Commands;
using VetDesk.Domain.Entities.Dispensing;
using VetDesk.Domain.Entities.Patients;
using VetDesk.Domain.Entities.Products;
using VetDesk.Domain.Entities.Staff;

namespace VetDesk.Application.UnitTests.Handlers;

[TestFixture]
public class PharmacyHandlerTests
{
    private const string WitnessPassword = "witness-password-1";

    private TestDatabase _database = null!;
    private StaffMember _nurse = null!;
    private StaffMember _witness = null!;
    private StaffMember _reception = null!;
    private Patient _atlas = null!;
    private Product _methadone = null!;

    [SetUp]
    public async Task SetUp()
    {
        _database = await TestDatabase.CreateAsync();
        _nurse = await _database.AddStaffAsync("Mia", "Reid", StaffRole.Nurse);
        _witness = await _database.AddStaffAsync("Asha", "Patel", StaffRole.Vet, WitnessPassword);
        _reception = await _database.AddStaffAsync("Jack", "Bennett", StaffRole.Reception, WitnessPassword);
        _atlas = await _database.AddPatientAsync("Atlas");
        _methadone = await _database.AddProductAsync("Methadone 10 mg/ml", ProductCategory.Medicine, 4.5m, stock: 10, controlled: true);
        await _database.SignInAsync(_nurse);
    }

    [TearDown]
    public void TearDown() => _database.Dispose();

    private async Task<DispenseItem> QueueAsync(Product product, decimal quantity)
    {
        var item = new DispenseItem { Patient = _atlas, Product = product, Quantity = quantity, PrescribedBy = "Dr Patel" };
        _database.Db.DispenseItems.Add(item);
        await _database.Db.SaveChangesAsync();
        return item;
    }

    private Task<SimpleResult.Result> DispenseAsync(int id, int? witnessId, string? password) =>
        new DispenseItemCommandHandler(_database.Db, _database.User, _database.Clock, _database.Hasher)
            .Handle(new DispenseItemCommand { Id = id, WitnessId = witnessId, WitnessPassword = password }, CancellationToken.None);

    [Test]
    public async Task ControlledDrug_WithoutWitness_IsRefused()
    {
        var item = await QueueAsync(_methadone, 0.8m);

        var result = await DispenseAsync(item.Id, null, null);

        result.ErrorCode.ShouldBe(DispenseItemError.WitnessRequired);
    }

    [Test]
    public async Task ControlledDrug_WitnessIsDispenser_IsRefused()
    {
        var item = await QueueAsync(_methadone, 0.8m);

        var result = await DispenseAsync(item.Id, _nurse.Id, "correct-horse-battery");

        result.ErrorCode.ShouldBe(DispenseItemError.WitnessRequired);
    }

    [Test]
    public async Task ControlledDrug_WitnessWithoutPermission_IsRefused()
    {
        var item = await QueueAsync(_methadone, 0.8m);

        var result = await DispenseAsync(item.Id, _reception.Id, WitnessPassword);

        result.ErrorCode.ShouldBe(DispenseItemError.WitnessRequired);
        result.ToString().ShouldContain("can't witness");
    }

    [Test]
    public async Task ControlledDrug_WrongWitnessPassword_IsRefused()
    {
        var item = await QueueAsync(_methadone, 0.8m);

        var result = await DispenseAsync(item.Id, _witness.Id, "guess");

        result.ErrorCode.ShouldBe(DispenseItemError.WitnessRequired);
        (await _database.Db.ControlledDrugEntries.CountAsync()).ShouldBe(0);
    }

    [Test]
    public async Task ControlledDrug_Witnessed_TakesStockAndWritesRegisterWithBalance()
    {
        var item = await QueueAsync(_methadone, 0.8m);

        var result = await DispenseAsync(item.Id, _witness.Id, WitnessPassword);

        result.IsSuccess.ShouldBeTrue(result.ToString());
        var entry = await _database.Db.ControlledDrugEntries.SingleAsync();
        entry.Change.ShouldBe(-0.8m);
        entry.Balance.ShouldBe(9.2m);
        entry.By.ShouldBe("Nurse Reid");
        entry.Witness.ShouldBe("Dr Patel");
        (await _database.Db.StockBatches.SingleAsync()).Quantity.ShouldBe(9.2m);
    }

    [Test]
    public async Task Dispense_MoreThanInStock_IsRefused()
    {
        var item = await QueueAsync(_methadone, 12m);

        var result = await DispenseAsync(item.Id, _witness.Id, WitnessPassword);

        result.ErrorCode.ShouldBe(DispenseItemError.InsufficientStock);
    }

    [Test]
    public async Task Dispense_TakesFromEarliestExpiryFirst()
    {
        var product = await _database.AddProductAsync("Meloxicam", ProductCategory.Medicine, 0.85m);
        product.Batches.Add(new StockBatch { Product = product, BatchNumber = "LATE", Quantity = 10, Expiry = new DateOnly(2028, 1, 1) });
        product.Batches.Add(new StockBatch { Product = product, BatchNumber = "SOON", Quantity = 4, Expiry = new DateOnly(2027, 1, 1) });
        await _database.Db.SaveChangesAsync();
        var item = await QueueAsync(product, 6m);

        (await DispenseAsync(item.Id, null, null)).IsSuccess.ShouldBeTrue();

        var batches = await _database.Db.StockBatches.Where(b => b.Product.Id == product.Id).ToDictionaryAsync(b => b.BatchNumber, b => b.Quantity);
        batches["SOON"].ShouldBe(0m);
        batches["LATE"].ShouldBe(8m);
    }

    [Test]
    public async Task BalanceCheck_Discrepancy_NeedsReason_ThenAdjustsAndKeepsItInTheRegister()
    {
        var handler = new RecordBalanceCheckCommandHandler(_database.Db, _database.User, _database.Clock, _database.Hasher);
        var command = new RecordBalanceCheckCommand { ProductId = _methadone.Id, Counted = 9.5m, WitnessId = _witness.Id, WitnessPassword = WitnessPassword };

        var withoutReason = await handler.Handle(command, CancellationToken.None);
        var withReason = await handler.Handle(new RecordBalanceCheckCommand
        {
            ProductId = _methadone.Id, Counted = 9.5m, WitnessId = _witness.Id, WitnessPassword = WitnessPassword, DiscrepancyReason = "Spillage, see incident log",
        }, CancellationToken.None);

        withoutReason.ErrorCode.ShouldBe(RecordBalanceCheckError.Discrepancy);
        withReason.Value.ShouldBe(-0.5m);
        var entries = await _database.Db.ControlledDrugEntries.OrderBy(e => e.Id).ToListAsync();
        entries.Select(e => e.Kind).ShouldBe([ControlledDrugEntryKind.Adjustment, ControlledDrugEntryKind.BalanceCheck]);
        entries[^1].Balance.ShouldBe(9.5m);
    }

    [Test]
    public async Task StockTake_ControlledDrug_IsRefused()
    {
        var result = await new RecordStockTakeCommandHandler(_database.Db, _database.User, _database.Clock)
            .Handle(new RecordStockTakeCommand { Counts = [new StockCount(_methadone.Id, 8)] }, CancellationToken.None);

        result.ErrorCode.ShouldBe(RecordStockTakeError.ControlledDrug);
    }

    [Test]
    public async Task RepeatRequestedByReception_NeedsVetAuthorisationBeforeDispensing()
    {
        var product = await _database.AddProductAsync("Meloxicam", ProductCategory.Medicine, 0.85m, stock: 100);
        await _database.SignInAsync(_reception);
        var request = await new RequestRepeatCommandHandler(_database.Db, _database.User)
            .Handle(new RequestRepeatCommand { PatientId = _atlas.Id, ProductId = product.Id, Quantity = 30 }, CancellationToken.None);
        await _database.SignInAsync(_nurse);

        var early = await DispenseAsync(request.Value, null, null);
        await _database.SignInAsync(_witness);
        var authorise = await new AuthoriseDispenseCommandHandler(_database.Db, _database.User).Handle(new AuthoriseDispenseCommand { Id = request.Value }, CancellationToken.None);
        await _database.SignInAsync(_nurse);
        var later = await DispenseAsync(request.Value, null, null);

        early.ErrorCode.ShouldBe(DispenseItemError.NeedsAuthorisation);
        authorise.IsSuccess.ShouldBeTrue();
        later.IsSuccess.ShouldBeTrue(later.ToString());
    }
}
