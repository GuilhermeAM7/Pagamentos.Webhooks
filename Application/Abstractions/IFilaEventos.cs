using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Abstractions
{
    public interface IFilaEventos
    {
        ValueTask EnfileirarAsync(Guid eventoId, CancellationToken ct);

        IAsyncEnumerable<Guid> ConsumirAsync(CancellationToken ct);
    }
}
