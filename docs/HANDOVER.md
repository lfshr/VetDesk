# VetDesk handover

Context for picking up VetDesk in a new Claude conversation or Project. Written 10 October 2026, at the end of a
Claude Code session in `C:\repos\VetDesk` on branch `claude/build-vetdesk-app-pcmhuy`.

## What VetDesk is

Practice management for a single-site UK veterinary clinic: booking and check-in, clinical records, consultations,
dispensing and stock, controlled-drug register with witnessing, invoicing with VAT, cash-up, user roles, nightly
backups. .NET 10, EF Core 10, Blazor screens, SQL Server (SQLite for tests and single-PC installs).

| Project | Role |
| --- | --- |
| `src/VetDesk.Domain` | Entities and their rules (VAT, PIN lock-out, locking a completed consultation). |
| `src/VetDesk.Application` | One MediatR command or query per use case, each returning a `Result` (from `src/SimpleResult`). Permission checks live here. |
| `src/VetDesk.Infrastructure` | EF Core `VetDeskDbContext`, SQL Server migrations, password/PIN hashing (PBKDF2), backups, demo data seeder. |
| `src/VetDesk.UI` | All screens, dialogs and the stylesheet (Razor class library). |
| `src/VetDesk.Desktop` | WPF window hosting `VetDesk.UI` in a BlazorWebView. Each PC connects to SQL Server directly. |
| `src/VetDesk.Web` | The same screens as Blazor Server (interactive server, no prerendering). Only the server touches the database. |
| `src/VetDesk.AppHost` | .NET Aspire 13.6 host for development: SQL Server in a container, Web and Desktop wired to it. |
| `src/VetDesk.ServiceDefaults` | Aspire telemetry and health checks, used by Web. |
| `tests/*` | NUnit + Shouldly. 42 domain tests, 60 handler tests (handlers run against in-memory SQLite). All passing. |

Screens call the Application layer in-process via `Backend.Send(...)`, with a fresh DI scope per request.
`UserSession` is scoped: one per desktop window, or one per browser tab (Blazor circuit) in Web.

## The people

The practice's staff, seeded as demo data and shown by **first name only** (no "Dr" or "Nurse" anywhere):

| Staff | Role | Demo PIN | Demo password |
| --- | --- | --- | --- |
| Douglas, Heather, Jen | Vet and administrator | `1234` | `vetdesk-demo` |
| Janet, Lesley, Lorna | Nurse | `1234` | none |
| Liam | Practice administrator (the owner of this project) | `1234` | `vetdesk-demo` |

## Decisions already made (don't reopen without asking)

1. **Sign-in is name tile + 4-digit PIN.** No email/password sign-in. The same PIN unlocks a locked desk and
   witnesses controlled drugs. Five wrong PINs in a row block that person for five minutes
   (`StaffMember.MaxPinAttempts`, `PinLockout`).
2. **Administrators also have a password**, asked for before Users and settings open (`UnlockSettingsCommand`).
   A PIN alone never grants `Permission.ManageSettings`; it is removed in `StaffRules.SignedInAsync` unless the
   password was confirmed. Settings close again when the desk locks.
3. **"No login at all" was rejected:** clinical records, the controlled-drug register (named supplier and witness)
   and UK data protection all need to know who did what.
4. **Surname and email are optional** for staff. `ShortName` is the first name and is what records store.
5. **No Active Directory, no Windows Pro/Enterprise.** Cost rules them out. So no BitLocker on Home machines unless
   Windows 11 "Device encryption" is available, and no domain Windows authentication between PCs.
6. **Direction for production: run VetDesk on one server, browsers at every desk.** Because without a domain each
   desktop install would need a SQL login with its password in plain text in `appsettings.json`, and a stolen
   laptop would expose it. With the browser model, laptops hold nothing.
7. **Preferred way to deploy: containers**, generated from the Aspire AppHost (see the plan below).

## Current state of the code

Everything below is committed on `claude/build-vetdesk-app-pcmhuy` (commit `f728f63` "Changed names and seed data",
merged in `74feecb`):

1. *EF Core tools:* `Microsoft.EntityFrameworkCore.Tools` in Infrastructure (Package Manager Console cmdlets).
2. *SQL Express:* Debug desktop and Web configs point at `localhost\SQLEXPRESS` (`VetDeskDev`, `VetDeskWeb`).
3. *Aspire:* `VetDesk.AppHost`, `VetDesk.ServiceDefaults`, Desktop reads environment variables, Desktop targets
   `net10.0-windows10.0.17763.0` (needed by WebView2; plain `net10.0-windows` crashed at start-up).
4. *PIN sign-in:* migration `StaffPins` (PIN columns, lock-out columns, optional email), new commands
   `SetUpPinCommand`, `UnlockSettingsCommand`, `ChangePinCommand`, query `GetSignInTilesQuery`, screens
   (`SignInScreen`, `LockScreen`, `PinPad`, `UnlockSettingsSection`, `StaffDialog`, witness picker), new seeded staff,
   first-name-only display, tests.

Accounts from before PINs (a password but no PIN) tap their name, enter the password once and choose a PIN.

## Running it in development

- `dotnet run --project src/VetDesk.AppHost` (or start the AppHost in Visual Studio). Needs Docker Desktop.
  SQL Server 2022 runs in container `vetdesk-sql` with data in volume `vetdesk-sql-data`; the generated `sa`
  password is in the AppHost's user secrets. Web runs migrations and seeds the demo practice, Desktop waits for Web.
