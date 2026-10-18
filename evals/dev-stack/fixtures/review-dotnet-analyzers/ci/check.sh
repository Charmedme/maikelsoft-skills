#!/usr/bin/env bash
# Quality gate for pull requests.
set -euo pipefail

dotnet build Orders.slnx -p:ErrorLog=build.sarif
roslynator analyze Orders.slnx
dotnet test
