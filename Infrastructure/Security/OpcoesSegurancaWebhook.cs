using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Security
{
    public sealed class OpcoesSegurancaWebhook
    {
        public const string Secao = "SegurancaWebhook";

        public string ApiKey { get; set; } = string.Empty;
    }
}
