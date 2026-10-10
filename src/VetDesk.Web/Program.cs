using VetDesk.Application;
using VetDesk.Infrastructure;
using VetDesk.Infrastructure.Persistence;
using VetDesk.Infrastructure.Services;
using VetDesk.UI;
using VetDesk.UI.Services;
using VetDesk.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddVetDeskUi();

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

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
