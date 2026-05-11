#!/usr/bin/env bash
set -Eeuo pipefail

GITHUB_OWNER="${GITHUB_OWNER:-${ORG:-Intellect-Informatics-Pvt-Ltd}}"
NUGET_SOURCE="${NUGET_SOURCE:-https://nuget.pkg.github.com/${GITHUB_OWNER}/index.json}"
PACKAGE_OUTPUT_DIR="${PACKAGE_OUTPUT_DIR:-artifacts/nuget}"
CONFIGURATION="${CONFIGURATION:-Release}"
INITIAL_VERSION="${INITIAL_VERSION:-1.0.0}"
NEW_VERSION="${NEW_VERSION:-}"
PUSH_PACKAGES="${PUSH_PACKAGES:-true}"
PACK_ALL="${PACK_ALL:-false}"
NUGET_API_KEY="${NUGET_API_KEY:-${GITHUB_TOKEN:-}}"

log() {
<<<<<<< ours
<<<<<<< ours
<<<<<<< ours
    printf '%s\n' "$*" >&2
=======
    printf '%s\n' "$*"
>>>>>>> theirs
=======
    printf '%s\n' "$*"
>>>>>>> theirs
=======
    printf '%s\n' "$*"
>>>>>>> theirs
}

die() {
    printf 'ERROR: %s\n' "$*" >&2
    exit 1
}

require_command() {
    command -v "$1" >/dev/null 2>&1 || die "Required command '$1' was not found."
}

for command_name in git dotnet curl jq sort; do
    require_command "$command_name"
done

if [ "$PUSH_PACKAGES" = "true" ] && [ -z "$NUGET_API_KEY" ]; then
    die "Set GITHUB_TOKEN or NUGET_API_KEY before publishing packages."
fi

resolve_base_ref() {
    if [ -n "${BASE_REF:-}" ]; then
        printf '%s\n' "$BASE_REF"
        return 0
    fi

    if [ -n "${GITHUB_EVENT_PATH:-}" ] && [ -f "$GITHUB_EVENT_PATH" ]; then
        local before_sha
        before_sha="$(jq -r '.before // empty' "$GITHUB_EVENT_PATH")"
        if [ -n "$before_sha" ] \
            && ! printf '%s' "$before_sha" | grep -Eq '^0+$' \
            && git cat-file -e "${before_sha}^{commit}" 2>/dev/null; then
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
        log "Detecting package changes since $base_ref..."
        git diff --name-only "$base_ref" HEAD
    else
        log "No base commit found; considering all tracked files."
        git ls-files
    fi
}

is_repo_wide_build_input() {
    case "$1" in
        *.sln|Directory.Build.props|Directory.Packages.props|NuGet.Config|global.json)
            return 0
            ;;
        *)
            return 1
            ;;
    esac
}

all_package_projects() {
    find src -mindepth 2 -maxdepth 2 -name '*.csproj' -type f | sort
}

project_for_file() {
    local file_path="$1"

    case "$file_path" in
        src/*/*)
            local project_dir="${file_path#src/}"
            project_dir="${project_dir%%/*}"

            find "src/$project_dir" -maxdepth 1 -name '*.csproj' -type f | sort | head -n 1
            ;;
    esac
}

projects_to_pack() {
    local file_path
    local project_path
<<<<<<< ours
<<<<<<< ours
<<<<<<< ours
=======
    local projects=()
>>>>>>> theirs
=======
    local projects=()
>>>>>>> theirs
=======
    local projects=()
>>>>>>> theirs

    while IFS= read -r file_path; do
        [ -z "$file_path" ] && continue

        if is_repo_wide_build_input "$file_path"; then
            log "Repository-level build input changed ($file_path); packing all src projects."
            all_package_projects
            return 0
        fi

        project_path="$(project_for_file "$file_path")"
        if [ -n "$project_path" ]; then
<<<<<<< ours
<<<<<<< ours
<<<<<<< ours
            log "Source package change detected ($file_path); packing all src projects."
            all_package_projects
            return 0
        fi
    done < <(changed_files)
=======
=======
>>>>>>> theirs
=======
>>>>>>> theirs
            projects+=("$project_path")
        fi
    done < <(changed_files)

    if [ "${#projects[@]}" -eq 0 ]; then
        return 0
    fi

    printf '%s\n' "${projects[@]}" | sort -u
<<<<<<< ours
<<<<<<< ours
>>>>>>> theirs
=======
>>>>>>> theirs
=======
>>>>>>> theirs
}

read_package_id() {
    local csproj_file="$1"
    local package_id

    package_id="$(sed -nE 's/.*<PackageId>[[:space:]]*([^<]+)[[:space:]]*<\/PackageId>.*/\1/p' "$csproj_file" | head -n 1)"

    if [ -n "$package_id" ]; then
        printf '%s\n' "$package_id"
    else
        basename "$csproj_file" .csproj
    fi
}

