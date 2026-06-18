# Sistema de Fluxo de Caixa — Desafio Arquiteto de Soluções

## Como avaliar este projeto

O enunciado prioriza **decisões e representações arquiteturais**; o código é um POC que valida o núcleo do domínio (CQRS + fila). Sugestão de leitura para o avaliador (~10 min):

| Ordem | O que avaliar | Onde |
|:-----:|---------------|------|
| 1 | Visão de contexto e capacidades de negócio | [C4 Nível 1](docs/architecture/c4-context.md) — **PlantUML C4** |
| 2 | Segregação de serviços e responsabilidades | [C4 Nível 2](docs/architecture/c4-containers.md) — **PlantUML C4** |
| 3 | Trade-offs e alternativas rejeitadas | [ADRs](docs/adr/README.md) |
| 4 | Requisitos com porquê e regras de negócio | [Requisitos Funcionais](docs/requirements/requisitos-funcionais.md) |
| 5 | RNFs mensuráveis (incl. 50 req/s e 5% perda) | [Requisitos Não Funcionais](docs/requirements/requisitos-nao-funcionais.md) |
| 6 | RBAC, JWT e critérios de integração | [RBAC](docs/security/rbac.md) |
| 7 | Rastreabilidade enunciado → documento | [`docs/README.md`](docs/README.md) |
| 8 | Fluxos por feature (sequência) | §13 · [`sequences.md`](docs/architecture/sequences.md) |

**POC implementado em código:** registro de lançamentos, fila SQS (LocalStack), worker de consolidação, consulta de saldo, testes unitários/integração.  
**Especificado na documentação (produção):** Cognito, RBAC multi-tenant, Redis, Lambda, DLQ — ver coluna *Estado no POC* nos ADRs.

> **Convenção de diagramas:** estrutura → **C4 PlantUML** · comportamento por feature → **sequência PlantUML** ([`sequences.md`](docs/architecture/sequences.md))

---

# 0. Documentação de Arquitetura e Governança

Este README cobre operação, diagramas de sequência e FinOps. A documentação arquitetural formal está em [`docs/`](docs/README.md), alinhada ao enunciado do desafio.

### Enunciado do desafio → onde encontrar

