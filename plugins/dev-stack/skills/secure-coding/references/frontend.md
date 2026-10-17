# Frontend security: Angular and AngularJS

Angular 22 and AngularJS 1.8. Component and service use is in `dev-stack:angular` and `dev-stack:angularjs`. The TypeScript snippets are not compiled: the check environment has no Angular packages.

## Contents

- What Angular does by default
- bypassSecurityTrust
- DOM writes
- CSP and Trusted Types
- XSRF with ASP.NET Core
- Token storage
- AngularJS (legacy)
- Never write

## What Angular does by default

- Interpolation `{{ }}` escapes. `[innerHTML]`, `[style]` and URL bindings go through the sanitizer.
- The sanitizer removes scripts and unsafe URLs. It cannot make a `ResourceURL` safe (script `src`, iframe `src`): Angular blocks a value from data there.
- AOT is the CLI default. Never build a template from user input, and never render Angular templates on the server with user data in them.

## bypassSecurityTrust

Each `bypassSecurityTrustHtml`, `Style`, `Script`, `Url` or `ResourceUrl` call turns off the sanitizer for that value. Each call is a review item.

- Accept a call only when the code built the value from validated parts. Example: an embed URL from a fixed host and a video id that matches `^[A-Za-z0-9_-]{11}$`.
- When Angular strips HTML that the user needs, bind with `[innerHTML]` and accept the result of the sanitizer. Do not bypass it.
- When you need the sanitized value in code, call `sanitizer.sanitize(SecurityContext.HTML, value)`.

```typescript
// Not compiled. A value built from validated parts.
const id = /^[A-Za-z0-9_-]{11}$/.test(videoId) ? videoId : null;
this.embedUrl = id
  ? this.sanitizer.bypassSecurityTrustResourceUrl(`https://www.youtube-nocookie.com/embed/${id}`)
  : null;
```

## DOM writes

Direct DOM writes skip the sanitizer: `nativeElement.innerHTML = ...`, `outerHTML`, `insertAdjacentHTML`, `document.write`. Use a template binding, `textContent`, or `Renderer2` with `setProperty(el, 'textContent', value)`.

## CSP and Trusted Types

- Give the Angular app a Content Security Policy. Two ways:
  - A nonce per response: the server sets `ngCspNonce="<nonce>"` on the root element, or provides the `CSP_NONCE` token, and sends the same nonce in the header.
  - `security.autoCsp` in `angular.json` for static hosting: hash-based, for scripts only, not with SSR.
- A start for a BFF-served app: `default-src 'self'; script-src 'self' 'nonce-<n>'; style-src 'self' 'nonce-<n>'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'`.
- Trusted Types: allow only the policy names that the app needs. `angular` for normal use. `angular#bundler` for lazy chunks. `angular#unsafe-bypass` only when the app calls `bypassSecurityTrust*`. `angular#unsafe-jit` for JIT. `angular#unsafe-upgrade` for an AngularJS hybrid.
- Trusted Types is Baseline in 2026. Start with `Content-Security-Policy-Report-Only` and fix the reports.

## XSRF with ASP.NET Core

- Angular `HttpClient` reads the cookie `XSRF-TOKEN` and sends the header `X-XSRF-TOKEN`. It does this only for mutating requests to relative or same-origin URLs.
- The server issues and validates the token: `AddAntiforgery(o => o.HeaderName = "X-XSRF-TOKEN")` and an endpoint that writes `GetAndStoreTokens(context).RequestToken` into a readable `XSRF-TOKEN` cookie. Code: `backend-dotnet.md`, section "Cookies, BFF and antiforgery".
- `UseAntiforgery` does not check DELETE. Validate it in the endpoint.

## Token storage

- Do not store tokens or session ids in `localStorage` or `sessionStorage`. Each XSS reads them.
- Default (SEC-03): BFF. The browser has only an `HttpOnly`, `Secure`, `SameSite` cookie. The BFF forwards API calls with the token.
- When the app must hold a token (static host, SEC-03 option B): keep the access token in memory only, with a short lifetime. Refresh through an `HttpOnly` cookie or a silent sign-in.

## AngularJS (legacy)

AngularJS has no security fixes since January 2022. Plan a migration. Until then:

- When an attacker controls a template or an expression, he has XSS in each version. The expression sandbox was removed in 1.6.
- Do not mix server templates (Razor) and AngularJS templates. A user name like `{{constructor.constructor('alert(1)')()}}` in server output runs as an expression. Put `ng-non-bindable` around server output with user data, or do not render user data on the server.
- Never pass user content to `$eval`, `$watch`, `$compile`, `$parse`, `$interpolate` or an `orderBy` expression.
- Use `ng-bind` or `{{ }}` for text. For HTML, load `ngSanitize` and bind with `ng-bind-html`, without `$sce.trustAsHtml`.
- Do not call `$sceProvider.enabled(false)`.
- Add a strict CSP as a second layer. With `ng-csp`, AngularJS does not use `eval`.

## Never write

| Wrong | Correct |
|-------|---------|
| `this.sanitizer.bypassSecurityTrustHtml(comment.body)` | `[innerHTML]="comment.body"` |
| `this.el.nativeElement.innerHTML = message` | `{{ message }}` or `textContent` |
| `localStorage.setItem('access_token', token)` | BFF cookie, or a token in a signal in memory |
| A template string built from user input | A component with bindings |
| `$scope.html = $sce.trustAsHtml(userText)` | `ng-bind-html` with `ngSanitize` |
| `@Model.Name` inside an AngularJS template | `ng-non-bindable` around server output |
| `script-src 'unsafe-inline' 'unsafe-eval'` | Nonce or `autoCsp` |
