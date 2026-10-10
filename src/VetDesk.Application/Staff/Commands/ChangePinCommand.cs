using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using SimpleResult;

namespace VetDesk.Application.Staff.Commands;

public enum ChangePinError { NotSignedIn, InvalidRequest, WrongPin }

public class ChangePinCommand : IRequest<Result>
{
    public string CurrentPin { get; init; } = string.Empty;
    public string NewPin { get; init; } = string.Empty;
}

public class ChangePinCommandHandler : IRequestHandler<ChangePinCommand, Result>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IPasswordHasher _hasher;
    private readonly IClock _clock;
    private readonly IDevicePinLimit _deviceLimit;

    public ChangePinCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user, IPasswordHasher hasher, IClock clock, IDevicePinLimit deviceLimit)
    {
        _dbContext = dbContext;
        _user = user;
        _hasher = hasher;
        _clock = clock;
        _deviceLimit = deviceLimit;
    }

    public async Task<Result> Handle(ChangePinCommand request, CancellationToken cancellationToken)
    {
        if (_user.StaffId is not { } staffId)
            return Result.Failure(ChangePinError.NotSignedIn, Access.NotSignedIn);
        if (StaffRules.PinProblem(request.NewPin) is { } problem)
            return Result.Failure(ChangePinError.InvalidRequest, problem);

        var staff = await _dbContext.StaffMembers.FirstAsync(s => s.Id == staffId, cancellationToken);
        if (await StaffRules.CheckPinAsync(_dbContext, _hasher, _deviceLimit, staff, request.CurrentPin, _clock.UtcNow, cancellationToken) is { } wrong)
            return Result.Failure(ChangePinError.WrongPin, wrong);

        staff.PinHash = _hasher.Hash(request.NewPin);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
