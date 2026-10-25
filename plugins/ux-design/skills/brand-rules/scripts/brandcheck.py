#!/usr/bin/env python3
"""Check and convert design tokens in the DTCG format (Design Tokens Format Module 2025.10).

    python3 brandcheck.py check   <tokens.json> [--theme dark=<file>] [--json]
    python3 brandcheck.py from-css <tokens.css> --out <tokens.json> [--prefix fl]
    python3 brandcheck.py to-css  <tokens.json> [--theme dark=<file>] [--prefix fl] --out <tokens.css>
    python3 brandcheck.py to-cd   <tokens.json> [--theme dark=<file>] --name <system> --out <tokens.json>
    python3 brandcheck.py copy    --product PRODUCT.md <file>... [--json]

check    Validates the tokens: names, $type (on the token or its group), value shapes, aliases
         (no missing target, no loop), a $description on each token, and the contrast pairs.
         A contrast pair is a color token with
         "$extensions": {"org.maikelsoft.contrast": {"on": ["{color.surface}"], "min": 4.5}}.
         Each pair is checked in each theme.
from-css Makes a first DTCG file from CSS custom properties (--<prefix>-<group>-<name>). The
         $type comes from the value. A person must check each $type and add each $description.
to-css   Writes the tokens as CSS custom properties. A theme file writes [data-theme="<id>"].
to-cd    Writes the tokens.json of a Claude Design System: each family is a list of
         {"name", "value", "usage"}. Values that the page cannot read are errors.
copy     Finds the words of the "Words" table of PRODUCT.md (column "Do not use") in the text
         of HTML and Markdown files. It also finds the plural with -s or -es.

A theme file has the same paths as the base file. It holds only the values that change.
Exit code: 0 when there is no error, 1 when there are errors, 2 for a usage error.
"""
import argparse
import json
import re
import sys
from html.parser import HTMLParser
from pathlib import Path

TYPES = {"color", "dimension", "fontFamily", "fontWeight", "duration", "cubicBezier", "number",
         "strokeStyle", "border", "transition", "shadow", "gradient", "typography"}
WEIGHTS = {"thin": 100, "hairline": 100, "extra-light": 200, "ultra-light": 200, "light": 300,
           "normal": 400, "regular": 400, "book": 400, "medium": 500, "semi-bold": 600,
           "demi-bold": 600, "bold": 700, "extra-bold": 800, "ultra-bold": 800, "black": 900,
           "heavy": 900, "extra-black": 950, "ultra-black": 950}
ALIAS = re.compile(r"^\{([^{}]+)\}$")
CD_NAME = re.compile(r"^[A-Za-z0-9][A-Za-z0-9_.-]{0,63}$")
EXT = "org.maikelsoft.contrast"


class Report:
    def __init__(self):
        self.items = []

    def error(self, rule, msg):
        self.items.append({"level": "error", "rule": rule, "message": msg})

    def warn(self, rule, msg):
        self.items.append({"level": "warning", "rule": rule, "message": msg})

    def errors(self):
        return sum(1 for i in self.items if i["level"] == "error")

    def finish(self, as_json, extra=None):
        if as_json:
            print(json.dumps({"errors": self.errors(), "items": self.items, **(extra or {})}, indent=2))
        else:
            for i in self.items:
                print(f'{i["level"].upper():7} {i["rule"]}: {i["message"]}')
            w = len(self.items) - self.errors()
            print(f"{self.errors()} errors, {w} warnings")
        return 1 if self.errors() else 0


# ---------- reading ----------

def flatten(doc, report=None):
    """Return {path: {"type", "value", "description", "extensions"}} with the $type of the groups."""
    out = {}

    def walk(node, path, inherited):
        if not isinstance(node, dict):
            return
        gtype = node.get("$type", inherited)
        if "$value" in node:
            out[".".join(path)] = {"type": node.get("$type", inherited), "value": node["$value"],
                                   "description": node.get("$description", ""),
                                   "extensions": node.get("$extensions", {}),
                                   "deprecated": node.get("$deprecated", False)}
            return
        for key, child in node.items():
            if key.startswith("$") and key != "$root":
                continue
            if report and key != "$root" and re.search(r"[{}.]", key):
                report.error("TK-NAME", f'{".".join(path + [key])}: a name must not contain "{{", "}}", or ".".')
            walk(child, path + [key], gtype)

    walk(doc, [], None)
    return out


