# Examples: UI text

Each example shows a weak text, the same facts after the rules, and what changed. Copy the method, not the words.

## English

Context from the human: the upload fails when the file is larger than 10 MB.

| Element | Before | After |
|---------|--------|-------|
| Error message | Oops! Something went wrong while uploading your file. Please try again later. | Cannot upload the file. The file is larger than 10 MB. Make the file smaller and try again. |
| Button | Submit | Upload file |

What changed:

- The error message says what happened, why, and what to do. "Try again later" did not help, because the size causes the error, not the time.
- "Oops" and "Please" are gone.
- The button names the action and the object.

## Dutch (B1)

Context from the human: the reader is a customer. Use "u". The product has the button "Factuur maken".

| Element | Before | After |
|---------|--------|-------|
| Confirmation (delete a draft invoice) | Weet u zeker dat u wilt doorgaan? [Ja] [Nee] | Concept-factuur verwijderen? U kunt dit niet ongedaan maken. [Concept verwijderen] [Annuleren] |
| Empty state | Er zijn op dit moment geen gegevens beschikbaar. | Nog geen facturen. Maak een factuur om te beginnen. [Factuur maken] |
| Error (no fix known) | Er is een fout opgetreden. | De factuur is niet verstuurd. `TODO: de oorzaak en de oplossing staan niet in de bron.` |

What changed:

- The confirmation names the action and the result. The buttons say the action, not "Ja" and "Nee".
- The human chose "u", so all text uses "u".
- The empty state says what is missing and how to start. The action is the existing button text, not a new term.
- For the last error, the product gives no cause and no fix. The text has a `TODO:`. The writer did not invent a fix.
