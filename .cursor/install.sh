#!/usr/bin/env bash
# Idempotent Cloud Agent setup for Songify.
#
# Songify is a Windows-only WPF (.NET Framework 4.8) desktop application, so it
# cannot be built or run on the Linux Cloud Agent VM. This script prepares the
# maximum achievable Linux development experience: the .NET SDK toolchain plus a
# full NuGet restore, which gives the editor/language server the complete
# dependency graph for navigation, analysis, and editing.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

# 1. Ensure the .NET SDK is available (idempotent: apt is a no-op if installed).
if ! command -v dotnet >/dev/null 2>&1; then
  echo "Installing .NET SDK 8.0..."
  sudo DEBIAN_FRONTEND=noninteractive apt-get update -y
  sudo DEBIAN_FRONTEND=noninteractive apt-get install -y dotnet-sdk-8.0
else
  echo ".NET SDK already present: $(dotnet --version)"
fi

# Keep first-run noise and telemetry out of automated setup.
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# 2. Restore NuGet packages so the dependency graph is ready for tooling.
echo "Restoring NuGet packages..."
dotnet restore "Songify Slim.sln"

echo "Setup complete. NOTE: building/running Songify requires Windows (WPF/.NET"
echo "Framework 4.8); the Linux VM supports editing, NuGet restore, and analysis."
