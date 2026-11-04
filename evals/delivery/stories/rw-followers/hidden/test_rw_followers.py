"""Hidden acceptance cases for rw-followers: the list of members who follow a profile.

REVIEW.md of the project binds this endpoint too: rule 1 (paging) and rule 2 (no email).
"""
import json
import unittest

from dsl.conduit import accepted, error_fields, refused_as_invalid, refused_as_unknown
from dsl_rw_followers import FollowersConduit, follower_names, profiles_count

PAUSE = 0.05  # keeps the follow moments apart


class FollowerListCases(unittest.TestCase):
    def setUp(self):
        self.app = FollowersConduit()
        self.target = self.app.register()

    def follow_in_order(self, *members):
        for member in members:
            reply = self.app.follow(member, self.target.username)
            self.assertTrue(accepted(reply), reply)
            self.app.wait(PAUSE)

    def test_the_list_holds_exactly_the_members_who_follow_the_profile(self):
        """AC1: not the members that the profile follows, not other members."""
        first, second = self.app.register(), self.app.register()
        followed_by_target, stranger = self.app.register(), self.app.register()
        self.follow_in_order(first, second)
        self.assertTrue(accepted(self.app.follow(self.target, followed_by_target.username)))
        self.assertTrue(accepted(self.app.follow(stranger, followed_by_target.username)))
        reply = self.app.followers(self.target.username)
        self.assertEqual(reply.status, 200, reply)
        self.assertEqual(sorted(follower_names(reply)), sorted([first.username, second.username]))
        self.assertEqual(profiles_count(reply), 2)

    def test_a_profile_without_followers_has_an_empty_list(self):
        """AC1."""
        reply = self.app.followers(self.target.username)
        self.assertEqual(reply.status, 200, reply)
        self.assertEqual((follower_names(reply), profiles_count(reply)), ([], 0))

    def test_the_most_recent_follower_comes_first(self):
        """AC2."""
        first, second, third = self.app.register(), self.app.register(), self.app.register()
        self.follow_in_order(second, third, first)
        reply = self.app.followers(self.target.username)
        self.assertEqual(follower_names(reply), [first.username, third.username, second.username])

    def test_a_member_who_unfollows_and_follows_again_moves_to_the_top(self):
        """Answer 1: the moment of the last follow counts."""
        early, late = self.app.register(), self.app.register()
        self.follow_in_order(early, late)
        self.assertTrue(accepted(self.app.unfollow(early, self.target.username)))
        self.assertEqual(follower_names(self.app.followers(self.target.username)), [late.username])
        self.follow_in_order(early)
        self.assertEqual(follower_names(self.app.followers(self.target.username)), [early.username, late.username])

    def test_following_tells_whether_the_current_reader_follows_each_follower(self):
        """AC3: each profile has username, bio, image and following of the current reader."""
        liked, other = self.app.register(), self.app.register()
        self.follow_in_order(liked, other)
        reader = self.app.register()
        self.assertTrue(accepted(self.app.follow(reader, liked.username)))
        reply = self.app.followers(self.target.username, reader)
        self.assertEqual(reply.status, 200, reply)
        profiles = {p["username"]: p for p in reply.body["profiles"]}
        for profile in profiles.values():
            self.assertTrue({"username", "bio", "image", "following"} <= set(profile), profile)
        self.assertIs(profiles[liked.username]["following"], True)
        self.assertIs(profiles[other.username]["following"], False)

    def test_following_is_about_the_reader_also_when_the_reader_is_the_profile_owner(self):
        """AC3: the owner reads the own list and follows one follower back."""
        back, not_back = self.app.register(), self.app.register()
        self.follow_in_order(back, not_back)
        self.assertTrue(accepted(self.app.follow(self.target, back.username)))
        reply = self.app.followers(self.target.username, self.target)
        self.assertEqual(reply.status, 200, reply)
        following = {p["username"]: p["following"] for p in reply.body["profiles"]}
        self.assertEqual(following, {back.username: True, not_back.username: False})

    def test_anybody_reads_the_list_without_a_token_and_following_is_false(self):
        """AC3, AC5."""
        follower = self.app.register()
        self.follow_in_order(follower)
        self.assertTrue(accepted(self.app.follow(self.target, follower.username)))
        reply = self.app.followers(self.target.username)
        self.assertEqual(reply.status, 200, reply)
        self.assertEqual([p["following"] for p in reply.body["profiles"]], [False])

    def test_an_unknown_user_name_gives_not_found(self):
        """AC4: while the list of a known member is found."""
        self.assertEqual(self.app.followers(self.target.username).status, 200)
        self.assertTrue(refused_as_unknown(self.app.followers(self.app.unique("nobody"))))

    def test_the_list_never_shows_an_email_address(self):
        """REVIEW 2: no email of a member in the response, also for the reader's own account."""
        follower = self.app.register()
        self.follow_in_order(follower)
        for viewer in (None, follower, self.target):
            reply = self.app.followers(self.target.username, viewer)
            self.assertEqual(reply.status, 200, reply)
            text = json.dumps(reply.body)
            self.assertNotIn("@example.com", text)
            self.assertNotIn('"email"', text)


