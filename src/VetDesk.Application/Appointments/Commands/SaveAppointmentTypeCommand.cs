using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Appointments;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Appointments.Commands;

public enum SaveAppointmentTypeError { NotAllowed, InvalidRequest, NotFound }

public class SaveAppointmentTypeCommand : IRequest<Result<int>>
{
    public int? Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public AppointmentKind Kind { get; init; }
    public int Minutes { get; init; } = 15;
    public string SeenBy { get; init; } = "Vet";
    public bool OnlineBooking { get; init; }
    public AppointmentColour Colour { get; init; }
    public bool IsActive { get; init; } = true;
}

public class SaveAppointmentTypeCommandHandler : IRequestHandler<SaveAppointmentTypeCommand, Result<int>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public SaveAppointmentTypeCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result<int>> Handle(SaveAppointmentTypeCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.ManageSettings))
            return Result.Failure(SaveAppointmentTypeError.NotAllowed, Access.Denied(Permission.ManageSettings));
        if (!Text.Has(request.Name))
            return Result.Failure(SaveAppointmentTypeError.InvalidRequest, "Give the appointment type a name.");
        if (request.Minutes is < 5 or > SaveAppointmentCommand.MaxMinutes || request.Minutes % 5 != 0)
            return Result.Failure(SaveAppointmentTypeError.InvalidRequest, "Length must be in 5-minute steps, up to 8 hours.");

        AppointmentType type;
        if (request.Id is { } id)
        {
            var existing = await _dbContext.AppointmentTypes.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
            if (existing is null)
                return Result.Failure(SaveAppointmentTypeError.NotFound, "That appointment type no longer exists.");
            type = existing;
            type.Name = request.Name.Trim();
        }
        else
        {
            type = new AppointmentType { Name = request.Name.Trim() };
            _dbContext.AppointmentTypes.Add(type);
        }

        type.Kind = request.Kind;
        type.Minutes = request.Minutes;
        type.SeenBy = request.SeenBy;
        type.OnlineBooking = request.OnlineBooking;
        type.Colour = request.Colour;
        type.IsActive = request.IsActive;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(type.Id);
    }
}
