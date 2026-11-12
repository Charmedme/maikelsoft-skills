"""Tests for plugins/dev-setup/shared/rolestore.py and its SessionStart hook. Run: python3 -m unittest discover -s tests"""
import importlib.util
import json
import os
import pathlib
import subprocess
import sys
import tempfile
import unittest

SCRIPT = pathlib.Path(__file__).resolve().parents[1] / "plugins" / "dev-setup" / "shared" / "rolestore.py"
spec = importlib.util.spec_from_file_location("rolestore", SCRIPT)
rs = importlib.util.module_from_spec(spec)
spec.loader.exec_module(rs)
GIT = ["git", "-c", "user.name=T", "-c", "user.email=t@example.com", "-c", "commit.gpgsign=false"]


def run(cmd, cwd, env=None, stdin=None):
    return subprocess.run(cmd, cwd=cwd, env=env, input=stdin, capture_output=True, text=True)


class Normalize(unittest.TestCase):
    def test_remote_forms_give_one_key(self):
        same = ["https://github.com/Example/Shop-Api.git", "git@github.com:example/shop-api.git",
                "ssh://git@github.com:22/example/shop-api", "https://user:token@github.com/example/shop-api/"]
        self.assertEqual({rs.normalize(u) for u in same}, {"github.com/example/shop-api"})

    def test_azure_devops_ssh_and_https_give_one_key(self):
        https = rs.normalize("https://org@dev.azure.com/Org/Shop/_git/shop-api")
        ssh = rs.normalize("git@ssh.dev.azure.com:v3/Org/Shop/shop-api")
        self.assertEqual(https, "dev.azure.com/org/shop/_git/shop-api")
        self.assertEqual(ssh, https)


class Store(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        base = pathlib.Path(self.tmp.name)
        self.repo, self.data = base / "repo", base / "data"
        self.repo.mkdir()
        run(GIT + ["init", "-q", "-b", "main"], self.repo)
        (self.repo / "a.txt").write_text("a\n", encoding="utf-8")
        run(GIT + ["add", "-A"], self.repo)
        run(GIT + ["commit", "-q", "-m", "a"], self.repo)
        self.env = {k: v for k, v in os.environ.items() if k not in ("DELIVERY_ROLE", "CLAUDE_PLUGIN_DATA")}

    def tearDown(self):
        self.tmp.cleanup()

    def cli(self, *args, cwd=None, env=None):
        if args[0] in ("set", "clear") and "project" in args and "--cwd" not in args:
            args = (*args[:1], "--cwd", str(cwd or self.repo), *args[1:])
        return run([sys.executable, str(SCRIPT), "--data", str(self.data), *args], cwd or self.repo, env or self.env)

    def hook(self, cwd, extra=None):
        env = dict(self.env, CLAUDE_PLUGIN_DATA=str(self.data), **(extra or {}))
        r = run([sys.executable, str(SCRIPT), "hook"], self.repo, env, json.dumps({"cwd": str(cwd)}))
        return json.loads(r.stdout)["hookSpecificOutput"]["additionalContext"] if r.stdout.strip() else ""

    def test_project_wins_over_global(self):
        self.cli("set", "--role", "product owner", "--scope", "global")
        self.cli("set", "--role", "developer", "--scope", "project")
        self.assertIn("Delivery role: developer. Scope: project.", self.hook(self.repo))

    def test_global_applies_without_a_project_entry(self):
        self.cli("set", "--role", "product owner", "--scope", "global")
        self.assertIn("Delivery role: product owner. Scope: global.", self.hook(self.repo))

    def test_second_worktree_gets_the_same_entry(self):
        self.cli("set", "--role", "tech lead", "--scope", "project")
        other = pathlib.Path(self.tmp.name) / "worktree"
        run(GIT + ["worktree", "add", "-q", "-b", "feature", str(other)], self.repo)
        text = self.hook(other)
        self.assertIn("Delivery role: tech lead. Scope: project.", text)
        self.assertIn("team=on", text)

    def test_remote_key_is_the_same_in_a_clone(self):
        run(GIT + ["remote", "add", "origin", "https://github.com/example/shop-api.git"], self.repo)
        self.cli("set", "--role", "designer", "--scope", "project")
        clone = pathlib.Path(self.tmp.name) / "clone"
        run(["git", "clone", "-q", str(self.repo), str(clone)], self.tmp.name)
        run(GIT + ["remote", "set-url", "origin", "git@github.com:Example/shop-api.git"], clone)
        self.assertIn("Delivery role: designer. Scope: project.", self.hook(clone))

    def test_delivery_role_wins(self):
        self.cli("set", "--role", "developer", "--scope", "project")
        text = self.hook(self.repo, {"DELIVERY_ROLE": "product owner; team=off"})
        self.assertIn("Delivery role: product owner. Scope: cloud.", text)
        self.assertIn("team=off", text)

    def test_no_role_prints_nothing(self):
        self.assertEqual(self.hook(self.repo), "")

    def test_no_data_folder_prints_nothing(self):
        r = run([sys.executable, str(SCRIPT), "hook"], self.repo, self.env, json.dumps({"cwd": str(self.repo)}))
        self.assertEqual((r.returncode, r.stdout), (0, ""))

    def test_route_changes_only_the_named_step(self):
        self.cli("set", "--role", "developer", "--scope", "project", "docs=off")
        self.cli("route", "team=on")
        entry = json.loads((self.data / "roles.json").read_text(encoding="utf-8"))["projects"]
        route = next(iter(entry.values()))["route"]
        self.assertEqual(route, dict(rs.DEFAULTS["developer"], docs="off", team="on"))

    def test_bad_step_fails(self):
        self.cli("set", "--role", "developer", "--scope", "global")
        r = self.cli("route", "--scope", "global", "speed=fast")
        self.assertNotEqual(r.returncode, 0)

    def test_route_without_role_fails(self):
        self.assertEqual(self.cli("route", "team=on").returncode, 2)

    def test_clear_project_falls_back_to_global(self):
        self.cli("set", "--role", "product owner", "--scope", "global")
        self.cli("set", "--role", "developer", "--scope", "project")
        self.cli("clear", "--scope", "project")
        self.assertIn("Scope: global", self.hook(self.repo))

    def test_project_scope_needs_cwd(self):
        r = run([sys.executable, str(SCRIPT), "--data", str(self.data), "set", "--role", "developer", "--scope", "project"],
                self.repo, self.env)
        self.assertEqual(r.returncode, 2)
        self.assertIn("--cwd", r.stderr)

    def test_set_prints_the_project_key(self):
        run(GIT + ["remote", "add", "origin", "https://github.com/example/shop-api.git"], self.repo)
        r = self.cli("set", "--role", "developer", "--scope", "project")
        self.assertIn("Project key: github.com/example/shop-api", r.stdout)

    def test_project_scope_outside_git_fails(self):
        outside = pathlib.Path(self.tmp.name) / "plain"
        outside.mkdir()
        r = self.cli("set", "--role", "developer", "--scope", "project", "--cwd", str(outside), cwd=outside)
        self.assertEqual(r.returncode, 2)


if __name__ == "__main__":
    unittest.main()
