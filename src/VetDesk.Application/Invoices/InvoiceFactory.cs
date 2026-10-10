using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Clients;
using VetDesk.Domain.Entities.Invoices;
using VetDesk.Domain.Entities.Patients;
using VetDesk.Domain.Entities.Practice;
using VetDesk.Domain.Services;

namespace VetDesk.Application.Invoices;

internal static class InvoiceFactory
{
    /// <summary>
    /// Builds an invoice with the next number in the sequence and its totals worked out. Nothing is saved here;
    /// the number sequence row has a version token, so two desks raising invoices at once can't take the same number.
    /// </summary>
    public static async Task<Invoice> CreateAsync(
        IVetDeskDbContext db, Client client, Patient? patient, IEnumerable<InvoiceLine> lines, DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var settings = await db.ClinicSettings.FirstAsync(cancellationToken);
        var sequence = await NextSequenceAsync(db, NumberSequence.Invoice, cancellationToken);

        var invoice = new Invoice
        {
            Number = Invoice.FormatNumber(sequence.Take()),
            Client = client,
            Patient = patient,
            IssuedUtc = nowUtc,
            DueDate = ClinicTime.Today(nowUtc).AddDays(settings.PaymentTermsDays),
        };
        invoice.Lines.AddRange(lines);
        invoice.SetTotals(VatCalculator.For(invoice.Lines));
        db.Invoices.Add(invoice);
        return invoice;
    }

    public static async Task<NumberSequence> NextSequenceAsync(IVetDeskDbContext db, string name, CancellationToken cancellationToken)
    {
        var sequence = await db.NumberSequences.FirstOrDefaultAsync(s => s.Name == name, cancellationToken);
        if (sequence is null)
        {
            sequence = new NumberSequence { Name = name };
            db.NumberSequences.Add(sequence);
        }

        return sequence;
    }
}
