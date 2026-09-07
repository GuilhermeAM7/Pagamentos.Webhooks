using System.Text.Json;
using Application.Domain.Entities;

namespace Application.Contracts;

/// <summary>
/// Detalhe de um evento. Diferente do resumo, inclui o corpo original recebido —
/// é o que permite auditar exatamente o que o parceiro mandou quando algo falhou.
/// </summary>
public sealed record EventoDetalheResponse(
    Guid Id,
    string IdTransacao,
    string? IdContrato,
    decimal? Valor,
    DateTimeOffset? DataPagamento,
    DateTimeOffset DataRecebido,
    DateTimeOffset? DataProcessado,
    string? StatusOrigem,
    string Status,
    string? StatusPagamento,
    int Tentativas,
    string? UltimoErro,
    JsonElement Payload)
{
    public static EventoDetalheResponse De(EventoWebhook evento) => new(
        evento.Id,
        evento.IdTransacao,
        evento.IdContrato,
        evento.Valor,
        evento.DataPagamento,
        evento.DataRecebido,
        evento.DataProcessado,
        evento.StatusOrigem,
        evento.Status.ToString(),
        evento.StatusPagamento?.ToString(),
        evento.Tentativas,
        evento.UltimoErro,
        // O payload é devolvido como JSON, não como string escapada: o corpo foi gravado
        // íntegro em jsonb, devolvê-lo entre aspas obrigaria o painel a fazer parse duas vezes.
        JsonSerializer.Deserialize<JsonElement>(evento.PayloadJson));
}
