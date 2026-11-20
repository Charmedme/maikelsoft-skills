# .NET 11 decisions

The team decisions that a move to .NET 11 needs. The order is the order in which a project meets them. The recommendation is the catalog default. Ask the human before you apply it (stack rule 4).

## Contents

- NET-01. Move to .NET 11
- NET-02. Runtime async in own assemblies
- NET-03. Target frameworks of a library package

## NET-01. Move to .NET 11

- Question: Do we move this project from `net10.0` (LTS) to `net11.0` (STS)?
- Ask when: the agent wants a C# 15 feature or a .NET 11 API.
- Answered when: the human asks for the move to .NET 11. The answer is "move". Do not ask it again.
- Options: Stay on `net10.0`: LTS, support until 2028-11-14, all packages have GA releases. Move to `net11.0`: C# 15 (`union`, `closed`), runtime async, EF Core 11, ASP.NET Core 11. Support also ends on 2028-11-14.
- Recommended: Stay on `net10.0` unless the task names a .NET 11 feature that the team needs. The support time is the same, so a move costs work and gives only features. Check the blockers first (`SKILL.md`, table in "First").
- Choose another option when: the team needs a C# 15 feature (for example `union` for result types) or an EF Core 11 feature. A measured performance gain is also a reason.
- Depends on: none.
- Revisit when: EFCore.PG 11 and `Asp.Versioning.OpenApi` for `Microsoft.OpenApi` 3.x have GA releases, or .NET 12 ships.
- Check: judge.
- Detect: N11-DET-NET-01.

## NET-02. Runtime async in own assemblies

- Question: Do we turn on runtime async for the assemblies of the project?
- Ask when: the project targets `net11.0`, and a task asks for faster async code or shorter stack traces.
- Options: Off (the default): the .NET libraries and ASP.NET Core use runtime async, the own code uses the compiler state machine. On: `<Features>runtime-async=on</Features>` in the project or in `Directory.Build.props`.
- Recommended: Off. Turn it on in one project when a measurement shows a gain, and the tests pass. The C# rules of async code do not change. A build on SDK 11.0.100 showed that the feature works without `EnablePreviewFeatures`.
- Choose another option when: a profile shows async overhead in a hot path, or the team needs shorter stack traces in the logs.
- Depends on: NET-01.
- Revisit when: a later SDK turns it on by default.
- Check: judge.
- Detect: N11-DET-NET-02.

## NET-03. Target frameworks of a library package

- Question: Does a library package that other apps use target `net11.0` only, or `net10.0;net11.0`?
- Ask when: a NuGet package of the team moves to .NET 11.
- Options: `net11.0` only: C# 15 in the code, but apps on .NET 10 cannot use the package. `net10.0;net11.0`: apps on both versions can use it, but the code stays on C# 14 features.
- Recommended: `net10.0;net11.0` while one app that uses the package stays on .NET 10. Keep the `Microsoft.Extensions.*` references, because two targets give no NU1510.
- Choose another option when: each app that uses the package targets `net11.0`.
- Depends on: NET-01.
- Revisit when: the last app on .NET 10 moves.
- Check: judge.
- Detect: N11-DET-NET-03.
