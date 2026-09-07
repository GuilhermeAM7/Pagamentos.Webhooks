using Application.Contracts;
using Application.Domain.Entities;
using FluentAssertions;
using System;
using Tests.Fixtures;
using Xunit;

namespace Tests.Unit
{
    public class EventoWebhookTests
    {
        [Fact]
        public void Criar_PreencheCamposEPendenteETentativasZero()
        {
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var req = new ConstrutorEventoWebhook().BuildRequisicao();

            var ev = EventoWebhook.Criar(req, "{}", agora);

            ev.Id.Should().NotBeEmpty();
            ev.IdTransacao.Should().Be(req.IdTransacao);
            ev.IdContrato.Should().Be(req.IdContrato);
            ev.Valor.Should().Be(req.Valor);
            ev.DataPagamento.Should().Be(req.DataPagamento);
            ev.DataRecebido.Should().Be(agora);
            ev.Status.Should().Be(Application.Domain.Enums.StatusProcessamento.Pendente);
            ev.Tentativas.Should().Be(0);
        }

        [Theory]
        [InlineData(" ABC ", "ABC")]
        [InlineData("ABC", "ABC")]
        public void Criar_AplicaTrimNoIdTransacao(string entrada, string esperado)
        {
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var req = new ConstrutorEventoWebhook().ComIdTransacao(entrada).BuildRequisicao();

            var ev = EventoWebhook.Criar(req, "{}", agora);

            ev.IdTransacao.Should().Be(esperado);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Criar_ComIdTransacaoInvalido_LancaArgumentException(string? id)
        {
            var agora = DateTimeOffset.UtcNow;
            var req = new ConstrutorEventoWebhook().ComIdTransacao(id!).BuildRequisicao();

            Action act = () => EventoWebhook.Criar(req, "{}", agora);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Criar_StatusDesconhecido_PreservaStatusOrigemEDefineUnknown()
        {
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var req = new ConstrutorEventoWebhook().ComStatus("uma-coisa") .BuildRequisicao();

            var ev = EventoWebhook.Criar(req, "{}", agora);

            ev.StatusOrigem.Should().Be("uma-coisa");
            ev.StatusPagamento.Should().Be(Application.Domain.Enums.PagamentoStatus.Unknown);
        }

        [Fact]
        public void DefinirProcessando_IncrementaTentativasELimpaUltimoErro()
        {
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var ev = new ConstrutorEventoWebhook().CriarEvento("{}", agora);
            ev.DefinirFalha("erro", permanente: false);

            ev.DefinirProcessando();

            ev.Tentativas.Should().Be(1);
            ev.UltimoErro.Should().BeNull();
            ev.Status.Should().Be(Application.Domain.Enums.StatusProcessamento.EmProcessamento);
        }

        [Fact]
        public void DefinirProcessando_EmEventoProcessado_LancaInvalidOperationException()
        {
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var ev = new ConstrutorEventoWebhook().CriarEvento("{}", agora);
            ev.DefinirConcluido(agora);

            Action act = () => ev.DefinirProcessando();

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void DefinirConcluido_DefineDataProcessadoELimpaUltimoErro()
        {
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var ev = new ConstrutorEventoWebhook().CriarEvento("{}", agora);
            ev.DefinirFalha("erro", permanente: true);

            ev.DefinirConcluido(agora);

            ev.DataProcessado.Should().Be(agora);
            ev.UltimoErro.Should().BeNull();
            ev.Status.Should().Be(Application.Domain.Enums.StatusProcessamento.Concluido);
        }

        [Fact]
        public void DefinirFalha_Permanente_VaiParaFalhaImediatamente()
        {
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var ev = new ConstrutorEventoWebhook().CriarEvento("{}", agora);

            ev.DefinirFalha("erro permanente", permanente: true);

            ev.Status.Should().Be(Application.Domain.Enums.StatusProcessamento.Falha);
        }

        [Fact]
        public void DefinirFalha_Transient_antesDoMax_VoltaParaPendente()
        {
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var ev = new ConstrutorEventoWebhook().CriarEvento("{}", agora);

            ev.DefinirProcessando();

            ev.DefinirFalha("erro transitório", permanente: false);

            ev.Status.Should().Be(Application.Domain.Enums.StatusProcessamento.Pendente);
        }

        [Fact]
        public void DefinirFalha_Transient_naTentativaMaxima_VaiParaFalha()
        {
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var ev = new ConstrutorEventoWebhook().CriarEvento("{}", agora);

            for (int i = 0; i < 5; i++)
                ev.DefinirProcessando();

            ev.DefinirFalha(new string('x', 10), permanente: false);

            ev.Status.Should().Be(Application.Domain.Enums.StatusProcessamento.Falha);
        }

        [Fact]
        public void DefinirFalha_TruncaUltimoErroEm2000Caracteres()
        {
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var ev = new ConstrutorEventoWebhook().CriarEvento("{}", agora);
            var longo = new string('a', 3000);

            ev.DefinirFalha(longo, permanente: true);

            ev.UltimoErro.Should().HaveLength(2000);
        }
    }
}
