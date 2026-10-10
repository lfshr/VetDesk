using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Invoices.Commands;
using VetDesk.Application.Invoices.Queries;
using VetDesk.Domain.Entities.Invoices;
using VetDesk.Domain.Entities.Patients;
using VetDesk.Domain.Entities.Products;
using VetDesk.Domain.Entities.Staff;

namespace VetDesk.Application.UnitTests.Handlers;

[TestFixture]
public class BillingHandlerTests
{
    private TestDatabase _database = null!;
    private Patient _patient = null!;
    private Product _consult = null!;
    private Product _food = null!;

    [SetUp]
    public async Task SetUp()
    {
        _database = await TestDatabase.CreateAsync();
        await _database.SignInAsync(await _database.AddStaffAsync("Jack", "Bennett", StaffRole.Reception));
        _patient = await _database.AddPatientAsync();
        _consult = await _database.AddProductAsync("Consultation", ProductCategory.Service, 52m);
        _food = await _database.AddProductAsync("Renal diet", ProductCategory.Retail, 24m, stock: 5, vat: VatRate.Zero);
    }

    [TearDown]
    public void TearDown() => _database.Dispose();

    private async Task<(int Id, string Number)> InvoiceAsync(params NewInvoiceLine[] lines)
    {
        var clientId = (await _database.Db.Patients.Include(p => p.Client).FirstAsync(p => p.Id == _patient.Id)).Client.Id;
        var result = await new CreateInvoiceCommandHandler(_database.Db, _database.User, _database.Clock)
            .Handle(new CreateInvoiceCommand { ClientId = clientId, PatientId = _patient.Id, Lines = lines }, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue(result.ToString());
        return result.Value;
    }

    private Task<SimpleResult.Result<decimal>> PayAsync(int invoiceId, decimal amount, PaymentMethod method = PaymentMethod.Cash) =>
        new TakePaymentCommandHandler(_database.Db, _database.User, _database.Clock)
            .Handle(new TakePaymentCommand { InvoiceId = invoiceId, Amount = amount, Method = method, Reference = "ref" }, CancellationToken.None);

    [Test]
    public async Task Invoice_MixedVatRates_TotalsCorrectly_AndNumbersRunInSequence()
    {
        var first = await InvoiceAsync(new NewInvoiceLine(_consult.Id, 1), new NewInvoiceLine(_food.Id, 1));
        var second = await InvoiceAsync(new NewInvoiceLine(_consult.Id, 1));

        var invoice = await _database.Db.Invoices.FirstAsync(i => i.Id == first.Id);
        invoice.Net.ShouldBe(76m);
        invoice.Vat.ShouldBe(10.40m);
        invoice.Total.ShouldBe(86.40m);
        first.Number.ShouldBe("INV-00001");
        second.Number.ShouldBe("INV-00002");
    }

    [Test]
    public async Task Payment_MoreThanDue_IsRefused()
    {
        var invoice = await InvoiceAsync(new NewInvoiceLine(_consult.Id, 1));

        var result = await PayAsync(invoice.Id, 70m);

        result.ErrorCode.ShouldBe(TakePaymentError.Overpayment);
    }

    [Test]
    public async Task PartPayment_LeavesBalanceDue()
    {
        var invoice = await InvoiceAsync(new NewInvoiceLine(_consult.Id, 1));

        var result = await PayAsync(invoice.Id, 20m);

        result.Value.ShouldBe(42.40m);
    }

    [Test]
    public async Task Refund_MoreThanPaid_IsRefused()
    {
        var invoice = await InvoiceAsync(new NewInvoiceLine(_consult.Id, 1));
        await PayAsync(invoice.Id, 20m);

        var result = await new RefundPaymentCommandHandler(_database.Db, _database.User, _database.Clock)
            .Handle(new RefundPaymentCommand { InvoiceId = invoice.Id, Amount = 25m, Reason = "Goodwill" }, CancellationToken.None);

        result.ErrorCode.ShouldBe(RefundPaymentError.MoreThanPaid);
    }

    [Test]
    public async Task CashUp_TotalsByMethod_NetsRefunds_AndExpectsFloatPlusCash()
    {
        var a = await InvoiceAsync(new NewInvoiceLine(_consult.Id, 1));
        var b = await InvoiceAsync(new NewInvoiceLine(_consult.Id, 1));
        await PayAsync(a.Id, 62.40m, PaymentMethod.Cash);
        await PayAsync(b.Id, 62.40m, PaymentMethod.Card);
        await new RefundPaymentCommandHandler(_database.Db, _database.User, _database.Clock)
            .Handle(new RefundPaymentCommand { InvoiceId = a.Id, Amount = 10m, Method = PaymentMethod.Cash, Reason = "Goodwill" }, CancellationToken.None);

        var cashUp = await new GetCashUpQueryHandler(_database.Db, _database.User, _database.Clock).Handle(new GetCashUpQuery(), CancellationToken.None);

        cashUp.Value.Cash.ShouldBe(52.40m);
        cashUp.Value.Card.ShouldBe(62.40m);
        cashUp.Value.ExpectedCash.ShouldBe(150m + 52.40m);
    }

    [Test]
    public async Task CloseDay_Short_NeedsNote_ThenStampsPaymentsAndCannotRunTwice()
    {
        var invoice = await InvoiceAsync(new NewInvoiceLine(_consult.Id, 1));
        await PayAsync(invoice.Id, 62.40m, PaymentMethod.Cash);
        var handler = new CloseDayCommandHandler(_database.Db, _database.User, _database.Clock);
        var today = new DateOnly(2026, 10, 9);

        var noNote = await handler.Handle(new CloseDayCommand { Date = today, CashCounted = 210m }, CancellationToken.None);
        var withNote = await handler.Handle(new CloseDayCommand { Date = today, CashCounted = 210m, Note = "£2.40 change error" }, CancellationToken.None);
        var again = await handler.Handle(new CloseDayCommand { Date = today, CashCounted = 212.40m }, CancellationToken.None);

        noNote.ErrorCode.ShouldBe(CloseDayError.NoteRequired);
        withNote.Value.ShouldBe(-2.40m);
        again.ErrorCode.ShouldBe(CloseDayError.AlreadyClosed);
        (await _database.Db.Payments.AllAsync(p => p.DayCloseId != null)).ShouldBeTrue();
    }
}
