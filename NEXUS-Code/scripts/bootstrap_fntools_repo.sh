#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

usage() {
  cat <<'EOF'
Usage:
  NEXUS-Code/scripts/bootstrap_fntools_repo.sh --destination-root <path> [options]

Options:
  --destination-root <path>  Required. Where to stage the standalone FnTools repo.
  --source-commit <sha>      Optional. Recorded source baseline commit. Defaults to current HEAD.
  --force                    Remove the destination root first if it already exists.
  --dry-run                  Print planned operations without writing files.
  --help                     Show this help.
EOF
}

destination_root=""
source_commit="$(git -C "$repo_root" rev-parse HEAD)"
force="false"
dry_run="false"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --destination-root)
      destination_root="${2:-}"
      shift 2
      ;;
    --source-commit)
      source_commit="${2:-}"
      shift 2
      ;;
    --force)
      force="true"
      shift
      ;;
    --dry-run)
      dry_run="true"
      shift
      ;;
    --help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown option: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

if [[ -z "$destination_root" ]]; then
  echo "Missing required option: --destination-root" >&2
  usage >&2
  exit 1
fi

destination_root="$(realpath -m "$destination_root")"

run() {
  if [[ "$dry_run" == "true" ]]; then
    printf '[dry-run] %s\n' "$*"
  else
    "$@"
  fi
}

write_file() {
  local path="$1"
  shift

  if [[ "$dry_run" == "true" ]]; then
    printf '[dry-run] write %s\n' "$path"
    return
  fi

  mkdir -p "$(dirname "$path")"
  cat >"$path"
}

copy_dir() {
  local source_rel="$1"
  local destination_rel="$2"
  local source_path="$repo_root/$source_rel"
  local destination_path="$destination_root/$destination_rel"

  if [[ ! -d "$source_path" ]]; then
    echo "Missing source directory: $source_path" >&2
    exit 1
  fi

  if [[ "$dry_run" == "true" ]]; then
    printf '[dry-run] copy %s -> %s\n' "$source_path" "$destination_path"
    return
  fi

  mkdir -p "$(dirname "$destination_path")"
  cp -R "$source_path" "$destination_path"
}

copy_file() {
  local source_rel="$1"
  local destination_rel="$2"
  local source_path="$repo_root/$source_rel"
  local destination_path="$destination_root/$destination_rel"

  if [[ ! -f "$source_path" ]]; then
    echo "Missing source file: $source_path" >&2
    exit 1
  fi

  if [[ "$dry_run" == "true" ]]; then
    printf '[dry-run] copy %s -> %s\n' "$source_path" "$destination_path"
    return
  fi

  mkdir -p "$(dirname "$destination_path")"
  cp "$source_path" "$destination_path"
}

replace_in_file() {
  local path="$1"
  local before="$2"
  local after="$3"

  if [[ "$dry_run" == "true" ]]; then
    printf '[dry-run] replace in %s: %s -> %s\n' "$path" "$before" "$after"
    return
  fi

  BEFORE="$before" AFTER="$after" perl -0pi -e '
    my $before = $ENV{BEFORE};
    my $after = $ENV{AFTER};
    s/\Q$before\E/$after/g;
  ' "$path"
}

rename_file() {
  local from_path="$1"
  local to_path="$2"

  if [[ "$dry_run" == "true" ]]; then
    printf '[dry-run] rename %s -> %s\n' "$from_path" "$to_path"
    return
  fi

  mv "$from_path" "$to_path"
}

if [[ -e "$destination_root" ]]; then
  if [[ "$force" == "true" ]]; then
    run rm -rf "$destination_root"
  else
    echo "Destination already exists: $destination_root" >&2
    echo "Use --force to replace it." >&2
    exit 1
  fi
fi

run mkdir -p "$destination_root"

copy_dir "NEXUS-Code/src/Nexus.FnHCI" "src/FnTools.FnHCI"
copy_dir "NEXUS-Code/src/Nexus.FnHCI.UI" "src/FnTools.FnHCI.UI"
copy_dir "NEXUS-Code/src/Nexus.FnHCI.UI.Blazor" "src/FnTools.FnHCI.UI.Blazor"
copy_dir "NEXUS-Code/tests/FnTools.Tests" "tests/FnTools.Tests"

copy_file "docs/fntools-foundation.md" "docs/foundation.md"
copy_file "docs/fnui-foundation.md" "docs/fnui-foundation.md"
copy_file "docs/fnhci-namespace-map.md" "docs/namespace-map.md"
copy_file "docs/fnhci-ui-blazor-requirements.md" "docs/fnui-requirements.md"
copy_file "docs/fnhci-ui-web-requirements.md" "docs/fnui-web-requirements.md"
copy_file "docs/fnhci-ui-native-host-requirements.md" "docs/fnui-native-host-requirements.md"
copy_file "docs/fnhci-conversation-reading-surface.md" "docs/conversation-reading-surface.md"
copy_file \
  "docs/decisions/0015-fnhci-owns-the-top-interaction-namespace.md" \
  "docs/decisions/0001-fnhci-owns-the-top-interaction-namespace.md"

rename_file \
  "$destination_root/src/FnTools.FnHCI/Nexus.FnHCI.fsproj" \
  "$destination_root/src/FnTools.FnHCI/FnTools.FnHCI.fsproj"
