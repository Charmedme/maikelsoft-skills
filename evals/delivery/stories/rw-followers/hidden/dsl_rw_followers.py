"""DSL actions for rw-followers: the follower list of a profile."""
from dsl.conduit import Conduit, _query, _token


class FollowersConduit(Conduit):
    def followers(self, username, viewer=None, **query):
        return self.http.get(f"/api/profiles/{username}/followers" + _query(query), token=_token(viewer))


def follower_names(reply):
    body = reply.body if isinstance(reply.body, dict) else {}
    return [profile["username"] for profile in body.get("profiles", [])]


def profiles_count(reply):
    return reply.body.get("profilesCount") if isinstance(reply.body, dict) else None
