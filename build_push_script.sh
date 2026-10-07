#!/usr/bin/env bash
set -Eeuo pipefail

GITHUB_OWNER="${GITHUB_OWNER:-${ORG:-Intellect-Informatics-Pvt-Ltd}}"
NUGET_SOURCE="${NUGET_SOURCE:-https://nuget.pkg.github.com/${GITHUB_OWNER}/index.json}"
GITHUB_PACKAGES_USERNAME="${GITHUB_PACKAGES_USERNAME:-${GITHUB_ACTOR:-}}"
GITHUB_PACKAGES_PAT="${GITHUB_PACKAGES_PAT:-}"
GITHUB_TOKEN="${GITHUB_TOKEN:-}"

NUGET_CONFIG="${NUGET_CONFIG:-NuGet.Config}"
PACKAGE_OUTPUT_DIR="${PACKAGE_OUTPUT_DIR:-artifacts/nuget}"
CONFIGURATION="${CONFIGURATION:-Release}"
INITIAL_VERSION="${INITIAL_VERSION:-1.0.0}"
NEW_VERSION="${NEW_VERSION:-}"
PUSH_PACKAGES="${PUSH_PACKAGES:-true}"
PACK_ALL="${PACK_ALL:-true}"
MAX_VERSION_PAGES="${MAX_VERSION_PAGES:-20}"

# Where a package's version comes from.
#   feed   (default; master pushes and workflow_dispatch - unchanged): every package is stamped with
#          NEW_VERSION, or with (the highest version published for any selected package) + 1 patch.
#   csproj (a push to r2-dev-stable, owner ruling 2026-10-07): each packable project is packed at
#          the version its OWN project declares (<Version>/<PackageVersion>/<VersionPrefix> in the
#          csproj or a Directory.Build.props above it, read back with `dotnet msbuild
#          -getProperty:PackageVersion`), and pushed only when that version is a plain release
#          (X.Y.Z) strictly newer than every version of that package id already on the feed.
#          Anything else - undeclared, prerelease, equal or older - is logged and skipped.
#          Every package actually pushed is written to PUBLISHED_MANIFEST as "<PackageId> <Version>",
#          which build.yml turns into the git tag nuget/<PackageId>/<Version>.
VERSION_SOURCE="${VERSION_SOURCE:-feed}"
PUBLISHED_MANIFEST="${PUBLISHED_MANIFEST:-artifacts/published-packages.txt}"

NUGET_API_KEY="${NUGET_API_KEY:-${GITHUB_PACKAGES_PAT:-${GITHUB_TOKEN:-}}}"
PACKAGE_QUERY_TOKEN="${GITHUB_PACKAGES_PAT:-${GITHUB_TOKEN:-}}"

log() {
    printf '%s\n' "$*" >&2
}

die() {
    printf 'ERROR: %s\n' "$*" >&2
    exit 1
}

require_command() {
    command -v "$1" >/dev/null 2>&1 ||
        die "Required command '$1' was not found."
}

for command_name in git dotnet curl jq sort find grep sed awk; do
    require_command "$command_name"
done

[ -f "$NUGET_CONFIG" ] ||
    die "NuGet configuration file not found: $NUGET_CONFIG"

case "$PACK_ALL" in
    true|false) ;;
    *) die "PACK_ALL must be true or false, but was '$PACK_ALL'." ;;
esac

case "$PUSH_PACKAGES" in
    true|false) ;;
    *) die "PUSH_PACKAGES must be true or false, but was '$PUSH_PACKAGES'." ;;
esac

case "$VERSION_SOURCE" in
    feed|csproj) ;;
    *) die "VERSION_SOURCE must be feed or csproj, but was '$VERSION_SOURCE'." ;;
esac

if [ "$VERSION_SOURCE" = "csproj" ] &&
   [ -n "$NEW_VERSION" ]; then
    die "NEW_VERSION cannot be combined with VERSION_SOURCE=csproj: the csproj is the version."
fi

