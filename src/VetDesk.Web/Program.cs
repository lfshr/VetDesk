using Microsoft.AspNetCore.DataProtection;
using VetDesk.Application;
using VetDesk.Infrastructure;
using VetDesk.Infrastructure.Persistence;
using VetDesk.Infrastructure.Services;
using VetDesk.UI;
using VetDesk.UI.Services;
using VetDesk.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddVetDeskUi();

// On the practice server the keys that protect Blazor's circuit state and antiforgery tokens must survive a container
// restart, or every open form breaks after an update. The Compose file points this at a volume.
var keysPath = builder.Configuration["VetDesk:DataProtectionKeysPath"];
if (!string.IsNullOrWhiteSpace(keysPath))
{
    builder.Services.AddDataProtection()
        .SetApplicationName("VetDesk")
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitialiseAsync();
app.Services.GetRequiredService<BackupScheduler>().Start();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapDefaultEndpoints();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
