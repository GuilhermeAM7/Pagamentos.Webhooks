# Pagamentos.Webhooks

Recebe notificações de pagamento de um banco parceiro, garante que o mesmo evento não
seja processado duas vezes, processa em background e expõe os eventos para consulta.

Escopo entregue: backend.

## Stack

- .NET 10 / ASP.NET Core Minimal API
- PostgreSQL 17
- EF Core + Npgsql
- xUnit, FluentAssertions e Testcontainers

## Como rodar

Pré-requisitos: .NET 10 SDK e Docker, mais a ferramenta de linha de comando do EF Core,
usada para aplicar as migrations:

```bash
dotnet tool install --global dotnet-ef
```

**1. Subir o banco** (aguarde ficar healthy):

```bash
docker compose up -d
```

**2. Restaurar as dependências:**

```bash
dotnet restore
```

**3. Aplicar as migrations:**

```bash
# PowerShell:  $env:ASPNETCORE_ENVIRONMENT = "Development"
# bash:        export ASPNETCORE_ENVIRONMENT=Development
dotnet ef database update --project Infrastructure --startup-project Api
```

**4. Subir a API** em `http://localhost:5213`:

```bash
dotnet run --project Api --launch-profile http
```

Para inspecionar o banco, o Adminer sobe em `http://localhost:8080`:

```bash
docker compose --profile ferramentas up -d
```

Na tela de login, **troque o sistema para PostgreSQL** — o Adminer abre selecionado em
MySQL/MariaDB:

| Campo | Valor |
|---|---|
| Sistema | PostgreSQL |
| Servidor | `postgres` |
| Usuário | `pagamentos` |
| Senha | `pagamentos_local_dev` |
| Base de dados | `pagamentos_webhooks` |

O servidor é `postgres`, e não `localhost`, porque o Adminer roda em container e alcança
o banco pelo nome do serviço na rede do Compose.

Para zerar tudo, incluindo o volume: `docker compose down -v`.

A connection string e a ApiKey de desenvolvimento estão em
`Api/appsettings.Development.json` e batem com os defaults do `docker-compose.yml`, para
o projeto rodar sem configuração adicional. Em produção vêm do ambiente
(`ConnectionStrings__Postgres` e `SegurancaWebhook__ApiKey`).

## Testes

```bash
dotnet test
```

Os testes de integração sobem um PostgreSQL via Testcontainers, então basta ter o Docker
rodando. Para executar apenas eles:

```bash
dotnet test --filter "Categoria=Integracao"
```

## API

O arquivo `Api/Api.http` contém todos os cenários abaixo prontos para execução no Visual
Studio, VS Code ou Rider.

### POST /webhooks/pagamento

Recebe a notificação. Header obrigatório: `X-Api-Key`.

```json
{
  "id_transacao": "txn-001",
  "id_contrato": "ctr-100",
  "valor": 1500.50,
  "data_pagamento": "2026-09-05T10:00:00Z",
  "status": "Liquidado"
}
```

`id_transacao` é a chave de idempotência. Campos não previstos no contrato são aceitos e
preservados no payload gravado.

| Código | Situação |
|---|---|
| `202 Accepted` | Aceito e enfileirado para processamento |
| `200 OK` | `id_transacao` já recebido antes; devolve o evento original |
| `422 Unprocessable Entity` | Regra de negócio violada; o evento é gravado com status `Falha` e o motivo |
| `400 Bad Request` | JSON malformado ou `id_transacao` ausente |
| `401 Unauthorized` | ApiKey ausente ou inválida |

Resposta do `202`, com `Location: /api/eventos/{id}`:

```json
{ "eventoId": "fae9417e-ca1f-4acd-904d-ac21ce6b310a", "situacao": "Aceito" }
```

Resposta do `422`:

```json
{
  "eventoId": "22b6184b-d849-4bbb-888c-3aa9bd4b7a91",
  "situacao": "Rejeitado",
  "erros": "id_contrato é obrigatório. | valor deve ser maior que zero (recebido: -50)."
}
```

### GET /api/eventos

Lista os eventos recebidos, do mais recente para o mais antigo.

| Parâmetro | Padrão | Observação |
|---|---|---|
| `status` | — | `Pendente`, `EmProcessamento`, `Concluido` ou `Falha` |
| `idContrato` | — | Filtro exato |
| `pagina` | `1` | |
| `tamanhoPagina` | `20` | Limitado a 100 |

```
GET /api/eventos?status=Falha&idContrato=ctr-100&pagina=1&tamanhoPagina=20
```

```json
{
  "itens": [
    {
      "id": "22b6184b-d849-4bbb-888c-3aa9bd4b7a91",
      "idTransacao": "txn-002",
      "idContrato": "ctr-100",
      "valor": -50.0,
      "dataPagamento": "2026-09-05T10:00:00+00:00",
      "dataRecebido": "2026-09-07T04:19:25.500799+00:00",
      "dataProcessado": null,
      "status": "Falha",
      "statusPagamento": "Liquidado",
      "tentativas": 0,
      "ultimoErro": "id_contrato é obrigatório. | valor deve ser maior que zero (recebido: -50)."
    }
  ],
  "pagina": 1,
  "tamanhoPagina": 20,
  "total": 1,
  "totalPaginas": 1
}
```

### GET /api/eventos/{id}

Detalhe de um evento. Além dos campos do resumo, traz `statusOrigem` e o payload original
recebido, em `payload`. Retorna `404` se o evento não existir.

```json
{
  "id": "fae9417e-ca1f-4acd-904d-ac21ce6b310a",
  "idTransacao": "txn-009",
  "idContrato": "ctr-100",
  "valor": 10.0,
  "dataPagamento": "2026-09-05T10:00:00+00:00",
  "dataRecebido": "2026-09-07T04:19:27.118204+00:00",
  "dataProcessado": "2026-09-07T04:19:29.495018+00:00",
  "statusOrigem": "Liquidado",
  "status": "Concluido",
  "statusPagamento": "Liquidado",
  "tentativas": 1,
  "ultimoErro": null,
  "payload": {
    "id_transacao": "txn-009",
    "id_contrato": "ctr-100",
    "valor": 10,
    "data_pagamento": "2026-09-05T10:00:00Z",
    "status": "Liquidado"
  }
}
```

## Estrutura

```
Api/              endpoints, filtros, Program.cs
Application/      domínio, contratos, serviços, validação, worker
Infrastructure/   EF Core, repositórios, mensageria, segurança
Tests/            Unit/, Integration/, Fixtures/
```
