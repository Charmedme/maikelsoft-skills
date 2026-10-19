#!/bin/sh
# Test-only wrapper for the scorer. PUPPETEER_EXECUTABLE_PATH points here; EVAL_CHROME names the real
# chrome-headless-shell 155.0.8059.79. Chrome refuses to start as root with the sandbox on, so the
# wrapper adds --no-sandbox only when the scorer runs as root. The app code keeps the sandbox on.
if [ "$(id -u)" = "0" ]; then
    exec "${EVAL_CHROME:?set EVAL_CHROME to chrome-headless-shell}" --no-sandbox "$@"
fi
exec "${EVAL_CHROME:?set EVAL_CHROME to chrome-headless-shell}" "$@"
