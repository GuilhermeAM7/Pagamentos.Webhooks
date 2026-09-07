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
            var res = ResultadoValidacao.Sucesso();

            res.IsValid.Should().BeTrue();
            res.Erros.Should().BeEmpty();
            res.Mensagem.Should().Be(string.Empty);
        }

        [Fact]
        public void Falha_ComDoisErros_RetornaValidoFalseEMensagemConcatenada()
        {
            var erros = new List<string> { "e1", "e2" };

            var res = ResultadoValidacao.Falha(erros);

            res.IsValid.Should().BeFalse();
            res.Erros.Should().HaveCount(2);
            res.Mensagem.Should().Be("e1 | e2");
        }

        [Fact]
        public void Falha_ComListaVazia_LancaArgumentException()
        {
            var erros = new List<string>();

            Action act = () => ResultadoValidacao.Falha(erros);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Falha_ComNull_LancaArgumentException()
        {
            Action act = () => ResultadoValidacao.Falha((IReadOnlyList<string>)null!);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Falha_ComUmaString_ProduzExatamenteUmErro()
        {
            var res = ResultadoValidacao.Falha("um erro");

            res.IsValid.Should().BeFalse();
            res.Erros.Should().ContainSingle().Which.Should().Be("um erro");
        }
    }
}
