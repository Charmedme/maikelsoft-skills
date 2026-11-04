#!/usr/bin/env python3
"""Mock of the products API for local development, with the built app on the same port.

    npm ci && npx ng build
    python3 mock-api/server.py            # http://localhost:8080 (env PORT, STATIC_DIR, API_DELAY_MS)

The app is served from dist/<project>/browser, with index.html for each unknown path (SPA routes).
The data lives in memory and starts again from the seed at each start, or after POST /__reset.
See README.md for the API and the dev endpoints.
"""
import copy
import datetime
import json
import mimetypes
import os
import pathlib
import re
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import parse_qs, urlsplit

ROOT = pathlib.Path(__file__).resolve().parent.parent
SEED = [
    {"id": 1, "name": "Oak desk", "code": "OD-100", "price": 349.0, "availableFrom": "2026-01-12"},
    {"id": 2, "name": "Desk lamp", "code": "DL-210", "price": 39.95, "availableFrom": "2026-02-01"},
    {"id": 3, "name": "Office chair", "code": "OC-300", "price": 189.0, "availableFrom": "2026-03-15"},
    {"id": 4, "name": "Monitor arm", "code": "MA-410", "price": 79.5, "availableFrom": "2026-04-01"},
    {"id": 5, "name": "Cable tray", "code": "CT-520", "price": 24.0, "availableFrom": "2026-05-20"},
]
CODE = re.compile(r"^[A-Z0-9-]{3,20}$")
DATE = re.compile(r"^\d{4}-\d{2}-\d{2}")


class Store:
    def __init__(self):
        self.lock = threading.Lock()
        self.reset()

    def reset(self):
        self.products = {p["id"]: {**copy.deepcopy(p), "version": 1} for p in SEED}
        self.next_id = max(self.products) + 1
        self.requests = []
        self.failures = []
        self.price_changes = {}


STORE = Store()


def static_dir():
    if os.environ.get("STATIC_DIR"):
        return pathlib.Path(os.environ["STATIC_DIR"]).resolve()
    found = sorted(ROOT.glob("dist/*/browser/index.html"))
    return found[0].parent if found else ROOT / "dist"


def problem(status, title, detail=None, errors=None):
    body = {"type": "about:blank", "title": title, "status": status}
    if detail:
        body["detail"] = detail
    if errors:
        body["errors"] = errors
    return status, body


def validate(data, own_id=None):
    """Return the errors of a product body, with PascalCase keys as ASP.NET Core sends them."""
    errors = {}
    name = data.get("name")
    if not isinstance(name, str) or not name.strip():
        errors["Name"] = ["The name is required."]
    elif len(name) > 100:
        errors["Name"] = ["The name can have at most 100 characters."]
    code = data.get("code")
    if not isinstance(code, str) or not CODE.match(code):
        errors["Code"] = ["The code must have 3 to 20 capital letters, digits, or hyphens."]
    elif any(p["code"] == code and p["id"] != own_id for p in STORE.products.values()):
        errors["Code"] = [f"The code {code} is already used by another product."]
    price = data.get("price")
    if isinstance(price, bool) or not isinstance(price, (int, float)) or price < 0:
        errors["Price"] = ["The price must be a number of 0 or more."]
    if not valid_date(data.get("availableFrom")):
        errors["AvailableFrom"] = ["The date is not valid."]
    return errors


def valid_date(value):
    if not isinstance(value, str) or not DATE.match(value):
        return False
    try:
        datetime.date.fromisoformat(value[:10])
        return True
    except ValueError:
        return False


def price_lines(ids, percentage):
    lines = []
    for i in ids:
        product = STORE.products.get(i)
        if product:
            new = round(product["price"] * (1 + percentage / 100), 2)
            lines.append({"productId": i, "name": product["name"], "oldPrice": product["price"], "newPrice": new})
    return lines


def parse_ids(value):
    return [int(x) for x in re.findall(r"\d+", value or "")]


def public(product):
    return {k: v for k, v in product.items() if k != "version"}


def etag(product):
    return f'"{product["version"]}"'


def product_fields(data):
    return {"name": data["name"].strip(), "code": data["code"], "price": float(data["price"]),
            "availableFrom": str(data["availableFrom"])[:10]}


