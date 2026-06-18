# ADR-0001: Migração gradual via Strangler Fig Pattern

## Status

**Accepted** — 2025-12-14

## Contexto

O sistema legado de fluxo de caixa é um monólito (front + API + banco acoplados) em produção com comerciantes ativos. O negócio exige:

- Modernização tecnológica (serverless, OIDC, CQRS)
- **Zero downtime** e **sem big bang**
- Coexistência temporária entre legado e novo sistema
- Sincronização de dados históricos para o novo banco

Restrições: equipe pequena, orçamento limitado (~USD 100/mês em idle moderado), necessidade de rollback por funcionalidade.

## Decisão

Adotar o **Strangler Fig Pattern**:

1. Manter o legado operacional
2. Construir o novo sistema ao lado (Blazor + APIs serverless)
3. Migrar **funcionalidade por funcionalidade** (primeiro relatórios, depois lançamentos)
4. Usar **CDC (Change Data Capture)** do banco legado para o Aurora
5. Unificar autenticação no mesmo IdP (Cognito) para SSO
6. Desligar o legado apenas quando 100% das capacidades estiverem migradas e validadas

## Alternativas consideradas

| Alternativa | Por que foi descartada |
|-------------|------------------------|
| **Big bang** — desligar legado e ligar novo em uma janela | Risco operacional inaceitável; rollback complexo; perda de receita em caso de falha |
| **Dual write** — escrever em legado e novo simultaneamente | Consistência difícil; lógica de reconciliação cara; bugs silenciosos em divergência |
| **API façade única** — BFF que roteia tudo sem migrar domínio | Adia modernização do core; mantém débito técnico no banco legado |
| **Reescrever tudo em paralelo sem CDC** | Migração de dados manual/ batch único; downtime ou inconsistência histórica |
| **Micro-frontends sem migração de backend** | Não resolve acoplamento e escalabilidade do backend legado |

## Consequências

### Positivas

- Rollback por funcionalidade (redirect reverso)
- Valor entregue incrementalmente (relatórios rápidos antes de migrar lançamentos)
- SSO evita múltiplos logins durante transição
- CDC preserva histórico sem reprocessamento manual

### Negativas

- **Complexidade operacional temporária** — dois sistemas, dois bancos, pipelines CDC
- **Período de inconsistência eventual** entre legado e novo durante sincronização
- **Custo duplicado** de infraestrutura até desligamento do legado
- Curva de aprendizado da equipe em dois codebases

### Riscos

- CDC com lag alto → relatórios no novo sistema desatualizados em relação ao legado
- Funcionalidades "meio migradas" geram confusão de UX se redirects mal definidos

## Estado no POC

| Item | Status |
|------|--------|
| Novo front Blazor | ✅ Implementado (`Frontend.Web`) |
| Novas APIs separadas | ✅ `Lancamentos.Api`, `Relatorios.Api`, `Consolidador.Worker` |
| Integração com legado / CDC | ❌ Não implementado |
| Redirect do front legado | ❌ Não implementado |
| Cognito SSO unificado | ❌ Não implementado |

O POC valida o **núcleo do novo sistema**, não a orquestração de migração.
