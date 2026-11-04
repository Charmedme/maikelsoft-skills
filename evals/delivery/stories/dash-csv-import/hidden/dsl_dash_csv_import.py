"""DSL helpers for dash-csv-import: CSV files and the import reply."""
from dsl.dashboard import Dashboard

HEADER = "Code,Name,Description"


def quote(field):
    """Standard CSV quoting: quote a field with a comma, a quote or a line break; double the quotes."""
    if any(ch in field for ch in ',"\r\n'):
        return '"' + field.replace('"', '""') + '"'
    return field


def csv_file(rows, header=HEADER, newline="\n", bom=False):
    """A CSV text; each row is a tuple of fields, or a raw line as a string."""
    lines = [header] + [row if isinstance(row, str) else ",".join(quote(f) for f in row) for row in rows]
    text = newline.join(lines) + newline
    return ("﻿" + text) if bom else text


def error_lines(reply):
    """The line numbers in the error list of a refused import."""
    errors = reply.body.get("errors") if isinstance(reply.body, dict) else None
    if not isinstance(errors, list):
        return None
    return sorted(item.get("line") for item in errors if isinstance(item, dict))


def counts(reply):
    body = reply.body if isinstance(reply.body, dict) else {}
    return {key: body.get(key) for key in ("created", "updated", "unchanged")}


class ImportDashboard(Dashboard):
    def with_code(self, who, code):
        """The products whose code equals the given code after the trim, ignoring case."""
        key = code.strip().lower()
        reply = self.products(who)
        assert reply.status == 200, f"products: {reply}"
        return [p for p in reply.body if (p.get("code") or "").strip().lower() == key]
