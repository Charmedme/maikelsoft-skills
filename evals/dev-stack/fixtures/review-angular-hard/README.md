# Products admin

The admin app for the product catalog. Angular 22 with standalone components, Transloco for the texts (English and Dutch), and Vitest for the unit tests.

## Requirements

- Node.js 24 or later, npm 11 or later
- Python 3.10 or later (only for the mock API)

## Install

```bash
npm ci
```

## Commands

| Command | What it does |
|---|---|
| `npx ng build` | Builds the app into `dist/products-admin/browser`. |
| `npx ng test --no-watch` | Runs the unit tests once (Vitest). |
| `npx ng lint` | Runs angular-eslint. |
| `npx ng serve` | Starts the dev server on port 4200. It does not proxy `/api`; use the mock API below. |

## Run the app with the mock API

The real API is in another repository. For local work, `mock-api/server.py` gives the same API with data in memory. It also serves the built app on the same port, so that `/api` calls need no proxy.

```bash
npx ng build
python3 mock-api/server.py
```

Then open http://localhost:8080. Environment variables:

| Variable | Default | Meaning |
|---|---|---|
| `PORT` | `8080` | The port of the server. |
| `STATIC_DIR` | `dist/*/browser` | The folder with the built app. |
| `API_DELAY_MS` | `50` | A delay for each API call, to make loading states visible. |
| `MOCK_API_LOG` | not set | Set to `1` to log each request. |

The data starts again from the seed at each start of the server.

## API

All bodies are JSON. Errors are ProblemDetails (`application/problem+json`), as the real ASP.NET Core API sends them.

| Method and path | Result |
|---|---|
| `GET /api/products` | 200, the list of products. |
| `GET /api/products/{id}` | 200 with an `ETag` header (the version of the product), or 404. |
| `POST /api/products` | 201 with the new product, or 400. |
| `PUT /api/products/{id}` | 200 with the product and the new `ETag`. Needs `If-Match` with the ETag: 428 without it, 412 when the product changed after the read. 400 for a wrong field, 404 for an unknown id. |
| `DELETE /api/products/{id}` | 204, or 404. |
| `GET /api/products?search={text}` | 200, the products whose name or code contains the text. |
| `GET /api/me` | 200, the signed-in user with the roles (`catalog`, `pricing`). |
| `GET /api/price-changes/preview?ids=1,2&percentage=10` | 200, the old and the new price of each product. |
| `POST /api/price-changes` | 201 with the price change. Body: `productIds`, `percentage` (-90 to 100), `effectiveDate` (`yyyy-MM-dd`). 400 for a wrong field. |
| `GET /api/price-changes/{id}` | 200 with the price change, or 404. |

A product:

```json
{ "id": 1, "name": "Oak desk", "code": "OD-100", "price": 349.0, "availableFrom": "2026-01-12" }
```

`availableFrom` is a date without a time (`yyyy-MM-dd`). The `code` must be unique.

A 400 response is a ValidationProblemDetails, for example:

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "Code": ["The code OD-100 is already used by another product."] }
}
```

## Dev endpoints of the mock

These endpoints exist only in the mock. They help with manual tests and end-to-end tests.

| Method and path | What it does |
|---|---|
| `POST /__reset` | Puts the seed data back and clears the request log. |
| `GET /__requests` | The log of the API calls: method, path, headers, and body of each call. |
| `POST /__fail` | Body `{"path": "/api/products/3", "status": 500, "method": "DELETE", "times": 1}`. The next call to that path (and method, if given) gets that status. |
| `POST /__touch/{id}` | Increases the version of the product, as if another user saved it. An optional body changes fields too. |
