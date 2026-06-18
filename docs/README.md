# Documentação de Arquitetura — Sistema de Fluxo de Caixa

Artefatos de **governança e modelagem** exigidos pelo desafio de Arquiteto de Soluções. O [README principal](../README.md) concentra operação (como rodar, APIs, diagramas de sequência, FinOps); este diretório concentra **visão sistêmica, decisões formalizadas e requisitos refinados**.

> **Convenção:** [C4 PlantUML](architecture/c4-context.md) para estrutura · [diagramas de sequência](architecture/sequences.md) para features.

> O desafio deixa claro: não é necessário implementar tudo em código, mas **sim** registrar decisões e representações arquiteturais no repositório.

## Guia rápido para avaliação

Siga a ordem em [Como avaliar este projeto](../README.md#como-avaliar-este-projeto) no README principal.

---

## Rastreabilidade: enunciado do desafio → documento

### Requisitos obrigatórios

| Requisito do desafio | Documento |
|----------------------|-----------|
| Mapeamento de domínios funcionais e capacidades de negócio | [README §7](../README.md) + [C4 Nível 1](architecture/c4-context.md) |
| Refinamento de requisitos funcionais | [Requisitos Funcionais](requirements/requisitos-funcionais.md) |
| Refinamento de requisitos não funcionais | [Requisitos Não Funcionais](requirements/requisitos-nao-funcionais.md) + [README §9](../README.md) |
| Desenho da solução completo (Arquitetura Alvo) | [C4 Nível 2](architecture/c4-containers.md) + [README §5](../README.md) |
| Justificativa de ferramentas, tecnologias e tipo de arquitetura | [ADRs](adr/README.md) + [README §10](../README.md) |
| Testes | [README §16](../README.md) · código em `test/` |
| README (como funciona e como rodar) | [README §15](../README.md) |
| Toda documentação no repositório | Este diretório `docs/` + README |

### Requisitos diferenciais

| Requisito do desafio | Documento |
|----------------------|-----------|
| Arquitetura de Transição (migração de legado) | [ADR-0001](adr/0001-strangler-fig-migration.md) + [README §3 e §6](../README.md) |
| Estimativa de custos (infra e licenças) | [README §14](../README.md) |
| Monitoramento e Observabilidade | [README §11](../README.md) · [RNF07](requirements/requisitos-nao-funcionais.md) |
| Critérios de segurança para consumo de serviços | [RBAC](security/rbac.md) · [ADR-0006](adr/0006-cognito-identity-provider.md) · [ADR-0009](adr/0009-rbac-jwt-claims.md) |

### RNF crítico do enunciado

| Regra de negócio | Como a arquitetura atende | Documento |
|------------------|---------------------------|-----------|
| Lançamentos **não** ficam indisponíveis se o consolidado cair | CQRS + fila: escrita desacoplada da consolidação | [ADR-0002](adr/0002-cqrs-event-driven-consolidation.md) · [README §13](../README.md) |
| Consolidado suporta **50 req/s** em pico | Fila absorve pico; worker/Lambda escala independente | [RNF02](requirements/requisitos-nao-funcionais.md) · [ADR-0005](adr/0005-sqs-async-messaging.md) |
| Máximo **5% de perda** de requisições no consolidado | Retry, DLQ, idempotência, alarmes | [ADR-0005](adr/0005-sqs-async-messaging.md) · [ADR-0008](adr/0008-consolidation-full-recalc-strategy.md) · [README §18](../README.md) |

### Papel do Arquiteto de Soluções (enunciado)

| Competência esperada | Artefato principal |
|----------------------|-------------------|
| Transformar requisitos em capacidades | [C4 Nível 1](architecture/c4-context.md) · [RFs](requirements/requisitos-funcionais.md) |
| Arquitetura de contexto e segregação de processos | [C4 Nível 2](architecture/c4-containers.md) |
| Padrões arquiteturais e trade-offs | [ADRs](adr/README.md) |
| Integração entre sistemas e serviços | [C4 Nível 1](architecture/c4-context.md) · [README §6](../README.md) |
| Documentação de decisões e diagramas | C4 + sequências por feature |

---

## Índice de artefatos

| Artefato | Descrição |
|----------|-----------|
| [C4 — Nível 1 (Contexto)](architecture/c4-context.md) | C4 PlantUML — atores, sistemas externos |
| [C4 — Nível 2 (Containers)](architecture/c4-containers.md) | C4 PlantUML — containers e protocolos |
| [Sequências por feature](architecture/sequences.md) | PlantUML — diagramas de sequência (RF01–RF10) |
| [Diagramas (PlantUML + SVG)](images/README.md) | Fontes `.puml` e regeneração de imagens |
| [ADRs](adr/README.md) | Decisões com contexto, alternativas e consequências |
| [Requisitos Funcionais](requirements/requisitos-funcionais.md) | RFs com objetivo de negócio, regras e critérios de aceite |
| [Requisitos Não Funcionais](requirements/requisitos-nao-funcionais.md) | RNFs mensuráveis e ligação com decisões |
| [RBAC e Autorização](security/rbac.md) | Papéis, permissões, claims JWT e enforcement |

## Evoluções futuras

Itens planejados mas fora do escopo do POC estão no [README §18](../README.md), com referência cruzada nos ADRs (ex.: DLQ → ADR-0005; cache Redis → ADR-0004; auth → ADR-0006/0009).
