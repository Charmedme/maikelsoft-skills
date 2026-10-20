# Tests for a Chrome renderer

Test the PDF, the blocked network, the encoding, and the timeout with the real `chrome-headless-shell`. A mock of `IPage` proves nothing about Chrome.

## Contents

- Chrome in tests and CI
- A test with a local listener
- What to assert

## Chrome in tests and CI

- Install the pinned `chrome-headless-shell` in the CI image (the same build as the Dockerfile) and set `PUPPETEER_EXECUTABLE_PATH`. Do not download it in the test.
- CI agents often run as root. Chrome then refuses to start with the sandbox on. Do not add `--no-sandbox` to the app. Run the tests as a non-root user, or point `PUPPETEER_EXECUTABLE_PATH` at a wrapper script that only the CI uses:

  ```sh
  #!/bin/sh
  # CI only: Chrome refuses root with the sandbox on. The app keeps the sandbox on.
  if [ "$(id -u)" = "0" ]; then exec /opt/chrome-headless-shell/chrome-headless-shell --no-sandbox "$@"; fi
  exec /opt/chrome-headless-shell/chrome-headless-shell "$@"
  ```

- Prove the sandbox in the shipped image with a smoke test (`container.md`): the integration tests do not.

## A test with a local listener

```csharp
[TestMethod]
public async Task Invoice_NoteWithInternalUrls_MakesNoRequest()
{
    var hits = new ConcurrentQueue<string>();
    var port = FreePort();
    using var listener = new HttpListener();
    listener.Prefixes.Add($"http://127.0.0.1:{port}/");
    listener.Start();
    _ = Task.Run(async () =>
    {
        while (listener.IsListening)
        {
            try
            {
                var context = await listener.GetContextAsync();
                hits.Enqueue(context.Request.Url!.AbsolutePath);
                context.Response.Close();
            }
            catch (HttpListenerException)
            {
                return;
            }
        }
    });

    var note = $"<img src=\"http://127.0.0.1:{port}/img\"><link rel=\"stylesheet\" href=\"http://127.0.0.1:{port}/css\">";
    var pdf = await _renderer.RenderAsync(InvoiceTemplate.Render(OrderWithNote(note)), TestContext.CancellationToken);

    Assert.AreEqual("%PDF-", Encoding.ASCII.GetString(pdf, 0, 5));
    Assert.IsTrue(hits.IsEmpty, "Requests: " + string.Join(", ", hits));
}

private static int FreePort()
{
    using var socket = new TcpListener(IPAddress.Loopback, 0);
    socket.Start();
    return ((IPEndPoint)socket.LocalEndpoint).Port;
}
```

## What to assert

| Test | Assert |
|------|--------|
| A plain order | Status 200, `application/pdf`, the body starts with `%PDF-`. |
| A note with `<img>`, `<link>`, `<script src>`, `<iframe>` to a local listener | Zero requests to the listener. |
| A note with markup (ten `<div style="page-break-before:always">`) | Fewer than five pages: count `/Type /Page` in the PDF bytes (Latin-1). |
| A template that never finishes (a `WaitForSelectorAsync` for a missing element) | A `WaitTaskTimeoutException` (a `PuppeteerException`, not a `TimeoutException`) within the configured timeout. The endpoint maps it to a 5xx ProblemDetails. |
| Parallel renders above the limit | All succeed, at most N pages at a time. |
| A unit test of the template | `HtmlEncoder` output: `&lt;b&gt;` for `<b>`. |
