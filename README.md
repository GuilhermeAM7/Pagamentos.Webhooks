# Sabemi — Serviço de Webhooks de Pagamento

Recebe notificações de pagamento (webhooks) de um banco parceiro, garante que o mesmo
evento não seja processado duas vezes, processa em background e expõe os eventos para um
painel administrativo.

**Escopo entregue: backend.** O frontend era desafio opcional e não foi feito — a API de
leitura que o painel consumiria está implementada e testada.

---

## Como rodar

Pré-requisitos: **.NET 10 SDK** e **Docker**.

```bash
# 1. Banco (aguarde ficar healthy)
docker compose up -d

# 2. Migrations
#    PowerShell:  $env:ASPNETCORE_ENVIRONMENT = "Development"
#    bash:        export ASPNETCORE_ENVIRONMENT=Development
dotnet ef database update --project Infrastructure --startup-project Api

# 3. API em http://localhost:5213
dotnet run --project Api --launch-profile http
```

Opcional — Adminer em `http://localhost:8080` (servidor: `postgres`):

```bash
docker compose --profile ferramentas up -d
```

Para zerar tudo, incluindo o volume: `docker compose down -v`.

### Testes

```bash
dotnet test
```

Os testes de integração sobem um PostgreSQL real via **Testcontainers** — só precisam do
Docker rodando, não do `docker compose up`. Para rodar só eles:

```bash
dotnet test --filter "Categoria=Integracao"
```

### Exercitando a API

`Api/Api.http` traz os 9 cenários da entrega (202, 200 duplicado, 422, 400, 401 e as
consultas do painel), executáveis direto do Visual Studio, VS Code ou Rider.

> **Sobre as credenciais no repositório.** A connection string e a ApiKey de
> desenvolvimento estão versionadas em `Api/appsettings.Development.json`, e são
> deliberadamente descartáveis: batem com os defaults do `docker-compose.yml` para que
> `clone → compose up → run` funcione sem configuração. Em produção viriam do ambiente
> (`ConnectionStrings__Postgres`, `SegurancaWebhook__ApiKey`); localmente podem ser
> sobrescritas por User Secrets.

---

## Contrato da API

### `POST /webhooks/pagamento`

Header obrigatório: `X-Api-Key`.

```json
{
  "id_transacao": "txn-001",
  "id_contrato": "ctr-100",
  "valor": 1500.50,
  "data_pagamento": "2026-09-05T10:00:00Z",
  "status": "Liquidado"
}
```

| Código | Situação | Gravou? |
|---|---|---|
| `202 Accepted` | Aceito, enfileirado para processamento | sim |
| `200 OK` | `id_transacao` já recebido — idempotente | não (devolve o evento original) |
| `422 Unprocessable` | Regra de negócio violada | **sim**, com `Falha` + motivo |
| `400 Bad Request` | JSON malformado ou `id_transacao` ausente | não |
| `401 Unauthorized` | ApiKey ausente ou inválida | não |

O `202` devolve `Location: /api/eventos/{id}`.

Campos desconhecidos enviados pelo parceiro não quebram a desserialização — são
capturados por `[JsonExtensionData]`, e o corpo original é gravado íntegro de qualquer
forma.

### `GET /api/eventos`

Listagem do painel. Filtros: `status`, `idContrato`, `pagina`, `tamanhoPagina`.

```
GET /api/eventos?status=Falha&idContrato=ctr-100&pagina=1&tamanhoPagina=20
```

Filtro e paginação são **sempre** aplicados no banco, com teto de 100 itens por página.
A listagem não carrega o `payload_json` — seria um `jsonb` por linha só para desenhar uma
tabela.

### `GET /api/eventos/{id}`

Detalhe, incluindo o payload original **como JSON estruturado**, não como string
escapada — o corpo foi gravado em `jsonb`; devolvê-lo entre aspas obrigaria o painel a
fazer parse duas vezes.

---

## Decisões de arquitetura

### 1. O endpoint rejeita pouco; o banco registra tudo

Só é recusado na porta o que é **inautêntico** (ApiKey inválida → 401) ou **ilegível**
(JSON malformado, `id_transacao` ausente → 400). Todo o resto é **persistido primeiro e
validado depois**: regra de negócio quebrada vira uma linha com `Status = Falha` e
`UltimoErro` preenchido, e a resposta é 422.

