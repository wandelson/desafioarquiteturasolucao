# ADR-0009: RBAC baseado em JWT claims e grupos Cognito

## Status

**Accepted** — 2025-12-14

## Contexto

Sistema multi-tenant B2B: cada comerciante (`merchantId`) possui usuários com papéis distintos. Requisitos:

- Operador registra lançamentos; não apaga histórico
- Admin gerencia usuários do merchant (futuro) e vê tudo do tenant
- Auditor só lê relatórios
- **Isolamento de dados**: usuário do merchant A nunca acessa dados do merchant B

O feedback apontou: *"JWT citado, mas faltou detalhar gerenciamento de papéis e permissões (RBAC)"*.

## Decisão

Implementar **RBAC em duas camadas**:

### Camada 1 — Borda (API Gateway)

- JWT Authorizer Cognito
- Rejeitar tokens expirados, `aud`/`iss` inválidos
- Opcional: validar scope OAuth2 mínimo por rota

### Camada 2 — Aplicação (Lambda/API)

- Extrair `custom:merchant_id` e `cognito:groups` do token
- **Policy-based authorization** por endpoint (ver matriz em [rbac.md](../security/rbac.md))
- **Filtro de tenant obrigatório** em todo query/command: `WHERE merchant_id = @merchantId`
- Negar com `403 Forbidden` se papel insuficiente; `404` se recurso de outro tenant (evitar enumeração)

### Papéis (Cognito Groups)

| Grupo Cognito | Papel | Descrição |
|---------------|-------|-----------|
| `merchant_operator` | Operador | CRUD lançamentos do próprio merchant |
| `merchant_admin` | Administrador | Tudo do operador + gestão de usuários (futuro) |
| `merchant_auditor` | Auditor | Somente leitura de relatórios e lançamentos |
| `platform_support` | Suporte plataforma | Leitura cross-tenant com auditoria (break-glass) |

## Alternativas consideradas

| Alternativa | Por que foi descartada |
|-------------|------------------------|
| **Autorização apenas no front** | Inseguro — APIs expostas diretamente |
| **RBAC em banco (tabelas de permissão)** | Necessário para permissões dinâmicas complexas; overkill no MVP com 3 papéis fixos |
| **ABAC puro (atributos arbitrários)** | Complexidade de política; Cognito groups + claims suficientes |
| **Um token por merchant compartilhado** | Sem rastreabilidade por usuário; viola auditoria |
| **Isolamento só por query string `merchantId`** | Spoofing trivial sem validação do token |

## Consequências

### Positivas

- Modelo padrão de mercado (OIDC + groups)
- Enforcement testável por integração (token com role X → 403 em rota Y)
- Evolução para ABAC sem mudar IdP

### Negativas

- Duplicação de regras se front também esconder botões (defesa em profundidade necessária)
- `platform_support` é vetor de risco — exige logging e aprovação
- Custom attribute `merchant_id` imutável por usuário — mudança de loja = novo usuário ou processo admin

## Estado no POC

| Item | Status |
|------|--------|
| Cognito groups | ❌ |
| Claims no token | ❌ |
| Middleware de autorização | ❌ |
| `merchant_id` no schema | ❌ |
| Documento RBAC completo | ✅ [security/rbac.md](../security/rbac.md) |

## Referência

Especificação detalhada: [RBAC e Autorização](../security/rbac.md)
