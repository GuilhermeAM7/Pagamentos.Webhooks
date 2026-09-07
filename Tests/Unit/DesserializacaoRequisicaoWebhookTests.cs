using Application.Contracts;
using FluentAssertions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Tests.Unit
{
    public class DesserializacaoRequisicaoWebhookTests
    {
        [Fact]
        public void JsonEmSnakeCase_PopulaTodasPropriedades()
        {
            // Arrange
            var json = "{\"id_transacao\":\"t1\",\"id_contrato\":\"c1\",\"valor\":123.45,\"data_pagamento\":\"2024-01-01T10:00:00Z\",\"status\":\"Liquidado\"}";

            // Act
            var req = JsonSerializer.Deserialize<RequisicaoWebhookPagamento>(json);

            // Assert
            req.Should().NotBeNull();
            req!.IdTransacao.Should().Be("t1");
            req.IdContrato.Should().Be("c1");
            req.Valor.Should().Be(123.45m);
        }

        [Fact]
        public void CampoNaoMapeado_CaiEmCamposExtras()
        {
            // Arrange
            var json = "{\"id_transacao\":\"t1\",\"campo_extra\":\"valor\"}";

            // Act
            var req = JsonSerializer.Deserialize<RequisicaoWebhookPagamento>(json);

            // Assert
            req.Should().NotBeNull();
            req!.CamposExtras.Should().ContainKey("campo_extra");
        }

        [Fact]
        public void CamposAusentes_DesserializaSemLancar_DeixandoNull()
        {
            // Arrange
            var json = "{ \"id_transacao\": \"t1\" }";

            // Act
            var req = JsonSerializer.Deserialize<RequisicaoWebhookPagamento>(json);

            // Assert
            req.Should().NotBeNull();
            req!.IdContrato.Should().BeNull();
            req.Valor.Should().BeNull();
            req.DataPagamento.Should().BeNull();
        }

        [Fact]
        public void ValorStringNumerica_ViraDecimalComPrecisao()
        {
            // Arrange
            var json = "{\"valor\": \"1234.56\" }";
            var options = new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };

            // Act
            var req = JsonSerializer.Deserialize<RequisicaoWebhookPagamento>(json, options);

            // Assert
            req.Should().NotBeNull();
            req!.Valor.Should().Be(1234.56m);
        }
    }
}
