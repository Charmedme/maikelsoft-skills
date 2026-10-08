# Examples: README

Each example shows a weak text, the same facts after the rules, and what changed. Copy the method, not the words.

## English

### Before

> Welcome to yamlcheck! yamlcheck is a powerful, blazing-fast and easy-to-use solution that leverages modern technologies to seamlessly validate all your YAML config files against JSON Schema, so DevOps teams never ship a broken config again. Just install it and you're good to go!

### After

~~~markdown
# yamlcheck

yamlcheck checks YAML configuration files against a JSON Schema. It is for DevOps teams that want to find a bad configuration before a deploy.

## Install

`TODO: the source does not give the install command.`
~~~

### What changed

- The first sentence says what the tool does. The second sentence names the reader and the main use.
- The marketing words are gone: "powerful", "blazing-fast", "easy-to-use", "leverages", "seamlessly", "just".
- "Just install it" has no command. The writer did not invent one. The README has a `TODO:` and the report asks the human.
- The contraction ("you're") is gone.

## Dutch (B1)

### Before

> Deze repository bevat de code van de Factuurservice. De service wordt gebruikt door meerdere applicaties voor het aanmaken van facturen en kan lokaal gedraaid worden middels docker compose.

### After

~~~markdown
# Factuurservice

De Factuurservice maakt facturen voor andere applicaties.

## Snel starten

Je kunt de service op je eigen computer starten met Docker Compose.
`TODO: het commando en de verwachte uitvoer staan niet in de bron.`
~~~

### What changed

- The first sentence says what the service does, not what the repository contains.
- "wordt gebruikt door" (passive) became an active sentence.
- "voor het aanmaken van" (noun made from a verb) became the verb "maakt".
- "middels" became "met". "gedraaid worden" became "starten".
- The source does not give the exact command. The writer did not guess `docker compose up`.
