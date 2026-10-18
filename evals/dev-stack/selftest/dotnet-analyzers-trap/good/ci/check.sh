#!/usr/bin/env bash
# CI gate. 'dotnet build -warnaserror' is the gate: it runs every analyzer with the Roslyn of the SDK.
# 'roslynator analyze' uses an older Roslyn and skips newer analyzers (Meziantou) without an error, so it is not the gate.
set -euo pipefail
dotnet tool restore
dotnet build Orders.slnx -warnaserror --no-incremental
# Report only: unused private members (IDE0051 in the build is the gate).
dotnet roslynator find-symbol Orders.slnx --unused --visibility private --symbol-kind method field property || true
