using Application.Contracts;
using Application.Domain.Entities;

namespace Application.Abstractions
{
    public interface IRepositorioEventoWebhook
    {
        Task<ResultadoInsercao> TentarAdicionarAsync(EventoWebhook evento, CancellationToken ct);

        Task<EventoWebhook?> ObterPorIdAsync(Guid id, CancellationToken ct);

        Task<IReadOnlyList<Guid>> ObterTravadosAsync(TimeSpan limite, int maximo, CancellationToken ct);

        Task<ResultadoPaginado<EventoResumoResponse>> ListarAsync(FiltroEventos filtro, CancellationToken ct);
    }
}
