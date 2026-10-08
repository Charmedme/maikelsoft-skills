---
name: doc-code-comments
description: Code comments and API doc comments (C# XML docs, TSDoc, JSDoc, Python docstrings, Javadoc). Use when the user wants comments or doc comments added, rewritten, or reviewed in source code.
allowed-tools: Bash(python3 ${CLAUDE_SKILL_DIR}/scripts/doccheck.py *)
---

# Code comments

Code comments are always in English, also in a project with Dutch documentation. There are two kinds:

- **Doc comments** describe a public API: what a type or member does, its parameters, the return value, and the errors. Tools and IDEs show them to the reader.
- **Inline comments** tell why the code is as it is. The code itself tells what it does.

<!-- BEGIN shared/workflow.md -->
## Modes

- **Write**: make a new document.
- **Rewrite**: improve an existing document. Keep each fact, code sample, command, link, and heading anchor. Change the structure and the language.
- **Review**: report the findings. Change nothing.

## Steps

1. **Read the language rules.** Read `references/language.md` in this skill folder. Done when you know the language, the level, and (Dutch) "je" or "u". If one is not clear, ask the human before you continue.
2. **Collect the facts.** Read the request, the code, and the existing docs. Done when each section of the template below has its facts. A section with a missing fact has a question for the human or a `TODO:`.
3. **Find the source errors.** Rewrite and review mode. Look for commands that cannot work and tools that are old. Also look for two names for one item, and facts that do not agree with the code. Done when you have a list of source errors. Do not fix a source error silently, and do not copy it silently.
4. **Make the outline.** Use the template below. Give each part of the content one document type. If content of a different type has its own document, move the content there and add a link. If not, keep the content in its own section, and suggest the new document in the report. A rewrite never deletes content. Done when each heading has one purpose.
5. **Write.** Write or rewrite mode only. In a rewrite, keep each heading that other pages can link to. If a heading must change, list the old anchor in the report. An explicit ID (`{#old-anchor}`) keeps the anchor on kramdown, MkDocs, and Hugo sites, but not on GitHub. Done when each section of the outline has its text.
6. **Check.** Run `python3 ${CLAUDE_SKILL_DIR}/scripts/doccheck.py --level <level> <file>`. In rewrite mode, add `--source <original file>`. Outside Claude Code, use the path of the `scripts` folder of this skill. Done when the checker shows 0 errors, and you fixed each warning or gave a reason to keep it.
7. **Report.** Give the file path, the checker summary line, the open questions, the `TODO:` items, and the source errors. In rewrite mode, also give the main changes and the changed anchors. In review mode, give each finding as: location, rule (a checker rule ID, an STE section, or "Source error"), problem, fix.
<!-- END shared/workflow.md -->

To check the comments, run the checker with `--comments` on the source file. It reads the comments that start a line, block comments, and Python docstrings, and it gives the line numbers of the source file. It does not read a comment at the end of a line of code. In rewrite mode, also give `--source <original file>`. The checker then reports each code line that changed.

## Templates

Use the doc comment format of the language:

~~~csharp
/// <summary>Calculates the customs value of a shipment line.</summary>
/// <param name="line">The shipment line. Must not be null.</param>
/// <returns>The customs value in euros.</returns>
/// <exception cref="ArgumentNullException">When <paramref name="line"/> is null.</exception>
~~~

~~~typescript
/**
 * Calculates the customs value of a shipment line.
 * @param line - The shipment line.
 * @returns The customs value in euros.
 * @throws {RangeError} When the quantity is negative.
 */
~~~

~~~python
def customs_value(line: ShipmentLine) -> Decimal:
    """Return the customs value of a shipment line in euros.

    Raises ValueError when the quantity is negative.
    """
~~~

## Rules for this type

- The first sentence of a doc comment is the summary. It says what the member does. Follow the convention of the language:
  - C#, TSDoc, JSDoc, Javadoc: a verb in the third person ("Calculates").
  - Python (PEP 257): an imperative verb ("Return").
  - Go: the name of the item first ("next reads the next rune.").
- Document each public member. Do not add a doc comment to a private member unless its behavior is not clear from its name and its code. In a review, keep the correct comments that exist.
- A comment that does not agree with the code is a source error. Report it first: a wrong comment is worse than no comment.
- Give the facts that the signature does not give: units, ranges, null handling, side effects, thread safety, errors.
- An inline comment gives the reason: a constraint, a workaround, a link to an issue or a specification. Delete a comment that only repeats the code.
- Write `TODO(<owner or issue>): <action>` for open work. Do not leave code in comments.
- Change only comments. Do not change the code.
