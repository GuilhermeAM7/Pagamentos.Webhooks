
using Application.Contracts;
using Application.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Domain.Entities
{
    public class EventoWebhook
    {
        public Guid Id { get; private set; }
        public string IdTransacao { get; private set; }
        public string? IdContrato { get; private set; }
        public decimal? Valor { get; private set; }
        public DateTimeOffset? DataPagamento { get; private set; }
        public DateTimeOffset DataRecebido { get; private set; }
        public DateTimeOffset? DataProcessado { get; private set; }
        public string? StatusOrigem { get; private set; }
        public StatusProcessamento Status { get; private set; }
        public PagamentoStatus? StatusPagamento { get; private set; }

        public int Tentativas { get; private set; }
        public string? UltimoErro { get; private set; }
        public string PayloadJson { get; private set; } = null!;

        private EventoWebhook() { }

        public static EventoWebhook Criar(RequisicaoWebhookPagamento req, string rawJson, DateTimeOffset now)
        {
            var idTransacao = req.IdTransacao?.Trim();
            if (string.IsNullOrWhiteSpace(idTransacao))
            {
                throw new ArgumentException("id_transacao é obrigatório.", nameof(req));
            }

            return new EventoWebhook
            {
                Id = Guid.NewGuid(),
                IdTransacao = idTransacao,
                IdContrato = req.IdContrato?.Trim(),
                Valor = req.Valor,
                DataPagamento = req.DataPagamento?.ToUniversalTime(),
                DataRecebido = now,
                StatusOrigem = req.Status?.Trim(),
                Status = StatusProcessamento.Pendente,
                StatusPagamento = req.Status is null ? null : ConversorPagamentoStatus.Parse(req.Status),
                Tentativas = 0,
                UltimoErro = null,
                DataProcessado = null,
                PayloadJson = rawJson
            };

        }

        public void DefinirProcessando()
        {
            if (Status is StatusProcessamento.Concluido)
                throw new InvalidOperationException($"Evento {Id} já foi processado.");

            Status = StatusProcessamento.EmProcessamento;
            Tentativas++;
            UltimoErro = null;
        }

        public void DefinirConcluido(DateTimeOffset now)
        {
            Status = StatusProcessamento.Concluido;
            DataProcessado = now;
            UltimoErro = null;
        }

        public void DefinirFalha(string erro, bool permanente)
        {
            UltimoErro = erro.Length > 2000 ? erro[..2000] : erro;

            Status = (permanente || Tentativas >= 5)
                ? StatusProcessamento.Falha
                : StatusProcessamento.Pendente;
        }
    }
}
