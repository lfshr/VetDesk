using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Clients;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Clients.Queries;

public enum SearchClientsError { NotAllowed }

public enum ClientFilter { All, BalanceDue, NewThisYear }

public enum ClientSort { Surname, LastVisit, Balance }

public class SearchClientsQuery : IRequest<Result<IReadOnlyList<ClientRow>>>
{
    public string? Text { get; init; }
    public ClientFilter Filter { get; init; }
    public ClientSort Sort { get; init; }
    public int Take { get; init; } = 200;
}

public class SearchClientsQueryHandler : IRequestHandler<SearchClientsQuery, Result<IReadOnlyList<ClientRow>>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public SearchClientsQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<ClientRow>>> Handle(SearchClientsQuery request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.ViewClinicalRecords))
            return Result.Failure(SearchClientsError.NotAllowed, Access.Denied(Permission.ViewClinicalRecords));

        var query = _dbContext.Clients.AsNoTracking().Include(c => c.Patients).AsQueryable();

        query = Matching(query, request.Text);

        if (request.Filter == ClientFilter.NewThisYear)
        {
            var startOfYear = StartOfYearUtc(_clock);
            query = query.Where(c => c.CreatedUtc >= startOfYear);
        }

        var clients = await query.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToListAsync(cancellationToken);
        var ids = clients.Select(c => c.Id).ToList();
        var balances = await Balances.ForClientsAsync(_dbContext, ids, cancellationToken);
        var visits = await LastVisits.ForPatientsAsync(_dbContext, clients.SelectMany(c => c.Patients).Select(p => p.Id).ToList(), cancellationToken);

        var rows = clients.Select(c => new ClientRow(
                c.Id, c.ClientNumber, c.FullName, c.Initials, c.Phone, c.Email,
                c.Patients.Where(p => !p.IsDeceased).Select(p => p.Name).ToList(),
                c.Patients.Select(p => visits.TryGetValue(p.Id, out var v) ? v : (DateTime?)null).Max(),
                balances.GetValueOrDefault(c.Id),
                Utc.Mark(c.CreatedUtc)))
            .Where(r => request.Filter != ClientFilter.BalanceDue || r.Balance > 0);

        rows = request.Sort switch
        {
            ClientSort.LastVisit => rows.OrderByDescending(r => r.LastVisitUtc ?? DateTime.MinValue),
            ClientSort.Balance => rows.OrderByDescending(r => r.Balance),
            _ => rows
        };

        IReadOnlyList<ClientRow> result = rows.Take(request.Take).ToList();
        return Result.Success(result);
    }

    /// <summary>The text search, shared with <see cref="CountClientsQueryHandler"/> so the filter counts match the list.</summary>
    internal static IQueryable<Client> Matching(IQueryable<Client> query, string? search)
    {
        if (Text.Clean(search) is not { } text)
            return query;

        var digits = Client.DigitsOf(text);
        var like = Search.Anywhere(text);
        // Postcodes and microchips are compared without spaces, so "BS65QT" finds "BS6 5QT" and a chip typed in groups
        // of three finds the stored digits.
        var compact = Search.Anywhere(text.Replace(" ", ""));
        var number = text.StartsWith("CL-", StringComparison.OrdinalIgnoreCase) && int.TryParse(text[3..], out var n) ? n : -1;
        return query.Where(c =>
            c.Id == number ||
            EF.Functions.Like(c.FirstName + " " + c.LastName, like, Search.EscapeText) ||
            (c.Email != null && EF.Functions.Like(c.Email, like, Search.EscapeText)) ||
            (c.Postcode != null && EF.Functions.Like(c.Postcode.Replace(" ", ""), compact, Search.EscapeText)) ||
            (digits.Length >= 4 && c.PhoneDigits.Contains(digits)) ||
            c.Patients.Any(p => EF.Functions.Like(p.Name, like, Search.EscapeText) ||
                (p.Microchip != null && EF.Functions.Like(p.Microchip, compact, Search.EscapeText))));
    }

    internal static DateTime StartOfYearUtc(IClock clock) =>
        ClinicTime.ToUtc(new DateOnly(ClinicTime.Today(clock.UtcNow).Year, 1, 1), TimeOnly.MinValue);
}

/// <summary>How many clients match the search text under each filter, for the counts on the Clients screen's filters.</summary>
public class CountClientsQuery : IRequest<Result<IReadOnlyDictionary<ClientFilter, int>>>
{
    public string? Text { get; init; }
}

public class CountClientsQueryHandler : IRequestHandler<CountClientsQuery, Result<IReadOnlyDictionary<ClientFilter, int>>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public CountClientsQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyDictionary<ClientFilter, int>>> Handle(CountClientsQuery request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.ViewClinicalRecords))
            return Result.Failure(SearchClientsError.NotAllowed, Access.Denied(Permission.ViewClinicalRecords));

        var startOfYear = SearchClientsQueryHandler.StartOfYearUtc(_clock);
        var clients = await SearchClientsQueryHandler.Matching(_dbContext.Clients.AsNoTracking(), request.Text)
            .Select(c => new { c.Id, IsNew = c.CreatedUtc >= startOfYear })
            .ToListAsync(cancellationToken);
        var balances = await Balances.ForClientsAsync(_dbContext, clients.Select(c => c.Id).ToList(), cancellationToken);

        IReadOnlyDictionary<ClientFilter, int> counts = new Dictionary<ClientFilter, int>
        {
            [ClientFilter.All] = clients.Count,
            [ClientFilter.BalanceDue] = clients.Count(c => balances.GetValueOrDefault(c.Id) > 0),
            [ClientFilter.NewThisYear] = clients.Count(c => c.IsNew),
        };
        return Result.Success(counts);
    }
}
