using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Clients;
using SimpleResult;

namespace VetDesk.Application.Clients.Queries;

public enum FindClientByPhoneError { NotSignedIn }

/// <summary>Used while typing a new client's phone number, to warn that the owner is already on file.</summary>
public class FindClientByPhoneQuery : IRequest<Result<ClientMatch?>>
{
    public string Phone { get; init; } = string.Empty;
    public int? ExcludeClientId { get; init; }
}

public record ClientMatch(int Id, string FullName, string ClientNumber, IReadOnlyList<string> PetNames);

public class FindClientByPhoneQueryHandler : IRequestHandler<FindClientByPhoneQuery, Result<ClientMatch?>>
{
    internal const int MinDigits = 10;

    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public FindClientByPhoneQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result<ClientMatch?>> Handle(FindClientByPhoneQuery request, CancellationToken cancellationToken)
    {
        if (!_user.IsSignedIn())
            return Result.Failure(FindClientByPhoneError.NotSignedIn, Access.NotSignedIn);

        var digits = Client.DigitsOf(request.Phone);
        if (digits.Length < MinDigits)
            return Result.Success<ClientMatch?>(null);

        var client = await _dbContext.Clients.AsNoTracking().Include(c => c.Patients)
            .FirstOrDefaultAsync(c => c.PhoneDigits == digits && c.Id != request.ExcludeClientId, cancellationToken);

        return Result.Success(client is null
            ? null
            : new ClientMatch(client.Id, client.FullName, client.ClientNumber, client.Patients.Select(p => p.Name).ToList()));
    }
}
