using VetDesk.Domain.Entities.Clients;
using VetDesk.Domain.Entities.Invoices;

namespace VetDesk.Domain.UnitTests.Entities;

[TestFixture]
public class InvoiceTests
{
    private static readonly DateOnly Today = new(2026, 10, 9);

    private static Invoice NewInvoice(decimal total, DateOnly due)
    {
        var invoice = new Invoice { Number = "INV-00001", Client = new Client { LastName = "Clarke", Phone = "07700900412" }, DueDate = due };
        invoice.SetTotals(new VatBreakdown(total / 1.2m, total - total / 1.2m));
        return invoice;
    }

    private static void Pay(Invoice invoice, decimal amount) =>
        invoice.Payments.Add(new Payment { Invoice = invoice, Amount = amount, TakenBy = "Jack Bennett" });

    [Test]
    public void NothingPaid_BeforeDueDate_IsUnpaid()
    {
        NewInvoice(120m, Today.AddDays(1)).StatusOn(Today).ShouldBe(InvoiceStatus.Unpaid);
    }

    [Test]
    public void NothingPaid_AfterDueDate_IsOverdue()
    {
        NewInvoice(120m, Today.AddDays(-1)).StatusOn(Today).ShouldBe(InvoiceStatus.Overdue);
    }

    [Test]
    public void SomePaid_IsPartPaid()
    {
        var invoice = NewInvoice(120m, Today);
        Pay(invoice, 20m);

        invoice.StatusOn(Today).ShouldBe(InvoiceStatus.PartPaid);
        invoice.AmountDue.ShouldBe(100m);
    }

    [Test]
    public void FullyPaid_IsPaid()
    {
        var invoice = NewInvoice(120m, Today);
        Pay(invoice, 120m);

        invoice.StatusOn(Today).ShouldBe(InvoiceStatus.Paid);
        invoice.AmountDue.ShouldBe(0m);
    }

    [Test]
    public void PaidThenFullyRefunded_IsRefunded()
    {
        var invoice = NewInvoice(120m, Today);
        Pay(invoice, 120m);
        Pay(invoice, -120m);

        invoice.StatusOn(Today).ShouldBe(InvoiceStatus.Refunded);
        invoice.Refunded.ShouldBe(120m);
    }

    [Test]
    public void ClaimSent_AndUnpaid_IsClaimSent()
    {
        var invoice = NewInvoice(120m, Today.AddDays(-30));
        invoice.Claim = new InsuranceClaim { Insurer = "Petplan", PolicyNumber = "P1", Condition = "Laceration" };

        invoice.StatusOn(Today).ShouldBe(InvoiceStatus.ClaimSent);
    }
}
