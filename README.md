# utils-LAndE

> Observability - structured logging, request/response capture and audit hooks.

*(That one line is curated. Everything else on this page is measured at generation time — see the foot of the file.)*

## At a glance

| | |
|---|---|
| Current branch | `r2-dev-stable` |
| HEAD | `a4fe54d Publish on a push to r2-dev-stable at the declared version, tagged nuget/<id>/<version>: no package here declares a version, so that push publishes nothing` |
| C# files | 168 |
| Controllers / HTTP endpoints | 2 / 5 |
| SQL files / tables declared | 0 / 0 |
| Test projects | `Intellect.Erp.ErrorHandling.UnitTests`, `Intellect.Erp.Observability.IntegrationTests`, `Intellect.Erp.Observability.Testing`, `Intellect.Erp.Observability.UnitTests` |

## Where this sits relative to `r2-dev-stable`

`r2-dev-stable` is the single integration branch: **every state's code merges onto it and nowhere else**, and one codebase serves all 30 states. A state branch exists only while that state's work is in flight.

- **`r2-dev-as`** — carries no work of its own beyond `r2-dev-stable`.
- **`r2-dev-ka`** — carries no work of its own beyond `r2-dev-stable`.
- **`r2-dev-tn`** — carries no work of its own beyond `r2-dev-stable`.

Every state branch is level with stable, so **there is no state-specific code in this repo right now**.

<!-- BEGIN docs/README-notes.md (hand-written, emitted verbatim; edit THAT file, not this one) -->
## Releasing the NuGet packages

Packages go to the organisation's GitHub Packages feed (`https://nuget.pkg.github.com/Intellect-Informatics-Pvt-Ltd/index.json`)
from `.github/workflows/build.yml`, which runs `build_push_script.sh`. Owner ruling 2026-10-07: **a push to
`r2-dev-stable` publishes** - nobody has to dispatch a workflow or merge to master to release.

| Trigger | Version each package gets | Published when |
|---|---|---|
| push to `r2-dev-stable` (`VERSION_SOURCE=csproj`) | the version **its own project declares** - `<Version>`/`<PackageVersion>`/`<VersionPrefix>` in the csproj or a `Directory.Build.props` above it, read back with `dotnet msbuild <csproj> -getProperty:PackageVersion`; never a `-p:Version` override | the version is a plain `X.Y.Z` **and** strictly newer (SemVer) than every version of that package id already on the feed |
| push to `master` | latest version on the feed + 1 patch, for every package | always (unchanged) |
| `workflow_dispatch` | the `version` input, else latest + 1 patch | only with `push_packages=true` (unchanged) |

