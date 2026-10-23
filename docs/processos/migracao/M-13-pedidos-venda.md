# M-13 Pedidos de venda

**Parte:** 2 (fase 4.1) · **Criticidade:** 🔴 · **Depende de:** [M-12](M-12-produtos-analises-proposta.md)
**Processo:** [P24 Migração](../P24-migracao.md) · **Tela:** [T07](../../telas/T07-migracao.md)

## Origem (Firebird `DBMAIS.FDB`)
| Tabela | Registros |
|---|---|
| `LAB_PROPOSTAS.ID_PEDIDOS_VENDAS` | 1.454 propostas |
|  | ≈1.435 pedidos |

## Destino (MySQL `labcontrol_v2`)
[`PEDIDOS_VENDA`](../../banco/objetos/tabelas/PEDIDOS_VENDA.md), [`PEDIDOS_VENDA_ITENS`](../../banco/objetos/tabelas/PEDIDOS_VENDA_ITENS.md)

## Regras de transformação
- `DISTINCT (empresa, COD, ANO, ID_PEDIDOS_VENDAS)` → **1 pedido por proposta**: `NR_PEDIDO = ID_PEDIDOS_VENDAS`, `ID_LAB_PROPOSTAS` = maior revisão que tem o número.
- 19 números repetidos em várias revisões: um pedido só (listar).
- `DT_PEDIDO = DT_AUTORIZACAO` (nula → data da proposta; listar).
- `SITUACAO = CONCLUIDO` se todas as amostras da proposta têm boletim; senão `EM_EXECUCAO` (sem amostras → `ABERTO`).
- Itens = produtos da revisão do pedido (qtde e Σ valores das análises).

## Regras gerais (todas as etapas)
- Conexão com `SET @LC_MIGRACAO = 1`; uma transação por etapa; `ID_LEGADO` + dicionário `idAntigo → idNovo`.
- Empresa 1; strings ISO8859_1 → UTF-8 com `Trim()`; 'S'/'N' → 1/0 (nulo → 0).
- Anomalias vão para o relatório da migração (não interrompem a etapa, salvo violação de integridade).

## Conferência ao final da etapa
- [x] Nº de pedidos = nº de propostas (cód./ano) com pedido: **1.433**. Os 1.434 números distintos do legado estão todos mapeados.

## Implementação (tarefa 4.1, 23/10/2026)
- `Services/Migracao/Etapas/M13PedidosVenda.cs`.
- INSERT direto em lote preservando `NR_PEDIDO`; 3.318 itens (produtos da revisão do pedido, valor = Σ das análises).
- Sequência `PEDIDO_VENDA` = 1.471 (maior número).
- **`SITUACAO`:** sem amostras → `ABERTO`, senão `EM_EXECUCAO`. O `CONCLUIDO` (todas as amostras com boletim) fica para a M-17 (5.1).
- **Legado real:**
  - a proposta 1189 tinha os pedidos 553 e 554 em revisões diferentes → fica o 554 (maior revisão); as amostras do 553 apontam para ele;
  - 20 pedidos sem data de autorização → data da solicitação.

**Status:** ✅ implementada na tarefa 4.1 (23/10/2026); `SITUACAO = CONCLUIDO` na 5.1.
