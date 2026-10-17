# T10-19 — Status dos Boletins

**Fase:** 2 · **Rota:** `/Auxiliares/BOLETINS_STATUS` · **Permissão:** `AUXILIARES.*` · **Criticidade:** ⚪
**Legado:** `LabControl/un_CadStatusBoletim.pas` · menu *Cadastros → Status dos Boletins* · 3 registro(s) no legado
**Objetos do banco:** [`BOLETINS_STATUS`](../banco/objetos/tabelas/BOLETINS_STATUS.md)
**Padrão da tela:** [T10-00](T10-00-crud-auxiliares.md) · **Processos:** [P01 Multiempresa](../processos/P01-multiempresa.md) · **Regras:** [RN-03](../regras/RN-03-cadastros.md)

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Descrição* | `DESCRICAO` | até 60 |
| Cor | `COR` | `#RRGGBB` |
| Ativo | `ATIVO` | inativo some dos combos de inclusão |

## Regras específicas
- Status inicial e status "emitido" são definidos nos parâmetros da empresa (T05); `SP_EMITIR_BOLETIM` aplica o status emitido.
- Único campo do boletim que pode mudar depois da emissão.
- Seed: Em elaboração, Em revisão, Enviado ao cliente.

## Onde é usado
Boletim (T27).

## Critérios de aceite
- [ ] Itens do [padrão T10-00](T10-00-crud-auxiliares.md#critérios-de-aceite-valem-para-todos).

**Status:** ⬜
