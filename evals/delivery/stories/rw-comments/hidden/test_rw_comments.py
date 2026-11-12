"""Hidden acceptance cases for rw-comments: readers add, read and remove comments on an article."""
import unittest

from dsl.conduit import Conduit, accepted, refused_as_forbidden, refused_as_invalid, refused_as_unknown


def comment_ids(reply):
    body = reply.body if isinstance(reply.body, dict) else {}
    return [item["id"] for item in body.get("comments", [])]


class CommentCases(unittest.TestCase):
    def setUp(self):
        self.app = Conduit()
        self.author = self.app.register()
        self.reader = self.app.register()
        self.slug = self.app.publish(self.author)["slug"]

    def add(self, member, slug=None, body=None):
        reply = self.app.comment(member, slug or self.slug, body or self.app.unique("Comment "))
        self.assertIn(reply.status, (200, 201), reply)
        return reply.body["comment"]

    def test_a_new_comment_has_its_fields_and_the_profile_of_the_reader_who_wrote_it(self):
        """AC1: id, createdAt, updatedAt, body and author (a profile of the commenter, not of the article author)."""
        text = self.app.unique("Great point ")
        reply = self.app.comment(self.reader, self.slug, text)
        self.assertIn(reply.status, (200, 201), reply)
        comment = reply.body["comment"]
        self.assertIsInstance(comment["id"], int)
        self.assertEqual(comment["body"], text)
        self.assertRegex(comment["createdAt"], r"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}")
        self.assertRegex(comment["updatedAt"], r"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}")
        self.assertEqual(comment["author"]["username"], self.reader.username)
        for field in ("bio", "image", "following"):
            self.assertIn(field, comment["author"])

    def test_each_visitor_reads_the_comments_of_all_readers_oldest_first(self):
        """AC2, answer 4: also without a token."""
        first = self.add(self.reader)["id"]
        second = self.add(self.author)["id"]
        third = self.add(self.reader)["id"]
        signed_out = self.app.comments(self.slug)
        self.assertEqual(signed_out.status, 200, signed_out)
        self.assertEqual(comment_ids(signed_out), [first, second, third])
        self.assertEqual(comment_ids(self.app.comments(self.slug, self.app.register())), [first, second, third])

    def test_the_comments_of_another_article_are_not_listed(self):
        """AC2: the list holds the comments of this article only."""
        other_slug = self.app.publish(self.author)["slug"]
        mine = self.add(self.reader)["id"]
        self.add(self.reader, other_slug)
        self.assertEqual(comment_ids(self.app.comments(self.slug)), [mine])

    def test_a_reader_removes_the_own_comment_and_the_other_comments_stay(self):
        """AC3, answer 6: the reply has an empty body."""
        gone = self.add(self.reader)["id"]
        kept = self.add(self.reader)["id"]
        reply = self.app.remove_comment(self.reader, self.slug, gone)
        self.assertTrue(accepted(reply), reply)
        self.assertIn(reply.body, (None, ""), reply)
        self.assertEqual(comment_ids(self.app.comments(self.slug)), [kept])

    def test_a_reader_who_is_not_the_author_cannot_remove_a_comment(self):
        """AC4, answer 1: 403 and the comment stays."""
        comment = self.add(self.reader)["id"]
        reply = self.app.remove_comment(self.app.register(), self.slug, comment)
        self.assertTrue(refused_as_forbidden(reply), reply)
        self.assertEqual(comment_ids(self.app.comments(self.slug)), [comment])

    def test_the_author_of_the_article_cannot_remove_a_comment_of_a_reader(self):
        """AC4: only the author of the comment can remove it, not the author of the article."""
        comment = self.add(self.reader)["id"]
        reply = self.app.remove_comment(self.author, self.slug, comment)
        self.assertTrue(refused_as_forbidden(reply), reply)
        self.assertEqual(comment_ids(self.app.comments(self.slug)), [comment])

    def test_a_comment_cannot_be_removed_through_another_article(self):
        """Answer 3: a comment id that belongs to another article gives 404, and the comment stays."""
        comment = self.add(self.reader)["id"]
        other_slug = self.app.publish(self.reader)["slug"]
        reply = self.app.remove_comment(self.reader, other_slug, comment)
        self.assertTrue(refused_as_unknown(reply), reply)
        self.assertEqual(comment_ids(self.app.comments(self.slug)), [comment])

    def test_removing_a_comment_that_was_already_removed_gives_not_found(self):
        """Answer 3: a comment that does not exist gives 404."""
        comment = self.add(self.reader)["id"]
        self.assertTrue(accepted(self.app.remove_comment(self.reader, self.slug, comment)))
        self.assertTrue(refused_as_unknown(self.app.remove_comment(self.reader, self.slug, comment)))

    def test_a_comment_of_only_spaces_is_refused_on_the_body_field(self):
        """AC5, answer 5."""
        for body in ("", "    ", " \t\n "):
            reply = self.app.comment(self.reader, self.slug, body)
            self.assertTrue(refused_as_invalid(reply, "body"), f"{body!r}: {reply}")
        self.assertEqual(comment_ids(self.app.comments(self.slug)), [])

    def test_following_tells_whether_the_current_reader_follows_the_comment_author(self):
        """AC6: per reader, and false without a token."""
        commenter = self.app.register()
        self.add(commenter)
        self.assertTrue(accepted(self.app.follow(self.reader, commenter.username)))
        def following(viewer):
            reply = self.app.comments(self.slug, viewer)
            return [c["author"]["following"] for c in reply.body["comments"]]
        self.assertEqual(following(self.reader), [True])
        self.assertEqual(following(self.author), [False])
        self.assertEqual(following(None), [False])

    def test_an_article_with_comments_can_be_deleted_and_its_comments_go_with_it(self):
        """AC7."""
        self.add(self.reader)
        self.add(self.author)
        reply = self.app.remove(self.author, self.slug)
        self.assertTrue(accepted(reply), reply)
        self.assertTrue(refused_as_unknown(self.app.read(self.slug)))
        self.assertTrue(refused_as_unknown(self.app.comments(self.slug)))


if __name__ == "__main__":
    unittest.main()
