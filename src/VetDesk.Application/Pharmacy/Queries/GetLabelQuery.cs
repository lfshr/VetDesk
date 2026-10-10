using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using SimpleResult;

namespace VetDesk.Application.Pharmacy.Queries;

public enum GetLabelError { NotSignedIn, NotFound }

public class GetLabelQuery : IRequest<Result<LabelDto>>
{
    public int DispenseItemId { get; init; }
}

public class GetLabelQueryHandler : IRequestHandler<GetLabelQuery, Result<LabelDto>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public GetLabelQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result<LabelDto>> Handle(GetLabelQuery request, CancellationToken cancellationToken)
    {
        if (!_user.IsSignedIn())
            return Result.Failure(GetLabelError.NotSignedIn, Access.NotSignedIn);

        var item = await _dbContext.DispenseItems.AsNoTracking()
            .Include(d => d.Patient).ThenInclude(p => p.Client)
            .Include(d => d.Product)
            .FirstOrDefaultAsync(d => d.Id == request.DispenseItemId, cancellationToken);
        if (item is null)
            return Result.Failure(GetLabelError.NotFound, "That item is no longer in the queue.");

        var clinic = await _dbContext.ClinicSettings.AsNoTracking().FirstAsync(cancellationToken);
        return Result.Success(new LabelDto(clinic.Name, clinic.Address, clinic.Phone, item.Patient.Name, item.Patient.Client.FullName,
            item.Product.Name, item.Quantity, item.Product.Unit, item.Directions, item.PrescribedBy,
            Utc.Mark(item.DispensedUtc ?? item.CreatedUtc), item.Product.IsControlledDrug));
    }
}
