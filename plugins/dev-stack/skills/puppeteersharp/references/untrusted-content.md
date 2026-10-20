# Untrusted content

A page that Chrome renders on the server runs inside your network. Each URL in the HTML is a request from the server. Treat HTML with user text (a note, a name, an address, a product description from a supplier) as untrusted.

## Contents

- What can go wrong
- Encode
- Block the network
- Remote assets (the logo)
- URLs from a request
- Order of the defenses

## What can go wrong

Checked with chrome-headless-shell 155 and a local HTTP listener: a note with `<img>`, `<link rel="stylesheet">`, `<script src>`, `<iframe>`, and `<object>` that point to `http://127.0.0.1:<port>/` gave a request for each tag when the note was not encoded and the network was open.

- SSRF: the server requests internal services, admin pages, or the cloud metadata endpoint. An `<iframe>` can show the answer in the PDF.
- Local files: a page that Chrome opens from a `file:` URL, or a navigation that a request controls, can show files of the container.
- Layout: markup in the note can hide lines, add pages, or fake a total.

## Encode

Encode each value at the point where it enters the HTML:

```csharp
var html = HtmlEncoder.Default;
sb.Append(CultureInfo.InvariantCulture, $"<p class=\"note\">{html.Encode(note)}</p>");
```

- Razor components with `HtmlRenderer` encode by default. Do not use `MarkupString` for user text.
- Keep line breaks with CSS (`white-space: pre-wrap`), not by inserting `<br>` into the user text.
- Checked: with encoding, a note of ten `<div style="page-break-before:always">` elements stays text on one page. Without encoding, each element starts a new page.

## Block the network

Use one of these two. Both were checked: zero requests to the listener.

1. `LaunchOptions.Allowlist` (Chrome 149 and later, CDP only). The browser fails each request whose URL does not match a pattern:

   ```csharp
   Allowlist = ["data:*", "about:blank"],
   ```

2. Request interception, for older Chrome builds or a rule per page:

   ```csharp
   await page.SetRequestInterceptionAsync(true);
   page.Request += async (_, e) =>
   {
       var url = e.Request.Url;
       if (url.StartsWith("data:", StringComparison.Ordinal) || url == "about:blank")
       {
           await e.Request.ContinueAsync();
       }
       else
       {
           await e.Request.AbortAsync();
       }
   };
   ```

Also turn JavaScript off (`SetJavaScriptEnabledAsync(false)`) and add a Content Security Policy:

```html
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src data:; style-src 'unsafe-inline'">
```

## Remote assets (the logo)

A blocked network also blocks your own logo on a CDN. Choose one:

- Inline it: an embedded resource or a file in the image, sent as a `data:image/png;base64,...` URI. This is the default.
- Download it one time at startup or at build time with `HttpClient`, then inline it.
- Allow only that host in the allow list (`"https://cdn.example.com/*"`). Use this only when the asset changes often. The host must not serve user content.

Fonts: install them in the image (`fonts-liberation`, `fonts-noto-core`) or inline them with `@font-face` and a data URI.

## URLs from a request

An endpoint that renders "any page" (`?url=`) is an SSRF tool. Do not build it. When a page must come from a URL:

- Allow only a fixed list of `https` hosts. Compare the host of the parsed `Uri`, not a string prefix.
- Resolve the host and refuse private, loopback, and link-local addresses.
- Never allow `file:`, `chrome:`, `data:` navigation from a request.
- Require authorization, and log who rendered which URL.

## Order of the defenses

Use all of them. Each one covers a gap in the others:

1. Encode the user text (layout and markup).
2. Block the network (SSRF, also from a template bug).
3. JavaScript off and a CSP (scripts in own templates).
4. A separate renderer container with egress blocked (OPS-03, SEC-21).
