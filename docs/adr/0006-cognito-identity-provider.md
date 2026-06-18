# ADR-0006: Amazon Cognito como Identity Provider centralizado

## Status

**Accepted** — 2025-12-14

## Contexto

O ecossistema atual tem autenticação própria no legado, não padronizada. Durante a migração Strangler Fig:

- Front legado e novo front Blazor devem compartilhar **SSO**
- APIs serverless precisam validar identidade na borda (API Gateway)
- Autorização multi-tenant via `merchantId` nos claims
- Conformidade: TLS, tokens de curta duração, rotação de refresh tokens

Requisitos: RF09 (autenticação), RF10 (autorização por comerciante), RBAC detalhado em [security/rbac.md](../security/rbac.md).

## Decisão

Adotar **Amazon Cognito User Pools** como IdP único:

- **OAuth2 Authorization Code + PKCE** para SPA Blazor WASM
- **OpenID Connect** — tokens JWT (access + id)
- **Grupos Cognito** mapeados para papéis RBAC (`merchant_operator`, `merchant_admin`, `merchant_auditor`)
- **Custom attributes**: `custom:merchant_id`
- **API Gateway JWT Authorizer** valida assinatura, `exp`, `iss`, `aud`
- Lambdas/APIs aplicam autorização fina (escopo + tenant) após validação na borda

## Alternativas consideradas

| Alternativa | Por que foi descartada |
|-------------|------------------------|
| **Auth0** | Excelente DX, mas custo adicional e dependência externa; Cognito integra nativamente com API Gateway IAM |
| **Keycloak self-hosted** | Custo operacional de cluster; contraria serverless gerenciado |
| **JWT emitido pela API legada** | Não unifica SSO; perpetua autenticação fraca do legado |
| **API Keys no API Gateway** | Sem identidade de usuário; inadequado para multi-tenant B2B |
| **AWS IAM SigV4 para usuários finais** | Modelo errado para comerciantes; IAM é para serviços/máquinas |

## Consequências

### Positivas

- SSO legado + novo durante migração
- Integração nativa API Gateway ↔ Cognito
- Grupos e custom attributes para RBAC sem banco de usuários próprio
- Conformidade com OAuth2/OIDC industry standard

### Negativas

- **Vendor lock-in** AWS para identidade
- Customização de UI de login limitada (Hosted UI) ou custo de implementar UI própria
- Debugging de claims mal configurados é frequente em projetos reais
- Cognito não substitui autorização de recurso — RBAC na aplicação ainda necessário

### Claims JWT esperados (access token)

```json
{
  "sub": "uuid-do-usuario",
  "cognito:groups": ["merchant_operator"],
  "custom:merchant_id": "merchant-123",
  "scope": "fluxo-caixa/lancamentos:write fluxo-caixa/relatorios:read"
}
```

## Estado no POC

| Item | Status |
|------|--------|
| Cognito User Pool | ❌ |
| JWT validation nas APIs | ❌ CORS `AllowAnyOrigin` sem auth |
| Grupos / RBAC | ❌ Especificado em [rbac.md](../security/rbac.md) |
| Blazor OIDC login | ❌ |

**Gap crítico do feedback:** JWT foi citado no README, mas RBAC e enforcement não foram detalhados. Este ADR + documento RBAC endereçam essa lacuna.