On `r2-dev-stable` everything else is **logged and skipped, never published**: a project that declares no version
(MSBuild's silent `1.0.0` default is a placeholder), a prerelease or non-`X.Y.Z` version (`1.0.0-preview.1`), and a
version equal to or older than the feed's ("already published / not newer"). So an ordinary push that changes no
version publishes nothing, and `--skip-duplicate` stays on as the backstop for a race.

**No package in this repo declares a version, so a push to `r2-dev-stable` publishes nothing here** - every
project below is logged `SKIP ... declares no version`. That is deliberate (owner ruling 2026-10-08: the
Observability/ErrorHandling pins stay as they are - the estate pins `1.0.10`; the feed also holds `1.0.11` of some ids,
from earlier master pushes). Releases still come from a master push or `workflow_dispatch`, which stamp their own version.

**To release from `r2-dev-stable`:** declare a version above the feed's highest - in `Directory.Build.props`, one line for
all of them: they reference one another as `ProjectReference`s, so a nuspec depends on its siblings at *their* version and
raising one alone can leave it depending on an older or undeclared sibling - commit, push. Check the feed's highest first
(`1.0.11` is the highest the estate has restored); a declaration equal to or below it is skipped.

Packages in this repo (the projects the script selects):

| Package id | Declared in | Declared now | On an `r2-dev-stable` push |
|---|---|---|---|
| `Intellect.Erp.AllObservabilityAndTraceabilitys` | nowhere (no `<Version>`) | - | skipped until a version is declared |
| `Intellect.Erp.ErrorHandling` | nowhere (no `<Version>`) | - | skipped until a version is declared |
| `Intellect.Erp.Observability.Abstractions` | nowhere (no `<Version>`) | - | skipped until a version is declared |
| `Intellect.Erp.Observability.AspNetCore` | nowhere (no `<Version>`) | - | skipped until a version is declared |
| `Intellect.Erp.Observability.AuditHooks` | nowhere (no `<Version>`) | - | skipped until a version is declared |
| `Intellect.Erp.Observability.Core` | nowhere (no `<Version>`) | - | skipped until a version is declared |
| `Intellect.Erp.Observability.Integrations.Messaging` | nowhere (no `<Version>`) | - | skipped until a version is declared |
| `Intellect.Erp.Observability.Integrations.Traceability` | nowhere (no `<Version>`) | - | skipped until a version is declared |
| `Intellect.Erp.Observability.Log4NetBridge` | nowhere (no `<Version>`) | - | skipped until a version is declared |
| `Intellect.Erp.Observability.Propagation` | nowhere (no `<Version>`) | - | skipped until a version is declared |
| `Intellect.Erp.RequestResponseLogging` | nowhere (no `<Version>`) | - | skipped until a version is declared |

### Evidence that a version was published

For every package the run actually pushed (the feed accepted it; a duplicate answer is not counted), the workflow's
`tag` job pushes a lightweight tag `nuget/<PackageId>/<Version>` on the commit it was built from, using the run's own
`GITHUB_TOKEN` (`permissions: contents: write` on that job only). An existing tag is left alone. Anyone with clone
access confirms a publish - no Actions or Packages API access needed - with:

```bash
git ls-remote --tags origin 'nuget/*'
```

A raised version with no tag a few minutes after the push means the run failed or skipped it, and only someone who can
read the Actions log can say which. Likely causes, in the order to check them: the feed already holds that version or a
newer one (logged `SKIP ... not newer`); the version is a prerelease (`SKIP ... prerelease`); `GH_TOKEN_NUGET` (or the
fallback `github.token`) cannot read the package's versions (HTTP 401/403 from `api.github.com/orgs/.../packages`) or
cannot push; restore failed against the private feed; `dotnet pack` failed (for example NU5104, a release package
depending on a prerelease sibling); or the `tag` job was refused a tag push (a tag ruleset, or an organisation policy
that caps `GITHUB_TOKEN` at read).

### Checking the r2-dev-stable mode locally

There is no script-test project in this repo, so this is the manual check. It packs at the declared versions with
`PUSH_PACKAGES=false` against a **stubbed feed** - nothing is pushed and no token is used. `published_versions` is the
script's feed lookup (the raw list `get_latest_version` reduces); the script only calls `main` when executed, so it
can be sourced and that one function replaced:

```bash
cd utils-LAndE
printf '%s\n' 'Intellect.Erp.ErrorHandling 1.0.11' > /tmp/feed.txt     # "<PackageId> <Version>" lines = what the feed holds
bash -c 'set -euo pipefail
  export VERSION_SOURCE=csproj PUSH_PACKAGES=false GITHUB_TOKEN= GITHUB_PACKAGES_PAT= NUGET_API_KEY= \
         PACKAGE_OUTPUT_DIR=/tmp/nuget-dryrun PUBLISHED_MANIFEST=/tmp/nuget-dryrun.manifest
  source ./build_push_script.sh
  published_versions() { awk -v id="$1" '"'"'$1 == id { print $2 }'"'"' /tmp/feed.txt; }
  main' 2>&1 | grep -E '^(PUBLISH|SKIP|Nothing)'
```

Expected with that stub (or an empty one): eleven `SKIP ... declares no version` lines and `Nothing newer than the feed was declared`. Declare a version in a scratch copy and it must print `PUBLISH` when it is above the stub's version and `SKIP ... already published / not newer` when it is not; a `-preview` version must print `SKIP ... prerelease`. Keep the token variables empty: with a token set, the script writes credentials into
`NuGet.Config`.
<!-- END docs/README-notes.md -->

## Design documents

Hand-written design records in `docs/` — the WHY behind the changes the change log
below only dates. Read these before modifying the subsystems they cover.

- [Intellect.Erp.Observability — Developer User Guide](docs/Developer_User_Guide.md)
- [Intellect.Erp.Traceability — Developer User Guide](docs/Traceability_Developer_User_Guide.md)
- [Adoption Guide](docs/adoption-guide.md)
- [utils-LAndE — One-page adoption quick reference](docs/adoption-quickref.md)
- [ELK Field Reference — Canonical Field Set (Schema v1)](docs/elk-field-reference.md)
- [Error Catalog Authoring Guide](docs/error-catalog-authoring.md)
- [Migration from log4net to Serilog](docs/migration-from-log4net.md)

## Change log — measured from git, newest first

Every entry below is read from this repo's own commits: **what** changed (the subject), **why** (the commit body's own first paragraph), **which files**, and the register / state-customization **ids** it carries. When a maintenance question arrives as a TD-xx or a state id (a two-letter state prefix and four digits: KA0005, MH0001, GJ0012...), the index maps it straight to the commits, and each commit to its files. Commits carrying a state id are indexed however old they are; everything else is the most recent 40.