| Requisito | Documento |
|-----------|-----------|
| Domínios funcionais e capacidades | [§7](#7-domínios-funcionais-e-capacidades-arquitetura-de-negócio) · [C4 L1](docs/architecture/c4-context.md) |
| Refinamento de RFs | [§8](#8-requisitos-funcionais-rf) · [Requisitos Funcionais](docs/requirements/requisitos-funcionais.md) |
| Refinamento de RNFs | [§9](#9-requisitos-não-funcionais-rnf) · [RNFs](docs/requirements/requisitos-nao-funcionais.md) |
| Arquitetura Alvo | [§5](#5️-arquitetura-final-novo-sistema) · [C4 L2](docs/architecture/c4-containers.md) |
| Justificativa de tecnologias | [§10](#10-justificativa-da-arquitetura-e-tecnologias) · [ADRs](docs/adr/README.md) |
| Arquitetura de Transição | [§3](#3estratégia-de-migração--strangler-fig-pattern) · [§6](#6-arquitetura-de-transição-migração-do-legado---strangler) · [ADR-0001](docs/adr/0001-strangler-fig-migration.md) |
| Estimativa de custos | [§14](#14-finops-high-level) |
| Monitoramento e Observabilidade | [§11](#11-monitoramento-e-observabilidade) |
| Segurança (consumo de serviços) | [§12](#12-segurança-e-integração) · [RBAC](docs/security/rbac.md) |
| Fluxos técnicos (sequência por feature) | [§13](#13-diagramas-de-sequência-features) · [sequences.md](docs/architecture/sequences.md) |
| Testes | [§16](#16-testes-funcionais-e-unitarios) |
| Como rodar localmente | [§15](#15como-rodar-a-aplicação-localmente) |
| Evoluções futuras | [§18](#18-proximos--passos) |

### RNF crítico do enunciado

| Regra | Atendimento |
|-------|-------------|
| Lançamentos disponíveis mesmo se consolidado cair | CQRS + fila — [ADR-0002](docs/adr/0002-cqrs-event-driven-consolidation.md) |
| Consolidado: 50 req/s em pico, máx. 5% de perda | Fila + retry/DLQ — [ADR-0005](docs/adr/0005-sqs-async-messaging.md) · [RNFs](docs/requirements/requisitos-nao-funcionais.md) |

Índice completo: [`docs/README.md`](docs/README.md)

---

# 1. O Problema / Contexto Atual
O sistema atual é composto por:

- Front-end legado monolítico
- Backend legado acoplado
- Banco de dados único
- Autenticação própria e não padronizada
- Baixa escalabilidade
- Dificuldade de manutenção
- Risco operacional ao evoluir funcionalidades

O negócio exige:

- Modernização sem interrupção
- Melhor performance
- Segurança unificada
- Escalabilidade sob demanda
- Evolução contínua
- Migração sem “big bang”



# 2.Objetivo da Migração
Modernizar o front com Blazor WebAssembly.
Modernizar o backend com arquitetura serverless.
Garantir segurança com OAuth2 + OpenID Connect.
Migrar sem downtime.
Manter o legado funcionando até o fim.
Toda a plataforma — legado e novo — usa **o mesmo Identity Provider** (ex.: Cognito OIDC).

### Benefícios:
- SSO entre front legado e novo front
- Tokens JWT padronizados
- Segurança consistente
- Autorização multi-tenant via claims
- Migração suave sem múltiplos logins

### Fluxo:
- Front legado → IdP
- Novo front Blazor → IdP
- API Gateway → valida JWT
- Lambdas → usam claims (`merchantId`, `roles`)


# 3.Estratégia de Migração — Strangler Fig Pattern
1. **Manter o legado funcionando**
2. Criar o novo sistema ao lado do legado
3. Redirecionar funcionalidades específicas para o novo front/backend
4. Usar **CDC** para sincronizar dados entre legado e novo banco
5. Expandir o novo sistema gradualmente
6. “Estrangular” o legado até substituí-lo por completo



# 4.Arquitetura Atual (Legado)

![Sistema Legado](docs/images/svg/c4-legado.svg)

**Fonte PlantUML (C4):** [`c4-legado.puml`](docs/images/plantuml/c4-legado.puml)



# 5.🏗️ Arquitetura Final (Novo sistema)
## 🧾 Sistema de Fluxo de Caixa, Consolidação Diária e Relatórios  

- Registro de lançamentos (débito/crédito)  
- Consolidação diária assíncrona  
- Relatórios rápidos  
- Arquitetura serverless  
- Alta escalabilidade e baixo acoplamento  
- Migração gradual de ambiente legado  
---

![Arquitetura Alvo — C4 Nível 2](docs/images/svg/c4-containers-prod.svg)

**Fonte PlantUML (C4):** [`c4-containers-prod.puml`](docs/images/plantuml/c4-containers-prod.puml) · [Documentação C4 L2](docs/architecture/c4-containers.md)


# 6. Arquitetura de Transição (Migração do Legado) - Strangler

![Arquitetura de Transição](docs/images/svg/c4-transicao-strangler.svg)

**Fonte PlantUML (C4):** [`c4-transicao-strangler.puml`](docs/images/plantuml/c4-transicao-strangler.puml) · [ADR-0001](docs/adr/0001-strangler-fig-migration.md)

## Fluxo de Migração (Simplificado)

![Fluxo de Migração](docs/images/svg/seq-migracao-etapas.svg)

**Fonte PlantUML:** [`seq-migracao-etapas.puml`](docs/images/plantuml/seq-migracao-etapas.puml)



# 7. Domínios Funcionais e Capacidades (Arquitetura de negócio)

> Alinhamento com o enunciado: *serviço de lançamentos* e *serviço de consolidado diário*. Visão C4: [Nível 1](docs/architecture/c4-context.md) · [Nível 2](docs/architecture/c4-containers.md)

**Lançamentos**
---------------

*   Registrar lançamento
    
*   Consultar lançamentos
    
*   Auditar histórico
    
*   Publicar evento “LançamentoCriado”
    

**Consolidação**
----------------

*   Processar eventos
    
*   Calcular saldo diário
    
*   Atualizar Redis
    
*   Reprocessar falhas (DLQ)
    

**Relatórios**
--------------

*   Consultar saldo diário
    
*   Gerar relatórios por período
    
*   Fallback para Aurora
    

**Segurança**
-------------

*   Autenticação (Cognito)
    
*   Autorização por comerciante
    
*   Tokens JWT
    

**Observabilidade**
-------------------

*   Logs estruturados
    
*   Métricas técnicas e de negócio
    
*   Alarmes
    
*   Tracing (X-Ray)
    

# 8. Requisitos Funcionais (RF)

> **Versão refinada** (objetivo de negócio, regras, critérios de aceite): [docs/requirements/requisitos-funcionais.md](docs/requirements/requisitos-funcionais.md)

Resumo:
*   **RF01** Registrar lançamento financeiro
    
*   **RF02** Consultar lançamentos
    
*   **RF03** Publicar evento de lançamento criado
    
*   **RF04** Processar eventos de lançamento
    
*   **RF05** Atualizar saldo diário consolidado
    
*   **RF06** Registrar falhas em DLQ
    
*   **RF07** Consultar saldo diário
    
*   **RF08** Gerar relatórios consolidados
    
*   **RF09** Autenticação via Cognito
    
*   **RF10** Autorização por comerciante
    
*   **RF11** Registrar logs estruturados
    
*   **RF12** Monitorar filas, erros e latência
    

# 9. Requisitos Não Funcionais (RNF)

> **Versão refinada** (métricas, medição, ligação com ADRs e RNF crítico do enunciado): [docs/requirements/requisitos-nao-funcionais.md](docs/requirements/requisitos-nao-funcionais.md)

Resumo:
### **Desempenho**

*   Saldo diário: < 50 ms (Redis)
    
*   Registro de lançamento: < 200 ms
    

### **Escalabilidade**

*   Consolidado: ≥ 50 req/s em pico (eventos na fila)
*   Perda máxima no consolidado: ≤ 5%
*   Fila absorve picos de escrita sem derrubar lançamentos

### **Disponibilidade**

*   Multi‑AZ
    
*   Tolerância a falhas
    
*   Lançamentos disponíveis mesmo se o consolidado cair (CQRS + fila)

### **Segurança**

*   TLS obrigatório
    
*   JWT
    
*   IAM least privilege
    
*   Criptografia KMS
    

### **Manutenibilidade**

*   Baixo acoplamento
    
*   Observabilidade completa
    

### **Custo**

*   Pay‑per‑use
    
*   Cache reduz carga no Aurora
    

# 10. Justificativa da Arquitetura e Tecnologias

> **Decisões formalizadas com alternativas e trade-offs:** consulte os [ADRs em `docs/adr/`](docs/adr/README.md). Esta seção é um resumo executivo; os ADRs são a fonte canônica das justificativas.

Atributos:
### ✅ Escalabilidade

Cada serviço escala de forma independente.

### ✅ Disponibilidade

Falhas isoladas não derrubam o sistema.

### ✅ Performance

Relatórios via Redis, lançamentos via Lambda, consolidação assíncrona.

### ✅ Segurança

Princípio de menor privilégio, JWT por serviço, superfícies menores.

### ✅ Observabilidade

Logs, métricas e alarmes por domínio.

### ✅ Manutenibilidade

Evolução contínua sem impacto no restante.

### ✅ Custo

Pay-per-use, cache reduz carga, Aurora Serverless ajusta capacidade.

### ✅ Suporte ao Strangler Fig Pattern

Permite substituir o legado por partes.

Produtos (detalhes nos ADRs):

### **Serverless** — [ADR-0007](docs/adr/0007-serverless-api-gateway-lambda.md)

*   Escalabilidade automática
*   Baixo custo
*   Alta disponibilidade
*   Zero manutenção

### **Aurora Serverless** — [ADR-0003](docs/adr/0003-aurora-transactional-store.md)

*   Transações ACID
*   Consistência forte
*   SQL completo

### **Redis** — [ADR-0004](docs/adr/0004-redis-read-model.md)

*   Leitura ultrarrápida
*   Ideal para saldos consolidados

### **SQS** — [ADR-0005](docs/adr/0005-sqs-async-messaging.md)

*   Desacoplamento total
*   Resiliência
*   Reprocessamento via DLQ

### **CQRS / Eventos** — [ADR-0002](docs/adr/0002-cqrs-event-driven-consolidation.md)

*   Escrita desacoplada da consolidação
*   Resiliência entre lançamentos e consolidado
# 11. Monitoramento e Observabilidade

> Detalhamento de RNFs: [RNF07](docs/requirements/requisitos-nao-funcionais.md#rnf07--observabilidade)

### **Logs**

*   CloudWatch Logs
    
*   Logs estruturados (JSON)
    

### **Métricas**

*   Latência
    
*   Erros
    
*   Tamanho da fila
    
*   Cache hit/miss
    

### **Alarmes**

*   DLQ > 0
    
*   Latência alta
    
*   Erros de Lambda
    

### **Tracing**

*   AWS X-Ray


# 12. Segurança e Integração

> RBAC, papéis, claims JWT e matriz de endpoints: [docs/security/rbac.md](docs/security/rbac.md) · [ADR-0006](docs/adr/0006-cognito-identity-provider.md) · [ADR-0009](docs/adr/0009-rbac-jwt-claims.md)
==========================

### **Autenticação e Autorização**

*   Cognito + JWT
    
*   Claims com comercianteId
    

### **Comunicação Segura**

*   TLS 1.2+
    
*   VPC privada
    
*   SGs restritivos
    

### **IAM Least Privilege**

*   Cada Lambda só acessa o que precisa
    

### **Auditoria**

*   CloudTrail
    
*   Logs de acesso
    
*   Logs de falha



# 13. Diagramas de Sequência (Features)

> **Convenção:** comportamento por feature → diagramas de sequência. Estrutura do sistema → [C4 PlantUML](docs/architecture/c4-context.md).

Índice completo com todos os fluxos: **[`docs/architecture/sequences.md`](docs/architecture/sequences.md)**

## Fluxo completo

![Fluxo completo](docs/images/svg/seq-fluxo-completo.svg)

**Fonte PlantUML:** [`seq-fluxo-completo.puml`](docs/images/plantuml/seq-fluxo-completo.puml) · [Índice](docs/architecture/sequences.md)

## Registrar lançamento (RF01)

![Sequência - Registrar Lançamento](docs/images/svg/seq-rf01-registrar-lancamento.svg)

**Fonte PlantUML:** [`seq-rf01-registrar-lancamento.puml`](docs/images/plantuml/seq-rf01-registrar-lancamento.puml)

## Consolidação (RF04/RF05)

![Sequência - Consolidação](docs/images/svg/seq-rf04-rf05-consolidar-dia.svg)

**Fonte PlantUML:** [`seq-rf04-rf05-consolidar-dia.puml`](docs/images/plantuml/seq-rf04-rf05-consolidar-dia.puml)

## Consulta de saldo (RF07)

![Sequência - Consulta de Saldo](docs/images/svg/seq-rf07-consultar-saldo.svg)

**Fonte PlantUML:** [`seq-rf07-consultar-saldo.puml`](docs/images/plantuml/seq-rf07-consultar-saldo.puml)
# 14. Finops (High-Level)
## 📊 FinOps – Resumo de Custos AWS

A arquitetura foi projetada seguindo princípios **FinOps** e **Serverless**, priorizando **baixo custo em idle**, **escalabilidade automática** e **pagamento por uso**.

### 📈 Cenário considerado
- ~1.000.000 requisições por mês
- Região AWS: us-east-1
- Perfil de uso: SaaS financeiro (lançamentos, consolidação e relatórios)

### 💰 Custo mensal estimado
**≈ USD 100 / mês**

### 🔍 Principais componentes de custo
- **Aurora Serverless v2 (~75%)**  
  Banco transacional ACID com auto scale e ACU mínimo configurado.
- **ElastiCache Redis (~12%)**  
  Cache de saldos consolidados, reduzindo leituras no banco.
- **Demais serviços (~13%)**  
  CloudFront, S3, API Gateway (HTTP API), Lambda, SQS/EventBridge e CloudWatch.

### ✅ Benefícios FinOps
- Sem servidores dedicados (EC2 ou Kubernetes)
- Zero custo quando não há tráfego
- Escala automática conforme a demanda
- Custos previsíveis por volume de requisições

### ⚠️ Pontos de atenção
- Configurar corretamente o ACU mínimo do Aurora
- Definir política de retenção de logs no CloudWatch
- Aplicar throttling no API Gateway para evitar abuso

> Esta estimativa é aproximada e pode variar conforme o volume real de uso, padrões de acesso e região AWS.



# 15.Como rodar a aplicação localmente

## 🧰 Pré-requisitos – LocalStack em Docker
- .NET 10 SDK installed
- PostgreSQL available and reachable
- Docker
- LocalStack
- (Optional) dotnet-ef tool: `dotnet tool install --global dotnet-ef`

Para executar o LocalStack localmente utilizando Docker, certifique-se de que os seguintes requisitos estejam atendidos.

### 🖥️ Sistema Operacional
- Windows 10/11 (com WSL2)
- macOS
- Linux

1) Subir o localstack/postgres usando o docker

> **Screenshots:** as imagens abaixo foram hospedadas no GitHub. Se não carregarem localmente, visualize o README em [github.com](https://github.com) após o push, ou substitua por arquivos em `docs/images/`.

<img width="341" height="477" alt="Docker compose" src="https://github.com/user-attachments/assets/0cac707c-48ae-43b4-8bb7-57a2039a96bd" />
```  
docker-compose up -d
```  

2. Verificar connection strings
- Edit the `Default` connection string in `src/Lancamentos.Api/appsettings.json`, `src/Relatorios.Api/appsettings.json` and `src/Consolidador.Worker/appsettings.json`, or set an environment variable:
3. Build solution
4. Aplicar migrations se necessário
  ```bash
  dotnet ef database update --startup-project src/Infrastructure
  ```

5. Subir os projetos conforme imagem abaixo.

<img width="803" height="541" alt="image" src="https://github.com/user-attachments/assets/7e03168f-463a-4733-9098-53db1716bf6a" />


## Como testar 

## (Lancamento.Api)
 - `http://localhost:5000/swagger`

  - 📘 API de Lançamentos
=====================

API responsável por registrar lançamentos financeiros (crédito e débito) em um fluxo de caixa diário.

## 📌 Endpoint

### ➕ Registrar Lançamento

- `POST /api/lancamentos`  
- Descrição: Registra um lançamento financeiro para uma data específica.

### 📥 Request

- Headers:
  - `Content-Type: application/json`

- Body (exemplo):

```json
{
  "valor": 150.00,
  "descricao": "Venda de produto",
  "data": "2025-01-10",
  "tipo": 1
}
```

### 🧾 Campos do Request

| Campo     | Tipo                | Obrigatório | Descrição                     |
|-----------|---------------------|-------------|-------------------------------|
| `valor`   | number (double)     | ✅          | Valor do lançamento (> 0)     |
| `descricao` | string            | ✅          | Descrição do lançamento       |
| `data`    | string (date)       | ✅          | Data no formato `yyyy-MM-dd`  |
| `tipo`    | integer             | ✅          | Tipo do lançamento (enum)     |

#### 🔢 Enum: `TipoLancamento`

| Código | Descrição |
|--------|-----------|
| 1      | Crédito   |
| 2      | Débito    |

---

### 📤 Response

- Sucesso: `201 Created` (corpo vazio no POC atual)

### ⚠️ Possíveis Erros

| Status | Descrição                         |
|--------|-----------------------------------|
| 400    | Dados inválidos (validação)       |
| 500    | Erro interno                      |

---

## 🧠 Observações Técnicas

- Arquitetura orientada a CQRS.  
- Validações realizadas na Application Layer.  
- Consolidação diária pode ocorrer de forma assíncrona (event-driven).  
- Compatível com MassTransit / SQS / Kafka.

---

# (Relatorio.Api)
API responsavel por gerar o relatório consolidado do dia. 

**Formato da data do relatório:** `yyyy-MM-dd`

## Exemplo de requisição
`GET /api/relatorios/2025-01-10`

## Response

- `200 OK` — relatório consolidado do dia (corpo JSON com `dia` e `saldo`)
- `204 No Content` — dia sem consolidação registrada

### Campos do Response (200)
| Campo | Tipo            | Descrição                     |
|-------|-----------------|-------------------------------|
| dia   | string (date)   | Data do relatório (yyyy-MM-dd)|
| saldo | number (double) | Saldo consolidado do dia      |

### Possíveis Erros
| Status | Descrição                  |
|--------|---------------------------|
| 400    | Data inválida             |
| 500    | Erro interno              |

### Observações Técnicas
- API de consulta (Query)
- Segue padrão CQRS
- Dados consolidados previamente via eventos
- Leitura otimizada (read model)
- Compatível com event-driven architecture

### Exemplo de uso (curl)

```bash
curl -X GET http://localhost:5001/api/relatorios/2025-01-10
```

# (Frontend)

Abaixo conferir a URL do frontend do comerciante.

  - `https://localhost:5280/fluxo-caixa`

Print screen da tela:    
<img width="1359" height="700" alt="image" src="https://github.com/user-attachments/assets/8d4fad41-13df-4f2a-a4cf-10bfb2a000d2" />



# 16. Testes funcionais e unitarios:


<img width="1054" height="418" alt="image" src="https://github.com/user-attachments/assets/f5cbc1d9-02c8-4acb-9236-836cfbae0ff7" />



# 18. Proximos  passos 

Evoluções planejadas (documentadas nos ADRs quando aplicável):

| Item | Referência |
|------|------------|
| Retry e DLQ para falhas transitórias | [ADR-0005](docs/adr/0005-sqs-async-messaging.md) |
| Idempotência / mensagens duplicadas | [ADR-0008](docs/adr/0008-consolidation-full-recalc-strategy.md) |
| Logs e observability | [RNF07](docs/requirements/requisitos-nao-funcionais.md) · [§11](#11-monitoramento-e-observabilidade) |
| Autenticação e autorização (RBAC) | [ADR-0006](docs/adr/0006-cognito-identity-provider.md) · [RBAC](docs/security/rbac.md) |
| Cache Redis (read model) | [ADR-0004](docs/adr/0004-redis-read-model.md) |
| Testes de contrato e performance | — |
| Segregação de bancos (write/read) | [ADR-0003](docs/adr/0003-aurora-transactional-store.md) · [ADR-0004](docs/adr/0004-redis-read-model.md) |
| IaC (CloudFormation/Terraform) | — |
| Novos requisitos funcionais | [Requisitos Funcionais](docs/requirements/requisitos-funcionais.md) |
| Deploy AWS (CI/CD) | [ADR-0007](docs/adr/0007-serverless-api-gateway-lambda.md) |

