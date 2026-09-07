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
            // Arrange
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var req = new ConstrutorEventoWebhook().BuildRequisicao();

            // Act
            var ev = EventoWebhook.Criar(req, "{}", agora);

            // Assert
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
            // Arrange
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var req = new ConstrutorEventoWebhook().ComIdTransacao(entrada).BuildRequisicao();

            // Act
            var ev = EventoWebhook.Criar(req, "{}", agora);

            // Assert
            ev.IdTransacao.Should().Be(esperado);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Criar_ComIdTransacaoInvalido_LancaArgumentException(string? id)
        {
            // Arrange
            var agora = DateTimeOffset.UtcNow;
            var req = new ConstrutorEventoWebhook().ComIdTransacao(id!).BuildRequisicao();

            // Act
            Action act = () => EventoWebhook.Criar(req, "{}", agora);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Criar_StatusDesconhecido_PreservaStatusOrigemEDefineUnknown()
        {
            // Arrange
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var req = new ConstrutorEventoWebhook().ComStatus("uma-coisa") .BuildRequisicao();

            // Act
            var ev = EventoWebhook.Criar(req, "{}", agora);

            // Assert
            ev.StatusOrigem.Should().Be("uma-coisa");
            ev.StatusPagamento.Should().Be(Application.Domain.Enums.PagamentoStatus.Unknown);
        }

        [Fact]
        public void DefinirProcessando_IncrementaTentativasELimpaUltimoErro()
        {
            // Arrange
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var ev = new ConstrutorEventoWebhook().CriarEvento("{}", agora);
            // simula erro anterior
            ev.DefinirFalha("erro", permanente: false);

            // Act
            ev.DefinirProcessando();

            // Assert
            ev.Tentativas.Should().Be(1);
            ev.UltimoErro.Should().BeNull();
            ev.Status.Should().Be(Application.Domain.Enums.StatusProcessamento.EmProcessamento);
        }

        [Fact]
        public void DefinirProcessando_EmEventoProcessado_LancaInvalidOperationException()
        {
            // Arrange
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var ev = new ConstrutorEventoWebhook().CriarEvento("{}", agora);
            ev.DefinirConcluido(agora);

            // Act
            Action act = () => ev.DefinirProcessando();

            // Assert
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void DefinirConcluido_DefineDataProcessadoELimpaUltimoErro()
        {
            // Arrange
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var ev = new ConstrutorEventoWebhook().CriarEvento("{}", agora);
            ev.DefinirFalha("erro", permanente: true);

            // Act
            ev.DefinirConcluido(agora);

            // Assert
            ev.DataProcessado.Should().Be(agora);
            ev.UltimoErro.Should().BeNull();
            ev.Status.Should().Be(Application.Domain.Enums.StatusProcessamento.Concluido);
        }

        [Fact]
        public void DefinirFalha_Permanente_VaiParaFalhaImediatamente()
        {
            // Arrange
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var ev = new ConstrutorEventoWebhook().CriarEvento("{}", agora);

            // Act
            ev.DefinirFalha("erro permanente", permanente: true);

            // Assert
            ev.Status.Should().Be(Application.Domain.Enums.StatusProcessamento.Falha);
        }

        [Fact]
        public void DefinirFalha_Transient_antesDoMax_VoltaParaPendente()
        {
            // Arrange
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var ev = new ConstrutorEventoWebhook().CriarEvento("{}", agora);

            // incrementa tentativas para 1
            ev.DefinirProcessando();

            // Act: tenta falhar transitório
            ev.DefinirFalha("erro transitório", permanente: false);

            // Assert
            ev.Status.Should().Be(Application.Domain.Enums.StatusProcessamento.Pendente);
        }

        [Fact]
        public void DefinirFalha_Transient_naTentativaMaxima_VaiParaFalha()
        {
            // Arrange
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var ev = new ConstrutorEventoWebhook().CriarEvento("{}", agora);

            // simula 5 tentativas
            for (int i = 0; i < 5; i++)
                ev.DefinirProcessando();

            // Act
            ev.DefinirFalha(new string('x', 10), permanente: false);

            // Assert
            ev.Status.Should().Be(Application.Domain.Enums.StatusProcessamento.Falha);
        }

        [Fact]
        public void DefinirFalha_TruncaUltimoErroEm2000Caracteres()
        {
            // Arrange
            var agora = DateTimeOffset.Parse("2024-01-01T12:00:00Z");
            var ev = new ConstrutorEventoWebhook().CriarEvento("{}", agora);
            var longo = new string('a', 3000);

            // Act
            ev.DefinirFalha(longo, permanente: true);

            // Assert
            ev.UltimoErro.Should().HaveLength(2000);
        }
    }
}
