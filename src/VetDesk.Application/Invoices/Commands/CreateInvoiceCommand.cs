using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Invoices;
using VetDesk.Domain.Entities.Staff;
using VetDesk.Domain.Services;
using SimpleResult;

namespace VetDesk.Application.Invoices.Commands;

public enum CreateInvoiceError { NotAllowed, InvalidRequest, ClientNotFound, PatientNotFound, ProductNotFound, AllergyConflict, ChangedElsewhere }

public record NewInvoiceLine(int ProductId, decimal Quantity);

/// <summary>
/// A counter sale or an invoice raised outside a consultation (the Billing screen's "New invoice"). Items are checked
/// against the patient's allergies here too, so a medicine can't slip through by being sold rather than prescribed.
/// </summary>
public class CreateInvoiceCommand : IRequest<Result<(int Id, string Number)>>
{
    public int ClientId { get; init; }
    public int? PatientId { get; init; }
    public IReadOnlyList<NewInvoiceLine> Lines { get; init; } = [];
    public string? AllergyOverrideReason { get; init; }
}

public class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, Result<(int Id, string Number)>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public CreateInvoiceCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result<(int Id, string Number)>> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.TakePayments))
            return Result.Failure(CreateInvoiceError.NotAllowed, Access.Denied(Permission.TakePayments));
        if (request.Lines.Count == 0)
            return Result.Failure(CreateInvoiceError.InvalidRequest, "Add at least one item.");
        if (request.Lines.Any(l => l.Quantity <= 0))
            return Result.Failure(CreateInvoiceError.InvalidRequest, "Quantities must be more than zero.");

        var client = await _dbContext.Clients.FirstOrDefaultAsync(c => c.Id == request.ClientId, cancellationToken);
        if (client is null)
            return Result.Failure(CreateInvoiceError.ClientNotFound, "Choose a client.");

        var patient = request.PatientId is { } patientId
            ? await _dbContext.Patients.Include(p => p.Allergies).Include(p => p.Client)
                .FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken)
            : null;
        if (request.PatientId is not null && (patient is null || patient.Client.Id != client.Id))
            return Result.Failure(CreateInvoiceError.PatientNotFound, "Choose one of this client's pets.");

        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _dbContext.Products.Where(p => productIds.Contains(p.Id) && p.IsActive).ToDictionaryAsync(p => p.Id, cancellationToken);
        if (products.Count != productIds.Count)
            return Result.Failure(CreateInvoiceError.ProductNotFound, "One of the items is no longer on the price list.");

        if (patient is not null)
        {
            var conflict = products.Values.Select(p => (Product: p, Conflicts: AllergyCheck.Conflicts(p, patient.Allergies)))
                .FirstOrDefault(x => x.Conflicts.Count > 0);
            if (conflict.Product is not null)
            {
                if (!Text.Has(request.AllergyOverrideReason))
                    return Result.Failure(CreateInvoiceError.AllergyConflict, $"{AllergyCheck.Describe(conflict.Product, conflict.Conflicts)}. A vet must give a reason to go ahead.");
                if (_user.Lacks(Permission.Prescribe))
                    return Result.Failure(CreateInvoiceError.NotAllowed, "Only a vet can override an allergy warning.");
            }
        }

        var invoice = await InvoiceFactory.CreateAsync(_dbContext, client, patient,
            request.Lines.Select(l => new InvoiceLine
            {
                Product = products[l.ProductId],
                Description = products[l.ProductId].Name,
                Quantity = l.Quantity,
                UnitPrice = products[l.ProductId].UnitPrice,
                VatRate = products[l.ProductId].VatRate,
            }), _clock.UtcNow, cancellationToken);

        if (!await _dbContext.TrySaveChangesAsync(cancellationToken))
            return Result.Failure(CreateInvoiceError.ChangedElsewhere, "Another desk raised an invoice at the same moment. Try again.");
        return Result.Success((invoice.Id, invoice.Number));
    }
}
