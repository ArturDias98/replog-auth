using Amazon.Lambda.AspNetCoreServer.Hosting;
using replog_api_auth.Auth;
using replog_api_auth.Endpoints;
using replog_api_auth.Interfaces;
using replog_api_auth.Repositories;
using replog_api_auth.Settings;
using replog_api_auth_core;
using replog_api_host;
using replog_api_host.Endpoints;
using replog_api_host.Middleware;
using replog_infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
if (builder.Environment.IsDevelopment())
    builder.Logging.AddConsole();
else
    builder.Logging.AddJsonConsole();

await SecretsLoader.LoadFromSecretsManagerAsync(builder);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddReplogCors(builder.Environment);

builder.Services.Configure<GoogleAuthSettings>(builder.Configuration.GetSection("Google"));
builder.Services.AddSingleton<IGoogleTokenValidator, GoogleTokenValidator>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddDynamoDb(builder.Configuration);
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandler>();
app.UseCors();

app.MapAuthEndpoints();
app.MapHealthEndpoint("/api/auth/health");

app.Run();

namespace replog_api_auth
{
    public partial class Program { }
}
