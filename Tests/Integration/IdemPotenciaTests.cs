using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Fixtures;

namespace Tests.Integration;

[Trait("Categoria", "Integracao")]
[Collection(ColecaoIntegracao.Nome)]
public class IdempotenciaTests(ApiFactory fabrica)
{
    private static StringContent Corpo(string idTransacao) => new(
        $$"""
        {
          "id_transacao": "{{idTransacao}}",
          "id_contrato": "ctr-100",
          "valor": 1500.50,
          "data_pagamento": "2026-09-05T10:00:00Z",
          "status": "Liquidado"
        }
        """, Encoding.UTF8, "application/json");

    [Fact]
    public async Task DezRequisicoesSimultaneas_MesmoIdTransacao_GravamApenasUmEvento()
    {
        var idTransacao = $"txn-{Guid.NewGuid():N}";
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Api-Key", "chave-de-teste");

        var respostas = await Task.WhenAll(
            Enumerable.Range(0, 10)
                      .Select(_ => cliente.PostAsync("/webhooks/pagamento", Corpo(idTransacao))));

        using var escopo = fabrica.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<AppDbContext>();

        var total = await contexto.Eventos.CountAsync(e => e.IdTransacao == idTransacao);
        total.Should().Be(1, "a constraint UNIQUE deve impedir qualquer duplicata");

        respostas.Count(r => r.StatusCode == HttpStatusCode.Accepted)
                 .Should().Be(1, "apenas uma requisição pode ter inserido o evento");

        respostas.Count(r => r.StatusCode == HttpStatusCode.OK)
                 .Should().Be(9, "as demais devem receber 200 como duplicadas");
    }

    [Fact]
    public async Task RequisicaoSemApiKey_Retorna401()
    {
        var cliente = fabrica.CreateClient();
        var resposta = await cliente.PostAsync("/webhooks/pagamento", Corpo("txn-sem-chave"));

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReenvioSequencial_DevolveOMesmoEventoId()
    {
        var idTransacao = $"txn-{Guid.NewGuid():N}";
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Api-Key", "chave-de-teste");

        var primeira = await cliente.PostAsync("/webhooks/pagamento", Corpo(idTransacao));
        var segunda = await cliente.PostAsync("/webhooks/pagamento", Corpo(idTransacao));

        primeira.StatusCode.Should().Be(HttpStatusCode.Accepted);
        segunda.StatusCode.Should().Be(HttpStatusCode.OK);

        var idPrimeira = (await primeira.Content.ReadFromJsonAsync<RespostaWebhook>())!.EventoId;
        var idSegunda = (await segunda.Content.ReadFromJsonAsync<RespostaWebhook>())!.EventoId;

        idSegunda.Should().Be(idPrimeira, "a resposta idempotente deve apontar para o evento original");
    }

    private sealed record RespostaWebhook(Guid EventoId, string Situacao);
}
