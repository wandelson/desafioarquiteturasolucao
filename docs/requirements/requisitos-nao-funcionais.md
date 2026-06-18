# Requisitos Não Funcionais

RNFs mensuráveis do Sistema de Fluxo de Caixa, com **meta**, **método de medição** e **decisão arquitetural** que os suporta.

---

## RNF críticos do enunciado do desafio

| Regra (enunciado) | Meta | Como a arquitetura atende | Documento |
|-------------------|------|---------------------------|-----------|
| Serviço de **lançamentos** não fica indisponível se o **consolidado** cair | Disponibilidade da escrita independente da consolidação | CQRS: POST persiste e publica evento; consolidação assíncrona via fila | [ADR-0002](../adr/0002-cqrs-event-driven-consolidation.md) |
| Serviço de **consolidado** suporta picos de **50 req/s** | ≥ 50 eventos/s processáveis com escala do consumer | Fila SQS absorve pico; worker/Lambda escala por concurrency; recálculo por dia é O(n) — ver evolução em ADR-0008 | [ADR-0005](../adr/0005-sqs-async-messaging.md) · RNF02.1 |
| Máximo **5% de perda** de requisições no consolidado | ≤ 5% mensagens não processadas após retries | Retry (3–5×), DLQ, idempotência no handler, alarme `DLQ depth > 0`, runbook de reprocessamento | [ADR-0005](../adr/0005-sqs-async-messaging.md) · [ADR-0008](../adr/0008-consolidation-full-recalc-strategy.md) |

> **Nota:** 50 req/s refere-se ao **throughput de consolidação** (eventos na fila), não ao POST de lançamentos. O desacoplamento permite que picos de escrita sejam absorvidos pela fila sem derrubar a API de lançamentos.

---

## RNF01 — Desempenho

| ID | Requisito | Meta | Medição | Decisão / POC |
|----|-----------|------|---------|---------------|
| RNF01.1 | Registro de lançamento (p95) | < 200 ms | CloudWatch + testes de carga | CQRS: escrita sem consolidação síncrona (ADR-0002) |
| RNF01.2 | Consulta saldo diário (p95) | < 50 ms | APM / CloudWatch | Redis read model (ADR-0004) — **POC não atende** (PostgreSQL direto) |
| RNF01.3 | Lag consolidação (p95) | < 5 s | Métrica SQS `ApproximateAgeOfOldestMessage` | SQS + worker dedicado (ADR-0005) |

---

## RNF02 — Escalabilidade

| ID | Requisito | Meta | Decisão |
|----|-----------|------|---------|
| RNF02.1 | Throughput do consolidado (eventos/s) | ≥ 50 req/s em pico | Concurrency Lambda/worker + profundidade da fila | SQS (ADR-0005) |
| RNF02.2 | Absorção de picos de escrita | Fila buffering sem rejeitar POST | SQS (ADR-0005) |
| RNF02.3 | Perda máxima no consolidado | ≤ 5% | Retry + DLQ + idempotência (ADR-0005, ADR-0008) |
| RNF02.4 | Escala independente read/write | Sim | CQRS containers separados (C4 L2) |

---

## RNF03 — Disponibilidade

| ID | Requisito | Meta | Decisão |
|----|-----------|------|---------|
| RNF03.1 | Multi-AZ | Sim | Aurora, Redis, Lambda em VPC multi-AZ |
| RNF03.2 | Falha na consolidação não bloqueia escrita | Sim | Event-driven (ADR-0002) — **validado no POC** |
| RNF03.3 | SLA alvo (produção) | 99.9% mensal | Health checks, alarmes RF12 |

---

## RNF04 — Segurança

| ID | Requisito | Implementação |
|----|-----------|---------------|
| RNF04.1 | TLS 1.2+ em trânsito | API GW, CloudFront |
| RNF04.2 | Autenticação OAuth2/OIDC | Cognito (ADR-0006) — **POC: ausente** |
| RNF04.3 | RBAC + isolamento tenant | ADR-0009, [rbac.md](../security/rbac.md) |
| RNF04.4 | IAM least privilege por Lambda | ADR-0007 |
| RNF04.5 | Criptografia em repouso | Aurora KMS, Redis encryption at rest |
| RNF04.6 | Auditoria de acesso | CloudTrail + logs estruturados RF11 |

---

## RNF05 — Manutenibilidade

| ID | Requisito | Implementação |
|----|-----------|---------------|
| RNF05.1 | Baixo acoplamento entre bounded contexts | APIs + fila separadas |
| RNF05.2 | Decisões documentadas | ADRs em `docs/adr/` |
| RNF05.3 | Testes automatizados | Unit (`Core.UnitTests`) + integração (`Api.IntegrationTests`) |
| RNF05.4 | Migrations versionadas | EF Core migrations |

---

## RNF06 — Custo (FinOps)

| ID | Requisito | Meta |
|----|-----------|------|
| RNF06.1 | Custo mensal estimado (1M req/mês) | ≈ USD 100 |
| RNF06.2 | Pay-per-use em compute | Lambda (ADR-0007) |
| RNF06.3 | Cache reduz ACU Aurora | Redis (ADR-0004) |

Ver estimativa detalhada no [README seção 14](../../README.md).

---

## RNF07 — Observabilidade

| ID | Requisito | Ferramenta |
|----|-----------|------------|
| RNF07.1 | Logs centralizados | CloudWatch Logs |
| RNF07.2 | Métricas de negócio | Lançamentos/dia, saldo médio, lag consolidação |
| RNF07.3 | Tracing distribuído | AWS X-Ray |
| RNF07.4 | Alarmes acionáveis | CloudWatch Alarms → SNS |

**Status POC:** ❌ Observabilidade não implementada (listada como próximo passo no README)

---

## Matriz RNF × Status POC

| Categoria | Produção (especificado) | POC |
|-----------|-------------------------|-----|
| Desempenho leitura | Redis < 50 ms | PostgreSQL, não medido |
| Segurança | Cognito + RBAC | Sem auth |
| Observabilidade | CloudWatch + X-Ray | Console.WriteLine |
| Escalabilidade | Serverless | Processos locais fixos |
| Resiliência write/read | Lançamentos up se consolidado down | ✅ Desacoplamento no POC |
| Consolidado 50 req/s / ≤5% perda | Retry + DLQ + escala consumer | 📋 Especificado; DLQ não no POC |
