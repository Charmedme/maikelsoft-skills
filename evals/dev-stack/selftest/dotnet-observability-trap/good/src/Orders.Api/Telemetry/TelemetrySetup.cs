using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Orders.Api.Telemetry;

public static class TelemetrySetup
{
    public static WebApplicationBuilder AddOrdersTelemetry(this WebApplicationBuilder builder)
    {
        builder.Logging.EnableRedaction();
        builder.Services.AddRedaction(redaction =>
            redaction.SetRedactor<ErasingRedactor>(new DataClassificationSet(DataTaxonomy.Personal)));
        builder.Services.AddSingleton<OrdersTelemetry>();

        // The distro throws at start without a connection string. Local runs and tests have none.
        if (string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        {
            return builder;
        }

        var sampling = builder.Configuration.GetSection("Telemetry");
        builder.Services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddSource(OrdersTelemetry.Name))
            .WithMetrics(metrics => metrics.AddMeter(OrdersTelemetry.Name))
            .UseMicrosoftOpenTelemetry(options =>
            {
                options.Exporters = ExportTarget.AzureMonitor;
                options.AzureMonitor.SamplingRatio = sampling.GetValue("SamplingRatio", 1.0F);
                options.AzureMonitor.TracesPerSecond = sampling.GetValue<double?>("TracesPerSecond");
            });
        return builder;
    }
}
