#!/usr/bin/env bash
# Copy the notes plugin to <target>/notes and make it a git repository, so a check can see each change.
set -euo pipefail
here="$(cd "$(dirname "$(readlink -f "$0")")" && pwd)"
cp -r "$here/notes" "$1/notes"
cd "$1/notes"
git -c init.defaultBranch=main init -q
git -c user.name=Dev -c user.email=dev@example.com add -A
git -c user.name=Dev -c user.email=dev@example.com -c commit.gpgsign=false commit -qm "notes 1.2.0"
