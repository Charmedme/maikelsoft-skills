"""DSL helpers for dash-audit-range: moments, days in fixed-offset zones, and the search reply."""
import datetime
import re

UTC = datetime.timezone.utc
# IANA zones with a fixed offset all year: the far east and the far west of the clock.
KIRITIMATI = ("Pacific/Kiritimati", datetime.timezone(datetime.timedelta(hours=14)))
MINUS_12 = ("Etc/GMT+12", datetime.timezone(datetime.timedelta(hours=-12)))

_MOMENT = re.compile(r"^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2})(?:\.(\d{1,9}))?(Z|[+-]\d{2}:\d{2})$")


def utc(year, month, day, hour=0, minute=0, second=0, millisecond=0):
    return datetime.datetime(year, month, day, hour, minute, second, millisecond * 1000, tzinfo=UTC)


def day_text(moment_or_date):
    return moment_or_date.strftime("%Y-%m-%d")


def parse_moment(text):
    """Parse an ISO 8601 moment with Z or an offset; return (aware datetime, offset in minutes) or None."""
    match = _MOMENT.match(text or "") if isinstance(text, str) else None
    if not match:
        return None
    base = datetime.datetime.strptime(match.group(1), "%Y-%m-%dT%H:%M:%S")
    fraction = int((match.group(2) or "0")[:6].ljust(6, "0"))
    zone = match.group(3)
    minutes = 0 if zone == "Z" else (1 if zone[0] == "+" else -1) * (int(zone[1:3]) * 60 + int(zone[4:6]))
    tz = datetime.timezone(datetime.timedelta(minutes=minutes))
    return base.replace(microsecond=fraction, tzinfo=tz), minutes


def items(reply):
    return list(reply.body.get("items") or []) if isinstance(reply.body, dict) else []


def total(reply):
    return reply.body.get("totalItems") if isinstance(reply.body, dict) else None


def object_ids(reply):
    return [item.get("objectId") for item in items(reply)]
