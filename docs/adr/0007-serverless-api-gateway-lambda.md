# ADR-0007: API Gateway + AWS Lambda para backend serverless

## Status

**Accepted** — 2025-12-14

## Contexto

Objetivos de modernização:

- Escalabilidade automática sem gestão de servidores
- Pay-per-use (FinOps)
- Superfície de ataque reduzida (sem EC2 exposto)
- Integração com Cognito JWT Authorizer
- Deploy independente por bounded context (Lançamentos vs Relatórios)

Volume: ~50 req/s pico, .NET 10 como runtime de equipe.

## Decisão

Expor APIs via **Amazon API Gateway (HTTP API)** com integração **AWS Lambda**:

| Rota | Lambda | Método |
|------|--------|--------|
| `/lancamentos` | `LancamentosFunction` | POST |
| `/relatorios/{dia}` | `RelatoriosFunction` | GET |

- **JWT Authorizer** no API Gateway (Cognito)
- Lambdas em **VPC privada** para acesso a Aurora e Redis
- IAM roles por função (least privilege)
- Throttling e WAF na borda (produção)

O POC usa **ASP.NET Core** em processos long-running para velocidade de desenvolvimento e testes de integração — mesma lógica de domínio (`Core.Domain`), empacotamento diferente.

## Alternativas consideradas

| Alternativa | Por que foi descartada |
|-------------|------------------------|
| **ECS Fargate / containers always-on** | Custo base mesmo sem tráfego; gestão de scaling policies |
| **EKS (Kubernetes)** | Complexidade operacional desproporcional ao time e escala |
| **EC2 + ALB** | Gestão de patches, AMIs, auto-scaling manual |
| **App Runner** | Menos maduro para .NET + VPC + integração fina com SQS triggers mistos |
| **Monólito único Lambda** | Viola separação CQRS e deploy independente |

## Consequências

### Positivas

- Escala a zero em Lambdas (custo idle baixo exceto Aurora/Redis)
- Blast radius isolado por função
- Métricas por função no CloudWatch
- Alinhamento com "Domínio Técnico AWS" positivo no feedback

### Negativas

- **Cold start** em .NET Lambda (mitigar com Native AOT ou provisioned concurrency se necessário)
- **Limite de tempo** (15 min) — irrelevante para handlers atuais, mas constraint arquitetural
- **VPC + Lambda** adiciona latência de ENI (mitigar com RDS Proxy, subnets adequadas)
- Worker de consolidação é **separado** (SQS trigger), não rota HTTP — dois modelos de deploy

### Custo (ordem de grandeza)

Lambda + API Gateway ≈ 13% do orçamento mensal estimado no cenário de 1M req/mês — secundário frente a Aurora.

## Estado no POC

| Item | Status |
|------|--------|
| `Lancamentos.Api` (Kestrel) | ✅ |
| `Relatorios.Api` (Kestrel) | ✅ |
| `Consolidador.Worker` (hosted service) | ✅ |
| Lambda packaging / SAM / CDK | ❌ |
| API Gateway | ❌ |
| IAM per-function | ❌ |

A lógica de domínio é **portável** para Lambda; a decisão de compute é de **deploy**, não de modelagem de negócio.
