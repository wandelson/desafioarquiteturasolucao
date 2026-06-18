# C4 Model — Nível 2: Diagrama de Containers

> **Propósito:** mostrar a decomposição em **containers** (aplicações/serviços deployáveis), suas responsabilidades, tecnologias e protocolos de comunicação — padrão esperado para revisão de arquitetura sênior.

## Princípios de decomposição

| Princípio | Como se manifesta |
|-----------|-------------------|
| **CQRS** | Escrita (`Lançamentos`) separada da leitura (`Relatórios`) |
| **Event-driven** | Consolidação reage a `LancamentoCriado` sem bloquear o POST |
| **Resiliência** | Falha na consolidação não impede registro do lançamento |
| **Strangler Fig** | Front legado redireciona funcionalidades migradas para o novo stack |

## Diagrama de Containers — Produção (alvo AWS)

```mermaid
C4Container
    title Diagrama de Containers — Arquitetura-alvo (Produção AWS)

    Person(comerciante, "Comerciante", "Usuário do fluxo de caixa")

    System_Boundary(fluxoCaixa, "Sistema de Fluxo de Caixa") {
        Container(front, "Front-end Blazor WASM", "Blazor WebAssembly", "Interface do comerciante: registro e consulta de saldos")
        Container(apiGw, "API Gateway", "Amazon API Gateway HTTP API", "Terminação TLS, validação JWT, roteamento, throttling")
        Container(apiLanc, "API de Lançamentos", "AWS Lambda (.NET)", "Command side: valida, persiste lançamento, publica evento")
        Container(worker, "Worker de Consolidação", "AWS Lambda (.NET)", "Reage a eventos; recalcula e persiste saldo diário")
        Container(apiRel, "API de Relatórios", "AWS Lambda (.NET)", "Query side: consulta saldo consolidado")
        ContainerDb(aurora, "Banco Transacional", "Aurora Serverless v2 (PostgreSQL)", "Write model: lançamentos ACID; fallback de leitura")
        ContainerDb(redis, "Cache de Saldos", "ElastiCache Redis", "Read model: saldos consolidados por dia/merchant")
        ContainerQueue(sqs, "Fila de Eventos", "Amazon SQS", "Desacoplamento write → consolidação; retry e DLQ")
        Container(cdn, "CDN + Static Hosting", "CloudFront + S3", "Distribuição do front Blazor WASM")
    }

    System_Ext(cognito, "Amazon Cognito", "OIDC — emissão de JWT")
    System_Ext(cloudwatch, "CloudWatch", "Logs, métricas, alarmes")
    System_Ext(legado, "Sistema Legado", "Monólito em migração")

    Rel(comerciante, cdn, "Acessa SPA", "HTTPS")
    Rel(comerciante, cognito, "Login OIDC", "HTTPS")
    Rel(front, cognito, "Obtém tokens", "OIDC/PKCE")
    Rel(front, apiGw, "Chama APIs", "HTTPS + Bearer JWT")
    Rel(apiGw, cognito, "Valida JWT", "JWKS")
    Rel(apiGw, apiLanc, "POST /lancamentos", "Lambda proxy")
    Rel(apiGw, apiRel, "GET /relatorios/{dia}", "Lambda proxy")
    Rel(apiLanc, aurora, "Persiste lançamento", "SQL/TLS")
    Rel(apiLanc, sqs, "Publica LancamentoCriado", "SQS SendMessage")
    Rel(sqs, worker, "Dispara consolidação", "Event source mapping")
    Rel(worker, aurora, "Lê lançamentos do dia", "SQL/TLS")
    Rel(worker, redis, "Atualiza saldo consolidado", "Redis")
    Rel(apiRel, redis, "Consulta saldo (primário)", "Redis")
    Rel(apiRel, aurora, "Fallback se cache miss", "SQL/TLS")
    Rel(apiLanc, cloudwatch, "Logs estruturados")
    Rel(worker, cloudwatch, "Logs estruturados")
    Rel(apiRel, cloudwatch, "Logs estruturados")
    Rel(legado, aurora, "CDC replica dados históricos", "DMS/CDC")
```

## Diagrama de Containers — POC local (implementado)

