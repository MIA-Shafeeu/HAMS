# HAMS — CI/CD Setup Runbook

One-time steps to get from "fresh Windows Server + fresh SQL Server 2022 + IIS" to a working
push-to-deploy pipeline for the web platform (`HAMS.WebHost`). Do these roughly in order — later
steps assume earlier ones are done.

Everything here targets **`HAMS.WebHost` only**. `HAMS.Worker` (the Hangfire background dispatcher
that sends queued SMS/email notifications) is not part of this pipeline yet — see "What's not
covered" at the bottom.

---

## 1. On the Windows Server: prerequisites

### 1.1 Install the .NET 10 Hosting Bundle
You said this is downloaded but not installed. Run the installer, then **restart IIS** so it picks
up the ASP.NET Core Module (ANCM):

```powershell
net stop /y was
net start w3svc
```

(`was` = Windows Process Activation Service; stopping it also stops `w3svc`/IIS, restarting `w3svc`
brings both back up.)

Verify it registered correctly:

```powershell
& "$env:ProgramFiles\dotnet\dotnet.exe" --info
Get-Module -ListAvailable -Name WebAdministration
```

If `WebAdministration` doesn't show up, enable it via Windows Features:

```powershell
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole, IIS-WebServer, IIS-CommonHttpFeatures, IIS-ManagementConsole, IIS-ManagementScriptingTools, IIS-ASPNET45
```

`IIS-ManagementScriptingTools` is the one that actually gives you the `WebAdministration` PowerShell
module `Deploy-HAMS.ps1` depends on (`Stop-WebAppPool`, `robocopy` target resolution, etc.).

### 1.2 Create the IIS site and application pool

```powershell
Import-Module WebAdministration

New-Item -ItemType Directory -Path "C:\inetpub\wwwroot\hams" -Force

New-WebAppPool -Name "HAMS"
Set-ItemProperty "IIS:\AppPools\HAMS" -Name managedRuntimeVersion -Value ""   # "No Managed Code" — required for ASP.NET Core
Set-ItemProperty "IIS:\AppPools\HAMS" -Name startMode -Value "AlwaysRunning"

New-WebSite -Name "HAMS" -PhysicalPath "C:\inetpub\wwwroot\hams" -ApplicationPool "HAMS" -Port 8081 -IPAddress "*"
```

`-IPAddress "*"` ("All Unassigned") is required here — binding to a specific IP like `127.0.0.1`
causes IIS/HTTP.sys to reject every request with "HTTP Error 400. The request hostname is invalid."
(confirmed live on this deployment). The site is still only reachable via the Cloudflare Tunnel
forwarding to `localhost:8081`; nothing else needs to listen on `8081` from the outside.

Grant the app pool identity read/execute on the site folder (it needs write too, since ASP.NET Core
sometimes writes a `DataProtection-Keys` folder under the site root at runtime unless redirected —
simplest is to just grant Modify here rather than fight that):

```powershell
icacls "C:\inetpub\wwwroot\hams" /grant "IIS AppPool\HAMS:(OI)(CI)M" /T
```

### 1.3 SQL Server: create the database and grant the app pool login

You said Windows Authentication and a fresh SQL Server 2022 install. Run
`deploy/sql/Setup-Database.sql` against the instance:

```powershell
sqlcmd -S . -E -C -i deploy\sql\Setup-Database.sql
```

