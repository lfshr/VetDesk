using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Consultations;
using VetDesk.Domain.Entities.Products;
using VetDesk.Domain.Entities.Staff;
using VetDesk.Domain.Services;
using SimpleResult;

namespace VetDesk.Application.Consultations.Commands;

public enum AddChargeError { NotAllowed, NotFound, ProductNotFound, Locked, InvalidRequest, AllergyConflict }

/// <summary>
/// Adds a catalogue item, a prescription or a free-text item to a consultation. A product that conflicts with a
/// recorded allergy (or free text that names one) is refused unless a vet (someone who may prescribe) gives a reason to
/// go ahead; the reason is kept on the line. A different price can be charged for this visit (see <see cref="ChargePrice"/>).
/// </summary>
public class AddChargeCommand : IRequest<Result<int>>
{
    public int ConsultationId { get; init; }

    /// <summary>The price-list item; null for a free-text item, which then needs a description, price and VAT rate.</summary>
    public int? ProductId { get; init; }

    public decimal Quantity { get; init; } = 1;
    public bool IsPrescription { get; init; }
    public string? Directions { get; init; }
    public string? AllergyOverrideReason { get; init; }

    /// <summary>Free-text items only: what the invoice line says.</summary>
    public string? Description { get; init; }

    /// <summary>Net unit price. Leave null to charge the list price.</summary>
    public decimal? UnitPrice { get; init; }

    /// <summary>Why the price is below the list price (required then).</summary>
    public string? PriceChangeReason { get; init; }

    /// <summary>Free-text items only.</summary>
    public VatRate? VatRate { get; init; }
}

public class AddChargeCommandHandler : IRequestHandler<AddChargeCommand, Result<int>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public AddChargeCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result<int>> Handle(AddChargeCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.WriteClinicalNotes))
            return Result.Failure(AddChargeError.NotAllowed, Access.Denied(Permission.WriteClinicalNotes));
        if (request.IsPrescription && _user.Lacks(Permission.Prescribe))
            return Result.Failure(AddChargeError.NotAllowed, Access.Denied(Permission.Prescribe));
        if (request.Quantity <= 0)
            return Result.Failure(AddChargeError.InvalidRequest, "Quantity must be more than zero.");

        var description = Text.Clean(request.Description);
        if (request.ProductId is null)
        {
            // A free-text medicine couldn't be allergy-checked by class, stock-tracked, labelled or witnessed.
            if (request.IsPrescription)
                return Result.Failure(AddChargeError.InvalidRequest, "Only medicines from the formulary can be prescribed.");
            if (description is null)
                return Result.Failure(AddChargeError.InvalidRequest, "Describe the item for the invoice.");
            if (request.UnitPrice is null || request.VatRate is null)
                return Result.Failure(AddChargeError.InvalidRequest, "Give the item a price and VAT rate.");
        }

        var consultation = await _dbContext.Consultations
            .Include(c => c.Patient).ThenInclude(p => p.Allergies)
            .FirstOrDefaultAsync(c => c.Id == request.ConsultationId, cancellationToken);
        if (consultation is null)
            return Result.Failure(AddChargeError.NotFound, "That consultation no longer exists.");
        if (consultation.IsLocked)
            return Result.Failure(AddChargeError.Locked, "This consultation is completed and locked.");

        ConsultationCharge charge;
        if (request.ProductId is { } productId)
        {
            var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId && p.IsActive, cancellationToken);
            if (product is null)
                return Result.Failure(AddChargeError.ProductNotFound, "That product isn't on the price list.");
            if (request.IsPrescription && product.Category != ProductCategory.Medicine)
                return Result.Failure(AddChargeError.InvalidRequest, "Only medicines can be prescribed.");
            charge = new ConsultationCharge
            {
                Consultation = consultation, Product = product, Description = product.Name, UnitPrice = product.UnitPrice, VatRate = product.VatRate,
            };
        }
        else
        {
            charge = new ConsultationCharge { Consultation = consultation, Description = description!, VatRate = request.VatRate!.Value };
        }

        charge.Quantity = request.Quantity;
        charge.IsPrescription = request.IsPrescription;
        charge.Directions = Text.Clean(request.Directions);
        if (request.UnitPrice is { } price && ChargePrice.Set(charge, price, request.PriceChangeReason, _user.Name) is { } priceError)
            return Result.Failure(AddChargeError.InvalidRequest, priceError);

        var conflicts = AllergyCheck.Conflicts(charge, consultation.Patient.Allergies);
        var overrideReason = Text.Clean(request.AllergyOverrideReason);
        if (conflicts.Count > 0)
        {
            if (overrideReason is null)
                return Result.Failure(AddChargeError.AllergyConflict, $"{AllergyCheck.Describe(charge.Description, conflicts)}. A vet must give a reason to go ahead.");
            if (_user.Lacks(Permission.Prescribe))
                return Result.Failure(AddChargeError.NotAllowed, "Only a vet can override an allergy warning.");
            charge.AllergyOverrideReason = $"{overrideReason} ({_user.Name})";
        }

        consultation.Charges.Add(charge);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(charge.Id);
    }
}
