using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Contracts
{
    public sealed record RequisicaoWebhookPagamento
    {
        [JsonPropertyName("id_transacao")]
        public string? IdTransacao { get; init; }

        [JsonPropertyName("id_contrato")]
        public string? IdContrato { get; init; }

        [JsonPropertyName("valor")]
        public decimal? Valor { get; init; }

        [JsonPropertyName("data_pagamento")]
        public DateTimeOffset? DataPagamento { get; init; }

        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? CamposExtras { get; init; }
    }
}
