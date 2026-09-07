using Application.Abstractions;
using Infrastructure.Messaging;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services)
        {
            services.AddDbContext<AppDbContext>((sp, opcoes) =>
            {
                var cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")
                    ?? throw new InvalidOperationException("Connection string 'Postgres' não configurada.");

                opcoes.UseNpgsql(cs).UseSnakeCaseNamingConvention();
            });

            services.AddScoped<IRepositorioEventoWebhook, RepositorioEventoWebhook>();
            services.AddScoped<IRepositorioStatusContrato, RepositorioStatusContrato>();
            services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();

            services.AddSingleton<IFilaEventos, FilaEventosChannel>();

            services.AddSingleton<IValidadorApiKey, ValidadorApiKey>();

            return services;
        }
    }
}
