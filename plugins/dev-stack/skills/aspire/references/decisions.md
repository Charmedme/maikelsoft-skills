# Aspire decisions

The team decisions that Aspire code needs. The order is the order in which a project meets them. The recommendation is the catalog default. Ask the human before you apply it (stack rule 4).

## Contents

- AHS-01. Aspire version and AppHost form
- AHS-02. Health endpoints outside Development
- AHS-03. Local data between runs
- AHS-04. Deployment target
- AHS-05. Secrets and parameters
- AHS-06. Agent access to the running AppHost
- AHS-07. Tests with the whole AppHost

## AHS-01. Aspire version and AppHost form

- Question: Does the AppHost use Aspire 17 with `Microsoft.NET.Sdk` and `AddDotnetProject`, or does it stay on 13.x with `Aspire.AppHost.Sdk` and `AddProject<>`?
- Ask when: a new AppHost, or the first change to an existing 13.x AppHost after 2026-11-18.
- Options: Aspire 17 with `Microsoft.NET.Sdk`: the current form, no obsolete warnings. Stay on 13.6.5: no change, but each later update gets more work. `Aspire.AppHost.Sdk/17.0.0` (the result of `aspire update`): obsolete (ASPIRE012), only a step in a move.
- Recommended: Aspire 17 with `Microsoft.NET.Sdk` for a new AppHost. An existing 13.x AppHost keeps its form until the human decides on the move. Then do the move as one change with `upgrade-17.md`.
- Choose another option when: a package that the AppHost needs has no 17.0.0 release, or the team must release before it can test the move.
- Depends on: none.
- Revisit when: Aspire 17.1 ships, or Aspire removes `Aspire.AppHost.Sdk`.
- Check: tool (AHS-OLD-SDK, AHS-ADD-PROJECT, ASPIRE012).
- Detect: AHS-DET-01.

## AHS-02. Health endpoints outside Development

- Question: Which health endpoints does a service map outside Development?
- Ask when: the first deployment of a service that uses ServiceDefaults.
- Options: Keep `MapDefaultEndpoints()` for Development only, and map the probes of API-16 (`dev-stack:aspnetcore`) for the platform. Remove the Development check from `MapDefaultEndpoints()`: `/health` and `/alive` in all environments. No probes outside Development.
- Recommended: Keep the Development check. Map the probes of API-16 (`/healthz/live` without checks, `/healthz/ready` with tagged checks) in all environments, and keep them off the public ingress. `/health` shows the state of each dependency to each caller.
- Choose another option when: the platform prescribes paths. Use those paths with the same split.
- Depends on: API-16 of `dev-stack:aspnetcore`.
- Revisit when: the ServiceDefaults template changes its endpoints.
- Check: judge.
- Detect: AHS-DET-02.

## AHS-03. Local data between runs

- Question: Do databases and brokers keep their data between two runs of the AppHost?
- Ask when: the first database or broker in the AppHost.
- Options: `WithDataVolume()`: the data stays, and a migration runs on the old data. No volume: each run starts empty, and seed data must run each time. `WithDataBindMount(path)`: the data is in a folder of the repository or the machine.
- Recommended: `WithDataVolume()` on each database and broker. A developer keeps test data, and the migrations get tested on existing data. Tests with `Aspire.Hosting.Testing` start without the volume when they need empty data.
- Choose another option when: each run must start from the same seed data (a demo, a training).
- Depends on: AHS-01.
- Revisit when: a major version of an engine changes, because an old volume does not start on a new major version.
- Check: judge.
- Detect: AHS-DET-03.

## AHS-04. Deployment target

- Question: Does the AppHost also make the deployment, and to which target?
- Ask when: the first deployment of a service in the AppHost.
- Options: Local runs and tests only: the platform team deploys with its own files. `aspire publish` to Docker Compose (`Aspire.Hosting.Docker`). `aspire publish` or `aspire deploy` to Kubernetes (`Aspire.Hosting.Kubernetes`, Helm 4). Azure Container Apps with azd or `Aspire.Hosting.Azure.AppContainers`.
- Recommended: Local runs and tests only, until the team chooses a target with the platform team. Then use the publisher for that target, and keep the files of `aspire publish` in review like code. A deployment has side effects, so `aspire deploy` and `azd up` run only when a person starts them.
- Choose another option when: the team owns its platform and wants one model for local runs and deployment.
- Depends on: AHS-01.
- Revisit when: the platform changes, or a publisher gets stable support for a feature that the team needs (for example persistent volumes on Kubernetes, ASPIRECOMPUTE002).
- Check: judge.
- Detect: AHS-DET-04.

## AHS-05. Secrets and parameters

- Question: Where do the secret values of the AppHost come from?
- Ask when: the first secret that a resource or a service needs (an API key, a password).
- Options: `AddParameter(name, secret: true)` with the value in the user secrets of the AppHost or from `aspire secret set`. Values in `appsettings.json` of the AppHost. Environment variables on each machine.
- Recommended: Secret parameters, with local values in user secrets or `aspire secret set`, and the platform secret store in a deployment. Let `AddPostgres` and `AddRedis` make their own password. No secret value is in git.
- Choose another option when: the team already has a secret store for development (for example a vault with a CLI).
- Depends on: AHS-01. Also the secret rules of `dev-stack:secure-coding`.
- Revisit when: the team adds a deployment target (AHS-04).
- Check: judge.
- Detect: AHS-DET-05.

## AHS-06. Agent access to the running AppHost

- Question: Does a coding agent get the live state, the logs, and the traces of the AppHost through MCP?
- Ask when: a developer uses a coding agent with the AppHost.
- Options: `aspire agent init --mcp` with project scope: the config uses `aspire agent mcp`. The CLI only (`aspire describe`, `aspire logs`, `aspire otel traces`): no MCP config. No agent access.
- Recommended: `aspire agent init --mcp` with project scope, on the developer machine only. The logs can contain personal data or tokens, so the MCP server does not run in CI or against a shared environment.
- Choose another option when: the team does not allow agents to read logs. Then use no agent access.
- Depends on: none.
- Revisit when: Aspire changes the MCP command again.
- Check: tool (AHS-MCP-START, AHS-DASHBOARD-MCP).
- Detect: AHS-DET-06.

## AHS-07. Tests with the whole AppHost

- Question: Which tests start the whole AppHost with `Aspire.Hosting.Testing`?
- Ask when: the first integration test in a solution with an AppHost.
- Options: `Aspire.Hosting.Testing` for each integration test: the real wiring, but slow and with shared data. `WebApplicationFactory` with Testcontainers for each API, and `Aspire.Hosting.Testing` for a few smoke tests across services. No AppHost tests.
- Recommended: `WebApplicationFactory` with Testcontainers for the tests of one API (TEST-07 of `test-stack:dotnet-testing`). `Aspire.Hosting.Testing` for a few smoke tests: each service starts, gets healthy, and can call its dependencies. Each wait has a timeout.
- Choose another option when: the behavior exists only across services (a message from the API to the worker). Then add an AppHost test for that flow.
- Depends on: TEST-01 and TEST-07 of `test-stack:dotnet-testing`.
- Revisit when: the AppHost tests take more than a few minutes in CI.
- Check: tool (AHS-TEST-DELAY), judge.
- Detect: AHS-DET-07.
