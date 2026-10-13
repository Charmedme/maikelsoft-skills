# CI for a plugin marketplace

Read this file when the repository has no tag workflow or no version check. Show the human the change and add it only when the human agrees.

## Contents

- Tag workflow
- Version check

## Tag workflow

This workflow runs on each push to `main`. For each plugin, it makes the tag `<plugin>--v<version>` when that tag does not exist yet.

Save it as `.github/workflows/tag.yml`:

```yaml
name: Tag releases

on:
  push:
    branches: [main]

permissions:
  contents: write

jobs:
  tag:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - uses: actions/setup-node@v4
        with:
          node-version: "22"

      - name: Install Claude Code
        run: npm install -g @anthropic-ai/claude-code

      - name: Tag each plugin version that has no tag
        run: |
          git config user.name "github-actions[bot]"
          git config user.email "41898282+github-actions[bot]@users.noreply.github.com"
          for p in plugins/*/; do
            manifest="$p.claude-plugin/plugin.json"
            name=$(python3 -c "import json,sys; print(json.load(open(sys.argv[1]))['name'])" "$manifest")
            version=$(python3 -c "import json,sys; print(json.load(open(sys.argv[1]))['version'])" "$manifest")
            tag="$name--v$version"
            if git rev-parse -q --verify "refs/tags/$tag" > /dev/null; then
              echo "exists: $tag"
              continue
            fi
            claude plugin tag "$p" --dry-run
            commit=$(git log -1 --format=%H -G'"version"' -- "$manifest")
            git tag -a "$tag" "$commit" -m "$name $version"
            git push origin "$tag"
          done
```

`claude plugin tag --dry-run` checks the manifests and stops the job when they do not agree. The tag points at the last commit that changed `version` in `plugin.json`. So a tag for an old release also points at the correct commit.

## Version check

This job fails a pull request that changes a plugin without a new version. Changes in `plugins/<plugin>/evals/` need no new version, because they do not reach the user. Add the job to the pull request workflow:

```yaml
  version:
    if: github.event_name == 'pull_request'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Each changed plugin has a new version
        run: python3 plugins/dev-build/skills/release-plugin/scripts/release.py bump-check --base origin/${{ github.base_ref }}
```

Change the path of `release.py` when the skill is in a different folder.
