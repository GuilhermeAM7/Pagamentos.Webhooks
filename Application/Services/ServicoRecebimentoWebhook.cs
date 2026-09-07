using Application.Abstractions;
using Application.Contracts;
using Application.Domain.Entities;
using Microsoft.Extensions.Logging;
using Sabemi.Webhooks.TesteTecnico.Application.Validation;

namespace Application.Services;

public sealed class ServicoRecebimentoWebhook(
    IRepositorioEventoWebhook repositorio,
    ValidadorWebhookPagamento validador,
    TimeProvider relogio,
    ILogger<ServicoRecebimentoWebhook> logger,
    IFilaEventos fila)
{
    public async Task<ResultadoRecebimento> ReceberAsync(
        RequisicaoWebhookPagamento requisicao, string payloadJson, CancellationToken ct)
    {
        var evento = EventoWebhook.Criar(requisicao, payloadJson, relogio.GetUtcNow());

        var validacao = validador.Validar(requisicao);
        if (!validacao.IsValid)
            evento.DefinirFalha(validacao.Mensagem, permanente: true);

        var resultado = await repositorio.TentarAdicionarAsync(evento, ct);

        if (resultado.Duplicado)
        {
            logger.LogInformation(
                "Evento {IdTransacao} já recebido anteriormente ({EventoId}).",
                evento.IdTransacao, resultado.EventoId);

            return ResultadoRecebimento.Duplicado(resultado.EventoId);
        }

        if (!validacao.IsValid)
        {
            logger.LogWarning(
                "Evento {EventoId} gravado como Falhou: {Erros}",
                evento.Id, validacao.Mensagem);

            return ResultadoRecebimento.Rejeitado(evento.Id, validacao.Mensagem);
        }

        await fila.EnfileirarAsync(evento.Id, ct);

        logger.LogInformation("Evento {EventoId} aceito ({IdTransacao}).",
            evento.Id, evento.IdTransacao);

        return ResultadoRecebimento.Aceito(evento.Id);
    }
}