def load(path):
    return json.loads(Path(path).read_text(encoding="utf-8"))


def themes_arg(values):
    out = []
    for v in values or []:
        if "=" not in v:
            raise SystemExit(f"--theme needs <id>=<file>, not {v}")
        tid, f = v.split("=", 1)
        out.append((tid, f))
    return out


def resolve(tokens, path, seen=()):
    """Return the final token for a path, following aliases. Raise ValueError on a loop or a gap."""
    if path in seen:
        raise ValueError("loop: " + " -> ".join(seen + (path,)))
    if path not in tokens:
        raise ValueError(f"missing token {{{path}}}")
    t = tokens[path]
    m = ALIAS.match(t["value"]) if isinstance(t["value"], str) else None
    if m:
        return resolve(tokens, m.group(1), seen + (path,))
    return t


def token_type(tokens, path):
    t = tokens[path]
    if t["type"]:
        return t["type"]
    try:
        return resolve(tokens, path)["type"]
    except ValueError:
        return None


# ---------- values ----------

def color_css(v):
    """Return a CSS color string for a DTCG color value, or None."""
    if isinstance(v, str):
        return v if re.fullmatch(r"#[0-9a-fA-F]{3,8}", v) else None
    if not isinstance(v, dict):
        return None
    if v.get("hex") and v.get("alpha", 1) == 1:
        return v["hex"].lower()
    space, comp, alpha = v.get("colorSpace"), v.get("components"), v.get("alpha", 1)
    if not isinstance(comp, list) or len(comp) != 3:
        return None
    comp = [0 if c == "none" else c for c in comp]
    if space == "srgb":
        rgb = [round(max(0, min(1, c)) * 255) for c in comp]
        if alpha == 1:
            return "#" + "".join(f"{c:02x}" for c in rgb)
        return f"rgba({rgb[0]}, {rgb[1]}, {rgb[2]}, {alpha})"
    if space in ("oklch", "oklab", "lch", "lab"):
        a = "" if alpha == 1 else f" / {alpha}"
        return f"{space}({comp[0]} {comp[1]} {comp[2]}{a})"
    if space == "hsl":
        a = "" if alpha == 1 else f" / {alpha}"
        return f"hsl({comp[0]} {comp[1]}% {comp[2]}%{a})"
    return None


def rgb_of(css):
    m = re.fullmatch(r"#([0-9a-f]{3}|[0-9a-f]{6})", css or "")
    if m:
        h = m.group(1)
        h = "".join(c * 2 for c in h) if len(h) == 3 else h
        return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))
    m = re.fullmatch(r"rgba?\((\d+),\s*(\d+),\s*(\d+)(?:,\s*([\d.]+))?\)", css or "")
    if m and (m.group(4) is None or float(m.group(4)) >= 1):
        return tuple(int(m.group(i)) for i in (1, 2, 3))
    return None


def contrast(a, b):
    def lum(c):
        out = []
        for x in c:
            x /= 255
            out.append(x / 12.92 if x <= 0.03928 else ((x + 0.055) / 1.055) ** 2.4)
        return 0.2126 * out[0] + 0.7152 * out[1] + 0.0722 * out[2]
    la, lb = lum(a), lum(b)
    return (max(la, lb) + 0.05) / (min(la, lb) + 0.05)


def dim_css(v):
    if isinstance(v, dict) and isinstance(v.get("value"), (int, float)) and v.get("unit") in ("px", "rem"):
        n = v["value"]
        n = int(n) if float(n).is_integer() else n
        return f"{n}{v['unit']}"
    if isinstance(v, str) and re.fullmatch(r"-?[\d.]+(px|rem)", v):
        return v
    return None


def family_css(v):
    if isinstance(v, str):
        v = [v]
    if not isinstance(v, list) or not v:
        return None
    generic = {"serif", "sans-serif", "monospace", "system-ui", "ui-monospace", "ui-sans-serif", "ui-serif", "cursive"}
    return ", ".join(f if f in generic or re.fullmatch(r"[\w-]+", f) else f'"{f}"' for f in v)


def weight_num(v):
    if isinstance(v, (int, float)) and 1 <= v <= 1000:
        return int(v)
    if isinstance(v, str) and v in WEIGHTS:
        return WEIGHTS[v]
    return None


