# T10-08 — Unidades de Medida

**Fase:** 2 · **Rota:** `/Auxiliares/UNIDADES` · **Permissão:** `AUXILIARES.*` · **Criticidade:** ⚪
**Legado:** `LabControl/un_CadUnidades.pas` · menu *Cadastros → Cadastros Básicos → Unidades* · 27 registro(s) no legado
**Objetos do banco:** [`UNIDADES`](../banco/objetos/tabelas/UNIDADES.md)
**Padrão da tela:** [T10-00](T10-00-crud-auxiliares.md) · **Processos:** [P01 Multiempresa](../processos/P01-multiempresa.md) · **Regras:** [RN-03](../regras/RN-03-cadastros.md)

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Descrição* | `DESCRICAO` | até 60 |
| Sigla* | `SIGLA` | até 15, **única** por empresa (ex.: UFC/g, mg/L) |
| Ativo | `ATIVO` | inativo some dos combos de inclusão |

## Regras específicas
- A sigla compõe o texto do resultado no boletim (`FN_FORMATA_RESULTADO`).
- Unidade de estoque do produto e unidades do resultado/especificação/quantidade analisada.

## Onde é usado
Produtos (T14), Resultados (T26), Boletim (T27).

## Critérios de aceite
- [x] Itens do [padrão T10-00](T10-00-crud-auxiliares.md#critérios-de-aceite-valem-para-todos).

## Implementação (tarefa 2.2)
Campos `Descricao` e `Sigla`; sigla repetida → "Já existe uma unidade com esta sigla." no campo Sigla (`UQ_UNIDADE`). Teste: `Sigla_duplicada_vai_para_o_campo_sigla`. Comum a todos: `Cada_auxiliar_lista_e_abre_o_formulario` (`AuxiliaresEspecificosTests`) + padrão T10-00 em `AuxiliaresTests`.

**Status:** ✅ concluída em 21/10/2026 (tarefa 2.2) — entrada no `CatalogoAuxiliares` (CRUD genérico da 2.1). Testes: `AuxiliaresEspecificosTests` e, no navegador, `CrudNavegadorTelasTests`.
