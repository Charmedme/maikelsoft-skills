#!/usr/bin/env bash
# CI gate: the Roslynator CLI is the gate (this misses Meziantou with CLI 1.0.1).
set -euo pipefail
dotnet tool restore
dotnet build Orders.slnx
dotnet roslynator analyze Orders.slnx --severity-level warning
dotnet roslynator find-symbol Orders.slnx --unused --visibility private || true
