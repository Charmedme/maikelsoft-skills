#!/usr/bin/env python3
"""Read a backlog draft, find duplicates, keep a push log, and export files.

    python3 backlogtree.py tree <file.md> [...] [--json]
    python3 backlogtree.py similar <file.md> --candidates <candidates.json> [--min 0.5]
    python3 backlogtree.py log init <push-log.json> <file.md> [...] --system <name>
    python3 backlogtree.py log set <push-log.json> <item id> <remote id> [--url <url>] [--status created|linked|skipped|updated]
    python3 backlogtree.py log status <push-log.json>
    python3 backlogtree.py export <file.md> [...] --format md|txt|html --split|--combined --out <folder>

An item is a Markdown heading with a "**Type:**" line under it, as the
backlog-* skills write it. "**Parent:**" names the title of the parent item.

candidates.json is a list of {"id": ..., "title": ..., "url": ...} from a
search in the ticket system. "similar" scores each draft item against each
candidate and prints the pairs with a score of --min or higher.

Exit code: 1 on an error in the input (unknown parent, missing file), else 0.
"""
import argparse
import html
import json
import pathlib
import re
import sys

ORDER = {"product goal": 0, "productdoel": 0, "epic": 1, "feature": 2, "kenmerk": 2,
         "user story": 3, "story": 3, "product backlog item": 3, "pbi": 3, "requirement": 3,
         "issue": 3, "spike": 3, "enabler": 3, "sprint goal": 9, "sprintdoel": 9}
HEADING = re.compile(r"^(#{1,6})\s+(.*)$")
TYPE_LINE = re.compile(r"^\*\*(type|soort):\*\*\s*(.+)$", re.I)
PARENT_LINE = re.compile(r"^\*\*(parent|ouder|bovenliggend):\*\*\s*(.+)$", re.I)
STOP = set("a an the of for to in on by and or with my our your is are be can i we as so that this "
           "de het een van voor naar in op met en of mijn onze je u is zijn als zodat dat dit wil ik".split())
NONE_PARENTS = {"none", "geen", "-", ""}
MIN_SCORE = 0.5   # similar: a lower score gives too many false matches


def slug(text):
    return re.sub(r"[^a-z0-9]+", "-", text.lower()).strip("-")[:60]


def parse(paths):
    items = []
    for path in paths:
        lines = pathlib.Path(path).read_text(encoding="utf-8").splitlines()
        heads = [(i, len(m.group(1)), m.group(2).strip()) for i, l in enumerate(lines) if (m := HEADING.match(l))]
        def own_type(n):
            for l in lines[heads[n][0] + 1:heads[n][0] + 7]:
                if HEADING.match(l):
                    return False
                if TYPE_LINE.match(l.strip()):
                    return True
            return False

        flags = [own_type(n) for n in range(len(heads))]
        for n, (i, level, title) in enumerate(heads):
            end = len(lines)
            for k in range(n + 1, len(heads)):
                j, lvl, _ = heads[k]
                if lvl <= level or flags[k]:
                    end = j
                    break
            block = lines[i + 1:end]
            kind = parent = None
            for l in block[:6]:
                s = l.strip()
                if HEADING.match(l):
                    break
                if m := TYPE_LINE.match(s):
                    kind = m.group(2).strip()
                if m := PARENT_LINE.match(s):
                    parent = m.group(2).strip()
            if kind is None:
                continue
            body = "\n".join(l for l in block if not TYPE_LINE.match(l.strip()) and not PARENT_LINE.match(l.strip()))
            items.append({"id": slug(title), "type": kind, "title": title,
                          "parent": None if (parent or "").lower() in NONE_PARENTS else parent,
                          "parent_text": parent,
                          "rank": ORDER.get(kind.lower(), 5), "body": body.strip(),
                          "file": str(path), "line": i + 1})
    return items


def check_tree(items):
    errors = []
    titles = {}
    for it in items:
        if it["title"].lower() in titles:
            errors.append(f"Two items have the title \"{it['title']}\".")
        titles[it["title"].lower()] = it
    for it in items:
        p = it["parent"]
        if p and p.lower() not in titles:
            errors.append(f"\"{it['title']}\": the parent \"{p}\" is not in the draft. "
                          "It must exist in the ticket system, or the item needs another parent.")
        elif p and titles[p.lower()]["rank"] >= it["rank"]:
            errors.append(f"\"{it['title']}\" ({it['type']}) has a parent of the same or a lower level.")
    return errors


