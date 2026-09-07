namespace Application.Contracts;

/// <summary>
/// Linha da listagem do painel. Deliberadamente NÃO expõe o payload bruto: a listagem
/// não deve arrastar um jsonb por linha só para exibir uma tabela.
/// </summary>
/// <remarks>
/// Os status vão como <see cref="string"/> e não como enum porque o System.Text.Json
/// serializa enum como número por padrão — o painel receberia <c>"status": 3</c> em vez
/// de <c>"status": "Falha"</c>.
/// </remarks>
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