def check_value(ttype, v, path, r):
    if isinstance(v, str) and ALIAS.match(v):
        return
    if ttype == "color" and color_css(v) is None:
        r.error("TK-VALUE", f"{path}: not a color in a supported color space (srgb, hsl, oklch, oklab, lch, lab, or a hex).")
    elif ttype == "dimension" and dim_css(v) is None:
        r.error("TK-VALUE", f'{path}: a dimension is {{"value": <number>, "unit": "px" | "rem"}}.')
    elif ttype == "fontFamily" and family_css(v) is None:
        r.error("TK-VALUE", f"{path}: a fontFamily is a string or a list of strings.")
    elif ttype == "fontWeight" and weight_num(v) is None:
        r.error("TK-VALUE", f"{path}: a fontWeight is a number from 1 to 1000 or a named weight.")
    elif ttype == "number" and not isinstance(v, (int, float)):
        r.error("TK-VALUE", f"{path}: a number token needs a JSON number.")
    elif ttype == "duration" and not (isinstance(v, dict) and v.get("unit") in ("ms", "s")):
        r.error("TK-VALUE", f'{path}: a duration is {{"value": <number>, "unit": "ms" | "s"}}.')
    elif ttype == "typography" and not (isinstance(v, dict) and "fontSize" in v and "fontFamily" in v):
        r.error("TK-VALUE", f"{path}: a typography token needs at least fontFamily and fontSize.")


# ---------- check ----------

def merged(base, theme_doc):
    """Return the tokens of the base with the values of the theme file."""
    out = {k: dict(v) for k, v in base.items()}
    for k, v in flatten(theme_doc).items():
        if k in out:
            out[k]["value"] = v["value"]
    return out


def color_value(tokens, path):
    try:
        t = resolve(tokens, path)
    except ValueError:
        return None
    return color_css(t["value"])


def cmd_check(args):
    r = Report()
    doc = load(args.tokens)
    tokens = flatten(doc, r)
    if not tokens:
        r.error("TK-EMPTY", "No tokens found. A token is an object with $value.")
    for path, t in tokens.items():
        ttype = token_type(tokens, path)
        if ttype is None:
            r.error("TK-TYPE", f"{path}: no $type on the token or its groups.")
        elif ttype not in TYPES:
            r.error("TK-TYPE", f"{path}: unknown $type {ttype}.")
        if isinstance(t["value"], str) and ALIAS.match(t["value"]):
            try:
                target = resolve(tokens, path)
                if t["type"] and target["type"] and t["type"] != target["type"]:
                    r.error("TK-ALIAS", f'{path}: $type {t["type"]} but the alias points to a {target["type"]}.')
            except ValueError as e:
                r.error("TK-ALIAS", f"{path}: {e}.")
        elif ttype:
            check_value(ttype, t["value"], path, r)
        if not t["description"] and not t["deprecated"]:
            r.warn("TK-USAGE", f"{path}: no $description. Say where to use the token.")
    themes = [("base", tokens)]
    for tid, f in themes_arg(args.theme):
        tdoc = load(f)
        extra = set(flatten(tdoc)) - set(tokens)
        for p in sorted(extra):
            r.error("TK-THEME", f"{tid}: {p} is not in the base file.")
        themes.append((tid, merged(tokens, tdoc)))
    pairs = 0
    for path, t in tokens.items():
        rule = t["extensions"].get(EXT) if isinstance(t["extensions"], dict) else None
        if not rule:
            continue
        ons = rule.get("on", [])
        ons = [ons] if isinstance(ons, str) else ons
        need = float(rule.get("min", 4.5))
        for on in ons:
            m = ALIAS.match(on)
            if not m:
                r.error("TK-PAIR", f'{path}: "on" must be an alias like "{{color.surface}}".')
                continue
            for tid, tk in themes:
                fg, bg = color_value(tk, path), color_value(tk, m.group(1))
                a, b = rgb_of(fg), rgb_of(bg)
                if a is None or b is None:
                    r.warn("TK-PAIR", f"{tid}: cannot compute {path} on {m.group(1)} (not an opaque sRGB color).")
                    continue
                pairs += 1
                c = contrast(a, b)
                if c < need:
                    r.error("TK-CONTRAST", f"{tid}: {path} on {m.group(1)} is {c:.2f}:1 (needs {need}:1).")
    return r.finish(args.json, {"tokens": len(tokens), "pairs_checked": pairs})


# ---------- from-css ----------

