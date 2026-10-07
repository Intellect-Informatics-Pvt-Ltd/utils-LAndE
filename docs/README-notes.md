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