- The AppHost uses `ContainerLifetime.Session` (changed by Liam), so the container goes when Aspire stops, **but the
  volume keeps the data.** To reset to fresh demo data: `docker volume rm vetdesk-sql-data`. The comment above that
  code in `AppHost.cs` still says "persistent lifetime" and needs updating.
- Demo data is only added to a database with no staff.
- `dotnet test VetDesk.sln` runs everything.
- Migrations: `dotnet ef migrations add <Name> --project src/VetDesk.Infrastructure --startup-project src/VetDesk.Infrastructure`
  (the design-time factory lives in Infrastructure). In Visual Studio's Package Manager Console, pass
  `-StartupProject VetDesk.Infrastructure`.

### Gotchas met so far

- After changing Desktop's target framework, Visual Studio kept restoring the old one ("assets file doesn't have a
  target for net10.0-windows10.0.17763.0"). Fix: unload and reload the project, or close VS and delete
  `src\VetDesk.Desktop\obj` and `.vs`.
- While the app runs from Visual Studio, command-line builds of Web/Desktop fail with locked files. Not a code error.
- Web has prerendering off, so `curl` can't see screen content; test the UI in a browser.
- The Settings page must re-render on `UserSession.Changed` (fixed) or the password unlock looks like it did nothing.
- A browser password manager may pop over the settings password field and clear it.

## The plan: run VetDesk on the practice server in containers

**Target:** one machine runs two containers, VetDesk.Web and SQL Server Express, from a Compose file generated by
Aspire. Every desk and laptop opens `https://<server>:5001` in a browser. SQL Server has no published port.

### Open decisions (ask Liam)

- **A. Which machine runs Docker?** Recommended: a small Linux PC (Docker Engine, starts unattended, free).
  Alternative: Docker Desktop on the existing Windows PC (free for a business this size, but it only runs once
  someone signs in to Windows and can be left stopped by updates).
- **B. Keep the WPF desktop app?** Recommendation: drop it for production, so no PC stores a database login.
- **C. HTTPS:** certificate mounted into the Web container, or a Caddy reverse-proxy container in front.
- **D. Restrict by network range?** An allow-list of the practice subnet (cheap, recommended).

### Phase 1 — Production readiness in code

1. Add Aspire Docker Compose support to the AppHost (`AddDockerComposeEnvironment`) so `aspire publish` produces
   `docker-compose.yaml` and `.env`. Keep the development setup working.
2. SQL Server container: `MSSQL_PID=Express` (the default Developer edition is not licensed for production; Express
   caps each database at 10 GB). Data volume, no host port, `restart: unless-stopped`.
3. Web container: image built by the .NET SDK (no Dockerfile); `ASPNETCORE_ENVIRONMENT=Production`;
   `DatabaseProvider=SqlServer`, `SeedDemoData=false`; a dedicated SQL login with the least rights it needs (not
   `sa`), its password only in the server's `.env`.
4. Persist ASP.NET Data Protection keys to a volume, otherwise every restart breaks open forms.
5. `appsettings.Production.json`: `AllowedHosts` set to the server name; HSTS (already in `Program.cs`) on.
6. Logging to the container's output (`docker logs`), kept by the Docker log driver with rotation.
7. Backups: SQL Server's `BACKUP` writes inside the SQL container, so mount a backup volume there and set
   Settings › Backup to that path. Document copying the files off the machine nightly.
8. Health endpoints stay Development-only; give Compose its own health check if needed.

### Phase 2 — Harden the PIN sign-in for the network

9. Practice-wide guessing limit on top of the per-person lock-out: for example 20 wrong PINs in 10 minutes from one
   IP blocks that address for 15 minutes, logged.
10. Optional subnet allow-list (decision D), set in config.
11. Check auto-lock (`ClinicSettings.AutoLockMinutes`, default 10) behaves with several tabs on one device.
12. Decide what happens on a server restart: today every tab is signed out (the session lives in the Blazor
    circuit). Acceptable for a PIN sign-in; note it for staff.

### Phase 3 — Server and desks

13. Install Docker on the chosen machine, copy the published Compose files and `.env`, `docker compose up -d`.
14. Certificate for the server name (decision C), trusted once on each desk and laptop; note its expiry date.
15. Firewall: only the HTTPS port open, private network only.
16. Desks and laptops: browser shortcut (Edge "Install this site as an app"), browser told not to save the PIN.
17. Remove the desktop app, connection strings and any SQLite files from laptops. This is what removes the
    stolen-laptop risk.
18. Physical security for the server: strong Windows/Linux password, auto-lock, lockable room or cabinet.

### Phase 4 — Verify and document

19. Test the Compose setup locally: sign-in, lock and unlock, a controlled-drug witness, a backup, a container
    restart with tabs open.
20. From a second device: the sign-in page loads; SQL Server is unreachable; the guessing limit triggers.
21. Replace the README's "Installing at the practice" section with the container install guide, plus how to update
    (publish a new image, `docker compose up -d`) and how to restore a backup.

## Explaining PIN sign-in to staff

Key points already agreed with Liam: records must show who did what; controlled drugs need a named supplier and a
named witness (the witness for every entry is the practice's procedure, not a legal requirement for every entry
type); client data must be protected; the record also protects the staff member. It is four taps. PINs must not be
shared; Liam, Douglas, Heather or Jen can reset one.
