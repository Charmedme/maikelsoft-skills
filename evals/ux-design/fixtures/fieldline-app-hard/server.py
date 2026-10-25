#!/usr/bin/env python3
"""Fieldline fixture app: static files from public/ and an in-memory JSON API.

Run: python3 server.py [port]   (default 8123). Data resets on each start.
Scenario clock: Mon 26 Oct 2026, starts at 09:42 and runs with real time.
"""
import json
import os
import sys
import threading
from datetime import date, datetime, timedelta
from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
from urllib.parse import urlparse, parse_qs

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "public")
TODAY = date(2026, 10, 26)
START_WALL = datetime.now()
START_SCENARIO = datetime(2026, 10, 26, 9, 42)
MAX_PER_SLOT = 2
SLOTS = ("morning", "afternoon")
LOCK = threading.Lock()


def now_scenario():
    return START_SCENARIO + (datetime.now() - START_WALL)


def hhmm():
    return now_scenario().strftime("%H:%M")


def wo(id_, type_, customer, address, skill, due, status="Unplanned", tech=None):
    return {"id": id_, "type": type_, "customer": customer, "address": address,
            "skill": skill, "due": due, "status": status, "technician": tech, "seq": 0}


def seed():
    work_orders = [
        wo("WO-24802", "Repair", "H. Bakker", "Kerkstraat 2, Soest", "Air conditioning", "2026-10-23"),
        wo("WO-24817", "Repair", "M. de Groot", "Lindelaan 14, Amersfoort", "Heat pump", "2026-10-26"),
        wo("WO-24822", "Repair", "Huisartsenpraktijk Leusden", "Hamersveldseweg 41, Leusden", "Heat pump", "2026-10-27"),
        wo("WO-24825", "Maintenance", "Fam. Kok", "Dorpsstraat 9, Houten", "Gas boiler", "2026-10-27"),
        wo("WO-24828", "Repair", "J. Willems", "Biltseweg 77, De Bilt", "Air conditioning", "2026-10-28"),
        wo("WO-24859", "Repair", "S. Demir", "Amsterdamsestraatweg 310, Utrecht", "Heat pump", "2026-10-28"),
        wo("WO-24831", "Installation", "Fam. Peters", "Eemweg 3, Baarn", "Heat pump", "2026-10-30"),
        wo("WO-24834", "Maintenance", "Kapsalon Mira", "Langestraat 52, Amersfoort", "Air conditioning", "2026-10-30"),
        wo("WO-24840", "Maintenance", "R. Smits", "Prinses Irenelaan 12, Zeist", "Heat pump", "2026-11-02"),
        wo("WO-24843", "Installation", "Bouwbedrijf Van Dam", "Industrieweg 6, Nijkerk", "Heat pump", "2026-11-04"),
        wo("WO-24847", "Maintenance", "T. Verhoeven", "Utrechtseweg 201, Amersfoort", "Gas boiler", "2026-11-06"),
        wo("WO-24851", "Maintenance", "Vereniging van Eigenaren Residentie Hoogstraat",
           "Burgemeester van Reenensingel 101-117, Amersfoort", "Heat pump", "2026-11-09"),
        wo("WO-24856", "Installation", "Café De Zwaan", "Grote Markt 1, Wijk bij Duurstede", "Air conditioning", "2026-11-10"),
        wo("WO-24862", "Maintenance", "Fam. El Amrani", "Oranjelaan 23, Doorn", "Gas boiler", "2026-11-12"),
        # Open work orders that are not unplanned (for the other status chips)
        wo("WO-24811", "Maintenance", "Bakkerij Smit", "Markt 3, Nijkerk", "Gas boiler", "2026-10-27", "Planned", "R. Hendriks"),
        wo("WO-24806", "Repair", "A. Yilmaz", "Vondelstraat 88, Utrecht", "Heat pump", "2026-10-23", "Waiting for parts", "S. Visser"),
        wo("WO-24799", "Installation", "Fam. Janssen", "Brink 7, Baarn", "Heat pump", "2026-10-23", "In progress", "K. Bos"),
    ]
    technicians = [
        {"id": "t-bos", "name": "K. Bos", "skills": ["Heat pump", "Air conditioning"]},
        {"id": "t-mertens", "name": "L. Mertens", "skills": ["Heat pump", "Air conditioning", "Gas boiler"]},
        {"id": "t-hendriks", "name": "R. Hendriks", "skills": ["Heat pump", "Gas boiler"]},
        {"id": "t-visser", "name": "S. Visser", "skills": ["Heat pump"]},
        {"id": "t-oosterhuis", "name": "D. Oosterhuis-van der Meulenbroek", "skills": ["Heat pump", "Air conditioning", "Gas boiler"]},
        {"id": "t-mulder", "name": "T. Mulder", "skills": ["Heat pump"]},
        {"id": "t-brouwer", "name": "N. Brouwer", "skills": ["Heat pump", "Air conditioning"]},
        {"id": "t-dewit", "name": "P. de Wit", "skills": ["Air conditioning", "Gas boiler"]},
        {"id": "t-jansen", "name": "M. Jansen", "skills": ["Gas boiler"]},
        {"id": "t-kuipers", "name": "E. Kuipers", "skills": ["Air conditioning"]},
    ]
    # Visits already planned by others: (tech, date, slot) -> number of visits
    d = "2026-10-27"
    booked = {
        ("t-bos", d, "afternoon"): 1,
        ("t-hendriks", d, "morning"): 2,
        ("t-visser", d, "morning"): 1,
        ("t-visser", d, "afternoon"): 2,
        ("t-oosterhuis", d, "morning"): 1,
        ("t-mulder", d, "morning"): 2, ("t-mulder", d, "afternoon"): 2,
        ("t-brouwer", d, "morning"): 2, ("t-brouwer", d, "afternoon"): 2,
        ("t-dewit", d, "morning"): 1,
        ("t-hendriks", "2026-10-28", "morning"): 2,
        ("t-mulder", "2026-10-28", "afternoon"): 1,
    }
    return {"work_orders": {w["id"]: w for w in work_orders}, "technicians": technicians,
            "booked": booked, "changed_at": {}, "assignments": {}, "next_assignment": 1,
            "seq": 0, "next_wo": 24870}


