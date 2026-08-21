# utils-LAndE

> No curated summary for this repo yet. What it does is best read from its 2 controller file(s); add a line to `PURPOSE` in `build/generate-module-readmes.py` rather than editing this file.

## At a glance

| | |
|---|---|
| Current branch | `r2-dev-stable` |
| HEAD | `f2cf8b1 Converge on one MySQL driver, and drop a query that could never run` |
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
| **TD-39** | `f2cf8b1`, `48280f0`, `32c2aa1` |
| **TD-125** | `32c2aa1` |
| **TD-127** | `48280f0`, `318184e` |
| **TD-134** | `94d2e36` |
| **TD-136** | `b6e087f` |
| **TD-138** | `b6e087f` |
| **TD-153** | `f2cf8b1` |
| **TD-154** | `f2cf8b1` |
| **TD-155** | `f2cf8b1` |
| **TD-156** | `f2cf8b1` |

### Commits

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

**`b6e087f`** 2026-08-21 — docs: regenerate module READMEs with the TD-136 / TD-138 findings · **TD-136** **TD-138**

> The FAS section now records what the module-local allocator audit actually found - that two of them had never run, naming columns fa_vouchermain does not have - and states plainly which half of TD-138 is still open: they all still read a MAX rather than incrementing a counter, so concurrent runs can collide.

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

## State READMEs — append, never fork

This file is generated ON `r2-dev-stable` and flows to every state branch through the sync merges, so state branches keep the full base context and history. A state branch that needs its own notes APPENDS a section **below this line** — never edits the generated body above — so the note survives regeneration and merges back cleanly when the state's work lands on stable:

```markdown
<!-- STATE APPENDIX (r2-dev-XX) — keep everything state-specific below this marker -->
```

---

*Generated by `build/generate-module-readmes.py` in the platform repo. Do not hand-edit: the next run overwrites it. Numbers above were measured when it ran, so re-run it after a state branch moves.*
