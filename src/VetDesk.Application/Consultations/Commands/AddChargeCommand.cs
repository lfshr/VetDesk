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
/// Adds a catalogue item or a prescription to a consultation. A product that conflicts with a recorded allergy
/// is refused unless a vet (someone who may prescribe) gives a reason to go ahead; the reason is kept on the line.
/// </summary>
public class AddChargeCommand : IRequest<Result<int>>
{
    public int ConsultationId { get; init; }
    public int ProductId { get; init; }
    public decimal Quantity { get; init; } = 1;
    public bool IsPrescription { get; init; }
    public string? Directions { get; init; }
    public string? AllergyOverrideReason { get; init; }
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

        var consultation = await _dbContext.Consultations
            .Include(c => c.Patient).ThenInclude(p => p.Allergies)
            .FirstOrDefaultAsync(c => c.Id == request.ConsultationId, cancellationToken);
        if (consultation is null)
            return Result.Failure(AddChargeError.NotFound, "That consultation no longer exists.");
        if (consultation.IsLocked)
            return Result.Failure(AddChargeError.Locked, "This consultation is completed and locked.");

        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive, cancellationToken);
        if (product is null)
            return Result.Failure(AddChargeError.ProductNotFound, "That product isn't on the price list.");
        if (request.IsPrescription && product.Category != ProductCategory.Medicine)
            return Result.Failure(AddChargeError.InvalidRequest, "Only medicines can be prescribed.");

        var conflicts = AllergyCheck.Conflicts(product, consultation.Patient.Allergies);
        var overrideReason = Text.Clean(request.AllergyOverrideReason);
        if (conflicts.Count > 0)
        {
            if (overrideReason is null)
                return Result.Failure(AddChargeError.AllergyConflict, $"{AllergyCheck.Describe(product, conflicts)}. A vet must give a reason to go ahead.");
            if (_user.Lacks(Permission.Prescribe))
                return Result.Failure(AddChargeError.NotAllowed, "Only a vet can override an allergy warning.");
        }

        var charge = new ConsultationCharge
        {
            Consultation = consultation,
            Product = product,
            Description = product.Name,
            Quantity = request.Quantity,
            UnitPrice = product.UnitPrice,
            VatRate = product.VatRate,
            IsPrescription = request.IsPrescription,
            Directions = Text.Clean(request.Directions),
            AllergyOverrideReason = conflicts.Count > 0 ? $"{overrideReason} ({_user.Name})" : null,
        };
        consultation.Charges.Add(charge);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(charge.Id);
    }
}
