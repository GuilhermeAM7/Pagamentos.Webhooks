using Application.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Abstractions
{
    public interface IRepositorioEventoWebhook
    {
        Task<ResultadoInsercao> TentarAdicionarAsync(EventoWebhook evento, CancellationToken ct);

        Task<EventoWebhook?> ObterPorIdAsync(Guid id, CancellationToken ct);

        Task<IReadOnlyList<Guid>> ObterTravadosAsync(TimeSpan limite, int maximo, CancellationToken ct);

    }
}
