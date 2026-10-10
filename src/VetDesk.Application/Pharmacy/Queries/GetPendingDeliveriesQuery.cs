using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Purchasing;
using SimpleResult;

namespace VetDesk.Application.Pharmacy.Queries;

public enum GetPendingDeliveriesError { NotSignedIn }

/// <summary>Orders that have been sent and not yet received: the "Receive delivery" count.</summary>
public class GetPendingDeliveriesQuery : IRequest<Result<IReadOnlyList<DeliveryDto>>>
{
}

public class GetPendingDeliveriesQueryHandler : IRequestHandler<GetPendingDeliveriesQuery, Result<IReadOnlyList<DeliveryDto>>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public GetPendingDeliveriesQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result<IReadOnlyList<DeliveryDto>>> Handle(GetPendingDeliveriesQuery request, CancellationToken cancellationToken)
    {
        if (!_user.IsSignedIn())
            return Result.Failure(GetPendingDeliveriesError.NotSignedIn, Access.NotSignedIn);

        var orders = await _dbContext.PurchaseOrders.AsNoTracking()
            .Include(o => o.Lines).ThenInclude(l => l.Product)
            .Where(o => o.Status == PurchaseOrderStatus.Sent)
            .OrderBy(o => o.SentUtc)
            .ToListAsync(cancellationToken);

        IReadOnlyList<DeliveryDto> deliveries = orders.Select(o => new DeliveryDto(o.Id, o.Number, o.Supplier, o.DeliverBy, Utc.Mark(o.SentUtc),
            o.Lines.Select(l => new DeliveryLineDto(l.Product.Id, l.Product.Name, l.Product.Unit, l.Product.IsControlledDrug, l.Quantity)).ToList())).ToList();
        return Result.Success(deliveries);
    }
}