if [ -n "$NEW_VERSION" ] &&
   [[ ! "$NEW_VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+([.-][0-9A-Za-z.-]+)?$ ]]; then
    die "NEW_VERSION '$NEW_VERSION' is not a supported semantic version."
fi

if [ "$PUSH_PACKAGES" = "true" ] &&
   [ -z "$NUGET_API_KEY" ]; then
    die "Set GITHUB_PACKAGES_PAT, GITHUB_TOKEN, or NUGET_API_KEY before publishing packages."
fi

configure_github_packages_source() {
    local token="${GITHUB_PACKAGES_PAT:-${GITHUB_TOKEN:-}}"

    if [ -z "$token" ]; then
        log "No GitHub Packages token is available; authenticated private restores may fail."
        return 0
    fi

    if [ -z "$GITHUB_PACKAGES_USERNAME" ]; then
        die "Set GITHUB_PACKAGES_USERNAME or GITHUB_ACTOR."
    fi

    log "Configuring GitHub Packages source for $GITHUB_PACKAGES_USERNAME."

    dotnet nuget update source github \
        --configfile "$NUGET_CONFIG" \
        --source "$NUGET_SOURCE" \
        --username "$GITHUB_PACKAGES_USERNAME" \
        --password "$token" \
        --store-password-in-clear-text \
        >/dev/null
}

resolve_base_ref() {
    if [ -n "${BASE_REF:-}" ]; then
        printf '%s\n' "$BASE_REF"
        return 0
    fi

    if [ -n "${GITHUB_EVENT_PATH:-}" ] &&
       [ -f "$GITHUB_EVENT_PATH" ]; then
        local before_sha

        before_sha="$(jq -r '.before // empty' "$GITHUB_EVENT_PATH")"

        if [ -n "$before_sha" ] &&
           ! printf '%s' "$before_sha" | grep -Eq '^0+$' &&
           git cat-file -e "${before_sha}^{commit}" 2>/dev/null; then
            printf '%s\n' "$before_sha"
            return 0
        fi
    fi

    if git rev-parse --verify HEAD^ >/dev/null 2>&1; then
        printf '%s\n' "HEAD^"
    fi
}

changed_files() {
    local base_ref
    base_ref="$(resolve_base_ref || true)"

    if [ "$PACK_ALL" = "true" ]; then
        git ls-files
        return 0
    fi

    if [ -n "$base_ref" ]; then
        log "Detecting changes since $base_ref..."
        git diff --name-only "$base_ref" HEAD
    else
        log "No base commit found; considering all tracked files."
        git ls-files
    fi
}

is_repo_wide_build_input() {
    case "$1" in
        *.sln|\
        Directory.Build.props|\
        Directory.Build.targets|\
        Directory.Packages.props|\
        NuGet.Config|\
        nuget.config|\
        global.json|\
        build_push_script.sh|\
        .github/workflows/*)
            return 0
            ;;
        *)
            return 1
            ;;
    esac
}

is_packable_project() {
    local csproj_file="$1"
    local base_name

    [ -f "$csproj_file" ] || return 1

    if grep -Eiq \
        '<IsPackable>[[:space:]]*false[[:space:]]*</IsPackable>' \
        "$csproj_file"; then
        return 1
    fi

    if grep -Eiq \
        '<PackageId>[[:space:]]*[^<]+[[:space:]]*</PackageId>' \
        "$csproj_file"; then
        return 0
    fi

    if grep -Eiq \
        '<(IsPackable|GeneratePackageOnBuild)>[[:space:]]*true[[:space:]]*</(IsPackable|GeneratePackageOnBuild)>' \
        "$csproj_file"; then
        return 0
    fi

    if grep -Eiq \
        '<Project[^>]+Sdk="[^"]*(Web|Worker)[^"]*"' \
        "$csproj_file"; then
        return 1
    fi

    if grep -Eiq \
        '<OutputType>[[:space:]]*(Exe|WinExe)[[:space:]]*</OutputType>' \
        "$csproj_file"; then
        return 1
    fi

    base_name="$(basename "$csproj_file" .csproj)"

    if [[ "$base_name" =~ ([._-]|^)(Test|Tests|Testing|IntegrationTests|UnitTests)$ ]]; then
        return 1
    fi

    grep -Eiq \
        '<Project[^>]+Sdk="Microsoft[.]NET[.]Sdk(["/]|[.][^"]*")' \
        "$csproj_file"
}

all_package_projects() {
    local csproj_file

    while IFS= read -r -d '' csproj_file; do
        if is_packable_project "$csproj_file"; then
            printf '%s\n' "$csproj_file"
        else
            log "Skipping non-packable project: $csproj_file"
        fi
    done < <(git ls-files -z '*.csproj' | sort -z)
}

project_for_file() {
    local file_path="$1"
    local project_path
    local project_dir
    local best_project=""
    local best_length=0

    if [[ "$file_path" == *.csproj ]] &&
       is_packable_project "$file_path"; then
        printf '%s\n' "$file_path"
        return 0
    fi

    while IFS= read -r project_path; do
        project_dir="$(dirname "$project_path")"

        if [[ "$file_path" == "$project_dir"/* ||
              "$file_path" == "$project_dir" ]]; then
            if [ "${#project_dir}" -gt "$best_length" ]; then
                best_project="$project_path"
                best_length="${#project_dir}"
            fi
        fi
    done < <(all_package_projects)

    if [ -n "$best_project" ]; then
        printf '%s\n' "$best_project"
    fi
}

projects_to_pack() {
    local file_path
    local project_path
    local selected_projects=()

    if [ "$PACK_ALL" = "true" ]; then
        log "PACK_ALL=true; selecting all packable projects."
        all_package_projects
        return 0
    fi

    while IFS= read -r file_path; do
        [ -z "$file_path" ] && continue

        if is_repo_wide_build_input "$file_path"; then
            log "Repository-level build input changed: $file_path"
            all_package_projects
            return 0
        fi

        project_path="$(project_for_file "$file_path")"

        if [ -n "$project_path" ]; then
            log "Package change detected: $file_path -> $project_path"
            selected_projects+=("$project_path")
        fi
    done < <(changed_files)

    if [ "${#selected_projects[@]}" -gt 0 ]; then
        printf '%s\n' "${selected_projects[@]}" | sort -u
    fi
}

read_package_id() {
    local csproj_file="$1"
    local package_id

    package_id="$(
        sed -nE \
            's/.*<PackageId>[[:space:]]*([^<]+)[[:space:]]*<\/PackageId>.*/\1/p' \
            "$csproj_file" |
            head -n 1
    )"

    if [ -n "$package_id" ]; then
        printf '%s\n' "$package_id"
    else
        basename "$csproj_file" .csproj
    fi
}

is_meta_project() {
    local csproj_file="$1"

    grep -Eq \
        '<IsMetaPackage>[[:space:]]*true[[:space:]]*</IsMetaPackage>' \
        "$csproj_file"
}

# Every version name of one package id on the feed, one per line (may include blank lines).
# Split out of get_latest_version unchanged so csproj mode can read the whole list; this is also
# the function a local dry run replaces with a stub (see the release notes in docs/README-notes.md).
published_versions() {
    local package_id="$1"

    if [ -z "$PACKAGE_QUERY_TOKEN" ]; then
        log "No query token available for $package_id."
        return 0
    fi

    local encoded_package_id
    encoded_package_id="$(jq -rn --arg package_id "$package_id" '$package_id | @uri')"

    local page
    local response
    local http_status
    local response_body
    local page_count
    local all_versions=""

    for ((page = 1; page <= MAX_VERSION_PAGES; page++)); do
        if ! response="$(
            curl -sS \
                -w '\n%{http_code}' \
                -H "Authorization: Bearer $PACKAGE_QUERY_TOKEN" \
                -H "Accept: application/vnd.github+json" \
                -H "X-GitHub-Api-Version: 2022-11-28" \
                "https://api.github.com/orgs/${GITHUB_OWNER}/packages/nuget/${encoded_package_id}/versions?per_page=100&page=${page}"
        )"; then
            die "Failed to query GitHub Packages for $package_id."
        fi

        http_status="${response##*$'\n'}"
        response_body="${response%$'\n'$http_status}"

        case "$http_status" in
            200)
                if ! printf '%s' "$response_body" | jq -e 'type == "array"' >/dev/null; then
                    die "Unexpected GitHub API response for $package_id."
                fi

                page_count="$(printf '%s' "$response_body" | jq 'length')"
                all_versions+=$'\n'"$(printf '%s' "$response_body" | jq -r '.[]?.name // empty')"

                if [ "$page_count" -lt 100 ]; then
                    break
                fi
                ;;
            404)
                if [ "$page" -eq 1 ]; then
                    log "No existing package found for $package_id."
                fi
                break
                ;;
            401)
                die "GitHub Packages authentication failed for $package_id."
                ;;
            403)
                die "The token user cannot read package $package_id."
                ;;
            *)
                die "Package lookup failed for $package_id with HTTP $http_status: $response_body"
                ;;
        esac
    done

    printf '%s\n' "$all_versions"
}

