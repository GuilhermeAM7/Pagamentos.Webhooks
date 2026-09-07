namespace Application.Contracts;

public sealed record EventoResumoResponse(
    Guid Id,
    string IdTransacao,
    string? IdContrato,
    decimal? Valor,
    DateTimeOffset? DataPagamento,
    DateTimeOffset DataRecebido,
    DateTimeOffset? DataProcessado,
    string Status,
    string? StatusPagamento,
    int Tentativas,
    string? UltimoErro);
