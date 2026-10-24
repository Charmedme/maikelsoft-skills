# Product questions

The product owner (PO) owns the product decisions. The developer owns the technical decisions. One question has one owner, so a technical decision cannot change a product decision without the PO.

## Which questions are product questions

A question is a product question when its answer changes what a user sees, can do, or gets:

- The user, the value, or the scope (what is in, what is out).
- The visible behaviour: screens, texts, messages, the order of steps.
- The result of an error or an edge case for the user (what the user sees, which status the API returns to a client).
- A business rule: a limit, a period, a price, who can do what.
- A conflict between the item and a technical fact (the item asks for a thing that the system cannot do as written).

A question about how the code does it is technical when the result for the user stays the same. Examples: a library, a pattern, a table design, a test seam.

## What to do

1. **Search first.** A decision that the PO made can answer the question already. Search the item, its comments and links, the backlog context file (section "Decisions"), and the decision records. In solo work the PO is the human in the session. Then an answer that cites an existing decision counts as approved. Write the decision ID and its words.
2. **Collect.** Put all open product questions in one list. Give each question the options and your recommended answer with the reason in one sentence. The PO can then answer "ok".
3. **Draft the comment.** Write one comment for the item, in the language of the item. Start with one line that says which work waits for the answer.
4. **Get approval.** Show the human the exact text. Post nothing before the human approves it.
5. **Post.** Post the approved text as a comment on the item with the comment tool of its system. Examples: `wit_add_work_item_comment`, `addCommentToJiraIssue`, `add_issue_comment`. No comment tool: give the text to the human to post.
6. **Wait.** Write the questions and the link of the comment in the section "Waiting for the PO" of `plan.md`. Stop the work that depends on an answer. Go on with the work that does not, after the human approved the acceptance tests at the gate Spec. Without a human, stop before the build.
7. **Continue.** On a new start, read the new comments of the item. Copy each answer of the PO to "Waiting for the PO", with its date. When a PO answer changes an acceptance criterion, ask the PO to change the item itself.

## Rules

- Never write a product decision in a technical decision record. When a technical choice changes the visible behaviour, it is a product question.
- The human in the session can answer a product question only when the human is the PO, or says that the PO agrees. Write who answered.
- No human to approve the comment (unattended run): do not post. Give the drafted comment in the report.
