#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

usage() {
  cat <<'EOF'
Usage:
  NEXUS-Code/scripts/bootstrap_cheddarbooks_repo.sh --destination-root <path> --fntools-root <path> [options]

Options:
  --destination-root <path>  Required. Where to stage the standalone CheddarBooks repo.
  --fntools-root <path>      Required. Local FnTools repo root used for bootstrap project references.
  --source-commit <sha>      Optional. Recorded source baseline commit. Defaults to current HEAD.
  --force                    Remove the destination root first if it already exists.
  --dry-run                  Print planned operations without writing files.
  --help                     Show this help.
EOF
}

destination_root=""
fntools_root=""
source_commit="$(git -C "$repo_root" rev-parse HEAD)"
force="false"
dry_run="false"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --destination-root)
      destination_root="${2:-}"
      shift 2
      ;;
    --fntools-root)
      fntools_root="${2:-}"
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

if [[ -z "$fntools_root" ]]; then
  echo "Missing required option: --fntools-root" >&2
  usage >&2
  exit 1
fi

destination_root="$(realpath -m "$destination_root")"
fntools_root="$(realpath -m "$fntools_root")"

if [[ ! -d "$fntools_root" ]]; then
  echo "FnTools root does not exist: $fntools_root" >&2
  exit 1
fi

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
  find "$destination_path" -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
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

