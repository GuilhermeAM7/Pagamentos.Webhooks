using Application.Abstractions;
using Application.Domain.Entities;
using Application.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Persistence.Repositories
{
    public sealed class RepositorioEventoWebhook(AppDbContext contexto, TimeProvider relogio)
    : IRepositorioEventoWebhook
    {
        private const string ConstraintIdTransacao = "ux_eventos_webhook_id_transacao";

        public async Task<ResultadoInsercao> TentarAdicionarAsync(EventoWebhook evento, CancellationToken ct)
        {
            contexto.Eventos.Add(evento);

            try
            {
                await contexto.SaveChangesAsync(ct);
                return ResultadoInsercao.Inserido(evento.Id);
            }
            catch (DbUpdateException ex) when (EhDuplicidadeDeTransacao(ex))
            {
                // O evento falhou na inserção mas continua rastreado como Added.
                // Sem desanexar, o próximo SaveChanges deste escopo reenviaria o INSERT.
                contexto.Entry(evento).State = EntityState.Detached;

                var idExistente = await contexto.Eventos
                    .AsNoTracking()
                    .Where(e => e.IdTransacao == evento.IdTransacao)
                    .Select(e => (Guid?)e.Id)
                    .FirstOrDefaultAsync(ct);

                if (idExistente is null)
                    throw;   // violação de unicidade sem linha correspondente: não é o caso esperado

                return ResultadoInsercao.JaExistia(idExistente.Value);
            }
        }

        public Task<EventoWebhook?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
            contexto.Eventos.FirstOrDefaultAsync(e => e.Id == id, ct);

        public async Task<IReadOnlyList<Guid>> ObterTravadosAsync(
            TimeSpan limite, int maximo, CancellationToken ct)
        {
            var corte = relogio.GetUtcNow() - limite;

            return await contexto.Eventos
                .AsNoTracking()
                .Where(e => (e.Status == StatusProcessamento.Pendente
                          || e.Status == StatusProcessamento.EmProcessamento)
                         && e.DataRecebido < corte)
                .OrderBy(e => e.DataRecebido)
                .Take(maximo)
                .Select(e => e.Id)
                .ToListAsync(ct);
        }

        private static bool EhDuplicidadeDeTransacao(DbUpdateException ex) =>
            ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: ConstraintIdTransacao
            };
    }
}
