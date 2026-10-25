# Container image and sandbox

The Dockerfile, the seccomp profile, and the Kubernetes settings for a Chrome renderer.

## Contents

- Dockerfile
- Why each line is there
- The sandbox and seccomp
- Kubernetes
- Separate renderer service

## Dockerfile

```dockerfile
# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Orders.Api/Orders.Api.csproj -c Release -o /app

# chrome-headless-shell, pinned to Chrome.DefaultBuildId of PuppeteerSharp 25.15.0.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS chrome
ARG CHROME_BUILD=155.0.8059.84
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl unzip \
    && curl -fsSLo /tmp/shell.zip "https://storage.googleapis.com/chrome-for-testing-public/${CHROME_BUILD}/linux64/chrome-headless-shell-linux64.zip" \
    && unzip -q /tmp/shell.zip -d /opt \
    && rm /tmp/shell.zip

# Full aspnet image (not chiseled): Chrome needs system libraries and fonts.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update \
    && apt-get install -y --no-install-recommends tini fonts-liberation fonts-noto-core \
       libasound2t64 libatk-bridge2.0-0t64 libatk1.0-0t64 libcups2t64 libdbus-1-3 libgbm1 \
       libnss3 libpango-1.0-0 libxcomposite1 libxdamage1 libxfixes3 libxkbcommon0 libxrandr2 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=chrome /opt/chrome-headless-shell-linux64 /opt/chrome-headless-shell
ENV PUPPETEER_EXECUTABLE_PATH=/opt/chrome-headless-shell/chrome-headless-shell \
    LANG=C.UTF-8
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["/usr/bin/tini", "--", "dotnet", "Orders.Api.dll"]
```

Checked on 2026-10-19: the runtime stage builds on `aspnet:10.0` (Ubuntu 24.04), and Chrome renders a PDF as user `app` (uid 1654) with the sandbox on, under the seccomp profile below.

## Why each line is there

- Separate `chrome` stage: `curl` and `unzip` do not go into the final image.
- `CHROME_BUILD`: the build of `Chrome.DefaultBuildId`. Change it with the package version. The zip lists its Debian dependencies in `deb.deps`.
- The `t64` package names are the Ubuntu 24.04 names. A Debian base uses the names without `t64`.
- `tini`: Chrome starts child processes. Without an init process, they become zombies when the parent ends. `docker run --init` is the alternative.
- `fonts-liberation`, `fonts-noto-core`, `LANG`: without fonts, text renders as boxes or with fallback glyphs.
- `USER $APP_UID`: the .NET images define the `app` user (1654). The image default is root.
- `PUPPETEER_EXECUTABLE_PATH`: PuppeteerSharp reads it when `LaunchOptions.ExecutablePath` is null.

## The sandbox and seccomp

Checked on 2026-10-19, Docker with its default seccomp profile:

| Setup | Result |
|-------|--------|
| root, sandbox on | "Running as root without --no-sandbox is not supported" |
| user `app`, default seccomp profile | "No usable sandbox!" |
| user `app`, default profile plus `clone`, `clone3`, `unshare` allowed | PDF rendered, sandbox on |
| user `app`, default profile plus `clone`, `clone3` only | "No usable sandbox!" |

Chrome's sandbox makes user namespaces. Docker's default profile allows `clone` with namespace flags, and `unshare`, only with `CAP_SYS_ADMIN`. Make a profile:

1. Take the default profile of your Docker version (`profiles/seccomp/default.json` in the moby repository).
2. Add one entry: `{"names": ["clone", "clone3", "unshare"], "action": "SCMP_ACT_ALLOW"}`.
3. Run the renderer with `docker run --security-opt seccomp=chrome.json`, or `security_opt: ["seccomp=chrome.json"]` in compose.

Do not use these instead: `--no-sandbox` (SEC-24), `--cap-add SYS_ADMIN` (much wider than the sandbox needs), `seccomp=unconfined` (removes all syscall filters).

A host with an AppArmor rule against unprivileged user namespaces (Ubuntu 23.10 and later) needs an AppArmor profile for Chrome too. Chrome names this case in its error message.

## Kubernetes

- `securityContext.runAsNonRoot: true`, `runAsUser: 1654`, `allowPrivilegeEscalation: false`, `capabilities.drop: ["ALL"]`.
- `seccompProfile: { type: Localhost, localhostProfile: profiles/chrome.json }`, with the profile on each node. The `RuntimeDefault` profile blocks the sandbox like the Docker default (inferred: the container runtimes ship a similar profile; prove it with the smoke test).
- Memory: each open page is a renderer process. Measure the memory of the browser plus the maximum number of pages under load. Set the memory request equal to the limit (OPS-13) above that value.
- `readOnlyRootFilesystem: true` needs a writable `emptyDir` on `/tmp` for the Chrome profile folder.
- A default-deny egress NetworkPolicy for the renderer (SEC-21). The renderer needs no egress when all assets are inline.

## Separate renderer service

Put the renderer in its own container when the HTML holds user text: its own image on full `aspnet:10.0`, no database credentials, no egress. The API keeps its chiseled image and calls the renderer over HTTP or a queue. For a small internal tool with own templates only, one image is fine.
