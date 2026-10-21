# T10-10 — Tipos de Resultado (descritivos)

**Fase:** 2 · **Rota:** `/Auxiliares/LAB_TIPOS_RESULTADOS` · **Permissão:** `AUXILIARES.*` · **Criticidade:** ⚪
**Legado:** `LabControl/un_TipoResultado.pas` · menu *Cadastros → Cadastros Básicos → Tipo de Resultados* · 61 registro(s) no legado
**Objetos do banco:** [`LAB_TIPOS_RESULTADOS`](../banco/objetos/tabelas/LAB_TIPOS_RESULTADOS.md)
**Padrão da tela:** [T10-00](T10-00-crud-auxiliares.md) · **Processos:** [P01 Multiempresa](../processos/P01-multiempresa.md) · **Regras:** [RN-03](../regras/RN-03-cadastros.md)

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Descrição* | `DESCRICAO` | até 80, única por empresa (ex.: "Ausência em 25 g", "Satisfatório") |
| Ativo | `ATIVO` | inativo some dos combos de inclusão |

## Regras específicas
- É o texto impresso no boletim quando o resultado (ou a especificação) é descritivo.
- Cadastro rápido na tela de resultados (tecla Insert no legado).

## Onde é usado
Resultados (T26), Boletim (T27).

## Critérios de aceite
- [x] Itens do [padrão T10-00](T10-00-crud-auxiliares.md#critérios-de-aceite-valem-para-todos).

## Implementação (tarefa 2.2)
Só `Descricao` (única pela `UNIQUE` da tabela). Comum a todos: `Cada_auxiliar_lista_e_abre_o_formulario` (`AuxiliaresEspecificosTests`) + padrão T10-00 em `AuxiliaresTests`.

**Status:** ✅ concluída em 21/10/2026 (tarefa 2.2) — entrada no `CatalogoAuxiliares` (CRUD genérico da 2.1). Testes: `AuxiliaresEspecificosTests` e, no navegador, `CrudNavegadorTelasTests`.
