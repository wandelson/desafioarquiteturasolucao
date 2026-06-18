# Diagramas do projeto

## Convenção (padrão único)

| Tipo | Notação | Fonte | Imagem |
|------|---------|-------|--------|
| Estrutura (contexto, containers, legado, transição) | **C4-PlantUML** | `plantuml/c4-*.puml` | `svg/c4-*.svg` |
| Comportamento por feature | **Sequência PlantUML** | `plantuml/seq-*.puml` | `svg/seq-*.svg` |

Nos arquivos `.md`:

```markdown
![Titulo](../images/svg/nome.svg)

**Fonte PlantUML:** [`nome.puml`](../images/plantuml/nome.puml)
```

Índices: [C4 L1/L2](../architecture/c4-context.md) · [Sequências](../architecture/sequences.md)

---

## C4 Model (PlantUML)

| Arquivo | Descrição | SVG |
|---------|-----------|-----|
| [`c4-context.puml`](plantuml/c4-context.puml) | Nível 1 — Contexto | [`c4-context.svg`](svg/c4-context.svg) |
| [`c4-containers-prod.puml`](plantuml/c4-containers-prod.puml) | Nível 2 — Produção AWS | [`c4-containers-prod.svg`](svg/c4-containers-prod.svg) |
| [`c4-containers-poc.puml`](plantuml/c4-containers-poc.puml) | Nível 2 — POC local | [`c4-containers-poc.svg`](svg/c4-containers-poc.svg) |
| [`c4-legado.puml`](plantuml/c4-legado.puml) | Legado (monolito) | [`c4-legado.svg`](svg/c4-legado.svg) |
| [`c4-transicao-strangler.puml`](plantuml/c4-transicao-strangler.puml) | Transição Strangler Fig | [`c4-transicao-strangler.svg`](svg/c4-transicao-strangler.svg) |

---

## Sequência por feature (PlantUML)

| Arquivo | RF | SVG |
|---------|-----|-----|
| [`seq-rf01-registrar-lancamento.puml`](plantuml/seq-rf01-registrar-lancamento.puml) | RF01, RF03 | [`seq-rf01-registrar-lancamento.svg`](svg/seq-rf01-registrar-lancamento.svg) |
| [`seq-rf04-rf05-consolidar-dia.puml`](plantuml/seq-rf04-rf05-consolidar-dia.puml) | RF04, RF05 | [`seq-rf04-rf05-consolidar-dia.svg`](svg/seq-rf04-rf05-consolidar-dia.svg) |
| [`seq-rf07-consultar-saldo.puml`](plantuml/seq-rf07-consultar-saldo.puml) | RF07 | [`seq-rf07-consultar-saldo.svg`](svg/seq-rf07-consultar-saldo.svg) |
| [`seq-rf02-consultar-lancamentos.puml`](plantuml/seq-rf02-consultar-lancamentos.puml) | RF02 | [`seq-rf02-consultar-lancamentos.svg`](svg/seq-rf02-consultar-lancamentos.svg) |
| [`seq-rf09-rf10-autenticacao.puml`](plantuml/seq-rf09-rf10-autenticacao.puml) | RF09, RF10 | [`seq-rf09-rf10-autenticacao.svg`](svg/seq-rf09-rf10-autenticacao.svg) |
| [`seq-rbac-enforcement.puml`](plantuml/seq-rbac-enforcement.puml) | RF10 | [`seq-rbac-enforcement.svg`](svg/seq-rbac-enforcement.svg) |
| [`seq-fluxo-completo.puml`](plantuml/seq-fluxo-completo.puml) | RF01–RF07 | [`seq-fluxo-completo.svg`](svg/seq-fluxo-completo.svg) |
| [`seq-migracao-etapas.puml`](plantuml/seq-migracao-etapas.puml) | Migração | [`seq-migracao-etapas.svg`](svg/seq-migracao-etapas.svg) |

---

## Regenerar todas as imagens

```bash
docker run --rm \
  -v "$(pwd)/docs/images/plantuml:/data" \
  plantuml/plantuml:latest -tsvg /data/*.puml

docker run --rm \
  -v "$(pwd)/docs/images/plantuml:/data" \
  plantuml/plantuml:latest -tsvg /data/*.puml

cp docs/images/plantuml/*.svg docs/images/svg/
rm -f docs/images/plantuml/*.svg
```

### Editar / preview

- Extensão **PlantUML** no VS Code / Cursor
- Online: [plantuml.com](https://www.plantuml.com/plantuml)

---

## Pastas

| Pasta | Conteúdo |
|-------|----------|
| [`plantuml/`](plantuml/) | Fontes `.puml` (C4 + sequência) |
| [`svg/`](svg/) | Imagens commitadas nos `.md` |
