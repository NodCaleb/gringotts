using Gringotts.BFF.Configuration;
using Gringotts.BFF.Endpoints;
using Gringotts.BFF.Internals;
using Gringotts.Contracts.Interfaces;
using Gringotts.Infrastructure.Bootstrapping;
using Gringotts.Infrastructure.Clients;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRedisDistributedCache(connectionName: "cache");

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
builder.Services.AddSingleton(jwtOptions);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                // Logs the raw Authorization header when present
                if (!string.IsNullOrEmpty(ctx.Token))
                    ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                       .CreateLogger("JWT").LogDebug("Token received ({Length} chars)", ctx.Token.Length);
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = ctx =>
            {
                ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                   .CreateLogger("JWT").LogError(ctx.Exception, "JWT auth failed");
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                   .CreateLogger("JWT");
                var name = ctx.Principal?.Identity?.Name ?? "(no name)";
                logger.LogInformation("JWT validated for {Name}", name);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// CORS for the Blazor origin
builder.Services.AddCors(o => o.AddPolicy("ui", policy =>
{
    policy
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();

    if (builder.Environment.IsDevelopment())
    {
        policy.SetIsOriginAllowed(origin =>
            new Uri(origin).Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
            origin.StartsWith("https://localhost")
        );
    }
    else
    {
        policy.SetIsOriginAllowed(origin =>
        {
            var uri = new Uri(origin);
            return uri.Host.EndsWith(".azurewebsites.net", StringComparison.OrdinalIgnoreCase)
                || uri.Host.EndsWith(".azurecontainerapps.io", StringComparison.OrdinalIgnoreCase);
        });
    }

}));

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register IHttpClientFactory and the ApiClient implementation for IApiClient
builder.Services.AddHttpClient("GringottsApiClient", client =>
{
    client.BaseAddress = new Uri("http+https://gringotts-api");
});

builder.Services.AddSingleton<IApiClient, ApiClient>();

builder.Services.AddCache("Redis");

builder.Services.AddScoped<TokensService>();

builder.Services.AddProblemDetails();

// Configure OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Gringotts BFF",
        Version = "v1",
        Description = "Backend for the Gringotts Web UI"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gringotts BFF V1");
    });
}

app.MapAuthEndpoints();

app.MapCustomersEndpoints();

app.MapTransactionsEndpoints();

app.MapDefaultEndpoints();

app.UseHttpsRedirection();

app.UseCors("ui");

app.UseAuthentication();

app.UseAuthorization();

app.Run();
