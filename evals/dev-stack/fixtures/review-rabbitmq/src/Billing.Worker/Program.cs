using Billing.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<InvoiceStore>();
builder.Services.AddHostedService<OrderPlacedConsumer>();

var host = builder.Build();
await host.RunAsync();
