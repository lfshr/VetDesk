using Microsoft.Extensions.DependencyInjection;
using VetDesk.Application.Common.Interfaces;

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
        return services;
    }
}
