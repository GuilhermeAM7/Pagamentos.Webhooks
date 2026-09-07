// DependencyInjection.cs
using Application.Services;
using Application.Workers;
using Microsoft.Extensions.DependencyInjection;
using Sabemi.Webhooks.TesteTecnico.Application.Validation;

namespace Application.DI;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<ValidadorWebhookPagamento>();
        services.AddScoped<ServicoRecebimentoWebhook>();
        services.AddScoped<ServicoProcessamentoPagamento>();
        services.AddHostedService<WorkerPagamento>();

        return services;
    }
}