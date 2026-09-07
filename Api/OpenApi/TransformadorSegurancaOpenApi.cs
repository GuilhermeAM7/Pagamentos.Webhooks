using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Api.OpenApi;

public sealed class TransformadorSegurancaOpenApi : IOpenApiDocumentTransformer
{
    public const string NomeEsquema = "ApiKey";

    public const string Titulo = "Pagamentos.Webhooks";

    public Task TransformAsync(
        OpenApiDocument documento,
        OpenApiDocumentTransformerContext contexto,
        CancellationToken ct)
    {
        documento.Info.Title = Titulo;
        documento.Info.Description =
            "Recebe notificacoes de pagamento de um banco parceiro, garante idempotencia por "
            + "id_transacao, processa em background e expoe os eventos para consulta.";

        documento.Components ??= new OpenApiComponents();
        documento.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        documento.Components.SecuritySchemes[NomeEsquema] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = "X-Api-Key",
            Description = "Chave do banco parceiro. Em desenvolvimento: chave-local-de-desenvolvimento"
        };

        var referencia = new OpenApiSecuritySchemeReference(NomeEsquema, documento);

        foreach (var (rota, item) in documento.Paths)
        {
            if (!rota.StartsWith("/webhooks", StringComparison.Ordinal))
                continue;

            foreach (var operacao in item.Operations!.Values)
            {
                operacao.Security ??= [];
                operacao.Security.Add(new OpenApiSecurityRequirement { [referencia] = [] });
            }
        }

        return Task.CompletedTask;
    }
}
