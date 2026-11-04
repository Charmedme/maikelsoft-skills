"""Browser probe for the case angular-delete: a delete action with a confirmation on /products.

The harness (../angular-common/harness.py) runs each function in TESTS against the built app and the
mock API, with the seed data. The tests find elements by role and accessible name, so the confirmation
can be a <dialog>, an element with role dialog or alertdialog, the native window.confirm(), or an inline
confirmation: a confirm button and a cancel button that appear in the row after the click on Delete.
For an inline confirmation, the dialog tests check that the focus moves to the new buttons, not the name or Escape.
"""
import re

from harness import (pause, body_lines, check, check_axe, check_no_sideways_scroll, accessible_name, describe_focus,
                     focus_inside, focus_is_lost, is_focused, new_lines, wait_until)

TESTS = [
    "confirm_removes_row_and_sends_one_delete",
    "cancel_keeps_row_and_returns_focus",
    "dialog_is_named_takes_focus_and_closes_on_escape",
    "focus_is_not_lost_after_delete",
    "result_is_announced_in_existing_live_region",
    "no_sideways_scroll_at_390_with_dialog_open",
    "axe_no_serious_violations_with_dialog_open",
    "failed_delete_keeps_row_and_shows_error",
    "dialog_texts_are_dutch_in_dutch",
]

TARGET, TARGET_ID = "Office chair", 3
DELETE = re.compile(r"delete|remove|verwijder", re.I)
CONFIRM = re.compile(r"delete|remove|verwijder|\byes\b|\bja\b|confirm|bevestig|\bok\b", re.I)
CANCEL = re.compile(r"cancel|annule|keep|behoud|\bno\b|\bnee\b|close|sluit|terug|back", re.I)
ERROR = re.compile(r"error|fail|could not|couldn't|cannot|can't|not (be )?(deleted|removed)|went wrong|try again"
                   r"|fout|mislukt|niet (worden )?verwijderd|kon niet|probeer", re.I)
ENGLISH = re.compile(r"\b(delete|remove|cancel|confirm|are you sure|sure|permanently|undone|yes|keep|"
                     r"product will|this product|the product)\b", re.I)
DUTCH = re.compile(r"verwijder|annuleer|weet je|weet u|zeker|\bja\b|\bnee\b|bevestig|behoud|terug|niet|product", re.I)
DIALOG_CSS = "dialog[open], [role=dialog], [role=alertdialog]"


def open_list(p, width=1440, height=900, lang=None):
    page = p.page(width, height)
    p.goto(page, "/products" + (f"?lang={lang}" if lang else ""))
    row(page).wait_for()
    return page


def row(page, name=TARGET):
    return page.get_by_role("row").filter(has_text=name)


def delete_button(page):
    button = row(page).get_by_role("button", name=DELETE)
    check(button.count() >= 1, f"no button with the name Delete (or Remove, Verwijderen) in the row of {TARGET}")
    return button.first


def dom_dialog(page):
    return page.locator(DIALOG_CSS).filter(visible=True)


def inline_confirm(page):
    """The row, when it shows a confirm button and a cancel button after the click on Delete. Else None."""
    r = row(page)
    if r.count() == 0:
        return None
    has_cancel = r.get_by_role("button", name=CANCEL).count() > 0
    confirms = [b for b in r.get_by_role("button", name=CONFIRM).all() if not CANCEL.search(b.inner_text() or "")]
    return r.first if has_cancel and confirms else None


def open_confirm(page, native="dismiss"):
    """Click Delete in the row. Return a dict: native (bool), inline (bool), message, button, dialog."""
    seen = {}

    def on_dialog(d):
        seen.update(message=d.message, type=d.type)
        d.accept() if native == "accept" else d.dismiss()

    page.on("dialog", on_dialog)
    button = delete_button(page)
    button.click()
    if not seen:
        wait_until(lambda: seen or dom_dialog(page).count() > 0 or inline_confirm(page) is not None, 3)
    page.remove_listener("dialog", on_dialog)
    if seen:
        return {"native": True, "inline": False, "message": seen["message"], "button": button, "dialog": None}
    if dom_dialog(page).count() == 0 and inline_confirm(page) is not None:
        return {"native": False, "inline": True, "message": "", "button": button, "dialog": inline_confirm(page)}
    check(dom_dialog(page).count() > 0, "no confirmation (dialog or inline buttons) after a click on Delete")
    return {"native": False, "inline": False, "message": "", "button": button, "dialog": dom_dialog(page).first}


def confirm_button(dialog):
    buttons = dialog.get_by_role("button", name=CONFIRM)
    for i in range(buttons.count()):
        if not CANCEL.search(buttons.nth(i).inner_text() or ""):
            return buttons.nth(i)
    check(False, "no confirm button (Delete, Yes, Confirm) in the dialog")


def cancel(dialog, page):
    buttons = dialog.get_by_role("button", name=CANCEL)
    if buttons.count():
        buttons.first.click()
    else:
        page.keyboard.press("Escape")


def confirm_delete(p, page):
    c = open_confirm(page, native="accept")
    if not c["native"]:
        confirm_button(c["dialog"]).click()
    return c


def wait_row_gone(page, seconds=5):
    return wait_until(lambda: row(page).count() == 0, seconds)


def deletes(p):
    return p.api.requests("DELETE", f"/api/products/{TARGET_ID}")


# --- Tests -------------------------------------------------------------------------------------

def confirm_removes_row_and_sends_one_delete(p):
    page = open_list(p)
    confirm_delete(p, page)
    check(wait_row_gone(page), f"the row of {TARGET} is still on the page after the confirmed delete")
    pause(0.5)
    sent = deletes(p)
    check(len(sent) == 1, f"expected 1 DELETE /api/products/{TARGET_ID}, got {len(sent)}")
    check(row(page, "Desk lamp").count() == 1, "the other rows are gone too")


