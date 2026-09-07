using Application.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Domain.Entities
{
    public class StatusContrato
    {
        public string IdContrato { get; private set; } = null!;
        public PagamentoStatus StatusPagamento { get; private set; }
        public decimal UltimoValor { get; private set; }
        public DateTimeOffset UltimaDataPagamento { get; private set; }
        public DateTimeOffset UltimaRecepcaoEm { get; private set; }
        public Guid UltimoEventoId { get; private set; }
        public DateTimeOffset AtualizadoEm { get; private set; }

        private StatusContrato() { }

        public static StatusContrato CriarAPartirDe(EventoWebhook evento, DateTimeOffset agora)
        {
            GarantirEventoAplicavel(evento);

            var contrato = new StatusContrato { IdContrato = evento.IdContrato! };
            contrato.Aplicar(evento, agora);
            return contrato;
        }

        public bool TentarAplicarPagamento(EventoWebhook evento, DateTimeOffset agora)
        {
            GarantirEventoAplicavel(evento);

            if (!string.Equals(IdContrato, evento.IdContrato, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Evento {evento.Id} pertence ao contrato '{evento.IdContrato}', não a '{IdContrato}'.");

            if (UltimoEventoId == evento.Id)
                return false;

            if (evento.DataPagamento!.Value < UltimaDataPagamento)
                return false;

            if (evento.DataPagamento.Value == UltimaDataPagamento &&
                evento.DataRecebido <= UltimaRecepcaoEm)
                return false;

            Aplicar(evento, agora);
            return true;
        }

        private void Aplicar(EventoWebhook evento, DateTimeOffset agora)
        {
            StatusPagamento = evento.StatusPagamento!.Value;
            UltimoValor = evento.Valor!.Value;
            UltimaDataPagamento = evento.DataPagamento!.Value;
            UltimaRecepcaoEm = evento.DataRecebido;
            UltimoEventoId = evento.Id;
            AtualizadoEm = agora;
        }

        private static void GarantirEventoAplicavel(EventoWebhook evento)
        {
            if (string.IsNullOrWhiteSpace(evento.IdContrato)
                || evento.Valor is null
                || evento.DataPagamento is null)
            {
                throw new InvalidOperationException();
            }
        }
    }
}
