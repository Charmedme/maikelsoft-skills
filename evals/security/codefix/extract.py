#!/usr/bin/env python3
"""Make challenges.json, the codefix choice set, from a clone of OWASP Juice Shop.

    python3 evals/security/codefix/extract.py <juice-shop clone>

Juice Shop (MIT, refer to LICENSE-juice-shop) has coding challenges in data/static/codefixes/: for each
challenge 3 or 4 candidate fixes, one with the suffix _correct, and an info.yml with the reason of each
fix. The vulnerable original is the code between the vuln-code-snippet markers in the source.

For each challenge in SKILLS, the script:
- takes the original snippet and each fix, and removes each comment line, also commented-out code,
  so that no comment tells which fix is correct;
- replaces the names that point to Juice Shop and its challenges (NAMES);
- replaces the body of each private key with a placeholder (KEY), so that no key goes into a commit;
- shows each fix as a unified diff against the original, in an order that comes from a hash of the
  challenge key, so that each run sees the same order.

The answer key (letter and the info.yml reasons) goes into answers.json, next to challenges.json.
Do not give answers.json to a run.
"""
import difflib
import hashlib
import json
import pathlib
import re
import sys

HERE = pathlib.Path(__file__).resolve().parent
JUICE_COMMIT = "c9f9922"

# Challenge key -> skill. Out of scope:
# - the 5 yml password-reset challenges: their correct option is only a comment ("use a one-time link"),
#   so the choice is trivial once the comments are gone;
# - scoreBoardChallenge: its correct option is "no change", a rule of the game, not a security fix;
# - the web3, NFT, and token-sale challenges.
SKILLS = {
    "accessLogDisclosureChallenge": "secure-coding",
    "changeProductChallenge": "secure-coding",
    "chatbotGreedyInjectionChallenge": "secure-coding",
    "chatbotPromptInjectionChallenge": "secure-coding",
    "dbSchemaChallenge": "secure-coding",
    "directoryListingChallenge": "secure-coding",
    "exposedMetricsChallenge": "secure-coding",
    "forgedReviewChallenge": "secure-coding",
    "iacLeakedKeyChallenge": "secure-coding",
    "loginAdminChallenge": "secure-coding",
    "loginBenderChallenge": "secure-coding",
    "loginJimChallenge": "secure-coding",
    "noSqlReviewsChallenge": "secure-coding",
    "passkeyCredentialOverwriteChallenge": "secure-coding",
    "passkeySignatureChallenge": "secure-coding",
    "redirectChallenge": "secure-coding",
    "redirectCryptoCurrencyChallenge": "secure-coding",
    "registerAdminChallenge": "secure-coding",
    "resetPasswordMortyChallenge": "secure-coding",
    "unionSqlInjectionChallenge": "secure-coding",
    "vulnerableDockerImageChallenge": "secure-coding",
    "weakPasswordChallenge": "secure-coding",
    "adminSectionChallenge": "web-security",
    "localXssChallenge": "web-security",
    "restfulXssChallenge": "web-security",
    "xssBonusChallenge": "web-security",
}

NAMES = [
    (r"OWASP Juice Shop", "Kiosk"), (r"Juice Shop", "Kiosk"), (r"juice-shop", "kiosk"), (r"juice_shop", "kiosk"),
    (r"juiceshop", "kiosk"), (r"juice-sh\.op", "kiosk.example"), (r"[Jj]uice", "Kiosk"), (r"OWASP", "Example"),
    (r"owasp", "example"), (r"Bjoern|Bjrn|Björn", "Sam"), (r"bjoern", "sam"), (r"Bender", "Alex"),
    (r"bender", "alex"), (r"\bJim\b", "Kim"), (r"\bjim\b", "kim"), (r"Morty", "Robin"), (r"morty", "robin"),
    (r"Uvogin", "Lee"), (r"uvogin", "lee"), (r"Kimminich", "Doe"),
    (r"challenges\.\w+", "events.flag"), (r"challengeUtils\.solveIf", "audit.markIf"),
    (r"challengeUtils", "audit"),
    # The game's own words. Lowercase "challenge" stays: in the passkey snippets it is the WebAuthn challenge.
    (r"coding-challenge/:challengeKey", "coding-badge/:badgeKey"), (r"\bChallenge", "Badge"),
]

COMMENT_LINE = re.compile(r"^\s*(//|#|/\*|\*|\*/)")
TRAILING = re.compile(r"\s+//\s.*$")
BLOCK = re.compile(r"/\*[\s\S]*?\*/")
# Challenges whose answer key is not a clear security choice. score.py leaves them out.
# - redirectCryptoCurrencyChallenge: the "fix" removes old wallet addresses, a game rule.
# - registerAdminChallenge: create.send.before runs after the write, so the keyed fix sets the role
#   only on the response.
# - resetPasswordMortyChallenge: with "trust proxy" on, req.ip is also spoofable, so option A is
#   as good a fix as the keyed option.
DISPUTED = {"redirectCryptoCurrencyChallenge", "registerAdminChallenge", "resetPasswordMortyChallenge"}
EXT = {".ts": "typescript", ".yml": "yaml", ".tf": "hcl", ".sol": "solidity"}


