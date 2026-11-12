"""Hidden acceptance cases for dash-delete-a11y: delete a product from the list with the keyboard.

Each case makes the catalog hold only its own products (through the API), then uses the page at
/products as the admin user, by role and accessible name, with the keyboard.
"""
import unittest

from drivers.browser import Browser
from dsl.dashboard import ADMIN, Dashboard, ProductListPage, unique


class DeleteWithKeyboardCases(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.app = Dashboard()
        cls.setup_member = cls.app.new_member()

    def catalog(self, count=3, width=1280):
        """Fresh products in the catalog and the product list open in a browser; returns the names in row order."""
        names = [unique("Lamp ") for _ in range(count)]
        self.products = {p["name"]: p for p in self.app.only_products(self.setup_member, names)}
        self.browser = Browser(width=width)
        self.browser.__enter__()
        self.addCleanup(self.browser.__exit__, None, None, None)
        self.list = ProductListPage(self.browser)
        self.list.open()
        self.assertTrue(self.list.wait_for_rows(names), "the products did not show on /products")
        return self.list.row_names(names)

    def still_stored(self, name):
        return self.app.product(ADMIN, self.products[name]["id"]).status == 200

    def test_each_delete_button_is_named_after_its_product(self):
        """AC1: 'Delete <product name>'."""
        names = self.catalog()
        for name in names:
            self.assertEqual(self.list.delete_button(name).count(), 1, name)

    def test_the_confirmation_is_a_dialog_named_delete_product_with_the_focus_on_cancel(self):
        """AC2."""
        names = self.catalog()
        self.list.start_delete(names[1])
        self.assertTrue(self.browser.wait_until(lambda: self.list.dialog().count() == 1),
                        "no dialog named 'Delete product'")
        self.assertTrue(self.list.focus_is_on(self.list.dialog_button("Cancel")), "the focus is not on Cancel")

    def test_escape_closes_the_dialog_without_a_delete_and_returns_the_focus(self):
        """AC3: Escape."""
        names = self.catalog()
        self.list.start_delete(names[1])
        self.assertTrue(self.browser.wait_until(lambda: self.list.dialog().count() == 1))
        self.browser.press("Escape")
        self.assertTrue(self.browser.wait_until(lambda: not self.list.dialog_open()), "the dialog stays open")
        self.assertTrue(self.list.focus_is_on(self.list.delete_button(names[1])))
        self.assertTrue(self.list.shows(names[1]))
        self.assertTrue(self.still_stored(names[1]))

    def test_cancel_closes_the_dialog_without_a_delete_and_returns_the_focus(self):
        """AC3: Cancel."""
        names = self.catalog()
        self.list.start_delete(names[0])
        self.list.press_in_dialog("Cancel")
        self.assertTrue(self.browser.wait_until(lambda: not self.list.dialog_open()), "the dialog stays open")
        self.assertTrue(self.list.focus_is_on(self.list.delete_button(names[0])))
        self.assertTrue(self.still_stored(names[0]))

    def test_delete_removes_the_row_and_a_status_message_names_the_product(self):
        """AC4: '<product name> deleted.' in a status message."""
        names = self.catalog()
        self.list.start_delete(names[1])
        self.list.press_in_dialog("Delete")
        self.assertTrue(self.browser.wait_until(lambda: not self.list.shows(names[1])), "the row stays")
        self.assertTrue(self.list.status_says(f"{names[1]} deleted."), self.browser.texts("status"))
        self.assertFalse(self.still_stored(names[1]))
        self.assertTrue(self.list.shows(names[0]) and self.list.shows(names[2]))

    def test_after_a_delete_the_focus_is_on_the_delete_button_of_the_next_row(self):
        """AC5: the next row."""
        names = self.catalog()
        self.list.start_delete(names[0])
        self.list.press_in_dialog("Delete")
        self.assertTrue(self.browser.wait_until(lambda: not self.list.shows(names[0])), "the row stays")
        self.assertTrue(self.list.focus_is_on(self.list.delete_button(names[1])), self.browser.focused())

    def test_after_deleting_the_last_row_the_focus_is_on_the_new_last_row(self):
        """AC5: the deleted row was the last row."""
        names = self.catalog()
        self.list.start_delete(names[-1])
        self.list.press_in_dialog("Delete")
        self.assertTrue(self.browser.wait_until(lambda: not self.list.shows(names[-1])), "the row stays")
        self.assertTrue(self.list.focus_is_on(self.list.delete_button(names[-2])), self.browser.focused())

    def test_after_deleting_the_only_row_the_focus_is_on_the_add_product_link(self):
        """AC5: the list is empty."""
        names = self.catalog(count=1)
        self.list.start_delete(names[0])
        self.list.press_in_dialog("Delete")
        self.assertTrue(self.browser.wait_until(lambda: not self.list.shows(names[0])), "the row stays")
        self.assertTrue(self.list.focus_is_on(self.list.add_product_link()), self.browser.focused())
        self.assertTrue(self.list.status_says(f"{names[0]} deleted."), self.browser.texts("status"))

    def test_a_name_of_100_characters_without_spaces_does_not_scroll_the_page_sideways_on_a_phone(self):
        """AC6: 390 px wide."""
        long_name = (unique("Wide") + "W" * 100)[:100]
        self.app.only_products(self.setup_member, [])
        self.products = {long_name: self.app.added_product(self.setup_member, name=long_name)}
        with Browser(width=390, height=844) as browser:
            page = ProductListPage(browser)
            page.open()
            self.assertTrue(page.wait_for_rows([long_name]), "the product did not show")
            self.assertFalse(browser.scrolls_sideways(), "the page scrolls sideways at 390 px")


if __name__ == "__main__":
    unittest.main()