get_latest_version() {
    local package_id="$1"
    local all_versions

    # `|| exit`: callers run this inside $(...), where bash does not inherit errexit, so a lookup
    # failure (die) must end this subshell exactly as it did before the split.
    all_versions="$(published_versions "$package_id")" || exit $?

    printf '%s\n' "$all_versions" |
        grep -E '^[0-9]+[.][0-9]+[.][0-9]+' |
        sort -V |
        tail -n 1 || true
}

increment_version() {
    local version="$1"

    if [[ "$version" =~ ^([0-9]+)\.([0-9]+)\.([0-9]+) ]]; then
        printf '%s.%s.%s\n' \
            "${BASH_REMATCH[1]}" \
            "${BASH_REMATCH[2]}" \
            "$((BASH_REMATCH[3] + 1))"
    else
        printf '%s\n' "$INITIAL_VERSION"
    fi
}

next_release_version() {
    if [ -n "$NEW_VERSION" ]; then
        printf '%s\n' "$NEW_VERSION"
        return 0
    fi

    local project_path
    local package_id
    local latest_version
    local latest_versions=()

    for project_path in "$@"; do
        package_id="$(read_package_id "$project_path")"
        latest_version="$(get_latest_version "$package_id")"

        if [ -n "$latest_version" ]; then
            latest_versions+=("$latest_version")
        fi
    done

    if [ "${#latest_versions[@]}" -eq 0 ]; then
        printf '%s\n' "$INITIAL_VERSION"
        return 0
    fi

    latest_version="$(
        printf '%s\n' "${latest_versions[@]}" |
            grep -E '^[0-9]+[.][0-9]+[.][0-9]+' |
            sort -V |
            tail -n 1 || true
    )"

    if [ -n "$latest_version" ]; then
        increment_version "$latest_version"
    else
        printf '%s\n' "$INITIAL_VERSION"
    fi
}

