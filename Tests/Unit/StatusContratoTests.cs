using Application.Domain.Entities;
using FluentAssertions;
using System;
using System.Collections.Generic;
using Tests.Fixtures;
using Xunit;

namespace Tests.Unit
{
    public class StatusContratoTests
    {
        [Fact]
        public void CriarAPartirDe_PreencheTodosCampos()
        {
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var ev = new ConstrutorEventoWebhook().CriarEvento("{}", agora);

            var status = StatusContrato.CriarAPartirDe(ev, agora);

            status.IdContrato.Should().Be(ev.IdContrato);
            status.StatusPagamento.Should().Be(ev.StatusPagamento);
            status.UltimoValor.Should().Be(ev.Valor);
            status.UltimaDataPagamento.Should().Be(ev.DataPagamento);
            status.UltimoEventoId.Should().Be(ev.Id);
        }

        [Fact]
        public void EventoMaisNovo_AplicaERetornaTrue()
        {
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var builder = new ConstrutorEventoWebhook();
            var ev1 = builder.ComDataPagamento(DateTimeOffset.Parse("2024-01-01T10:00:00Z")).CriarEvento("{}", agora);
            var contrato = StatusContrato.CriarAPartirDe(ev1, agora);

            var ev2 = builder.ComDataPagamento(DateTimeOffset.Parse("2024-01-02T10:00:00Z")).CriarEvento("{}", agora.AddDays(1));

            var ok = contrato.TentarAplicarPagamento(ev2, agora.AddDays(1));

            ok.Should().BeTrue();
            contrato.UltimoEventoId.Should().Be(ev2.Id);
        }

        [Fact]
        public void EventoMaisAntigo_RetornaFalseENaoAlteraEstado()
        {
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var builder = new ConstrutorEventoWebhook();
            var evNew = builder.ComDataPagamento(DateTimeOffset.Parse("2024-01-02T10:00:00Z")).CriarEvento("{}", agora);
            var contrato = StatusContrato.CriarAPartirDe(evNew, agora);

            var antesStatus = contrato.StatusPagamento;
            var antesValor = contrato.UltimoValor;
            var antesData = contrato.UltimaDataPagamento;

            var evOld = builder.ComDataPagamento(DateTimeOffset.Parse("2024-01-01T10:00:00Z")).CriarEvento("{}", agora.AddDays(-1));

            var ok = contrato.TentarAplicarPagamento(evOld, agora);

            ok.Should().BeFalse();
            contrato.StatusPagamento.Should().Be(antesStatus);
            contrato.UltimoValor.Should().Be(antesValor);
            contrato.UltimaDataPagamento.Should().Be(antesData);
        }

        [Fact]
        public void MesmoEventoAplicadoDuasVezes_RetornaFalseNaSegunda()
        {
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var ev = new ConstrutorEventoWebhook().ComDataPagamento(DateTimeOffset.Parse("2024-01-02T10:00:00Z")).CriarEvento("{}", agora);
            var contrato = StatusContrato.CriarAPartirDe(ev, agora);

            var ok = contrato.TentarAplicarPagamento(ev, agora);

            ok.Should().BeFalse();
        }

        [Fact]
        public void EmpateDataPagamento_ComRecebidoPosterior_Aplica()
        {
            var agora = DateTimeOffset.Parse("2024-01-02T12:00:00Z");
            var builder = new ConstrutorEventoWebhook();
            var ev1 = builder.ComDataPagamento(DateTimeOffset.Parse("2024-01-02T10:00:00Z")).CriarEvento("{}", DateTimeOffset.Parse("2024-01-02T09:00:00Z"));
            var contrato = StatusContrato.CriarAPartirDe(ev1, agora);

            var ev2 = builder.ComDataPagamento(DateTimeOffset.Parse("2024-01-02T10:00:00Z")).CriarEvento("{}", DateTimeOffset.Parse("2024-01-02T11:00:00Z"));

            var ok = contrato.TentarAplicarPagamento(ev2, agora);

            ok.Should().BeTrue();
            contrato.UltimoEventoId.Should().Be(ev2.Id);
        }

