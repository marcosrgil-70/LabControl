# T10-13 — Moedas

**Fase:** 2 · **Rota:** `/Auxiliares/MOEDAS` · **Permissão:** `AUXILIARES.*` · **Criticidade:** ⚪
**Legado:** `— (sem tela no Delphi; 2 registros cadastrados direto no banco)` · menu *—* · 2 registro(s) no legado
**Objetos do banco:** [`MOEDAS`](../banco/objetos/tabelas/MOEDAS.md)
**Padrão da tela:** [T10-00](T10-00-crud-auxiliares.md) · **Processos:** [P01 Multiempresa](../processos/P01-multiempresa.md) · **Regras:** [RN-03](../regras/RN-03-cadastros.md)

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Descrição* | `DESCRICAO` | até 40 |
| Sigla* | `SIGLA` | até 10, única (ex.: R$, US$) |
| Ativo | `ATIVO` | inativo some dos combos de inclusão |

## Regras específicas
- Seed: Real (R$) e Dólar (US$).
- Opcional na proposta (`ID_MOEDAS`).

## Onde é usado
Proposta (T20) e Pedido (T21).

## Critérios de aceite
- [x] Itens do [padrão T10-00](T10-00-crud-auxiliares.md#critérios-de-aceite-valem-para-todos).

## Implementação (tarefa 2.2)
Campos `Descricao` e `Sigla`; sigla repetida → "Já existe uma moeda com esta sigla." (`UQ_MOEDA`). Teste: `Sigla_duplicada_vai_para_o_campo_sigla`. Comum a todos: `Cada_auxiliar_lista_e_abre_o_formulario` (`AuxiliaresEspecificosTests`) + padrão T10-00 em `AuxiliaresTests`.

**Status:** ✅ concluída em 21/10/2026 (tarefa 2.2) — entrada no `CatalogoAuxiliares` (CRUD genérico da 2.1). Testes: `AuxiliaresEspecificosTests` e, no navegador, `CrudNavegadorTelasTests`.
