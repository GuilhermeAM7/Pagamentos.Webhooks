using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Abstractions;

public enum ResultadoAutenticacao
{
    Valida,
    CabecalhoAusente,
    ChaveInvalida
}

public interface IValidadorApiKey
{
    ResultadoAutenticacao Validar(string? chaveRecebida);
}
