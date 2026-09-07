using Application.Common;
using Application.Contracts;
using Application.Domain.Enums;

namespace Sabemi.Webhooks.TesteTecnico.Application.Validation;

public sealed class ValidadorWebhookPagamento(TimeProvider relogio)
{
    private static readonly TimeSpan ToleranciaFuturo = TimeSpan.FromMinutes(10);
    private const int TamanhoMaximoIdContrato = 100;

    public ResultadoValidacao Validar(RequisicaoWebhookPagamento requisicao)
    {
        var erros = new List<string>();

        if (string.IsNullOrWhiteSpace(requisicao.IdContrato))
            erros.Add("id_contrato é obrigatório.");
        else if (requisicao.IdContrato.Length > TamanhoMaximoIdContrato)
            erros.Add($"id_contrato excede {TamanhoMaximoIdContrato} caracteres " +
                      $"(recebido: {requisicao.IdContrato.Length}).");

        if (requisicao.Valor is null)
            erros.Add("valor é obrigatório.");
        else if (requisicao.Valor <= 0)
            erros.Add($"valor deve ser maior que zero (recebido: {requisicao.Valor}).");

        if (requisicao.DataPagamento is null)
            erros.Add("data_pagamento é obrigatória.");
        else if (requisicao.DataPagamento > relogio.GetUtcNow().Add(ToleranciaFuturo))
            erros.Add($"data_pagamento está no futuro ({requisicao.DataPagamento.Value:O}).");

        if (string.IsNullOrWhiteSpace(requisicao.Status))
            erros.Add("status é obrigatório.");
        else if (!ConversorPagamentoStatus.TryParse(requisicao.Status, out _))
            erros.Add($"status '{requisicao.Status}' não é reconhecido.");

        return erros.Count == 0
            ? ResultadoValidacao.Sucesso()
            : ResultadoValidacao.Falha(erros);
    }
}
