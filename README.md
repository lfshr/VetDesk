# VetDesk

Practice management for a single-site veterinary clinic: booking and check-in, clinical records, consultations,
dispensing and stock, controlled-drug witnessing, invoicing with VAT, cash-up, user roles and nightly backups.

The screens are Blazor Server components. At the practice, one server runs them (VetDesk.Web, SQL Server Express and
Caddy for HTTPS, in Docker). Each desk opens them in the VetDesk desktop app, a window onto that server that holds no
data, database login or secrets. A browser works too.

## Projects

| Project | What it is |
| --- | --- |
| `src/VetDesk.Domain` | Entities and the rules that belong to them (VAT, ages, locking a completed consultation). |
| `src/VetDesk.Application` | One command or query per use case (MediatR), each returning a `Result` instead of throwing. Permission checks live here, not in the screens. |
| `src/VetDesk.Infrastructure` | EF Core (`VetDeskDbContext`, SQL Server migrations), password hashing, backups, demo data. |
| `src/SimpleResult` | The `Result` / `Result<T>` type used by every handler. |
| `src/VetDesk.UI` | All the screens, dialogs and the stylesheet (a Razor class library). |
| `src/VetDesk.Web` | **The app the practice server runs**: the screens as Blazor Server. The only part of VetDesk that connects to the database. |
| `src/VetDesk.Desktop` | The desktop app at each desk: a WPF window showing VetDesk.Web in WebView2. Its only setting is the server's address. |
| `src/VetDesk.AppHost` | Aspire host: SQL Server in a container plus Web (and Desktop on Windows) for development; `aspire publish` writes the server's Docker Compose files. |
| `src/VetDesk.ServiceDefaults` | Aspire's telemetry and health-check defaults, used by `VetDesk.Web`. |
| `tests/*` | NUnit + Shouldly tests for the domain rules, the handlers (run against in-memory SQLite) and the desktop app's address rules. |

The screens call the Application layer in-process on the server (`Backend.Send(...)`), with a fresh DI scope per
request, so every screen goes through the same commands, queries and permission checks. There is no separate API.

## Running it

You need the .NET 10 SDK. The desktop app needs Windows 10 or 11 with the WebView2 runtime (already present on
current Windows).

**Everything with Aspire, SQL Server in a container (needs Docker Desktop or Podman):**

```
dotnet run --project src/VetDesk.AppHost
```

This starts SQL Server in a container (`vetdesk-sql`, data kept in the `vetdesk-sql-data` volume), runs the
migrations, adds the demo practice, then starts the browser host and, on Windows, the desktop app pointed at it. The
Aspire dashboard opens with every app's logs and traces. The container keeps running after you stop the AppHost so
the next start is quick; remove the container and volume to start again from an empty database.

**Desktop (Windows), against a running Web:** start Web first (below, or with the AppHost), then

```
dotnet run --project src/VetDesk.Desktop
```

A Debug build reads `appsettings.Debug.json`, which points the window at `http://localhost:5180`, Web's development
address. F12 opens the developer tools. If Web isn't running, the window says it can't reach the server and offers
Retry.

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

**The server.** `aspire publish` (from `src/VetDesk.AppHost`) writes `docker-compose.yaml` and `.env` for the
practice server: SQL Server Express, VetDesk.Web and Caddy for HTTPS, with only port 443 open. Fill in `.env`
(server name, allowed networks, the two SQL passwords, the backup folder) and run `docker compose up -d`. On an empty
database, the first desk to open VetDesk is asked for the first administrator (name, PIN and password). That person
then adds the other staff, each with a PIN to hand over, under Settings › Users and roles. Surnames and emails are
optional.

**Each desk.** Trust Caddy's root certificate once (the desktop app uses the Windows certificate store, as Edge
does), then run the desktop app with `VetDesk:ServerUrl` in its `appsettings.json` set to the server's address:

```json
{ "VetDesk": { "ServerUrl": "https://vetdesk.local" } }
```

That is the app's only setting. It holds no connection string, so nothing on a desk PC or laptop gives access to the
database. Password and form autofill are off in the window, so PINs are never offered for saving. Opening
`https://vetdesk.local` in a browser works too.

## Several desks on one record

Everyone at the practice can work on the same records at once, and sees the others' work as it happens:

- **Live typing.** On a consultation, and in the patient, client and clinic details forms, what one person types shows
  on everyone else's screen as they type it. A box someone is typing in shows "Heather is typing…" and is read-only
  for the others until she moves on or pauses for a few seconds; the other boxes stay free. Consultation notes save
  themselves two seconds after the last keystroke.
- **Who else is here.** Each record shows who else has it open, and who is editing.
- **Live lists.** Today, the schedule (and the free times when booking), patients, clients, billing, pharmacy and
  settings refresh as soon as another desk saves something they show.
- **Safety rules stay.** While someone completes a consultation nobody else can change its note, and once completed it
  is locked. A save based on an out-of-date copy is still refused rather than overwriting anyone's work.

It works because every desk is connected to the one VetDesk server: the drafts and the feed of changes live in that
server's memory, and Blazor sends each desk its updated screen over the SignalR connection it already uses. Running two
copies of VetDesk.Web against one database would need a shared backplane (for example Redis) before desks on different
copies could see each other.

## Backups and restore

Set the folder and time under Settings › Backup. The server checks every few minutes and takes the day's backup once
the time has passed, whether or not any desk has VetDesk open. "Back up now" takes one immediately. The history on
that page shows every run and any failure. If no folder is set, each night's run is recorded as a failure and raises
a notice, so a practice can't go without backups unnoticed.

On the Docker Compose server install the folder starts as the backup volume (`/var/opt/mssql/backup`, set by
`VetDesk__DefaultBackupFolder`), so backups run from the first night. In `.env`, point `SQL_BINDMOUNT_0` and
`SQL_INIT_BINDMOUNT_1` at the same host folder: the sql-init job hands that folder to SQL Server's user (uid 10001)
on every start, because Docker creates a missing folder owned by root and SQL Server can't write to it.

- **SQL Server:** a native `BACKUP DATABASE ... WITH CHECKSUM` to a `.bak` file. The folder is on the **database
  server's** disk (or a share its service account can write to). Restore with SQL Server Management Studio
  (Databases › Restore Database) or `RESTORE DATABASE VetDesk FROM DISK = N'...\VetDesk-....bak'
  WITH REPLACE`, with VetDesk.Web stopped (`docker compose stop web`).
- **SQLite** (development and demos only): `VACUUM INTO` a `.db` file, a consistent copy taken while the app keeps
  running. Restore by stopping VetDesk.Web and copying the backup over the database file.

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
