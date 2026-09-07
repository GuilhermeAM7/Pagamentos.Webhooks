using Application.Abstractions;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.Workers;

public sealed class WorkerPagamento(
    IFilaEventos fila,
    IServiceScopeFactory fabricaDeEscopos,
    ILogger<WorkerPagamento> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("WorkerPagamento iniciado.");

        await foreach (var eventoId in fila.ConsumirAsync(stoppingToken))
        {
            try
            {
                using var escopo = fabricaDeEscopos.CreateScope();

                var servico = escopo.ServiceProvider
                    .GetRequiredService<ServicoProcessamentoPagamento>();

                await servico.ProcessarAsync(eventoId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro não tratado no evento {EventoId}.", eventoId);
            }
        }

        logger.LogInformation("WorkerPagamento encerrado.");
    }
}