def parse_shadow(raw):
    """Parse a CSS box-shadow into DTCG shadow objects. Return None when a part does not fit."""
    parts, depth, cur = [], 0, ""
    for ch in raw:
        depth += ch == "("
        depth -= ch == ")"
        if ch == "," and depth == 0:
            parts.append(cur)
            cur = ""
        else:
            cur += ch
    parts.append(cur)
    out = []
    for part in parts:
        m = re.fullmatch(r"\s*(inset\s+)?((?:-?[\d.]+(?:px)?\s+){2,3}(?:-?[\d.]+(?:px)?\s+)?)(#[0-9a-fA-F]{3,6}|rgba?\([^)]*\))\s*", part)
        if not m:
            return None
        nums = [float(x.replace("px", "")) for x in m.group(2).split()]
        nums += [0] * (4 - len(nums))
        cm = re.fullmatch(r"rgba?\(\s*(\d+)[,\s]+(\d+)[,\s]+(\d+)(?:[,\s/]+([\d.]+))?\s*\)", m.group(3))
        if cm:
            color = {"colorSpace": "srgb", "components": [round(int(cm.group(i)) / 255, 4) for i in (1, 2, 3)],
                     "alpha": float(cm.group(4)) if cm.group(4) else 1}
        else:
            h = m.group(3).lower()
            h = "#" + "".join(c * 2 for c in h[1:]) if len(h) == 4 else h
            color = {"colorSpace": "srgb", "components": [round(int(h[i:i + 2], 16) / 255, 4) for i in (1, 3, 5)], "hex": h}

        def d(n):
            return {"value": int(n) if float(n).is_integer() else n, "unit": "px"}
        sh = {"color": color, "offsetX": d(nums[0]), "offsetY": d(nums[1]), "blur": d(nums[2]), "spread": d(nums[3])}
        if m.group(1):
            sh["inset"] = True
        out.append(sh)
    return out if len(out) > 1 else out[0]


def cmd_from_css(args):
    css = re.sub(r"/\*.*?\*/", "", Path(args.css).read_text(encoding="utf-8"), flags=re.S)
    root = re.search(r":root\s*\{([^}]*)\}", css)
    props = re.findall(r"--([\w-]+)\s*:\s*([^;]+);", root.group(1) if root else css)
    if not props:
        print("No custom properties found.", file=sys.stderr)
        return 1
    names = [n for n, _ in props]
    prefix = args.prefix
    if prefix is None:
        first = {n.split("-")[0] for n in names}
        prefix = first.pop() if len(first) == 1 and len(names) > 1 else ""
    lookup = {n: v.strip() for n, v in props}
    doc, skipped = {}, []
    for name, raw in props:
        rest = name[len(prefix) + 1:] if prefix and name.startswith(prefix + "-") else name
        group, _, leaf = rest.partition("-")
        if not leaf:
            leaf = "$root"
        raw = raw.strip()
        m = re.fullmatch(r"var\(\s*--([\w-]+)\s*\)", raw)
        if m:
            target = m.group(1)
            trest = target[len(prefix) + 1:] if prefix and target.startswith(prefix + "-") else target
            tg, _, tl = trest.partition("-")
            node = {"$value": "{" + f"{tg}.{tl or '$root'}" + "}"}
            ttype = None
            raw2 = lookup.get(target, "")
        else:
            node, ttype = {}, None
            raw2 = raw
        val = None
        if re.fullmatch(r"#[0-9a-fA-F]{3}|#[0-9a-fA-F]{6}", raw2):
            ttype = "color"
            h = raw2.lower()
            h = "#" + "".join(c * 2 for c in h[1:]) if len(h) == 4 else h
            val = {"colorSpace": "srgb", "components": [round(int(h[i:i + 2], 16) / 255, 4) for i in (1, 3, 5)], "hex": h}
        elif re.fullmatch(r"-?[\d.]+(px|rem)", raw2):
            ttype = "dimension"
            num = float(re.match(r"-?[\d.]+", raw2).group(0))
            val = {"value": int(num) if num.is_integer() else num, "unit": re.search(r"(px|rem)$", raw2).group(1)}
        elif re.fullmatch(r"\d{3}", raw2) and re.search(r"weight", name):
            ttype, val = "fontWeight", int(raw2)
        elif re.fullmatch(r"[\d.]+", raw2):
            ttype, val = "number", float(raw2) if "." in raw2 else int(raw2)
        elif re.search(r"shadow", name) and parse_shadow(raw2):
            ttype, val = "shadow", parse_shadow(raw2)
        elif re.search(r"font", name) and "," in raw2:
            ttype = "fontFamily"
            val = [f.strip().strip('"\'') for f in raw2.split(",")]
        else:
            skipped.append(f"--{name}: {raw}")
            continue
        node.setdefault("$value", val)
        node["$type"] = ttype
        node["$description"] = ""
        doc.setdefault(group, {})[leaf] = node
    Path(args.out).write_text(json.dumps(doc, indent=2) + "\n", encoding="utf-8")
    print(f"Wrote {args.out}: {len(props) - len(skipped)} tokens. Add a $description to each token.")
    for s in skipped:
        print(f"SKIPPED {s} (no DTCG type for this value; add it by hand)")
    return 0


