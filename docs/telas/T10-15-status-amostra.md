# T10-15 — Status das Amostras

**Fase:** 2 · **Rota:** `/Auxiliares/AMOSTRAS_STATUS` · **Permissão:** `AUXILIARES.*` · **Criticidade:** ⚪
**Legado:** `LabControl/un_CadStatusAmostra.pas` · menu *Cadastros → Status das Amostras* · 4 registro(s) no legado
**Objetos do banco:** [`AMOSTRAS_STATUS`](../banco/objetos/tabelas/AMOSTRAS_STATUS.md)
**Padrão da tela:** [T10-00](T10-00-crud-auxiliares.md) · **Processos:** [P01 Multiempresa](../processos/P01-multiempresa.md) · **Regras:** [RN-03](../regras/RN-03-cadastros.md)

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Descrição* | `DESCRICAO` | até 60, única |
| Cor | `COR` | seletor de cor, `#RRGGBB` (legado `TColor` BGR) |
| Permite resultado | `PERMITE_RESULTADO` | **novo**: libera a amostra para Resultados, Boletim, Etiquetas e fila |
| Ativo | `ATIVO` | inativo some dos combos de inclusão |

## Regras específicas
- Deve existir ao menos um status com "permite resultado" (validar ao desmarcar/inativar).
- No legado o filtro usava um único status (`EMPRESAS_PARAMETROS.ID_AMOSTRAS_STATUS_RESULTADOS = 1`, "Liberada"); a migração marca `PERMITE_RESULTADO` nesse status.
- Seed: Liberada para análise (verde, permite), Bloqueada (azul), Reprovada – devolvida (roxo), Reprovada – descartada (vermelho).

## Onde é usado
Recebimento (T22), Resultados (T26), Boletim (T27), Etiquetas (T25).

## Critérios de aceite
- [ ] Itens do [padrão T10-00](T10-00-crud-auxiliares.md#critérios-de-aceite-valem-para-todos).

**Status:** ⬜
