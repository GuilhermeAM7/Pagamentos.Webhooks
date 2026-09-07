using Application.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Tests.Unit
{
    public class ConversorStatusPagamentoTests
    {
        [Theory]
        [InlineData("liquidado", PagamentoStatus.Liquidado)]
        [InlineData("Liquidado", PagamentoStatus.Liquidado)]
        [InlineData("LIquidAdo", PagamentoStatus.Liquidado)]
        public void Parse_StringConhecidaEmQualquerCaixa_Converte(string entrada, PagamentoStatus esperado)
        {
            var resultado = ConversorPagamentoStatus.Parse(entrada);

            resultado.Should().Be(esperado);
        }

        [Theory]
        [InlineData("desconhecido")]
        [InlineData("algo-que-nao-existe")]
        public void TryParse_StringDesconhecida_RetornaFalseEUnknown(string entrada)
        {
            var ok = ConversorPagamentoStatus.TryParse(entrada, out var status);

            ok.Should().BeFalse();
            status.Should().Be(PagamentoStatus.Unknown);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void TryParse_NullOuVazio_RetornaFalseEUnknown(string entrada)
        {
            var ok = ConversorPagamentoStatus.TryParse(entrada, out var status);

            ok.Should().BeFalse();
            status.Should().Be(PagamentoStatus.Unknown);
        }

        [Theory]
        [InlineData("999")]
        public void TryParse_StringNumerica_RetornaUnknown(string entrada)
        {
            var ok = ConversorPagamentoStatus.TryParse(entrada, out var status);

            ok.Should().BeFalse();
            status.Should().Be(PagamentoStatus.Unknown);
        }

        [Fact]
        public void TryParse_Falha_DeixaOutParametroComoUnknown()
        {
            var ok = ConversorPagamentoStatus.TryParse("invalido", out var status);

            ok.Should().BeFalse();
            status.Should().Be(PagamentoStatus.Unknown);
        }
    }
}
