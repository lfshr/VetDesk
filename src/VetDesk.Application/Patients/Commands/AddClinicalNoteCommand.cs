using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Patients;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Patients.Commands;

public enum AddClinicalNoteError { NotAllowed, InvalidRequest, NotFound }

/// <summary>Adds a note to the clinical history. Notes are permanent; a correction is another note.</summary>
public class AddClinicalNoteCommand : IRequest<Result<int>>
{
    public int PatientId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Body { get; init; }
}

public class AddClinicalNoteCommandHandler : IRequestHandler<AddClinicalNoteCommand, Result<int>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public AddClinicalNoteCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result<int>> Handle(AddClinicalNoteCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.WriteClinicalNotes))
            return Result.Failure(AddClinicalNoteError.NotAllowed, Access.Denied(Permission.WriteClinicalNotes));
        if (Text.Clean(request.Title) is not { } title)
            return Result.Failure(AddClinicalNoteError.InvalidRequest, "Give the note a title.");

        var patient = await _dbContext.Patients.FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken);
        if (patient is null)
            return Result.Failure(AddClinicalNoteError.NotFound, "That patient no longer exists.");

        var note = new ClinicalNote
        {
            Patient = patient,
            Title = title,
            Body = request.Body?.Trim() ?? string.Empty,
            Author = _user.Name,
            WrittenUtc = _clock.UtcNow,
        };
        _dbContext.ClinicalNotes.Add(note);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(note.Id);
    }
}