# ---------- to-css ----------

def css_value(tokens, path, prefix):
    t = tokens[path]
    v = t["value"]
    if isinstance(v, str):
        m = ALIAS.match(v)
        if m:
            return f"var(--{css_name(m.group(1), prefix)})"
    ttype = token_type(tokens, path)
    if ttype == "color":
        return color_css(v)
    if ttype == "dimension":
        return dim_css(v)
    if ttype == "fontFamily":
        return family_css(v)
    if ttype == "fontWeight":
        return str(weight_num(v))
    if ttype == "number":
        return str(v)
    if ttype == "duration":
        return f'{v["value"]}{v["unit"]}'
    if ttype == "shadow":
        items = v if isinstance(v, list) else [v]
        parts = []
        for s in items:
            parts.append(" ".join(filter(None, ["inset" if s.get("inset") else "", dim_css(s.get("offsetX")),
                                                dim_css(s.get("offsetY")), dim_css(s.get("blur")),
                                                dim_css(s.get("spread")), color_css(s.get("color"))])))
        return ", ".join(parts)
    return None


def css_name(path, prefix):
    name = path.replace(".$root", "").replace(".", "-")
    return f"{prefix}-{name}" if prefix else name


def cmd_to_css(args):
    tokens = flatten(load(args.tokens))
    lines = ["/* Generated from " + Path(args.tokens).name + " by brandcheck.py. Do not edit. */", ":root {"]
    bad = 0
    for path in tokens:
        if token_type(tokens, path) == "typography":
            continue
        v = css_value(tokens, path, args.prefix)
        if v is None:
            print(f"SKIPPED {path}", file=sys.stderr)
            bad += 1
            continue
        lines.append(f"  --{css_name(path, args.prefix)}: {v};")
    lines.append("}")
    for tid, f in themes_arg(args.theme):
        theme = merged(tokens, load(f))
        lines.append(f'[data-theme="{tid}"] {{')
        for path in flatten(load(f)):
            v = css_value(theme, path, args.prefix)
            if v:
                lines.append(f"  --{css_name(path, args.prefix)}: {v};")
        lines.append("}")
    Path(args.out).write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"Wrote {args.out}.")
    return 1 if bad else 0


# ---------- to-cd ----------

def cd_name(path):
    return path.replace(".$root", "").replace(".", "-")


def family_of(path, ttype):
    head = path.split(".")[0].lower()
    if ttype == "color":
        return "color"
    if ttype == "shadow":
        return "shadow"
    if ttype == "dimension":
        if re.search(r"radius|radii|round|corner", head):
            return "radius"
        if re.search(r"space|spacing|gap|gutter", head):
            return "spacing"
        if re.search(r"size|font|text|type", head):
            return "size"
        return "size"
    return None


