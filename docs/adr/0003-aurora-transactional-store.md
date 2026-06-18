# ADR-0003: Aurora Serverless v2 como store transacional

## Status

**Accepted** — 2025-12-14

## Contexto

Lançamentos financeiros exigem:

- **Consistência forte** na escrita (ACID)
- Integridade referencial e constraints (valor > 0, tipos enumerados)
- SQL para relatórios ad-hoc e migrações via EF Core
- Escalabilidade elástica com custo proporcional ao uso (FinOps)

Volume estimado: ~1M requisições/mês, picos de ~50 req/s, dados por comerciante (multi-tenant futuro).

## Decisão

Utilizar **Amazon Aurora Serverless v2 (compatível PostgreSQL)** como banco transacional do write model:

- Tabela `lancamentos` como fonte da verdade das movimentações
- Tabela `saldos_diarios` como projeção materializada (read model persistido; cache Redis em produção)
- Migrations via EF Core
- ACU mínimo configurado para balancear custo (~75% do orçamento) vs. latência de cold start

## Alternativas consideradas

| Alternativa | Por que foi descartada |
|-------------|------------------------|
| **DynamoDB** | Modelagem de agregados financeiros com queries por dia/merchant é mais natural em SQL; transações multi-item limitadas; curva de modelagem para equipe .NET/SQL |
| **RDS PostgreSQL provisionado** | Paga capacidade fixa 24/7; pior FinOps em cenários de baixo tráfego noturno |
| **MongoDB / document store** | Sem ACID multi-documento robusto no perfil de uso; relatórios agregados mais complexos |
| **Manter banco legado indefinidamente** | Contraria objetivo de modernização e Strangler Fig |
| **Aurora Serverless v1** | v2 tem scaling mais previsível e melhor suporte a PostgreSQL moderno |

## Consequências

### Positivas

- SQL completo, EF Core, migrations testadas no POC
- Multi-AZ, backups automáticos, ponto de recuperação
- Compatibilidade com CDC a partir do legado (se também PostgreSQL) ou via DMS
- Serverless scaling reduz provisionamento manual

### Negativas

- **ACU mínimo gera custo base** mesmo sem tráfego (~USD 75/mês no cenário documentado)
- Latência de conexão em Lambda exige **RDS Proxy** ou pool cuidadoso (custo adicional)
- Não é o store ideal para leitura ultrarrápida de saldos → Redis complementar (ADR-0004)

### Riscos

- Subdimensionar ACU mínimo → throttling em picos
- Superdimensionar → desperdício FinOps

## Estado no POC

| Item | Status |
|------|--------|
| PostgreSQL 16 local (docker-compose) | ✅ |
| Schema `lancamentos` + `saldos_diarios` | ✅ Migration `20251214183959_Init` |
| Aurora Serverless v2 em AWS | ❌ Não provisionado |
| RDS Proxy | ❌ |
| Coluna `merchant_id` (multi-tenant) | ❌ Ausente no schema |
| Índices por `(merchant_id, data)` | ❌ |

**Nota crítica:** o POC usa PostgreSQL standalone, não Aurora. A decisão de ADR é para **produção**; o schema deve evoluir antes do go-live (tenant, índices, auditoria).