(`-S .` = local default instance. If you installed a **named** instance instead of the default,
this needs `-S .\<InstanceName>` here AND the connection string in `Deploy-HAMS.ps1` needs
`Server=.\<InstanceName>` too — check `services.msc` for a service named
`SQL Server (<InstanceName>)`; if it just says `SQL Server (MSSQLSERVER)`, you're on the default
instance and don't need to change anything.)

This script creates the `HAMS` database, creates a SQL login for the `IIS AppPool\HAMS` Windows
identity (works because SQL Server and IIS are on the same box), and grants it `db_owner` — needed
because the app applies its own EF Core migrations on startup (see §3 below), not a separate
DBA-run migration step.

---

## 2. GitHub: repository, secrets, and the self-hosted runner

### 2.1 Runner machine prerequisites: Git + the .NET SDK

The self-hosted runner is just a background process that polls GitHub and executes the workflow's
steps **locally, as if you'd typed them yourself** — so anything a step needs, the machine needs
installed first. Two things are missing on a fresh box that the Hosting Bundle (§1.1) doesn't cover:

- **Git** — `actions/checkout@v4` (the very first step of both jobs) shells out to `git` to clone
  the repo; without it on `PATH`, every run fails immediately at the checkout step, before your
  own code ever runs.
- **The .NET SDK** (not just the Hosting Bundle) — the Hosting Bundle only installs the ASP.NET
  Core **runtime** + the IIS integration (ANCM), enough to *run* an already-published app. It does
  NOT include `dotnet publish`/`dotnet build`, which need the full SDK. `Deploy-HAMS.ps1` calls
  `dotnet publish` directly on this machine, so the SDK has to be here too.

Install both (run as Administrator):

```powershell
winget install --id Git.Git -e --source winget
winget install --id Microsoft.DotNet.SDK.10 -e --source winget
```

If `winget` isn't available on this Windows Server build (`winget --version` to check — some
Windows Server images don't ship the App Installer by default), install manually instead:
- Git: download and run the installer from [git-scm.com/download/win](https://git-scm.com/download/win) (defaults are fine).
- .NET SDK: download and run the installer from [dotnet.microsoft.com/download/dotnet/10.0](https://dotnet.microsoft.com/download/dotnet/10.0) (the **SDK**, not just the runtime).

**Close and reopen any PowerShell/terminal window** after installing (PATH changes don't apply to
already-open sessions) — then confirm both are visible to a fresh session before continuing:

```powershell
git --version
dotnet --version
```

If you already registered the runner as a service before installing these, restart it afterward so
it picks up the new `PATH`:

```powershell
Restart-Service actions.runner.*
```

### 2.2 Create the repository
On [github.com/new](https://github.com/new): create a **private** repository (no README/gitignore/
license — this project already has all three). Don't push yet — finish §2.3–2.4 first so the very
first push already has the workflow wired to real secrets.

### 2.3 Repository secrets and variables
**Settings → Secrets and variables → Actions**:

| Type | Name | Value |
|---|---|---|
| Secret | `PROD_JWT_SIGNING_KEY` | a fresh random key — **deliberately different from the dev key** (which now lives only in your local user-secrets, never in git). Generate one yourself rather than reusing any value that's ever appeared in a chat transcript or committed file: `powershell -Command "[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))"` — paste the output straight into the GitHub secret field and nowhere else. |
| Secret | `PROD_MSGOWL_API_KEY` | *(after you rotate the Msgowl key on their dashboard — see §2.3.1)* |
| Secret | `PROD_MSGOWL_SENDER_ID` | your Msgowl sender id (e.g. `HESS INC`) |
| Variable | `PROD_MSGOWL_ENABLED` | `false` until the rotated key is in place, then `true` |

**Never paste a real secret value into any file that gets committed** — not even this one. Secrets
belong in exactly one place: the GitHub Secrets UI (or, for local dev, `dotnet user-secrets`).

#### 2.3.1 Rotate the Msgowl key
You confirmed the key currently sitting in `appsettings.Development.json`'s git history-to-be was
live. It's already been moved to your local `dotnet user-secrets` store (outside the repo) for
local dev, but the key **value itself** is still the old one — log into Msgowl's dashboard, revoke
that key, issue a new one, and use the new one for both:
- `PROD_MSGOWL_API_KEY` above, and
- `dotnet user-secrets set "Msgowl:ApiKey" "<new-key>"` from `src/Host/HAMS.WebHost` locally, if you
  want local dev to actually send real SMS (otherwise leave `Msgowl:Enabled=false` locally and it
  falls back to the log-only dev sender, same as today).

### 2.4 Self-hosted runner
**Settings → Actions → Runners → New self-hosted runner** (choose Windows). GitHub shows you a
`config.cmd` command with a repo-specific token baked in — run the **download** and **configure**
steps it gives you directly on the IIS box (after §2.1's Git/.NET SDK install), in whatever folder
you want the runner installed (e.g. `C:\actions-runner`), then instead of running `run.cmd`
interactively, install it as a service so it survives reboots:

```powershell
cd C:\actions-runner
.\svc install
.\svc start
```

Verify: **Settings → Actions → Runners** should show it as "Idle".

### 2.5 First push

```powershell
git remote add origin https://github.com/<you>/<repo>.git
git branch -M main
git push -u origin main
```

Watch the **Actions** tab: `build-and-test` runs on GitHub's own runner (fast, no server access
needed); `deploy` only fires on a push to `main` and runs on your self-hosted runner.

---

## 3. The first deploy — creating the real production admin account

`Deploy-HAMS.ps1` publishes and starts the site, but an **empty** database has no accounts at all.
Once the first deploy finishes and `/health` is answering, create the real System Administrator —
this is a one-time HTTP call, no server/SQL access needed:

```powershell
# From anywhere that can reach the tunnel:
Invoke-RestMethod -Method Post -Uri "https://edu.hessgroup.org/api/v1/setup/bootstrap-admin" `
    -ContentType "application/json" `
    -Body '{"username":"<your-real-admin-username>","password":"<a-real-strong-password>"}'
```

This endpoint (`SetupEndpoints`/`ISetupService`) permanently refuses once **any** System
Administrator exists — checked live against the role-assignment table, not just "does any user
row exist" (a guardian or student's first OTP/PIN login also creates a user row, and must not be
mistaken for "already bootstrapped"). You can safely leave it reachable; it's a dead end after the
first successful call, and every account after this one goes through the authenticated
`IStaffAccountService` instead (Admin → Staff Accounts & Roles in the web UI).

---

## 4. What this pipeline does on every push to `main`

1. **`build-and-test`** (GitHub-hosted `ubuntu-latest`): restores/builds/tests `HAMS.Web.slnx` — a
   solution filter that excludes `HAMS.Mobile` (its Android/iOS/MacCatalyst/Windows targets need
   MAUI workloads a plain Linux runner doesn't have; mobile CI is a separate, later decision).
2. **`deploy`** (your self-hosted runner, only after `build-and-test` passes, only on `main`):
   runs `deploy/Deploy-HAMS.ps1`, which:
   - `dotnet publish -c Release`
   - stops the `HAMS` app pool, waits for it to actually stop
   - copies the publish output into `C:\inetpub\wwwroot\hams` (not a mirror/purge copy — anything
     else already living in that folder is left alone)
   - (re)writes `appsettings.Production.json` from the GitHub secrets above — this file is never
     part of the git repo or the publish output, so it can never accidentally get overwritten with
     empty values or committed
   - starts the app pool and polls `/health` until it answers, failing the deploy loudly if it
     doesn't within 60 seconds

No manual approval gate is configured (you said direct-push is fine) — every merge to `main`
deploys automatically. If you change your mind later: **Settings → Environments → production →
required reviewers** — no workflow file changes needed.

---

## 5. What's not covered by this pass

- **`HAMS.Worker`** (the Hangfire background dispatcher — sends queued absence-notification
  SMS/email, any future recurring job) isn't deployed or running anywhere yet. Guardian OTP login
  still works fine (that path calls the SMS sender directly, synchronously, bypassing the outbox by
  design) — but a student being marked absent won't actually notify their guardian until `Worker`
  is running somewhere. Worth deciding deliberately: a second Windows Service on the same box, folded
  into `HAMS.WebHost` as an in-process Hangfire server instead, or deferred until notifications are
  actually needed. Say the word and I'll extend the pipeline to cover it.
- **`HAMS.Mobile`** has no CI at all yet (see §4) — building it would need either a self-hosted
  runner with the MAUI workloads installed, or GitHub's `windows-latest` hosted runner plus an
  explicit `dotnet workload install maui` step. Not built into this pass since you scoped this to
  "the web platform."
- **Cloudflare Tunnel config itself** — you said it's already pointed at `localhost:8081`, so
  nothing here touches `cloudflared`'s own config. Worth double-checking it's set to forward plain
  `http://localhost:8081` (not `https://`) since IIS isn't terminating TLS on this binding — TLS is
  handled entirely by Cloudflare's edge.