get_latest_version() {
    local package_id="$1"

    if [ -z "${GITHUB_TOKEN:-}" ]; then
        log "No GITHUB_TOKEN available; using initial version for $package_id." >&2
        return 0
    fi

    local encoded_package_id
    encoded_package_id="$(jq -rn --arg package_id "$package_id" '$package_id | @uri')"

    local response
    if ! response="$(curl -sS \
        -w '\n%{http_code}' \
        -H "Authorization: Bearer $GITHUB_TOKEN" \
        -H "Accept: application/vnd.github+json" \
        "https://api.github.com/orgs/${GITHUB_OWNER}/packages/nuget/${encoded_package_id}/versions?per_page=100")"; then
        die "Failed to query GitHub Packages for $package_id."
    fi

    local http_status="${response##*$'\n'}"
    local response_body="${response%$'\n'$http_status}"

    case "$http_status" in
        200)
            local latest_version
            latest_version="$(printf '%s' "$response_body" \
                | jq -r '.[]?.name // empty' \
                | grep -E '^[0-9]+[.][0-9]+[.][0-9]+' \
                | sort -V \
                | tail -n 1 || true)"
            printf '%s\n' "$latest_version"
            ;;
        404)
            log "No existing package found for $package_id; using initial version." >&2
            ;;
        *)
            die "GitHub package lookup failed for $package_id with HTTP $http_status: $response_body"
            ;;
    esac
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

<<<<<<< ours
<<<<<<< ours
<<<<<<< ours
next_release_version() {
=======
next_version_for_package() {
    local package_id="$1"

>>>>>>> theirs
=======
next_version_for_package() {
    local package_id="$1"

>>>>>>> theirs
=======
next_version_for_package() {
    local package_id="$1"

>>>>>>> theirs
    if [ -n "$NEW_VERSION" ]; then
        printf '%s\n' "$NEW_VERSION"
        return 0
    fi

<<<<<<< ours
<<<<<<< ours
<<<<<<< ours
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

    latest_version="$(printf '%s\n' "${latest_versions[@]}" \
        | grep -E '^[0-9]+[.][0-9]+[.][0-9]+' \
        | sort -V \
        | tail -n 1 || true)"
=======
    local latest_version
    latest_version="$(get_latest_version "$package_id")"
>>>>>>> theirs
=======
    local latest_version
    latest_version="$(get_latest_version "$package_id")"
>>>>>>> theirs
=======
    local latest_version
    latest_version="$(get_latest_version "$package_id")"
>>>>>>> theirs

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
    [ -f "$nupkg_file" ] || die "Expected package was not generated: $nupkg_file"

    printf '%s\n' "$nupkg_file"
}

push_package() {
    local nupkg_file="$1"

    log "Pushing $nupkg_file to $NUGET_SOURCE..."
    dotnet nuget push "$nupkg_file" \
        --api-key "$NUGET_API_KEY" \
        --source "$NUGET_SOURCE" \
        --skip-duplicate
}

main() {
    local selected_projects=()
    local project_path
<<<<<<< ours
<<<<<<< ours
<<<<<<< ours
    local package_id
    local package_version
    local release_version
    local nupkg_file
=======
>>>>>>> theirs
=======
>>>>>>> theirs
=======
>>>>>>> theirs

    while IFS= read -r project_path; do
        [ -z "$project_path" ] && continue
        selected_projects+=("$project_path")
    done < <(projects_to_pack)

    if [ "${#selected_projects[@]}" -eq 0 ]; then
        log "No changed package projects found; nothing to pack."
        return 0
    fi

    log "Projects selected for packaging:"
    printf '  - %s\n' "${selected_projects[@]}"

<<<<<<< ours
<<<<<<< ours
<<<<<<< ours
    release_version="$(next_release_version "${selected_projects[@]}")"
    log "Using package version $release_version for this release."

=======
>>>>>>> theirs
=======
>>>>>>> theirs
=======
>>>>>>> theirs
    rm -rf "$PACKAGE_OUTPUT_DIR"
    mkdir -p "$PACKAGE_OUTPUT_DIR"

    log "Restoring solution..."
    dotnet restore Intellect.Erp.Observability.sln

    for project_path in "${selected_projects[@]}"; do
        package_id="$(read_package_id "$project_path")"
<<<<<<< ours
<<<<<<< ours
<<<<<<< ours
        package_version="$release_version"
        pack_project "$project_path" "$package_id" "$package_version"
        nupkg_file="$PACKAGE_OUTPUT_DIR/${package_id}.${package_version}.nupkg"
=======
        package_version="$(next_version_for_package "$package_id")"
        nupkg_file="$(pack_project "$project_path" "$package_id" "$package_version")"
>>>>>>> theirs
=======
        package_version="$(next_version_for_package "$package_id")"
        nupkg_file="$(pack_project "$project_path" "$package_id" "$package_version")"
>>>>>>> theirs
=======
        package_version="$(next_version_for_package "$package_id")"
        nupkg_file="$(pack_project "$project_path" "$package_id" "$package_version")"
>>>>>>> theirs

        if [ "$PUSH_PACKAGES" = "true" ]; then
            push_package "$nupkg_file"
        else
            log "PUSH_PACKAGES=false; skipping publish for $nupkg_file."
        fi
    done

    log "Script execution completed."
}

main "$@"
