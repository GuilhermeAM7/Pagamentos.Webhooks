using Application.Abstractions;
using Application.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Persistence.Repositories
{
    public sealed class RepositorioStatusContrato(AppDbContext contexto) : IRepositorioStatusContrato
    {
        public Task<StatusContrato?> ObterPorContratoAsync(string idContrato, CancellationToken ct) =>
            contexto.StatusContratos.FirstOrDefaultAsync(c => c.IdContrato == idContrato, ct);

        public void Adicionar(StatusContrato contrato) => contexto.StatusContratos.Add(contrato);
    }
}
