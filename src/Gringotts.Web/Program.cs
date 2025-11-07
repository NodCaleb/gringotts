using Gringotts.Contracts.Interfaces;
using Gringotts.Web.Components;
using Gringotts.Web.Internals;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();

builder.Services.AddScoped<AccessTokenStore>();
builder.Services.AddTransient<AuthenticatedHttpHandler>();

builder.Services.AddHttpClient("GringottsAuthClient", client =>
{
    client.BaseAddress = new Uri("http+https://gringotts-bff");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    // Include credentials so the refresh cookie is sent
    var handler = new HttpClientHandler
    {
        UseCookies = true
    };
    return handler;
});

builder.Services.AddHttpClient("GringottsBffClient", client =>
{
    client.BaseAddress = new Uri("http+https://gringotts-bff");
})
.AddHttpMessageHandler<AuthenticatedHttpHandler>();

// Typed client for communicating with the Gringotts BFF under the `/bff` endpoints
builder.Services.AddSingleton<IBffClient, BffClient>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
