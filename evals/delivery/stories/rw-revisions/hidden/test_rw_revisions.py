"""Hidden acceptance cases for rw-revisions: versioned edits, the revision list and old slugs."""
import threading
import unittest

from dsl.conduit import (accepted, article, articles, refused_as_conflict, refused_as_forbidden, refused_as_invalid,
                         refused_as_signed_out, refused_as_unknown)
from dsl_rw_revisions import RevisionConduit, revision_list

ROUNDS = 6


class RevisionCases(unittest.TestCase):
    def setUp(self):
        self.app = RevisionConduit()
        self.author = self.app.register()
        self.other = self.app.register()

    def new_title(self, prefix="Rev"):
        title = self.app.unique(f"{prefix} ")
        return title, title.lower().replace(" ", "-")

    def current(self, slug, viewer=None):
        reply = self.app.read(slug, viewer)
        self.assertEqual(reply.status, 200, reply)
        return article(reply)

    # AC1
    def test_a_new_article_has_version_1_in_each_response(self):
        """AC1: create, single article and list."""
        created = self.app.write(self.author)
        self.assertTrue(accepted(created), created)
        self.assertEqual(article(created).get("version"), 1)
        slug = article(created)["slug"]
        self.assertEqual(self.current(slug, self.other).get("version"), 1)
        listed = articles(self.app.browse(author=self.author.username))
        self.assertEqual([a.get("version") for a in listed], [1])

    def test_each_update_that_changes_the_article_raises_the_version_by_one(self):
        """AC1: body, then title, then tags."""
        slug = self.app.publish(self.author, tags=["x"])["slug"]
        second = self.app.revised(self.author, slug, 1, body="Second body")
        self.assertEqual(second.get("version"), 2)
        title, _ = self.new_title()
        third = self.app.revised(self.author, slug, 2, title=title)
        self.assertEqual(third.get("version"), 3)
        fourth = self.app.revised(self.author, third["slug"], 3, tagList=["x", "y"])
        self.assertEqual(fourth.get("version"), 4)
        self.assertEqual(self.current(fourth["slug"]).get("version"), 4)

    def test_a_version_given_on_create_is_ignored(self):
        """Answer 6."""
        created = self.app.write(self.author, version=7)
        self.assertTrue(accepted(created), created)
        self.assertEqual(article(created).get("version"), 1)

    def test_an_update_that_changes_nothing_keeps_the_version_and_makes_no_revision(self):
        """Answer 1: same title, description, body and tags."""
        original = self.app.publish(self.author, tags=["a", "b"])
        same = self.app.revise(self.author, original["slug"], 1, title=original["title"],
                               description=original["description"], body=original["body"], tagList=["a", "b"])
        self.assertEqual(same.status, 200, same)
        self.assertEqual(article(same).get("version"), 1)
        reply = self.app.revisions(original["slug"], self.author)
        self.assertEqual(reply.status, 200, reply)
        self.assertEqual((revision_list(reply), reply.body.get("revisionsCount")), ([], 0))

    # AC2, AC3, AC4
    def test_an_update_from_an_older_version_is_refused_and_nothing_changes(self):
        """AC2: 409 with an error on version."""
        slug = self.app.publish(self.author, body="Phone text")["slug"]
        self.app.revised(self.author, slug, 1, body="Newer text")
        stale = self.app.revise(self.author, slug, 1, body="Laptop text")
        self.assertTrue(refused_as_conflict(stale, "version"), stale)
        now = self.current(slug)
        self.assertEqual((now["body"], now.get("version")), ("Newer text", 2))
        self.assertEqual(self.app.revisions(slug, self.author).body.get("revisionsCount"), 1)

    def test_an_update_from_a_version_that_does_not_exist_yet_is_refused(self):
        """AC2: any version that is not the current one."""
        slug = self.app.publish(self.author)["slug"]
        reply = self.app.revise(self.author, slug, 2, body="From the future")
        self.assertTrue(refused_as_conflict(reply, "version"), reply)
        self.assertEqual(self.current(slug).get("version"), 1)

    def test_an_update_without_a_version_is_refused(self):
        """AC3: 422 with an error on version, and nothing changes."""
        original = self.app.publish(self.author)
        reply = self.app.edit(self.author, original["slug"], body="No version sent")
        self.assertTrue(refused_as_invalid(reply, "version"), reply)
        self.assertEqual(self.current(original["slug"])["body"], original["body"])

    def test_two_updates_from_the_same_version_at_the_same_time_give_one_success_and_one_conflict(self):
        """AC4: repeated rounds on one article."""
        slug = self.app.publish(self.author)["slug"]
        version = 1
        for round_number in range(ROUNDS):
            gate = threading.Barrier(2)
            replies = {}

            def update(side):
                gate.wait()
                replies[side] = self.app.revise(self.author, slug, version, body=f"{side} {round_number}")

            threads = [threading.Thread(target=update, args=(side,)) for side in ("laptop", "phone")]
            for thread in threads:
                thread.start()
            for thread in threads:
                thread.join(30)
            statuses = sorted(reply.status for reply in replies.values())
            self.assertEqual(statuses, [200, 409], f"round {round_number}: {replies}")
            winner = next(side for side, reply in replies.items() if reply.status == 200)
            now = self.current(slug)
            self.assertEqual((now["body"], now.get("version")), (f"{winner} {round_number}", version + 1))
            version += 1
        self.assertEqual(self.app.revisions(slug, self.author).body.get("revisionsCount"), ROUNDS)

    # AC5, AC6
    def test_the_revisions_are_the_earlier_versions_newest_first(self):
        """AC5: version, title, description, body, tagList and updatedAt of each earlier version."""
        first_title, _ = self.new_title()
        slug = self.app.publish(self.author, title=first_title, description="D1", body="B1", tags=["t1", "t2"])["slug"]
        self.app.revised(self.author, slug, 1, body="B2", tagList=["t3"])
        third_title, _ = self.new_title()
        current = self.app.revised(self.author, slug, 2, title=third_title, description="D3")
        reply = self.app.revisions(current["slug"], self.author)
        self.assertEqual(reply.status, 200, reply)
        revisions = revision_list(reply)
        self.assertEqual(reply.body.get("revisionsCount"), 2)
        self.assertEqual([r["version"] for r in revisions], [2, 1])
        newest, oldest = revisions
        self.assertEqual((newest["title"], newest["description"], newest["body"], sorted(newest["tagList"])),
                         (first_title, "D1", "B2", ["t3"]))
        self.assertEqual((oldest["title"], oldest["description"], oldest["body"], sorted(oldest["tagList"])),
                         (first_title, "D1", "B1", ["t1", "t2"]))
        for revision in revisions:
            self.assertRegex(revision["updatedAt"], r"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}")

    def test_a_new_article_has_no_revisions(self):
        """AC5: only earlier versions are listed."""
        slug = self.app.publish(self.author)["slug"]
        reply = self.app.revisions(slug, self.author)
        self.assertEqual(reply.status, 200, reply)
        self.assertEqual((revision_list(reply), reply.body.get("revisionsCount")), ([], 0))

    def test_another_member_cannot_read_the_revisions(self):
        """AC6: 403."""
        slug = self.app.publish(self.author)["slug"]
        self.app.revised(self.author, slug, 1, body="Private draft history")
        reply = self.app.revisions(slug, self.other)
        self.assertTrue(refused_as_forbidden(reply), reply)
        self.assertNotIn("Body text", str(reply.body))

    def test_the_revisions_need_a_signed_in_member(self):
        """AC6: 401 without a token."""
        slug = self.app.publish(self.author)["slug"]
        self.app.revised(self.author, slug, 1, body="Second")
        self.assertTrue(refused_as_signed_out(self.app.revisions(slug)))

    def test_the_revisions_of_an_unknown_article_are_not_found(self):
        """AC6: 404, while the revisions of a known article are found."""
        slug = self.app.publish(self.author)["slug"]
        self.assertEqual(self.app.revisions(slug, self.author).status, 200)
        self.assertTrue(refused_as_unknown(self.app.revisions(self.app.unique("no-such-article-"), self.author)))

    # AC7, AC8, AC9
    def test_the_old_slug_still_reads_the_article_and_its_comments(self):
        """AC7: the response has the new slug."""
        old_title, old_slug = self.new_title("Old")
        new_title, new_slug = self.new_title("New")
        self.app.publish(self.author, title=old_title)
        self.assertTrue(accepted(self.app.comment(self.other, old_slug, "Kept comment")))
        moved = self.app.revised(self.author, old_slug, 1, title=new_title)
        self.assertEqual(moved["slug"], new_slug)
        via_old = self.app.read(old_slug, self.other)
        self.assertEqual(via_old.status, 200, via_old)
        self.assertEqual((article(via_old)["slug"], article(via_old)["title"]), (new_slug, new_title))
        comments = self.app.comments(old_slug)
        self.assertEqual(comments.status, 200, comments)
        self.assertEqual([c["body"] for c in comments.body["comments"]], ["Kept comment"])

    def test_an_old_slug_stays_reserved_for_its_article(self):
        """AC8: a new article whose title gives the old slug gets another slug."""
        old_title, old_slug = self.new_title("Taken")
        new_title, new_slug = self.new_title("Moved")
        self.app.publish(self.author, title=old_title)
        self.app.revised(self.author, old_slug, 1, title=new_title)
        newcomer = self.app.publish(self.other, title=old_title)
        self.assertNotEqual(newcomer["slug"], old_slug)
        self.assertNotEqual(newcomer["slug"], new_slug)
        self.assertEqual(self.current(old_slug)["slug"], new_slug)
        self.assertEqual(self.current(newcomer["slug"])["author"]["username"], self.other.username)

    def test_deleting_an_article_frees_its_slugs_and_drops_its_history(self):
        """AC9: a new article with an old title gets that slug and starts with no revisions."""
        old_title, old_slug = self.new_title("Gone")
        new_title, new_slug = self.new_title("Later")
        self.app.publish(self.author, title=old_title)
        self.app.revised(self.author, old_slug, 1, title=new_title)
        self.assertTrue(accepted(self.app.remove(self.author, new_slug)))
        self.assertTrue(refused_as_unknown(self.app.read(old_slug)))
        self.assertTrue(refused_as_unknown(self.app.read(new_slug)))
        reborn = self.app.publish(self.other, title=old_title)
        self.assertEqual(reborn["slug"], old_slug)
        self.assertEqual(reborn.get("version"), 1)
        history = self.app.revisions(old_slug, self.other)
        self.assertEqual((history.status, history.body.get("revisionsCount")), (200, 0), history)
        self.assertEqual(self.app.publish(self.other, title=new_title)["slug"], new_slug)

    def test_an_old_slug_cannot_be_used_to_update_or_delete(self):
        """Answer 3: 404, and the article stays as it was."""
        old_title, old_slug = self.new_title("Before")
        new_title, new_slug = self.new_title("After")
        self.app.publish(self.author, title=old_title)
        self.app.revised(self.author, old_slug, 1, title=new_title)
        self.assertTrue(refused_as_unknown(self.app.revise(self.author, old_slug, 2, body="Via old link")))
        self.assertTrue(refused_as_unknown(self.app.remove(self.author, old_slug)))
        now = self.current(new_slug)
        self.assertEqual((now["body"], now.get("version")), ("Body text", 2))

    def test_favorites_go_through_the_current_slug_only(self):
        """Answer 4."""
        old_title, old_slug = self.new_title("Fav")
        new_title, new_slug = self.new_title("Faved")
        self.app.publish(self.author, title=old_title)
        self.app.revised(self.author, old_slug, 1, title=new_title)
        self.assertEqual(self.app.read(old_slug).status, 200)
        self.assertTrue(refused_as_unknown(self.app.favorite(self.other, old_slug)))
        self.assertEqual(self.current(new_slug)["favoritesCount"], 0)
        self.assertEqual(self.app.favorite(self.other, new_slug).status, 200)
        self.assertTrue(refused_as_unknown(self.app.unfavorite(self.other, old_slug)))
        self.assertEqual(self.current(new_slug)["favoritesCount"], 1)

    def test_an_article_can_go_back_to_an_earlier_title_and_slug(self):
        """Answer 7."""
        first_title, first_slug = self.new_title("First")
        second_title, second_slug = self.new_title("Second")
        self.app.publish(self.author, title=first_title)
        self.app.revised(self.author, first_slug, 1, title=second_title)
        back = self.app.revise(self.author, second_slug, 2, title=first_title)
        self.assertEqual(back.status, 200, back)
        self.assertEqual((article(back)["slug"], article(back).get("version")), (first_slug, 3))
        self.assertEqual(self.current(first_slug)["title"], first_title)
        self.assertEqual(self.current(second_slug)["slug"], first_slug)

    # existing behaviour that the version check must keep (the upstream PUT checks are off for this story)
    def test_an_update_changes_only_the_fields_it_sends(self):
        """AC2 with existing behaviour: title, description and tags stay when only the body is sent."""
        original = self.app.publish(self.author, tags=["keep", "these"])
        updated = self.app.revised(self.author, original["slug"], 1, body="Only the body")
        self.assertEqual((updated["title"], updated["description"], updated["slug"], sorted(updated["tagList"])),
                         (original["title"], original["description"], original["slug"], ["keep", "these"]))

    def test_another_member_cannot_update_the_article(self):
        """Existing behaviour: 403 also with the current version."""
        original = self.app.publish(self.author)
        reply = self.app.revise(self.other, original["slug"], 1, body="Not mine")
        self.assertTrue(refused_as_forbidden(reply), reply)
        self.assertEqual(self.current(original["slug"])["body"], original["body"])


if __name__ == "__main__":
    unittest.main()
