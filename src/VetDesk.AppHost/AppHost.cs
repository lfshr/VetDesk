var builder = DistributedApplication.CreateBuilder(args);

// SQL Server in a container. The data volume and persistent lifetime keep the database (and a warm SQL Server)
// between runs; remove the "vetdesk-sql" container and the "vetdesk-sql-data" volume to start from empty.
var sql = builder.AddSqlServer("sql")
    .WithContainerName("vetdesk-sql")
    .WithDataVolume("vetdesk-sql-data")
    .WithLifetime(ContainerLifetime.Session);

// The resource name becomes ConnectionStrings:VetDesk in each app, the same key appsettings.json uses.
var database = sql.AddDatabase("VetDesk");

// Web runs the migrations and adds the demo practice before /health answers.
var web = builder.AddProject<Projects.VetDesk_Web>("web")
    .WithReference(database)
    .WaitFor(database)
    .WithEnvironment("VetDesk__DatabaseProvider", "SqlServer")
    .WithEnvironment("VetDesk__SeedDemoData", "true")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

// Desktop (WPF, Windows only) shares the database, as every desk at the practice does. It waits for Web so the two
// don't both migrate and seed an empty database at the same time.
if (OperatingSystem.IsWindows())
{
    builder.AddProject<Projects.VetDesk_Desktop>("desktop")
        .WithReference(database)
        .WithEnvironment("VetDesk__DatabaseProvider", "SqlServer")
        .WaitFor(web);
}

builder.Build().Run();
