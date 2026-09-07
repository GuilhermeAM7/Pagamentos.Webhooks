using Api.OpenApi;
using Infrastructure.Security;

namespace Api.DI;

public static class DependencyInjection
{
    private const string PoliticaCorsPainel = "painel";

    private const string OrigemPainel = "http://localhost:5173";

    private const string CaminhoDocumentoOpenApi = "/openapi/v1.json";

    public static IServiceCollection AddSegurancaWebhook(
        this IServiceCollection services, IConfiguration configuracao)
    {
        services.AddOptions<OpcoesSegurancaWebhook>()
            .Bind(configuracao.GetSection(OpcoesSegurancaWebhook.Secao))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey), "ApiKey do webhook não configurada.")
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddCorsPainel(this IServiceCollection services)
    {
        services.AddCors(opcoes => opcoes.AddPolicy(PoliticaCorsPainel, politica => politica
            .WithOrigins(OrigemPainel)
            .AllowAnyHeader()
            .AllowAnyMethod()));

        return services;
    }

    public static WebApplication UseCorsPainel(this WebApplication app)
    {
        app.UseCors(PoliticaCorsPainel);

        return app;
    }

    public static IServiceCollection AddOpenApiDocumentado(this IServiceCollection services)
    {
        services.AddOpenApi(opcoes =>
            opcoes.AddDocumentTransformer<TransformadorSegurancaOpenApi>());

        return services;
    }

    public static WebApplication UseSwaggerEmDesenvolvimento(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return app;

        app.MapOpenApi();
        app.UseSwaggerUI(opcoes =>
        {
            opcoes.SwaggerEndpoint(CaminhoDocumentoOpenApi, $"{TransformadorSegurancaOpenApi.Titulo} v1");
            opcoes.DocumentTitle = TransformadorSegurancaOpenApi.Titulo;
        });

        return app;
    }
}
