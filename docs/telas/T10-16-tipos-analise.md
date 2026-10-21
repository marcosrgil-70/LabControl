# T10-16 — Tipos de Análise

**Fase:** 2 · **Rota:** `/Auxiliares/ANALISES_TIPO` · **Permissão:** `AUXILIARES.*` · **Criticidade:** 🔴 compõe o número da amostra
**Legado:** `LabControl/un_CadTipoAnalise.pas` · menu *Cadastros → Tipos de Análises* · 13 registro(s) no legado
**Objetos do banco:** [`ANALISES_TIPO`](../banco/objetos/tabelas/ANALISES_TIPO.md)
**Padrão da tela:** [T10-00](T10-00-crud-auxiliares.md) · **Processos:** [P01 Multiempresa](../processos/P01-multiempresa.md) · [P04 Número da amostra](../processos/P04-numero-amostra-ano.md) · **Regras:** [RN-03](../regras/RN-03-cadastros.md)

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Código* | `CODIGO` | **novo**: 1–99, único por empresa — é o **AA** do número da amostra |
| Descrição* | `DESCRICAO` | até 60, única |
| Ativo | `ATIVO` | inativo some dos combos de inclusão |

## Regras específicas
- Mesma regra dos tipos de amostra: **código imutável depois de usado** (amostra ou parâmetro).
- O tipo de análise do parâmetro define quais tipos podem ser escolhidos no recebimento (só os tipos das análises do item da proposta).
- Na migração `CODIGO = ID antigo`.

## Códigos atuais no legado (preservar na migração)
| Código | Descrição |
|---:|---|
| 01 | CONTAGEM / PESQUISA / FÍSICO-QUÍMICO |
| 02 | ESTERILIDADE |
| 03 | ENDOTOXINAS |
| 04 | EFICÁCIA DE CONSERVANTE |
| 05 | DOSEAMENTO DE ANTIBIÓTICO |
| 06 | DOSEAMENTO DE VITAMINA |
| 07 | ATIVIDADE REDUCIONAL |
| 08 | DESENVOLVIMENTO DE METODOLOGIA |
| 09 | VALIDAÇÃO DE MÉTODO |
| 10 | IDENTIFICAÇÃO |
| 11 | MIC TEST |
| 12 | PROMOÇÃO DE CRESCIMENTO |
| 13 | FÍSICO-QUÍMICO |

## Onde é usado
Parâmetros de análise (T15), Recebimento (T22), número da amostra.

## Critérios de aceite
- [x] Itens do [padrão T10-00](T10-00-crud-auxiliares.md#critérios-de-aceite-valem-para-todos).
- [x] Código bloqueado para edição quando já existe amostra usando o tipo.
- [x] Código fora de 1–99 ou repetido é recusado.

## Implementação (tarefa 2.2)
Campos `Codigo` (inteiro 1–99) e `Descricao`. `PodeAlterarCampo`: código travado quando há amostra **ou parâmetro de análise** com o tipo. Testes: `Codigo_de_1_a_99_e_unico`, `Codigo_do_tipo_de_analise_trava_quando_um_parametro_usa`. Comum a todos: `Cada_auxiliar_lista_e_abre_o_formulario` (`AuxiliaresEspecificosTests`) + padrão T10-00 em `AuxiliaresTests`.

**Status:** ✅ concluída em 21/10/2026 (tarefa 2.2) — entrada no `CatalogoAuxiliares` (CRUD genérico da 2.1) — código 1–99 (`CK_ANATIP_COD`) travado quando há amostra **ou parâmetro de análise** usando o tipo (`PodeAlterarCampo`). Testes: `AuxiliaresEspecificosTests` e, no navegador, `CrudNavegadorTelasTests`.