pack_project() {
    local csproj_file="$1"
    local package_id="$2"
    local package_version="$3"

    log "Packing $package_id $package_version from $csproj_file..."

    dotnet pack "$csproj_file" \
        --configuration "$CONFIGURATION" \
        --no-restore \
        -p:Version="$package_version" \
        -p:PackageVersion="$package_version" \
        -p:ContinuousIntegrationBuild=true \
        -o "$PACKAGE_OUTPUT_DIR"

    local nupkg_file="$PACKAGE_OUTPUT_DIR/${package_id}.${package_version}.nupkg"

    [ -f "$nupkg_file" ] ||
        die "Expected package was not generated: $nupkg_file"
}

push_package() {
    local nupkg_file="$1"

    log "Pushing $nupkg_file to $NUGET_SOURCE..."

    if ! dotnet nuget push "$nupkg_file" \
        --api-key "$NUGET_API_KEY" \
        --source "$NUGET_SOURCE" \
        --skip-duplicate; then
        die "Failed to push $nupkg_file. Verify package access and token permissions."
    fi
}

# ---------------------------------------------------------------------------------------------
# VERSION_SOURCE=csproj
# ---------------------------------------------------------------------------------------------

# True when the project states its own version: <Version>, <PackageVersion> or <VersionPrefix>
# in the csproj itself or in a Directory.Build.props between it and the repository root. Without
# one, MSBuild silently answers 1.0.0 - a placeholder, never a release.
declares_version() {
    local csproj_file="$1"
    local dir
    local candidates=("$csproj_file")

    dir="$(dirname "$csproj_file")"
    while :; do
        [ -f "$dir/Directory.Build.props" ] && candidates+=("$dir/Directory.Build.props")
        [ "$dir" = "." ] || [ "$dir" = "/" ] && break
        dir="$(dirname "$dir")"
    done

    grep -Eq '<(Version|PackageVersion|VersionPrefix)>[^<]+</(Version|PackageVersion|VersionPrefix)>' \
        "${candidates[@]}"
}

# The version `dotnet pack` would stamp on this project with no -p:Version.
declared_package_version() {
    local csproj_file="$1"

    dotnet msbuild "$csproj_file" -nologo -getProperty:PackageVersion -p:Configuration="$CONFIGURATION" |
        tail -n 1 |
        tr -d '[:space:]'
}

