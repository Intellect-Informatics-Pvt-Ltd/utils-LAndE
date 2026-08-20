# utils-LAndE

> No curated summary for this repo yet. What it does is best read from its 2 controller file(s); add a line to `PURPOSE` in `build/generate-module-readmes.py` rather than editing this file.

## At a glance

| | |
|---|---|
| Current branch | `r2-dev-stable` |
| HEAD | `24fb92e ci: park test workflows on manual trigger until feed auth is proven (TD-127)` |
| C# files | 165 |
| Controllers / HTTP endpoints | 2 / 5 |
| SQL files / tables declared | 0 / 0 |
| Test projects | `Intellect.Erp.ErrorHandling.UnitTests`, `Intellect.Erp.Observability.IntegrationTests`, `Intellect.Erp.Observability.Testing`, `Intellect.Erp.Observability.UnitTests` |

## Where this sits relative to `r2-dev-stable`

`r2-dev-stable` is the single integration branch: **every state's code merges onto it and nowhere else**, and one codebase serves all 30 states. A state branch exists only while that state's work is in flight.

- **`r2-dev-as`** — carries no work of its own beyond `r2-dev-stable`.
- **`r2-dev-ka`** — carries no work of its own beyond `r2-dev-stable`.
- **`r2-dev-tn`** — carries no work of its own beyond `r2-dev-stable`.

Every state branch is level with stable, so **there is no state-specific code in this repo right now**.

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

Every entry below is read from this repo's own commits: **what** changed (the subject), **why** (the commit body's own first paragraph), **which files**, and the register / state-customization **ids** it carries. When a maintenance question arrives as a TD-xx or a state id (KA/AS/TN/WBxxxx), the index maps it straight to the commits, and each commit to its files.

### Register & customization id index

| Id | Commits |
|---|---|
| **TD-39** | `3ff4c5b` |
| **TD-125** | `3ff4c5b` |
| **TD-127** | `24fb92e` |

### Commits

**`24fb92e`** 2026-08-19 — ci: park test workflows on manual trigger until feed auth is proven (TD-127) · **TD-127**

> The suites were switched on estate-wide and then failed at restore with 401 against the private GitHub Packages feed. Auth was added and hardened, but it could not be confirmed working from this side - the Actions logs are not readable here - so three blind fixes in a row is where this stops.

Files: `.github/workflows/tests.yml`

**`88a072a`** 2026-08-19 — fix(ci): make feed authentication tolerant so it cannot fail the job

> A repo with no root NuGet.Config (l3_SHG) or one without a 'github' source made 'dotnet nuget update source' error and took the whole job down. Both are now a skip-with-notice. If the private feed really was needed, restore still reports the honest 401 rather than a confusing failure in the auth step.

Files: `.github/workflows/tests.yml`

**`66d146d`** 2026-08-19 — fix(ci): authenticate the private GitHub Packages feed before restore

> Every tests.yml did a bare 'dotnet restore', which returns 401 Unauthorized: the Intellect.* packages live on the org's PRIVATE GitHub Packages feed (NuGet.Config -> source 'github') and credentials are deliberately not committed. build.yml already handled this via configure_github_packages_source() in build_push_script.sh; the test workflows never did, so they failed at restore before running one test.

Files: `.github/workflows/tests.yml`

**`3ff4c5b`** 2026-08-19 — ci(TD-125): add publish-free tests.yml running on r2-dev-stable · **TD-125** **TD-39**

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

## How to run it

```bash
git clone <this repo> && cd utils-LAndE
git checkout r2-dev-stable
dotnet build Intellect.Erp.Observability.sln
dotnet test Intellect.Erp.Observability.sln
```

The database comes from the platform repo, not from here:

```bash
# in l2r2-platform-build
mysql -u root -p <empty_database> < db/stable_baseline_ddl.sql
```

It **refuses a non-empty schema** by design. Verify by counting, not by exit code:

```bash
mysql -u root -p -N -e "SELECT COUNT(*) FROM information_schema.tables \
  WHERE table_schema='<db>' AND table_type='BASE TABLE';"
```

---

*Generated by `build/generate-module-readmes.py` in the platform repo. Do not hand-edit: the next run overwrites it. Numbers above were measured when it ran, so re-run it after a state branch moves.*
