# Pagamentos.Webhooks

Recebe notificações de pagamento de um banco parceiro, garante que o mesmo evento não
seja processado duas vezes, processa em background e expõe os eventos para consulta.


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

Com a API no ar, o Swagger fica em **`http://localhost:5213/swagger`**. Para exercitar o
`POST`, clique em **Authorize** e informe a ApiKey `chave-local-de-desenvolvimento`.

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

Três endpoints. Com a aplicação rodando, o Swagger em `/swagger` documenta os schemas e
permite executar cada um.

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/webhooks/pagamento` | Recebe a notificação do parceiro. Exige o header `X-Api-Key` |
| `GET` | `/api/eventos` | Lista os eventos. Filtros: `status`, `idContrato`, `pagina`, `tamanhoPagina` |
| `GET` | `/api/eventos/{id}` | Detalhe de um evento, com o payload original recebido |

Corpo do `POST`:

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

| Código | Situação | Gravou? |
|---|---|---|
| `202 Accepted` | Aceito e enfileirado para processamento | sim |
| `200 OK` | `id_transacao` já recebido antes; devolve o evento original | não |
| `422 Unprocessable Entity` | Regra de negócio violada | **sim**, com status `Falha` e o motivo |
| `400 Bad Request` | JSON malformado ou `id_transacao` ausente | não |
| `401 Unauthorized` | ApiKey ausente ou inválida | não |

O `422` grava de propósito: um payload que o parceiro mandou errado é a informação mais
útil para o operador. Consulte `GET /api/eventos?status=Falha` para vê-los com o motivo
em `ultimoErro`.

A listagem é paginada no banco, com teto de 100 itens por página.

O CORS está habilitado apenas para a origem `http://localhost:5173`, do painel
administrativo.

O arquivo `Api/Api.http` traz os nove cenários prontos para execução no Visual Studio,
VS Code ou Rider.

## Estrutura

```
Api/              endpoints, filtros, Program.cs
Application/      domínio, contratos, serviços, validação, worker
Infrastructure/   EF Core, repositórios, mensageria, segurança
Tests/            Unit/, Integration/, Fixtures/
```
