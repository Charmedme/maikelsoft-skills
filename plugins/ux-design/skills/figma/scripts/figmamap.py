#!/usr/bin/env python3
"""Map Figma variables to the design tokens of the project, and count the Figma MCP calls.

    python3 figmamap.py map <variables.json> --tokens <tokens.json | tokens.css>... [--out figma-map.md] [--json]
    python3 figmamap.py count <figma-calls.log> --limit <n> [--add <tool> [--node <id>]]

map    <variables.json> is the result of the get_variable_defs tool, saved as JSON: an object of
       {"<collection/name>": "<value>"}, or a list of {"name", "value"}. The tokens are a DTCG
       file or a CSS file with custom properties. For each variable, the script finds the token
       with the same value, and the token whose name matches best. It writes a Markdown table:
         exact     same value and a matching name
         value     same value, other name: use the token, and note the name
         conflict  matching name, other value: the design and the code differ
         none      no token: a proposed new token, or a value outside the system
       Exit code 1 when there is a conflict.
count  Counts the read calls of today in the log. With --add it first adds one line for the
       tool. Write tools (use_figma, upload_assets, generate_image, generate_figma_design,
       create_new_file, add_code_connect_map, whoami) do not count. Exit code 3 when the count
       is at or above the limit: stop the reads and tell the human.
"""
import argparse
import datetime
import json
import re
import sys
from pathlib import Path

EXEMPT = {"use_figma", "upload_assets", "generate_image", "generate_figma_design", "create_new_file",
          "add_code_connect_map", "whoami"}


def norm_value(v):
    """Return a value in one form: #rrggbb for colors, a number string for sizes."""
    if isinstance(v, (int, float)):
        n = float(v)
        return f"{int(n) if n.is_integer() else n}"
    s = str(v).strip().lower()
    m = re.fullmatch(r"#([0-9a-f]{3}|[0-9a-f]{6})(ff)?", s)
    if m:
        h = m.group(1)
        return "#" + ("".join(c * 2 for c in h) if len(h) == 3 else h)
    m = re.fullmatch(r"(-?[\d.]+)\s*(px)?", s)
    if m:
        n = float(m.group(1))
        return f"{int(n) if n.is_integer() else n}"
    m = re.fullmatch(r"rgba?\((\d+),\s*(\d+),\s*(\d+)(?:,\s*1(?:\.0+)?)?\)", s)
    if m:
        return "#" + "".join(f"{int(m.group(i)):02x}" for i in (1, 2, 3))
    return s


def words(name):
    name = re.sub(r"([a-z])([A-Z])", r"\1-\2", name).lower()
    w = [x for x in re.split(r"[^a-z0-9]+", name) if x]
    stop = {"color", "colors", "colour", "fl", "var", "token", "tokens", "primitives", "semantic", "default", "base", "value"}
    return [x for x in w if x not in stop] or w


def name_score(a, b):
    wa, wb = set(words(a)), set(words(b))
    if not wa or not wb:
        return 0.0
    return len(wa & wb) / len(wa | wb)


def load_tokens(paths):
    out = {}
    for p in paths:
        text = Path(p).read_text(encoding="utf-8")
        if p.endswith(".css"):
            text = re.sub(r"/\*.*?\*/", "", text, flags=re.S)
            props = dict(re.findall(r"--([\w-]+)\s*:\s*([^;]+);", text))

            def res(v, d=0):
                m = re.fullmatch(r"var\(\s*--([\w-]+)\s*\)", v.strip())
                return res(props.get(m.group(1), ""), d + 1) if m and d < 5 else v.strip()
            for k, v in props.items():
                out["--" + k] = norm_value(res(v))
            continue
        doc = json.loads(text)

        def walk(node, path):
            if not isinstance(node, dict):
                return
            if "$value" in node:
                v = node["$value"]
                if isinstance(v, dict) and "hex" in v:
                    v = v["hex"]
                elif isinstance(v, dict) and "value" in v and "unit" in v:
                    v = f'{v["value"]}{v["unit"]}' if v["unit"] == "rem" else v["value"]
                if isinstance(v, (str, int, float)):
                    out[".".join(path)] = norm_value(v)
                return
            for k, c in node.items():
                if not k.startswith("$") or k == "$root":
                    walk(c, path + [k])
        walk(doc, [])
    return out


