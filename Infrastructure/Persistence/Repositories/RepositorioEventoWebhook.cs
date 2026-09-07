using Application.Abstractions;
using Application.Contracts;
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
                contexto.Entry(evento).State = EntityState.Detached;

                var idExistente = await contexto.Eventos
                    .AsNoTracking()
                    .Where(e => e.IdTransacao == evento.IdTransacao)
                    .Select(e => (Guid?)e.Id)
                    .FirstOrDefaultAsync(ct);

                if (idExistente is null)
                    throw;

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

        public async Task<ResultadoPaginado<EventoResumoResponse>> ListarAsync(
            FiltroEventos filtro, CancellationToken ct)
        {
            var f = filtro.Normalizar();

            var consulta = contexto.Eventos.AsNoTracking();

            if (f.Status is { } status)
                consulta = consulta.Where(e => e.Status == status);

            if (f.IdContrato is { } contrato)
                consulta = consulta.Where(e => e.IdContrato == contrato);

            var total = await consulta.CountAsync(ct);

            var linhas = await consulta
                .OrderByDescending(e => e.DataRecebido)
                .ThenBy(e => e.Id)
                .Skip((f.Pagina - 1) * f.TamanhoPagina)
                .Take(f.TamanhoPagina)
                .Select(e => new
                {
                    e.Id,
                    e.IdTransacao,
                    e.IdContrato,
                    e.Valor,
                    e.DataPagamento,
                    e.DataRecebido,
                    e.DataProcessado,
                    e.Status,
                    e.StatusPagamento,
                    e.Tentativas,
                    e.UltimoErro
                })
                .ToListAsync(ct);

            var itens = linhas.ConvertAll(l => new EventoResumoResponse(
                l.Id,
                l.IdTransacao,
                l.IdContrato,
                l.Valor,
                l.DataPagamento,
                l.DataRecebido,
                l.DataProcessado,
                l.Status.ToString(),
                l.StatusPagamento?.ToString(),
                l.Tentativas,
                l.UltimoErro));

            return new ResultadoPaginado<EventoResumoResponse>(itens, f.Pagina, f.TamanhoPagina, total);
        }

        private static bool EhDuplicidadeDeTransacao(DbUpdateException ex) =>
            ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: ConstraintIdTransacao
            };
    }
}
