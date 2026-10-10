using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Application.Reports;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Settings.Queries;

public enum ExportClientsError { NotAllowed }

/// <summary>"Export": every client and pet as a table, for the screen to save as CSV.</summary>
public class ExportClientsQuery : IRequest<Result<ReportTable>>
{
}

public class ExportClientsQueryHandler : IRequestHandler<ExportClientsQuery, Result<ReportTable>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public ExportClientsQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result<ReportTable>> Handle(ExportClientsQuery request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.ManageSettings))
            return Result.Failure(ExportClientsError.NotAllowed, Access.Denied(Permission.ManageSettings));

        var patients = await _dbContext.Patients.AsNoTracking().Include(p => p.Client)
            .OrderBy(p => p.Client.LastName).ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return Result.Success(new ReportTable("Clients and patients", $"{patients.Count} patients",
            ["Client number", "Owner", "Phone", "Email", "Address", "Postcode", "Reminders", "Patient number", "Pet", "Species", "Breed", "Sex", "Date of birth", "Microchip", "Deceased"],
            patients.Select(p => (IReadOnlyList<string>)new[]
            {
                p.Client.ClientNumber, p.Client.FullName, p.Client.Phone, p.Client.Email ?? "", p.Client.Address ?? "", p.Client.Postcode ?? "",
                p.Client.RemindersOptIn ? "Yes" : "No", p.PatientNumber, p.Name, p.Species, p.Breed ?? "", p.SexLabel,
                p.DateOfBirth?.ToString("yyyy-MM-dd") ?? "", p.Microchip ?? "", p.IsDeceased ? "Yes" : "No"
            }).ToList(), null));
    }
}
