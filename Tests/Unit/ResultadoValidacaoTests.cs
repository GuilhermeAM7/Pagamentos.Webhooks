using Application.Common;
using FluentAssertions;
using System;
using System.Collections.Generic;
using Xunit;

namespace Tests.Unit
{
    public class ResultadoValidacaoTests
    {
        [Fact]
        public void Sucesso_RetornaValidoTrueErrosVazioMensagemVazia()
        {
            // Act
            var res = ResultadoValidacao.Sucesso();

            // Assert
            res.IsValid.Should().BeTrue();
            res.Erros.Should().BeEmpty();
            res.Mensagem.Should().Be(string.Empty);
        }

        [Fact]
        public void Falha_ComDoisErros_RetornaValidoFalseEMensagemConcatenada()
        {
            // Arrange
            var erros = new List<string> { "e1", "e2" };

            // Act
            var res = ResultadoValidacao.Falha(erros);

            // Assert
            res.IsValid.Should().BeFalse();
            res.Erros.Should().HaveCount(2);
            res.Mensagem.Should().Be("e1 | e2");
        }

        [Fact]
        public void Falha_ComListaVazia_LancaArgumentException()
        {
            // Arrange
            var erros = new List<string>();

            // Act / Assert
            Action act = () => ResultadoValidacao.Falha(erros);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Falha_ComNull_LancaArgumentException()
        {
            // Act / Assert
            Action act = () => ResultadoValidacao.Falha((IReadOnlyList<string>?)null);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Falha_ComUmaString_ProduzExatamenteUmErro()
        {
            // Act
            var res = ResultadoValidacao.Falha("um erro");

            // Assert
            res.IsValid.Should().BeFalse();
            res.Erros.Should().ContainSingle().Which.Should().Be("um erro");
        }
    }
}
