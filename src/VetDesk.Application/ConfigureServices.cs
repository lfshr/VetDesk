using Microsoft.Extensions.DependencyInjection;
using VetDesk.Application.Common.Live;

namespace VetDesk.Application;

public static class ConfigureServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ConfigureServices).Assembly));
        services.AddSingleton<RecordChangeFeed>();

        return services;
    }
}