### Register & customization id index

| Id | Commits |
|---|---|
| **TD-39** | `f2cf8b1`, `48280f0`, `32c2aa1` |
| **TD-125** | `32c2aa1` |
| **TD-127** | `48280f0`, `318184e` |
| **TD-134** | `94d2e36` |
| **TD-153** | `f2cf8b1` |
| **TD-154** | `f2cf8b1` |
| **TD-155** | `560cd67`, `f2cf8b1` |
| **TD-156** | `560cd67`, `f2cf8b1` |
| **TD-157** | `560cd67` |
| **TD-158** | `560cd67` |
| **TD-163** | `5f9dbfd` |

### Commits

**`a4fe54d`** 2026-10-07 — Publish on a push to r2-dev-stable at the declared version, tagged nuget/<id>/<version>: no package here declares a version, so that push publishes nothing

> Versions: none of the 11 packable projects (Intellect.Erp.ErrorHandling, RequestResponseLogging, AllObservabilityAndTraceabilitys and the eight Intellect.Erp.Observability.*) declares <Version>/<PackageVersion>/<VersionPrefix>, in its csproj or in Directory.Build.props, so on r2-dev-stable every one is logged `SKIP ... declares no version` and none is packed or pushed. Owner ruling 2026-10-08: the LAndE pins stay as they are (the estate pins 1.0.10; the feed also holds 1.0.11 of some ids), so no version is declared and nothing is released. tests.yml's note that build.yml must never run on r2-dev-stable is replaced by what is true now.

Files: `.github/workflows/build.yml`, `.github/workflows/tests.yml`, `build_push_script.sh`, `docs/README-notes.md`

