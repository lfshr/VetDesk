# VetDesk

Practice management for a single-site veterinary clinic: booking and check-in, clinical records, consultations,
dispensing and stock, controlled-drug witnessing, invoicing with VAT, cash-up, user roles and nightly backups.

The screens are Blazor components. At the practice they run inside a Windows desktop app (WPF hosting a
BlazorWebView), so there's no web server to install or keep running.

## Projects

| Project | What it is |
| --- | --- |
| `src/VetDesk.Domain` | Entities and the rules that belong to them (VAT, ages, locking a completed consultation). |
| `src/VetDesk.Application` | One command or query per use case (MediatR), each returning a `Result` instead of throwing. Permission checks live here, not in the screens. |
| `src/VetDesk.Infrastructure` | EF Core (`VetDeskDbContext`, SQL Server migrations), password hashing, backups, demo data. |
| `src/SimpleResult` | The `Result` / `Result<T>` type used by every handler. |
| `src/VetDesk.UI` | All the screens, dialogs and the stylesheet (a Razor class library). |
| `src/VetDesk.Desktop` | **The app the practice runs**: a WPF window hosting `VetDesk.UI`. |
| `src/VetDesk.Web` | The same screens in a browser, for development on machines without Windows. |
| `src/VetDesk.AppHost` | Aspire host for development: SQL Server in a container, plus Web and Desktop wired to it. |
| `src/VetDesk.ServiceDefaults` | Aspire's telemetry and health-check defaults, used by `VetDesk.Web`. |
| `tests/*` | NUnit + Shouldly tests for the domain rules and the handlers (handlers run against in-memory SQLite). |

The screens call the Application layer in-process (`Backend.Send(...)`), with a fresh DI scope per request, so
every screen goes through the same commands, queries and permission checks.

## Running it

You need the .NET 10 SDK. The desktop app needs Windows 10 or 11 with the WebView2 runtime (already present on
current Windows).

**Everything with Aspire, SQL Server in a container (needs Docker Desktop or Podman):**

```
dotnet run --project src/VetDesk.AppHost
```

This starts SQL Server in a container (`vetdesk-sql`, data kept in the `vetdesk-sql-data` volume), runs the
migrations, adds the demo practice, then starts the browser host and, on Windows, the desktop app against it. The
Aspire dashboard opens with every app's logs and traces. The container keeps running after you stop the AppHost so
the next start is quick; remove the container and volume to start again from an empty database.

**Desktop (Windows), with demo data:**

```
dotnet run --project src/VetDesk.Desktop
```

A Debug build reads `appsettings.Debug.json`: a `VetDeskDev` database on `localhost\SQLEXPRESS` (install SQL Server
Express first), created by migrations and filled with a demo practice on first start. F12 opens the developer tools.

**Signing in.** Tap your name and enter your 4-digit PIN; the same PIN unlocks the desk and witnesses controlled
drugs. Five wrong PINs in a row block that person for five minutes. Administrators also have a password, asked for
before users and practice settings open; they close again when the desk locks. In the demo data:

| Staff | Role | PIN | Password |
| --- | --- | --- | --- |
| Douglas, Heather, Jen | Vet and administrator | `1234` | `vetdesk-demo` |
| Janet, Lesley, Lorna | Nurse | `1234` | none |
| Liam | Practice administrator | `1234` | `vetdesk-demo` |

An account from before PINs (it has a password but no PIN) taps its name, enters the password once and chooses a PIN.

**Browser (any OS), with demo data:**

```
dotnet run --project src/VetDesk.Web
```

then open the URL it prints. This uses a `VetDeskWeb` database on `localhost\SQLEXPRESS`.

**Tests:**

```
dotnet test VetDesk.sln
```

## Installing at the practice

1. Install SQL Server Express (or use an existing SQL Server) on the machine that will hold the data.
2. Publish the desktop app: `dotnet publish src/VetDesk.Desktop -c Release -r win-x64 --self-contained`.
3. Copy the publish folder to each desk PC and edit `appsettings.json` next to `VetDesk.Desktop.exe`:

   ```json
   {
     "VetDesk": { "DatabaseProvider": "SqlServer", "SeedDemoData": false },
     "ConnectionStrings": { "VetDesk": "Server=CLINIC-PC\\SQLEXPRESS;Database=VetDesk;Trusted_Connection=True;TrustServerCertificate=True" }
   }
   ```

4. Start VetDesk on one PC. It creates or upgrades the database (EF Core migrations) and, on an empty database,
   asks for the first administrator (name, PIN and password). That person then adds the other staff, each with a
   PIN to hand over, under Settings › Users and roles. Surnames and emails are optional.

`DatabaseProvider` can also be `Sqlite` for a practice with a single PC; the database is then a file
(`Data Source=%LOCALAPPDATA%\VetDesk\vetdesk.db`) and is created from the model rather than by migrations.

## Backups and restore

Set the folder and time under Settings › Backup. While any desk has VetDesk open, it checks every few minutes and
takes the day's backup once the time has passed; the first desk to notice does it and the others skip. "Back up now"
takes one immediately. The history on that page shows every run and any failure. If no folder is set, each night's
run is recorded as a failure and raises a notice, so a practice can't go without backups unnoticed.

On the Docker Compose server install the folder starts as the backup volume (`/var/opt/mssql/backup`, set by
`VetDesk__DefaultBackupFolder`), so backups run from the first night. In `.env`, point `SQL_BINDMOUNT_0` and
`SQL_INIT_BINDMOUNT_1` at the same host folder: the sql-init job hands that folder to SQL Server's user (uid 10001)
on every start, because Docker creates a missing folder owned by root and SQL Server can't write to it.

- **SQL Server:** a native `BACKUP DATABASE ... WITH CHECKSUM` to a `.bak` file. The folder is on the **database
  server's** disk (or a share its service account can write to), not the desk PC's. Restore with SQL Server
  Management Studio (Databases › Restore Database) or `RESTORE DATABASE VetDesk FROM DISK = N'...\VetDesk-....bak'
  WITH REPLACE`, with VetDesk closed on every desk.
- **SQLite:** `VACUUM INTO` a `.db` file, a consistent copy taken while the app keeps running. Restore by closing
  VetDesk and copying the backup over the database file.

Copy the backup folder off-site; a backup on the same disk as the database doesn't survive that disk failing.

## Adding a migration

Migrations are for SQL Server and live in `src/VetDesk.Infrastructure/Migrations`:

```
dotnet tool install --global dotnet-ef
cd src/VetDesk.Infrastructure
dotnet ef migrations add <Name> --output-dir Migrations
```

## Not connected yet

These appear in the design but have no integration behind them, and the screens say so:

- SMS and email reminders: reminders are listed and can be exported to CSV, but nothing is sent.
- Card terminal: card payments are recorded, not taken.
- Labs, accounting packages, the client portal, online booking and multi-branch support are out of scope for v1.
