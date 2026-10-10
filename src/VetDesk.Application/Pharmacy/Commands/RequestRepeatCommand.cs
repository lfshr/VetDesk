using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Dispensing;
using VetDesk.Domain.Entities.Products;
using VetDesk.Domain.Services;
using SimpleResult;

namespace VetDesk.Application.Pharmacy.Commands;

public enum RequestRepeatError { NotSignedIn, InvalidRequest, PatientNotFound, ProductNotFound, AllergyConflict }

/// <summary>
/// A repeat prescription request, e.g. taken at reception. Unless a vet raises it, it waits in the queue as
/// "Needs vet authorisation".
/// </summary>
public class RequestRepeatCommand : IRequest<Result<int>>
{
    public int PatientId { get; init; }
    public int ProductId { get; init; }
    public decimal Quantity { get; init; }
    public string? Directions { get; init; }
}

public class RequestRepeatCommandHandler : IRequestHandler<RequestRepeatCommand, Result<int>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public RequestRepeatCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result<int>> Handle(RequestRepeatCommand request, CancellationToken cancellationToken)
    {
        if (!_user.IsSignedIn())
            return Result.Failure(RequestRepeatError.NotSignedIn, Access.NotSignedIn);
        if (request.Quantity <= 0)
            return Result.Failure(RequestRepeatError.InvalidRequest, "Quantity must be more than zero.");

        var patient = await _dbContext.Patients.Include(p => p.Allergies).FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken);
        if (patient is null)
            return Result.Failure(RequestRepeatError.PatientNotFound, "That patient no longer exists.");

        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive, cancellationToken);
        if (product is null || product.Category != ProductCategory.Medicine || !product.IsStocked)
            return Result.Failure(RequestRepeatError.ProductNotFound, "Choose a stocked medicine.");

        var conflicts = AllergyCheck.Conflicts(product, patient.Allergies);
        if (conflicts.Count > 0)
            return Result.Failure(RequestRepeatError.AllergyConflict, $"{AllergyCheck.Describe(product, conflicts)}. Ask a vet to prescribe in a consultation.");

        var canPrescribe = _user.Has(Domain.Entities.Staff.Permission.Prescribe);
        var item = new DispenseItem
        {
            Patient = patient,
            Product = product,
            Quantity = request.Quantity,
            Directions = Text.Clean(request.Directions),
            PrescribedBy = _user.Name,
            RequiresAuthorisation = !canPrescribe,
            AuthorisedBy = canPrescribe ? _user.Name : null,
        };
        _dbContext.DispenseItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(item.Id);
    }
}
