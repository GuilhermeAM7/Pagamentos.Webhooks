using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Abstractions
{
    public interface IUnidadeDeTrabalho
    {
        Task<int> SalvarAsync(CancellationToken ct);
    }
}
