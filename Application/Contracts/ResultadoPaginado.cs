namespace Application.Contracts;

public sealed record ResultadoPaginado<T>(
    IReadOnlyList<T> Itens,
    int Pagina,
    int TamanhoPagina,
    int Total)
{
    public int TotalPaginas => TamanhoPagina <= 0
        ? 0
        : (int)Math.Ceiling(Total / (double)TamanhoPagina);
}
