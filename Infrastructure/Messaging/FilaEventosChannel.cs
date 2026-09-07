using System.Threading.Channels;
using Application.Abstractions;

namespace Infrastructure.Messaging;

public sealed class FilaEventosChannel : IFilaEventos
{
    private const int CapacidadeMaxima = 1_000;

    private readonly Channel<Guid> _canal = Channel.CreateBounded<Guid>(
        new BoundedChannelOptions(CapacidadeMaxima)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });

    public ValueTask EnfileirarAsync(Guid eventoId, CancellationToken ct) =>
        _canal.Writer.WriteAsync(eventoId, ct);

    public IAsyncEnumerable<Guid> ConsumirAsync(CancellationToken ct) =>
        _canal.Reader.ReadAllAsync(ct);
}