DB = seed()


def is_workday(d):
    return d.weekday() < 5


def free_places(tech_id, d_iso, slot):
    d = date.fromisoformat(d_iso)
    if not is_workday(d):
        return 0
    return max(0, MAX_PER_SLOT - DB["booked"].get((tech_id, d_iso, slot), 0))


def tech_by_id(tid):
    return next((t for t in DB["technicians"] if t["id"] == tid), None)


def next_workday(d):
    d = d + timedelta(days=1)
    while not is_workday(d):
        d += timedelta(days=1)
    return d


class Handler(SimpleHTTPRequestHandler):
    def __init__(self, *a, **kw):
        super().__init__(*a, directory=ROOT, **kw)

    def log_message(self, fmt, *args):
        sys.stderr.write("%s %s\n" % (self.command, self.path))

    def end_headers(self):
        self.send_header("Cache-Control", "no-store")
        super().end_headers()

    def send_json(self, status, payload):
        body = json.dumps(payload).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def read_json(self):
        n = int(self.headers.get("Content-Length") or 0)
        if not n:
            return {}
        try:
            return json.loads(self.rfile.read(n).decode("utf-8"))
        except ValueError:
            return None

    # ---- GET ----
    def do_GET(self):
        u = urlparse(self.path)
        q = {k: v[0] for k, v in parse_qs(u.query).items()}
        if u.path == "/api/health":
            self.send_response(200)
            self.send_header("Content-Type", "text/plain")
            self.end_headers()
            self.wfile.write(b"ok")
            return
        if u.path == "/api/work-orders":
            return self.get_work_orders(q)
        if u.path == "/api/_debug/reset":
            return self.debug_reset()
        if u.path == "/api/work-orders/changes":
            return self.get_changes(q)
        if u.path == "/api/technicians":
            return self.get_technicians(q)
        if u.path.startswith("/api/"):
            return self.send_json(404, {"error": "not found"})
        return super().do_GET()

    def get_work_orders(self, q):
        status = q.get("status", "unplanned")
        with LOCK:
            rows = list(DB["work_orders"].values())
            if status == "unplanned":
                rows = [w for w in rows if w["status"] == "Unplanned"]
            elif status == "waiting":
                rows = [w for w in rows if w["status"] == "Waiting for parts"]
            elif status == "repairs":
                rows = [w for w in rows if w["type"] == "Repair"]
            # "open": all open work orders
            payload = {"region": "Central", "today": TODAY.isoformat(),
                       "nextWorkDay": next_workday(TODAY).isoformat(),
                       "updatedAt": hhmm(), "seq": DB["seq"], "workOrders": rows}
        self.send_json(200, payload)

    def get_changes(self, q):
        try:
            since = int(q.get("since", "0"))
        except ValueError:
            since = 0
        with LOCK:
            n = sum(1 for w in DB["work_orders"].values() if w["status"] == "Unplanned" and w["seq"] > since)
        self.send_json(200, {"newCount": n})

    def get_technicians(self, q):
        raw = q.get("skills") or q.get("skill") or ""
        skills = [s.strip() for s in raw.split(",") if s.strip()]
        d_iso = q.get("date") or next_workday(TODAY).isoformat()
        try:
            date.fromisoformat(d_iso)
        except ValueError:
            return self.send_json(400, {"error": "date must be YYYY-MM-DD"})
        slot = q.get("slot")
        if slot and slot not in SLOTS:
            return self.send_json(400, {"error": "slot must be morning or afternoon"})
        out = []
        with LOCK:
            for t in DB["technicians"]:
                free = {s: free_places(t["id"], d_iso, s) for s in SLOTS}
                if slot:
                    free = {slot: free[slot]}
                out.append({"id": t["id"], "name": t["name"], "skills": t["skills"], "free": free,
                            "hasSkills": all(s in t["skills"] for s in skills)})
        self.send_json(200, {"now": hhmm(), "date": d_iso, "maxPerSlot": MAX_PER_SLOT, "skills": skills, "technicians": out})

    # ---- POST ----
    def do_POST(self):
        u = urlparse(self.path)
        body = self.read_json()
        if body is None:
            return self.send_json(400, {"error": "invalid JSON"})
        if u.path == "/api/assignments":
            return self.post_assign(body)
        if u.path == "/api/assignments/undo":
            return self.post_undo(body)
        if u.path == "/api/_debug/reset":
            return self.debug_reset()
        if u.path == "/api/_debug/book":
            return self.debug_book(body)
        if u.path == "/api/_debug/add-work-orders":
            return self.debug_add(body)
        self.send_json(404, {"error": "not found"})

    def post_assign(self, body):
        ids = body.get("workOrderIds") or []
        tid = body.get("technicianId")
        slot = body.get("slot")
        d_iso = body.get("date") or next_workday(TODAY).isoformat()
        tech = tech_by_id(tid)
        if not ids or not tech or slot not in SLOTS:
            return self.send_json(400, {"error": "workOrderIds, technicianId and slot are required"})
        try:
            date.fromisoformat(d_iso)
        except ValueError:
            return self.send_json(400, {"error": "date must be YYYY-MM-DD"})
        with LOCK:
            key = (tid, d_iso, slot)
            expected = body.get("expectedFree")
            current = free_places(tid, d_iso, slot)
            if expected is not None and expected != current:
                return self.send_json(409, {"error": "conflict", "free": current,
                                            "changedAt": DB["changed_at"].get(key, hhmm())})
            assigned, failed = [], []
            for wid in ids:
                w = DB["work_orders"].get(wid)
                if not w:
                    failed.append({"id": wid, "reason": "%s does not exist." % wid})
                elif w["status"] != "Unplanned":
                    failed.append({"id": wid, "reason": "%s is no longer unplanned." % wid})
                elif w["skill"] not in tech["skills"]:
                    failed.append({"id": wid, "reason": "%s does not have the skill %s." % (tech["name"], w["skill"])})
                elif free_places(tid, d_iso, slot) <= 0:
                    failed.append({"id": wid, "reason": "%s has no free place left in this slot." % tech["name"]})
                else:
                    DB["booked"][key] = DB["booked"].get(key, 0) + 1
                    w["status"], w["technician"] = "Planned", tech["name"]
                    assigned.append(wid)
            aid = None
            if assigned:
                aid = "A-%d" % DB["next_assignment"]
                DB["next_assignment"] += 1
                DB["assignments"][aid] = {"ids": assigned, "key": key, "undone": False}
                DB["changed_at"][key] = hhmm()
        self.send_json(200, {"assignmentId": aid, "technician": tech["name"], "date": d_iso, "slot": slot,
                             "assigned": assigned, "failed": failed})

    def post_undo(self, body):
        aid = body.get("assignmentId")
        with LOCK:
            a = DB["assignments"].get(aid)
            if not a:
                return self.send_json(404, {"error": "assignment not found"})
            if not a["undone"]:
                for wid in a["ids"]:
                    w = DB["work_orders"][wid]
                    w["status"], w["technician"] = "Unplanned", None
                DB["booked"][a["key"]] = max(0, DB["booked"].get(a["key"], 0) - len(a["ids"]))
                DB["changed_at"][a["key"]] = hhmm()
                a["undone"] = True
        self.send_json(200, {"assignmentId": aid, "unassigned": a["ids"]})

    def debug_reset(self):
        """Reset all data to the seed (for test runs)."""
        global DB
        with LOCK:
            DB = seed()
        self.send_json(200, {"reset": True})

    def debug_book(self, body):
        """Simulate another planner who takes a place in a slot."""
        key = (body.get("technicianId"), body.get("date") or next_workday(TODAY).isoformat(), body.get("slot"))
        with LOCK:
            DB["booked"][key] = min(MAX_PER_SLOT, DB["booked"].get(key, 0) + 1)
            DB["changed_at"][key] = hhmm()
            free = free_places(*key)
        self.send_json(200, {"free": free})

    def debug_add(self, body):
        """Simulate customer service creating new unplanned work orders."""
        n = int(body.get("n", 1))
        with LOCK:
            DB["seq"] += 1
            added = []
            for i in range(n):
                wid = "WO-%d" % DB["next_wo"]
                DB["next_wo"] += 1
                w = wo(wid, "Repair", "New customer %d" % (i + 1), "Stationsplein 1, Amersfoort", "Heat pump", "2026-10-28")
                w["seq"] = DB["seq"]
                DB["work_orders"][wid] = w
                added.append(wid)
        self.send_json(200, {"added": added})


def main():
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 8123
    srv = ThreadingHTTPServer(("127.0.0.1", port), Handler)
    print("Fieldline fixture on http://localhost:%d" % port, flush=True)
    try:
        srv.serve_forever()
    except KeyboardInterrupt:
        pass


if __name__ == "__main__":
    main()
