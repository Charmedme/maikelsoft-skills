#!/usr/bin/env bash
# CI gate: analyzers and unused code.
set -euo pipefail
dotnet tool restore
dotnet build Orders.slnx
dotnet roslynator analyze Orders.slnx --severity-level warning
dotnet roslynator find-unused Orders.slnx
