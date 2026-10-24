# Partner contract

`Orders.Api.json` is the OpenAPI document of the Orders API. The webshop partner generates its typed client from this file. The admin endpoints are not in it.

## How the file is made

The build writes the file (`Microsoft.Extensions.ApiDescription.Server`). Build the solution and commit the file together with each change of an endpoint or a contract record:

```bash
dotnet build
git add docs/openapi
```

CI builds the solution and fails when the committed file is not current (`.github/workflows/contract.yml`).

## Build without a database

The build starts the app in the Production environment to read the endpoints. The build machine has no database, so `Database:MigrateOnStartup` is `false` in `appsettings.json`. `appsettings.Development.json` sets it to `true` for local work.

## Try the API

Run the API in Development and open `/scalar`. The live document is at `/openapi/v1.json`. Production does not serve the document or the UI.
