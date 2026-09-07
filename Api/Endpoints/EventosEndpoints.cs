// Endpoints/EventosEndpoints.cs
using Application.Abstractions;
using Application.Contracts;
using Application.Domain.Enums;

namespace Api.Endpoints;

/// <summary>
/// API de leitura consumida pelo painel administrativo.
/// </summary>
/// <remarks>
/// Sem <c>FiltroApiKey</c> de propósito: a ApiKey autentica o <em>parceiro</em> que envia
/// webhooks, não o operador que consulta o painel. Reaproveitá-la aqui daria ao banco
/// parceiro acesso de leitura a todos os eventos. Em produção este grupo teria a sua
/// própria autenticação — está no README como evolução.
/// </remarks>
public static class EventosEndpoints
{
    public static IEndpointRouteBuilder MapEventosEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/eventos").WithTags("Eventos");

        grupo.MapGet("/", ListarEventos).WithName("ListarEventos");
        grupo.MapGet("/{id:guid}", ObterEvento).WithName("ObterEvento");

        return app;
    }

    private static async Task<IResult> ListarEventos(
        IRepositorioEventoWebhook repositorio,
        CancellationToken ct,
        StatusProcessamento? status = null,
        string? idContrato = null,
        int pagina = 1,
        int tamanhoPagina = FiltroEventos.TamanhoPaginaPadrao)
    {
        var filtro = new FiltroEventos
        {
            Status = status,
            IdContrato = idContrato,
            Pagina = pagina,
            TamanhoPagina = tamanhoPagina
        };

        return TypedResults.Ok(await repositorio.ListarAsync(filtro, ct));
    }

    private static async Task<IResult> ObterEvento(
        Guid id,
        IRepositorioEventoWebhook repositorio,
        CancellationToken ct)
    {
        var evento = await repositorio.ObterPorIdAsync(id, ct);

        return evento is null
            ? TypedResults.NotFound(new { erro = $"Evento {id} não encontrado." })
            : TypedResults.Ok(EventoDetalheResponse.De(evento));
    }
}
