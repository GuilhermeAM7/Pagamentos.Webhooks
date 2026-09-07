using Application.Domain.Enums;

namespace Application.Contracts;

/// <summary>
/// Filtro da listagem do painel. Nunca é aplicado em memória: vira WHERE/LIMIT no banco.
/// </summary>
public sealed record FiltroEventos
{
    public const int TamanhoPaginaPadrao = 20;
    public const int TamanhoPaginaMaximo = 100;

    public StatusProcessamento? Status { get; init; }
    public string? IdContrato { get; init; }
    public int Pagina { get; init; } = 1;
    public int TamanhoPagina { get; init; } = TamanhoPaginaPadrao;

    /// <summary>
    /// Aplica o teto de página. Sem isso, `?tamanhoPagina=1000000` seria um DoS de um
    /// parâmetro só — o cliente escolhe o filtro, mas não escolhe o custo da consulta.
    /// </summary>
    public FiltroEventos Normalizar() => this with
    {
        IdContrato = string.IsNullOrWhiteSpace(IdContrato) ? null : IdContrato.Trim(),
        Pagina = Pagina < 1 ? 1 : Pagina,
        TamanhoPagina = TamanhoPagina switch
        {
            < 1 => TamanhoPaginaPadrao,
            > TamanhoPaginaMaximo => TamanhoPaginaMaximo,
            _ => TamanhoPagina
        }
    };
}
