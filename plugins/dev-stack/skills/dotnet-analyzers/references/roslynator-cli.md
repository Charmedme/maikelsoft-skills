# Roslynator CLI

## Contents

- Install
- The Roslyn limit
- Commands
- Exit codes
- Commands that do not exist

## Install

```bash
dotnet tool install -g roslynator.dotnet.cli --version 1.0.1
roslynator --version
```

Version 1.0.1 (2026-10-04) runs on .NET 10 and uses Roslyn 5.0.0. It contains no analyzers. It loads the analyzers that the project references and reads the severities from `.editorconfig`. On a machine where `dotnet` is not in `/usr/share/dotnet`, set `DOTNET_ROOT`, else the tool does not start.

## The Roslyn limit

The .NET SDK 10.0.402 has Roslyn 5.9. An analyzer package can ship builds for many Roslyn versions, and NuGet picks the newest build that the SDK supports. Meziantou.Analyzer 3.0.303 then gives its Roslyn 5.9 build. The CLI on Roslyn 5.0 cannot load it. It skips the analyzer without an error: only `-v diag` shows "Cannot load ... types". `roslynator analyze` then reports a false clean for those rules.

Rules:

- Use `dotnet build -warnaserror` as the gate. Never the CLI.
- Use the CLI for the jobs that only it does: `fix` for named IDs, `find-symbol --unused`, and `spellcheck`.

## Commands

| Job | Command |
|---|---|
| Fix named IDs | `roslynator fix <proj> --supported-diagnostics CA1510 CA1829 CA2016` |
| List findings at warning and up | `roslynator analyze <proj> --severity-level warning` |
| Find unused private methods and fields | `roslynator find-symbol <proj> --unused --visibility private --symbol-kind method field` |
| Remove them | Add `--remove` to the `find-symbol` command. Build and test after it. |
| Spelling of names and comments | `roslynator spellcheck <proj> --words <word list files> --dry-run` |
| Move old config to 5.0 keys | `roslynator migrate <path>` |

Output of `find-symbol` (tested):

```text
method Shop.BadService.Hidden()
method Shop.BadService.Unused()

2 method symbols

2 symbols found
```

Output of `fix` (tested):

```text
Unfixed diagnostics:
  1 RCS1155 Use StringComparison when comparing strings

Fixed diagnostics:
  1 CA2016    Forward the 'CancellationToken' parameter to methods
  1 VSTHRD100 Avoid async void methods

2 diagnostics fixed
```

`spellcheck` needs `--words` with word list files, for example from the wordb repository. Without it, the command stops with "Required option 'words' is missing".

## Exit codes

| Command | Exit code |
|---|---|
| `analyze` | 1 when it finds a diagnostic at or above `--severity-level` (default `info`). 0 when it finds none. Add `--return-success-on-diagnostics` for a report only. |
| `fix` | 0, also when some IDs are "Unfixed" or "Unfixable". Read the output. |
| `find-symbol` | 0. Read the "symbols found" line. |

## Commands that do not exist

| Invented | Use |
|---|---|
| `roslynator find-unused` | `roslynator find-symbol --unused` |
| `roslynator format` | `dotnet format` |
| `roslynator analyze --fix` | `roslynator fix` |

Roslynator 5.0 removed old rules and keys. RCS1008, RCS1009, RCS1010, RCS1012, RCS1176, and RCS1177 became RCS1264. RCS1036 became RCS0063. RCS1063, RCS1064, and RCS1065 became RCS1252. The keys `roslynator.max_line_length` and `roslynator.prefix_field_identifier_with_underscore` became `roslynator_max_line_length` and `roslynator_prefix_field_identifier_with_underscore`. The VS extension has no analyzers since 5.0: use the NuGet package.
