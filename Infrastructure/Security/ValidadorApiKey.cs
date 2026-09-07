using System.Security.Cryptography;
using System.Text;
using Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Security;

public sealed class ValidadorApiKey(IOptions<OpcoesSegurancaWebhook> opcoes) : IValidadorApiKey
{
    public ResultadoAutenticacao Validar(string? chaveRecebida)
    {
        if (string.IsNullOrWhiteSpace(chaveRecebida))
            return ResultadoAutenticacao.CabecalhoAusente;

        return CryptographicOperations.FixedTimeEquals(
                   Encoding.UTF8.GetBytes(opcoes.Value.ApiKey),
                   Encoding.UTF8.GetBytes(chaveRecebida))
            ? ResultadoAutenticacao.Valida
            : ResultadoAutenticacao.ChaveInvalida;
    }
}
