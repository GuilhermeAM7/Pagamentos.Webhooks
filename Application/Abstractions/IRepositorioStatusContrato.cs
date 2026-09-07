using Application.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Abstractions
{
    public interface IRepositorioStatusContrato
    {
        Task<StatusContrato?> ObterPorContratoAsync(string idContrato, CancellationToken ct);

        void Adicionar(StatusContrato contrato);
    }
}
