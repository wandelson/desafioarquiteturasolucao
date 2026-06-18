# ADR-0008: Estratégia de consolidação por recálculo completo do dia

## Status

**Accepted** — 2025-12-14

## Contexto

Ao receber `LancamentoCriadoEvent`, o worker deve atualizar o saldo consolidado do dia. Duas abordagens principais:

1. **Incremental** — `saldo += valorEfetivo` do lançamento
2. **Recálculo completo** — `SUM(valorEfetivo)` de todos os lançamentos do dia

O handler atual (`ConsolidarDiaHandler`) implementa recálculo completo.

## Decisão

Adotar **recálculo completo do dia** a cada evento processado:

```csharp
var lancamentos = await _lancRepo.ObterPorDiaAsync(dia);
var saldo = lancamentos.Sum(l => l.ValorEfetivo());
await _saldoRepo.SalvarAsync(new SaldoDiario(dia, saldo));
```

**Motivo principal:** idempotência natural — reprocessar a mesma mensagem ou processar fora de ordem produz o mesmo resultado, desde que todos os lançamentos estejam persistidos.

## Alternativas consideradas

| Alternativa | Por que foi descartada (com ressalvas) |
|-------------|----------------------------------------|
| **Atualização incremental** (`saldo += delta`) | Rápida O(1), mas **não idempotente** — mensagem duplicada duplica efeito; exige dedup por `LancamentoId` |
| **Event Sourcing + snapshots** | Correto teoricamente; complexidade alta |
| **Lock pessimista por dia** | Serializa consolidação; gargalo em dias movimentados |
| **Batch único ao fim do dia** | Saldo intradia incorreto |

## Consequências

### Positivas

- **Idempotência matemática** em reprocessamento (desde que lançamento já commitado no banco)
- Correção automática se evento anterior foi perdido mas lançamento existe
- Implementação simples, fácil de testar
- Alinhada ao volume atual (dias com centenas de lançamentos, não milhões)

### Negativas

- **O(n)** por evento — n lançamentos do dia relidos a cada mensagem
- Em dia com 10.000 lançamentos e 10.000 eventos → carga O(n²) agregada (aceitável no MVP, não em escala)
- Pressão de leitura no Aurora a cada consolidação
- Latência de consolidação cresce com o dia

### Evolução prevista (quando escalar)

| Gatilho | Ação |
|---------|------|
| > 500 lançamentos/dia/merchant | Migrar para incremental + tabela `processed_events(lancamento_id)` |
| Lag de fila > SLA | Debounce: consolidar no máximo 1× a cada X segundos por dia |
| Leitura Aurora cara | Incremental com Redis `HINCRBYFLOAT` |

## Estado no POC

| Item | Status |
|------|--------|
| Recálculo completo | ✅ `ConsolidarDiaHandler.ProcessarDia` |
| Filtro por `merchantId` | ❌ |
| Debounce / coalescing | ❌ |
| Tabela de idempotência | ❌ |

## Regra de negócio associada

Saldo do dia = Σ (créditos) − Σ (débitos), onde crédito soma `+valor` e débito soma `−valor` (`Lancamento.ValorEfetivo()`).
