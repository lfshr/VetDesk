using Microsoft.Extensions.DependencyInjection;

namespace VetDesk.Application;

public static class ConfigureServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ConfigureServices).Assembly));

        return services;
    }
}
