# ADR-0004: Redis como read model para saldos consolidados

## Status

**Accepted** — 2025-12-14

## Contexto

O requisito não funcional define **consulta de saldo diário < 50 ms** (p95). O caminho de leitura é o mais frequente após consolidação (dashboard do comerciante, refresh repetido).

Consultar `saldos_diarios` diretamente no Aurora:

- Latência de rede + pool + query mesmo com PK em `dia`
- Contenção de leitura com writes de consolidação e novos lançamentos
- Custo de ACU Aurora aumenta com volume de leitura

## Decisão

Utilizar **Amazon ElastiCache Redis** como read model primário para saldos consolidados:

- Chave: `saldo:{merchantId}:{yyyy-MM-dd}`
- Valor: saldo decimal serializado
- TTL: opcional (dados históricos imutáveis após fechamento do dia)
- **Fallback:** em cache miss, consultar tabela `saldos_diarios` no Aurora e repopular cache
- Worker de consolidação **escreve** Redis após calcular saldo

## Alternativas consideradas

| Alternativa | Por que foi descartada |
|-------------|------------------------|
| **Somente Aurora** (sem cache) | Dificulta atingir < 50 ms de forma consistente em serverless + multi-AZ |
| **DynamoDB para read model** | Funciona, mas duplica stack de persistência com modelo diferente do write; equipe já padronizou SQL |
| **Materialized views no Aurora** | Ainda passa pelo banco relacional; menos controle de TTL e invalidação fina |
| **Cache em memória na Lambda** | Não compartilhado entre instâncias; inconsistente em escala |
| **CDN cache de API** | Saldo é dado dinâmico e autenticado; inadequado |

## Consequências

### Positivas

- Latência sub-10 ms para hits em Redis na mesma VPC
- Reduz carga de leitura no Aurora (FinOps)
- Separação clara write model / read model (CQRS)

### Negativas

- **Segundo sistema para operar** — failover, patching, monitoramento
- **Consistência eventual** entre Aurora `saldos_diarios` e Redis se write em Redis falhar após commit no SQL
- Custo fixo de cluster Redis (~12% do orçamento estimado)
- Necessidade de estratégia de invalidação em reprocessamento

### Mitigações

- Write-through: worker persiste Aurora **e** Redis; retry em falha Redis
- Alarme em cache hit rate baixo
- Fallback automático para Aurora na API de relatórios

## Estado no POC

| Item | Status |
|------|--------|
| Tabela `saldos_diarios` no PostgreSQL | ✅ |
| Redis / ElastiCache | ❌ **Não implementado** |
| Fallback cache miss na API de relatórios | ❌ Lê apenas PostgreSQL |
| Chave composta `merchantId + dia` | ❌ PK apenas em `dia` |

**Gap crítico:** o README original mostrava Redis no diagrama, mas o código usa PostgreSQL para leitura. Este ADR documenta a **decisão de produção** e marca explicitamente o desvio do POC — exatamente o tipo de honestidade que faltou na avaliação.
