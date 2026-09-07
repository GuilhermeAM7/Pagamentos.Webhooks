using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Abstractions
{
    public sealed record ResultadoInsercao(bool Duplicado, Guid EventoId)
    {
        public static ResultadoInsercao Inserido(Guid id) => new(false, id);
        public static ResultadoInsercao JaExistia(Guid id) => new(true, id);
    }
}
