# T10-14 — Tipos de Amostra

**Fase:** 2 · **Rota:** `/Auxiliares/AMOSTRAS_TIPO` · **Permissão:** `AUXILIARES.*` · **Criticidade:** 🔴 compõe o número da amostra
**Legado:** `LabControl/un_CadTipoAmostra.pas` · menu *Cadastros → Tipos de Amostras* · 13 registro(s) no legado
**Objetos do banco:** [`AMOSTRAS_TIPO`](../banco/objetos/tabelas/AMOSTRAS_TIPO.md)
**Padrão da tela:** [T10-00](T10-00-crud-auxiliares.md) · **Processos:** [P01 Multiempresa](../processos/P01-multiempresa.md) · [P04 Número da amostra](../processos/P04-numero-amostra-ano.md) · **Regras:** [RN-03](../regras/RN-03-cadastros.md)

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Código* | `CODIGO` | **novo**: 1–99, único por empresa — é o **TT** do número da amostra |
| Descrição* | `DESCRICAO` | até 60, única |
| Ativo | `ATIVO` | inativo some dos combos de inclusão |

## Regras específicas
- No legado o **ID** compunha o número (TT). Com "tudo por empresa" o ID auto-increment não serve: o `CODIGO` assume esse papel.
- **O código não pode ser alterado depois de usado em alguma amostra** (os números já impressos mudariam). A tela bloqueia o campo e o serviço valida (`EXISTS` em `LAB_HIST_AMOSTRAS`).
- Na migração `CODIGO = ID antigo`.

## Códigos atuais no legado (preservar na migração)
| Código | Descrição |
|---:|---|
| 01 | FÁRMACO |
| 02 | COSMÉTICO |
| 03 | VETERINÁRIO |
| 04 | ALIMENTO |
| 05 | ÁGUA |
| 06 | AR E SUPERFÍCIE |
| 07 | SUPERFÍCIE |
| 08 | DISPOSITIVO CIRÚRGICO |
| 09 | MICRORGANISMO |
| 10 | CÉLULAS CRIOPRESERVADAS |
| 11 | ENSAIO DE PROFICIÊNCIA |
| 14 | RESINA MISTA |
| 15 | TESTE DE GRAVIDEZ |

## Onde é usado
Recebimento de amostras (T22) e número da amostra em todas as telas.

## Critérios de aceite
- [x] Itens do [padrão T10-00](T10-00-crud-auxiliares.md#critérios-de-aceite-valem-para-todos).
- [x] Código bloqueado para edição quando já existe amostra usando o tipo.
- [x] Código fora de 1–99 ou repetido é recusado.

## Implementação (tarefa 2.2)
Campos `Codigo` (inteiro 1–99) e `Descricao`. `PodeAlterarCampo`: código travado (cadeado + valor escondido no formulário, recusa no serviço) quando existe amostra com o tipo. Corrigido no `AuxiliarService.Converter`: campo `short` virava `int` no ternário (InvalidCastException ao gravar o código). Testes: `Codigo_de_1_a_99_e_unico`, `Codigo_do_tipo_de_amostra_trava_depois_de_usado`, navegador `Auxiliares_codigo_travado_cor_e_status_que_permite_resultado`. Comum a todos: `Cada_auxiliar_lista_e_abre_o_formulario` (`AuxiliaresEspecificosTests`) + padrão T10-00 em `AuxiliaresTests`.

**Status:** ✅ concluída em 21/10/2026 (tarefa 2.2) — entrada no `CatalogoAuxiliares` (CRUD genérico da 2.1) — código 1–99 (`CK_AMOTIP_COD`) travado no formulário e no serviço quando há amostra (`PodeAlterarCampo` → `EXISTS` em `LAB_HIST_AMOSTRAS`). Testes: `AuxiliaresEspecificosTests` e, no navegador, `CrudNavegadorTelasTests`.
