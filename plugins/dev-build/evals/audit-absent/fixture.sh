#!/usr/bin/env bash
set -euo pipefail
repo="$(cd "$(dirname "$(readlink -f "$0")")/../../../.." && pwd)"
bash "$repo/evals/dev-build/fixtures/audit-plugin/make.sh" .
