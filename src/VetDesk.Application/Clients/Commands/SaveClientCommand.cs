using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Clients;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Clients.Commands;

public enum SaveClientError { NotAllowed, InvalidRequest, NotFound, DuplicatePhone, ChangedElsewhere }

/// <summary>Adds a client or edits their contact details. Two clients can't share a phone number.</summary>
public class SaveClientCommand : IRequest<Result<int>>
{
    public int? Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Postcode { get; init; }
    public bool RemindersOptIn { get; init; } = true;
    public Guid? Version { get; init; }
}

public class SaveClientCommandHandler : IRequestHandler<SaveClientCommand, Result<int>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public SaveClientCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result<int>> Handle(SaveClientCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.ViewClinicalRecords))
            return Result.Failure(SaveClientError.NotAllowed, Access.Denied(Permission.ViewClinicalRecords));

        if (ClientRules.Validate(request.LastName, request.Phone, request.Email) is { } problem)
            return Result.Failure(SaveClientError.InvalidRequest, problem);

        var digits = Client.DigitsOf(request.Phone);
        var duplicate = await _dbContext.Clients.AsNoTracking()
            .FirstOrDefaultAsync(c => c.PhoneDigits == digits && c.Id != request.Id, cancellationToken);
        if (duplicate is not null)
            return Result.Failure(SaveClientError.DuplicatePhone,
                $"{request.Phone.Trim()} already belongs to {duplicate.FullName} ({duplicate.ClientNumber}). Add the pet to their account instead.");

        Client client;
        if (request.Id is { } id)
        {
            var existing = await _dbContext.Clients.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (existing is null)
                return Result.Failure(SaveClientError.NotFound, "That client no longer exists.");
            if (request.Version is { } version && existing.Version != version)
                return Result.Failure(SaveClientError.ChangedElsewhere, Saving.ChangedElsewhere);
            client = existing;
            client.FirstName = request.FirstName.Trim();
            client.LastName = request.LastName.Trim();
            client.Phone = request.Phone;
        }
        else
        {
            client = new Client { FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), Phone = request.Phone };
            _dbContext.Clients.Add(client);
        }

        client.Email = Text.Clean(request.Email);
        client.Address = Text.Clean(request.Address);
        client.Postcode = Text.Clean(request.Postcode)?.ToUpperInvariant();
        client.RemindersOptIn = request.RemindersOptIn;

        if (!await _dbContext.TrySaveChangesAsync(cancellationToken))
            return Result.Failure(SaveClientError.ChangedElsewhere, Saving.ChangedElsewhere);

        return Result.Success(client.Id);
    }
}
