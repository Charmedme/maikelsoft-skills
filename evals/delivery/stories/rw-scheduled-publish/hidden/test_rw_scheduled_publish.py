"""Hidden acceptance cases for rw-scheduled-publish: an article with a publish time in the future.

The system runs on the same machine, so the clock of this process stands in for the server clock.
Only two cases wait for a publish time; each waits about three seconds.
"""
import datetime
import unittest

from dsl.conduit import accepted, article, articles, articles_count, refused_as_invalid, refused_as_unknown, slugs
from dsl_rw_scheduled_publish import UTC, ScheduleConduit, moment, parse_utc

LATER = 3  # seconds until a near publish time


def far_future():
    return moment(30 * 24 * 3600, offset_hours=1)


class ScheduleCases(unittest.TestCase):
    def setUp(self):
        self.app = ScheduleConduit()
        self.author = self.app.register()
        self.reader = self.app.register()

    def assert_utc_text(self, value, where):
        self.assertIsNotNone(parse_utc(value), f"{where}: publishAt {value!r} is not a UTC time ending in Z")
        return parse_utc(value)

    # AC1, AC2
    def test_an_article_without_a_publish_time_is_visible_to_other_readers_at_once(self):
        """AC1."""
        slug = self.app.publish(self.author)["slug"]
        self.assertEqual(self.app.read(slug, self.reader).status, 200)
        self.assertIn(slug, slugs(self.app.browse(author=self.author.username)))

    def test_each_article_response_has_publish_at_in_utc_ending_in_z(self):
        """AC2, AC1: create, update, single, list and feed; without a publish time it is now."""
        created = self.app.write(self.author)
        self.assertTrue(accepted(created), created)
        slug = article(created)["slug"]
        when = self.assert_utc_text(article(created).get("publishAt"), "create")
        self.assertLess(abs((when - datetime.datetime.now(UTC)).total_seconds()), 120)
        self.assert_utc_text(article(self.app.edit(self.author, slug, body="Edited")).get("publishAt"), "update")
        self.assert_utc_text(article(self.app.read(slug, self.reader)).get("publishAt"), "single")
        listed = [a for a in articles(self.app.browse(author=self.author.username)) if a["slug"] == slug]
        self.assertEqual(len(listed), 1)
        self.assert_utc_text(listed[0].get("publishAt"), "list")
        self.app.follow(self.reader, self.author.username)
        fed = [a for a in articles(self.app.feed(self.reader)) if a["slug"] == slug]
        self.assertEqual(len(fed), 1)
        self.assert_utc_text(fed[0].get("publishAt"), "feed")

    def test_a_publish_time_with_an_offset_comes_back_as_the_same_moment_in_utc(self):
        """AC1, AC2: 2099-01-01T10:00:00+02:00 is 08:00 UTC."""
        created = self.app.schedule(self.author, "2099-01-01T10:00:00+02:00")
        self.assertTrue(accepted(created), created)
        expected = datetime.datetime(2099, 1, 1, 8, 0, 0, tzinfo=UTC)
        self.assertEqual(self.assert_utc_text(article(created).get("publishAt"), "create"), expected)
        own = self.app.read(article(created)["slug"], self.author)
        self.assertEqual(self.assert_utc_text(article(own).get("publishAt"), "single"), expected)

    # AC3: invisible for others on each read path
    def test_a_scheduled_article_does_not_exist_for_other_readers_or_without_a_token(self):
        """AC3: the single article gives 404."""
        slug = self.app.scheduled(self.author, far_future())["slug"]
        self.assertTrue(refused_as_unknown(self.app.read(slug, self.reader)))
        self.assertTrue(refused_as_unknown(self.app.read(slug)))

    def test_a_scheduled_article_is_not_in_the_article_list_of_others(self):
        """AC3: the list, with and without the author filter, signed in and signed out."""
        slug = self.app.scheduled(self.author, far_future())["slug"]
        for viewer in (self.reader, None):
            self.assertNotIn(slug, slugs(self.app.browse(viewer)))
            self.assertNotIn(slug, slugs(self.app.browse(viewer, author=self.author.username)))

    def test_a_scheduled_article_is_not_in_the_feed_of_a_follower(self):
        """AC3: the feed."""
        self.assertTrue(accepted(self.app.follow(self.reader, self.author.username)))
        visible = self.app.publish(self.author)["slug"]
        hidden = self.app.scheduled(self.author, far_future())["slug"]
        feed = self.app.feed(self.reader)
        self.assertIn(visible, slugs(feed))
        self.assertNotIn(hidden, slugs(feed))
        self.assertEqual(articles_count(feed), 1)

    def test_a_scheduled_article_is_not_found_by_its_tag(self):
        """AC3, AC9: the tag filter, with its count."""
        tag = self.app.unique("event")
        visible = self.app.publish(self.author, tags=[tag])["slug"]
        self.app.scheduled(self.author, far_future(), tags=[tag])
        for viewer in (self.reader, None):
            reply = self.app.browse(viewer, tag=tag)
            self.assertEqual((slugs(reply), articles_count(reply)), ([visible], 1))

    def test_an_article_moved_back_into_the_future_leaves_the_favorite_filter(self):
        """AC3, answer 3: the favorite filter; also for the reader who marked it."""
        slug = self.app.publish(self.author)["slug"]
        self.assertTrue(accepted(self.app.favorite(self.reader, slug)))
        self.assertEqual(slugs(self.app.browse(favorited=self.reader.username)), [slug])
        self.assertTrue(accepted(self.app.reschedule(self.author, slug, far_future())))
        for viewer in (self.reader, None):
            reply = self.app.browse(viewer, favorited=self.reader.username)
            self.assertEqual((slugs(reply), articles_count(reply)), ([], 0))

    def test_an_author_can_hide_a_visible_article_again_with_a_later_publish_time(self):
        """Answer 3: single article and list."""
        slug = self.app.publish(self.author)["slug"]
        self.assertEqual(self.app.read(slug, self.reader).status, 200)
        reply = self.app.reschedule(self.author, slug, far_future())
        self.assertTrue(accepted(reply), reply)
        self.assertTrue(refused_as_unknown(self.app.read(slug, self.reader)))
        self.assertTrue(refused_as_unknown(self.app.read(slug)))
        self.assertNotIn(slug, slugs(self.app.browse(author=self.author.username)))
        self.assertEqual(self.app.read(slug, self.author).status, 200)

    # AC4, AC5, AC9
    def test_the_author_sees_the_own_scheduled_article_and_counts_it(self):
        """AC4, AC9: single article and the author filter; the count follows the viewer."""
        self.app.publish(self.author)
        slug = self.app.scheduled(self.author, far_future())["slug"]
        self.assertEqual(article(self.app.read(slug, self.author))["slug"], slug)
        own = self.app.browse(self.author, author=self.author.username)
        self.assertIn(slug, slugs(own))
        self.assertEqual(articles_count(own), 2)
        for viewer in (self.reader, None):
            other = self.app.browse(viewer, author=self.author.username)
            self.assertEqual((len(articles(other)), articles_count(other)), (1, 1))

    def test_nobody_can_favorite_or_comment_on_a_scheduled_article(self):
        """AC5: 404, the author included."""
        slug = self.app.scheduled(self.author, far_future())["slug"]
        for member in (self.author, self.reader):
            self.assertTrue(refused_as_unknown(self.app.favorite(member, slug)), member)
            self.assertTrue(refused_as_unknown(self.app.comment(member, slug, "Early")), member)
        self.assertEqual(article(self.app.read(slug, self.author))["favoritesCount"], 0)

    # AC6, AC7, AC8
    def test_a_scheduled_article_appears_for_everybody_at_its_publish_time(self):
        """AC6, AC8, answer 2: no further action; single, list, tag filter and tag list."""
        tag = self.app.unique("launch")
        when = moment(LATER, offset_hours=-3)
        slug = self.app.scheduled(self.author, when, tags=[tag])["slug"]
        self.assertTrue(refused_as_unknown(self.app.read(slug)))
        self.assertNotIn(tag, self.app.tags().body["tags"])
        self.app.wait_until(when)
        self.assertEqual(self.app.read(slug, self.reader).status, 200)
        self.assertEqual(self.app.read(slug).status, 200)
        self.assertEqual(slugs(self.app.browse(tag=tag)), [slug])
        self.assertIn(slug, slugs(self.app.browse(self.reader, author=self.author.username)))
        self.assertIn(tag, self.app.tags().body["tags"])

    def test_a_publish_time_in_the_past_publishes_at_once(self):
        """AC7."""
        slug = self.app.scheduled(self.author, moment(-2 * 24 * 3600, offset_hours=-5))["slug"]
        self.assertEqual(self.app.read(slug, self.reader).status, 200)
        self.assertEqual(self.app.read(slug).status, 200)
        self.assertIn(slug, slugs(self.app.browse(author=self.author.username)))

    def test_the_tag_list_shows_only_tags_of_articles_that_everybody_can_see(self):
        """AC8: a tag only on a scheduled article is hidden; a tag also on a visible article is shown."""
        hidden_tag, shared_tag = self.app.unique("secret"), self.app.unique("shared")
        self.app.scheduled(self.author, far_future(), tags=[hidden_tag, shared_tag])
        self.app.publish(self.reader, tags=[shared_tag])
        tags = self.app.tags().body["tags"]
        self.assertNotIn(hidden_tag, tags)
        self.assertIn(shared_tag, tags)

    def test_the_list_order_stays_newest_created_first(self):
        """Answer 6: a scheduled article keeps its place by creation, not by publish time."""
        when = moment(LATER)
        scheduled = self.app.scheduled(self.author, when)["slug"]
        published = self.app.publish(self.author)["slug"]
        self.app.wait_until(when)
        self.assertEqual(slugs(self.app.browse(self.reader, author=self.author.username)), [published, scheduled])

    # answers 4 and 5
    def test_a_publish_time_that_is_not_a_date_and_time_is_refused(self):
        """Answer 4: 422 with an error on publishAt, on create and on update."""
        slug = self.app.publish(self.author)["slug"]
        for text in ("next tuesday", "2099-13-45T10:00:00Z"):
            self.assertTrue(refused_as_invalid(self.app.schedule(self.author, text), "publishAt"), text)
            self.assertTrue(refused_as_invalid(self.app.reschedule(self.author, slug, text), "publishAt"), text)
        self.assertEqual(self.app.read(slug, self.reader).status, 200)

    def test_a_publish_time_without_an_offset_is_refused(self):
        """Answer 5: no Z and no offset gives 422 on publishAt, on create and on update."""
        slug = self.app.publish(self.author)["slug"]
        text = "2099-01-01T10:00:00"
        self.assertTrue(refused_as_invalid(self.app.schedule(self.author, text), "publishAt"))
        self.assertTrue(refused_as_invalid(self.app.reschedule(self.author, slug, text), "publishAt"))
        self.assertEqual(self.app.read(slug, self.reader).status, 200)


if __name__ == "__main__":
    unittest.main()
