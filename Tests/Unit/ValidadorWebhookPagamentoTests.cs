using Application.Common;
using Application.Contracts;
using FluentAssertions;
using System;
using Xunit;
using Application.Validation;

namespace Tests.Unit
{
    internal class TestTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public TestTimeProvider(DateTimeOffset now) => _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
    }

    public class ValidadorWebhookPagamentoTests
    {
        [Fact]
        public void RequisicaoCompletaECorreta_EhValida()
        {
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var prov = new TestTimeProvider(agora);
            var v = new ValidadorWebhookPagamento(prov);
            var req = new RequisicaoWebhookPagamento
            {
                IdContrato = "ctr-1",
                Valor = 100m,
                DataPagamento = agora,
                Status = "Liquidado"
            };

            var res = v.Validar(req);

            res.IsValid.Should().BeTrue();
        }

        [Fact]
        public void AcumulaTodosErros_RetornaTodosErros()
        {
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var prov = new TestTimeProvider(agora);
            var v = new ValidadorWebhookPagamento(prov);
            var req = new RequisicaoWebhookPagamento
            {
                IdContrato = null,
                Valor = 0m,
                DataPagamento = agora.AddHours(1),
                Status = null
            };

            var res = v.Validar(req);

            res.IsValid.Should().BeFalse();
            res.Erros.Should().HaveCount(4);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IdContratoInvalido_GeraErro(string id)
        {
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var v = new ValidadorWebhookPagamento(new TestTimeProvider(agora));
            var req = new RequisicaoWebhookPagamento { IdContrato = id, Valor = 1m, DataPagamento = agora, Status = "Liquidado" };

            var res = v.Validar(req);

            res.IsValid.Should().BeFalse();
            res.Erros.Should().ContainSingle(e => e.Contains("id_contrato"));
        }

        [Theory]
        [InlineData(null)]
        public void ValorNull_GeraErro(decimal? valor)
        {
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var v = new ValidadorWebhookPagamento(new TestTimeProvider(agora));
            var req = new RequisicaoWebhookPagamento { IdContrato = "c", Valor = valor, DataPagamento = agora, Status = "Liquidado" };

            var res = v.Validar(req);

            res.IsValid.Should().BeFalse();
            res.Erros.Should().Contain(e => e.Contains("valor é obrigatório"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void ValorZeroOuNegativo_GeraErroEIncluiValor(decimal valor)
        {
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var v = new ValidadorWebhookPagamento(new TestTimeProvider(agora));
            var req = new RequisicaoWebhookPagamento { IdContrato = "c", Valor = valor, DataPagamento = agora, Status = "Liquidado" };

            var res = v.Validar(req);

            res.IsValid.Should().BeFalse();
            res.Erros.Should().Contain(e => e.Contains("valor deve ser maior que zero") && e.Contains(valor.ToString()));
        }

        [Fact]
        public void DataPagamentoNull_GeraErro()
        {
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var v = new ValidadorWebhookPagamento(new TestTimeProvider(agora));
            var req = new RequisicaoWebhookPagamento { IdContrato = "c", Valor = 1m, DataPagamento = null, Status = "Liquidado" };

            var res = v.Validar(req);

            res.IsValid.Should().BeFalse();
            res.Erros.Should().Contain(e => e.Contains("data_pagamento é obrigatória"));
        }

        [Fact]
        public void DataPagamentoAlémToleranciaDeFuturo_GeraErro()
        {
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var v = new ValidadorWebhookPagamento(new TestTimeProvider(agora));
            var req = new RequisicaoWebhookPagamento { IdContrato = "c", Valor = 1m, DataPagamento = agora.AddMinutes(11), Status = "Liquidado" };

            var res = v.Validar(req);

            res.IsValid.Should().BeFalse();
            res.Erros.Should().Contain(e => e.Contains("data_pagamento está no futuro"));
        }

        [Fact]
        public void DataPagamentoNaBordaDaTolerancia_NaoGeraErro()
        {
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var v = new ValidadorWebhookPagamento(new TestTimeProvider(agora));
            var req = new RequisicaoWebhookPagamento { IdContrato = "c", Valor = 1m, DataPagamento = agora.AddMinutes(10), Status = "Liquidado" };

            var res = v.Validar(req);

            res.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("algum-status-desconhecido")]
        [InlineData("999")]
        public void StatusInvalidos_GeramErro(string? status)
        {
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var v = new ValidadorWebhookPagamento(new TestTimeProvider(agora));
            var req = new RequisicaoWebhookPagamento { IdContrato = "c", Valor = 1m, DataPagamento = agora, Status = status };

            var res = v.Validar(req);

            res.IsValid.Should().BeFalse();
            res.Erros.Should().Contain(e => e.Contains("status"));
        }
    }
}
