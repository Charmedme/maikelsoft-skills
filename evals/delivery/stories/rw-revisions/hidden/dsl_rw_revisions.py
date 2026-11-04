"""DSL actions for rw-revisions: versioned edits and the revision list of an article."""
from dsl.conduit import Conduit, _token


class RevisionConduit(Conduit):
    def revise(self, author, slug, version, **fields):
        """Update an article from the given version (the version the author started from)."""
        return self.http.put(f"/api/articles/{slug}", {"article": {**fields, "version": version}}, token=_token(author))

    def revised(self, author, slug, version, **fields):
        reply = self.revise(author, slug, version, **fields)
        assert reply.status == 200, f"revise {slug} from {version}: {reply}"
        return reply.body["article"]

    def revisions(self, slug, viewer=None):
        return self.http.get(f"/api/articles/{slug}/revisions", token=_token(viewer))


def revision_list(reply):
    return list(reply.body.get("revisions", [])) if isinstance(reply.body, dict) else []
