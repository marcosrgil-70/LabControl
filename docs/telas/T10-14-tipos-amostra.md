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
- [ ] Itens do [padrão T10-00](T10-00-crud-auxiliares.md#critérios-de-aceite-valem-para-todos).
- [ ] Código bloqueado para edição quando já existe amostra usando o tipo.
- [ ] Código fora de 1–99 ou repetido é recusado.

**Status:** ⬜