is_release_version() {
    [[ "$1" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]
}

# 0 when release version $1 is strictly newer than every version listed on stdin (SemVer order:
# a prerelease X.Y.Z-tag is older than X.Y.Z; build metadata after '+' is ignored).
is_newer_than_all() {
    local candidate="$1"
    local published
    local core

    while IFS= read -r published; do
        published="${published%%+*}"
        [ -z "$published" ] && continue
        core="${published%%-*}"

        [ "$core" = "$candidate" ] && [ "$core" = "$published" ] && return 1

        if [ "$core" != "$candidate" ] &&
           [ "$(printf '%s\n%s\n' "$core" "$candidate" | sort -V | tail -n 1)" != "$candidate" ]; then
            return 1
        fi
    done

    return 0
}

# Push one package. Returns 0 when the feed accepted it, 3 when the feed already had it
# (--skip-duplicate turns that conflict into success, so it is read from the output).
push_package_reporting() {
    local nupkg_file="$1"
    local output

    log "Pushing $nupkg_file to $NUGET_SOURCE..."

    if ! output="$(dotnet nuget push "$nupkg_file" \
        --api-key "$NUGET_API_KEY" \
        --source "$NUGET_SOURCE" \
        --skip-duplicate 2>&1)"; then
        printf '%s\n' "$output" >&2
        die "Failed to push $nupkg_file. Verify package access and token permissions."
    fi

    printf '%s\n' "$output" >&2

    # A string test, not `printf | grep -q`: under pipefail, grep -q leaving early can SIGPIPE printf.
    if [[ "$output" == *"already exists at feed"* ]]; then
        return 3
    fi
}

main_csproj() {
    local project_path
    local package_id
    local version
    local versions_on_feed
    local nupkg_file
    local push_status
    local leaf_projects=()
    local meta_projects=()
    local release_projects=()
    local release_ids=()
    local release_versions=()
    local index

    rm -rf "$PACKAGE_OUTPUT_DIR"
    mkdir -p "$PACKAGE_OUTPUT_DIR" "$(dirname "$PUBLISHED_MANIFEST")"
    : > "$PUBLISHED_MANIFEST"

    for project_path in "$@"; do
        package_id="$(read_package_id "$project_path")"

        if ! declares_version "$project_path"; then
            log "SKIP $package_id: $project_path declares no version (MSBuild's 1.0.0 default is a placeholder) - not published."
            continue
        fi

        version="$(declared_package_version "$project_path")"

        if ! is_release_version "$version"; then
            log "SKIP $package_id $version: a prerelease or non-X.Y.Z version is never published from r2-dev-stable."
            continue
        fi

        versions_on_feed="$(published_versions "$package_id")" || exit $?

        if ! printf '%s\n' "$versions_on_feed" | is_newer_than_all "$version"; then
            log "SKIP $package_id $version: already published / not newer than $(
                printf '%s\n' "$versions_on_feed" | grep -E '^[0-9]' | sort -V | tail -n 1) on the feed."
            continue
        fi

        log "PUBLISH $package_id $version (declared by $project_path; newer than everything on the feed)."

        if is_meta_project "$project_path"; then
            meta_projects+=("$project_path")
        else
            leaf_projects+=("$project_path")
        fi
    done

    if [ "${#leaf_projects[@]}" -eq 0 ] && [ "${#meta_projects[@]}" -eq 0 ]; then
        log "Nothing newer than the feed was declared; nothing to pack or publish."
        return 0
    fi

    configure_github_packages_source

    # Packed WITHOUT -p:Version: a global Version would also flow into every ProjectReference and
    # stamp the dependency ranges, which is exactly what must come from each project's own csproj.
    for project_path in ${leaf_projects[@]+"${leaf_projects[@]}"}; do
        dotnet restore "$project_path" --configfile "$NUGET_CONFIG"
    done

    for project_path in ${leaf_projects[@]+"${leaf_projects[@]}"} ${meta_projects[@]+"${meta_projects[@]}"}; do
        package_id="$(read_package_id "$project_path")"
        version="$(declared_package_version "$project_path")"

        if is_meta_project "$project_path"; then
            dotnet restore "$project_path" \
                --configfile "$NUGET_CONFIG" \
                -p:RestoreAdditionalProjectSources="$PACKAGE_OUTPUT_DIR"
        fi

        log "Packing $package_id $version from $project_path at its declared version..."

        dotnet pack "$project_path" \
            --configuration "$CONFIGURATION" \
            --no-restore \
            -p:ContinuousIntegrationBuild=true \
            -o "$PACKAGE_OUTPUT_DIR"

        nupkg_file="$PACKAGE_OUTPUT_DIR/${package_id}.${version}.nupkg"
        [ -f "$nupkg_file" ] ||
            die "Expected package was not generated: $nupkg_file"

        release_projects+=("$project_path")
        release_ids+=("$package_id")
        release_versions+=("$version")
    done

    for index in "${!release_projects[@]}"; do
        package_id="${release_ids[$index]}"
        version="${release_versions[$index]}"
        nupkg_file="$PACKAGE_OUTPUT_DIR/${package_id}.${version}.nupkg"

        if [ "$PUSH_PACKAGES" != "true" ]; then
            log "PUSH_PACKAGES=false; packed but not pushing $nupkg_file."
            continue
        fi

        push_status=0
        push_package_reporting "$nupkg_file" || push_status=$?

        if [ "$push_status" -eq 3 ]; then
            log "$package_id $version was already on the feed (duplicate) - not recorded as published by this run."
            continue
        fi

        printf '%s %s\n' "$package_id" "$version" >> "$PUBLISHED_MANIFEST"
        log "Published $package_id $version."
    done

    log "Script execution completed (VERSION_SOURCE=csproj)."
}

