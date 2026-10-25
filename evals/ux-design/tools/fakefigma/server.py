#!/usr/bin/env python3
"""A fake Figma MCP server for the figma skill test. It speaks MCP over stdio (JSON-RPC, one
message per line), offers the read tools of the Figma server with recorded data, and logs each call
to the file in FAKEFIGMA_LOG."""
import json
import os
import sys
import time

FRAMES = {"12:1": "FL-31 / Work orders / default", "12:2": "FL-31 / Work orders / empty",
          "12:3": "FL-31 / Assign panel / ready", "12:4": "FL-31 / Assign panel / no technician",
          "12:5": "FL-31 / Toast / undo"}
OTHER = {f"30:{i}": f"Archive / Old dashboard v{i}" for i in range(1, 16)}
OTHER.update({f"40:{i}": f"FL-44 / Parts / state {i}" for i in range(1, 6)})
VARS = {"Color/Accent": "#0B6E66", "Color/Text/Default": "#1B2430", "Color/Text/Muted": "#5A6678",
        "Color/Surface": "#FFFFFF", "Color/Danger": "#B42318", "Spacing/2": "8", "Spacing/4": "16",
        "Radius/Md": "6", "Color/Brand/Promo": "#E0457B"}
TOOLS = [
    {"name": "get_metadata", "description": "Get the node tree (IDs, names, sizes) of a page or a node.",
     "inputSchema": {"type": "object", "properties": {"nodeId": {"type": "string"}, "fileKey": {"type": "string"}}}},
    {"name": "get_design_context", "description": "Get the design context (layout, styles, components, text) of a node as code.",
     "inputSchema": {"type": "object", "properties": {"nodeId": {"type": "string"}, "fileKey": {"type": "string"}}, "required": ["nodeId"]}},
    {"name": "get_variable_defs", "description": "Get the variables and styles used in the file or a node.",
     "inputSchema": {"type": "object", "properties": {"nodeId": {"type": "string"}, "fileKey": {"type": "string"}}}},
    {"name": "get_screenshot", "description": "Get a screenshot of a node.",
     "inputSchema": {"type": "object", "properties": {"nodeId": {"type": "string"}, "fileKey": {"type": "string"}}, "required": ["nodeId"]}},
    {"name": "whoami", "description": "The signed-in user, the plan, and the seat.",
     "inputSchema": {"type": "object", "properties": {}}},
]


def log(name, args):
    p = os.environ.get("FAKEFIGMA_LOG")
    if p:
        with open(p, "a") as f:
            f.write(json.dumps({"t": time.time(), "tool": name, "args": args}) + "\n")


def call(name, a):
    log(name, a)
    if name == "whoami":
        return os.environ.get("FAKEFIGMA_WHOAMI", "user: planner-team; plan: Professional; seat: View")
    if name == "get_metadata":
        rows = [f'<frame id="{k}" name="{v}" width="1440" height="900"/>' for k, v in {**FRAMES, **OTHER}.items()]
        return '<page id="0:1" name="Product">\n' + "\n".join(rows) + "\n</page>"
    if name == "get_variable_defs":
        return json.dumps(VARS)
    nid = a.get("nodeId", "")
    if nid in FRAMES:
        if name == "get_screenshot":
            return f"[screenshot of {FRAMES[nid]}: 1440x900, image omitted in this test]"
        return (f"// {FRAMES[nid]}\n<div className=\"shell\"><nav/>\n<main style={{{{padding: 'var(--Spacing/4)'}}}}>"
                f"<h1 style={{{{color: 'var(--Color/Text/Default)'}}}}>Unplanned work orders</h1>"
                f"<button style={{{{background: 'var(--Color/Accent)', borderRadius: 'var(--Radius/Md)'}}}}>Assign</button>"
                f"<p style={{{{color: 'var(--Color/Text/Muted)'}}}}>Not shown: 6 technicians without the skill</p></main></div>")
    if nid in OTHER:
        return f"// {OTHER[nid]}\n<div>An old screen that is not part of FL-31.</div>"
    return "Error: node not found"


def main():
    for line in sys.stdin:
        try:
            msg = json.loads(line)
        except json.JSONDecodeError:
            continue
        mid, method = msg.get("id"), msg.get("method")
        if method == "initialize":
            res = {"protocolVersion": msg.get("params", {}).get("protocolVersion", "2025-06-18"),
                   "capabilities": {"tools": {}}, "serverInfo": {"name": "figma", "version": "0.0.1-test"}}
        elif method == "tools/list":
            res = {"tools": TOOLS}
        elif method == "tools/call":
            p = msg.get("params", {})
            res = {"content": [{"type": "text", "text": call(p.get("name"), p.get("arguments") or {})}]}
        elif mid is None:
            continue
        else:
            res = {}
        sys.stdout.write(json.dumps({"jsonrpc": "2.0", "id": mid, "result": res}) + "\n")
        sys.stdout.flush()


if __name__ == "__main__":
    main()