**`ffd4178`** 2026-10-07 — Observability gains SensitiveText (mask addresses, logins and connection values, an exception's client-safe text, the db# fingerprint), the global exception handler returns exception details only in Development - not merely outside an environment named Production, which the estate's state-named environments never were - and masks every message it returns or logs, the redaction engine masks MySQL 'user'@'host', connection-string values, IPv4 addresses and URL credentials, and the request/response logger always masks Aadhaar, mobile, OTP, PIN, security-answer and connection-string fields whatever a service configures.

Files: `src/Intellect.Erp.Observability.Abstractions/SensitiveText.cs`, `src/Intellect.Erp.Observability.AspNetCore/Middleware/GlobalExceptionMiddleware.cs`, `src/Intellect.Erp.Observability.Core/DefaultRedactionEngine.cs`, `src/Intellect.Erp.RequestResponseLogging/Helpers/PayloadMaskingHelper.cs`, `src/Intellect.Erp.RequestResponseLogging/Intellect.Erp.RequestResponseLogging.csproj`, `tests/Intellect.Erp.Observability.IntegrationTests/GlobalExceptionMiddlewareIntegrationTests.cs`, `tests/Intellect.Erp.Observability.UnitTests/Abstractions/SensitiveTextTests.cs`, `tests/Intellect.Erp.Observability.UnitTests/Intellect.Erp.Observability.UnitTests.csproj`, `tests/Intellect.Erp.Observability.UnitTests/RequestResponseLogging/PayloadMaskingTests.cs`

**`2e850d7`** 2026-09-05 — README: regenerated, and the six utils repos finally have a purpose line

> Regenerated after this week: the new controllers, tables and routes now appear in each module page — WorkItemInbox in configurationsAPI, the two oracle tables in FAS, the four new client surfaces in ERPClient.

Files: `README.md`

**`5f9dbfd`** 2026-08-22 — docs: regenerated after the TN gap closure (TD-163) · **TD-163**

Files: `README.md`

**`c27c8a0`** 2026-08-21 — docs: module README gains the analytics-surface contract (generated)

Files: `README.md`

**`560cd67`** 2026-08-21 — Close TD-155 and TD-156 - and both were wrong about their own severity · **TD-155** **TD-156** **TD-157** **TD-158**

> TD-155 named three advisories. Auditing all 33 repos found SEVENTEEN vulnerable package versions, one of them CRITICAL. Ten are now fixed, and the seven that remain are not fixable by a version bump, so they are recorded individually rather than left as a warning nobody reads.

Files: `README.md`

**`f2cf8b1`** 2026-08-21 — Converge on one MySQL driver, and drop a query that could never run · **TD-153** **TD-154** **TD-155** **TD-156** **TD-39**

> ONE DRIVER. The estate carried two ADO.NET drivers for the same database, often in the same process: MySql.Data at six versions and MySqlConnector at four, with 17 repos referencing both. Two drivers means two connection pools and two sets of semantics behind one connection string. Everything is now MySqlConnector 2.5.0 - one version, no MySql.Data anywhere.

Files: `README.md`

**`93ae793`** 2026-08-21 — Move to .NET 10, and pin the SDK that builds it

> TARGET FRAMEWORK. Every project moves net8.0 -> net10.0. The six utils repos that PUBLISH packages multi-target net8.0;net10.0 instead, so one package id at one version carries lib/net8.0 and lib/net10.0 and a consumer still on .NET 8 keeps resolving. Renaming the package for the new framework was considered and rejected: two ids for the same library means a diamond dependency can pull both, and two copies of the same types with different identities is a worse failure than the one it avoids.

Files: `.github/workflows/build.yml`, `.github/workflows/tests.yml`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `src/Intellect.Erp.AllObservabilityAndTraceabilitys/Intellect.Erp.AllObservabilityAndTraceabilitys.csproj`, `src/Intellect.Erp.Observability.AuditHooks/Intellect.Erp.Observability.AuditHooks.csproj`, `src/Intellect.Erp.Observability.Core/Intellect.Erp.Observability.Core.csproj`, `src/Intellect.Erp.RequestResponseLogging/Intellect.Erp.RequestResponseLogging.csproj`

**`3d2f451`** 2026-08-21 — docs: refresh the generated change log after the trailer removal

> The Co-Authored-By trailer was stripped from this repo's commits, which changed their SHAs. This README's change log is read from git, so it is regenerated to quote hashes that still resolve.

Files: `README.md`

**`aefb748`** 2026-08-21 — docs: refresh the generated change log after the trailer removal

> The Co-Authored-By trailer was stripped from this repo's commits on r2-dev-stable and the seven state branches, which changed their SHAs. This README's change log is read from git, so it is regenerated to quote hashes that still resolve.

Files: `README.md`

**`48280f0`** 2026-08-21 — TD-39 closed: publish on the ref, not on "was not a workflow_dispatch" · **TD-127** **TD-39**

> build.yml set PUSH_PACKAGES=true for every event that was not a workflow_dispatch. That is safe only while `on: push:` lists master alone - so adding r2-dev-stable or pull_request to the trigger block, which is the one change everyone wants, would have published a NuGet package on every dev push. The register recorded that as "do not widen this", which left the trap in place rather than removing it.

Files: `.github/workflows/build.yml`

**`94d2e36`** 2026-08-20 — docs: FAS voucher-integrity section + state-appendix convention in the generated README · **TD-134**

> Every FAS-connected module's README now carries the voucher-integrity fixes (TD-134/135/139, pre-posting correction), the governing switches with defaults and implications, and the reconciliation flow - Dev/DevOps read it in the module they work in, not only in l3_FAS. Connection is MEASURED (git grep for the FAS/VoucherProcessing surface), never curated. State branches append below the STATE APPENDIX marker, never edit the generated body, so context and history survive the merge back onto r2-dev-stable.

Files: `README.md`

**`318184e`** 2026-08-19 — ci: park test workflows on manual trigger until feed auth is proven (TD-127) · **TD-127**

> The suites were switched on estate-wide and then failed at restore with 401 against the private GitHub Packages feed. Auth was added and hardened, but it could not be confirmed working from this side - the Actions logs are not readable here - so three blind fixes in a row is where this stops.

Files: `.github/workflows/tests.yml`

**`e9a0c54`** 2026-08-19 — fix(ci): make feed authentication tolerant so it cannot fail the job

> A repo with no root NuGet.Config (l3_SHG) or one without a 'github' source made 'dotnet nuget update source' error and took the whole job down. Both are now a skip-with-notice. If the private feed really was needed, restore still reports the honest 401 rather than a confusing failure in the auth step.

Files: `.github/workflows/tests.yml`

**`d1667a4`** 2026-08-19 — fix(ci): authenticate the private GitHub Packages feed before restore

> Every tests.yml did a bare 'dotnet restore', which returns 401 Unauthorized: the Intellect.* packages live on the org's PRIVATE GitHub Packages feed (NuGet.Config -> source 'github') and credentials are deliberately not committed. build.yml already handled this via configure_github_packages_source() in build_push_script.sh; the test workflows never did, so they failed at restore before running one test.

Files: `.github/workflows/tests.yml`

**`32c2aa1`** 2026-08-19 — ci(TD-125): add publish-free tests.yml running on r2-dev-stable · **TD-125** **TD-39**

> This repo had no test workflow: build.yml only packs and publishes NuGet packages, so its suite had never run in CI. Cloned from the estate reference (l3_DMS/.github/workflows/tests.yml) - restore, build, test, upload .trx.

Files: `.github/workflows/tests.yml`

**`a196b49`** 2026-08-18 — docs: generated module README

> Written by build/generate-module-readmes.py in the platform repo. Every number is measured at generation time - tables from this repo's own db/**.sql, endpoints from its controllers, test projects from its csproj files, and the state delta from git log r2-dev-stable..r2-dev-XX here.

Files: `README.md`

**`7d45e08`** 2026-08-07 — Standardize NuGet package workflow and authentication

Files: `.github/workflows/build.yml`, `NuGet.Config`, `build_push_script.sh`

**`2d86bdc`** 2026-06-03 — changes for  creating RequestResposeLogging

Files: `Directory.Packages.props`, `Intellect.Erp.Observability.sln`, `NuGet.Config`, `src/Intellect.Erp.Observability.Abstractions/IAppLogger.cs`, `src/Intellect.Erp.Observability.Testing/FakeAppLogger.cs`, `src/Intellect.Erp.RequestResponseLogging/Constants/LoggingConstants.cs`, `src/Intellect.Erp.RequestResponseLogging/Exceptions/RequestBodyTooLargeException.cs`, `src/Intellect.Erp.RequestResponseLogging/Extensions/ApplicationBuilderExtensions.cs`, `src/Intellect.Erp.RequestResponseLogging/Extensions/ServiceCollectionExtensions.cs`, `src/Intellect.Erp.RequestResponseLogging/Helpers/EnvironmentValidator.cs` — and 13 more

**`c56168a`** 2026-05-12 — update package

Files: `NuGet.Config`, `build_push_script.sh`

**`8d845dd`** 2026-05-12 — update configs

Files: `build_push_script.sh`

**`7ff5540`** 2026-05-12 — update configs

Files: `build_push_script.sh`

**`d248ac2`** 2026-05-12 — update configs

Files: `build_push_script.sh`, `src/Intellect.Erp.Observability.Testing/FakeAppLogger.cs`

**`5b65481`** 2026-05-12 — update configs

Files: `.github/workflows/build.yml`, `NuGet.Config`, `build_push_script.sh`

**`1875c70`** 2026-05-12 — Changes for updating the NuGet Package Credentials

Files: `NuGet.Config`

**`2be4e88`** 2026-05-12 — Changes for build Issue

Files: `.github/workflows/build.yml`, `Intellect.Erp.Observability.sln`, `NuGet.Config`

**`f31e6e7`** 2026-05-12 — Changes for consolidating the packages

Files: `Directory.Build.props`, `Directory.Packages.props`, `Intellect.Erp.Observability.sln`, `NuGet.Config`, `samples/SampleHost/Properties/launchSettings.json`, `src/Intellect.Erp.AllObservabilityAndTraceabilitys/Intellect.Erp.AllObservabilityAndTraceabilitys.csproj`

**`b911149`** 2026-05-11 — Revert "Changes for Making Single NuGet Package for all  Observability And Traceability's"

> This reverts commit 4c5a08575461affcd44a9db0032b9f15e2fdbf2a.

Files: `AllObservabilityAndTraceabilitys/Intellect.Erp.AllObservabilityAndTraceabilitys.csproj`, `Directory.Packages.props`, `NuGet.Config`

**`4c5a085`** 2026-05-11 — Changes for Making Single NuGet Package for all  Observability And Traceability's

Files: `AllObservabilityAndTraceabilitys/Intellect.Erp.AllObservabilityAndTraceabilitys.csproj`, `Directory.Packages.props`, `NuGet.Config`

**`7ddebe5`** 2026-05-11 — update workflow

Files: `.github/workflows/build.yml`, `build_push_script.sh`

**`aac189f`** 2026-05-11 — remove file conflict for script

Files: `.github/workflows/build.yml`, `build_push_script.sh`

**`4622424`** 2026-05-11 — remove file conflict for script

Files: `.github/workflows/build.yml`, `build_push_script.sh`

**`6002b06`** 2026-05-11 — add script for nuget upload

Files: `build_push_script.sh`

**`ae80d38`** 2026-05-11 — created the build.yml under .github/workflows

Files: `.github/workflows/build.yml`

**`d331ee4`** 2026-04-24 — Dev Guide PDF

Files: `docs/Developer_User_Guide.pdf`

**`93851ae`** 2026-04-24 — Observability - Logging and Error Handling

Files: `.gitignore`, `.kiro/specs/utils-lande-observability/.config.kiro`, `.kiro/specs/utils-lande-observability/design.md`, `.kiro/specs/utils-lande-observability/requirements.md`, `.kiro/specs/utils-lande-observability/tasks.md`, `Directory.Build.props`, `Directory.Packages.props`, `Intellect.Erp.Observability.sln`, `NuGet.Config`, `README.md` — and 177 more

**`b5a93b6`** 2026-04-23 — Initial commit

Files: `.gitignore`, `README.md`

## Build, run and test it

Written for two readers: the DevOps engineer who has to produce a build of this repo, and the tester who has to exercise it. Everything below is measured from this checkout (port, profile, test projects, medium membership); the commands are the estate's, not this repo's own.

### 1. Build and unit-test on any machine

The SDK is pinned in the platform repo's `global.json`; the private NuGet feed and the token contract are checked by `ops/l2r2 doctor` and `ops/l2r2 env check` there. Do those first on a new machine - a feed failure reads as a compile error otherwise.

```bash
git clone <this repo> && cd utils-LAndE
git checkout r2-dev-stable
dotnet restore Intellect.Erp.Observability.sln
dotnet build Intellect.Erp.Observability.sln -c Release --no-restore
dotnet test tests/Intellect.Erp.ErrorHandling.UnitTests/Intellect.Erp.ErrorHandling.UnitTests.csproj -c Release --no-build --logger "trx;LogFilePrefix=Intellect.Erp.ErrorHandling.UnitTests" --results-directory artifacts/test-results
dotnet test tests/Intellect.Erp.Observability.IntegrationTests/Intellect.Erp.Observability.IntegrationTests.csproj -c Release --no-build --logger "trx;LogFilePrefix=Intellect.Erp.Observability.IntegrationTests" --results-directory artifacts/test-results
dotnet test tests/Intellect.Erp.Observability.UnitTests/Intellect.Erp.Observability.UnitTests.csproj -c Release --no-build --logger "trx;LogFilePrefix=Intellect.Erp.Observability.UnitTests" --results-directory artifacts/test-results
```

Each test project is run **by its csproj, never through the solution**: a solution can leave a test project unbuilt (its configuration rows carry no `Build.0`) and report green having run nothing - that hid 86 failures in `l3_auditProcessing`. `LogFilePrefix` writes one trx per target framework; a fixed `LogFileName` lets the second framework of a multi-targeted project overwrite the first.

The verdict is the **failure set against the recorded baseline**, not a count and not rc=0 - some suites carry known pre-existing failures that are recorded rather than hidden:

```bash
# in l2r2-platform-build
ops/l2r2 test run --repo utils-LAndE                 # run it, print the result
ops/l2r2 test baseline compare --repo utils-LAndE    # diff the failure SET against the baseline
ops/l2r2 test baseline show --repo utils-LAndE       # what is recorded, and when
```

A test that fails here and is NOT in the baseline is a regression. A test in the baseline that now passes is progress - re-record it (`baseline record --apply`) so the next person does not have to rediscover it.

**Where this module stands right now** - every failing test by name, new versus known, what each open pull request into `r2-dev-stable` does to the suite, and the trend since August - is `docs/testing/results/utils-LAndE.md` in the platform repo, re-measured by `build/test-estate.py run --prs`.

### 2. Where it runs

`utils-LAndE` is a **library**, consumed by the service modules as a package from the private feed. It has no port, no container, no systemd unit and no place of its own on the offline medium: it ships inside every service that references it. To see it running, bring up a service that uses it (section 5e.1b of the platform README) and exercise that service.

### 3. The database

The database comes from the platform repo, not from here. This repo's own `.sql` files describe its tables (0 declared); the ONE schema every state runs is `db/stable_baseline_ddl.sql`, and a module's `CREATE TABLE IF NOT EXISTS` never runs against it because the table is already there.

```bash
# in l2r2-platform-build
ops/l2r2 db baseline --database <empty_database> --apply   # imposes db/stable_baseline_ddl.sql, counted
mysql -u root -p -N -e "SELECT COUNT(*) FROM information_schema.tables \
  WHERE table_schema='<db>' AND table_type='BASE TABLE';"    # verify by counting, never by rc
```

It **refuses a non-empty schema** by design. Apply it in a Linux container with `lower_case_table_names=0`: a Mac cannot see the case defects that bite on a server (platform README §4.8b).

### 4. Before you push

```bash
# in l2r2-platform-build - the same guards CI runs, in the same order
ops/l2r2 ci guards            # static: DDL conventions, baseline/module agreement, secrets, READMEs
ops/l2r2 ci schema            # needs a MySQL you can write to: applies the baseline into a throwaway schema
python3 build/config-hygiene.py scan --repos utils-LAndE --branch r2-dev-stable   # no credential in any appsettings
```

A secret committed here does not only fail CI: the offline media builder **refuses the payload** (`epacs-media`, exit 3, naming file and key), so a medium cannot be cut until it is removed.

## State READMEs — append, never fork

This file is generated ON `r2-dev-stable` and flows to every state branch through the sync merges, so state branches keep the full base context and history. A state branch that needs its own notes APPENDS a section **below this line** — never edits the generated body above — so the note survives regeneration and merges back cleanly when the state's work lands on stable:

```markdown
<!-- STATE APPENDIX (r2-dev-XX) — keep everything state-specific below this marker -->
```

---

*Generated by `build/generate-module-readmes.py` in the platform repo. Do not hand-edit: the next run overwrites it. Numbers above were measured when it ran, so re-run it after a state branch moves.*
