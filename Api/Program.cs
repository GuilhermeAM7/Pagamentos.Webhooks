using Api.DI;
using Api.Endpoints;
using Application.DI;
using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment()
    && builder.Configuration.GetConnectionString("Postgres") is null)
{
    throw new InvalidOperationException("Connection string 'Postgres' não configurada.");
}

builder.Services.AddSegurancaWebhook(builder.Configuration);
builder.Services.AddCorsPainel();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddOpenApiDocumentado();

var app = builder.Build();

app.UseSwaggerEmDesenvolvimento();

app.UseHttpsRedirection();

app.UseCorsPainel();

app.MapWebhookEndpoints();
app.MapEventosEndpoints();

app.Run();

public partial class Program { }