class FollowerPagingCases(unittest.TestCase):
    """REVIEW 1 on the new list endpoint. One profile with 21 followers; the cases only read it."""

    FOLLOWERS = 21

    @classmethod
    def setUpClass(cls):
        cls.app = FollowersConduit()
        cls.target = cls.app.register()
        cls.order = []
        for _ in range(cls.FOLLOWERS):
            member = cls.app.register()
            reply = cls.app.follow(member, cls.target.username)
            assert reply.status == 200, reply
            cls.order.insert(0, member.username)
            cls.app.wait(PAUSE)

    def page(self, **query):
        return self.app.followers(self.target.username, **query)

    def assert_refused_on(self, reply, field):
        self.assertTrue(refused_as_invalid(reply, field), reply)
        with_messages = {name for name in error_fields(reply) if reply.body["errors"][name]}
        self.assertEqual(with_messages, {field}, reply)

    def test_the_list_has_20_followers_and_the_count_of_all_when_no_limit_is_sent(self):
        """REVIEW 1: limit 20 by default; the count of all matching items next to the list."""
        reply = self.page()
        self.assertEqual(follower_names(reply), self.order[:20])
        self.assertEqual(profiles_count(reply), self.FOLLOWERS)

    def test_limit_and_offset_select_a_page_in_the_order_of_the_list(self):
        """REVIEW 1, AC2."""
        reply = self.page(limit=3, offset=2)
        self.assertEqual(follower_names(reply), self.order[2:5])
        self.assertEqual(profiles_count(reply), self.FOLLOWERS)

    def test_a_limit_of_100_is_allowed(self):
        """REVIEW 1: at most 100."""
        reply = self.page(limit=100)
        self.assertEqual(follower_names(reply), self.order)

    def test_a_limit_outside_1_to_100_is_refused_on_the_limit_field(self):
        """REVIEW 1: below 1 or above 100 gives 422 on limit; 1 is allowed."""
        for limit in (101, 0, -1):
            self.assert_refused_on(self.page(limit=limit), "limit")
        self.assertEqual(follower_names(self.page(limit=1)), self.order[:1])

    def test_a_negative_offset_is_refused_on_the_offset_field(self):
        """REVIEW 1."""
        self.assert_refused_on(self.page(offset=-1), "offset")

    def test_an_offset_past_the_end_gives_an_empty_list_with_the_full_count(self):
        """REVIEW 1: the count of all matching items."""
        reply = self.page(offset=self.FOLLOWERS)
        self.assertEqual(reply.status, 200, reply)
        self.assertEqual((follower_names(reply), profiles_count(reply)), ([], self.FOLLOWERS))


if __name__ == "__main__":
    unittest.main()
