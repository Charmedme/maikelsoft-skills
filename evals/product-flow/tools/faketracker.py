#!/usr/bin/env python3
"""A fake ticket system for tests. State and a call log live in one folder.

    faketracker.py <state dir> init ado|jira|jira-goal
    faketracker.py <state dir> fields <type>
    faketracker.py <state dir> info
    faketracker.py <state dir> search "<words>"
    faketracker.py <state dir> get <id>
    faketracker.py <state dir> create --type <type> --title <title> [--body-file f | --body text] [--ac-file f] [--parent <id>] [--labels a,b] [--field name=value ...]
    faketracker.py <state dir> link <child id> <parent id>
"""
import argparse, json, pathlib, re, sys

SEED = {
    "ado": {"system": "Azure DevOps", "project": "OfficeShop", "process": "Agile",
            "levels": [{"name": "Epics", "types": ["Epic"]}, {"name": "Features", "types": ["Feature"]},
                       {"name": "Stories", "types": ["User Story"]}, {"name": "Tasks", "types": ["Task", "Bug"]}],
            "text_format": "HTML for System.Description and Microsoft.VSTS.Common.AcceptanceCriteria",
            "items": [{"id": "1201", "type": "Feature", "title": "Product catalogue", "parent": None},
                      {"id": "1202", "type": "User Story", "title": "Find a product by article number", "parent": "1201"},
                      {"id": "1203", "type": "User Story", "title": "Export the order list to Excel", "parent": None}]},
    "jira": {"system": "Jira", "project": "SHOP", "plan": "Standard",
             "levels": [{"level": 1, "types": ["Epic"]}, {"level": 0, "types": ["Story", "Task", "Bug"]},
                        {"level": -1, "types": ["Subtask"]}],
             "text_format": "Markdown is accepted by this tool; acceptance criteria: no custom field, use the description",
             "items": [{"id": "SHOP-7", "type": "Story", "title": "Manage delivery addresses in my account", "parent": None},
                       {"id": "SHOP-9", "type": "Epic", "title": "Invoices online", "parent": None}]},
}
SEED["jira-goal"] = json.loads(json.dumps(SEED["jira"]))
FIELDS = {
    "ado": {"Feature": [{"name": "System.Title", "kind": "string"}, {"name": "System.Description", "kind": "html"},
                        {"name": "Microsoft.VSTS.Common.AcceptanceCriteria", "kind": "html"},
                        {"name": "Microsoft.VSTS.Common.BusinessValue", "label": "Business Value", "kind": "integer"},
                        {"name": "Microsoft.VSTS.Common.ValueArea", "label": "Value Area", "kind": "picklist",
                         "values": ["Business", "Architectural"]}]},
    "jira": {"Epic": [{"name": "summary", "kind": "string"}, {"name": "description", "kind": "text"}]},
    "jira-goal": {"Epic": [{"name": "summary", "kind": "string"}, {"name": "description", "kind": "text"},
                           {"name": "customfield_10050", "label": "Goal", "kind": "text"},
                           {"name": "customfield_10051", "label": "Success metric", "kind": "text"}]},
}


def load(d):
    return json.loads((d / "state.json").read_text())


def save(d, s):
    (d / "state.json").write_text(json.dumps(s, indent=2))


def log(d, line):
    with open(d / "calls.log", "a") as f:
        f.write(line + "\n")


def words(t):
    return {w.rstrip("s") for w in re.findall(r"[a-z0-9]+", t.lower()) if len(w) > 2}


def main():
    d = pathlib.Path(sys.argv[1]); cmd = sys.argv[2]; rest = sys.argv[3:]
    if cmd == "init":
        d.mkdir(parents=True, exist_ok=True); s = SEED[rest[0]]; s = json.loads(json.dumps(s)); s["next"] = 1300
        s["fields"] = FIELDS[rest[0]]; save(d, s); return 0
    s = load(d)
    log(d, " ".join([cmd] + rest))
    if cmd == "info":
        print(json.dumps({k: s[k] for k in s if k not in ("items", "next", "fields")}, indent=2))
    elif cmd == "fields":
        base = [{"name": "title", "kind": "string"}, {"name": "body", "kind": "text"}]
        print(json.dumps(s.get("fields", {}).get(rest[0], base), indent=2))
    elif cmd == "search":
        q = words(" ".join(rest))
        hits = [i for i in s["items"] if q & words(i["title"])]
        print(json.dumps([{"id": i["id"], "type": i["type"], "title": i["title"]} for i in hits], indent=2))
    elif cmd == "get":
        print(json.dumps(next((i for i in s["items"] if i["id"] == rest[0]), {"error": "not found"}), indent=2))
    elif cmd == "create":
        p = argparse.ArgumentParser(); p.add_argument("--type", required=True); p.add_argument("--title", required=True)
        p.add_argument("--body"); p.add_argument("--body-file"); p.add_argument("--ac-file"); p.add_argument("--parent"); p.add_argument("--labels")
        p.add_argument("--field", action="append", default=[])
        a = p.parse_args(rest)
        known = {f["name"]: f for f in s.get("fields", {}).get(a.type, [])}
        fields = {}
        for kv in a.field:
            name, _, value = kv.partition("=")
            if name not in known:
                print(json.dumps({"error": f"field '{name}' does not exist on {a.type}"})); return 1
            if known[name]["kind"] == "integer" and not value.strip().isdigit():
                print(json.dumps({"error": f"field '{name}' takes a number"})); return 1
            if known[name]["kind"] == "picklist" and value not in known[name]["values"]:
                print(json.dumps({"error": f"field '{name}' takes one of {known[name]['values']}"})); return 1
            fields[name] = value
        types = [t for lv in s["levels"] for t in lv["types"]]
        if a.type not in types:
            print(json.dumps({"error": f"type '{a.type}' does not exist. Types: {types}"})); return 1
        if a.parent and not any(i["id"] == a.parent for i in s["items"]):
            print(json.dumps({"error": f"parent {a.parent} not found"})); return 1
        body = a.body or (pathlib.Path(a.body_file).read_text() if a.body_file else "")
        prefix = "SHOP-" if s["system"] == "Jira" else ""
        iid = f"{prefix}{s['next']}"; s["next"] += 1
        s["items"].append({"id": iid, "type": a.type, "title": a.title, "parent": a.parent, "labels": a.labels,
                           "body": body, "ac": pathlib.Path(a.ac_file).read_text() if a.ac_file else None, "fields": fields, "new": True})
        save(d, s); print(json.dumps({"id": iid, "url": f"https://tracker.example/{iid}"}))
    elif cmd == "link":
        for i in s["items"]:
            if i["id"] == rest[0]:
                i["parent"] = rest[1]
        save(d, s); print(json.dumps({"ok": True}))
    return 0


if __name__ == "__main__":
    sys.exit(main())
