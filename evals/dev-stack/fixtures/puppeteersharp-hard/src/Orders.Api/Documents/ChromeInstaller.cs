using Microsoft.Extensions.Options;
using PuppeteerSharp;

namespace Orders.Api.Documents;

// Makes sure that a browser is present before the first document is rendered.
public sealed class ChromeInstaller(IOptions<DocumentOptions> options, ILogger<ChromeInstaller> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (options.Value.ChromePath is not null || Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH") is not null)
        {
            return;
        }

        var installed = await new BrowserFetcher().DownloadAsync();
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Chrome {BuildId} is ready.", installed.BuildId);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
