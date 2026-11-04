"""Hidden acceptance cases for rw-favorites: readers mark articles as a favorite."""
import unittest

from dsl.conduit import Conduit, accepted, article, articles, articles_count, slugs


def find(reply, slug):
    for item in articles(reply):
        if item["slug"] == slug:
            return item
    raise AssertionError(f"{slug} not in {reply}")


class FavoriteCases(unittest.TestCase):
    def setUp(self):
        self.app = Conduit()
        self.author = self.app.register()
        self.reader = self.app.register()
        self.other = self.app.register()

    def test_favorited_is_true_only_for_the_reader_who_marked_the_single_article(self):
        """AC4: single article, per reader and signed out."""
        slug = self.app.publish(self.author)["slug"]
        reply = self.app.favorite(self.reader, slug)
        self.assertEqual(reply.status, 200, reply)
        self.assertIs(article(self.app.read(slug, self.reader))["favorited"], True)
        self.assertIs(article(self.app.read(slug, self.other))["favorited"], False)
        self.assertIs(article(self.app.read(slug, self.author))["favorited"], False)
        self.assertIs(article(self.app.read(slug))["favorited"], False)

    def test_favorited_is_true_only_for_the_reader_who_marked_it_in_the_article_list(self):
        """AC4: the article list."""
        marked = self.app.publish(self.author)["slug"]
        unmarked = self.app.publish(self.author)["slug"]
        self.assertTrue(accepted(self.app.favorite(self.reader, marked)))
        mine = self.app.browse(self.reader, author=self.author.username)
        self.assertIs(find(mine, marked)["favorited"], True)
        self.assertIs(find(mine, unmarked)["favorited"], False)
        theirs = self.app.browse(self.other, author=self.author.username)
        self.assertIs(find(theirs, marked)["favorited"], False)
        anonymous = self.app.browse(author=self.author.username)
        self.assertIs(find(anonymous, marked)["favorited"], False)

    def test_favorited_is_true_only_for_the_reader_who_marked_it_in_the_feed(self):
        """AC4: the feed."""
        marked = self.app.publish(self.author)["slug"]
        unmarked = self.app.publish(self.author)["slug"]
        for member in (self.reader, self.other):
            self.assertTrue(accepted(self.app.follow(member, self.author.username)))
        self.assertTrue(accepted(self.app.favorite(self.reader, marked)))
        mine = self.app.feed(self.reader)
        self.assertIs(find(mine, marked)["favorited"], True)
        self.assertIs(find(mine, unmarked)["favorited"], False)
        self.assertIs(find(self.app.feed(self.other), marked)["favorited"], False)

    def test_the_count_is_the_number_of_readers_in_the_single_article_the_list_and_the_feed(self):
        """AC1, AC5: each reader adds one; every read path shows the same count."""
        slug = self.app.publish(self.author)["slug"]
        first = self.app.favorite(self.reader, slug)
        self.assertEqual(first.status, 200, first)
        self.assertEqual((article(first)["favorited"], article(first)["favoritesCount"]), (True, 1))
        second = self.app.favorite(self.other, slug)
        self.assertEqual(article(second)["favoritesCount"], 2)
        self.assertTrue(accepted(self.app.follow(self.author, self.other.username)))
        self.assertTrue(accepted(self.app.follow(self.reader, self.author.username)))
        self.assertEqual(article(self.app.read(slug))["favoritesCount"], 2)
        self.assertEqual(find(self.app.browse(author=self.author.username), slug)["favoritesCount"], 2)
        self.assertEqual(find(self.app.feed(self.reader), slug)["favoritesCount"], 2)

    def test_removing_a_mark_lowers_the_count_and_keeps_the_marks_of_other_readers(self):
        """AC2, AC4, AC5."""
        slug = self.app.publish(self.author)["slug"]
        self.app.favorite(self.reader, slug)
        self.app.favorite(self.other, slug)
        reply = self.app.unfavorite(self.reader, slug)
        self.assertEqual(reply.status, 200, reply)
        self.assertEqual((article(reply)["favorited"], article(reply)["favoritesCount"]), (False, 1))
        self.assertIs(article(self.app.read(slug, self.other))["favorited"], True)
        self.assertEqual(find(self.app.browse(self.other, author=self.author.username), slug)["favoritesCount"], 1)

    def test_marking_an_article_twice_is_not_an_error_and_the_count_stays(self):
        """Answer 1."""
        slug = self.app.publish(self.author)["slug"]
        self.app.favorite(self.reader, slug)
        again = self.app.favorite(self.reader, slug)
        self.assertEqual(again.status, 200, again)
        self.assertEqual((article(again)["favorited"], article(again)["favoritesCount"]), (True, 1))
        self.assertEqual(article(self.app.read(slug))["favoritesCount"], 1)

    def test_removing_a_mark_that_does_not_exist_is_not_an_error(self):
        """Answer 2."""
        slug = self.app.publish(self.author)["slug"]
        self.app.favorite(self.other, slug)
        reply = self.app.unfavorite(self.reader, slug)
        self.assertEqual(reply.status, 200, reply)
        self.assertEqual((article(reply)["slug"], article(reply)["favorited"], article(reply)["favoritesCount"]),
                         (slug, False, 1))

    def test_an_author_can_mark_the_own_article(self):
        """Answer 3."""
        slug = self.app.publish(self.author)["slug"]
        reply = self.app.favorite(self.author, slug)
        self.assertEqual(reply.status, 200, reply)
        self.assertEqual((article(reply)["favorited"], article(reply)["favoritesCount"]), (True, 1))

    def test_the_favorited_filter_lists_only_the_articles_that_this_user_marked(self):
        """AC6: marks of other readers do not count."""
        mine = self.app.publish(self.author)["slug"]
        theirs = self.app.publish(self.author)["slug"]
        self.app.favorite(self.reader, mine)
        self.app.favorite(self.other, theirs)
        reply = self.app.browse(favorited=self.reader.username)
        self.assertEqual(slugs(reply), [mine])
        self.assertEqual(articles_count(reply), 1)

    def test_the_favorited_filter_combines_with_tag_author_and_paging(self):
        """Answer 4: articlesCount is the number of matching articles, not the page size."""
        tag = self.app.unique("tag")
        second_author = self.app.register()
        tagged = [self.app.publish(self.author, tags=[tag])["slug"] for _ in range(3)]
        other_tag = self.app.publish(self.author, tags=[self.app.unique("tag")])["slug"]
        by_second = self.app.publish(second_author, tags=[tag])["slug"]
        not_marked = self.app.publish(self.author, tags=[tag])["slug"]
        for slug in [*tagged, other_tag, by_second]:
            self.app.favorite(self.reader, slug)
        with_tag = self.app.browse(favorited=self.reader.username, tag=tag)
        self.assertEqual(sorted(slugs(with_tag)), sorted([*tagged, by_second]))
        self.assertNotIn(not_marked, slugs(with_tag))
        self.assertEqual(articles_count(with_tag), 4)
        with_author = self.app.browse(favorited=self.reader.username, tag=tag, author=self.author.username)
        self.assertEqual(sorted(slugs(with_author)), sorted(tagged))
        self.assertEqual(articles_count(with_author), 3)
        page = self.app.browse(favorited=self.reader.username, tag=tag, author=self.author.username, limit=2, offset=1)
        self.assertEqual(slugs(page), list(reversed(tagged))[1:3])
        self.assertEqual(articles_count(page), 3)

    def test_an_unknown_user_name_in_the_favorited_filter_gives_an_empty_list(self):
        """Answer 5."""
        self.app.publish(self.author)
        reply = self.app.browse(favorited=self.app.unique("nobody"))
        self.assertEqual(reply.status, 200, reply)
        self.assertEqual((articles(reply), articles_count(reply)), ([], 0))


if __name__ == "__main__":
    unittest.main()
