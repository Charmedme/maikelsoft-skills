#!/usr/bin/env python3
"""Simulated product owner for the grill test.

    python3 oracle.py "<one question>"

The product owner knows hidden facts. A fact comes back only when the question
is about its topic. For a choice in the question, the product owner always takes
the recommended answer. Each call goes to log.jsonl next to this script.
The agent under test must not read this file or the log.
"""
import json
import pathlib
import re
import sys

FACTS = {
    "N1-current-work": (r"\b(today|currently|at the moment|right now|current (way|process|workflow|situation)|how do (they|buyers|customers|users) .{0,40} now|how does .{0,40} work now|workaround)\b",
                        "Today buyers e-mail an Excel file to customer service, and customer service types each line into the shop by hand."),
    "N2-pain": (r"(what goes wrong|where do .{0,30} (get stuck|go wrong)|pain|frustrat|complain|biggest problem|main problem|most errors|what problems)",
                "Most errors come from article numbers: buyers use their own article numbers, not the shop numbers."),
    "N3-exception": (r"\b(exceptions?|special cases?|unusual|work differently|do differently|edge cases? (in|of) (their|the) (work|process|orders))\b",
                     "Some buyers send one file with lines for different delivery addresses."),
    "N4-segments": (r"(which customers|what customers|groups? of (customers|buyers|users)|customer groups?|types? of (customers|buyers|users)|segments?|large customers|all customers|every customer|who (will|would) use)",
                    "The 20 largest customers already order through EDI. They do not need the upload."),
    "N5-success": (r"\b(measure|metric|kpi|success|how do we know|how will we know)\b",
                   "Customer service spends about 12 hours each week to type these orders."),
    "I1-account-admin": (r"(account admin|account settings|master data|maintain|maintenance|manage (the )?(list|mapping|numbers)|admin (page|screen|module))",
                         "Customer service keeps a list of customer article numbers in the account admin. Buyers cannot see it."),
    "I2-reporting": (r"\b(report|reports|reporting|order history|analytics|dashboard)\b",
                     "Sales reports must show which orders came from an upload."),
    "I3-roles": (r"\b(roles?|permissions?|authori[sz]\w*|who (can|may) (upload|use|place)|approval|approve)\b",
                 "Orders above 5,000 euro need approval of a manager in the customer account, also for an upload."),
}

question = " ".join(sys.argv[1:]).lower()
hits = [key for key, (pattern, _) in FACTS.items() if re.search(pattern, question)]
with open(pathlib.Path(__file__).with_name("log.jsonl"), "a", encoding="utf-8") as log:
    log.write(json.dumps({"q": question, "hits": hits}) + "\n")
lines = [FACTS[key][1] for key in hits]
lines.append("For each choice in this question: take your recommended answer.")
print("\n".join(lines))
