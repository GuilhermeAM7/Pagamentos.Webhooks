using System.Text.Json;
using Application.Domain.Entities;

namespace Application.Contracts;

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
        JsonSerializer.Deserialize<JsonElement>(evento.PayloadJson));
}
