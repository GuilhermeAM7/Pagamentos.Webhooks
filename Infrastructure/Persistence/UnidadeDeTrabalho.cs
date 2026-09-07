using Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Persistence
{
    public sealed class UnidadeDeTrabalho(AppDbContext contexto) : IUnidadeDeTrabalho
    {
        public Task<int> SalvarAsync(CancellationToken ct) => contexto.SaveChangesAsync(ct);
    }
}
