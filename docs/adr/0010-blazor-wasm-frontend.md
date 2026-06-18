# ADR-0010: Blazor WebAssembly para front-end do comerciante

## Status

**Accepted** — 2025-12-14

## Contexto

Substituir front legado por interface moderna, integrada ao mesmo IdP, executável como static site (custo baixo em CDN). Equipe com skills .NET/C#.

Funcionalidades do front:

- Registrar lançamento (crédito/débito)
- Consultar saldo/relatório do dia
- Autenticação OIDC com PKCE (SPA)

## Decisão

Adotar **Blazor WebAssembly** hospedado em **S3 + CloudFront**:

- Compartilhamento de tipos/contratos com backend .NET (opcional)
- OIDC via `Microsoft.AspNetCore.Components.WebAssembly.Authentication`
- Chamadas às APIs via `HttpClient` com Bearer token
- Roteamento client-side; deep link via CloudFront error pages → `index.html`

## Alternativas consideradas

| Alternativa | Por que foi descartada |
|-------------|------------------------|
| **React / Angular** | Stack heterogênea; equipe .NET; duplicação de modelos |
| **Blazor Server** | Conexão SignalR persistente; custo e escala piores que WASM static |
| **Next.js SSR** | Excelente SEO, mas desnecessário para app autenticado B2B de backoffice |
| **Manter front legado indefinidamente** | Contraria modernização UX e Strangler Fig |
| **MAUI / mobile** | Fora do escopo web do desafio |

## Consequências

### Positivas

- Deploy estático barato (S3/CloudFront)
- UX SPA sem reload completo
- Ecossistema .NET unificado

### Negativas

- **Payload inicial WASM** maior que JS framework típico (mitigar com trimming, lazy load)
- Segurança: lógica sensível **não** pode ficar só no client — RBAC no backend obrigatório
- WASM debugging mais difícil que server-side

## Estado no POC

| Item | Status |
|------|--------|
| Blazor WASM app | ✅ `Frontend.Web` |
| Tela fluxo de caixa | ✅ `/fluxo-caixa` |
| OIDC login | ❌ |
| Deploy S3/CloudFront | ❌ Dev local Kestrel |
| Chamada autenticada às APIs | ❌ |