```mermaid
C4Container
    title Diagrama de Containers — POC Local (repositório atual)

    Person(dev, "Desenvolvedor / Avaliador", "Executa e testa o POC")

    System_Boundary(poc, "POC Fluxo de Caixa") {
        Container(frontPoc, "Front-end Blazor WASM", "ASP.NET Core + Blazor", "Tela de fluxo de caixa (dev local)")
        Container(apiLancPoc, "Lancamentos.Api", "ASP.NET Core 10", "POST /api/lancamentos — escrita")
        Container(workerPoc, "Consolidador.Worker", ".NET Worker + MassTransit", "Consome fila lancamento-criado")
        Container(apiRelPoc, "Relatorios.Api", "ASP.NET Core 10", "GET /api/relatorios/{dia} — leitura")
        ContainerDb(pg, "PostgreSQL", "PostgreSQL 16", "lancamentos + saldos_diarios (write e read model no mesmo DB)")
        ContainerQueue(localSqs, "Fila SQS", "LocalStack SQS", "Fila lancamento-criado via MassTransit")
    }

    Rel(dev, frontPoc, "Navega", "HTTPS")
    Rel(dev, apiLancPoc, "Swagger / API", "HTTP")
    Rel(dev, apiRelPoc, "Swagger / API", "HTTP")
    Rel(frontPoc, apiLancPoc, "Registra lançamento", "HTTP")
    Rel(frontPoc, apiRelPoc, "Consulta relatório", "HTTP")
    Rel(apiLancPoc, pg, "INSERT lancamentos", "SQL")
    Rel(apiLancPoc, localSqs, "Send LancamentoCriado", "SQS")
    Rel(localSqs, workerPoc, "Consume", "SQS")
    Rel(workerPoc, pg, "SELECT + UPSERT saldos_diarios", "SQL")
    Rel(apiRelPoc, pg, "SELECT saldos_diarios", "SQL")
```

## Matriz de containers

| Container | Responsabilidade | Tecnologia (produção) | Tecnologia (POC) | Protocolos |
|-----------|------------------|----------------------|-------------------|------------|
| Front-end | UX do comerciante | Blazor WASM + CloudFront/S3 | Blazor WASM (Kestrel dev) | HTTPS, OIDC (prod) |
| API Gateway | Segurança na borda, roteamento | API Gateway HTTP API | *Ausente* — APIs expostas diretamente | HTTPS, JWT |
| API Lançamentos | Validar e persistir; publicar evento | Lambda .NET | `Lancamentos.Api` | REST/JSON |
| Worker Consolidação | Recalcular saldo do dia | Lambda .NET (SQS trigger) | `Consolidador.Worker` | Mensageria |
| API Relatórios | Consultar saldo consolidado | Lambda .NET | `Relatorios.Api` | REST/JSON |
| Banco transacional | Fonte da verdade dos lançamentos | Aurora Serverless v2 | PostgreSQL 16 | SQL |
| Cache / read model | Leitura rápida de saldos | Redis | Tabela `saldos_diarios` no PostgreSQL | Redis / SQL |
| Fila | Desacoplamento assíncrono | SQS + DLQ | LocalStack SQS | SQS |

## Fluxos principais entre containers

### 1. Registro de lançamento (write path)

```
Comerciante → Front → API Gateway → API Lançamentos → Aurora
                                              ↓
                                            SQS → Worker → Redis (+ Aurora read)
```

**Garantia:** o lançamento é persistido antes da publicação do evento (ordem no handler). A consolidação é **eventualmente consistente**.

### 2. Consulta de saldo (read path)

```
Comerciante → Front → API Gateway → API Relatórios → Redis
                                              ↓ (miss)
                                            Aurora (saldos_diarios)
```

### 3. Migração (Strangler)

```
Legado → CDC → Aurora (dados históricos)
Legado Front → redirect → Novo Front (funcionalidades migradas)
```

## Gaps críticos: POC vs. produção

| Aspecto | Produção (diagrama superior) | POC (código atual) | Impacto |
|---------|------------------------------|--------------------|---------|
| Autenticação | Cognito + JWT no API Gateway | Nenhuma | Qualquer cliente pode chamar as APIs |
| Multi-tenant | `merchantId` em todas as entidades | Ausente no schema | Dados não isolados por comerciante |
| Read model | Redis com fallback Aurora | Tabela PostgreSQL | Funcional, mas sem latência < 50 ms prometida |
| DLQ / idempotência | SQS DLQ + deduplicação | Retry MassTransit (3×); sem DLQ; reprocessamento recalcula dia inteiro | Risco em mensagens duplicadas |
| Compute | Lambda serverless | Processos ASP.NET/Core long-running | POC não valida cold start, IAM por função |
| Observabilidade | CloudWatch + X-Ray | `Console.WriteLine` no worker | Sem métricas/alertas reais |

Esses gaps são tratados nos [ADRs](../adr/README.md) (decisão + estado de implementação) e nos [requisitos funcionais](../requirements/requisitos-funcionais.md).

## Referências

- [C4 Model — Contexto](c4-context.md)
- [ADR-002: CQRS e eventos](../adr/0002-cqrs-event-driven-consolidation.md)
- [ADR-004: Redis como read model](../adr/0004-redis-read-model.md)
- [RBAC](../security/rbac.md)
