# CI steps

Nobody tested these files in a real pipeline. The commands ran on the local SDK.

## Order

1. `dotnet restore`. NuGetAudit runs here (NU1901 to NU1904).
2. `dotnet format --verify-no-changes --no-restore`. Exit 2 when a file needs a change.
3. `dotnet build -warnaserror --no-restore`. `-warnaserror` covers the compiler, NuGet, and MSBuild warnings. `TreatWarningsAsErrors` alone covers the compiler only.
4. `dotnet test --no-build`.
5. Optional: upload the `obj/*.sarif` files as a build artifact, or to GitHub code scanning.

## GitHub Actions

```yaml
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5
      - uses: actions/setup-dotnet@v5
        with:
          global-json-file: global.json
      - run: dotnet restore
      - run: dotnet format --verify-no-changes --no-restore
      - run: dotnet build -warnaserror --no-restore
      - run: dotnet test --no-build
```

GitHub Actions sets `CI=true`, so `TreatWarningsAsErrors` of the baseline is on too. Check the action versions before you use them (stack rule 2).

## Azure Pipelines

```yaml
steps:
  - task: UseDotNet@2
    inputs:
      useGlobalJson: true
  - script: dotnet restore
  - script: dotnet format --verify-no-changes --no-restore
  - script: dotnet build -warnaserror --no-restore
  - script: dotnet test --no-build
```

Azure Pipelines sets the variable `TF_BUILD`. It also sets `CI=true` on hosted agents [I]. When you are not sure, pass `-warnaserror`: it does not depend on the variable.

## Vulnerability audit (QUAL-13)

`-warnaserror` turns NU1901 to NU1904 into errors in the restore of the build. To suppress one advisory, add the advisory URL with a reason:

```xml
<ItemGroup>
  <!-- Not reachable: we do not call the affected API. Approved by <human>, <date>. -->
  <NuGetAuditSuppress Include="https://github.com/advisories/<id>" />
</ItemGroup>
```

When an advisory must not break the build at once, use `<WarningsNotAsErrors>$(WarningsNotAsErrors);NU1901;NU1902</WarningsNotAsErrors>` for the low and moderate levels.
