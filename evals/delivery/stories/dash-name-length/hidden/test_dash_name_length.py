"""Hidden acceptance cases for dash-name-length: a product name has at most 100 characters."""
import unittest

from dsl.dashboard import Dashboard, refused_as_invalid, unique


def name_of(length):
    """A name of plain letters with exactly `length` characters, unique for this run."""
    start = unique("Name")
    return (start + "x" * length)[:length] if length >= len(start) else "y" * length


class NameLengthCases(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.app = Dashboard()
        cls.manager = cls.app.new_member()

    def stored(self, product_id):
        reply = self.app.product(self.manager, product_id)
        self.assertEqual(reply.status, 200, reply)
        return reply.body

    def test_a_new_product_with_a_name_of_101_characters_is_refused_on_name(self):
        """AC1, AC2: create refuses a longer name with 400 and an error on Name."""
        for length in (101, 500):
            reply = self.app.add_product(self.manager, unique("NL-"), name_of(length))
            self.assertTrue(refused_as_invalid(reply, "Name"), f"{length}: {reply}")

    def test_a_refused_new_product_is_not_stored(self):
        """AC2: nothing is created."""
        code = unique("NL-")
        self.app.add_product(self.manager, code, name_of(101))
        codes = [p["code"] for p in self.app.products(self.manager).body]
        self.assertNotIn(code, codes)

    def test_a_new_product_with_a_name_of_exactly_100_characters_is_accepted(self):
        """AC3."""
        name = name_of(100)
        reply = self.app.add_product(self.manager, unique("NL-"), name)
        self.assertEqual(reply.status, 201, reply)
        self.assertEqual(self.stored(reply.body["id"])["name"], name)

    def test_an_update_to_a_name_of_101_characters_is_refused_and_the_product_stays(self):
        """AC2: update refuses a longer name with 400 and an error on Name; nothing changes."""
        product = self.app.added_product(self.manager)
        reply = self.app.update_product(self.manager, product["id"], code=product["code"], name=name_of(101),
                                        description="Changed")
        self.assertTrue(refused_as_invalid(reply, "Name"), reply)
        now = self.stored(product["id"])
        self.assertEqual((now["name"], now["description"]), (product["name"], product["description"]))

    def test_an_update_to_a_name_of_exactly_100_characters_is_accepted(self):
        """AC3."""
        product = self.app.added_product(self.manager)
        name = name_of(100)
        reply = self.app.update_product(self.manager, product["id"], code=product["code"], name=name,
                                        description=product["description"])
        self.assertEqual(reply.status, 200, reply)
        self.assertEqual(self.stored(product["id"])["name"], name)

    def test_an_existing_product_with_a_longer_name_stays_as_it_is(self):
        """Answer 2: existing products with longer names stay until somebody edits them."""
        code, name = unique("NL-OLD-"), name_of(150)
        self.app.db.run(f"""INSERT INTO "Products" ("Id","Code","Name","Description","CreatedDateTime")
                           VALUES (gen_random_uuid(), '{code}', '{name}', 'Old', now());""")
        listed = [p for p in self.app.products(self.manager).body if p["code"] == code]
        self.assertEqual([p["name"] for p in listed], [name])
        self.assertEqual(self.stored(listed[0]["id"])["name"], name)


if __name__ == "__main__":
    unittest.main()
