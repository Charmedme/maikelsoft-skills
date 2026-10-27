# Supply chain

Software Supply Chain Failures is A03 in the OWASP Top 10:2025: few data hits, but the highest average exploit and impact scores. Decision: SEC-15.

## Contents

- Before you add a package
- NuGet
- npm and pnpm
- CI workflows
- Scanners are dependencies too
- SBOM
- Never write

## Before you add a package

LLMs invent package names: at least 5.2% of the names from commercial models and 21.7% from open models in one study (576,000 samples). Attackers register such names ("slopsquatting").

1. Look up the exact name on nuget.org or npmjs.com. Done when you have the URL of the package page.
2. Check the owner, the repository link, the first release date, the download count and the last release. A new package with a name close to a popular one is a warning sign.
3. Check the license, and if the license changed in a recent major version.
4. Prefer a package that the solution already uses, or a framework feature.
5. Tell the human the name, version, license and reason, and wait for the approval (stack rule 5).

## NuGet

- NuGetAudit is on by default since .NET 8. For `net10.0` projects, `NuGetAuditMode` defaults to `all` (transitive packages too).
- Treat NU1903 (high) and NU1904 (critical) as errors in CI:

  ```xml
  <PropertyGroup>
    <WarningsAsErrors>$(WarningsAsErrors);NU1903;NU1904</WarningsAsErrors>
  </PropertyGroup>
  ```

- Do not set `<NuGetAudit>false</NuGetAudit>`. Suppress one advisory with a reason:

  ```xml
  <ItemGroup>
    <!-- Not reachable: we do not parse PKCS#7 input. Remove when the base image has 10.0.14. -->
    <NuGetAuditSuppress Include="https://github.com/advisories/GHSA-xxxx-xxxx-xxxx" />
  </ItemGroup>
  ```

- Report and fix:

  ```
  dotnet package list --vulnerable --include-transitive --format json
  dotnet package update --vulnerable
  ```

  `dotnet package update --vulnerable` (NuGet 7, .NET 10 SDK) raises a vulnerable transitive package to a fixed version with a direct reference. Tested on 2026-10-16: it raised `System.Security.Cryptography.Pkcs` 10.0.0 to 10.0.13.
- Lock files for apps: `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>`, commit `packages.lock.json`, and restore in CI with `dotnet restore --locked-mode`.
- Central Package Management (`Directory.Packages.props`) gives one version per package.
- Package Source Mapping in `nuget.config` when you use a private feed. It stops dependency confusion (a public package with the name of an internal one). It applies to restore only, not to packages that are already in the global packages folder.

  ```xml
  <packageSourceMapping>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
    <packageSource key="company">
      <package pattern="Company.*" />
    </packageSource>
  </packageSourceMapping>
  ```

- NuGet has no release-age setting. Use the cooldown of Dependabot or Renovate.
- SecurityCodeScan has no release since 2022. Do not add it.

## npm and pnpm

- Commit `package-lock.json`. Use `npm ci` in CI and in Dockerfiles, never `npm install`.
- Run `npm audit --omit=dev` and `npm audit signatures`.
- Cooldown: npm 11.10 and later, `min-release-age=3` (days) in `.npmrc`. pnpm 10.16 and later, `minimumReleaseAge` (minutes, default 1440 since pnpm 11).
- Install scripts: pnpm blocks dependency build scripts until you approve them (`allowBuilds`). With npm, set `ignore-scripts=true` and allow the few packages that need a script.
- Dependabot or Renovate with grouped updates and a cooldown. The Dependabot `cooldown` default is 3 days for version updates. It does not delay security updates.

## CI workflows

- Pin each action to a full commit SHA, with the version in a comment:

  ```yaml
  - uses: actions/checkout@08eba0b27e820071cde6df949e0beb9ba4906955 # v4.3.0
  ```

  A tag can move. In March 2026 attackers force-pushed about 75 tags of `aquasecurity/trivy-action`.
- Set `permissions:` to the least rights at the workflow level (`contents: read`) and raise them per job.
- Do not give secrets to workflows that run for pull requests from forks. Avoid `pull_request_target` with a checkout of the pull request code.
- Use OIDC federation to the cloud instead of long-lived keys (SEC-07).
- Let Dependabot update the SHAs (`package-ecosystem: "github-actions"`).

## Scanners are dependencies too

On 2026-03-19, Trivy itself was compromised: the v0.69.4 binary was malicious and the `trivy-action` tags pointed to malware (CVE-2026-33634). The scanners run with the secrets of the pipeline.

- Pin each scanner by version and checksum, or by image digest. Current Trivy: 0.76.1 (2026-10-21).
- Verify the signature or checksum of a downloaded binary.
- When a pipeline ran Trivy 0.69.4 or a moved `trivy-action` tag, rotate each secret that the pipeline could read.

## SBOM

Produce a CycloneDX or SPDX SBOM per build, for example `trivy fs --format cyclonedx --output sbom.json .` or Microsoft `sbom-tool`. Keep it with the release.

## Never write

| Wrong | Correct |
|-------|---------|
| A package name from memory | Look it up on the registry first |
| `<NuGetAudit>false</NuGetAudit>` | `NuGetAuditSuppress` per advisory, with a reason |
| `npm install` in CI | `npm ci` |
| `uses: actions/checkout@v4` | `uses: actions/checkout@<sha> # v4.x.y` |
| `aquasec/trivy:latest` | `aquasec/trivy:0.76.1@sha256:<digest>` |
| `dotnet list package --vulnerable` (still works) | `dotnet package list --vulnerable --include-transitive` |