def cmd_to_cd(args):
    r = Report()
    tokens = flatten(load(args.tokens))
    theme_list = [("light" if not args.base_id else args.base_id, tokens)]
    for tid, f in themes_arg(args.theme):
        theme_list.append((tid, merged(tokens, load(f))))
    out = {"name": args.name, "version": 1, "meta": {"source": f"DTCG {Path(args.tokens).name} via brandcheck.py to-cd"}}
    color_names = {p for p in tokens if token_type(tokens, p) == "color"}
    used = set()
    fams = {}
    families, sizes, weights, styles = {}, {}, {}, []
    for path, t in tokens.items():
        ttype = token_type(tokens, path)
        name = cd_name(path)
        usage = t["description"]
        if ttype in ("fontFamily",):
            try:
                families[name] = family_css(resolve(tokens, path)["value"])
            except ValueError as e:
                r.error("CD-ALIAS", f"{path}: {e}.")
            continue
        if ttype == "typography":
            try:
                v = resolve(tokens, path)["value"]
            except ValueError as e:
                r.error("CD-ALIAS", f"{path}: {e}.")
                continue
            styles.append((path, v, usage))
            continue
        if ttype in ("fontWeight", "number", "duration", "cubicBezier"):
            if ttype == "fontWeight":
                weights[name] = t
            continue
        fam = family_of(path, ttype)
        if fam is None:
            r.warn("CD-SKIP", f"{path}: $type {ttype} has no family in Claude Design. Not written.")
            continue
        if not CD_NAME.match(name):
            r.error("CD-NAME", f"{path}: the name {name} does not fit [A-Za-z0-9][A-Za-z0-9_.-]{{0,63}}.")
            continue
        if name in used:
            r.error("CD-DUP", f"{path}: the name {name} is used two times. The page drops the second.")
            continue
        used.add(name)
        if not usage:
            r.error("CD-USAGE", f"{path}: no $description. Each token needs a usage note.")
        if fam == "color":
            vals = {}
            for tid, tk in theme_list:
                tv = tk[path]["value"]
                m = ALIAS.match(tv) if isinstance(tv, str) else None
                if m and m.group(1) in color_names:
                    vals[tid] = "{" + cd_name(m.group(1)) + "}"
                    continue
                c = color_value(tk, path)
                if c is None:
                    r.error("CD-VALUE", f"{tid}: {path} has no color value that the page can read.")
                else:
                    vals[tid] = c
            if len(set(vals.values())) == 1 and len(theme_list) == 1:
                value = next(iter(vals.values()))
            else:
                value = vals
            fams.setdefault("color", []).append({"name": name, "value": value, "usage": usage})
        else:
            try:
                tv = resolve(tokens, path)
            except ValueError as e:
                r.error("CD-ALIAS", f"{path}: {e}.")
                continue
            v = css_value({path: tv, **tokens}, path, "") if fam == "shadow" else dim_css(tv["value"])
            if v is None or "var(" in v:
                r.error("CD-VALUE", f"{path}: the page cannot read this value.")
                continue
            fams.setdefault(fam, []).append({"name": name, "value": v, "usage": usage})
    if "color" in fams:
        out["color"] = {"themes": [{"id": tid, "name": tid.capitalize()} for tid, _ in theme_list], "tokens": fams.pop("color")}
    type_out = {"fonts": [], "families": {}, "groups": []}
    fam_key = {}
    for name, css in families.items():
        key = name.split("-")[-1] if name.split("-")[-1] not in type_out["families"] else name
        type_out["families"][key] = css
        fam_key[css] = key
    group_styles = {}
    for path, v, usage in styles:
        fsize = dim_css(resolve_inline(tokens, v.get("fontSize")))
        fam_v = resolve_inline(tokens, v.get("fontFamily"))
        fam_css = family_css(fam_v)
        key = fam_key.get(fam_css)
        if key is None and fam_css:
            key = f"family{len(type_out['families']) + 1}"
            type_out["families"][key] = fam_css
            fam_key[fam_css] = key
        style = {"name": cd_name(path.split(".", 1)[-1] if "." in path else path), "fontSize": fsize}
        lh = resolve_inline(tokens, v.get("lineHeight"))
        if lh is not None:
            style["lineHeight"] = lh if isinstance(lh, (int, float)) else dim_css(lh)
        fw = weight_num(resolve_inline(tokens, v.get("fontWeight")))
        if fw:
            style["fontWeight"] = fw
        if not fsize:
            r.error("CD-VALUE", f"{path}: fontSize is not a px or rem dimension.")
            continue
        group = path.split(".")[1].capitalize() if path.count(".") >= 2 else "Text"
        group_styles.setdefault((group, key), []).append(style)
    for (group, key), sts in group_styles.items():
        type_out["groups"].append({"name": group, "family": key, "styles": sts})
    if type_out["families"] or type_out["groups"]:
        out["type"] = type_out
    for fam, lst in fams.items():
        out[fam] = {"tokens": lst}
    if not styles and families:
        r.warn("CD-TYPE", "No typography tokens. The type ramp of the system stays empty.")
    Path(args.out).write_text(json.dumps(out, indent=2) + "\n", encoding="utf-8")
    counts = {k: len(v["tokens"]) for k, v in out.items() if isinstance(v, dict) and "tokens" in v}
    return r.finish(args.json, {"out": args.out, "families": counts})


