# M-12 Produtos e análises da proposta

**Parte:** 2 (fase 4.1) · **Criticidade:** 🔴 · **Depende de:** [M-11](M-11-propostas.md), [M-10](M-10-produtos-parametros.md)
**Processo:** [P24 Migração](../P24-migracao.md) · **Tela:** [T07](../../telas/T07-migracao.md)

## Origem (Firebird `DBMAIS.FDB`)
| Tabela | Registros |
|---|---|
| `LAB_PROPOSTAS_PRODUTOS` | 5.761 |
| `LAB_PROPOSTAS_PRODANALISES` | 19.252 |

## Destino (MySQL `labcontrol_v2`)
[`LAB_PROPOSTAS_PRODUTOS`](../../banco/objetos/tabelas/LAB_PROPOSTAS_PRODUTOS.md), [`LAB_PROPOSTAS_PRODANALISES`](../../banco/objetos/tabelas/LAB_PROPOSTAS_PRODANALISES.md)

## Regras de transformação
- `ITEM` = antigo `ID_LAB_PROPOSTAS_PRODUTOS` (o nº do item); nova chave própria; dicionário (proposta, item) → ID novo.
- Análises: valores **copiados** (triggers desligadas — não recalcular); `TIPO_DOCUMENTO` em maiúsculas (BOLETIM/RELATORIO).
- `ID_ORIGEM` (produto/análise da revisão anterior) por (item) e (item, parâmetro) quando único.
- 2 itens com o mesmo parâmetro duas vezes: migrar e listar.

## Regras gerais (todas as etapas)
- Conexão com `SET @LC_MIGRACAO = 1`; uma transação por etapa; `ID_LEGADO` + dicionário `idAntigo → idNovo`.
- Empresa 1; strings ISO8859_1 → UTF-8 com `Trim()`; 'S'/'N' → 1/0 (nulo → 0).
- Anomalias vão para o relatório da migração (não interrompem a etapa, salvo violação de integridade).

## Conferência ao final da etapa
- [ ] 5.761 / 19.252; Σ `VR_TOTAL` das análises por proposta = `VR_SOMA_VR_TOT_ANALIS` do legado.

**Status:** ⬜
