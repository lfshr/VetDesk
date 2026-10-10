using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Clients;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Clients;
using VetDesk.Domain.Entities.Patients;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Patients.Commands;

public enum CreateClientAndPatientError { NotAllowed, InvalidRequest }

public record CreatedClientAndPatient(int ClientId, int PatientId, bool AddedToExistingClient, string ClientName);

/// <summary>
/// The "New client and patient" form. When the phone number already belongs to a client, the pet is added to that
/// account instead of creating a duplicate owner, as the design's warning says.
/// </summary>
public class CreateClientAndPatientCommand : IRequest<Result<CreatedClientAndPatient>>
{
    public string OwnerName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string PetName { get; init; } = string.Empty;
    public string Species { get; init; } = "Dog";
    public string? Breed { get; init; }
    public Sex Sex { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? Microchip { get; init; }
}

public class CreateClientAndPatientCommandHandler : IRequestHandler<CreateClientAndPatientCommand, Result<CreatedClientAndPatient>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public CreateClientAndPatientCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result<CreatedClientAndPatient>> Handle(CreateClientAndPatientCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.ViewClinicalRecords))
            return Result.Failure(CreateClientAndPatientError.NotAllowed, Access.Denied(Permission.ViewClinicalRecords));

        var (first, last) = Client.SplitName(request.OwnerName);
        if (ClientRules.Validate(last, request.Phone, request.Email) is { } clientProblem)
            return Result.Failure(CreateClientAndPatientError.InvalidRequest, clientProblem);
        if (PatientRules.Validate(request.PetName, request.Species, request.DateOfBirth, ClinicTime.Today(_clock.UtcNow)) is { } petProblem)
            return Result.Failure(CreateClientAndPatientError.InvalidRequest, petProblem);

        var digits = Client.DigitsOf(request.Phone);
        var client = await _dbContext.Clients.FirstOrDefaultAsync(c => c.PhoneDigits == digits, cancellationToken);
        var existing = client is not null;
        if (client is null)
        {
            client = new Client { FirstName = first, LastName = last, Phone = request.Phone, Email = Text.Clean(request.Email) };
            _dbContext.Clients.Add(client);
        }

        var patient = new Patient
        {
            Name = request.PetName.Trim(),
            Client = client,
            Species = PatientRules.CleanSpecies(request.Species),
            Breed = Text.Clean(request.Breed),
            Sex = request.Sex,
            DateOfBirth = request.DateOfBirth,
            Microchip = PatientRules.CleanMicrochip(request.Microchip),
        };
        _dbContext.Patients.Add(patient);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreatedClientAndPatient(client.Id, patient.Id, existing, client.FullName));
    }
}
