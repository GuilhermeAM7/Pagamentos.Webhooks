using Api.Endpoints;
using Application.DI;
using Infrastructure;
using Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment()
    && builder.Configuration.GetConnectionString("Postgres") is null)
{
    throw new InvalidOperationException("Connection string 'Postgres' não configurada.");
}

builder.Services.AddOptions<OpcoesSegurancaWebhook>()
    .Bind(builder.Configuration.GetSection(OpcoesSegurancaWebhook.Secao))
    .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey), "ApiKey do webhook não configurada.")
    .ValidateOnStart();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapWebhookEndpoints();

app.Run();

public partial class Program { }