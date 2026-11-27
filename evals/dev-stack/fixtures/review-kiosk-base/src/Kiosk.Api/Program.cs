using Kiosk.Api.Account;
using Kiosk.Api.Admin;
using Kiosk.Api.B2B;
using Kiosk.Api.Basket;
using Kiosk.Api.Catalog;
using Kiosk.Api.Chatbot;
using Kiosk.Api.Data;
using Kiosk.Api.Files;
using Kiosk.Api.Hosting;
using Kiosk.Api.Mail;
using Kiosk.Api.Orders;
using Kiosk.Api.Reviews;
using Kiosk.Api.Support;
using Kiosk.Api.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKioskData(builder.Configuration);
builder.Services.AddKioskAuthentication(builder.Configuration);
builder.Services.AddKioskAuthorization();
builder.Services.AddKioskCors();
builder.Services.AddKioskForwardedHeaders();
builder.Services.AddKioskRateLimiting(builder.Configuration);
builder.Services.AddKioskOutputCache();
builder.Services.AddKioskRequestLogging(builder.Configuration, builder.Environment);
builder.Services.AddKioskDataProtection(builder.Configuration);
builder.Services.AddKioskErrorHandling();
builder.Services.AddAntiforgery();
builder.Services.AddSignalR();
builder.Services.AddDirectoryBrowser();
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

builder.Services.AddMail();
builder.Services.AddUsers(builder.Configuration);
builder.Services.AddCatalog();
builder.Services.AddBasket();
builder.Services.AddOrders(builder.Configuration);
builder.Services.AddReviews();
builder.Services.AddChatbot();
builder.Services.AddFiles(builder.Configuration);
builder.Services.AddB2b();
builder.Services.AddAdmin();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseKioskErrorHandling();
app.UseKioskRequestLogging();
app.UseKioskStaticContent();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseAntiforgery();
app.UseOutputCache();

app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapPasswordResetEndpoints();
app.MapAccountEndpoints();
app.MapStaffAccountEndpoints();
app.MapRedirectEndpoints();
app.MapProductEndpoints();
app.MapBasketEndpoints();
app.MapOrderEndpoints();
app.MapReviewEndpoints();
app.MapChatEndpoints();
app.MapHub<SupportHub>("/hubs/support");
app.MapFileEndpoints();
app.MapB2bEndpoints();
app.MapAdminEndpoints();
app.MapMetricsEndpoints();

await app.InitializeDatabaseAsync();

await app.RunAsync();

public partial class Program;