class Handler(BaseHTTPRequestHandler):
    server_version = "ProductsMock/1.0"

    def log_message(self, fmt, *args):
        if os.environ.get("MOCK_API_LOG"):
            super().log_message(fmt, *args)

    # -- plumbing ---------------------------------------------------------------
    def read_body(self):
        length = int(self.headers.get("Content-Length") or 0)
        raw = self.rfile.read(length) if length else b""
        if not raw:
            return None
        try:
            return json.loads(raw)
        except ValueError:
            return raw.decode("utf-8", "replace")

    def send(self, status, body=None, headers=None):
        payload = b"" if body is None else json.dumps(body).encode()
        self.send_response(status)
        if body is not None:
            ctype = "application/problem+json" if isinstance(body, dict) and "title" in body and "status" in body else "application/json"
            self.send_header("Content-Type", ctype)
        self.send_header("Content-Length", str(len(payload)))
        self.send_header("Cache-Control", "no-store")
        for k, v in (headers or {}).items():
            self.send_header(k, v)
        self.end_headers()
        if payload and self.command != "HEAD":
            self.wfile.write(payload)

    def handle_any(self):
        url = urlsplit(self.path)
        path = url.path.rstrip("/") or "/"
        if path.startswith("/__"):
            return self.dev(path, self.read_body())
        if path == "/api" or path.startswith("/api/"):
            body = self.read_body()
            with STORE.lock:
                STORE.requests.append({"method": self.command, "path": self.path, "headers": dict(self.headers.items()),
                                       "body": body, "time": time.time()})
                failure = self.take_failure(path)
            delay = float(os.environ.get("API_DELAY_MS", "50")) / 1000
            if delay:
                time.sleep(delay)
            if failure:
                return self.send(*problem(failure, "The server could not handle the request."))
            with STORE.lock:
                return self.api(path, parse_qs(url.query), body)
        if self.command not in ("GET", "HEAD"):
            return self.send(405)
        return self.static(path)

    def take_failure(self, path):
        for f in STORE.failures:
            if f["path"] == path and f.get("method", self.command).upper() == self.command:
                f["times"] -= 1
                if f["times"] <= 0:
                    STORE.failures.remove(f)
                return f["status"]
        return None

    def static(self, path):
        base = static_dir()
        target = (base / path.lstrip("/")).resolve()
        if base not in target.parents or not target.is_file():
            target = base / "index.html"
        if not target.is_file():
            return self.send(404, {"title": "Build the app first: npx ng build", "status": 404})
        data = target.read_bytes()
        self.send_response(200)
        ctype = mimetypes.guess_type(target.name)[0] or "application/octet-stream"
        if target.suffix in (".js", ".mjs"):
            ctype = "text/javascript"
        self.send_header("Content-Type", ctype)
        self.send_header("Content-Length", str(len(data)))
        self.send_header("Cache-Control", "no-cache")
        self.end_headers()
        if self.command != "HEAD":
            self.wfile.write(data)

    # -- dev endpoints ---------------------------------------------------------
    def dev(self, path, body):
        """Dev endpoints, for manual tests and end-to-end tests. They are not part of the real API."""
        with STORE.lock:
            if path == "/__reset" and self.command == "POST":
                STORE.reset()
                return self.send(204)
            if path == "/__requests" and self.command == "GET":
                return self.send(200, STORE.requests)
            if path == "/__fail" and self.command == "POST" and isinstance(body, dict) and "path" in body:
                STORE.failures.append({"path": body["path"].rstrip("/"), "status": int(body.get("status", 500)),
                                       "times": int(body.get("times", 1)), **({"method": body["method"].upper()} if body.get("method") else {})})
                return self.send(204)
            m = re.fullmatch(r"/__touch/(\d+)", path)
            if m and self.command == "POST":
                product = STORE.products.get(int(m.group(1)))
                if not product:
                    return self.send(404)
                product["version"] += 1
                if isinstance(body, dict):
                    product.update({k: v for k, v in body.items() if k in ("name", "code", "price", "availableFrom")})
                return self.send(200, public(product), {"ETag": etag(product)})
        return self.send(404)

    # -- API ---------------------------------------------------------------------
    def api(self, path, query, body):
        if path == "/api/me" and self.command == "GET":
            return self.send(200, {"name": "Dev user", "roles": ["catalog", "pricing"]})
        if path.startswith("/api/price-changes"):
            return self.price_change(path, query, body)
        if path == "/api/products":
            if self.command == "GET":
                items = sorted(STORE.products.values(), key=lambda p: p["id"])
                term = (query.get("search") or [""])[0].strip().lower()
                if term:
                    items = [p for p in items if term in p["name"].lower() or term in p["code"].lower()]
                return self.send(200, [public(p) for p in items])
            if self.command == "POST":
                data = body if isinstance(body, dict) else {}
                errors = validate(data)
                if errors:
                    return self.send(*problem(400, "One or more validation errors occurred.", errors=errors))
                product = {"id": STORE.next_id, **product_fields(data), "version": 1}
                STORE.products[product["id"]] = product
                STORE.next_id += 1
                return self.send(201, public(product), {"ETag": etag(product), "Location": f"/api/products/{product['id']}"})
            return self.send(405)
        m = re.fullmatch(r"/api/products/(\d+)", path)
        if m:
            product = STORE.products.get(int(m.group(1)))
            if self.command in ("GET", "HEAD"):
                if not product:
                    return self.send(*problem(404, "Not Found", f"Product {m.group(1)} does not exist."))
                return self.send(200, public(product), {"ETag": etag(product)})
            if self.command == "PUT":
                return self.put(m.group(1), product, body)
            if self.command == "DELETE":
                if not product:
                    return self.send(*problem(404, "Not Found", f"Product {m.group(1)} does not exist."))
                del STORE.products[product["id"]]
                return self.send(204)
            return self.send(405)
        return self.send(*problem(404, "Not Found"))

    def price_change(self, path, query, body):
        if path == "/api/price-changes/preview" and self.command == "GET":
            try:
                percentage = float((query.get("percentage") or ["0"])[0])
            except ValueError:
                return self.send(*problem(400, "One or more validation errors occurred.",
                                          errors={"Percentage": ["The percentage is not a number."]}))
            return self.send(200, price_lines(parse_ids((query.get("ids") or [""])[0]), percentage))
        if path == "/api/price-changes" and self.command == "POST":
            data = body if isinstance(body, dict) else {}
            errors = {}
            ids = data.get("productIds")
            if not isinstance(ids, list) or not ids or any(i not in STORE.products for i in ids):
                errors["ProductIds"] = ["Select one or more existing products."]
            percentage = data.get("percentage")
            if isinstance(percentage, bool) or not isinstance(percentage, (int, float)) or not -90 <= percentage <= 100:
                errors["Percentage"] = ["The percentage must be between -90 and 100."]
            if not valid_date(data.get("effectiveDate")) or len(str(data.get("effectiveDate"))) != 10:
                errors["EffectiveDate"] = ["Use a date without a time (yyyy-MM-dd)."]
            if errors:
                return self.send(*problem(400, "One or more validation errors occurred.", errors=errors))
            change_id = len(STORE.price_changes) + 1
            change = {"id": change_id, "productIds": ids, "percentage": percentage, "effectiveDate": data["effectiveDate"],
                      "lines": price_lines(ids, percentage)}
            STORE.price_changes[change_id] = change
            return self.send(201, change, {"Location": f"/api/price-changes/{change_id}"})
        m = re.fullmatch(r"/api/price-changes/(\d+)", path)
        if m and self.command == "GET":
            change = STORE.price_changes.get(int(m.group(1)))
            if not change:
                return self.send(*problem(404, "Not Found", f"Price change {m.group(1)} does not exist."))
            return self.send(200, change)
        return self.send(*problem(404, "Not Found"))

    def put(self, raw_id, product, body):
        if not product:
            return self.send(*problem(404, "Not Found", f"Product {raw_id} does not exist."))
        if_match = self.headers.get("If-Match")
        if not if_match:
            return self.send(*problem(428, "Precondition Required", "Send the ETag of the product in If-Match."))
        tags = [t.strip().removeprefix("W/").strip('"') for t in if_match.split(",")]
        if "*" not in tags and str(product["version"]) not in tags:
            return self.send(*problem(412, "Precondition Failed", "The product was changed by someone else."))
        data = body if isinstance(body, dict) else {}
        errors = validate(data, own_id=product["id"])
        if errors:
            return self.send(*problem(400, "One or more validation errors occurred.", errors=errors))
        product.update(product_fields(data))
        product["version"] += 1
        return self.send(200, public(product), {"ETag": etag(product)})

    do_GET = do_POST = do_PUT = do_DELETE = do_PATCH = do_HEAD = handle_any


def main():
    port = int(os.environ.get("PORT", "8080"))
    server = ThreadingHTTPServer(("127.0.0.1", port), Handler)
    print(f"Mock API and app on http://localhost:{port} (static files: {static_dir()})", flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass


if __name__ == "__main__":
    main()
