#!/usr/bin/env bash
set -euo pipefail
repo="$(cat "$(dirname "$0")/../.repo-root" 2>/dev/null || (cd "$(dirname "$0")/../../../.." && pwd))"
cp -r "$repo/evals/dev-build/fixtures/eval-plugin/hello" ./hello
mkdir -p evals && cp -r "$repo/evals/dev-build/fixtures/eval-plugin/old-evals/hello" ./evals/hello
