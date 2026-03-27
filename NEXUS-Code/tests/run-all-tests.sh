#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")"/../.. && pwd)"

dotnet build "$repo_root/NEXUS-Code/NEXUS-Code.slnx"
dotnet run --no-build --project "$repo_root/NEXUS-Code/tests/Nexus.Foundation.Tests/Nexus.Foundation.Tests.fsproj"
dotnet run --no-build --project "$repo_root/NEXUS-Code/tests/FnTools.Tests/FnTools.Tests.fsproj"
dotnet run --no-build --project "$repo_root/NEXUS-Code/tests/CheddarBooks.Tests/CheddarBooks.Tests.fsproj"
