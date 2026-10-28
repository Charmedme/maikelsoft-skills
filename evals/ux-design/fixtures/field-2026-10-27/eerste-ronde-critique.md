# Critique van de mockups

Mockup-rubric van `ui-craft` (1 tot 3 per criterium). Gescoord op de screenshots in `shots/` (390 × 844, licht; één frame donker). Twee rondes: na ronde 1 zijn 6 fouten hersteld (zie onder). Geen criterium scoort nu 1.

## Ronde 1: gevonden en hersteld

| Frame | Probleem | Score voor | Fix |
|---|---|---|---|
| start (alle), concept-b/b-start | Het codeveld liep buiten het scherm | Alignment 1 | `.invoer { width: 100% }` |
| end/default, concept-a/a-einde | De winnaarskaart kromp in hoogte (flex-shrink in een vol scherm) | Content fit 1 | `flex-shrink: 0` voor de blokken van `.inhoud` en `.ndk-midden` |
| round/voorlezen-afstand | De audiobalk stond in de rondebalk en overlapte de naam | Alignment 1 | Audiobalk naar het midden, boven de kaartruggen |
| round/uitslag, success, voorlezen | Midden verticaal gecentreerd: een gat van 150 px boven de tekst | Rhythm 2 | Midden begint boven met `space-6` |
| concept-c/c-afstand | Versturen liep buiten het chatpaneel | Alignment 1 | `.ndk-chat { grid-template-columns: minmax(0, 1fr) }` |
| end/overflow | De badge "winnaar" brak midden in het woord | Content fit 1 | DS-9: badge `nowrap` |
| round/proposed-op-1 | Het tafelscherm in 2 kolommen maakte de stelling 1 woord breed | Hierarchy 1 | Eén kolom |

## Ronde 2: scores

| Frames | Hierarchy | Alignment | Rhythm | Density | Consistency | Content fit | Reden |
|---|---|---|---|---|---|---|---|
| concept-a (8) | 3 | 3 | 3 | 3 | 2 | 3 | Stelling altijd eerst; nieuwe stoelenrij en kaartrug (proposed) |
| concept-b (8) | 3 | 3 | 2 | 2 | 2 | 2 | Eén ding per scherm, maar veel lege ruimte; kiezen toont 1 van 7 kaarten |
| concept-c (8) | 2 | 3 | 3 | 3 | 2 | 3 | De tabbalk trekt aandacht met tellers; verder bekend |
| start (6) | 3 | 3 | 3 | 3 | 3 | 3 | Echte foutteksten bij de velden |
| lobby (9) | 3 | 3 | 3 | 3 | 2 | 3 | Stoelen-grid en samenvattingsregel zijn nieuw (UXD-008) |
| round: kiezen (default, in-progress, empty, error, disabled, kiezen-afstand, donker) | 3 | 3 | 3 | 3 | 2 | 3 | Taak en samenvatting bij de knop (PAT-ACT-08) |
| round: overflow | 3 | 3 | 2 | 2 | 2 | 3 | Lange vraag + lange kaart: 2 kaarten in beeld; zie open vraag over hoogte |
| round: wachten, leider, voorlezen, beoordelen | 3 | 3 | 3 | 3 | 2 | 3 | Het vak in de stelling is de zin die je voorleest |
| round: uitslag, botte-schande, pauze, toeschouwer | 3 | 3 | 3 | 3 | 2 | 3 | Regel bij het besluit: 0:24 bij de Botte Schande, "nodig 3, nu 2" bij de pauze |
| round: stand-vel, chat-vel | 3 | 3 | 3 | 3 | 2 | 3 | De stelling blijft boven het paneel zichtbaar |
| round: offline | 3 | 3 | 3 | 3 | 3 | 3 | Reden bij de knop |
| round: proposed-op-1, op-2 | 2 | 3 | 2 | 3 | 2 | 2 | Het tafelscherm is een schets op telefoonbreedte; echte maat 1920 × 1080 niet gemaakt |
| end (6) | 3 | 3 | 3 | 3 | 2 | 3 | Winnaarsrij met balk en woord (DS-8) |

## Open punten

- Op de rondebalk breekt "Anna leest voor" op 2 regels als er ook een knop Chat staat (op afstand). Mag zo blijven, of kort tot "Anna leest".
- De tafel neemt met een lange vraag 340 px; dan zijn 2 kaarten zichtbaar. Testen in de research backlog (rang 1).
- 820 en 1440 px zijn niet als mockup gemaakt.
