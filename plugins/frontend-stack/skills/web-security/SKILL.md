---
name: web-security
description: Security rules for browser code in Angular 22 and AngularJS 1.8 apps with an ASP.NET Core backend - the Angular sanitizer and bypassSecurityTrust, direct DOM writes, Content Security Policy with a nonce or autoCsp, Trusted Types, XSRF with the X-XSRF-TOKEN header, token storage in the browser, and AngularJS expression injection. Use when browser code renders user data as HTML, calls bypassSecurityTrust, writes innerHTML, stores a token or session id, sets a CSP or Trusted Types, sends a state change from the browser, or touches AngularJS templates, or when the user asks for a security review of front-end code. Not for the server side of sign-in, cookies, antiforgery, CORS, headers, or the supply chain (dev-stack:secure-coding), or component code without a security question (frontend-stack:angular).
metadata:
  verified: "2026-10-27"
  versions: "Angular 22, AngularJS 1.8, OWASP Top 10:2025, ASVS 5.0.0, CWE Top 25:2025"
---

# Web security

Security rules for code that runs in the browser. Component and service use is in `frontend-stack:angular`. The server side of sign-in, cookies, antiforgery, CORS, and headers is in `dev-stack:secure-coding`. This skill adds what makes the browser code safe.

## First

1. Read `references/stack-rules.md`.
2. Read the versions in `package.json`: `@angular/core` or `angular` 1.x. The rules are for Angular 22 and AngularJS 1.8.
3. Name each place where user data reaches the DOM, and each place where the browser keeps a credential. Done when you can name each place and its control.
4. Read `references/frontend.md` for the detail of each rule below.

## Defaults for new code

The decisions are in `dev-stack:secure-coding` (`references/decisions.md`). When that skill is not installed, use these defaults and say so in the report.

| Area | Default | ID |
|------|---------|----|
| Sign-in of the Angular app | BFF: OIDC on the server, `HttpOnly` cookie, antiforgery | SEC-03 |
| CORS | None: one origin through the BFF | SEC-09 |
| Headers and CSP | nginx for static files, middleware for APIs. CSP with a nonce or `autoCsp` | SEC-10 |

## Rules

1. Bind user data with interpolation or `[innerHTML]`, and let the Angular sanitizer work. The sanitizer removes scripts and unsafe URLs.
2. Call `bypassSecurityTrust*` only on a value that the code built from validated parts. An example is a fixed host and an id that matches a strict pattern. Each call is a review item.
3. Write text to the DOM with a template binding, `textContent`, or `Renderer2.setProperty(el, 'textContent', value)`. Direct writes (`innerHTML`, `outerHTML`, `insertAdjacentHTML`, `document.write`) skip the sanitizer.
4. Keep tokens and session ids out of `localStorage` and `sessionStorage`. Each XSS reads them. With the BFF, the browser has only an `HttpOnly` cookie. When the app must hold a token, keep it in memory with a short lifetime.
5. Give the app a Content Security Policy: a nonce per response (`ngCspNonce` or `CSP_NONCE`), or `security.autoCsp` for static hosting. Start with `Content-Security-Policy-Report-Only` and fix the reports.
6. With Trusted Types, allow only the policy names that the app needs: `angular`, and `angular#bundler` for lazy chunks. Add an `unsafe` policy name only for the feature that needs it.
7. Let `HttpClient` send the XSRF header. It reads the cookie `XSRF-TOKEN` and sends `X-XSRF-TOKEN` for mutating requests to the same origin. The server must issue and check the token.
8. Never build an Angular template from user input. Keep AOT on for production builds.
9. AngularJS has had no security fixes since January 2022. Until the migration, do these steps:
   - Put `ng-non-bindable` around server output with user data.
   - Bind HTML with `ng-bind-html` and `ngSanitize`.
   - Never pass user data to `$eval`, `$compile`, `$parse`, `$interpolate`, or `$sce.trustAs*`.

## Never write

| Wrong (what an LLM writes) | Correct |
|----------------------------|---------|
| `this.sanitizer.bypassSecurityTrustHtml(comment.body)` | `[innerHTML]="comment.body"` |
| `this.el.nativeElement.innerHTML = message` | `{{ message }}` or `textContent` |
| `localStorage.setItem('access_token', token)` | BFF cookie, or a token in a signal in memory |
| A template string built from user input | A component with bindings |
| `$scope.html = $sce.trustAsHtml(userText)` | `ng-bind-html` with `ngSanitize` |
| `@Model.Name` inside an AngularJS template | `ng-non-bindable` around server output |
| `script-src 'unsafe-inline' 'unsafe-eval'` | Nonce or `autoCsp` |

## Check

Run these after each change. Fix each error. Fix each warning or give the reason to keep it.

1. The rule check of this skill. A pass is `0 errors`:

   ```
   python3 <skill folder>/scripts/stackcheck.py --root <repo> --changed <base branch>
   ```

2. Dependencies. A pass is "found 0 vulnerabilities": `npm audit --omit=dev`.
3. In the browser, open the app with the CSP header. A pass is no CSP report for the app's own scripts and styles.

## Review checklist

1. No new `bypassSecurityTrust*` on data that the code did not build from validated parts. `tool (SC-TS-BYPASS-TRUST)` + `judge`
2. No `innerHTML` or `outerHTML` writes, `insertAdjacentHTML`, or `document.write`. `tool (SC-TS-INNERHTML, SC-TS-DOM-WRITE)`
3. No tokens or session ids in `localStorage` or `sessionStorage`. `tool (SC-TS-TOKEN-STORAGE)`
4. The XSRF cookie and header pair works with the server. `judge`
5. A CSP exists, and Trusted Types list only the policy names that the app needs. `judge`
6. No Angular template from user input, and AOT in production. `judge`
7. AngularJS: no user data in server templates without `ng-non-bindable`, no `$sce.trustAs*` or `$eval` on user input. `tool (SC-NG1-TRUST-HTML)` + `judge`

## Report

End with the report of stack rule 8 (`references/stack-rules.md`). A security review uses the verdict words and the findings columns of the security reviewer of `dev-stack:secure-coding`.

## References

- `references/frontend.md`: the sanitizer, `bypassSecurityTrust`, DOM writes, CSP and Trusted Types, XSRF, token storage, AngularJS, with code. Read for each change.
