# Decisions: migrate FluentAssertions

This skill carries out one team decision. The full catalog of test decisions is in `dev-stack:dotnet-testing` (`references/decisions.md` of that skill).

## TEST-02. Assertion library

- Question: For new test projects, do we use AwesomeAssertions 9, FluentAssertions 7.2.2, or plain MSTest `Assert`? For this skill: do we move this existing project from FluentAssertions to AwesomeAssertions 9?
- Ask when: before step 4 of the procedure, together with the package approval.
- Options: AwesomeAssertions 9: Apache-2.0 fork of FluentAssertions, FA 8.0 API, namespace `AwesomeAssertions`, MSTest 4 support since 9.3. FluentAssertions 7.2.2: last Apache-2.0 line, fixes only, needs the MSTest 4 bridge. FluentAssertions 8: commercial Xceed license for company use, not allowed. MSTest `Assert` only: no extra package, no object graph comparison.
- Recommended: AwesomeAssertions 9 for new repos. Keep FluentAssertions 7.2.2, pinned `[7.2.2,8.0.0)`, in repos that already use FA. The team supports both. Migrate an existing repo when the team wants the current API, no bridge, and a library that gets new features.
- Choose another option when: the reasons in "When not to migrate" in `SKILL.md` apply. Then keep FluentAssertions 7.2.2 with the bridge.
- Depends on: TEST-01.
- Revisit when: AwesomeAssertions has no release for a year.
- Check: tool (MFA-PKG-FA-LEFT, MFA-USING-FA)
- Detect: MFA-USING-FA (FluentAssertions still in use). `dotnet-testing` has the detect rule TST-DET-TEST-02.
