using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Tests.Fixtures;

namespace Tests.Integration;

/// <summary>
/// Cobre a API de leitura do painel — incluindo o requisito "Visualização de Erros":
/// um payload rejeitado precisa aparecer na listagem com o motivo, não sumir.
/// </summary>
[Trait("Categoria", "Integracao")]
public class ListagemEventosTests(ApiFactory fabrica) : IClassFixture<ApiFactory>
{
    private HttpClient ClienteAutenticado()
    {
        var cliente = fabrica.CreateClient();
        cliente.DefaultRequestHeaders.Add("X-Api-Key", "chave-de-teste");
        return cliente;
    }

    private static StringContent Json(string corpo) =>
        new(corpo, Encoding.UTF8, "application/json");

    [Fact]
    public async Task PayloadInvalido_ApareceNaListagemComOMotivoDaFalha()
    {
        // Arrange — valor negativo e id_contrato ausente: quebra a regra de negócio,
        // mas o evento precisa ser GRAVADO para o painel poder exibi-lo.
        var idTransacao = $"txn-inv-{Guid.NewGuid():N}";
        var cliente = ClienteAutenticado();

        // Act
        var recebimento = await cliente.PostAsync("/webhooks/pagamento", Json(
            $$"""
            {"id_transacao":"{{idTransacao}}","valor":-50,
             "data_pagamento":"2026-09-05T10:00:00Z","status":"Liquidado"}
            """));

        var pagina = await cliente.GetFromJsonAsync<PaginaEventos>(
            "/api/eventos?status=Falha&tamanhoPagina=100");

        // Assert
        recebimento.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var linha = pagina!.Itens.Should().ContainSingle(e => e.IdTransacao == idTransacao).Subject;
        linha.Status.Should().Be("Falha");
        linha.UltimoErro.Should().NotBeNullOrWhiteSpace(
            "o painel precisa mostrar POR QUE o evento falhou, não apenas que falhou");
    }

    [Fact]
    public async Task FiltroPorContrato_NaoDevolveEventosDeOutroContrato()
    {
        // Arrange
        var contrato = $"ctr-{Guid.NewGuid():N}";
        var cliente = ClienteAutenticado();

        await cliente.PostAsync("/webhooks/pagamento", Json(
            $$"""
            {"id_transacao":"txn-{{Guid.NewGuid():N}}","id_contrato":"{{contrato}}","valor":10.5,
             "data_pagamento":"2026-09-05T10:00:00Z","status":"Liquidado"}
            """));

        await cliente.PostAsync("/webhooks/pagamento", Json(
            $$"""
            {"id_transacao":"txn-{{Guid.NewGuid():N}}","id_contrato":"outro-contrato","valor":10.5,
             "data_pagamento":"2026-09-05T10:00:00Z","status":"Liquidado"}
            """));

        // Act
        var pagina = await cliente.GetFromJsonAsync<PaginaEventos>(
            $"/api/eventos?idContrato={contrato}");

        // Assert
        pagina!.Itens.Should().OnlyContain(e => e.IdContrato == contrato);
        pagina.Itens.Should().HaveCount(1);
    }

    [Fact]
    public async Task Detalhe_DevolveOPayloadOriginalComoJsonEstruturado()
    {
        // Arrange
        var idTransacao = $"txn-{Guid.NewGuid():N}";
        var cliente = ClienteAutenticado();

        var recebimento = await cliente.PostAsync("/webhooks/pagamento", Json(
            $$"""
            {"id_transacao":"{{idTransacao}}","id_contrato":"ctr-detalhe","valor":1500.50,
             "data_pagamento":"2026-09-05T10:00:00Z","status":"Liquidado"}
            """));

        var eventoId = (await recebimento.Content.ReadFromJsonAsync<RespostaWebhook>())!.EventoId;

        // Act — o header Location do 202 precisa apontar para uma rota que existe.
        var localizacao = recebimento.Headers.Location!.ToString();
        var detalhe = await cliente.GetFromJsonAsync<DetalheEvento>(localizacao);

        // Assert
        localizacao.Should().Be($"/api/eventos/{eventoId}");
        detalhe!.IdTransacao.Should().Be(idTransacao);

        detalhe.Payload.ValueKind.Should().Be(JsonValueKind.Object,
            "o corpo foi gravado em jsonb; devolvê-lo como string escapada obrigaria o painel a fazer parse duas vezes");
        detalhe.Payload.GetProperty("id_transacao").GetString().Should().Be(idTransacao);
    }

    [Fact]
    public async Task TamanhoPaginaAcimaDoTeto_EhLimitadoPeloServidor()
    {
        var cliente = ClienteAutenticado();

        var pagina = await cliente.GetFromJsonAsync<PaginaEventos>("/api/eventos?tamanhoPagina=1000000");

        pagina!.TamanhoPagina.Should().Be(100,
            "o cliente escolhe o filtro, mas não escolhe o custo da consulta");
    }

    [Fact]
    public async Task EventoInexistente_Retorna404()
    {
        var resposta = await ClienteAutenticado().GetAsync($"/api/eventos/{Guid.NewGuid()}");

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record PaginaEventos(
        IReadOnlyList<LinhaEvento> Itens, int Pagina, int TamanhoPagina, int Total, int TotalPaginas);

    private sealed record LinhaEvento(
        Guid Id, string IdTransacao, string? IdContrato, string Status, string? UltimoErro);

    private sealed record DetalheEvento(
        Guid Id, string IdTransacao, string Status, string? UltimoErro, JsonElement Payload);

    private sealed record RespostaWebhook(Guid EventoId, string Situacao);
}
