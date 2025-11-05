using Gringotts.Web;
using Gringotts.Web.Components;
using Gringotts.Contracts.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();

// Typed client for communicating with the Gringotts BFF under the `/bff` endpoints
builder.Services.AddHttpClient<IBffClient, BffClient>(client =>
{
    // This uses the same service discovery scheme pattern as other projects in the solution.
    client.BaseAddress = new Uri("http+https://gringotts-bff");
});

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
