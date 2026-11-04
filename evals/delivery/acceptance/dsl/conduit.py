"""DSL for Conduit: domain actions in the words of the product. Test cases call these, never HTTP.

Each action returns the driver Response, so a case can check the outcome in domain terms with the
helpers at the end. A test case that needs a new action for a story adds it to the story's own
`dsl_<story>.py` in its hidden folder, built on the same Conduit class.
"""
import itertools
import os
import time
import uuid

from drivers.http import Http

_counter = itertools.count(1)


class Member:
    def __init__(self, username, email, password, token):
        self.username = username
        self.email = email
        self.password = password
        self.token = token

    def __repr__(self):
        return f"Member({self.username})"


class Conduit:
    def __init__(self, base_url=None):
        self.http = Http(base_url or os.environ["SUT_URL"], auth_scheme="Token")

    # members
    def unique(self, prefix):
        return f"{prefix}{next(_counter)}{uuid.uuid4().hex[:6]}"

    def register(self, name=None, password="secret-pass-1"):
        username = name or self.unique("member")
        email = f"{username}@example.com"
        reply = self.http.post("/api/users", {"user": {"username": username, "email": email, "password": password}})
        assert reply.status in (200, 201), f"register {username}: {reply}"
        return Member(username, email, password, reply.body["user"]["token"])

    def try_register(self, username, email, password="secret-pass-1"):
        return self.http.post("/api/users", {"user": {"username": username, "email": email, "password": password}})

    def sign_in(self, email, password):
        return self.http.post("/api/users/login", {"user": {"email": email, "password": password}})

    def current_user(self, token):
        return self.http.get("/api/user", token=token)

    def update_user(self, member, **fields):
        return self.http.put("/api/user", {"user": fields}, token=member.token)

    def profile(self, username, viewer=None):
        return self.http.get(f"/api/profiles/{username}", token=_token(viewer))

    def follow(self, viewer, username):
        return self.http.post(f"/api/profiles/{username}/follow", token=_token(viewer))

    def unfollow(self, viewer, username):
        return self.http.delete(f"/api/profiles/{username}/follow", token=_token(viewer))

    # articles
    def write(self, author, title=None, description="About it", body="Body text", tags=None, **extra):
        article = {"title": title or self.unique("Title "), "description": description, "body": body}
        if tags is not None:
            article["tagList"] = tags
        article.update(extra)
        return self.http.post("/api/articles", {"article": article}, token=_token(author))

    def publish(self, author, **kw):
        reply = self.write(author, **kw)
        assert reply.status in (200, 201), f"publish: {reply}"
        return reply.body["article"]

    def read(self, slug, viewer=None):
        return self.http.get(f"/api/articles/{slug}", token=_token(viewer))

    def edit(self, author, slug, **fields):
        return self.http.put(f"/api/articles/{slug}", {"article": fields}, token=_token(author))

    def remove(self, author, slug):
        return self.http.delete(f"/api/articles/{slug}", token=_token(author))

    def browse(self, viewer=None, **query):
        return self.http.get("/api/articles" + _query(query), token=_token(viewer))

    def feed(self, viewer, **query):
        return self.http.get("/api/articles/feed" + _query(query), token=_token(viewer))

    def tags(self):
        return self.http.get("/api/tags")

    # favorites
    def favorite(self, viewer, slug):
        return self.http.post(f"/api/articles/{slug}/favorite", token=_token(viewer))

    def unfavorite(self, viewer, slug):
        return self.http.delete(f"/api/articles/{slug}/favorite", token=_token(viewer))

    # comments
    def comment(self, viewer, slug, body="Nice article"):
        return self.http.post(f"/api/articles/{slug}/comments", {"comment": {"body": body}}, token=_token(viewer))

    def comments(self, slug, viewer=None):
        return self.http.get(f"/api/articles/{slug}/comments", token=_token(viewer))

    def remove_comment(self, viewer, slug, comment_id):
        return self.http.delete(f"/api/articles/{slug}/comments/{comment_id}", token=_token(viewer))

    @staticmethod
    def wait(seconds):
        time.sleep(seconds)


# outcome helpers, in domain words
def accepted(reply):
    return 200 <= reply.status < 300


def refused_as_invalid(reply, field=None):
    if reply.status != 422:
        return False
    if field is None:
        return True
    errors = (reply.body or {}).get("errors") if isinstance(reply.body, dict) else None
    return isinstance(errors, dict) and field in errors


def refused_as_unknown(reply):
    return reply.status == 404


def refused_as_forbidden(reply):
    return reply.status == 403


def refused_as_signed_out(reply):
    return reply.status == 401


def refused_as_conflict(reply, field=None):
    if reply.status != 409:
        return False
    return field is None or field in error_fields(reply)


def error_fields(reply):
    """The field names in the error body {"errors": {field: [...]}}, or an empty set."""
    errors = (reply.body or {}).get("errors") if isinstance(reply.body, dict) else None
    return set(errors) if isinstance(errors, dict) else set()


def slugs(reply, key="articles"):
    body = reply.body if isinstance(reply.body, dict) else {}
    return [item["slug"] for item in body.get(key, [])]


def articles(reply):
    """The article list of a list or feed reply, or an empty list."""
    return list(reply.body.get("articles", [])) if isinstance(reply.body, dict) else []


def article(reply):
    """The article of a single-article reply, or None."""
    return reply.body.get("article") if isinstance(reply.body, dict) else None


def articles_count(reply):
    return reply.body.get("articlesCount") if isinstance(reply.body, dict) else None


def _token(member):
    if member is None:
        return None
    return member if isinstance(member, str) else member.token


def _query(query):
    if not query:
        return ""
    from urllib.parse import urlencode
    return "?" + urlencode({k: v for k, v in query.items() if v is not None})
