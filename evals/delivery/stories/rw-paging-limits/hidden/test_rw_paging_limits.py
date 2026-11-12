"""Hidden acceptance cases for rw-paging-limits: limits on the page size of the article list and the feed."""
import unittest

from dsl.conduit import Conduit, articles, articles_count, error_fields, refused_as_invalid, refused_as_signed_out

ARTICLES = 101


class PagingLimitCases(unittest.TestCase):
    """One author with 101 articles and one reader who follows only that author.

    The cases only read this data, so they stay independent of each other.
    """

    @classmethod
    def setUpClass(cls):
        cls.app = Conduit()
        cls.author = cls.app.register()
        cls.reader = cls.app.register()
        for _ in range(ARTICLES):
            cls.app.publish(cls.author)
        reply = cls.app.follow(cls.reader, cls.author.username)
        assert reply.status == 200, reply

    def article_list(self, **query):
        return self.app.browse(author=self.author.username, **query)

    def the_feed(self, **query):
        return self.app.feed(self.reader, **query)

    def assert_refused_on(self, reply, field):
        """422 with an error on the wrong field, and no error message on a field that is right."""
        self.assertTrue(refused_as_invalid(reply, field), reply)
        with_messages = {name for name in error_fields(reply) if reply.body["errors"][name]}
        self.assertEqual(with_messages, {field}, reply)

    def test_the_article_list_has_20_articles_when_no_limit_is_sent(self):
        """AC1."""
        reply = self.article_list()
        self.assertEqual((len(articles(reply)), articles_count(reply)), (20, ARTICLES))

    def test_the_feed_has_20_articles_when_no_limit_is_sent(self):
        """AC1, AC5."""
        reply = self.the_feed()
        self.assertEqual((len(articles(reply)), articles_count(reply)), (20, ARTICLES))

    def test_a_limit_of_100_gives_100_articles_in_the_list_and_the_feed(self):
        """AC2, answer 1: limit=100 is allowed."""
        for reply in (self.article_list(limit=100), self.the_feed(limit=100)):
            self.assertEqual(reply.status, 200, reply)
            self.assertEqual((len(articles(reply)), articles_count(reply)), (100, ARTICLES))

    def test_a_limit_above_100_is_refused_on_the_limit_field_in_the_article_list(self):
        """AC3, answer 1, answer 3: 101 and the 1000000 of the context are refused, not reduced."""
        for limit in (101, 1000000):
            self.assert_refused_on(self.article_list(limit=limit), "limit")

    def test_a_limit_above_100_is_refused_on_the_limit_field_in_the_feed(self):
        """AC3, AC5."""
        for limit in (101, 1000000):
            self.assert_refused_on(self.the_feed(limit=limit), "limit")

    def test_a_limit_below_1_is_refused_in_the_list_and_the_feed(self):
        """AC3, AC5: 0 and negative values; 1 is allowed."""
        for limit in (0, -1):
            self.assert_refused_on(self.article_list(limit=limit), "limit")
            self.assert_refused_on(self.the_feed(limit=limit), "limit")
        for reply in (self.article_list(limit=1), self.the_feed(limit=1)):
            self.assertEqual((reply.status, len(articles(reply))), (200, 1), reply)

    def test_a_negative_offset_is_refused_on_the_offset_field_in_the_list_and_the_feed(self):
        """AC3, AC5."""
        self.assert_refused_on(self.article_list(offset=-1), "offset")
        self.assert_refused_on(self.the_feed(offset=-1), "offset")

    def test_an_offset_of_0_is_allowed(self):
        """Answer 2."""
        for reply in (self.article_list(offset=0, limit=5), self.the_feed(offset=0, limit=5)):
            self.assertEqual((reply.status, len(articles(reply))), (200, 5), reply)

    def test_a_value_that_is_not_a_whole_number_is_refused_on_its_field(self):
        """AC4, AC5."""
        for query, field in (({"limit": "abc"}, "limit"), ({"limit": "2.5"}, "limit"), ({"offset": "1.5"}, "offset"),
                             ({"offset": "ten"}, "offset")):
            self.assert_refused_on(self.article_list(**query), field)
            self.assert_refused_on(self.the_feed(**query), field)

    def test_an_offset_past_the_end_gives_an_empty_page_with_the_full_count(self):
        """AC6, answer 2: in the list and in the feed."""
        for reply in (self.article_list(offset=ARTICLES), self.the_feed(offset=500, limit=100)):
            self.assertEqual(reply.status, 200, reply)
            self.assertEqual((articles(reply), articles_count(reply)), ([], ARTICLES))

    def test_the_last_partial_page_keeps_the_full_count(self):
        """AC6: articlesCount is the number of all matching articles, not the page size."""
        reply = self.article_list(limit=100, offset=100)
        self.assertEqual((len(articles(reply)), articles_count(reply)), (1, ARTICLES))

    def test_the_feed_without_a_token_gives_401_before_the_values_are_checked(self):
        """Answer 5."""
        for query in ({"limit": 101}, {"offset": -1}, {"limit": "abc"}):
            self.assertTrue(refused_as_signed_out(self.app.feed(None, **query)), query)


if __name__ == "__main__":
    unittest.main()
