#!/usr/bin/env bash
set -euo pipefail
repo="$(cd "$(dirname "$(readlink -f "$0")")/../../../.." && pwd)"
cp -r "$repo/evals/dev-build/fixtures/eval-plugin/hello" ./hello
