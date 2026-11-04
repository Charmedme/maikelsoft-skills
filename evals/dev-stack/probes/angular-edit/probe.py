"""Browser probe for the case angular-edit-hard: the edit page /products/:id/edit.

The harness (../angular-common/harness.py) runs each function in TESTS against the built app and the
mock API, with the seed data. The browser runs in the time zone Pacific/Auckland (UTC+13 in March), so
a date that goes through toISOString() of a local midnight shows up one day early in the PUT body.
The tests find fields by their label and buttons and links by their accessible name.
"""
import re

from harness import (pause, body_lines, check, check_axe, check_no_sideways_scroll, describe_focus, header, is_focused,
                     new_lines, wait_until)

CONTEXT = {"timezone_id": "Pacific/Auckland", "locale": "en-NZ"}

TESTS = [
    "edit_link_opens_form_with_values_and_save_returns_to_list",
    "server_field_error_is_shown_and_linked_to_the_field",
    "submit_stays_enabled_and_focus_moves_to_first_error",
    "double_click_sends_one_put",
    "if_match_is_sent",
    "conflict_keeps_typed_values_and_says_so",
    "date_is_sent_without_shift",
    "list_shows_new_values_after_save",
    "unknown_id_shows_not_found",
    "no_sideways_scroll_at_390",
    "axe_no_serious_violations",
    "labels_are_dutch_in_dutch",
]

TARGET, TARGET_ID, TARGET_CODE = "Desk lamp", 2, "DL-210"
EDIT = re.compile(r"edit|change|bewerk|wijzig", re.I)
SAVE = re.compile(r"save|opslaan|bewaar|update|wijzigingen", re.I)
LABELS = {
    "name": re.compile(r"^\W*(product\s*)?(name|naam)\b", re.I),
    "code": re.compile(r"^\W*(product\s*)?code\b", re.I),
    "price": re.compile(r"^\W*(price|prijs)\b", re.I),
    "date": re.compile(r"available|beschikbaar", re.I),
}
CONFLICT = re.compile(r"changed|modified|updated by|someone else|another user|other user|conflict|no longer|"
                      r"gewijzigd|aangepast|veranderd|iemand anders|andere gebruiker|conflict", re.I)
NOT_FOUND = re.compile(r"not found|does not exist|doesn't exist|no product|unknown|niet gevonden|bestaat niet|"
                       r"geen product|onbekend", re.I)
CODE_TAKEN = re.compile(r"already|in use|taken|exists|unique|used|gebruik|bestaat|uniek|bezet", re.I)
ENGLISH = re.compile(r"\b(name|price|available from|save|cancel|edit product|back)\b", re.I)


def field(page, key):
    return page.get_by_label(LABELS[key]).filter(visible=True).first


def form_ready(page):
    check(wait_until(lambda: page.get_by_label(LABELS["name"]).filter(visible=True).count() > 0, 6),
          "no visible field with the label Name on the edit page")
    check(wait_until(lambda: field(page, "code").input_value() == TARGET_CODE, 4),
          f"the code field does not show {TARGET_CODE} after the load")


def open_edit(p, width=1440, height=900, lang=None, product_id=TARGET_ID):
    page = p.page(width, height)
    p.goto(page, f"/products/{product_id}/edit" + (f"?lang={lang}" if lang else ""))
    form_ready(page)
    return page


def save_button(page):
    button = page.get_by_role("button", name=SAVE).filter(visible=True)
    check(button.count() >= 1, "no Save button on the edit page")
    return button.first


def row(page, name):
    return page.get_by_role("row").filter(has_text=name)


def puts(p):
    return p.api.requests("PUT", f"/api/products/{TARGET_ID}")


def wait_left_edit(page, seconds=5):
    return wait_until(lambda: re.search(r"/products/?(\?.*)?$", page.url), seconds)