Isso é o oposto do reflexo comum de validar na entrada e devolver 400. A razão é
concreta: o enunciado pede **visualização de erros** no painel. Se o backend rejeitasse
payload inválido sem gravar, não haveria erro nenhum para exibir — o requisito seria
impossível de atender. Um payload que o parceiro mandou errado é justamente a informação
mais valiosa para o operador.

### 2. Idempotência é garantida pelo banco, não pelo código

```csharp
contexto.Eventos.Add(evento);
try            { await contexto.SaveChangesAsync(ct); return Inserido(...); }
catch (DbUpdateException ex) when (EhDuplicidadeDeTransacao(ex)) { ... }
```

O caminho óbvio — `SELECT` para checar se já existe, depois `INSERT` — tem uma janela de
corrida entre as duas consultas. Sob entrega concorrente (que é o normal em webhook: o
parceiro reenvia por timeout enquanto a primeira ainda processa), duas requisições passam
pelo `SELECT` juntas e ambas inserem.

A garantia está numa **constraint `UNIQUE`** em `id_transacao`. O código tenta inserir e
trata a violação, capturada de forma estreita — `SqlState 23505` **e**
`ConstraintName = ux_eventos_webhook_id_transacao`, para não engolir outra violação de
unicidade que apareça no futuro.

Dois detalhes que só aparecem quando isso roda de verdade:

- Após a violação a entidade continua rastreada como `Added`. Sem
  `Entry(evento).State = Detached`, o próximo `SaveChanges` do mesmo escopo reenviaria o
  INSERT condenado.
- Se a violação ocorre mas nenhuma linha correspondente é encontrada, a exceção é
  relançada — é um caso que não deveria existir, e mascará-lo esconderia um bug.

**É o teste mais importante do projeto:** `IdempotenciaTests` dispara 10 POSTs
simultâneos com o mesmo `id_transacao` e exige **1 linha, 1× 202 e 9× 200**. Uma
implementação com `SELECT` antes do `INSERT` falha nele.

### 3. Enfileirar só depois do commit, e só o `Guid`

O evento vai para um `Channel` em memória **depois** que a transação commitou, e o que
trafega é apenas o identificador — o worker relê do banco. Enfileirar antes do commit
cria uma corrida onde o worker busca um evento que ainda não existe; enfileirar o objeto
inteiro faria a fila carregar estado que pode já estar obsoleto.

O canal é `CreateBounded`, não `CreateUnbounded`. Uma fila ilimitada não tem
backpressure: sob rajada, ela cresce até o processo morrer por memória. Limitada, o
produtor espera — degrada a latência em vez de derrubar o serviço.

### 4. Escopo por mensagem no worker

`BackgroundService` é singleton e `DbContext` é scoped. Injetar o `DbContext` direto no
worker o transformaria de fato em singleton — um contexto vivo pelo tempo do processo,
acumulando entidades rastreadas e vazando estado entre mensagens. O worker cria um escopo
por mensagem via `IServiceScopeFactory`.

O `try/catch` dentro do loop nunca deixa exceção escapar: uma exceção não tratada em
`ExecuteAsync` mata o `BackgroundService` **silenciosamente**, e o serviço passaria a
aceitar webhooks sem nunca mais processá-los.

### 5. A projeção não acumula

`status_contrato` é o estado consolidado por contrato. Nenhum campo usa `+=`: acumulador
não é idempotente, e se o mesmo evento for aplicado duas vezes o total fica errado sem
deixar rastro. Todo campo é **estado absoluto** derivado de um único evento.

`TentarAplicarPagamento` ignora eventos mais antigos que o estado já consolidado — a
projeção converge para o mesmo resultado **independentemente da ordem** em que os eventos
chegarem, o que é a única premissa segura com entrega assíncrona. Há teste unitário para
essa convergência.

### 6. Detalhes de modelagem

- **`decimal`** para valor monetário, nunca `double`.
- **Enums gravados como string** (`HasConversion<string>()`): legível em consulta direta e
  imune a reordenação do enum.
- **`DateTimeOffset` normalizado para UTC** na criação — o Npgsql só aceita offset zero em
  `timestamptz`.
- **`payload_json` em `jsonb`**, gravado como veio: é o log bruto, a fonte da verdade para
  auditar o que o parceiro realmente mandou. Nunca é re-serializado.
