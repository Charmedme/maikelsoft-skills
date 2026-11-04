"""DSL actions for rw-account-delete: a member deletes the own account."""
from dsl.conduit import Conduit, _token

_ABSENT = object()


class AccountConduit(Conduit):
    def delete_account(self, member, password=_ABSENT):
        """DELETE /api/user. Without a password argument the member's own password is sent;
        password=None sends a body without the password field."""
        if password is _ABSENT:
            password = member.password
        body = {"user": {}} if password is None else {"user": {"password": password}}
        return self.http.delete("/api/user", body, token=_token(member))

    def comment_ids(self, slug, viewer=None):
        reply = self.comments(slug, viewer)
        assert reply.status == 200, f"comments of {slug}: {reply}"
        return [c["id"] for c in reply.body["comments"]]
