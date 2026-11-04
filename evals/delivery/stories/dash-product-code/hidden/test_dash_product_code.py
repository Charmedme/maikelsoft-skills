"""Hidden acceptance cases for dash-product-code: each product code is used only once."""
import unittest

from dsl.dashboard import Dashboard, refused_as_invalid, unique

RACE_ROUNDS = 3
RACERS = 4


class ProductCodeCases(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.app = Dashboard()
        cls.manager = cls.app.new_member()

    def code(self):
        return unique("Ab-")

    def add(self, code, name=None):
        return self.app.add_product(self.manager, code, name or unique("Product "))

    def added(self, code):
        reply = self.add(code)
        self.assertEqual(reply.status, 201, reply)
        return reply.body

    def stored(self, product_id):
        reply = self.app.product(self.manager, product_id)
        self.assertEqual(reply.status, 200, reply)
        return reply.body

    def holders(self, code):
        """The products whose code is the given code after the trim, ignoring case."""
        key = code.strip().lower()
        return [p for p in self.app.products(self.manager).body if (p["code"] or "").strip().lower() == key]

    def update(self, product, **fields):
        body = {"code": product["code"], "name": product["name"], "description": product["description"]}
        body.update(fields)
        return self.app.update_product(self.manager, product["id"], **body)

    def test_a_new_code_is_stored_without_the_outer_white_space_and_with_its_case(self):
        """AC1, answer 1: only trimmed, the case does not change."""
        code = self.code()
        product = self.added(f"  \t{code} ")
        self.assertEqual(self.stored(product["id"])["code"], code)

    def test_a_new_product_with_a_code_that_exists_is_refused_on_code(self):
        """AC3."""
        code = self.code()
        self.added(code)
        reply = self.add(code)
        self.assertTrue(refused_as_invalid(reply, "Code"), reply)
        self.assertEqual(len(self.holders(code)), 1)

    def test_codes_that_differ_only_in_case_and_outer_spaces_are_the_same(self):
        """AC2, AC3: the example of the context, ab-100 / AB-100 / ' AB-100 '."""
        code = self.code()
        self.added(code.lower())
        for variant in (code.upper(), f" {code.upper()} ", f"{code}\t"):
            reply = self.add(variant)
            self.assertTrue(refused_as_invalid(reply, "Code"), f"{variant!r}: {reply}")
        self.assertEqual(len(self.holders(code)), 1)

    def test_a_seeded_product_code_cannot_be_used_again(self):
        """AC2, AC3: also against the products that were there before (PD002)."""
        reply = self.add(" pd002 ")
        self.assertTrue(refused_as_invalid(reply, "Code"), reply)

    def test_codes_that_only_start_the_same_are_different(self):
        """AC2: equal means equal, not 'starts with'."""
        code = self.code()
        self.added(code)
        self.added(code + "0")
        self.assertEqual(len(self.holders(code)), 1)

    def test_an_update_to_the_code_of_another_product_is_refused_and_nothing_changes(self):
        """AC3: on update too."""
        taken = self.code()
        self.added(taken)
        product = self.added(self.code())
        reply = self.update(product, code=f" {taken.upper()} ", name="Changed name")
        self.assertTrue(refused_as_invalid(reply, "Code"), reply)
        now = self.stored(product["id"])
        self.assertEqual((now["code"], now["name"]), (product["code"], product["name"]))

    def test_a_product_keeps_its_own_code_on_update_also_when_only_the_case_changes(self):
        """AC4."""
        code = self.code()
        product = self.added(code)
        same = self.update(product, name="New name")
        self.assertEqual(same.status, 200, same)
        recased = self.update(product, code=code.upper())
        self.assertEqual(recased.status, 200, recased)
        self.assertEqual(self.stored(product["id"])["code"], code.upper())

    def test_an_updated_code_is_stored_trimmed(self):
        """AC1 on update."""
        product = self.added(self.code())
        new_code = self.code()
        reply = self.update(product, code=f"  {new_code}  ")
        self.assertEqual(reply.status, 200, reply)
        self.assertEqual(self.stored(product["id"])["code"], new_code)

    def test_the_code_of_a_deleted_product_can_be_used_again(self):
        """AC5."""
        code = self.code()
        first = self.added(code)
        self.assertTrue(refused_as_invalid(self.add(code.upper()), "Code"))
        self.assertEqual(self.app.delete_product(self.manager, first["id"]).status, 200)
        self.added(code.upper())
        self.assertEqual(len(self.holders(code)), 1)

    def test_an_empty_code_is_refused_on_code(self):
        """Answer 2: empty or only spaces, on create and on update."""
        for code in ("", "   "):
            self.assertTrue(refused_as_invalid(self.add(code), "Code"), repr(code))
        product = self.added(self.code())
        self.assertTrue(refused_as_invalid(self.update(product, code="  "), "Code"))
        self.assertEqual(self.stored(product["id"])["code"], product["code"])

    def test_when_several_people_create_the_same_code_at_once_only_one_product_gets_it(self):
        """AC6, answer 4: the others get the normal 400 on Code."""
        racers = [self.app.new_member() for _ in range(RACERS)]
        for round_number in range(RACE_ROUNDS):
            code = self.code()
            variants = [code, code.upper(), f" {code} ", code.lower()]
            replies = self.app.at_the_same_moment(*[
                (lambda who=who, variant=variant: self.app.add_product(who, variant, unique("Racer ")))
                for who, variant in zip(racers, variants)])
            statuses = sorted(reply.status for reply in replies)
            self.assertEqual(statuses, [201] + [400] * (RACERS - 1), f"round {round_number}: {replies}")
            for reply in replies:
                if reply.status == 400:
                    self.assertTrue(refused_as_invalid(reply, "Code"), reply)
            self.assertEqual(len(self.holders(code)), 1, f"round {round_number}")


if __name__ == "__main__":
    unittest.main()
