using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Contracts
{
    public enum SituacaoRecebimento
    {
        Aceito,
        Duplicado,
        Rejeitado
    }

    public sealed record ResultadoRecebimento(
        SituacaoRecebimento Situacao,
        Guid EventoId,
        string? Erros = null)
    {
        public static ResultadoRecebimento Aceito(Guid id) => new(SituacaoRecebimento.Aceito, id);
        public static ResultadoRecebimento Duplicado(Guid id) => new(SituacaoRecebimento.Duplicado, id);
        public static ResultadoRecebimento Rejeitado(Guid id, string erros)
            => new(SituacaoRecebimento.Rejeitado, id, erros);
    }
}
