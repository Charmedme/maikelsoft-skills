"""Protocol driver: JSON over HTTP. The DSL layer above uses it; test cases never do."""
import json
import urllib.error
import urllib.request
import uuid


class Response:
    def __init__(self, status, body, headers):
        self.status = status
        self.body = body
        self.headers = headers

    def __repr__(self):
        return f"<{self.status} {json.dumps(self.body)[:300]}>"


class Http:
    def __init__(self, base_url, auth_scheme="Token", timeout=20):
        self.base_url = base_url.rstrip("/")
        self.auth_scheme = auth_scheme
        self.timeout = timeout

    def request(self, method, path, body=None, token=None, headers=None):
        data = None if body is None else json.dumps(body).encode("utf-8")
        request = urllib.request.Request(self.base_url + path, data=data, method=method)
        request.add_header("Accept", "application/json")
        if data is not None:
            request.add_header("Content-Type", "application/json")
        if token:
            request.add_header("Authorization", f"{self.auth_scheme} {token}")
        for name, value in (headers or {}).items():
            request.add_header(name, value)
        try:
            with urllib.request.urlopen(request, timeout=self.timeout) as reply:
                return Response(reply.status, _decode(reply.read()), dict(reply.headers))
        except urllib.error.HTTPError as error:
            return Response(error.code, _decode(error.read()), dict(error.headers))

    def get(self, path, **kw):
        return self.request("GET", path, **kw)

    def post(self, path, body=None, **kw):
        return self.request("POST", path, body, **kw)

    def put(self, path, body=None, **kw):
        return self.request("PUT", path, body, **kw)

    def delete(self, path, body=None, **kw):
        return self.request("DELETE", path, body, **kw)

    def post_file(self, path, field, filename, content, content_type="text/csv", headers=None, timeout=None):
        """POST one file as multipart/form-data under the form field `field`."""
        boundary = "----evalboundary" + uuid.uuid4().hex
        data = b"".join([
            f"--{boundary}\r\n".encode(),
            f'Content-Disposition: form-data; name="{field}"; filename="{filename}"\r\n'.encode(),
            f"Content-Type: {content_type}\r\n\r\n".encode(),
            content,
            f"\r\n--{boundary}--\r\n".encode(),
        ])
        request = urllib.request.Request(self.base_url + path, data=data, method="POST")
        request.add_header("Accept", "application/json")
        request.add_header("Content-Type", f"multipart/form-data; boundary={boundary}")
        for name, value in (headers or {}).items():
            request.add_header(name, value)
        try:
            with urllib.request.urlopen(request, timeout=timeout or self.timeout) as reply:
                return Response(reply.status, _decode(reply.read()), dict(reply.headers))
        except urllib.error.HTTPError as error:
            return Response(error.code, _decode(error.read()), dict(error.headers))


def _decode(raw):
    if not raw:
        return None
    try:
        return json.loads(raw.decode("utf-8"))
    except ValueError:
        return raw.decode("utf-8", errors="replace")
