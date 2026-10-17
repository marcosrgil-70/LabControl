# T10-17 — Status das Análises

**Fase:** 2 · **Rota:** `/Auxiliares/ANALISES_STATUS` · **Permissão:** `AUXILIARES.*` · **Criticidade:** ⚪
**Legado:** `LabControl/un_CadStatusAnalise.pas` · menu *Cadastros → Status das Análises* · 3 registro(s) no legado
**Objetos do banco:** [`ANALISES_STATUS`](../banco/objetos/tabelas/ANALISES_STATUS.md)
**Padrão da tela:** [T10-00](T10-00-crud-auxiliares.md) · **Processos:** [P01 Multiempresa](../processos/P01-multiempresa.md) · **Regras:** [RN-03](../regras/RN-03-cadastros.md)

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Descrição* | `DESCRICAO` | até 60 |
| Cor | `COR` | `#RRGGBB` |
| Ativo | `ATIVO` | inativo some dos combos de inclusão |

## Regras específicas
- Status de cada teste da amostra (`LAB_HIST_AMOSTRAS_TESTES.ID_ANALISES_STATUS`) — pouco usado no legado; a fila de análises (T26) usa a ausência de resultado.
- Seed: Aguardando análise, Em análise, Concluída.

## Onde é usado
Testes da amostra (T22/T26).

## Critérios de aceite
- [ ] Itens do [padrão T10-00](T10-00-crud-auxiliares.md#critérios-de-aceite-valem-para-todos).

**Status:** ⬜
