"""Run one headless claude process and stop it early when it is stuck.

Used by the run scripts of the evals. The watch reads the stream-json output while the run goes on and
stops the process when one of these is true:

- wall time: the run takes longer than `max_minutes`;
- stall: the stream has no new output for `stall_minutes`;
- loop: the same failing tool call (same tool, same input, same error) comes back `max_repeats` times;
- errors: `max_errors` tool errors come one after the other, with no success between them.

The stop reason goes to `<run>/stop.json`, so the scorer can report it. A run that ends by itself has no stop.json.
"""
import hashlib
import json
import pathlib
import subprocess
import time

DEFAULTS = {"max_minutes": 25, "stall_minutes": 6, "max_repeats": 4, "max_errors": 8, "poll_seconds": 10}


def _key(tool_use):
    raw = json.dumps({"name": tool_use.get("name"), "input": tool_use.get("input")}, sort_keys=True)
    return hashlib.sha1(raw.encode()).hexdigest()


class _Reader:
    """Reads the new lines of stream.jsonl and keeps the error counters."""

    def __init__(self, path):
        self.path, self.pos, self.buf = path, 0, ""
        self.calls, self.repeats, self.streak, self.tools = {}, {}, 0, 0

    def poll(self):
        """Read new output. Return the number of new bytes."""
        try:
            with open(self.path, encoding="utf-8", errors="ignore") as f:
                f.seek(self.pos)
                data = f.read()
                self.pos = f.tell()
        except OSError:
            return 0
        self.buf += data
        *lines, self.buf = self.buf.split("\n")
        for line in lines:
            if line.startswith("{"):
                try:
                    self._event(json.loads(line))
                except ValueError:
                    pass
        return len(data)

    def _event(self, d):
        if d.get("type") == "assistant":
            for c in d.get("message", {}).get("content", []):
                if c.get("type") == "tool_use":
                    self.calls[c["id"]] = _key(c)
                    self.tools += 1
        elif d.get("type") == "user":
            content = d.get("message", {}).get("content")
            for c in content if isinstance(content, list) else []:
                if c.get("type") != "tool_result":
                    continue
                if c.get("is_error"):
                    self.streak += 1
                    err = hashlib.sha1(json.dumps(c.get("content"), sort_keys=True)[:500].encode()).hexdigest()
                    k = (self.calls.get(c.get("tool_use_id")), err)
                    self.repeats[k] = self.repeats.get(k, 0) + 1
                else:
                    self.streak = 0

    def worst_repeat(self):
        return max(self.repeats.values(), default=0)


def run_watched(cmd, cwd, env, run_dir, limits=None, log=print):
    """Start cmd with stdout to run_dir/stream.jsonl. Return the exit code, or the stop reason."""
    lim = dict(DEFAULTS, **(limits or {}))
    run_dir = pathlib.Path(run_dir)
    stream = run_dir / "stream.jsonl"
    start = last_output = time.time()
    with open(stream, "w") as out, open(run_dir / "stderr.log", "w") as err:
        proc = subprocess.Popen(cmd, cwd=cwd, env=env, stdout=out, stderr=err)
        reader = _Reader(stream)
        reason = None
        while proc.poll() is None:
            time.sleep(lim["poll_seconds"])
            if reader.poll():
                last_output = time.time()
            now = time.time()
            if now - start > lim["max_minutes"] * 60:
                reason = f"wall time over {lim['max_minutes']} min"
            elif now - last_output > lim["stall_minutes"] * 60:
                reason = f"no output for {lim['stall_minutes']} min"
            elif reader.worst_repeat() >= lim["max_repeats"]:
                reason = f"the same failing tool call {reader.worst_repeat()} times"
            elif reader.streak >= lim["max_errors"]:
                reason = f"{reader.streak} tool errors in a row"
            if reason:
                proc.terminate()
                try:
                    proc.wait(30)
                except subprocess.TimeoutExpired:
                    proc.kill()
                break
        reader.poll()
    minutes = round((time.time() - start) / 60, 1)
    if reason:
        (run_dir / "stop.json").write_text(json.dumps({"reason": reason, "minutes": minutes, "tool_calls": reader.tools}),
                                           encoding="utf-8")
        log(f"STOP {run_dir}: {reason} after {minutes} min")
        return "stopped: " + reason
    return proc.returncode