def load_vars(path):
    data = json.loads(Path(path).read_text(encoding="utf-8"))
    if isinstance(data, dict):
        return [(k, v) for k, v in data.items() if isinstance(v, (str, int, float))]
    return [(x["name"], x["value"]) for x in data if isinstance(x, dict) and "name" in x]


def cmd_map(args):
    tokens = load_tokens(args.tokens)
    by_value = {}
    for k, v in tokens.items():
        by_value.setdefault(v, []).append(k)
    rows, conflicts = [], 0
    for name, raw in load_vars(args.variables):
        v = norm_value(raw)
        same = by_value.get(v, [])
        best_same = max(same, key=lambda t: name_score(name, t), default=None)
        best_name = max(tokens, key=lambda t: name_score(name, t), default=None)
        s_same = name_score(name, best_same) if best_same else 0
        s_name = name_score(name, best_name) if best_name else 0
        if best_same and s_same >= 0.5:
            rows.append((name, raw, best_same, "exact"))
        elif best_name and s_name >= 0.6 and tokens[best_name] != v:
            conflicts += 1
            rows.append((name, raw, f"{best_name} = {tokens[best_name]}", "conflict"))
        elif best_same:
            rows.append((name, raw, best_same, "value"))
        else:
            rows.append((name, raw, "", "none"))
    lines = ["| Figma variable | Value | Project token | Match |", "|---|---|---|---|"]
    lines += [f"| `{n}` | `{v}` | {('`' + t + '`') if t else '-'} | {m} |" for n, v, t, m in rows]
    counts = {m: sum(1 for r in rows if r[3] == m) for m in ("exact", "value", "conflict", "none")}
    summary = ", ".join(f"{c} {m}" for m, c in counts.items())
    text = "\n".join(lines) + f"\n\n{len(rows)} variables: {summary}.\n"
    if args.out:
        Path(args.out).write_text(text, encoding="utf-8")
    if args.json:
        print(json.dumps({"rows": [dict(zip(("variable", "value", "token", "match"), r)) for r in rows], "counts": counts}, indent=2))
    else:
        print(text)
    return 1 if conflicts else 0


def cmd_count(args):
    log = Path(args.log)
    today = datetime.date.today().isoformat()
    if args.add:
        log.parent.mkdir(parents=True, exist_ok=True)
        with log.open("a", encoding="utf-8") as f:
            f.write(f"{datetime.datetime.now().isoformat(timespec='seconds')}\t{args.add}\t{args.node or '-'}\n")
    n = 0
    if log.exists():
        for line in log.read_text(encoding="utf-8").splitlines():
            parts = line.split("\t")
            if len(parts) >= 2 and parts[0].startswith(today) and parts[1] not in EXEMPT:
                n += 1
    left = args.limit - n
    print(f"{n} read calls today, limit {args.limit}, {max(0, left)} left.")
    return 3 if left <= 0 else 0


def main(argv=None):
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = p.add_subparsers(dest="cmd", required=True)
    c = sub.add_parser("map")
    c.add_argument("variables")
    c.add_argument("--tokens", nargs="+", required=True)
    c.add_argument("--out")
    c.add_argument("--json", action="store_true")
    c.set_defaults(fn=cmd_map)
    c = sub.add_parser("count")
    c.add_argument("log")
    c.add_argument("--limit", type=int, required=True)
    c.add_argument("--add")
    c.add_argument("--node")
    c.set_defaults(fn=cmd_count)
    args = p.parse_args(argv)
    return args.fn(args)


if __name__ == "__main__":
    sys.exit(main())
