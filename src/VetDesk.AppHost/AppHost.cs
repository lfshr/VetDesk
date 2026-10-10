using Aspire.Hosting.Docker.Resources.ServiceNodes;

var builder = DistributedApplication.CreateBuilder(args);

// "aspire publish" writes docker-compose.yaml and .env for the practice server: SQL Server Express, VetDesk.Web and
// Caddy for HTTPS. "aspire run" (or F5) is unchanged and runs the development setup below. Publish-only parts are
// guarded by isPublishing so development keeps using sa and the Aspire dashboard.
var isPublishing = builder.ExecutionContext.IsPublishMode;

// No dashboard on the practice server: it would be another open port, and logs are read with "docker compose logs".
// Each service's log is kept by Docker with rotation (five files of 10 MB), so the server's disk can't fill up.
builder.AddDockerComposeEnvironment("compose")
    .WithDashboard(false)
    .ConfigureComposeFile(file =>
    {
        foreach (var service in file.Services.Values)
        {
            service.Logging = new Logging
            {
                Driver = "json-file",
                Options = new() { ["max-size"] = "10m", ["max-file"] = "5" },
            };
        }

        // Declared here because the web service adds its volume directly (see below).
        file.AddVolume(new Volume { Name = "vetdesk-web-home", Driver = "local" });
    });

// SQL Server in a container. The data volume keeps the database between runs; the container itself only lives as
// long as the Aspire session. Remove the "vetdesk-sql-data" volume to start from an empty database.
var sql = builder.AddSqlServer("sql")
    .WithContainerName("vetdesk-sql")
    .WithDataVolume("vetdesk-sql-data")
    .WithLifetime(ContainerLifetime.Session);

// The resource name becomes ConnectionStrings:VetDesk in each app, the same key appsettings.json uses.
var database = sql.AddDatabase("VetDesk");

if (!isPublishing)
{
    // Web runs the migrations and adds the demo practice before /health answers.
    var web = builder.AddProject<Projects.VetDesk_Web>("web")
        .WithReference(database)
        .WaitFor(database)
        .WithEnvironment("VetDesk__DatabaseProvider", "SqlServer")
        .WithEnvironment("VetDesk__SeedDemoData", "true")
        .WithHttpHealthCheck("/health")
        .WithExternalHttpEndpoints();

    // Desktop (WPF, Windows only) shares the database, as every desk at the practice does. It waits for Web so the
    // two don't both migrate and seed an empty database at the same time.
    if (OperatingSystem.IsWindows())
    {
        builder.AddProject<Projects.VetDesk_Desktop>("desktop")
            .WithReference(database)
            .WithEnvironment("VetDesk__DatabaseProvider", "SqlServer")
            .WaitFor(web);
    }
}
else
{
    // The name desks type into the browser, e.g. vetdesk.local. Caddy issues the certificate for it and Web only
    // answers requests for it.
    var serverName = builder.AddParameter("server-name");
    // Password for the vetdesk_app SQL login that Web uses instead of sa. Kept only in the server's .env.
    var appSqlPassword = builder.AddParameter("app-sql-password", secret: true);
    // The practice network's ranges, comma-separated, e.g. 192.168.1.0/24. Web refuses any other address.
    var allowedNetworks = builder.AddParameter("allowed-networks");

    // The default image is Developer edition, which is not licensed for production. Express caps each database at
    // 10 GB. The backup folder is a bind mount so the nightly .bak files can be copied straight to the USB drive.
    sql.WithEnvironment("MSSQL_PID", "Express")
        .WithBindMount("backups", "/var/opt/mssql/backup")
        .PublishAsDockerComposeService((_, service) => service.Restart = "unless-stopped");

    // One-off job, run on every "docker compose up": creates the database and the least-rights vetdesk_app login
    // (or resets its password to the one in .env), then exits. It reuses the SQL Server image for its sqlcmd.
    var sqlImage = sql.Resource.Annotations.OfType<ContainerImageAnnotation>().Single();
    var sqlInit = builder.AddContainer("sql-init", sqlImage.Image, sqlImage.Tag ?? "latest")
        .WithImageRegistry(sqlImage.Registry)
        .WithBindMount("deploy", "/init", isReadOnly: true)
        .WithEntrypoint("/bin/bash")
        .WithArgs("/init/sql-init.sh")
        .WithEnvironment("SQLCMDPASSWORD", sql.Resource.PasswordParameter)
        .WithEnvironment("APP_SQL_PASSWORD", appSqlPassword)
        .WaitFor(sql);

    var web = builder.AddProject<Projects.VetDesk_Web>("web")
        .WithEnvironment("ConnectionStrings__VetDesk", ReferenceExpression.Create(
            $"Server=sql,1433;Database=VetDesk;User ID=vetdesk_app;Password={appSqlPassword};TrustServerCertificate=true"))
        .WithEnvironment("VetDesk__DatabaseProvider", "SqlServer")
        .WithEnvironment("VetDesk__SeedDemoData", "false")
        .WithEnvironment("VetDesk__DataProtectionKeysPath", "/home/app/data-protection-keys")
        .WithEnvironment("AllowedHosts", serverName)
        .WithEnvironment("VetDesk__AllowedNetworks", allowedNetworks)
        .WaitForCompletion(sqlInit)
        .PublishAsDockerComposeService((_, service) =>
        {
            service.Restart = "unless-stopped";
            // Holds the Data Protection keys. Mounted over the app user's home so Docker gives the new volume that
            // folder's owner; a volume anywhere else would belong to root and the app couldn't write to it.
            service.AddVolume(new Volume { Name = "vetdesk-web-home", Type = "volume", Source = "vetdesk-web-home", Target = "/home/app" });
        });
    var webEndpoint = web.GetEndpoint("http");

    // HTTPS in front of Web. Caddy's own certificate authority signs the certificate and renews it; its root is
    // trusted once on each desk. The volume keeps that root, so the desks don't need to trust a new one.
    builder.AddContainer("caddy", "caddy", "2.10")
        .WithArgs("caddy", "reverse-proxy", "--from", ReferenceExpression.Create($"https://{serverName}"),
            "--to", webEndpoint.Property(EndpointProperty.HostAndPort), "--internal-certs")
        .WithVolume("vetdesk-caddy-data", "/data")
        .WithHttpsEndpoint(port: 443, targetPort: 443)
        .WithExternalHttpEndpoints()
        .WaitFor(web)
        .PublishAsDockerComposeService((_, service) => service.Restart = "unless-stopped");
}

builder.Build().Run();
