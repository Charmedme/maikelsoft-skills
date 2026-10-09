import os
import pathlib
import sys
import tempfile
import unittest

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1]))
import todo  # noqa: E402


class Core(unittest.TestCase):
    def test_add_gives_next_id(self):
        tasks = todo.add(todo.add([], "a"), "b")
        self.assertEqual([t["id"] for t in tasks], [1, 2])

    def test_complete_marks_done(self):
        tasks = todo.complete(todo.add([], "a"), 1)
        self.assertTrue(tasks[0]["done"])

    def test_complete_unknown_raises(self):
        with self.assertRaises(KeyError):
            todo.complete([], 5)

    def test_render_empty(self):
        self.assertEqual(todo.render([]), "No tasks.")


class Cli(unittest.TestCase):
    def test_add_then_list(self):
        with tempfile.TemporaryDirectory() as d:
            os.environ["TODO_FILE"] = str(pathlib.Path(d) / "t.json")
            self.assertEqual(todo.main(["add", "Buy milk"]), 0)
            self.assertEqual(todo.load(todo.store_path())[0]["title"], "Buy milk")


if __name__ == "__main__":
    unittest.main()
