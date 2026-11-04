"""DSL for the dashboard (Practical.CleanArchitecture Monolith): domain actions in the words of the product.

Test cases call these, never HTTP or SQL. A member is a user name; the API signs the member in with the
test header X-Test-User. `admin` has all permissions, `user` can only read products. `new_member` makes a
fresh member with the Admin role or with chosen permissions, so that a case has its own audit trail and
its own rate-limit budget (200 requests per minute for each user).
"""
import datetime
import itertools
import os
import threading
import uuid

from drivers.database import Database, literal
from drivers.http import Http

ADMIN = "admin"
READER = "user"
_counter = itertools.count(1)

PERMISSION = {name: f"Permission:{name}" for name in (
    "GetProducts", "GetProduct", "AddProduct", "UpdateProduct", "DeleteProduct", "GetProductAuditLogs",
    "GetAuditLogs")}


def unique(prefix):
    return f"{prefix}{next(_counter)}{uuid.uuid4().hex[:6]}"


class Dashboard:
    def __init__(self, base_url=None):
        self.base_url = base_url or os.environ["SUT_URL"]
        self.http = Http(self.base_url, auth_scheme="Bearer", timeout=60)
        self._db = None

    @property
    def db(self):
        if self._db is None:
            self._db = Database(self.base_url)
        return self._db

    @staticmethod
    def unique(prefix):
        return unique(prefix)

    # members
    def new_member(self, role="Admin", permissions=None, name=None):
        """A new user with the given role (Admin or User), or with only the given permission names."""
        name = name or unique("member")
        email = f"{name}@example.test"
        sql = f"""
            INSERT INTO "Users" ("Id","UserName","NormalizedUserName","Email","NormalizedEmail","EmailConfirmed",
                "PhoneNumberConfirmed","TwoFactorEnabled","LockoutEnabled","AccessFailedCount","SecurityStamp",
                "ConcurrencyStamp","CreatedDateTime")
            VALUES (gen_random_uuid(), {literal(name)}, {literal(name.upper())}, {literal(email)},
                {literal(email.upper())}, true, false, false, false, 0, md5(random()::text),
                gen_random_uuid()::text, now());
        """
        if permissions is None:
            sql += f"""
            INSERT INTO "UserRoles" ("Id","UserId","RoleId","CreatedDateTime")
            SELECT gen_random_uuid(), u."Id", r."Id", now() FROM "Users" u, "Roles" r
            WHERE u."NormalizedUserName" = {literal(name.upper())} AND r."NormalizedName" = {literal(role.upper())};
            """
        else:
            for permission in permissions:
                sql += f"""
            INSERT INTO "UserClaims" ("Id","Type","Value","UserId","CreatedDateTime")
            SELECT gen_random_uuid(), 'Permission', {literal(PERMISSION.get(permission, permission))}, u."Id", now()
            FROM "Users" u WHERE u."NormalizedUserName" = {literal(name.upper())};
            """
        self.db.run(sql)
        return name

    # products
    def add_product(self, who, code, name, description="Description", **extra):
        body = {"code": code, "name": name, "description": description, **extra}
        return self.http.post("/api/products", body, headers=_as(who))

    def added_product(self, who, code=None, name=None, description="Description"):
        reply = self.add_product(who, code or unique("P-"), name or unique("Product "), description)
        assert reply.status == 201, f"add product: {reply}"
        return reply.body

    def product(self, who, product_id):
        return self.http.get(f"/api/products/{product_id}", headers=_as(who))

    def products(self, who):
        return self.http.get("/api/products", headers=_as(who))

    def update_product(self, who, product_id, **fields):
        """PUT the product; the fields are sent as given (code, name, description, rowVersion, ...)."""
        return self.http.put(f"/api/products/{product_id}", fields, headers=_as(who))

    def delete_product(self, who, product_id):
        return self.http.delete(f"/api/products/{product_id}", headers=_as(who))

    def product_history(self, who, product_id):
        """The audit trail of one product (GET /api/products/{id}/auditlogs), newest first."""
        return self.http.get(f"/api/products/{product_id}/auditlogs", headers=_as(who))

    def import_products(self, who, content, filename="products.csv"):
        """POST /api/products/importcsv with the file in the form field formFile; content is text or bytes."""
        data = content.encode("utf-8") if isinstance(content, str) else content
        return self.http.post_file("/api/products/importcsv", "formFile", filename, data,
                                   headers=_as(who), timeout=180)

    # audit log
    def audit_log(self, who):
        return self.http.get("/api/auditlogentries", headers=_as(who))

    def search_audit_log(self, who, **query):
        return self.http.get("/api/auditlogentries/search" + _query(query), headers=_as(who))

    def record_audit_entries(self, entries):
        """Store audit entries at fixed moments: a list of (user name, action, aware datetime, object id)."""
        values = []
        for user_name, action, moment, object_id in entries:
            stamp = moment.astimezone(datetime.timezone.utc).strftime("%Y-%m-%d %H:%M:%S.%f+00")
            values.append(f"""SELECT gen_random_uuid(), u."Id", {literal(action)}, {literal(object_id)},
                {literal('{"Code":"X"}')}, {literal(stamp)}::timestamptz FROM "Users" u
                WHERE u."NormalizedUserName" = {literal(user_name.upper())}""")
        self.db.run('INSERT INTO "AuditLogEntries" ("Id","UserId","Action","ObjectId","Log","CreatedDateTime")\n'
                    + "\nUNION ALL\n".join(values) + ";")

    # concurrency
    @staticmethod
    def at_the_same_moment(*actions):
        """Run the actions in threads that start together; return their results in order."""
        gate = threading.Barrier(len(actions))
        results = [None] * len(actions)

        def run(index, action):
            gate.wait()
            results[index] = action()

        threads = [threading.Thread(target=run, args=(i, a)) for i, a in enumerate(actions)]
        for thread in threads:
            thread.start()
        for thread in threads:
            thread.join(120)
        return results


    def only_products(self, who, names):
        """Make the catalog hold exactly new products with these names (all others are deleted)."""
        reply = self.products(who)
        assert reply.status == 200, f"products: {reply}"
        for product in reply.body:
            deleted = self.delete_product(who, product["id"])
            assert deleted.status == 200, f"delete {product['code']}: {deleted}"
        return [self.added_product(who, name=name) for name in names]