def strip_comments(code: str) -> str:
    code = BLOCK.sub("", code)
    lines = []
    for line in code.splitlines():
        if COMMENT_LINE.match(line) and "vuln-code-snippet" not in line:
            continue
        lines.append(TRAILING.sub("", line).rstrip())
    return "\n".join(lines).strip("\n") + "\n"


# Juice Shop hardcodes a private key on purpose. The challenge is the key in the source, not its value.
KEY = re.compile(r"(-----BEGIN [A-Z ]*PRIVATE KEY-----)[\s\S]*?(-----END [A-Z ]*PRIVATE KEY-----)")


def anonymize(code: str) -> str:
    for pattern, repl in NAMES:
        code = re.sub(pattern, repl, code)
    return KEY.sub(lambda m: m.group(1) + "\\n<key body removed>\\n" + m.group(2), code)


def original_snippet(clone: pathlib.Path, key: str) -> str:
    """The code between the vuln-code-snippet markers of the key, as lib/codingChallenges.ts cuts it."""
    for path in sorted(clone.rglob("*")):
        if not path.is_file() or path.suffix not in EXT or "node_modules" in path.parts or "codefixes" in path.parts:
            continue
        src = path.read_text(encoding="utf-8", errors="replace")
        m = re.search(rf"[/#]{{0,2}} vuln-code-snippet start.*\b{key}\b[\s\S]*?vuln-code-snippet end.*\b{key}\b", src)
        if not m:
            continue
        snippet = m.group(0)
        snippet = re.sub(r"\s?[/#]{0,2} vuln-code-snippet start.*[\r\n]{0,2}", "", snippet)
        snippet = re.sub(r"\s?[/#]{0,2} vuln-code-snippet end.*", "", snippet)
        snippet = re.sub(r".*[/#]{0,2} vuln-code-snippet hide-line[\r\n]{0,2}", "", snippet)
        snippet = re.sub(r".*[/#]{0,2} vuln-code-snippet hide-start[\s\S]*?[/#]{0,2} vuln-code-snippet hide-end[\r\n]{0,2}", "", snippet)
        snippet = re.sub(r"\s*[/#]{0,2} vuln-code-snippet (vuln|neutral)-line.*", "", snippet)
        return snippet.strip() + "\n", EXT[path.suffix]
    raise SystemExit(f"{key}: no snippet in the source")


def info_reasons(info: pathlib.Path) -> dict:
    """id -> explanation, from the fixes list of info.yml (a small parser: the file has one fixed form)."""
    text = info.read_text(encoding="utf-8")
    reasons = {}
    for m in re.finditer(r"- id: (\d+)\s*\n\s*explanation: (['\"])(.*?)\2\s*$", text, re.M | re.S):
        reasons[int(m.group(1))] = m.group(3).replace("''", "'")
    return reasons


def main() -> int:
    if len(sys.argv) != 2:
        print(__doc__)
        return 2
    clone = pathlib.Path(sys.argv[1]).resolve()
    fixes_dir = clone / "data" / "static" / "codefixes"
    challenges, answers = [], {}
    for n, (key, skill) in enumerate(SKILLS.items(), start=1):
        cid = f"CF{n:02d}"
        original, lang = original_snippet(clone, key)
        original = anonymize(strip_comments(original))
        fixes = sorted(fixes_dir.glob(f"{key}_*"))
        fixes = [f for f in fixes if not f.name.endswith(".info.yml")]
        order = sorted(fixes, key=lambda f: hashlib.sha1(f"{key}/{f.name}".encode()).hexdigest())
        options, correct, ids = {}, None, {}
        for letter, f in zip("ABCD", order):
            fixed = anonymize(strip_comments(f.read_text(encoding="utf-8", errors="replace")))
            diff = "".join(difflib.unified_diff(original.splitlines(True), fixed.splitlines(True),
                                                "original", f"option {letter}", n=3))
            options[letter] = diff or "(no change to the code)\n"
            ids[letter] = int(re.search(rf"{key}_(\d+)", f.name).group(1))
            if "_correct" in f.name:
                correct = letter
        reasons = info_reasons(fixes_dir / f"{key}.info.yml")
        challenges.append({"id": cid, "skill": skill, "language": lang, "original": original, "options": options})
        answers[cid] = {"correct": correct, "source": key, "disputed": key in DISPUTED,
                        "reasons": {letter: reasons.get(i, "") for letter, i in ids.items()}}
    (HERE / "challenges.json").write_text(json.dumps(challenges, indent=1) + "\n", encoding="utf-8")
    (HERE / "answers.json").write_text(json.dumps(answers, indent=1) + "\n", encoding="utf-8")
    print(f"{len(challenges)} challenges from Juice Shop {JUICE_COMMIT}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
