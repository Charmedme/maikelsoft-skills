#!/usr/bin/env python3
"""Run the browser tests of one Angular probe against a running app and mock API.

    python3 harness.py <probe.py> --base-url http://127.0.0.1:<port> --out results.json [--test <name> ...]

The probe module has TESTS (the test names, in order), optional CONTEXT (extra options for
browser.new_context, for example timezone_id), and one function for each test name that takes a Probe.
Before each test, the harness calls POST /__reset on the mock API. A test passes when its function
returns without an exception.

The results file is {test name: {"outcome": "Passed" | "Failed", "message": text, "seconds": n}}.
axe-core 4.14.0 (axe.min.js, MPL-2.0, see axe-core.LICENSE.txt) is in this folder.
"""
import argparse
import importlib.util
import json
import pathlib
import re
import sys
import time
import traceback
import urllib.request

from playwright.sync_api import sync_playwright

HERE = pathlib.Path(__file__).resolve().parent
AXE = HERE / "axe.min.js"
DEFAULT_TIMEOUT_MS = 15000


class ProbeFailure(AssertionError):
    pass


def check(condition, message):
    if not condition:
        raise ProbeFailure(message)


_PAGES = []


def pause(seconds):
    """Wait, and let Playwright handle the events of the page (time.sleep blocks them, so page.url gets stale)."""
    live = [p for p in _PAGES if not p.is_closed()]
    if live:
        live[-1].wait_for_timeout(seconds * 1000)
    else:
        time.sleep(seconds)


def wait_until(fn, seconds=5.0, interval=0.1):
    """Call fn until it returns a true value or the time is over. Return the last value."""
    end = time.monotonic() + seconds
    value = fn()
    while not value and time.monotonic() < end:
        pause(interval)
        value = fn()
    return value


class Api:
    def __init__(self, base):
        self.base = base.rstrip("/")

    def call(self, method, path, body=None, headers=None):
        data = None if body is None else json.dumps(body).encode()
        req = urllib.request.Request(self.base + path, data=data, method=method,
                                     headers={"Content-Type": "application/json", **(headers or {})})
        try:
            with urllib.request.urlopen(req, timeout=10) as resp:
                raw = resp.read()
                return resp.status, json.loads(raw) if raw else None
        except urllib.error.HTTPError as e:
            raw = e.read()
            return e.code, json.loads(raw) if raw else None

    def reset(self):
        self.call("POST", "/__reset")

    def requests(self, method=None, path=None):
        _, log = self.call("GET", "/__requests")
        return [r for r in log or [] if (method is None or r["method"] == method)
                and (path is None or r["path"].split("?")[0].rstrip("/") == path)]

    def fail_next(self, path, status, method=None, times=1):
        self.call("POST", "/__fail", {"path": path, "status": status, "method": method, "times": times})

    def touch(self, product_id, changes=None):
        return self.call("POST", f"/__touch/{product_id}", changes)


def header(request, name):
    """Value of a logged request header, found without regard to case."""
    for k, v in request.get("headers", {}).items():
        if k.lower() == name.lower():
            return v
    return None


class Probe:
    """What a test gets: the base URL, the mock API, and new pages."""

    def __init__(self, browser, base, context_options):
        self.browser = browser
        self.base = base.rstrip("/")
        self.api = Api(base)
        self.context_options = context_options
        self.contexts = []
        self.console_errors = []

    def page(self, width=1440, height=900):
        context = self.browser.new_context(viewport={"width": width, "height": height}, **self.context_options)
        context.set_default_timeout(DEFAULT_TIMEOUT_MS)
        self.contexts.append(context)
        page = context.new_page()
        _PAGES.append(page)
        page.on("pageerror", lambda e: self.console_errors.append(str(e)))
        return page

    def goto(self, page, path):
        page.goto(self.base + path, wait_until="load", timeout=30000)
        return page

    def close(self):
        for c in self.contexts:
            c.close()
        _PAGES.clear()


# --- Helpers for the probes --------------------------------------------------------------

def sideways_overflow(page):
    """Pixels that the page scrolls sideways (0 is good)."""
    return page.evaluate("""() => Math.max(document.documentElement.scrollWidth, document.body.scrollWidth)
                                   - document.documentElement.clientWidth""")


