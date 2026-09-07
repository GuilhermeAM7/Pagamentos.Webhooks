using Application.Abstractions;

namespace Api.Filters;

public sealed class FiltroApiKey(
    IValidadorApiKey validador, ILogger<FiltroApiKey> logger) : IEndpointFilter
{
    public const string NomeCabecalho = "X-Api-Key";

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext contexto, EndpointFilterDelegate proximo)
    {
        var chave = contexto.HttpContext.Request.Headers[NomeCabecalho].FirstOrDefault();

        var resultado = validador.Validar(chave);

        if (resultado is not ResultadoAutenticacao.Valida)
        {
            logger.LogWarning("Requisição rejeitada em {Caminho}: {Motivo}",
                contexto.HttpContext.Request.Path, resultado);

            return TypedResults.Unauthorized();
        }

        return await proximo(contexto);
    }
}