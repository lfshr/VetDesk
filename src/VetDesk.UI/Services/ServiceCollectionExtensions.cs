using Microsoft.Extensions.DependencyInjection;
using VetDesk.Application.Common.Interfaces;
using VetDesk.UI.Services.Live;

namespace VetDesk.UI.Services;

public static class ServiceCollectionExtensions
{
    /// <summary>Services the screens need. Hosts call this after AddApplicationServices and AddInfrastructureServices.</summary>
    public static IServiceCollection AddVetDeskUi(this IServiceCollection services)
    {
        services.AddScoped<UserSession>();
        services.AddScoped<CurrentUser>();
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentUser>());
        services.AddScoped<DeviceAddress>();
        services.AddScoped<IDeviceAddress>(sp => sp.GetRequiredService<DeviceAddress>());
        services.AddScoped<Backend>();
        services.AddScoped<ToastService>();
        services.AddScoped<DialogService>();
        services.AddScoped<Hotkeys>();
        services.AddScoped<Crumbs>();
        services.AddScoped<OpenPatients>();

        // Live editing: one desk per window or tab, and who has what open and what they're typing, across every desk.
        services.AddScoped<Desk>();
        services.AddScoped<IDesk>(sp => sp.GetRequiredService<Desk>());
        services.AddSingleton<Presence>();
        services.AddSingleton<LiveDrafts>();
        return services;
    }
}
