"""Hidden acceptance cases for rw-account-delete: a member deletes the own account and content."""
import unittest

from dsl.conduit import (accepted, article, articles, articles_count, refused_as_invalid, refused_as_signed_out,
                         refused_as_unknown, slugs)
from dsl_rw_account_delete import AccountConduit


class AccountDeleteCases(unittest.TestCase):
    def setUp(self):
        self.app = AccountConduit()
        self.other = self.app.register()

    def leaving_member(self):
        """Registered last on purpose: the deleted account is the most recent one."""
        return self.app.register()

    def delete(self, member):
        reply = self.app.delete_account(member)
        self.assertEqual(reply.status, 204, reply)

    # AC1, AC2, answer 3
    def test_a_member_deletes_the_own_account_with_the_password(self):
        """AC1: 204, and the account is gone."""
        member = self.leaving_member()
        self.delete(member)
        self.assertTrue(refused_as_signed_out(self.app.current_user(member.token)))

    def test_a_wrong_password_is_refused_and_nothing_is_deleted(self):
        """AC2."""
        member = self.leaving_member()
        slug = self.app.publish(member)["slug"]
        reply = self.app.delete_account(member, password="not-the-password")
        self.assertTrue(refused_as_invalid(reply, "password"), reply)
        self.assertEqual(self.app.current_user(member.token).status, 200)
        self.assertEqual(self.app.read(slug, self.other).status, 200)
        self.assertEqual(self.app.sign_in(member.email, member.password).status, 200)

    def test_a_missing_password_is_refused_and_nothing_is_deleted(self):
        """AC2."""
        member = self.leaving_member()
        reply = self.app.delete_account(member, password=None)
        self.assertTrue(refused_as_invalid(reply, "password"), reply)
        self.assertEqual(self.app.current_user(member.token).status, 200)
        self.assertEqual(self.app.profile(member.username).status, 200)

    def test_deleting_an_account_without_a_token_is_refused(self):
        """Answer 3: 401."""
        reply = self.app.delete_account(None, password="secret-pass-1")
        self.assertTrue(refused_as_signed_out(reply), reply)

    # AC3, answer 2
    def test_the_old_token_is_refused_on_each_signed_in_action(self):
        """AC3: each request with the old token gives 401."""
        member = self.leaving_member()
        slug = self.app.publish(self.other)["slug"]
        self.delete(member)
        replies = {
            "current user": self.app.current_user(member.token),
            "update user": self.app.update_user(member, bio="still here"),
            "write": self.app.write(member),
            "feed": self.app.feed(member),
            "follow": self.app.follow(member, self.other.username),
            "favorite": self.app.favorite(member, slug),
            "comment": self.app.comment(member, slug, "Still here"),
            "delete again": self.app.delete_account(member),
        }
        for action, reply in replies.items():
            self.assertTrue(refused_as_signed_out(reply), f"{action}: {reply}")

    def test_the_old_token_is_refused_also_on_pages_that_need_no_sign_in(self):
        """AC3: 'each request with a token that was issued to the deleted account is refused with 401'."""
        member = self.leaving_member()
        slug = self.app.publish(self.other)["slug"]
        self.delete(member)
        replies = {
            "article list": self.app.browse(member),
            "single article": self.app.read(slug, member),
            "profile": self.app.profile(self.other.username, member),
            "comments": self.app.comments(slug, member),
        }
        for action, reply in replies.items():
            self.assertTrue(refused_as_signed_out(reply), f"{action}: {reply}")

    def test_signing_in_with_the_old_email_and_password_fails(self):
        """AC3."""
        member = self.leaving_member()
        self.delete(member)
        reply = self.app.sign_in(member.email, member.password)
        self.assertTrue(400 <= reply.status < 500, reply)

    def test_the_old_token_does_not_work_for_an_account_registered_right_after(self):
        """Answer 2: a token issued before the delete works for nobody, also not for a newer account."""
        member = self.leaving_member()
        self.delete(member)
        newcomer = self.app.register()
        reply = self.app.current_user(member.token)
        self.assertTrue(refused_as_signed_out(reply), reply)
        self.assertNotIn(newcomer.username, str(reply.body))
        self.assertTrue(refused_as_signed_out(self.app.write(member)))
        self.assertEqual(articles_count(self.app.browse(author=newcomer.username)), 0)

    def test_the_old_token_does_not_work_after_the_same_name_registers_again(self):
        """Answer 2, AC9."""
        member = self.leaving_member()
        self.delete(member)
        again = self.app.try_register(member.username, member.email)
        self.assertIn(again.status, (200, 201), again)
        self.assertTrue(refused_as_signed_out(self.app.current_user(member.token)))
        self.assertTrue(refused_as_signed_out(self.app.update_user(member, bio="taken over")))

    # AC4 to AC8
    def test_the_articles_of_the_member_are_deleted_with_the_comments_on_them(self):
        """AC4."""
        member = self.leaving_member()
        first = self.app.publish(member)["slug"]
        second = self.app.publish(member)["slug"]
        self.assertTrue(accepted(self.app.comment(self.other, first, "Nice")))
        self.delete(member)
        for slug in (first, second):
            self.assertTrue(refused_as_unknown(self.app.read(slug, self.other)), slug)
            self.assertTrue(refused_as_unknown(self.app.comments(slug)), slug)
        reply = self.app.browse(author=member.username)
        self.assertEqual((articles(reply), articles_count(reply)), ([], 0))

    def test_the_comments_of_the_member_on_other_articles_are_deleted(self):
        """AC5: the comments of other members stay."""
        slug = self.app.publish(self.other)["slug"]
        member = self.leaving_member()
        self.assertTrue(accepted(self.app.comment(member, slug, "Mine")))
        kept = self.app.comment(self.other, slug, "Theirs").body["comment"]["id"]
        self.assertTrue(accepted(self.app.comment(member, slug, "Mine again")))
        self.delete(member)
        self.assertEqual(self.app.comment_ids(slug), [kept])

    def test_the_favorites_of_the_member_are_removed_and_the_counts_go_down(self):
        """AC6: in the single article and in the list."""
        slug = self.app.publish(self.other)["slug"]
        fan = self.app.register()
        member = self.leaving_member()
        self.app.favorite(fan, slug)
        self.app.favorite(member, slug)
        self.assertEqual(article(self.app.read(slug))["favoritesCount"], 2)
        self.delete(member)
        self.assertEqual(article(self.app.read(slug))["favoritesCount"], 1)
        listed = [a for a in articles(self.app.browse(author=self.other.username)) if a["slug"] == slug]
        self.assertEqual(listed[0]["favoritesCount"], 1)

    def test_the_follows_of_and_on_the_member_are_removed(self):
        """AC7, AC9: a new account with the same name follows nobody and has no followers."""
        follower = self.app.register()
        followed = self.app.register()
        member = self.leaving_member()
        self.assertTrue(accepted(self.app.follow(member, followed.username)))
        self.assertTrue(accepted(self.app.follow(follower, member.username)))
        self.delete(member)
        again = self.app.try_register(member.username, member.email, member.password)
        self.assertIn(again.status, (200, 201), again)
        new_token = again.body["user"]["token"]
        self.app.publish(followed)
        self.app.publish(new_token)
        feed = self.app.feed(new_token)
        self.assertEqual((articles(feed), articles_count(feed)), ([], 0))
        self.assertIs(self.app.profile(member.username, follower).body["profile"]["following"], False)
        self.assertEqual(articles_count(self.app.feed(follower)), 0)

    def test_the_profile_of_the_deleted_member_is_not_found(self):
        """AC8."""
        member = self.leaving_member()
        self.delete(member)
        self.assertTrue(refused_as_unknown(self.app.profile(member.username)))
        self.assertTrue(refused_as_unknown(self.app.profile(member.username, self.other)))

    # AC9
    def test_a_new_member_with_the_same_name_and_email_starts_with_nothing(self):
        """AC9: no articles, favorites or comments of the old account."""
        slug = self.app.publish(self.other)["slug"]
        member = self.leaving_member()
        self.app.publish(member)
        self.app.favorite(member, slug)
        self.app.comment(member, slug, "Old words")
        self.delete(member)
        again = self.app.try_register(member.username, member.email)
        self.assertIn(again.status, (200, 201), again)
        self.assertEqual(articles_count(self.app.browse(author=member.username)), 0)
        self.assertEqual(articles_count(self.app.browse(favorited=member.username)), 0)
        self.assertIs(article(self.app.read(slug, again.body["user"]["token"]))["favorited"], False)
        authors = [c["author"]["username"] for c in self.app.comments(slug).body["comments"]]
        self.assertNotIn(member.username, authors)

    # AC10, answers 4 and 5
    def test_the_tags_used_only_by_the_deleted_member_leave_the_tag_list(self):
        """AC10: a tag still used by another article stays."""
        own_tag, shared_tag = self.app.unique("own"), self.app.unique("shared")
        self.app.publish(self.other, tags=[shared_tag])
        member = self.leaving_member()
        self.app.publish(member, tags=[own_tag, shared_tag])
        self.assertIn(own_tag, self.app.tags().body["tags"])
        self.delete(member)
        tags = self.app.tags().body["tags"]
        self.assertNotIn(own_tag, tags)
        self.assertIn(shared_tag, tags)

    def test_a_tag_leaves_the_tag_list_when_its_last_article_is_deleted(self):
        """Answer 4: also when an author deletes an article."""
        tag = self.app.unique("gone")
        slug = self.app.publish(self.other, tags=[tag])["slug"]
        self.assertIn(tag, self.app.tags().body["tags"])
        self.assertTrue(accepted(self.app.remove(self.other, slug)))
        self.assertNotIn(tag, self.app.tags().body["tags"])

    def test_a_tag_leaves_the_tag_list_when_it_is_removed_from_its_last_article(self):
        """Answer 4: also when an author removes a tag from an article."""
        dropped, kept = self.app.unique("dropped"), self.app.unique("kept")
        slug = self.app.publish(self.other, tags=[dropped, kept])["slug"]
        self.assertTrue(accepted(self.app.edit(self.other, slug, tagList=[kept])))
        tags = self.app.tags().body["tags"]
        self.assertNotIn(dropped, tags)
        self.assertIn(kept, tags)

    def test_members_who_favorited_a_deleted_article_simply_no_longer_see_it(self):
        """Answer 5."""
        member = self.leaving_member()
        slug = self.app.publish(member)["slug"]
        self.assertTrue(accepted(self.app.favorite(self.other, slug)))
        self.delete(member)
        reply = self.app.browse(self.other, favorited=self.other.username)
        self.assertEqual(reply.status, 200, reply)
        self.assertEqual((slugs(reply), articles_count(reply)), ([], 0))


if __name__ == "__main__":
    unittest.main()
