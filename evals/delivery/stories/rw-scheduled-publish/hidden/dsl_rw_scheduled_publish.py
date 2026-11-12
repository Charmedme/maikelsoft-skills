"""DSL actions for rw-scheduled-publish: articles with a publish time."""
import datetime
import re
import time

from dsl.conduit import Conduit

UTC = datetime.timezone.utc
_UTC_TEXT = re.compile(r"^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2})(?:\.(\d{1,9}))?Z$")


def moment(seconds_from_now=0.0, offset_hours=0):
    """A moment as an aware datetime in the given UTC offset, rounded up to a whole second."""
    zone = datetime.timezone(datetime.timedelta(hours=offset_hours))
    value = datetime.datetime.now(UTC) + datetime.timedelta(seconds=seconds_from_now)
    if value.microsecond:
        value = value.replace(microsecond=0) + datetime.timedelta(seconds=1)
    return value.astimezone(zone)


def iso(value):
    """ISO 8601 in whole seconds with an explicit offset ('Z' for UTC)."""
    text = value.isoformat(timespec="seconds")
    return text[:-6] + "Z" if text.endswith("+00:00") else text


def parse_utc(text):
    """Parse a UTC time that ends in 'Z'; None when the text has another form."""
    match = _UTC_TEXT.match(text or "") if isinstance(text, str) else None
    if not match:
        return None
    base = datetime.datetime.strptime(match.group(1), "%Y-%m-%dT%H:%M:%S").replace(tzinfo=UTC)
    fraction = (match.group(2) or "0")[:6].ljust(6, "0")
    return base + datetime.timedelta(microseconds=int(fraction))


class ScheduleConduit(Conduit):
    def schedule(self, author, at, **kw):
        """Create an article with publishAt; `at` is a datetime or the raw text to send."""
        return self.write(author, publishAt=at if isinstance(at, str) else iso(at), **kw)

    def scheduled(self, author, at, **kw):
        reply = self.schedule(author, at, **kw)
        assert reply.status in (200, 201), f"schedule: {reply}"
        return reply.body["article"]

    def reschedule(self, author, slug, at):
        return self.edit(author, slug, publishAt=at if isinstance(at, str) else iso(at))

    @staticmethod
    def wait_until(when, margin=0.4):
        delay = (when - datetime.datetime.now(UTC)).total_seconds() + margin
        if delay > 0:
            time.sleep(delay)