rename_file \
  "$destination_root/src/FnTools.FnHCI.UI/Nexus.FnHCI.UI.fsproj" \
  "$destination_root/src/FnTools.FnHCI.UI/FnTools.FnHCI.UI.fsproj"
rename_file \
  "$destination_root/src/FnTools.FnHCI.UI.Blazor/Nexus.FnHCI.UI.Blazor.fsproj" \
  "$destination_root/src/FnTools.FnHCI.UI.Blazor/FnTools.FnHCI.UI.Blazor.fsproj"

replace_in_file \
  "$destination_root/src/FnTools.FnHCI.UI/FnTools.FnHCI.UI.fsproj" \
  "..\\Nexus.FnHCI\\Nexus.FnHCI.fsproj" \
  "..\\FnTools.FnHCI\\FnTools.FnHCI.fsproj"
replace_in_file \
  "$destination_root/src/FnTools.FnHCI.UI.Blazor/FnTools.FnHCI.UI.Blazor.fsproj" \
  "..\\Nexus.FnHCI.UI\\Nexus.FnHCI.UI.fsproj" \
  "..\\FnTools.FnHCI.UI\\FnTools.FnHCI.UI.fsproj"
replace_in_file \
  "$destination_root/tests/FnTools.Tests/FnTools.Tests.fsproj" \
  "..\\..\\src\\Nexus.FnHCI\\Nexus.FnHCI.fsproj" \
  "..\\..\\src\\FnTools.FnHCI\\FnTools.FnHCI.fsproj"
replace_in_file \
  "$destination_root/tests/FnTools.Tests/FnTools.Tests.fsproj" \
  "..\\..\\src\\Nexus.FnHCI.UI\\Nexus.FnHCI.UI.fsproj" \
  "..\\..\\src\\FnTools.FnHCI.UI\\FnTools.FnHCI.UI.fsproj"
replace_in_file \
  "$destination_root/tests/FnTools.Tests/FnTools.Tests.fsproj" \
  "..\\..\\src\\Nexus.FnHCI.UI.Blazor\\Nexus.FnHCI.UI.Blazor.fsproj" \
  "..\\..\\src\\FnTools.FnHCI.UI.Blazor\\FnTools.FnHCI.UI.Blazor.fsproj"

write_file "$destination_root/.gitignore" <<'EOF'
bin/
obj/
.vs/
TestResults/
EOF

write_file "$destination_root/README.md" <<EOF
# FnTools

This repo is the extracted reusable tooling and interaction-library line from \`NEXUS-EMERGING\`.

Bootstrap source:

- source repo: \`NEXUS-EMERGING\`
- source commit: \`$source_commit\`

Current included projects:

- \`FnTools.FnHCI\`
- \`FnTools.FnHCI.UI\`
- \`FnTools.FnHCI.UI.Blazor\`

Current included test project:

- \`FnTools.Tests\`

Start here:

- \`docs/foundation.md\`
- \`docs/namespace-map.md\`
- \`docs/fnui-requirements.md\`
EOF

write_file "$destination_root/bootstrap-source.toml" <<EOF
schema_version = 1
artifact_kind = "fntools_repo_bootstrap"
source_repo = "NEXUS-EMERGING"
source_commit = "$source_commit"

[[projects]]
path = "src/FnTools.FnHCI/FnTools.FnHCI.fsproj"

[[projects]]
path = "src/FnTools.FnHCI.UI/FnTools.FnHCI.UI.fsproj"

[[projects]]
path = "src/FnTools.FnHCI.UI.Blazor/FnTools.FnHCI.UI.Blazor.fsproj"

[[tests]]
path = "tests/FnTools.Tests/FnTools.Tests.fsproj"
EOF

if [[ "$dry_run" == "true" ]]; then
  printf '[dry-run] dotnet new sln --name FnTools --format slnx --output %s\n' "$destination_root"
  printf '[dry-run] dotnet sln add src/FnTools.FnHCI/FnTools.FnHCI.fsproj\n'
  printf '[dry-run] dotnet sln add src/FnTools.FnHCI.UI/FnTools.FnHCI.UI.fsproj\n'
  printf '[dry-run] dotnet sln add src/FnTools.FnHCI.UI.Blazor/FnTools.FnHCI.UI.Blazor.fsproj\n'
  printf '[dry-run] dotnet sln add tests/FnTools.Tests/FnTools.Tests.fsproj\n'
else
  dotnet new sln --name FnTools --format slnx --output "$destination_root" >/dev/null
  dotnet sln "$destination_root/FnTools.slnx" add \
    "$destination_root/src/FnTools.FnHCI/FnTools.FnHCI.fsproj" \
    "$destination_root/src/FnTools.FnHCI.UI/FnTools.FnHCI.UI.fsproj" \
    "$destination_root/src/FnTools.FnHCI.UI.Blazor/FnTools.FnHCI.UI.Blazor.fsproj" \
    "$destination_root/tests/FnTools.Tests/FnTools.Tests.fsproj" >/dev/null
fi

printf 'FnTools repo bootstrap prepared.\n'
printf '  Source commit: %s\n' "$source_commit"
printf '  Destination: %s\n' "$destination_root"
printf '  Dry run: %s\n' "$dry_run"