def resolve_inline(tokens, v):
    if isinstance(v, str):
        m = ALIAS.match(v)
        if m:
            try:
                return resolve(tokens, m.group(1))["value"]
            except ValueError:
                return None
    return v


# ---------- copy ----------

class TextOnly(HTMLParser):
    def __init__(self):
        super().__init__()
        self.parts, self.skip = [], 0

    def handle_starttag(self, tag, attrs):
        if tag in ("script", "style"):
            self.skip += 1
        for k, v in attrs:
            if k in ("aria-label", "placeholder", "title", "alt") and v:
                self.parts.append(v)

    def handle_endtag(self, tag):
        if tag in ("script", "style") and self.skip:
            self.skip -= 1

    def handle_data(self, data):
        if not self.skip:
            self.parts.append(data)


def banned_words(product):
    text = Path(product).read_text(encoding="utf-8")
    m = re.search(r"^##+\s*Words\b.*?$(.*?)(?=^##\s|\Z)", text, re.S | re.M | re.I)
    if not m:
        return []
    rows = [l for l in m.group(1).splitlines() if l.strip().startswith("|")]
    if len(rows) < 3:
        return []
    head = [c.strip().lower() for c in rows[0].strip("|").split("|")]
    try:
        col = next(i for i, c in enumerate(head) if "not use" in c or "avoid" in c or "niet" in c)
    except StopIteration:
        return []
    use_col = next((i for i, c in enumerate(head) if i != col and ("use" in c or "gebruik" in c)), None)
    out = []
    for row in rows[2:]:
        cells = [c.strip() for c in row.strip("|").split("|")]
        if col < len(cells):
            for w in re.split(r",\s*", cells[col]):
                w = w.strip("`* ")
                if w:
                    out.append((w, cells[use_col] if use_col is not None and use_col < len(cells) else ""))
    return out


def cmd_copy(args):
    r = Report()
    words = banned_words(args.product)
    if not words:
        r.error("CP-WORDS", f'No "Words" table with a "Do not use" column in {args.product}.')
        return r.finish(args.json)
    hits = 0
    for f in args.files:
        raw = Path(f).read_text(encoding="utf-8", errors="replace")
        if f.endswith((".html", ".htm")):
            p = TextOnly()
            p.feed(raw)
            text = " ".join(p.parts)
        else:
            text = re.sub(r"`[^`]*`", " ", raw)
        for w, use in words:
            for m in re.finditer(rf"(?<![\w-]){re.escape(w)}(?:s|es)?(?![\w-])", text, re.I):
                hits += 1
                ctx = " ".join(text[max(0, m.start() - 30):m.end() + 30].split())
                r.error("CP-BANNED", f'{f}: "{m.group(0)}" ({"use: " + use if use else "not allowed"}) in "...{ctx}..."')
    return r.finish(args.json, {"words": len(words), "hits": hits})


def main(argv=None):
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = p.add_subparsers(dest="cmd", required=True)
    c = sub.add_parser("check")
    c.add_argument("tokens")
    c.add_argument("--theme", action="append")
    c.add_argument("--json", action="store_true")
    c.set_defaults(fn=cmd_check)
    c = sub.add_parser("from-css")
    c.add_argument("css")
    c.add_argument("--out", required=True)
    c.add_argument("--prefix")
    c.set_defaults(fn=cmd_from_css)
    c = sub.add_parser("to-css")
    c.add_argument("tokens")
    c.add_argument("--theme", action="append")
    c.add_argument("--prefix", default="")
    c.add_argument("--out", required=True)
    c.set_defaults(fn=cmd_to_css)
    c = sub.add_parser("to-cd")
    c.add_argument("tokens")
    c.add_argument("--theme", action="append")
    c.add_argument("--base-id", default="light")
    c.add_argument("--name", required=True)
    c.add_argument("--out", required=True)
    c.add_argument("--json", action="store_true")
    c.set_defaults(fn=cmd_to_cd)
    c = sub.add_parser("copy")
    c.add_argument("--product", required=True)
    c.add_argument("files", nargs="+")
    c.add_argument("--json", action="store_true")
    c.set_defaults(fn=cmd_copy)
    args = p.parse_args(argv)
    return args.fn(args)


if __name__ == "__main__":
    sys.exit(main())
