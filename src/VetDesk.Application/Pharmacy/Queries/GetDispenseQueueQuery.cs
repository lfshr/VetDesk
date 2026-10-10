using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Dispensing;
using SimpleResult;

namespace VetDesk.Application.Pharmacy.Queries;

public enum GetDispenseQueueError { NotSignedIn }

public class GetDispenseQueueQuery : IRequest<Result<IReadOnlyList<DispenseRow>>>
{
}

public class GetDispenseQueueQueryHandler : IRequestHandler<GetDispenseQueueQuery, Result<IReadOnlyList<DispenseRow>>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public GetDispenseQueueQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result<IReadOnlyList<DispenseRow>>> Handle(GetDispenseQueueQuery request, CancellationToken cancellationToken)
    {
        if (!_user.IsSignedIn())
            return Result.Failure(GetDispenseQueueError.NotSignedIn, Access.NotSignedIn);

        var items = await _dbContext.DispenseItems.AsNoTracking()
            .Include(d => d.Patient).ThenInclude(p => p.Client)
            .Include(d => d.Product).ThenInclude(p => p.Batches)
            .Where(d => d.Status == DispenseStatus.Pending)
            .OrderBy(d => d.CreatedUtc)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        IReadOnlyList<DispenseRow> rows = items.Select(d => new DispenseRow(
            d.Id, d.Patient.Id, d.Patient.Name, d.Patient.Client.ShortName, d.Product.Id, d.Product.Name, d.Quantity, d.Product.Unit,
            d.Directions, d.PrescribedBy, Utc.Mark(d.CreatedUtc), d.Product.IsControlledDrug, d.AwaitingAuthorisation, d.Product.OnHand,
            d.Version)).ToList();
        return Result.Success(rows);
    }
}
