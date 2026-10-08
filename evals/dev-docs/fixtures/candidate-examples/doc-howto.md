# Examples: how-to guide

Each example shows a weak text, the same facts after the rules, and what changed. Copy the method, not the words.

## English

### Before

> To deploy the app you should first make sure you're on the main branch and that tests pass, then the build can be started with `make build` which produces an image and pushes it to the registry; after that, deploy it with `helm upgrade app ./chart` and check the logs.

### After

~~~markdown
# Deploy the app

At the end of this guide, the new version of the app runs in the cluster.

## Steps

1. Check out the main branch:

   ```sh
   git checkout main
   ```

2. Run the tests. If a test fails, stop here.
   `TODO: the source does not give the test command.`

3. Make the image and push it to the registry:

   ```sh
   make build
   ```

4. Deploy the image:

   ```sh
   helm upgrade app ./chart
   ```

## Make sure that it works

Read the logs of the app. `TODO: the source does not give the command for the logs.`
~~~

### What changed

- One action in each step. The long sentence had five actions.
- The condition comes first: "If a test fails, stop here."
- "should" and "make sure you're" are gone. The step is an instruction.
- The source does not give the commands for the tests and the logs. The writer did not invent them. Each one has a `TODO:`.
- The semicolon is gone.

## Dutch (B1)

### Before

> Om de database te migreren dient eerst een backup gemaakt te worden, waarna het migratiescript middels `./migrate.sh up` kan worden uitgevoerd; controleer na afloop of de versie in de tabel schema_version is opgehoogd.

### After

~~~markdown
# De database migreren

Na deze stappen heeft de database de nieuwe versie van het schema.

## Stappen

1. Maak een backup van de database.
   `TODO: het commando voor de backup staat niet in de bron.`

2. Start het migratiescript:

   ```sh
   ./migrate.sh up
   ```

## Controleer het resultaat

Kijk in de tabel `schema_version`. De versie is nu hoger dan voor de migratie.
~~~

### What changed

- "dient ... gemaakt te worden" (passive, formal) became an instruction: "Maak een backup".
- "middels" became a plain sentence. "kan worden uitgevoerd" (passive) became "Start".
- One instruction in each step, and a separate section for the check.
- The backup command is not in the source, so the text has a `TODO:`. The writer did not invent a command.
