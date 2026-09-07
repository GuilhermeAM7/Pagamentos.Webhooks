using Application.Contracts;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace Tests.Unit
{
    public class DesserializacaoRequisicaoWebhookTests
    {
        [Fact]
        public void JsonEmSnakeCase_PopulaTodasPropriedades()
        {
            var json = "{\"id_transacao\":\"t1\",\"id_contrato\":\"c1\",\"valor\":123.45,\"data_pagamento\":\"2024-01-01T10:00:00Z\",\"status\":\"Liquidado\"}";

            var req = JsonSerializer.Deserialize<RequisicaoWebhookPagamento>(json);

            req.Should().NotBeNull();
            req!.IdTransacao.Should().Be("t1");
            req.IdContrato.Should().Be("c1");
            req.Valor.Should().Be(123.45m);
        }

        [Fact]
        public void CampoNaoMapeado_CaiEmCamposExtras()
        {
            var json = "{\"id_transacao\":\"t1\",\"campo_extra\":\"valor\"}";

            var req = JsonSerializer.Deserialize<RequisicaoWebhookPagamento>(json);

            req.Should().NotBeNull();
            req!.CamposExtras.Should().ContainKey("campo_extra");
        }

        [Fact]
        public void CamposAusentes_DesserializaSemLancar_DeixandoNull()
        {
            var json = "{ \"id_transacao\": \"t1\" }";

            var req = JsonSerializer.Deserialize<RequisicaoWebhookPagamento>(json);

            req.Should().NotBeNull();
            req!.IdContrato.Should().BeNull();
            req.Valor.Should().BeNull();
            req.DataPagamento.Should().BeNull();
        }
    }
}
