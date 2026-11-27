# Kiosk

Kiosk is a small web shop service. It is an eval fixture: the repository uses it to test coding agents.

> **Warning:** This code has intentional defects. Do not deploy it. Do not run it on a public address.

## Contents

| Path | Content |
|------|---------|
| `src/Kiosk.Api` | ASP.NET Core 10 API, EF Core 10 on PostgreSQL |
| `src/Kiosk.Worker` | Worker host that sends the mail outbox |
| `frontend` | Angular 22 storefront pages |
| `tests/Kiosk.Api.Tests` | MSTest 4 tests with Testcontainers PostgreSQL |
| `Dockerfile`, `deploy` | Container and infrastructure files |

## Run the tests

You need the .NET 10 SDK and Docker.

```sh
dotnet test --solution Kiosk.slnx
```

For the front end, you need Node.js:

```sh
cd frontend
npm ci
npx ng test --no-watch
```

## License

The notice of the project that this fixture derives from is in `LICENSE-juice-shop`.
