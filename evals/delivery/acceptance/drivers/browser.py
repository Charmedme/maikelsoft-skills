"""Protocol driver: a real browser through Playwright. The DSL layer above uses it; test cases never do.

Find elements by role and accessible name, the way a keyboard or screen reader user meets them, so that
a change of markup or style does not break a case that still holds.
"""
import contextlib
import os
import time

from playwright.sync_api import sync_playwright


class Browser:
    def __init__(self, base_url=None, width=1280, height=900):
        self.base_url = (base_url or os.environ["UI_URL"]).rstrip("/")
        self.width, self.height = width, height
        self._playwright = self._browser = self.page = None

    def __enter__(self):
        self._playwright = sync_playwright().start()
        self._browser = self._playwright.chromium.launch()
        context = self._browser.new_context(viewport={"width": self.width, "height": self.height})
        self.page = context.new_page()
        return self

    def __exit__(self, *exc):
        with contextlib.suppress(Exception):
            self._browser.close()
        with contextlib.suppress(Exception):
            self._playwright.stop()

    def open(self, path):
        self.page.goto(self.base_url + path, wait_until="networkidle")

    def resize(self, width, height=None):
        self.page.set_viewport_size({"width": width, "height": height or self.height})

    def by_role(self, role, name=None, exact=True):
        return self.page.get_by_role(role, name=name, exact=exact) if name is not None else self.page.get_by_role(role)

    def press(self, key):
        self.page.keyboard.press(key)

    def focused(self):
        """Role, accessible name, and tag of the element that has the focus."""
        return self.page.evaluate("""() => {
            const el = document.activeElement;
            if (!el) return null;
            const label = el.getAttribute('aria-label') || el.innerText || el.value || '';
            return {tag: el.tagName.toLowerCase(), role: el.getAttribute('role'), name: label.trim()};
        }""")

    def scrolls_sideways(self):
        return self.page.evaluate("() => document.documentElement.scrollWidth > document.documentElement.clientWidth")

    def set_timeout(self, milliseconds):
        """Default wait for each locator action; a missing element then fails fast."""
        self.page.set_default_timeout(milliseconds)

    def wait_until(self, check, timeout=5.0, interval=0.1):
        """Poll `check()` until it returns a true value or the time is up; return the last value."""
        deadline = time.monotonic() + timeout
        while True:
            value = check()
            if value or time.monotonic() >= deadline:
                return value
            self.page.wait_for_timeout(int(interval * 1000))

    def has_focus(self, locator):
        """True when the element of the locator (exactly one) is the focused element."""
        if locator.count() != 1:
            return False
        return locator.evaluate("el => el === document.activeElement")

    def texts(self, role):
        """The text of each element with the role, white space collapsed."""
        return [" ".join(text.split()) for text in self.page.get_by_role(role).all_inner_texts()]