        [Theory]
        [InlineData("2024-01-02T11:00:00Z")]
        [InlineData("2024-01-02T09:00:00Z")]
        public void EmpateDataPagamento_ComRecebidoAnteriorOuIgual_NaoAplica(string recebido)
        {
            var agora = DateTimeOffset.Parse("2024-01-02T12:00:00Z");
            var builder = new ConstrutorEventoWebhook();
            var ev1 = builder.ComDataPagamento(DateTimeOffset.Parse("2024-01-02T10:00:00Z")).CriarEvento("{}", DateTimeOffset.Parse("2024-01-02T10:00:00Z"));
            var contrato = StatusContrato.CriarAPartirDe(ev1, agora);

            var ev2 = builder.ComDataPagamento(DateTimeOffset.Parse("2024-01-02T10:00:00Z")).CriarEvento("{}", DateTimeOffset.Parse(recebido));

            var ok = contrato.TentarAplicarPagamento(ev2, agora);

            if (DateTimeOffset.Parse(recebido) <= ev1.DataRecebido)
                ok.Should().BeFalse();
            else
                ok.Should().BeTrue();
        }

        [Fact]
        public void EventoDeOutroContrato_LancaInvalidOperationException()
        {
            var agora = DateTimeOffset.Parse("2024-01-02T12:00:00Z");
            var ev = new ConstrutorEventoWebhook().ComIdContrato("ctr-A").ComDataPagamento(DateTimeOffset.Parse("2024-01-02T10:00:00Z")).CriarEvento("{}", agora);
            var contrato = StatusContrato.CriarAPartirDe(ev, agora);

            var outro = new ConstrutorEventoWebhook().ComIdContrato("ctr-B").ComDataPagamento(DateTimeOffset.Parse("2024-01-03T10:00:00Z")).CriarEvento("{}", agora);

            Action act = () => contrato.TentarAplicarPagamento(outro, agora);

            act.Should().Throw<InvalidOperationException>();
        }

        public static TheoryData<string?, decimal?, DateTimeOffset?> CamposObrigatoriosAusentes => new()
{
    { null,    100m, new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero) },
    { "ctr-1", null, new DateTimeOffset(2024, 1, 1, 10, 0, 0, TimeSpan.Zero) },
    { "ctr-1", 100m, null },
};

        [Theory]
        [MemberData(nameof(CamposObrigatoriosAusentes))]
        public void CriarAPartirDe_EventoSemCamposObrigatorios_LancaInvalidOperationException(
            string? idContrato, decimal? valor, DateTimeOffset? dataPagamento)
        {
            var agora = new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.Zero);

            var evento = new ConstrutorEventoWebhook()
                .ComIdContrato(idContrato)
                .ComValor(valor)
                .ComDataPagamento(dataPagamento)
                .CriarEvento("{}", agora);

            Action acao = () => StatusContrato.CriarAPartirDe(evento, agora);

            acao.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void AplicarListaEmOrdensDiferentes_ConvergeParaMesmoEstadoFinal()
        {
            var agora = DateTimeOffset.Parse("2024-01-10T12:00:00Z");
            var b = new ConstrutorEventoWebhook();
            var e1 = b.ComIdTransacao("t1").ComDataPagamento(DateTimeOffset.Parse("2024-01-01T10:00:00Z")).ComValor(100).CriarEvento("{}", DateTimeOffset.Parse("2024-01-01T11:00:00Z"));
            var e2 = b.ComIdTransacao("t2").ComDataPagamento(DateTimeOffset.Parse("2024-01-02T10:00:00Z")).ComValor(200).CriarEvento("{}", DateTimeOffset.Parse("2024-01-02T11:00:00Z"));
            var e3 = b.ComIdTransacao("t3").ComDataPagamento(DateTimeOffset.Parse("2024-01-03T10:00:00Z")).ComValor(300).CriarEvento("{}", DateTimeOffset.Parse("2024-01-03T11:00:00Z"));

            var listA = new List<EventoWebhook> { e1, e2, e3 };
            var listB = new List<EventoWebhook> { e3, e1, e2 };

            var c1 = StatusContrato.CriarAPartirDe(e1, agora);
            foreach (var ev in listA)
                c1.TentarAplicarPagamento(ev, agora);

            var c2 = StatusContrato.CriarAPartirDe(e1, agora);
            foreach (var ev in listB)
                c2.TentarAplicarPagamento(ev, agora);

            c1.StatusPagamento.Should().Be(c2.StatusPagamento);
            c1.UltimoValor.Should().Be(c2.UltimoValor);
            c1.UltimaDataPagamento.Should().Be(c2.UltimaDataPagamento);
        }
    }
}
