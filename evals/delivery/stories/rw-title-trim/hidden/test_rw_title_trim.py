"""Hidden acceptance cases for rw-title-trim: the spaces around an article title are removed."""
import unittest

from dsl.conduit import Conduit, accepted, article, refused_as_invalid


class TitleTrimCases(unittest.TestCase):
    def setUp(self):
        self.app = Conduit()
        self.author = self.app.register()

    def test_a_new_title_loses_the_spaces_tabs_and_line_breaks_at_the_start_and_end(self):
        """AC1: white space is spaces, tabs and line breaks; stored title is trimmed on create."""
        name = self.app.unique("Trip ")
        created = self.app.write(self.author, title=f" \t\r\n  {name} \t\n\r ")
        self.assertTrue(accepted(created), created)
        self.assertEqual(article(created)["title"], name)
        stored = self.app.read(article(created)["slug"])
        self.assertEqual(article(stored)["title"], name)

    def test_the_spaces_inside_a_title_stay_as_they_are(self):
        """Answer 1: only the start and the end are trimmed."""
        inner = self.app.unique("My  \t trip   to ")
        created = self.app.write(self.author, title=f"   {inner}   ")
        self.assertTrue(accepted(created), created)
        self.assertEqual(article(created)["title"], inner)

    def test_the_slug_is_made_from_the_trimmed_title(self):
        """AC2: no odd dashes from the outer white space."""
        word = self.app.unique("trip").lower()
        created = self.app.write(self.author, title=f"\t  Summer {word}  \n")
        self.assertTrue(accepted(created), created)
        self.assertEqual(article(created)["slug"], f"summer-{word}")

    def test_an_updated_title_is_trimmed_and_the_slug_follows_it(self):
        """AC1, AC2: the same rule on update."""
        original = self.app.publish(self.author)
        word = self.app.unique("winter").lower()
        updated = self.app.edit(self.author, original["slug"], title=f"\n  Winter {word} \t")
        self.assertTrue(accepted(updated), updated)
        self.assertEqual(article(updated)["title"], f"Winter {word}")
        self.assertEqual(article(updated)["slug"], f"winter-{word}")
        self.assertEqual(article(self.app.read(f"winter-{word}"))["title"], f"Winter {word}")

    def test_a_new_title_of_only_white_space_is_refused_on_the_title_field(self):
        """AC3: empty after the trim gives 422 with the error on title (create)."""
        for title in ("   ", " \t\r\n "):
            reply = self.app.write(self.author, title=title)
            self.assertTrue(refused_as_invalid(reply, "title"), f"{title!r}: {reply}")

    def test_an_update_to_a_title_of_only_white_space_is_refused_and_the_title_stays(self):
        """AC3, answer 3: same refusal on update; nothing changes."""
        original = self.app.publish(self.author)
        reply = self.app.edit(self.author, original["slug"], title=" \t \n ")
        self.assertTrue(refused_as_invalid(reply, "title"), reply)
        stored = article(self.app.read(original["slug"]))
        self.assertEqual(stored["title"], original["title"])

    def test_an_update_to_an_empty_title_is_refused(self):
        """AC3: a title that is empty after the trim is refused on update, also the empty string."""
        original = self.app.publish(self.author)
        reply = self.app.edit(self.author, original["slug"], title="")
        self.assertTrue(refused_as_invalid(reply, "title"), reply)
        self.assertEqual(article(self.app.read(original["slug"]))["title"], original["title"])

    def test_an_update_without_a_title_keeps_the_current_title(self):
        """Answer 2."""
        original = self.app.publish(self.author)
        reply = self.app.edit(self.author, original["slug"], body="New body")
        self.assertTrue(accepted(reply), reply)
        self.assertEqual(article(reply)["title"], original["title"])
        self.assertEqual(article(reply)["slug"], original["slug"])

    def test_the_description_and_the_body_stay_exactly_as_typed(self):
        """AC4: on create and on update."""
        created = self.app.write(self.author, title="  " + self.app.unique("Keep "),
                                 description="  about \t it \n", body="\n  first line  \n\t")
        self.assertTrue(accepted(created), created)
        stored = article(self.app.read(article(created)["slug"]))
        self.assertEqual(stored["description"], "  about \t it \n")
        self.assertEqual(stored["body"], "\n  first line  \n\t")
        updated = self.app.edit(self.author, stored["slug"], description=" new desc ", body="\tnew body\n")
        self.assertTrue(accepted(updated), updated)
        self.assertEqual(article(updated)["description"], " new desc ")
        self.assertEqual(article(updated)["body"], "\tnew body\n")


if __name__ == "__main__":
    unittest.main()
