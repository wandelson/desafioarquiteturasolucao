# RBAC e Autorização

Especificação de **Role-Based Access Control** para consumo seguro das APIs (requisito diferencial do enunciado: *critérios de segurança para integração de serviços*).

Relacionado: [ADR-0006](../adr/0006-cognito-identity-provider.md), [ADR-0009](../adr/0009-rbac-jwt-claims.md), RF09, RF10.

---

## 1. Modelo de ameaça (resumo)

| Ameaça | Mitigação |
|--------|-----------|
| Usuário acessa dados de outro merchant | `merchant_id` do JWT; filtro obrigatório em SQL |
| Operador executa ação de admin | Verificação de `cognito:groups` por endpoint |
| Token roubado | TTL curto, HTTPS, refresh rotativo, opcional MFA |
| API chamada sem token | API Gateway JWT Authorizer → 401 |
| Escalação via manipulação de `merchantId` no body | Ignorar body; usar apenas claim do token |

---

## 2. Papéis (roles)

| Papel | Grupo Cognito | Público | Descrição |
|-------|---------------|---------|-----------|
| **Operador** | `merchant_operator` | Funcionário do caixa | Registra lançamentos; consulta saldo do dia |
| **Administrador do merchant** | `merchant_admin` | Dono/gerente da loja | Tudo do operador + gestão de usuários do tenant (futuro) |
| **Auditor** | `merchant_auditor` | Contabilidade interna | Somente leitura de lançamentos e relatórios |
| **Suporte plataforma** | `platform_support` | Time Carrefour/ops | Leitura cross-tenant com log de break-glass |

### Hierarquia (herança de permissões)

```
merchant_admin ⊃ merchant_operator
merchant_auditor ∩ {write} = ∅
platform_support → somente leitura + auditoria reforçada
```

---

## 3. Permissões (scopes OAuth2)

Escopos no resource server `fluxo-caixa`:

| Scope | Descrição |
|-------|-----------|
| `fluxo-caixa/lancamentos:read` | Listar/consultar lançamentos |
| `fluxo-caixa/lancamentos:write` | Criar lançamento |
| `fluxo-caixa/relatorios:read` | Consultar saldos e relatórios |
| `fluxo-caixa/admin:users` | Gerenciar usuários do merchant (futuro) |

---

## 4. Matriz papel × permissão × endpoint

| Endpoint | Método | operator | admin | auditor | support |
|----------|--------|:--------:|:-----:|:-------:|:-------:|
| `/lancamentos` | POST | ✅ | ✅ | ❌ | ❌ |
| `/lancamentos` | GET | ✅ | ✅ | ✅ | ✅* |
| `/relatorios/{dia}` | GET | ✅ | ✅ | ✅ | ✅* |
| `/relatorios` (período) | GET | ✅ | ✅ | ✅ | ✅* |
| `/admin/users` | * | ❌ | ✅ | ❌ | ❌ |

\* `platform_support`: permitido com header `X-Break-Glass-Reason` obrigatório + log imutável em CloudTrail/SIEM.

### Códigos HTTP

| Situação | Código |
|----------|--------|
| Sem token / token inválido | `401 Unauthorized` |
| Token válido, papel insuficiente | `403 Forbidden` |
| Recurso de outro tenant | `404 Not Found` (não `403`, para não revelar existência) |
| Validação de payload | `400 Bad Request` |

---

## 5. Claims JWT (access token)

Exemplo decodificado (payload):

```json
{
  "sub": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "iss": "https://cognito-idp.us-east-1.amazonaws.com/us-east-1_XXXXX",
  "aud": "fluxo-caixa-api",
  "exp": 1735689600,
  "iat": 1735688700,
  "token_use": "access",
  "scope": "fluxo-caixa/lancamentos:write fluxo-caixa/relatorios:read",
  "cognito:groups": ["merchant_operator"],
  "custom:merchant_id": "merchant-7f3a2b1c",
  "email": "caixa@lojaexemplo.com.br"
}
```

### Regras de validação

| Claim | Validação |
|-------|-----------|
| `exp` | Rejeitar se expirado (clock skew ≤ 30 s) |
| `iss` | Whitelist do User Pool |
| `aud` ou `client_id` | Deve ser o client da API |
| `custom:merchant_id` | Obrigatório para todos os papéis exceto `platform_support` |
| `cognito:groups` | Pelo menos um grupo válido |
| `scope` | Deve conter scope mínimo da rota (opcional na borda; obrigatório na app) |