class ProductListPage:
    """The product list of the Angular app (/products), used the way a keyboard user meets it.

    Wraps a drivers.browser.Browser. Elements are found by role and accessible name only.
    """

    def __init__(self, browser):
        self.browser = browser
        self.page = browser.page

    def open(self, user=None):
        self.browser.open("/products" + (f"?test_user={user}" if user else ""))
        self.browser.set_timeout(5000)

    def delete_button(self, product_name):
        return self.browser.by_role("button", f"Delete {product_name}")

    def row_names(self, product_names):
        """The given product names in the order of the rows on the page."""
        rows = self.page.get_by_role("row").all_inner_texts()
        found = []
        for row in rows:
            for name in product_names:
                if name in row and name not in found:
                    found.append(name)
        return found

    def start_delete(self, product_name):
        """Move the focus to the delete button of the row and press Enter."""
        self._one(self.delete_button(product_name), f"button 'Delete {product_name}'").focus()
        self.browser.press("Enter")

    def dialog(self):
        return self.browser.by_role("dialog", "Delete product")

    def dialog_button(self, name):
        return self.dialog().get_by_role("button", name=name, exact=True)

    def press_in_dialog(self, name):
        """Move the focus to a button of the dialog and press Enter."""
        self._one(self.dialog_button(name), f"button '{name}' in a dialog named 'Delete product'").focus()
        self.browser.press("Enter")

    def _one(self, locator, what):
        if not self.browser.wait_until(lambda: locator.count() == 1):
            raise AssertionError(f"no single {what} on the page")
        return locator

    def dialog_open(self):
        return self.page.get_by_role("dialog").count() > 0

    def focus_is_on(self, locator, timeout=5.0):
        return self.browser.wait_until(lambda: self.browser.has_focus(locator), timeout)

    def status_says(self, text, timeout=5.0):
        return self.browser.wait_until(lambda: text in self.browser.texts("status"), timeout)

    def add_product_link(self):
        return self.browser.by_role("link", "Add Product")

    def shows(self, product_name):
        return self.page.get_by_role("row").filter(has_text=product_name).count() > 0

    def wait_for_rows(self, product_names, timeout=10.0):
        return self.browser.wait_until(lambda: all(self.shows(n) for n in product_names), timeout)


# outcome helpers, in domain words
def accepted(reply):
    return 200 <= reply.status < 300


def error_fields(reply):
    """The field names of a validation problem ({"errors": {field: [...]}}), lower case."""
    errors = reply.body.get("errors") if isinstance(reply.body, dict) else None
    return {str(name).lower() for name in errors} if isinstance(errors, dict) else set()


def refused_as_invalid(reply, field=None):
    """400 with a validation error on the field (the field name is compared ignoring case)."""
    return reply.status == 400 and (field is None or field.lower() in error_fields(reply))


def refused_as_conflict(reply):
    return reply.status == 409


def refused_as_unknown(reply):
    return reply.status == 404


def refused_as_forbidden(reply):
    return reply.status == 403


def refused_as_signed_out(reply):
    return reply.status == 401


def _as(who):
    return {"X-Test-User": who} if who else {}


def _query(query):
    if not query:
        return ""
    from urllib.parse import urlencode
    return "?" + urlencode({k: v for k, v in query.items() if v is not None})