relative_ref() {
  local from_dir="$1"
  local to_path="$2"
  realpath --relative-to="$from_dir" "$to_path"
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

copy_dir "NEXUS-Code/src/Nexus.CheddarBooks.LaundryLog" "src/CheddarBooks.LaundryLog"
copy_dir "NEXUS-Code/src/Nexus.CheddarBooks.LaundryLog.UI" "src/CheddarBooks.LaundryLog.UI"
copy_dir "NEXUS-Code/tests/CheddarBooks.Tests" "tests/CheddarBooks.Tests"
copy_dir "docs/application-domains/cheddarbooks/laundrylog" "docs/laundrylog"

copy_dir "docs/application-domains/cheddarbooks" "docs/cheddarbooks-docs"
if [[ "$dry_run" != "true" ]]; then
  rm -rf "$destination_root/docs/cheddarbooks-docs/laundrylog"
fi

if [[ "$dry_run" == "true" ]]; then
  ui_to_fntools_ref="(computed relative path to FnTools.FnHCI.UI)"
  tests_to_fntools_ref="(computed relative path to FnTools.FnHCI.UI)"
else
  ui_to_fntools_ref="$(relative_ref \
    "$destination_root/src/CheddarBooks.LaundryLog.UI" \
    "$fntools_root/src/FnTools.FnHCI.UI/FnTools.FnHCI.UI.fsproj")"
  tests_to_fntools_ref="$(relative_ref \
    "$destination_root/tests/CheddarBooks.Tests" \
    "$fntools_root/src/FnTools.FnHCI.UI/FnTools.FnHCI.UI.fsproj")"
fi

rename_file \
  "$destination_root/src/CheddarBooks.LaundryLog/Nexus.CheddarBooks.LaundryLog.fsproj" \
  "$destination_root/src/CheddarBooks.LaundryLog/CheddarBooks.LaundryLog.fsproj"
rename_file \
  "$destination_root/src/CheddarBooks.LaundryLog.UI/Nexus.CheddarBooks.LaundryLog.UI.fsproj" \
  "$destination_root/src/CheddarBooks.LaundryLog.UI/CheddarBooks.LaundryLog.UI.fsproj"

replace_in_file \
  "$destination_root/src/CheddarBooks.LaundryLog.UI/CheddarBooks.LaundryLog.UI.fsproj" \
  "..\\Nexus.CheddarBooks.LaundryLog\\Nexus.CheddarBooks.LaundryLog.fsproj" \
  "../CheddarBooks.LaundryLog/CheddarBooks.LaundryLog.fsproj"
replace_in_file \
  "$destination_root/src/CheddarBooks.LaundryLog.UI/CheddarBooks.LaundryLog.UI.fsproj" \
  "..\\Nexus.FnHCI.UI\\Nexus.FnHCI.UI.fsproj" \
  "$ui_to_fntools_ref"

replace_in_file \
  "$destination_root/tests/CheddarBooks.Tests/CheddarBooks.Tests.fsproj" \
  "..\\..\\src\\Nexus.CheddarBooks.LaundryLog\\Nexus.CheddarBooks.LaundryLog.fsproj" \
  "../../src/CheddarBooks.LaundryLog/CheddarBooks.LaundryLog.fsproj"
replace_in_file \
  "$destination_root/tests/CheddarBooks.Tests/CheddarBooks.Tests.fsproj" \
  "..\\..\\src\\Nexus.CheddarBooks.LaundryLog.UI\\Nexus.CheddarBooks.LaundryLog.UI.fsproj" \
  "../../src/CheddarBooks.LaundryLog.UI/CheddarBooks.LaundryLog.UI.fsproj"
replace_in_file \
  "$destination_root/tests/CheddarBooks.Tests/CheddarBooks.Tests.fsproj" \
  "..\\..\\src\\Nexus.FnHCI.UI\\Nexus.FnHCI.UI.fsproj" \
  "$tests_to_fntools_ref"

write_file "$destination_root/.gitignore" <<'EOF'
bin/
obj/
.vs/
TestResults/
EOF

write_file "$destination_root/README.md" <<EOF
# CheddarBooks

This repo is the extracted concrete application line for \`CheddarBooks\`, beginning with \`LaundryLog\`.

Bootstrap source:

- source repo: \`NEXUS-EMERGING\`
- source commit: \`$source_commit\`
- bootstrap dependency root: \`$fntools_root\`

Current included projects:

- \`CheddarBooks.LaundryLog\`
- \`CheddarBooks.LaundryLog.UI\`

Current included test project:

- \`CheddarBooks.Tests\`

Start here:

- \`docs/foundation.md\`
- \`docs/laundrylog/introduction.md\`
- \`docs/laundrylog/requirements.md\`
EOF

write_file "$destination_root/bootstrap-source.toml" <<EOF
schema_version = 1
artifact_kind = "cheddarbooks_repo_bootstrap"
source_repo = "NEXUS-EMERGING"
source_commit = "$source_commit"
bootstrap_fntools_root = "$fntools_root"

[[projects]]
path = "src/CheddarBooks.LaundryLog/CheddarBooks.LaundryLog.fsproj"

[[projects]]
path = "src/CheddarBooks.LaundryLog.UI/CheddarBooks.LaundryLog.UI.fsproj"

[[tests]]
path = "tests/CheddarBooks.Tests/CheddarBooks.Tests.fsproj"
EOF

write_file "$destination_root/docs/foundation.md" <<'EOF'
# CheddarBooks Foundation

See:

- `./cheddarbooks-docs/README.md`
- `./laundrylog/introduction.md`
EOF

if [[ "$dry_run" == "true" ]]; then
  printf '[dry-run] dotnet new sln --name CheddarBooks --format slnx --output %s\n' "$destination_root"
  printf '[dry-run] dotnet sln add src/CheddarBooks.LaundryLog/CheddarBooks.LaundryLog.fsproj\n'
  printf '[dry-run] dotnet sln add src/CheddarBooks.LaundryLog.UI/CheddarBooks.LaundryLog.UI.fsproj\n'
  printf '[dry-run] dotnet sln add tests/CheddarBooks.Tests/CheddarBooks.Tests.fsproj\n'
else
  dotnet new sln --name CheddarBooks --format slnx --output "$destination_root" >/dev/null
  dotnet sln "$destination_root/CheddarBooks.slnx" add \
    "$destination_root/src/CheddarBooks.LaundryLog/CheddarBooks.LaundryLog.fsproj" \
    "$destination_root/src/CheddarBooks.LaundryLog.UI/CheddarBooks.LaundryLog.UI.fsproj" \
    "$destination_root/tests/CheddarBooks.Tests/CheddarBooks.Tests.fsproj" >/dev/null
fi

printf 'CheddarBooks repo bootstrap prepared.\n'
printf '  Source commit: %s\n' "$source_commit"
printf '  Destination: %s\n' "$destination_root"
printf '  FnTools root: %s\n' "$fntools_root"
printf '  Dry run: %s\n' "$dry_run"
