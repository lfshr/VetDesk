using Microsoft.AspNetCore.DataProtection;
using VetDesk.Application;
using VetDesk.Infrastructure;
using VetDesk.Infrastructure.Persistence;
using VetDesk.Infrastructure.Services;
using VetDesk.UI;
using VetDesk.UI.Services;
using Microsoft.AspNetCore.Components.Server.Circuits;
using VetDesk.Web.Components;
using VetDesk.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddVetDeskUi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CircuitHandler, DeviceAddressCircuitHandler>();
builder.Services.AddScoped<CircuitHandler, DeskConnectionCircuitHandler>();

// On the practice server the keys that protect Blazor's circuit state and antiforgery tokens must survive a container
// restart, or every open form breaks after an update. The Compose file points this at a volume.
var keysPath = builder.Configuration["VetDesk:DataProtectionKeysPath"];
if (!string.IsNullOrWhiteSpace(keysPath))
{
    builder.Services.AddDataProtection()
        .SetApplicationName("VetDesk")
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

var allowList = NetworkAllowList.FromSetting(builder.Configuration["VetDesk:AllowedNetworks"]);

var app = builder.Build();

if (allowList.IsEmpty && !app.Environment.IsDevelopment())
    app.Logger.LogWarning("VetDesk:AllowedNetworks is empty, so VetDesk answers devices on any network.");

await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitialiseAsync();
app.Services.GetRequiredService<BackupScheduler>().Start();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/", createScopeForErrors: true);
    app.UseHsts();
}

// Runs after the forwarded-headers middleware (added at the start of the pipeline when
// ASPNETCORE_FORWARDEDHEADERS_ENABLED is set), so behind Caddy this checks the desk's address, not Caddy's.
app.Use(async (context, next) =>
{
    if (allowList.Allows(context.Connection.RemoteIpAddress))
    {
        await next(context);
        return;
    }

    app.Logger.LogWarning("Refused a request from {Address}, which is outside VetDesk:AllowedNetworks.", context.Connection.RemoteIpAddress);
    context.Response.StatusCode = StatusCodes.Status403Forbidden;
    await context.Response.WriteAsync("VetDesk can only be used on the practice network.");
});

app.UseStaticFiles();
app.UseAntiforgery();

app.MapDefaultEndpoints();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
