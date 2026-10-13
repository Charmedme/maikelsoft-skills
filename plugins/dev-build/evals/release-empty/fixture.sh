#!/usr/bin/env bash
set -euo pipefail
repo="$(cat "$(dirname "$0")/../.repo-root" 2>/dev/null || (cd "$(dirname "$0")/../../../.." && pwd))"
bash "$repo/evals/dev-build/fixtures/release-repo/make.sh" . empty