---

## 6. Enforcement em camadas

![RBAC — enforcement em camadas](../images/svg/seq-rbac-enforcement.svg)

**Fonte PlantUML:** [`seq-rbac-enforcement.puml`](../images/plantuml/seq-rbac-enforcement.puml) · [Sequências](../architecture/sequences.md#rf10--enforcement-rbac)

### 6.1 API Gateway (borda)

- JWT Authorizer apontando para Cognito JWKS
- Mapeamento de rotas públicas: apenas `/health` (sem auth)

### 6.2 Middleware ASP.NET (aplicação)

Pseudocódigo do padrão esperado em produção:

```csharp
// Extrair claims
var merchantId = User.FindFirst("custom:merchant_id")?.Value;
var groups = User.FindAll("cognito:groups").Select(c => c.Value);

// Policy example
[Authorize(Policy = "LancamentosWrite")]
public async Task<IActionResult> Post(...)

// Policy handler
// merchant_operator OR merchant_admin + scope lancamentos:write
```

### 6.3 Camada de dados (última linha de defesa)

```sql
-- Toda query MUST incluir tenant
SELECT * FROM lancamentos
WHERE merchant_id = @merchantId AND data = @dia;
```

**Nunca** confiar em `merchantId` vindo do request body ou query string.

---

## 7. Provisionamento de usuários (Cognito)

| Fluxo | Responsável | Ação |
|-------|-------------|------|
| Onboarding novo merchant | Plataforma | Cria merchant; primeiro usuário como `merchant_admin` |
| Novo funcionário | `merchant_admin` | Convite via Cognito AdminCreateUser ou self-signup com código |
| Desligamento | `merchant_admin` | `AdminDisableUser` |
| Auditor externo | Plataforma | Usuário `merchant_auditor` com `custom:merchant_id` fixo |

### Mapeamento grupo ↔ scope (Cognito Pre Token Generation Lambda)

| Grupo | Scopes injetados no token |
|-------|---------------------------|
| `merchant_operator` | `lancamentos:read`, `lancamentos:write`, `relatorios:read` |
| `merchant_admin` | Todos do operator + `admin:users` |
| `merchant_auditor` | `lancamentos:read`, `relatorios:read` |

---

## 8. Evolução do schema para multi-tenant

Colunas a adicionar antes do go-live:

```sql
ALTER TABLE lancamentos ADD COLUMN merchant_id VARCHAR(36) NOT NULL;
ALTER TABLE saldos_diarios DROP CONSTRAINT PK_saldos_diarios;
ALTER TABLE saldos_diarios ADD PRIMARY KEY (merchant_id, dia);
CREATE INDEX ix_lancamentos_merchant_data ON lancamentos(merchant_id, data);
```

Evento `LancamentoCriadoEvent` deve incluir `MerchantId`.

---

## 9. Cenários de teste de autorização

| # | Cenário | Token | Request | Esperado |
|---|---------|-------|---------|----------|
| T1 | Operador cria lançamento | operator, merchant-A | POST /lancamentos | 201 |
| T2 | Auditor tenta criar | auditor, merchant-A | POST /lancamentos | 403 |
| T3 | Operador A lê saldo de A | operator, merchant-A | GET /relatorios/2025-01-10 | 200 |
| T4 | Operador A lê saldo de B (mesmo endpoint, dados filtrados) | operator, merchant-A | GET com dados só de A | 200 ou 404 se vazio |
| T5 | Sem token | — | GET /relatorios/... | 401 |
| T6 | Token expirado | exp no passado | GET | 401 |
| T7 | Support sem break-glass | platform_support | GET cross-tenant | 403 |
| T8 | Support com break-glass | platform_support + header | GET merchant-B | 200 + audit log |

---

## 10. Status no POC

| Capacidade | Status |
|------------|--------|
| Cognito User Pool + grupos | ❌ |
| JWT validation | ❌ |
| Policies ASP.NET | ❌ |
| `merchant_id` no banco | ❌ |
| CORS `AllowAnyOrigin` sem credenciais | ⚠️ Inseguro para produção |
| Especificação RBAC (este documento) | ✅ |

O POC permite testar **domínio e CQRS** sem auth. A especificação acima define o contrato para implementação pré-produção — gap reconhecido explicitamente nos ADRs e requisitos.
