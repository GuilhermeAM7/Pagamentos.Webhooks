using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Common
{
    public sealed record ResultadoValidacao
    {
        public IReadOnlyList<string> Erros { get; }

        public bool IsValid => Erros.Count == 0;

        public string Mensagem => string.Join(" | ", Erros);

        private ResultadoValidacao(IReadOnlyList<string> erros) => Erros = erros;

        public static ResultadoValidacao Sucesso() => new([]);

        public static ResultadoValidacao Falha(IReadOnlyList<string> erros)
        {
            if (erros is null || erros.Count == 0)
                throw new ArgumentException("Uma falha precisa ter ao menos um erro.", nameof(erros));

            return new ResultadoValidacao(erros);
        }

        public static ResultadoValidacao Falha(string erro) => Falha([erro]);
    }
}
