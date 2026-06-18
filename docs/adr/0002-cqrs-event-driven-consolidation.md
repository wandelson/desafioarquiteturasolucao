# ADR-0002: CQRS com consolidação assíncrona orientada a eventos

## Status

**Accepted** — 2025-12-14

## Contexto

O problema central do domínio: ao registrar um lançamento, o comerciante precisa de **confirmação rápida** da escrita; o **saldo consolidado do dia** pode ser calculado de forma assíncrona sem bloquear a requisição.

No legado, escrita e consolidação ocorriam no mesmo request/transação, causando:

- Latência elevada em dias com muitos lançamentos
- Falha na consolidação derrubando o registro
- Acoplamento entre módulo de lançamento e módulo de relatório

Requisitos relacionados: RF01, RF03, RF04, RF05, RF07; RNF de latência (< 200 ms escrita, < 50 ms leitura).

## Decisão

Separar **Command** e **Query** com comunicação assíncrona:

1. **Write model** (`Lancamentos.Api`): persiste `Lancamento` no banco transacional e publica `LancamentoCriadoEvent` na fila
2. **Processador** (`Consolidador.Worker`): consome evento, recalcula saldo do dia, persiste read model
3. **Read model** (`Relatorios.Api`): consulta apenas dados consolidados (Redis em produção; tabela `saldos_diarios` no POC)

Padrão: **CQRS pragmático** (não Event Sourcing completo — o evento é notificação, não fonte da verdade).

## Alternativas consideradas

| Alternativa | Por que foi descartada |
|-------------|------------------------|
| **Consolidação síncrona** no mesmo request do POST | Viola RNF de latência; acoplamento write/read; falha na consolidação = falha no registro |
| **Event Sourcing** — eventos como única fonte da verdade | Complexidade excessiva para o escopo; replay e snapshots adicionam custo de equipe |
| **Dual write** — atualizar lançamento e saldo na mesma transação | Escalabilidade de leitura limitada; lock contention no saldo diário |
| **Batch noturno** — consolidar uma vez por dia | Saldo desatualizado intradia; não atende expectativa do comerciante |
| **Kafka como backbone** | Overkill para volume atual (~1M req/mês); custo operacional maior que SQS |

## Consequências

### Positivas

- Registro de lançamento desacoplado da consolidação → maior resiliência
- Read path otimizado independentemente (cache, índices, réplicas)
- Escalabilidade independente de write e read workers
- Alinhamento com feedback positivo do avaliador ("CQRS resolveu resiliência entre consolidado e lançamento")

### Negativas

- **Consistência eventual** — entre POST e consulta de saldo há janela onde saldo pode estar desatualizado
- **Complexidade distribuída** — fila, consumer, monitoramento de lag
- Necessidade de estratégia para mensagens duplicadas e falhas (ver ADR-0005, ADR-0008)

### Métricas de aceite

- `POST /lancamentos` persiste e retorna antes da consolidação completar
- Lag p95 fila → consolidação < 5 s em condições normais
- Saldo consolidado converge após processamento do evento

## Estado no POC

| Item | Status | Evidência |
|------|--------|-----------|
| Handler de escrita + publicação de evento | ✅ | `RegistrarLancamentoHandler.cs` |
| Consumer assíncrono | ✅ | `LancamentoCriadoConsumer.cs` |
| API de leitura separada | ✅ | `Relatorios.Api` |
| Contrato de evento | ✅ | `LancamentoCriadoEvent` |
| Garantia de ordenação por dia | ❌ | Eventos do mesmo dia podem ser processados fora de ordem |
| Idempotência na consolidação | ❌ | Reprocessamento recalcula (seguro matematicamente, mas ineficiente) |

## Diagrama de referência

Ver [C4 Containers](../architecture/c4-containers.md) e [diagramas de sequência no README](../../README.md#13-diagramas-de-sequência-high-level).
