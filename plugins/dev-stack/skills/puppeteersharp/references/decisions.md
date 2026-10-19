# PuppeteerSharp decisions

The team decision that a Chrome renderer needs. The recommendation is the catalog default. Ask the human before you apply it (stack rule 4). OPS-03 (the container base image) belongs to the Docker skill. Its catalog row already says that a headless Chrome renderer gets its own image on full `aspnet:10.0`.

## SEC-24. Chrome `--no-sandbox`

- Question: May Chrome run without its sandbox (`--no-sandbox`, `--disable-setuid-sandbox`, `PUPPETEER_DANGEROUS_NO_SANDBOX`)?
- Ask when: the first Dockerfile, compose file, or launch option for Chrome, or the first time Chrome does not start in a container.
- Options: Sandbox on: a non-root user and a seccomp profile that allows `clone`, `clone3`, and `unshare`. Sandbox off with an ADR: the container is the only isolation. Sandbox off without a decision: the common fix in examples on the internet.
- Recommended: Not allowed by default. Allowed only with an ADR that names the isolation (non-root user, read-only file system, no secrets in the container, egress blocked), and only for trusted templates without user text. The sandbox is the barrier between a renderer exploit and the container.
- Choose another option when: the platform cannot run a custom seccomp profile and cannot allow user namespaces, and the renderer only renders own templates. Then write the ADR.
- Depends on: OPS-03 (base image), SEC-21 (workload hardening).
- Revisit when: the platform supports a seccomp profile or user namespaces for the renderer.
- Check: tool (PPTR-NO-SANDBOX), judge for the ADR.
- Detect: PPTR-DET-SEC-24.
