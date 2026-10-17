# Containers and Kubernetes: the security part

Only the code and configuration side: Dockerfiles, manifests, secrets. Image building is in `dev-stack:docker`. Manifests, probes and resources are in `dev-stack:kubernetes`. Decision: SEC-21.

## Contents

- Dockerfile
- Pod security
- Secrets
- Network
- Checks

## Dockerfile

- Each .NET 8 and later Linux image has the user `app` with UID 1654 (`$APP_UID`). Chiseled and distroless images run as `app` by default. Other images run as root until you set `USER`.
- On a non-chiseled image, set `USER $APP_UID` as the last user. Use the number: Kubernetes `runAsNonRoot` cannot verify a user name and gives `CreateContainerConfigError` for `USER app`.
- Prefer `aspnet:10.0-noble-chiseled` (no shell, no package manager, fewer CVEs). Use `-chiseled-extra` when the app needs ICU or time zones.
- Keep port 8080. Port 80 needs root or `NET_BIND_SERVICE`.
- Multi-stage build. Pin the base image to a patch tag or a digest, and let a bot raise the pins.
- No secrets in `ARG` or `ENV`: they stay in the image metadata and history (build check `SecretsUsedInArgOrEnv`). Use a secret mount:

  ```dockerfile
  RUN --mount=type=secret,id=nugetconfig,target=/root/.nuget/NuGet/NuGet.Config \
      dotnet restore --locked-mode
  ```

- `.dockerignore` excludes `.git`, `**/bin`, `**/obj`, `appsettings.Development.json`, `.env`, `*.pfx`, `*.key`.
- Angular static files: `nginxinc/nginx-unprivileged` (port 8080, UID 101), not the official `nginx` image (root, port 80).
- Scan each image in CI and fail on fixable HIGH and CRITICAL: `trivy image --severity HIGH,CRITICAL --ignore-unfixed --exit-code 1 <image>`.

## Pod security

Label each app namespace `pod-security.kubernetes.io/enforce: restricted`. A Pod that meets "restricted":

```yaml
spec:
  automountServiceAccountToken: false   # unless the app calls the Kubernetes API
  securityContext:
    runAsNonRoot: true
    runAsUser: 1654                      # the .NET app user
    runAsGroup: 1654
    fsGroup: 1654
    seccompProfile:
      type: RuntimeDefault
  containers:
    - name: api
      securityContext:
        allowPrivilegeEscalation: false
        readOnlyRootFilesystem: true
        capabilities:
          drop: ["ALL"]
      volumeMounts:
        - name: tmp
          mountPath: /tmp
  volumes:
    - name: tmp
      emptyDir: {}
```

- `readOnlyRootFilesystem: true` needs an `emptyDir` on `/tmp`: the .NET diagnostic port and temp files write there.
- No `privileged`, `hostNetwork`, `hostPID`, `hostIPC` or `hostPath`. Only `NET_BIND_SERVICE` can be added back.
- PodSecurityPolicy is gone since Kubernetes 1.25. Do not write it.

## Secrets

- A Kubernetes `Secret` is only base64, and it is not encrypted in etcd unless the cluster turns on encryption at rest.
- Keep secrets in an external store (Azure Key Vault, AWS Secrets Manager, Vault). Sync them with External Secrets Operator or the Secrets Store CSI Driver (OPS-10).
- Never put a connection string in a `ConfigMap`. Never commit a `Secret` manifest with values.
- Prefer secret files over environment variables when the store supports rotation: file mounts refresh, environment values do not.

## Network

- A default-deny NetworkPolicy per namespace for ingress and egress. Then allow: gateway to API on 8080, API to the database, and DNS (UDP and TCP 53). Without the DNS rule, each name lookup fails.
- An egress policy is the second layer against SSRF: the app cannot reach cloud metadata or internal services that it does not need.
- ingress-nginx is retired since March 2026 and gets no security patches. Use a Gateway API implementation for new work.

## Checks

| Check | Tool |
|-------|------|
| No privileged Pod or host namespaces | `tool (SC-K8S-PRIVILEGED)` |
| No secrets in `ARG` or `ENV` | `tool (SC-DOCKER-SECRET-ARG)` |
| Misconfiguration in Dockerfiles and manifests | `trivy config .` |
| Last user is not root | hadolint DL3002 |
| Restricted fields complete | kube-linter `run-as-non-root`, `privilege-escalation-container`, `no-read-only-root-fs` |
| Admission accepts the Pod | `kubectl apply --dry-run=server` against a real cluster |
