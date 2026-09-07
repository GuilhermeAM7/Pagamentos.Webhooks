using Application.Contracts;
using Application.Domain.Entities;

namespace Application.Abstractions
{
    public interface IRepositorioEventoWebhook
    {
        Task<ResultadoInsercao> TentarAdicionarAsync(EventoWebhook evento, CancellationToken ct);

        Task<EventoWebhook?> ObterPorIdAsync(Guid id, CancellationToken ct);

        Task<IReadOnlyList<Guid>> ObterTravadosAsync(TimeSpan limite, int maximo, CancellationToken ct);

        /// <summary>
        /// Listagem do painel. Recebe o filtro cru e o normaliza internamente — a garantia
        /// do teto de página não pode depender de quem chama lembrar de aplicá-lo.
        /// </summary>
        Task<ResultadoPaginado<EventoResumoResponse>> ListarAsync(FiltroEventos filtro, CancellationToken ct);
    }
}
