using Application.Abstractions;
using Application.Domain.Entities;
using Application.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public sealed class ServicoProcessamentoPagamento(
    IRepositorioEventoWebhook repositorioEventos,
    IRepositorioStatusContrato repositorioContratos,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio,
    ILogger<ServicoProcessamentoPagamento> logger)
{
    private static readonly TimeSpan ProcessamentoPesado = TimeSpan.FromSeconds(2);

    public async Task ProcessarAsync(Guid eventoId, CancellationToken ct)
    {
        var evento = await repositorioEventos.ObterPorIdAsync(eventoId, ct);

        if (evento is null)
        {
            logger.LogWarning("Evento {EventoId} não encontrado.", eventoId);
            return;
        }

        if (evento.Status is StatusProcessamento.Concluido)
            return;   // idempotência do efeito: reprocessar não faz nada

        evento.DefinirProcessando();
        await unidadeDeTrabalho.SalvarAsync(ct);

        try
        {
            // Simulação da regra de negócio pesada exigida pelo enunciado.
            await Task.Delay(ProcessamentoPesado, relogio, ct);

            var agora = relogio.GetUtcNow();
            var contrato = await repositorioContratos.ObterPorContratoAsync(evento.IdContrato!, ct);

            if (contrato is null)
                repositorioContratos.Adicionar(StatusContrato.CriarAPartirDe(evento, agora));
            else if (!contrato.TentarAplicarPagamento(evento, agora))
                logger.LogInformation(
                    "Evento {EventoId} não aplicado: mais antigo que o estado consolidado.", eventoId);

            evento.DefinirConcluido(agora);

            // Contrato e evento saem no MESMO commit.
            await unidadeDeTrabalho.SalvarAsync(ct);

            logger.LogInformation("Evento {EventoId} processado.", eventoId);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;   // encerramento do host não é falha do evento
        }
        catch (Exception ex)
        {
            evento.DefinirFalha(ex.Message, permanente: false);
            await unidadeDeTrabalho.SalvarAsync(ct);

            logger.LogError(ex, "Falha ao processar evento {EventoId}.", eventoId);
        }
    }
}