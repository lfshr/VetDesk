using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleResult;

namespace VetDesk.UI.Services;

/// <summary>
/// How screens talk to the Application layer. Every request gets a fresh DI scope, so each command has its own
/// short-lived DbContext: the UI scope lasts as long as the browser tab's connection (its circuit), and one
/// long-lived DbContext would serve stale data to the whole session. Unexpected exceptions (database down, a bug) are logged
/// and come back as a failed result so the screen can say so instead of crashing.
/// </summary>
public class Backend
{
    private readonly IServiceScopeFactory _scopes;
    private readonly UserSession _session;
    private readonly DeviceAddress _device;
    private readonly ILogger<Backend> _logger;

    public Backend(IServiceScopeFactory scopes, UserSession session, DeviceAddress device, ILogger<Backend> logger)
    {
        _scopes = scopes;
        _session = session;
        _device = device;
        _logger = logger;
    }

    public async Task<Result<T>> Send<T>(IRequest<Result<T>> request)
    {
        try
        {
            await using var scope = Prepare();
            return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Request} failed", request.GetType().Name);
            return Result<T>.Failure(Unexpected(ex));
        }
    }

    public async Task<Result> Send(IRequest<Result> request)
    {
        try
        {
            await using var scope = Prepare();
            return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Request} failed", request.GetType().Name);
            return Result.Failure(Unexpected(ex));
        }
    }

    private AsyncServiceScope Prepare()
    {
        var scope = _scopes.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<CurrentUser>().Set(_session.IsLocked ? null : _session.Staff);
        scope.ServiceProvider.GetRequiredService<DeviceAddress>().Address = _device.Address;
        return scope;
    }

    private static Exception Unexpected(Exception ex) =>
        new InvalidOperationException("VetDesk couldn't complete that. Check the connection to the database and try again. " +
                                      "If it keeps happening, tell your practice administrator.", ex);
}
