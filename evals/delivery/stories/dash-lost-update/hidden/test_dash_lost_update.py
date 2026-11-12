"""Hidden acceptance cases for dash-lost-update: an update from an old version of a product is refused."""
import unittest

from dsl.dashboard import Dashboard, refused_as_conflict, refused_as_invalid, unique

ROUNDS = 5


class LostUpdateCases(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.app = Dashboard()
        cls.editor = cls.app.new_member()
        cls.other_editor = cls.app.new_member()

    def new_product(self):
        product = self.read(self.app.added_product(self.editor)["id"])
        self.assertTrue(product.get("rowVersion"), f"no rowVersion in {product}")
        return product

    def read(self, product_id):
        reply = self.app.product(self.editor, product_id)
        self.assertEqual(reply.status, 200, reply)
        return reply.body

    def save(self, who, product, version, **changes):
        body = {"code": product["code"], "name": product["name"], "description": product["description"],
                "rowVersion": version}
        body.update(changes)
        return self.app.update_product(who, product["id"], **body)

    def history(self, product_id):
        reply = self.app.product_history(self.editor, product_id)
        self.assertEqual(reply.status, 200, reply)
        return reply.body

    def test_the_single_product_and_the_list_give_a_row_version(self):
        """AC1: a non-empty value for each product; the list and the single product agree."""
        product = self.new_product()
        listed = [p for p in self.app.products(self.editor).body if p["id"] == product["id"]]
        self.assertEqual(len(listed), 1)
        self.assertEqual(listed[0].get("rowVersion"), product["rowVersion"])
        self.assertTrue(all(p.get("rowVersion") for p in self.app.products(self.editor).body))

    def test_an_update_with_the_current_row_version_succeeds_and_gives_a_new_one(self):
        """AC2, AC5."""
        product = self.new_product()
        reply = self.save(self.editor, product, product["rowVersion"], name="First edit")
        self.assertEqual(reply.status, 200, reply)
        now = self.read(product["id"])
        self.assertEqual(now["name"], "First edit")
        self.assertTrue(now.get("rowVersion"))
        self.assertNotEqual(now["rowVersion"], product["rowVersion"])
        if isinstance(reply.body, dict) and reply.body.get("rowVersion"):
            self.assertEqual(reply.body["rowVersion"], now["rowVersion"])

    def test_the_new_row_version_works_for_the_next_update(self):
        """AC5: the editor goes on from the version the server gave back."""
        product = self.new_product()
        self.assertEqual(self.save(self.editor, product, product["rowVersion"], name="One").status, 200)
        second = self.read(product["id"])
        reply = self.save(self.editor, second, second["rowVersion"], name="Two")
        self.assertEqual(reply.status, 200, reply)
        self.assertEqual(self.read(product["id"])["name"], "Two")

    def test_the_row_version_from_the_list_can_be_sent_back(self):
        """AC1, AC2, answer 1: the client sends back exactly what it got."""
        product = self.new_product()
        listed = [p for p in self.app.products(self.editor).body if p["id"] == product["id"]][0]
        self.assertTrue(listed.get("rowVersion"), listed)
        reply = self.save(self.editor, listed, listed.get("rowVersion"), description="From the list")
        self.assertEqual(reply.status, 200, reply)

    def test_an_update_from_an_old_version_is_refused_and_nothing_changes(self):
        """AC3, answer 2: 409 with a problem details body."""
        product = self.new_product()
        start = product["rowVersion"]
        self.assertEqual(self.save(self.other_editor, product, start, name="Phone edit").status, 200)
        reply = self.save(self.editor, product, start, name="Laptop edit", description="Laptop text")
        self.assertTrue(refused_as_conflict(reply), reply)
        self.assertIsInstance(reply.body, dict, reply)
        self.assertEqual(reply.body.get("status"), 409, reply)
        now = self.read(product["id"])
        self.assertEqual((now["name"], now["description"]), ("Phone edit", product["description"]))

    def test_an_update_without_a_row_version_is_refused_on_row_version(self):
        """AC4: 400 with a validation error on RowVersion; nothing changes."""
        product = self.read(self.app.added_product(self.editor)["id"])
        body = {"code": product["code"], "name": "No version", "description": product["description"]}
        reply = self.app.update_product(self.editor, product["id"], **body)
        self.assertTrue(refused_as_invalid(reply, "RowVersion"), reply)
        self.assertEqual(self.read(product["id"])["name"], product["name"])

    def test_two_updates_from_the_same_version_at_the_same_moment_give_one_success_and_one_conflict(self):
        """AC6: repeated rounds on one product."""
        product = self.new_product()
        for round_number in range(ROUNDS):
            current = self.read(product["id"])
            names = [unique("Laptop "), unique("Phone ")]
            replies = self.app.at_the_same_moment(
                lambda: self.save(self.editor, current, current["rowVersion"], name=names[0]),
                lambda: self.save(self.other_editor, current, current["rowVersion"], name=names[1]))
            statuses = [reply.status for reply in replies]
            self.assertEqual(sorted(statuses), [200, 409], f"round {round_number}: {replies}")
            winner = names[statuses.index(200)]
            self.assertEqual(self.read(product["id"])["name"], winner, f"round {round_number}")

    def test_the_audit_log_gets_an_entry_only_for_the_update_that_succeeded(self):
        """Answer 4."""
        product = self.new_product()
        before = len(self.history(product["id"]))
        start = product["rowVersion"]
        self.assertEqual(self.save(self.other_editor, product, start, name="Accepted").status, 200)
        self.assertTrue(refused_as_conflict(self.save(self.editor, product, start, name="Refused")))
        entries = self.history(product["id"])
        self.assertEqual(len(entries), before + 1)
        self.assertEqual(entries[0]["data"]["name"], "Accepted")
        self.assertNotIn("Refused", [e["data"]["name"] for e in entries])

    def test_a_product_is_deleted_without_a_version(self):
        """Answer 3."""
        product = self.app.added_product(self.editor)
        self.assertEqual(self.app.delete_product(self.editor, product["id"]).status, 200)
        self.assertEqual(self.app.product(self.editor, product["id"]).status, 404)


if __name__ == "__main__":
    unittest.main()
