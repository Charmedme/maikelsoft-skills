"""Protocol driver: the PostgreSQL database of the dashboard, for setup data that the API cannot make.

The API has no endpoint to give a user a role or a permission, and an audit log entry always gets the
current time. The DSL uses this driver to add test users with permissions and audit entries at fixed
moments. Test cases never use it directly.

The suite starts the database in the container `pca-db-eval<api port>`; PCA_DB_CONTAINER overrides it.
"""
import os
import subprocess
import urllib.parse


def container_for(base_url):
    if os.environ.get("PCA_DB_CONTAINER"):
        return os.environ["PCA_DB_CONTAINER"]
    return f"pca-db-eval{urllib.parse.urlparse(base_url).port}"


class Database:
    def __init__(self, base_url=None):
        self.container = container_for(base_url or os.environ["SUT_URL"])

    def run(self, sql):
        """Run SQL with psql; return the rows of the last statement as lists of text."""
        result = subprocess.run(
            ["docker", "exec", "-i", self.container, "psql", "-U", "postgres", "-d", "classifiedads",
             "-v", "ON_ERROR_STOP=1", "-q", "-At", "-F", "\t"],
            input=sql, text=True, capture_output=True, timeout=60)
        if result.returncode != 0:
            raise AssertionError(f"setup SQL failed in {self.container}: {result.stderr.strip()[:400]}")
        return [line.split("\t") for line in result.stdout.splitlines() if line]


def literal(value):
    """A SQL text literal."""
    if value is None:
        return "NULL"
    return "'" + str(value).replace("'", "''") + "'"