def cancel_keeps_row_and_returns_focus(p):
    page = open_list(p)
    c = open_confirm(page, native="dismiss")
    if not c["native"]:
        cancel(c["dialog"], page)
        if not c["inline"]:
            check(wait_until(lambda: dom_dialog(page).count() == 0, 3), "the dialog stays open after Cancel")
    pause(0.3)
    check(row(page).count() == 1, "the row is gone after Cancel")
    check(not deletes(p), "a DELETE was sent after Cancel")
    check(wait_until(lambda: is_focused(c["button"]), 2),
          f"after Cancel the focus is on {describe_focus(page)}, not on the Delete button")


def dialog_is_named_takes_focus_and_closes_on_escape(p):
    page = open_list(p)
    c = open_confirm(page, native="dismiss")
    if c["native"]:
        return  # window.confirm() is named by its text, takes the focus, and closes on Escape.
    dialog = c["dialog"]
    if c["inline"]:
        check(wait_until(lambda: focus_inside(dialog) and not focus_is_lost(page), 2),
              f"the inline confirmation does not take the focus (focus: {describe_focus(page)})")
        return
    name = accessible_name(dialog)
    check(name, "the dialog has no accessible name (aria-labelledby or aria-label)")
    check(wait_until(lambda: focus_inside(dialog), 2),
          f"the focus is not in the dialog when it opens (focus: {describe_focus(page)})")
    page.keyboard.press("Escape")
    check(wait_until(lambda: dom_dialog(page).count() == 0, 2), "the dialog does not close on Escape")
    check(row(page).count() == 1 and not deletes(p), "Escape deleted the product")


def focus_is_not_lost_after_delete(p):
    page = open_list(p)
    confirm_delete(p, page)
    check(wait_row_gone(page), "the row is not removed, so the focus cannot be checked")
    pause(0.5)
    check(not focus_is_lost(page), f"after the delete the focus is lost ({describe_focus(page)})")


MARK_LIVE = """() => {
    window.__probeLive = window.__probeLive || [];
    const sel = '[role=status], [role=alert], [role=log], [aria-live=polite], [aria-live=assertive]';
    for (const el of document.querySelectorAll(sel)) {
        if (el.getAttribute('aria-live') === 'off' || el.hasAttribute('data-probe-live')) continue;
        el.setAttribute('data-probe-live', String(window.__probeLive.length));
        window.__probeLive.push((el.textContent || '').trim());
    }
    return window.__probeLive.length;
}"""
ANNOUNCED = """() => [...document.querySelectorAll('[data-probe-live]')].some(el => {
    const text = (el.textContent || '').trim();
    return el.isConnected && text !== '' && text !== window.__probeLive[Number(el.getAttribute('data-probe-live'))];
})"""


def result_is_announced_in_existing_live_region(p):
    page = open_list(p)
    page.evaluate(MARK_LIVE)
    c = open_confirm(page, native="accept")
    if not c["native"]:
        pause(0.2)
        count = page.evaluate(MARK_LIVE)
        check(count, "no live region (role=status or aria-live) is in the page before the delete is confirmed")
        confirm_button(c["dialog"]).click()
    check(wait_row_gone(page), "the row is not removed")
    check(wait_until(lambda: page.evaluate(ANNOUNCED), 3),
          "no live region that existed before the delete got a new text after it "
          "(a region that is created together with its text is not announced)")


def no_sideways_scroll_at_390_with_dialog_open(p):
    page = open_list(p, 390, 844)
    check_no_sideways_scroll(page, "products list")
    c = open_confirm(page, native="dismiss")
    if c["native"]:
        return
    pause(0.3)
    check_no_sideways_scroll(page, "dialog open")
    if c["inline"]:
        return
    box = c["dialog"].bounding_box()
    check(box is not None and box["x"] >= -1 and box["x"] + box["width"] <= 391,
          f"the dialog does not fit in 390 px: {box}")


def axe_no_serious_violations_with_dialog_open(p):
    page = open_list(p)
    c = open_confirm(page, native="dismiss")
    pause(0.3)
    check_axe(page, "products list with the dialog open" if not c["native"] else "products list")


def failed_delete_keeps_row_and_shows_error(p):
    p.api.fail_next(f"/api/products/{TARGET_ID}", 500, method="DELETE")
    page = open_list(p)
    before = body_lines(page)
    confirm_delete(p, page)
    check(wait_until(lambda: len(deletes(p)) >= 1, 3), "no DELETE was sent")
    pause(1.0)

    def error_shown():
        return [l for l in new_lines(before, body_lines(page)) if ERROR.search(l)]

    shown = wait_until(error_shown, 3)
    check(row(page).count() == 1, "the row is gone after the DELETE failed with 500")
    check(shown, "no error text is visible after the DELETE failed with 500")


def dialog_texts_are_dutch_in_dutch(p):
    page = open_list(p, lang="nl")
    check(page.get_by_role("columnheader", name=re.compile("Naam")).count() == 1, "the page is not in Dutch")
    button_name = accessible_name(delete_button(page))
    check(not ENGLISH.search(button_name), f"the Delete button is in English in Dutch: '{button_name}'")
    c = open_confirm(page, native="dismiss")
    text = c["message"] if c["native"] else " ".join([c["dialog"].inner_text(), accessible_name(c["dialog"])])
    text = text.replace(TARGET, " ")
    hit = ENGLISH.search(text)
    check(not hit, f"English text in the dialog in Dutch: '{hit.group(0) if hit else ''}' in '{text[:120]}'")
    check(DUTCH.search(text), f"the dialog text is not Dutch: '{text[:120]}'")