def ordered(items):
    """Parents before children: the push order."""
    return sorted(items, key=lambda it: (it["rank"], it["file"], it["line"]))


def words(text):
    return {w.rstrip("s") for w in re.findall(r"[a-z0-9à-ÿ]+", text.lower()) if w not in STOP and len(w) > 1}


def score(a, b):
    wa, wb = words(a), words(b)
    return round(len(wa & wb) / len(wa | wb), 2) if wa and wb else 0.0


def cmd_tree(args):
    items = parse(args.files)
    errors = check_tree(items)
    if args.json:
        print(json.dumps({"items": [{k: v for k, v in it.items() if k != "body"} for it in ordered(items)],
                          "errors": errors}, indent=2))
    else:
        by_parent = {}
        for it in ordered(items):
            by_parent.setdefault((it["parent"] or "").lower(), []).append(it)

        def show(key, depth):
            for it in by_parent.get(key, []):
                print(f"{'  ' * depth}- [{it['type']}] {it['title']}  (id: {it['id']})")
                show(it["title"].lower(), depth + 1)
        show("", 0)
        known = {it["title"].lower() for it in items}
        for key, children in by_parent.items():
            if key and key not in known:
                print(f"- [outside the draft] {key}")
                for it in children:
                    print(f"  - [{it['type']}] {it['title']}  (id: {it['id']})")
        for e in errors:
            print(f"ERROR {e}")
        print(f"{len(items)} items, {len(errors)} errors")
    return 1 if errors else 0


def cmd_similar(args):
    items = parse(args.files)
    cands = json.loads(pathlib.Path(args.candidates).read_text(encoding="utf-8"))
    hits = []
    for it in items:
        for c in cands:
            s = score(it["title"], c.get("title", ""))
            if s >= args.min:
                hits.append({"item": it["id"], "title": it["title"], "candidate": c.get("id"),
                             "candidate_title": c.get("title"), "url": c.get("url"), "score": s})
    hits.sort(key=lambda h: -h["score"])
    for h in hits:
        print(f"{h['score']:.2f}  {h['title']}  <->  {h['candidate']}: {h['candidate_title']}")
    print(f"{len(hits)} possible duplicates (score >= {args.min})")
    return 0


def cmd_log(args):
    log_path = pathlib.Path(args.log)
    if args.action == "init":
        items = ordered(parse(args.files))
        if log_path.exists():
            print(f"{log_path}: exists. Use \"log status\" to continue a push.")
            return 1
        data = {"system": args.system, "items": [{"id": it["id"], "type": it["type"], "title": it["title"],
                                                   "parent": it["parent"], "remote_id": None, "url": None,
                                                   "status": "pending"} for it in items]}
        log_path.write_text(json.dumps(data, indent=2), encoding="utf-8")
        print(f"{log_path}: {len(items)} items, all pending")
        return 0
    data = json.loads(log_path.read_text(encoding="utf-8"))
    if args.action == "set":
        for it in data["items"]:
            if it["id"] == args.item:
                it.update(remote_id=args.remote, url=args.url, status=args.status)
                log_path.write_text(json.dumps(data, indent=2), encoding="utf-8")
                print(f"{args.item}: {args.status} as {args.remote}")
                return 0
        print(f"{args.item}: not in the log")
        return 1
    remote = {it["title"].lower(): it["remote_id"] for it in data["items"]}
    counts = {}
    for it in data["items"]:
        counts[it["status"]] = counts.get(it["status"], 0) + 1
        parent_id = remote.get((it["parent"] or "").lower()) if it["parent"] else "-"
        print(f"{it['status']:8} {it['remote_id'] or '':10} [{it['type']}] {it['title']}  (parent id: {parent_id})")
    print(", ".join(f"{v} {k}" for k, v in counts.items()))
    nxt = next((it for it in data["items"] if it["status"] == "pending"), None)
    print(f"next: {nxt['id']}" if nxt else "next: none, the push is complete")
    return 0