def open_list_and_edit(p):
    page = p.page()
    p.goto(page, "/products")
    target = row(page, TARGET).first
    target.wait_for()
    link = target.get_by_role("link", name=EDIT).or_(target.get_by_role("button", name=EDIT))
    check(link.count() >= 1, f"no Edit link in the row of {TARGET}")
    link.first.click()
    check(wait_until(lambda: re.search(rf"/products/{TARGET_ID}/edit", page.url), 4),
          f"the Edit link goes to {page.url}, not to /products/{TARGET_ID}/edit")
    form_ready(page)
    return page


def described_text(locator):
    return locator.evaluate("""el => (el.getAttribute('aria-describedby') || '').split(/\\s+/).filter(Boolean)
        .map(id => document.getElementById(id)).filter(Boolean).map(e => e.textContent.trim()).join(' ')""")


# --- Tests -------------------------------------------------------------------------------------

def edit_link_opens_form_with_values_and_save_returns_to_list(p):
    page = open_list_and_edit(p)
    check(field(page, "name").input_value() == TARGET, f"the name field shows '{field(page, 'name').input_value()}'")
    price = field(page, "price").input_value().replace(",", ".")
    check(re.fullmatch(r"39\.950*", price), f"the price field shows '{price}', not 39.95")
    date = field(page, "date").input_value()
    check(date in ("2026-02-01", "01/02/2026", "1/02/2026", "2026/02/01"), f"the date field shows '{date}', not 2026-02-01")
    field(page, "name").fill("Desk lamp XL")
    save_button(page).click()
    check(wait_left_edit(page), f"after the save the page stays at {page.url}")
    check(len(puts(p)) == 1, f"expected 1 PUT, got {len(puts(p))}")


def server_field_error_is_shown_and_linked_to_the_field(p):
    page = open_edit(p)
    code = field(page, "code")
    code.fill("OD-100")
    save_button(page).click()
    check(wait_until(lambda: len(puts(p)) >= 1, 4), "no PUT was sent for a valid form")
    check(wait_until(lambda: code.get_attribute("aria-invalid") == "true", 3),
          "the code field has no aria-invalid=\"true\" after the 400 for the code")
    text = wait_until(lambda: described_text(code), 2)
    check(text, "the code field has no aria-describedby that points to an error text")
    check(CODE_TAKEN.search(text), f"the text linked to the code field does not give the server error: '{text}'")


def submit_stays_enabled_and_focus_moves_to_first_error(p):
    page = open_edit(p)
    field(page, "name").fill("")
    field(page, "code").fill("x")
    button = save_button(page)
    check(button.is_enabled() and button.get_attribute("aria-disabled") != "true",
          "the Save button is disabled while the form is invalid")
    button.click()
    check(wait_until(lambda: is_focused(field(page, "name")), 3),
          f"after Save with errors the focus is on {describe_focus(page)}, not on the first field with an error (Name)")


def double_click_sends_one_put(p):
    page = open_edit(p)
    field(page, "name").fill("Desk lamp twice")
    save_button(page).dblclick()
    pause(1.5)
    check(len(puts(p)) == 1, f"a double click on Save sent {len(puts(p))} PUT requests")


def if_match_is_sent(p):
    page = open_edit(p)
    field(page, "name").fill("Desk lamp with tag")
    save_button(page).click()
    check(wait_until(lambda: len(puts(p)) >= 1, 4), "no PUT was sent")
    value = header(puts(p)[0], "If-Match")
    check(value, "the PUT has no If-Match header")
    check(value.strip().removeprefix("W/").strip('"') == "1", f"If-Match is {value}, not the ETag \"1\" of the GET")


def conflict_keeps_typed_values_and_says_so(p):
    page = open_edit(p)
    p.api.touch(TARGET_ID)
    before = body_lines(page)
    field(page, "name").fill("Desk lamp typed")
    field(page, "price").fill("41.25")
    save_button(page).click()
    check(wait_until(lambda: len(puts(p)) >= 1, 4), "no PUT was sent")
    pause(1.0)
    check(re.search(rf"/products/{TARGET_ID}/edit", page.url), f"after the 412 the page went to {page.url}")
    shown = wait_until(lambda: [l for l in new_lines(before, body_lines(page)) if CONFLICT.search(l)], 3)
    check(shown, "no message says that someone else changed the product")
    name, price = field(page, "name").input_value(), field(page, "price").input_value()
    check(name == "Desk lamp typed", f"after the 412 the name field shows '{name}', not the typed value")
    check(price.replace(",", ".") in ("41.25", "41.250"), f"after the 412 the price field shows '{price}'")