- **Índices, cada um justificado por uma consulta:** `UNIQUE(id_transacao)` (idempotência),
  `(id_contrato)` (filtro do painel), `(status, data_recebido DESC)` (painel).
- **`xmin` como token de concorrência** em `status_contrato`, via shadow property.
- Comparação de ApiKey com **`CryptographicOperations.FixedTimeEquals`**, não `==` — `==`
  faz curto-circuito no primeiro byte diferente e vaza o prefixo correto por tempo de
  resposta.

---

## Testes

**67 testes, todos verdes.**

- `Tests/Unit/` — domínio puro, sem banco e sem mock: transições de estado,
  convergência da projeção, validador e conversão de status (incluindo strings numéricas,
  que `Enum.TryParse` aceita — daí o `Enum.IsDefined`).
- `Tests/Integration/IdempotenciaTests.cs` — a corrida de 10 requisições descrita acima.
- `Tests/Integration/ListagemEventosTests.cs` — a API do painel, incluindo o caminho
  completo da visualização de erros: um payload inválido devolve 422 **e** aparece na
  listagem com o motivo.

Os testes de integração usam **PostgreSQL real** via Testcontainers. O provider InMemory
do EF Core não implementa constraints únicas — a suíte passaria com a idempotência
quebrada, que é exatamente o bug que ela existe para pegar.

---

## O que eu faria diferente em produção

| Hoje | Em produção | Por quê |
|---|---|---|
| ApiKey em header | **HMAC-SHA256** do corpo, com timestamp e janela anti-replay | ApiKey vaza em log de proxy e não prova que o corpo não foi adulterado |
| `Channel` em memória | **Broker durável** (RabbitMQ/SQS) com DLQ | O que está no canal se perde se o processo cair |
| `GET /api/eventos` aberto | Autenticação própria do painel (OIDC) | A ApiKey autentica o *parceiro*, não o *operador* — reutilizá-la daria ao banco acesso de leitura ao painel |
| Paginação por offset | **Keyset pagination** | `OFFSET` degrada linearmente e pode pular linhas sob escrita concorrente |
| `ILogger` | **OpenTelemetry** com trace propagado até o worker | Hoje não dá para seguir um `id_transacao` de ponta a ponta |
| Retry inline | **Polly** com backoff exponencial e jitter | Retry ingênuo sincroniza clientes e amplifica a falha |

E o **`ReconcilerWorker`**: um `PeriodicTimer` chamando `ObterTravadosAsync` — que já
existe em `IRepositorioEventoWebhook` — para reenfileirar eventos que ficaram `Pendente`
ou `EmProcessamento` além do limite. É o que fecha o argumento "e se o processo cair no
meio?". Vale registrar que a durabilidade **já está no banco**: a fila em memória é
otimização de latência, não a fonte da verdade. Nenhum evento aceito é perdido; ele fica
`Pendente` até ser reprocessado. O reconciliador automatiza essa retomada.

---

## O que ficou de fora, deliberadamente

- **HMAC-SHA256 na entrada.** O enunciado pedia validação "simples", e o atrito de assinar
  cada requisição atrapalharia a demonstração. Está na tabela acima como evolução.
- **Endpoint `POST /dev/simular`.** `Api/Api.http` já cobre os cenários de forma
  declarativa; um endpoint de simulação seria código a mais para o mesmo resultado, e uma
  rota de desenvolvimento viajando junto com o código de produção.
- **Serilog, Polly, Dockerfile da API no compose.** Nenhum resolve um problema que este
  serviço tem hoje. Saber quando *não* adicionar infraestrutura é parte da decisão.

---

## Stack

.NET 10 · ASP.NET Core Minimal API · PostgreSQL 17 · EF Core + Npgsql ·
xUnit + FluentAssertions + Testcontainers

Direção de dependência: `Api → Application ← Infrastructure`. A `Application` não
referencia nenhum pacote de infraestrutura — só abstrações. A `Api` referencia
`Infrastructure` **apenas** no composition root.

```
Api/              endpoints, filtros, Program.cs, DI raiz
Application/      domínio, contratos, serviços, validação, workers, abstrações
Infrastructure/   EF Core, repositórios, mensageria, segurança
Tests/            Unit/, Integration/, Fixtures/
```
