using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Products;
using VetDesk.Domain.Services;
using SimpleResult;

namespace VetDesk.Application.Products.Queries;

public enum SearchProductsError { NotSignedIn }

/// <summary>
/// The price list and formulary search. Given a patient, each result carries the allergies it conflicts with so
/// the screen can flag it before anything is added.
/// </summary>
public class SearchProductsQuery : IRequest<Result<IReadOnlyList<ProductDto>>>
{
    public string? Text { get; init; }
    public IReadOnlyCollection<ProductCategory>? Categories { get; init; }
    public int? PatientId { get; init; }
    public bool IncludeInactive { get; init; }
    public int Take { get; init; } = 50;
}

public class SearchProductsQueryHandler : IRequestHandler<SearchProductsQuery, Result<IReadOnlyList<ProductDto>>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public SearchProductsQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result<IReadOnlyList<ProductDto>>> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {
        if (!_user.IsSignedIn())
            return Result.Failure(SearchProductsError.NotSignedIn, Access.NotSignedIn);

        var query = _dbContext.Products.AsNoTracking().Include(p => p.Batches).AsQueryable();
        if (!request.IncludeInactive)
            query = query.Where(p => p.IsActive);
        if (request.Categories is { Count: > 0 } categories)
            query = query.Where(p => categories.Contains(p.Category));
        if (Text.Clean(request.Text) is { } text)
        {
            var like = Search.Anywhere(text);
            query = query.Where(p => EF.Functions.Like(p.Name, like, Search.EscapeText) || EF.Functions.Like(p.Code, like, Search.EscapeText) ||
                                     (p.Ingredients != null && EF.Functions.Like(p.Ingredients, like, Search.EscapeText)));
        }

        var products = await query.OrderBy(p => p.Category).ThenBy(p => p.Name).Take(request.Take).ToListAsync(cancellationToken);

        var allergies = request.PatientId is { } patientId
            ? await _dbContext.PatientAllergies.AsNoTracking().Where(a => a.Patient.Id == patientId).ToListAsync(cancellationToken)
            : [];

        IReadOnlyList<ProductDto> dtos = products
            .Select(p => ProductDto.From(p, AllergyCheck.Conflicts(p, allergies).Select(a => a.Substance).ToList()))
            .ToList();
        return Result.Success(dtos);
    }
}
