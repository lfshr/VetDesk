using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Clients;
using VetDesk.Domain.Entities.Patients;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Patients.Queries;

public enum SearchPatientsError { NotAllowed }

public enum SpeciesFilter { All, Dogs, Cats, Other }

public enum PatientSort { LastVisit, PetName, OwnerSurname }

/// <summary>
/// Finds patients by pet or owner name, phone, postcode, microchip or PT number, as the Patients screen and the
/// Today quick search describe.
/// </summary>
public class SearchPatientsQuery : IRequest<Result<IReadOnlyList<PatientRow>>>
{
    public string? Text { get; init; }
    public SpeciesFilter Species { get; init; }
    public PatientSort Sort { get; init; }
    public bool IncludeDeceased { get; init; } = true;
    public int Take { get; init; } = 200;
}

public class SearchPatientsQueryHandler : IRequestHandler<SearchPatientsQuery, Result<IReadOnlyList<PatientRow>>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public SearchPatientsQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<PatientRow>>> Handle(SearchPatientsQuery request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.ViewClinicalRecords))
            return Result.Failure(SearchPatientsError.NotAllowed, Access.Denied(Permission.ViewClinicalRecords));

        var query = _dbContext.Patients.AsNoTracking()
            .Include(p => p.Client)
            .Include(p => p.Allergies)
            .AsQueryable();

        if (!request.IncludeDeceased)
            query = query.Where(p => !p.IsDeceased);

        query = request.Species switch
        {
            SpeciesFilter.Dogs => query.Where(p => p.Species == "Dog"),
            SpeciesFilter.Cats => query.Where(p => p.Species == "Cat"),
            SpeciesFilter.Other => query.Where(p => p.Species != "Dog" && p.Species != "Cat"),
            _ => query
        };

        query = Matching(query, request.Text);

        var patients = await query.OrderBy(p => p.Name).Take(2000).ToListAsync(cancellationToken);
        var visits = await LastVisits.ForPatientsAsync(_dbContext, patients.Select(p => p.Id).ToList(), cancellationToken);
        var balances = await Balances.ForClientsAsync(_dbContext, patients.Select(p => p.Client.Id).Distinct().ToList(), cancellationToken);
        var today = ClinicTime.Today(_clock.UtcNow);

        IEnumerable<Patient> sorted = request.Sort switch
        {
            PatientSort.PetName => patients.OrderBy(p => p.Name),
            PatientSort.OwnerSurname => patients.OrderBy(p => p.Client.LastName).ThenBy(p => p.Name),
            _ => patients.OrderByDescending(p => visits.TryGetValue(p.Id, out var v) ? v : DateTime.MinValue).ThenBy(p => p.Name)
        };

        IReadOnlyList<PatientRow> result = sorted.Take(request.Take).Select(p => ToRow(p, visits, balances, today)).ToList();
        return Result.Success(result);
    }

    /// <summary>The text search, shared with <see cref="CountPatientsQueryHandler"/> so the filter counts match the list.</summary>
    internal static IQueryable<Patient> Matching(IQueryable<Patient> query, string? search)
    {
        if (Text.Clean(search) is not { } text)
            return query;

        var like = Search.Anywhere(text);
        var compact = Search.Anywhere(text.Replace(" ", ""));
        var digits = Client.DigitsOf(text);
        var number = text.StartsWith("PT-", StringComparison.OrdinalIgnoreCase) && int.TryParse(text[3..], out var n) ? n : -1;
        return query.Where(p =>
            p.Id == number ||
            EF.Functions.Like(p.Name, like, Search.EscapeText) ||
            EF.Functions.Like(p.Client.FirstName + " " + p.Client.LastName, like, Search.EscapeText) ||
            (p.Microchip != null && EF.Functions.Like(p.Microchip, compact, Search.EscapeText)) ||
            (p.Client.Postcode != null && EF.Functions.Like(p.Client.Postcode.Replace(" ", ""), compact, Search.EscapeText)) ||
            (digits.Length >= 4 && p.Client.PhoneDigits.Contains(digits)));
    }

    private static PatientRow ToRow(Patient p, Dictionary<int, DateTime> visits, Dictionary<int, decimal> balances, DateOnly today) => new(
        p.Id, p.Name, p.PatientNumber, p.Species, p.SpeciesAndBreed, p.AgeLabel(today), p.Client.Id, p.Client.FullName,
        p.Client.Phone, p.Microchip, visits.TryGetValue(p.Id, out var v) ? v : null, balances.GetValueOrDefault(p.Client.Id),
        p.IsDeceased, p.Allergies.Select(a => a.Label).ToList());
}

/// <summary>How many patients match the search text in each species filter, for the counts on the Patients screen's filters.</summary>
public class CountPatientsQuery : IRequest<Result<IReadOnlyDictionary<SpeciesFilter, int>>>
{
    public string? Text { get; init; }
}

public class CountPatientsQueryHandler : IRequestHandler<CountPatientsQuery, Result<IReadOnlyDictionary<SpeciesFilter, int>>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public CountPatientsQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result<IReadOnlyDictionary<SpeciesFilter, int>>> Handle(CountPatientsQuery request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.ViewClinicalRecords))
            return Result.Failure(SearchPatientsError.NotAllowed, Access.Denied(Permission.ViewClinicalRecords));

        var bySpecies = await SearchPatientsQueryHandler.Matching(_dbContext.Patients.AsNoTracking(), request.Text)
            .GroupBy(p => p.Species)
            .Select(g => new { Species = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var dogs = bySpecies.Where(g => g.Species == "Dog").Sum(g => g.Count);
        var cats = bySpecies.Where(g => g.Species == "Cat").Sum(g => g.Count);
        var all = bySpecies.Sum(g => g.Count);
        IReadOnlyDictionary<SpeciesFilter, int> counts = new Dictionary<SpeciesFilter, int>
        {
            [SpeciesFilter.All] = all,
            [SpeciesFilter.Dogs] = dogs,
            [SpeciesFilter.Cats] = cats,
            [SpeciesFilter.Other] = all - dogs - cats,
        };
        return Result.Success(counts);
    }
}
