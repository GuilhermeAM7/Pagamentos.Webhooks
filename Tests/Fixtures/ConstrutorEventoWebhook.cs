using Application.Contracts;
using Application.Domain.Entities;
using System;

namespace Tests.Fixtures
{
    public class ConstrutorEventoWebhook
    {
        private string _idTransacao = "tx-123";
        private string? _idContrato = "ctr-1";
        private decimal? _valor = 100m;
        private DateTimeOffset? _dataPagamento = DateTimeOffset.Parse("2024-01-01T10:00:00Z");
        private string? _status = "Liquidado";

        public ConstrutorEventoWebhook ComIdTransacao(string id)
        {
            _idTransacao = id;
            return this;
        }

        public ConstrutorEventoWebhook ComIdContrato(string? id)
        {
            _idContrato = id;
            return this;
        }

        public ConstrutorEventoWebhook ComValor(decimal? valor)
        {
            _valor = valor;
            return this;
        }

        public ConstrutorEventoWebhook ComDataPagamento(DateTimeOffset? data)
        {
            _dataPagamento = data;
            return this;
        }

        public ConstrutorEventoWebhook ComStatus(string? status)
        {
            _status = status;
            return this;
        }

        public RequisicaoWebhookPagamento BuildRequisicao()
        {
            return new RequisicaoWebhookPagamento
            {
                IdTransacao = _idTransacao,
                IdContrato = _idContrato,
                Valor = _valor,
                DataPagamento = _dataPagamento,
                Status = _status
            };
        }

        public EventoWebhook CriarEvento(string rawJson, DateTimeOffset agora)
        {
            var req = BuildRequisicao();
            return EventoWebhook.Criar(req, rawJson, agora);
        }
    }
}
