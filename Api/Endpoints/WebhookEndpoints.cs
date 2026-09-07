using System.Text.Json;
using Api.Filters;
using Application.Contracts;
using Application.Services;

namespace Api.Endpoints;

public static class WebhookEndpoints
{
    private static readonly JsonSerializerOptions OpcoesJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static IEndpointRouteBuilder MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/webhooks")
            .WithTags("Webhooks")
            .AddEndpointFilter<FiltroApiKey>();

        grupo.MapPost("/pagamento", ReceberPagamento)
             .WithName("ReceberWebhookPagamento");

        return app;
    }

    private static async Task<IResult> ReceberPagamento(
        HttpRequest requisicaoHttp,
        ServicoRecebimentoWebhook servico,
        CancellationToken ct)
    {
        string payloadJson;
        using (var leitor = new StreamReader(requisicaoHttp.Body, leaveOpen: true))
            payloadJson = await leitor.ReadToEndAsync(ct);

        if (string.IsNullOrWhiteSpace(payloadJson))
            return TypedResults.BadRequest(new { erro = "Corpo da requisição vazio." });

        RequisicaoWebhookPagamento? requisicao;
        try
        {
            requisicao = JsonSerializer.Deserialize<RequisicaoWebhookPagamento>(payloadJson, OpcoesJson);
        }
        catch (JsonException ex)
        {
            return TypedResults.BadRequest(new { erro = "JSON inválido.", detalhe = ex.Message });
        }

        if (requisicao is null || string.IsNullOrWhiteSpace(requisicao.IdTransacao))
            return TypedResults.BadRequest(new { erro = "id_transacao é obrigatório." });

        var resultado = await servico.ReceberAsync(requisicao, payloadJson, ct);

        return resultado.Situacao switch
        {
            SituacaoRecebimento.Aceito =>
                TypedResults.Accepted(
                    $"/api/eventos/{resultado.EventoId}",
                    new { eventoId = resultado.EventoId, situacao = "Aceito" }),

            SituacaoRecebimento.Duplicado =>
                TypedResults.Ok(
                    new { eventoId = resultado.EventoId, situacao = "Duplicado" }),

            SituacaoRecebimento.Rejeitado =>
                TypedResults.UnprocessableEntity(
                    new { eventoId = resultado.EventoId, situacao = "Rejeitado", erros = resultado.Erros }),

            _ => throw new InvalidOperationException($"Situação não tratada: {resultado.Situacao}")
        };
    }
}