def check_no_sideways_scroll(page, where):
    over = sideways_overflow(page)
    check(over <= 1, f"{where}: the page scrolls {over} px sideways at {page.viewport_size['width']} px")


def axe_violations(page, impacts=("serious", "critical")):
    page.add_script_tag(content=AXE.read_text(encoding="utf-8"))
    result = page.evaluate("""async () => {
        const r = await axe.run(document, {resultTypes: ['violations']});
        return r.violations.map(v => ({id: v.id, impact: v.impact,
            targets: v.nodes.slice(0, 3).map(n => n.target.join(' '))}));
    }""")
    return [v for v in result if v["impact"] in impacts]


def check_axe(page, where):
    bad = axe_violations(page)
    check(not bad, f"{where}: axe " + "; ".join(f"{v['id']} ({v['impact']}) at {', '.join(v['targets'])}" for v in bad))


def accessible_name(locator):
    """Accessible name of the element, from its ARIA snapshot ('- dialog "Delete product?"')."""
    first = (locator.aria_snapshot() or "").splitlines()[0] if locator.count() else ""
    m = re.match(r'^\s*-\s*[\w-]+\s+"(.*)"', first)
    return m.group(1).strip() if m else ""


def is_focused(locator):
    try:
        return locator.evaluate("el => el === document.activeElement")
    except Exception:
        return False


def focus_inside(locator):
    try:
        return locator.evaluate("el => el.contains(document.activeElement)")
    except Exception:
        return False


def describe_focus(page):
    return page.evaluate("""() => {
        const el = document.activeElement;
        if (!el) return 'nothing';
        const name = el.getAttribute('aria-label') || (el.textContent || '').trim().slice(0, 40);
        return el.tagName.toLowerCase() + (el.id ? '#' + el.id : '') + (name ? ` "${name}"` : '');
    }""")


def focus_is_lost(page):
    """True when the focus is on the body, on nothing, or on an element that is not visible."""
    return page.evaluate("""() => {
        const el = document.activeElement;
        if (!el || el === document.body || el === document.documentElement) return true;
        if (!el.isConnected) return true;
        const r = el.getClientRects();
        return r.length === 0;
    }""")


def body_lines(page):
    text = page.locator("body").inner_text()
    return [l.strip() for l in text.splitlines() if l.strip()]


def new_lines(before, after):
    old = set(before)
    return [l for l in after if l not in old]


def run(probe_file, base_url, names, out):
    spec = importlib.util.spec_from_file_location("probe_module", probe_file)
    module = importlib.util.module_from_spec(spec)
    sys.path.insert(0, str(HERE))
    spec.loader.exec_module(module)
    tests = [t for t in module.TESTS if not names or t in names]
    results = {}
    api = Api(base_url)
    with sync_playwright() as pw:
        browser = pw.chromium.launch()
        for name in tests:
            api.reset()
            probe = Probe(browser, base_url, getattr(module, "CONTEXT", {}))
            start = time.monotonic()
            try:
                getattr(module, name)(probe)
                outcome, message = "Passed", ""
            except AssertionError as e:
                outcome, message = "Failed", str(e)
            except Exception as e:  # noqa: BLE001 - a Playwright timeout is a failure of the run under test
                outcome = "Failed"
                message = f"{type(e).__name__}: " + str(e).splitlines()[0][:300] if str(e) else traceback.format_exc(limit=1)
            finally:
                probe.close()
            results[name] = {"outcome": outcome, "message": message, "seconds": round(time.monotonic() - start, 1)}
            print(f"{name}: {outcome} {message}", flush=True)
        browser.close()
    pathlib.Path(out).write_text(json.dumps(results, indent=2), encoding="utf-8")
    return 0 if all(r["outcome"] == "Passed" for r in results.values()) else 1


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("probe", type=pathlib.Path)
    parser.add_argument("--base-url", required=True)
    parser.add_argument("--out", required=True)
    parser.add_argument("--test", action="append")
    args = parser.parse_args()
    return run(args.probe, args.base_url, args.test, args.out)


if __name__ == "__main__":
    # Run the copy that the probes import ('harness'), so that both share one module state (_PAGES).
    sys.path.insert(0, str(HERE))
    import harness
    sys.exit(harness.main())
