# Architecture Decision Records (ADRs)

Registro estruturado das decisões arquiteturais do Sistema de Fluxo de Caixa, seguindo o formato inspirado em [Michael Nygard](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions).

## Formato

Cada ADR contém:

- **Status** — Proposed | Accepted | Deprecated | Superseded
- **Contexto** — problema e restrições
- **Decisão** — o que foi escolhido
- **Alternativas consideradas** — opções descartadas e por quê
- **Consequências** — ganhos, custos e riscos
- **Estado no POC** — honestidade sobre o que o código em `src/` implementa hoje

## Índice

| ADR | Título | Status |
|-----|--------|--------|
| [0001](0001-strangler-fig-migration.md) | Migração gradual via Strangler Fig Pattern | Accepted |
| [0002](0002-cqrs-event-driven-consolidation.md) | CQRS com consolidação assíncrona orientada a eventos | Accepted |
| [0003](0003-aurora-transactional-store.md) | Aurora Serverless v2 como store transacional | Accepted |
| [0004](0004-redis-read-model.md) | Redis como read model para saldos consolidados | Accepted |
| [0005](0005-sqs-async-messaging.md) | Amazon SQS para mensageria assíncrona | Accepted |
| [0006](0006-cognito-identity-provider.md) | Amazon Cognito como Identity Provider centralizado | Accepted |
| [0007](0007-serverless-api-gateway-lambda.md) | API Gateway + Lambda para backend serverless | Accepted |
| [0008](0008-consolidation-full-recalc-strategy.md) | Estratégia de consolidação por recálculo completo do dia | Accepted |
| [0009](0009-rbac-jwt-claims.md) | RBAC baseado em JWT claims e grupos Cognito | Accepted |
| [0010](0010-blazor-wasm-frontend.md) | Blazor WebAssembly para front-end do comerciante | Accepted |

## Como ler os ADRs

O enunciado exige **justificativa na escolha de ferramentas e tipo de arquitetura**. A seção 10 do README é um resumo; cada ADR abaixo registra **contexto, alternativas rejeitadas e consequências** — formato esperado para documentação de decisões arquiteturais.
