# T10-20 — Status das Propostas

**Fase:** 2 · **Rota:** `/Auxiliares/LAB_PROPOSTAS_STATUS` · **Permissão:** `AUXILIARES.*` · **Criticidade:** ⚪
**Legado:** `LabControl/un_CadStatusPropostas.pas` · menu *Cadastros → Status das Propostas* · 3 registro(s) no legado
**Objetos do banco:** [`LAB_PROPOSTAS_STATUS`](../banco/objetos/tabelas/LAB_PROPOSTAS_STATUS.md)
**Padrão da tela:** [T10-00](T10-00-crud-auxiliares.md) · **Processos:** [P01 Multiempresa](../processos/P01-multiempresa.md) · **Regras:** [RN-03](../regras/RN-03-cadastros.md)

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Descrição* | `DESCRICAO` | até 60 |
| Cor | `COR` | `#RRGGBB` |
| Permite receber amostra | `PERMITE_AMOSTRA` | **novo**, padrão sim |
| Ativo | `ATIVO` | inativo some dos combos de inclusão |

## Regras específicas
- `PERMITE_AMOSTRA = 0` → o recebimento recusa a proposta ("O status da proposta não permite receber amostras.").
- Status inicial e status "Enviado ao cliente" definidos na T05. Atenção: no legado `ID_LAB_PROPOSTAS_STATUS_ENVCLI` estava **nulo**, então a gravação automática de `DT_ENVIO_CLIENTE` nunca aconteceu — a migração deve apontar o status 3.
- Cor do legado -16777214 (cor de sistema) vira nulo na migração.
- Seed: Em elaboração, Aguardando informação do cliente, Enviado ao cliente.

## Onde é usado
Proposta (T20), Recebimento (T22).

## Critérios de aceite
- [ ] Itens do [padrão T10-00](T10-00-crud-auxiliares.md#critérios-de-aceite-valem-para-todos).

**Status:** ⬜
