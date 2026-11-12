"""Hidden acceptance cases for dash-audit-range: search the audit log by day, time zone, user and action.

Most cases store audit entries at fixed moments in the past (through the DSL), each for a user of its
own, so the day boundaries are exact and no case depends on the current hour. One case uses an entry
that the product screen makes now, and picks a zone in which the local day differs from the UTC day.
"""
import datetime
import unittest

from dsl.dashboard import ADMIN, READER, Dashboard, refused_as_forbidden, refused_as_invalid, refused_as_signed_out
from dsl_dash_audit_range import (KIRITIMATI, MINUS_12, UTC, day_text, items, object_ids, parse_moment, total,
                                  utc)

ACTION = "UPDATED_PRODUCT"


class AuditSearchCases(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.app = Dashboard()
        cls.auditor = cls.app.new_member()

    def author_with(self, *entries, action=ACTION):
        """A new user with audit entries: (label, moment) pairs; the label is the object id."""
        name = self.app.new_member(role="User")
        self.app.record_audit_entries([(name, action, moment, label) for label, moment in entries])
        return name

    def search(self, **query):
        reply = self.app.search_audit_log(self.auditor, **query)
        self.assertEqual(reply.status, 200, reply)
        return reply

    def found(self, **query):
        return sorted(object_ids(self.search(**query)))

    def assert_refused_on(self, field, **query):
        reply = self.app.search_audit_log(self.auditor, **query)
        self.assertTrue(refused_as_invalid(reply, field), f"{query}: {reply}")

    # AC4
    def test_each_item_has_its_fields_and_the_newest_entry_comes_first(self):
        """AC4: id, userName, action, objectId, createdDateTime (UTC) and log; newest first."""
        name = self.author_with(("a", utc(2025, 5, 6, 8)), ("c", utc(2025, 5, 6, 20, 15, 30)),
                                ("b", utc(2025, 5, 6, 12)))
        reply = self.search(userName=name)
        self.assertEqual((total(reply), reply.body.get("page"), reply.body.get("pageSize")), (3, 1, 20))
        self.assertEqual(object_ids(reply), ["c", "b", "a"])
        first = items(reply)[0]
        for field in ("id", "userName", "action", "objectId", "createdDateTime", "log"):
            self.assertIn(field, first)
        self.assertEqual((first["userName"], first["action"]), (name, ACTION))
        moment = parse_moment(first["createdDateTime"])
        self.assertIsNotNone(moment, first["createdDateTime"])
        self.assertEqual(moment, (utc(2025, 5, 6, 20, 15, 30), 0))

    # AC2: days in UTC
    def test_without_a_time_zone_both_days_are_included_in_utc(self):
        """AC2: from and to are whole UTC days, both included."""
        name = self.author_with(("before", utc(2025, 4, 9, 23, 59, 59, 999)), ("first", utc(2025, 4, 10)),
                                ("middle", utc(2025, 4, 11, 12)), ("last", utc(2025, 4, 12, 23, 59, 59, 999)),
                                ("after", utc(2025, 4, 13)))
        self.assertEqual(self.found(userName=name, **{"from": "2025-04-10"}, to="2025-04-12"),
                         ["first", "last", "middle"])

    def test_a_single_day_runs_from_midnight_to_midnight(self):
        """AC2: from = to is one whole day."""
        name = self.author_with(("before", utc(2025, 4, 9, 23, 59, 59, 999)), ("start", utc(2025, 4, 10)),
                                ("end", utc(2025, 4, 10, 23, 59, 59, 999)), ("after", utc(2025, 4, 11)))
        self.assertEqual(self.found(userName=name, **{"from": "2025-04-10"}, to="2025-04-10"), ["end", "start"])

    # AC2: days in far time zones
    def test_a_day_in_kiritimati_starts_ten_hours_before_the_utc_day(self):
        """AC2: UTC+14; 2025-04-10 there is 2025-04-09T10:00Z up to 2025-04-10T10:00Z."""
        name = self.author_with(("before", utc(2025, 4, 9, 9, 59, 59)), ("start", utc(2025, 4, 9, 10)),
                                ("end", utc(2025, 4, 10, 9, 59, 59)), ("after", utc(2025, 4, 10, 10)))
        found = self.found(userName=name, **{"from": "2025-04-10"}, to="2025-04-10", timeZone=KIRITIMATI[0])
        self.assertEqual(found, ["end", "start"])

    def test_a_day_at_utc_minus_12_starts_twelve_hours_after_the_utc_day(self):
        """AC2: Etc/GMT+12 is UTC-12; 2025-04-10 there is 2025-04-10T12:00Z up to 2025-04-11T12:00Z."""
        name = self.author_with(("before", utc(2025, 4, 10, 11, 59, 59)), ("start", utc(2025, 4, 10, 12)),
                                ("end", utc(2025, 4, 11, 11, 59, 59)), ("after", utc(2025, 4, 11, 12)))
        found = self.found(userName=name, **{"from": "2025-04-10"}, to="2025-04-10", timeZone=MINUS_12[0])
        self.assertEqual(found, ["end", "start"])

    def test_a_day_with_a_clock_change_follows_the_rules_of_the_zone(self):
        """AC1, AC2: Europe/Amsterdam on 2025-03-30 has 23 hours (UTC+1 until 01:00Z, then UTC+2)."""
        name = self.author_with(("before", utc(2025, 3, 29, 22, 59, 59)), ("start", utc(2025, 3, 29, 23)),
                                ("end", utc(2025, 3, 30, 21, 59, 59)), ("after", utc(2025, 3, 30, 22, 30)))
        found = self.found(userName=name, **{"from": "2025-03-30"}, to="2025-03-30", timeZone="Europe/Amsterdam")
        self.assertEqual(found, ["end", "start"])

    def test_an_entry_made_now_is_found_on_its_local_day_in_a_far_zone(self):
        """AC2 with a real entry from the product screen: the local day, not the UTC day, decides."""
        editor = self.app.new_member()
        before = datetime.datetime.now(UTC)
        self.app.added_product(editor)
        after = datetime.datetime.now(UTC)
        for zone_name, zone in (KIRITIMATI, MINUS_12):
            local_day = day_text(before.astimezone(zone))
            if local_day == day_text(after.astimezone(zone)) and local_day != day_text(before):
                break
        else:
            self.skipTest("the entry was made on a day boundary of both zones; run again")
        query = {"userName": editor, "from": local_day, "to": local_day}
        self.assertEqual(total(self.search(**query, timeZone=zone_name)), 1)
        utc_day = day_text(before)
        if utc_day == day_text(after):
            self.assertEqual(total(self.search(userName=editor, timeZone=zone_name,
                                               **{"from": utc_day, "to": utc_day})), 0)
            self.assertEqual(total(self.search(userName=editor, **{"from": utc_day, "to": utc_day})), 1)

    # answer 1: open ends
    def test_from_or_to_alone_limits_only_that_side(self):
        """Answer 1: a missing from or to means no limit on that side."""
        name = self.author_with(("old", utc(2020, 1, 1)), ("mid", utc(2025, 4, 10, 12)), ("new", utc(2025, 12, 31)))
        self.assertEqual(self.found(userName=name, **{"from": "2025-04-10"}), ["mid", "new"])
        self.assertEqual(self.found(userName=name, to="2025-04-10"), ["mid", "old"])
        self.assertEqual(self.found(userName=name), ["mid", "new", "old"])

    # AC3
    def test_the_user_name_matches_exactly_ignoring_case(self):
        """AC3, answer 5: no partial match on a longer name."""
        name = self.author_with(("mine", utc(2025, 4, 10)))
        self.app.new_member(role="User", name=name + "x")
        self.app.record_audit_entries([(name + "x", ACTION, utc(2025, 4, 10), "longer")])
        self.assertEqual(self.found(userName=name.upper()), ["mine"])
        self.assertEqual(items(self.search(userName=name.upper()))[0]["userName"], name)

    def test_the_action_matches_exactly_ignoring_case_and_combines_with_the_other_filters(self):
        """AC3: each filter is optional; action alone and with a user name."""
        action = self.app.unique("RANGE_CHECK_").upper()
        name = self.author_with(("exact", utc(2025, 4, 10)), action=action)
        self.app.record_audit_entries([(name, action + "_MORE", utc(2025, 4, 10), "longer"),
                                       (name, ACTION, utc(2025, 4, 10), "other")])
        self.assertEqual(self.found(action=action.lower()), ["exact"])
        self.assertEqual(self.found(action=action.lower(), userName=name, **{"from": "2025-04-10"},
                                    to="2025-04-10"), ["exact"])

    # AC5, answers 6 and 7
    def test_pages_hold_20_entries_by_default_and_count_all_entries(self):
        """AC5, answer 6, answer 7: page 1 and 2, a page past the end, totalItems of all matches."""
        start = utc(2025, 6, 1)
        entries = [(f"e{i:02d}", start + datetime.timedelta(minutes=i)) for i in range(25)]
        name = self.author_with(*entries)
        newest_first = [label for label, _ in reversed(entries)]
        first = self.search(userName=name)
        self.assertEqual((object_ids(first), total(first)), (newest_first[:20], 25))
        second = self.search(userName=name, page=2)
        self.assertEqual((object_ids(second), total(second), second.body.get("page")), (newest_first[20:], 25, 2))
        past = self.search(userName=name, page=3)
        self.assertEqual((object_ids(past), total(past)), ([], 25))
        small = self.search(userName=name, page=2, pageSize=10)
        self.assertEqual((object_ids(small), small.body.get("pageSize")), (newest_first[10:20], 10))

    def test_a_page_size_of_100_is_allowed(self):
        """AC5: at most 100."""
        reply = self.search(pageSize=100)
        self.assertEqual(reply.body.get("pageSize"), 100)
        self.assertLessEqual(len(items(reply)), 100)

    # AC6 and answers 2 to 4
    def test_page_and_page_size_out_of_range_are_refused_on_their_field(self):
        """AC6, answer 2: page below 1, pageSize below 1 or above 100."""
        self.assert_refused_on("page", page=0)
        self.assert_refused_on("page", page=-1)
        self.assert_refused_on("pageSize", pageSize=0)
        self.assert_refused_on("pageSize", pageSize=101)

    def test_from_after_to_is_refused_on_to(self):
        """AC6, answer 3."""
        self.assert_refused_on("to", **{"from": "2025-04-11"}, to="2025-04-10")
        self.search(**{"from": "2025-04-10"}, to="2025-04-10")

    def test_a_date_that_is_not_yyyy_mm_dd_is_refused_on_its_field(self):
        """AC6, answer 2: wrong format and a day that does not exist."""
        self.assert_refused_on("from", **{"from": "2025-4-10"})
        self.assert_refused_on("from", **{"from": "2025-02-30"})
        self.assert_refused_on("to", to="10-04-2025")
        self.assert_refused_on("to", to="2025-04-10T00:00:00Z")

    def test_an_unknown_time_zone_is_refused_on_time_zone(self):
        """AC6, answer 2."""
        self.assert_refused_on("timeZone", timeZone="Mars/Olympus_Mons")

    def test_a_range_of_more_than_366_days_is_refused_on_to(self):
        """Answer 4: 366 days counting both days is allowed (a leap year), 367 is refused."""
        self.search(**{"from": "2024-01-01"}, to="2024-12-31")
        self.search(**{"from": "2025-01-01"}, to="2026-01-01")
        self.assert_refused_on("to", **{"from": "2024-01-01"}, to="2025-01-01")

    # AC7
    def test_the_search_needs_the_permission_to_read_audit_logs(self):
        """AC7: a user without it gets 403; without sign-in 401."""
        self.assertEqual(self.app.search_audit_log(ADMIN).status, 200)
        self.assertTrue(refused_as_forbidden(self.app.search_audit_log(READER)))
        self.assertTrue(refused_as_signed_out(self.app.search_audit_log(None)))


if __name__ == "__main__":
    unittest.main()
