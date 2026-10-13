# Epic rules

Each rule has an ID. The checker (`backlogcheck.py`) finds the rules marked "checker". You find the others. A review cites the ID.

## Contents

- Title and outcome
- Size and scope
- Features
- SAFe
- Dutch

## Title and outcome

- **E1 Change, not system.** The title names the change for the users or the business: "Customers manage their own account". It does not name a system part or a project code. (checker)
- **E2 Outcome with a measure.** The outcome says why the work is worth doing. "Measure:" gives the number. When nobody can give the number, write "Measure: open question" and put the question in the "Notes". Do not invent a target. (checker)

## Size and scope

- **E3 More than one feature.** An epic has 2 to 6 features. One feature is not an epic: use `backlog-feature`. More than 6 features means 2 epics, or a product goal above them. (checker: warning)
- **E4 In and Out.** "In" lists the results for the users. "Out" lists what a reader can expect but does not get. Each thing that the request names is "In", or "Out" with the reason. (checker)

## Features

- **E5 Through backlog-feature.** Each feature follows the rules of the `backlog-feature` skill. Each feature has this epic as its parent.
- **E6 Delivery order.** Put the features in the order of delivery. The first feature gives the most value, or tests the biggest risk. Give the reason in one line in the "Notes" when the order is not clear.
- **E7 No acceptance criteria on the epic.** The features and the stories hold the criteria. The team closes an epic when it finished the features and the measure shows the outcome.
- **E8 No bugs, no tasks.** Neither is part of an epic.

## SAFe

In SAFe, an epic has a hypothesis instead of a plain outcome: "We believe that <epic> gives <outcome> for <users>. We know that we are right when <measure>." Also add the MVP: the smallest set of features that tests the hypothesis. Write it under "Notes" as "MVP: <features>".

## Dutch

Use the headings "Resultaat", "Scope", "Features", and "Notities". Use "Meten:", "Binnen scope:", and "Buiten scope:". Follow the Dutch rules in `language.md`.