def to_html(md):
    out, in_list = [], False
    for line in md.splitlines():
        s = line.strip()
        text = html.escape(s)
        text = re.sub(r"\*\*(.+?)\*\*", r"<strong>\1</strong>", text)
        text = re.sub(r"`(.+?)`", r"<code>\1</code>", text)
        if m := re.match(r"^(#{1,6})\s+(.*)$", s):
            if in_list:
                out.append("</ul>")
                in_list = False
            n = len(m.group(1))
            out.append(f"<h{n}>{html.escape(m.group(2))}</h{n}>")
        elif m := re.match(r"^[-*]\s+(\[[ xX]\]\s+)?(.*)$", s):
            if not in_list:
                out.append("<ul>")
                in_list = True
            box = "&#9744; " if m.group(1) else ""
            item = re.sub(r"\*\*(.+?)\*\*", r"<strong>\1</strong>", html.escape(m.group(2)))
            out.append(f"<li>{box}{item}</li>")
        elif s:
            if in_list:
                out.append("</ul>")
                in_list = False
            out.append(f"<p>{text}</p>")
    if in_list:
        out.append("</ul>")
    return "\n".join(out)


def to_txt(md):
    t = re.sub(r"^#{1,6}\s+", "", md, flags=re.M)
    t = re.sub(r"\*\*(.+?)\*\*", r"\1", t)
    t = re.sub(r"`(.+?)`", r"\1", t)
    return re.sub(r"^(\s*)[-*]\s+\[[ xX]\]\s+", r"\1[ ] ", t, flags=re.M)


def render(item):
    parent_line = f"**Parent:** {item['parent_text']}\n" if item["parent_text"] else ""
    head = f"## {item['title']}\n\n**Type:** {item['type']}\n{parent_line}\n"
    return head + item["body"] + "\n"


def cmd_export(args):
    items = ordered(parse(args.files))
    out = pathlib.Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    ext = {"md": "md", "txt": "txt", "html": "html"}[args.format]
    docs = [(f"{n + 1:02d}-{it['id']}", render(it)) for n, it in enumerate(items)] if args.split \
        else [("backlog", "\n".join(render(it) for it in items))]
    for name, md in docs:
        if args.format == "html":
            body = to_html(md)
            text = f"<!doctype html>\n<html><head><meta charset=\"utf-8\"><title>{html.escape(name)}</title></head>\n<body>\n{body}\n</body></html>\n"
        else:
            text = md if args.format == "md" else to_txt(md)
        (out / f"{name}.{ext}").write_text(text, encoding="utf-8")
        print(f"MADE {out / (name + '.' + ext)}")
    return 0


def main():
    p = argparse.ArgumentParser()
    sub = p.add_subparsers(dest="cmd", required=True)
    t = sub.add_parser("tree"); t.add_argument("files", nargs="+"); t.add_argument("--json", action="store_true")
    s = sub.add_parser("similar"); s.add_argument("files", nargs="+"); s.add_argument("--candidates", required=True)
    s.add_argument("--min", type=float, default=MIN_SCORE)
    lg = sub.add_parser("log"); lg.add_argument("action", choices=["init", "set", "status"]); lg.add_argument("log")
    lg.add_argument("rest", nargs="*"); lg.add_argument("--system", default="files"); lg.add_argument("--url")
    lg.add_argument("--status", default="created", choices=["created", "linked", "skipped", "updated"])
    e = sub.add_parser("export"); e.add_argument("files", nargs="+"); e.add_argument("--format", required=True, choices=["md", "txt", "html"])
    g = e.add_mutually_exclusive_group(required=True); g.add_argument("--split", action="store_true"); g.add_argument("--combined", action="store_true")
    e.add_argument("--out", required=True)
    args = p.parse_args()
    for f in getattr(args, "files", []):
        if not pathlib.Path(f).is_file():
            print(f"{f}: file not found")
            return 1
    if args.cmd == "log":
        if args.action == "init":
            args.files = args.rest
        elif args.action == "set":
            if len(args.rest) != 2:
                print("log set needs <item id> <remote id>")
                return 1
            args.item, args.remote = args.rest
    return {"tree": cmd_tree, "similar": cmd_similar, "log": cmd_log, "export": cmd_export}[args.cmd](args)


if __name__ == "__main__":
    sys.exit(main())
