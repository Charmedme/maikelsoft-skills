"""Hidden acceptance cases for dash-csv-import: import products from a CSV file, all or nothing."""
import unittest

from dsl.dashboard import READER, refused_as_forbidden, unique
from dsl_dash_csv_import import ImportDashboard, counts, csv_file, error_lines

LIMIT = 1000


class CsvImportCases(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.app = ImportDashboard()
        cls.manager = cls.app.new_member()

    def code(self):
        return unique("IMP-")

    def run_import(self, text, who=None):
        return self.app.import_products(who or self.manager, text)

    def imported(self, text):
        reply = self.run_import(text)
        self.assertEqual(reply.status, 200, reply)
        return counts(reply)

    def one(self, code):
        found = self.app.with_code(self.manager, code)
        self.assertEqual(len(found), 1, f"{code}: {found}")
        return found[0]

    def history(self, product_id):
        reply = self.app.product_history(self.manager, product_id)
        self.assertEqual(reply.status, 200, reply)
        return reply.body

    def assert_nothing_stored(self, *codes):
        for code in codes:
            self.assertEqual(self.app.with_code(self.manager, code), [], code)

    # AC1, AC2, AC6
    def test_new_lines_create_products(self):
        """AC1, AC2, AC6."""
        first, second = self.code(), self.code()
        result = self.imported(csv_file([(first, "Desk lamp", "Warm light"), (second, "Desk chair", "Black")]))
        self.assertEqual(result, {"created": 2, "updated": 0, "unchanged": 0})
        lamp, chair = self.one(first), self.one(second)
        self.assertEqual((lamp["code"], lamp["name"], lamp["description"]), (first, "Desk lamp", "Warm light"))
        self.assertEqual((chair["name"], chair["description"]), ("Desk chair", "Black"))

    def test_quoted_fields_keep_commas_quotes_and_line_breaks(self):
        """AC1: standard CSV quoting."""
        code = self.code()
        name = 'Cable, 2 m "long"'
        description = "First line\nSecond line, with a comma"
        self.assertEqual(self.imported(csv_file([(code, name, description)]))["created"], 1)
        product = self.one(code)
        self.assertEqual((product["name"], product["description"]), (name, description))

    def test_a_byte_order_mark_and_windows_line_ends_are_allowed(self):
        """AC1: a UTF-8 BOM at the start."""
        code = self.code()
        result = self.imported(csv_file([(code, "Mug", "Blue")], newline="\r\n", bom=True))
        self.assertEqual(result, {"created": 1, "updated": 0, "unchanged": 0})
        self.assertEqual(self.one(code)["name"], "Mug")

    def test_a_line_with_the_code_of_an_existing_product_updates_that_product(self):
        """AC2: the code matches after the trim, ignoring case."""
        code = self.code()
        existing = self.app.added_product(self.manager, code=code, name="Old name", description="Old text")
        result = self.imported(csv_file([(f"  {code.lower()} ", "New name", "New text")]))
        self.assertEqual(result, {"created": 0, "updated": 1, "unchanged": 0})
        product = self.one(code)
        self.assertEqual((product["id"], product["name"], product["description"]),
                         (existing["id"], "New name", "New text"))

    def test_a_line_that_changes_nothing_counts_as_unchanged(self):
        """AC6, AC7: no change and no audit entry."""
        code = self.code()
        existing = self.app.added_product(self.manager, code=code, name="Same name", description="Same text")
        entries = len(self.history(existing["id"]))
        result = self.imported(csv_file([(code, "Same name", "Same text")]))
        self.assertEqual(result, {"created": 0, "updated": 0, "unchanged": 1})
        self.assertEqual(len(self.history(existing["id"])), entries)

    def test_importing_the_same_file_twice_leaves_everything_unchanged_the_second_time(self):
        """Answer 5."""
        text = csv_file([(self.code(), "Pen", "Red"), (self.code(), "Pencil", "")])
        self.assertEqual(self.imported(text), {"created": 2, "updated": 0, "unchanged": 0})
        self.assertEqual(self.imported(text), {"created": 0, "updated": 0, "unchanged": 2})

    # AC3, AC4, answer 6
    def test_one_wrong_line_stores_nothing_and_names_the_line(self):
        """AC3, answer 6: 400, a problem details object with errors [{line, message}]."""
        good, bad, other = self.code(), self.code(), self.code()
        code = self.code()
        existing = self.app.added_product(self.manager, code=code, name="Keep me", description="Keep")
        reply = self.run_import(csv_file([(good, "Good", "x"), (bad, "", "no name"), (code, "Changed", "y"),
                                          (other, "Also good", "z")]))
        self.assertEqual(reply.status, 400, reply)
        self.assertEqual(error_lines(reply), [3], reply)
        message = reply.body["errors"][0].get("message")
        self.assertTrue(isinstance(message, str) and message.strip(), reply)
        self.assert_nothing_stored(good, bad, other)
        self.assertEqual(self.one(code)["name"], existing["name"])

    def test_each_wrong_line_is_reported_with_its_line_number(self):
        """AC4: empty code, a code of spaces, empty name, a name over 100 characters; 100 is fine."""
        long_name = ("N" * 101)
        fine = self.code()
        reply = self.run_import(csv_file([
            ("", "No code", "x"),
            ("   ", "Blank code", "x"),
            (self.code(), "", "x"),
            (self.code(), long_name, "x"),
            (fine, "M" * 100, "x"),
        ]))
        self.assertEqual(reply.status, 400, reply)
        self.assertEqual(error_lines(reply), [2, 3, 4, 5], reply)
        self.assert_nothing_stored(fine)

    def test_a_code_that_appears_twice_in_the_file_is_reported_on_both_lines(self):
        """AC4: the same code after the trim, ignoring case."""
        twice, single = self.code(), self.code()
        reply = self.run_import(csv_file([(twice, "First", "x"), (single, "Single", "x"),
                                          (f" {twice.lower()} ", "Second", "x")]))
        self.assertEqual(reply.status, 400, reply)
        self.assertEqual(error_lines(reply), [2, 4], reply)
        self.assert_nothing_stored(twice, single)

    # AC5, answer 2
    def test_a_file_with_more_than_1000_product_lines_is_refused_as_a_whole(self):
        """AC5."""
        prefix = self.code()
        reply = self.run_import(csv_file([(f"{prefix}-{i}", f"Bulk {i}", "") for i in range(LIMIT + 1)]))
        self.assertEqual(reply.status, 400, reply)
        self.assert_nothing_stored(f"{prefix}-0", f"{prefix}-{LIMIT}")

    def test_a_file_with_1000_product_lines_and_empty_lines_is_imported(self):
        """AC5, answer 2: exactly 1000 is allowed; empty lines are skipped and not counted."""
        prefix = self.code()
        rows = []
        for i in range(LIMIT):
            rows.append((f"{prefix}-{i}", f"Bulk {i}", ""))
            if i % 250 == 0:
                rows.append("")
        self.assertEqual(self.imported(csv_file(rows)), {"created": LIMIT, "updated": 0, "unchanged": 0})
        self.assertEqual(self.one(f"{prefix}-{LIMIT - 1}")["name"], f"Bulk {LIMIT - 1}")

    def test_empty_lines_are_skipped(self):
        """Answer 2."""
        first, second = self.code(), self.code()
        text = csv_file(["", (first, "One", "x"), "", "", (second, "Two", "y")]) + "\n"
        self.assertEqual(self.imported(text), {"created": 2, "updated": 0, "unchanged": 0})

    def test_another_header_is_refused_as_a_whole(self):
        """Answer 1: the columns are always Code,Name,Description."""
        code = self.code()
        for header in ("Name,Code,Description", "Code,Name"):
            reply = self.run_import(csv_file([(code, "Swapped", "x")], header=header))
            self.assertEqual(reply.status, 400, f"{header}: {reply}")
        self.assert_nothing_stored(code)

    def test_the_description_can_be_empty(self):
        """Answer 3."""
        code = self.code()
        self.assertEqual(self.imported(csv_file([(code, "No description", "")]))["created"], 1)
        self.assertIn(self.one(code)["description"], ("", None))

    def test_the_code_is_trimmed_and_the_name_and_description_are_kept_as_written(self):
        """Answer 4."""
        code = self.code()
        self.imported(csv_file([(f"  {code}\t", "  Spaced name ", " spaced text  ")]))
        product = self.one(code)
        self.assertEqual((product["code"], product["name"], product["description"]),
                         (code, "  Spaced name ", " spaced text  "))

    # AC7
    def test_created_and_changed_products_get_the_same_audit_entries_as_on_the_product_screen(self):
        """AC7: a create entry and an update entry by the importing user; unchanged products get none."""
        importer = self.app.new_member()
        new_code, changed_code, same_code = self.code(), self.code(), self.code()
        changed = self.app.added_product(self.manager, code=changed_code, name="Before", description="x")
        same = self.app.added_product(self.manager, code=same_code, name="Same", description="x")
        same_entries = len(self.history(same["id"]))
        reply = self.run_import(csv_file([(new_code, "Brand new", "x"), (changed_code, "After", "x"),
                                          (same_code, "Same", "x")]), who=importer)
        self.assertEqual(reply.status, 200, reply)
        created = self.history(self.one(new_code)["id"])
        self.assertEqual([(e["userName"], e["action"]) for e in created], [(importer, "CREATED")])
        self.assertEqual(created[0]["data"]["name"], "Brand new")
        updated = self.history(changed["id"])
        self.assertEqual((updated[0]["userName"], updated[0]["action"], updated[0]["data"]["name"]),
                         (importer, "UPDATED", "After"))
        self.assertEqual(len(updated), 2)
        self.assertEqual(len(self.history(same["id"])), same_entries)

    # AC8
    def test_only_a_user_who_may_add_and_update_products_can_import(self):
        """AC8: one of the two permissions is not enough; others get 403 and nothing is stored."""
        code = self.code()
        text = csv_file([(code, "Guarded", "x")])
        add_only = self.app.new_member(permissions=["GetProducts", "GetProduct", "AddProduct"])
        update_only = self.app.new_member(permissions=["GetProducts", "GetProduct", "UpdateProduct"])
        for who in (READER, add_only, update_only):
            self.assertTrue(refused_as_forbidden(self.run_import(text, who=who)), who)
        self.assert_nothing_stored(code)
        both = self.app.new_member(permissions=["AddProduct", "UpdateProduct"])
        self.assertEqual(self.run_import(text, who=both).status, 200)
        self.assertEqual(self.one(code)["name"], "Guarded")


if __name__ == "__main__":
    unittest.main()
