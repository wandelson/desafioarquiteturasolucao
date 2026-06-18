# ADR-0005: Amazon SQS para mensageria assíncrona

## Status

**Accepted** — 2025-12-14

## Contexto

A consolidação diária deve ser disparada após cada lançamento sem acoplamento síncrono. Requisitos:

- Desacoplamento entre `Lancamentos.Api` e `Consolidador.Worker`
- Retry em falhas transitórias
- DLQ para mensagens envenenadas (RF06)
- Volume moderado; equipe .NET familiar com abstrações (MassTransit)

## Decisão

Utilizar **Amazon SQS** como broker de eventos de domínio:

- Fila `lancamento-criado` com payload `LancamentoCriadoEvent`
- **Dead Letter Queue (DLQ)** após N tentativas (recomendado: 3–5)
- Consumer: Lambda em produção; Worker .NET + MassTransit no POC
- Visibility timeout alinhado ao p99 de processamento da consolidação
- Abstração `IEventQueue` no domínio para não acoplar a MassTransit

## Alternativas consideradas

| Alternativa | Por que foi descartada |
|-------------|------------------------|
| **Amazon EventBridge** | Excelente para roteamento multi-consumidor; para 1 produtor → 1 consumidor, SQS é mais simples e barato |
| **SNS fan-out + SQS** | Útil se múltiplos consumidores no futuro; YAGNI no MVP |
| **Apache Kafka (MSK)** | Custo operacional e complexidade desproporcionais ao volume (~1M msg/mês) |
| **Chamada HTTP direta** ao worker | Acoplamento síncrono; sem buffer em picos; sem retry nativo |
| **RabbitMQ self-hosted** | Gestão de cluster; contraria decisão serverless gerenciado |

## Consequências

### Positivas

- Buffer de picos (RFN escalabilidade)
- Retry e DLQ nativos
- Pay-per-request alinhado a FinOps
- MassTransit abstrai SQS no POC e produção

### Negativas

- **At-least-once delivery** — consumer deve ser idempotente (ver ADR-0008)
- Ordenação não garantida entre mensagens (exceto FIFO com trade-off de throughput)
- Observabilidade de lag exige métricas `ApproximateNumberOfMessagesVisible`
- Debugging distribuído mais difícil que chamada local

### Configuração recomendada (produção)

| Parâmetro | Valor sugerido | Motivo |
|-----------|----------------|--------|
| `maxReceiveCount` → DLQ | 5 | Balancear retry vs. poison message |
| Visibility timeout | 6× duração média do handler | Evitar reprocessamento paralelo |
| Retention DLQ | 14 dias | Análise post-mortem |

## Estado no POC

| Item | Status | Evidência |
|------|--------|-----------|
| Publicação na fila | ✅ | `MassTransitEventQueue.cs` |
| Consumo com MassTransit | ✅ | `Consolidador.Worker` |
| LocalStack SQS | ✅ | `docker-compose.yml` |
| Retry (3×, 5 s) | ✅ | `UseMessageRetry` no worker |
| DLQ configurada | ❌ | Não há fila DLQ no LocalStack setup |
| Idempotência / dedup | ❌ | |
| Métricas de fila | ❌ | |

O README listava DLQ como requisito (RF06) e "próximo passo" — inconsistência corrigida neste ADR.