def date_is_sent_without_shift(p):
    page = open_edit(p)
    date = field(page, "date")
    if date.get_attribute("type") == "date":
        date.fill("2026-03-01")
    else:
        date.fill("")
        date.type("2026-03-01")
    save_button(page).click()
    check(wait_until(lambda: len(puts(p)) >= 1, 4), "no PUT was sent")
    body = puts(p)[0]["body"] or {}
    sent = str(body.get("availableFrom"))
    check(sent[:10] == "2026-03-01", f"availableFrom was sent as '{sent}' for 2026-03-01 (in Pacific/Auckland)")


def list_shows_new_values_after_save(p):
    page = open_list_and_edit(p)
    field(page, "name").fill("Desk lamp Pro")
    field(page, "price").fill("42.5")
    save_button(page).click()
    check(wait_left_edit(page), f"after the save the page stays at {page.url}")
    check(wait_until(lambda: row(page, "Desk lamp Pro").count() == 1, 4), "the list does not show the new name")
    text = row(page, "Desk lamp Pro").first.inner_text()
    check("42.50" in text or "42,50" in text, f"the row does not show the new price 42.50: '{text}'")


def unknown_id_shows_not_found(p):
    page = p.page()
    p.goto(page, "/products/999/edit")
    shown = wait_until(lambda: [l for l in body_lines(page) if NOT_FOUND.search(l)], 5)
    check(shown, "no not-found message for product 999")
    check(page.get_by_label(LABELS["name"]).filter(visible=True).count() == 0, "an empty form shows for product 999")


def no_sideways_scroll_at_390(p):
    page = p.page(390, 844)
    p.goto(page, "/products")
    row(page, TARGET).first.wait_for()
    check_no_sideways_scroll(page, "products list")
    p.goto(page, f"/products/{TARGET_ID}/edit")
    form_ready(page)
    check_no_sideways_scroll(page, "edit page")
    submit_with_error(page)
    check_no_sideways_scroll(page, "edit page with an error")


def axe_no_serious_violations(p):
    page = open_edit(p)
    check_axe(page, "edit page")
    submit_with_error(page)
    check_axe(page, "edit page with an error")


def labels_are_dutch_in_dutch(p):
    page = p.page()
    p.goto(page, "/products?lang=nl")
    target = row(page, TARGET).first
    target.wait_for()
    link = target.get_by_role("link", name=EDIT).or_(target.get_by_role("button", name=EDIT))
    check(link.count() >= 1, "no Edit link (Bewerken, Wijzigen) in the row in Dutch")
    link.first.click()
    form_ready(page)
    for key, dutch in (("name", "naam"), ("price", "prijs"), ("date", "beschikbaar")):
        label = page.get_by_label(LABELS[key]).filter(visible=True).first
        name = label.evaluate("el => (el.labels && el.labels[0] ? el.labels[0].textContent : el.getAttribute('aria-label')) || ''")
        check(dutch in name.lower(), f"the label of the {key} field is '{name.strip()}' in Dutch")
    button = accessible_text(save_button(page))
    check(not ENGLISH.search(button), f"the Save button is '{button}' in Dutch")
    main = page.locator("main").inner_text() if page.locator("main").count() else page.locator("body").inner_text()
    hit = ENGLISH.search(main.replace(TARGET, " "))
    check(not hit, f"English text on the edit page in Dutch: '{hit.group(0) if hit else ''}'")


def submit_with_error(page):
    """Empty the name and press Save (if the button is enabled), to show the error state."""
    field(page, "name").fill("")
    field(page, "name").blur()
    button = save_button(page)
    if button.is_enabled():
        button.click()
    pause(0.5)


def accessible_text(locator):
    return (locator.get_attribute("aria-label") or locator.inner_text() or "").strip()