main() {
    local selected_projects=()
    local leaf_projects=()
    local meta_projects=()
    local nupkg_files=()
    local project_path
    local package_id
    local release_version
    local nupkg_file

    while IFS= read -r project_path; do
        [ -z "$project_path" ] && continue
        selected_projects+=("$project_path")
    done < <(projects_to_pack)

    if [ "${#selected_projects[@]}" -eq 0 ]; then
        log "No packable NuGet projects were found; nothing to pack."
        return 0
    fi

    log "Projects selected for packaging:"
    printf '  - %s\n' "${selected_projects[@]}" >&2

    if [ "$VERSION_SOURCE" = "csproj" ]; then
        main_csproj "${selected_projects[@]}"
        return 0
    fi

    for project_path in "${selected_projects[@]}"; do
        if is_meta_project "$project_path"; then
            meta_projects+=("$project_path")
        else
            leaf_projects+=("$project_path")
        fi
    done

    release_version="$(next_release_version "${selected_projects[@]}")"
    log "Using package version $release_version."

    rm -rf "$PACKAGE_OUTPUT_DIR"
    mkdir -p "$PACKAGE_OUTPUT_DIR"

    configure_github_packages_source

    log "Restoring non-meta package projects..."

    for project_path in "${leaf_projects[@]}"; do
        dotnet restore "$project_path" --configfile "$NUGET_CONFIG"
    done

    for project_path in "${leaf_projects[@]}"; do
        package_id="$(read_package_id "$project_path")"
        pack_project "$project_path" "$package_id" "$release_version"
        nupkg_file="$PACKAGE_OUTPUT_DIR/${package_id}.${release_version}.nupkg"
        nupkg_files+=("$nupkg_file")
    done

    log "Restoring meta-package projects against local artifacts..."

    for project_path in "${meta_projects[@]}"; do
        dotnet restore "$project_path" \
            --configfile "$NUGET_CONFIG" \
            -p:RestoreAdditionalProjectSources="$PACKAGE_OUTPUT_DIR"
    done

    for project_path in "${meta_projects[@]}"; do
        package_id="$(read_package_id "$project_path")"
        pack_project "$project_path" "$package_id" "$release_version"
        nupkg_file="$PACKAGE_OUTPUT_DIR/${package_id}.${release_version}.nupkg"
        nupkg_files+=("$nupkg_file")
    done

    for nupkg_file in "${nupkg_files[@]}"; do
        if [ "$PUSH_PACKAGES" = "true" ]; then
            push_package "$nupkg_file"
        else
            log "PUSH_PACKAGES=false; skipping $nupkg_file."
        fi
    done

    log "Script execution completed."
}

# Run only when executed. A local dry run may `source` this file, replace published_versions with
# a stub, and call main itself (docs/README-notes.md, "Releasing").
if [[ "${BASH_SOURCE[0]}" == "$0" ]]; then
    main "$@"
fi
