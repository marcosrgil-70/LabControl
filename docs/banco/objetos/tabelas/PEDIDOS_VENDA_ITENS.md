# PEDIDOS_VENDA_ITENS

Itens contratados do pedido (produto × quantidade × valor).

**Grupo:** Propostas e pedidos · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-13

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| — | — |

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_PEDIDOS_VENDA_ITENS` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 3 | `ID_PEDIDOS_VENDA` | INT | não |  | aplicação |  |
| 4 | `ID_LAB_PROPOSTAS_PRODUTOS` | INT | não |  | aplicação |  |
| 5 | `ID_PRODUTOS` | INT | não |  | aplicação |  |
| 6 | `QTDE_CONTRATADA` | INT | não |  | aplicação |  |
| 7 | `VR_TOTAL` | DECIMAL(15,2) | não | 0 | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_PEDIDOS_VENDA_ITENS`
- **Única** `UQ_PEDITEM` (ID_PEDIDOS_VENDA, ID_LAB_PROPOSTAS_PRODUTOS)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |
| `ID_PEDIDOS_VENDA` | [`PEDIDOS_VENDA`](../tabelas/PEDIDOS_VENDA.md) | ID_PEDIDOS_VENDA | CASCADE |
| `ID_LAB_PROPOSTAS_PRODUTOS` | [`LAB_PROPOSTAS_PRODUTOS`](../tabelas/LAB_PROPOSTAS_PRODUTOS.md) | ID_LAB_PROPOSTAS_PRODUTOS | bloqueia |
| `ID_PRODUTOS` | [`PRODUTOS`](../tabelas/PRODUTOS.md) | ID_PRODUTOS | bloqueia |

### É referenciada por
—

## Triggers
- —

## Usada por
- **Views:** [`VW_PEDIDOS_VENDA_SALDO`](../views/VW_PEDIDOS_VENDA_SALDO.md)
- **Rotinas que alteram:** [`SP_GERAR_PEDIDO_VENDA`](../rotinas/SP_GERAR_PEDIDO_VENDA.md), [`SP_ATUALIZAR_PEDIDO_REVISAO`](../rotinas/SP_ATUALIZAR_PEDIDO_REVISAO.md)
- **Rotinas que leem:** [`PR_ATUALIZAR_SITUACAO_PEDIDO`](../rotinas/PR_ATUALIZAR_SITUACAO_PEDIDO.md)
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T21 Pedidos de venda](../../../telas/T21-pedidos-venda.md)
- **Processos:** [P08 Pedido de venda](../../../processos/P08-pedido-venda.md)

## Regras de negócio
- Recriados por `SP_ATUALIZAR_PEDIDO_REVISAO`.
- Recebido × contratado na view `VW_PEDIDOS_VENDA_SALDO`.

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- Chave auto-incremento: `ID_PEDIDOS_VENDA_ITENS`.

## Definição (DDL)
```sql
CREATE TABLE PEDIDOS_VENDA_ITENS (
  ID_PEDIDOS_VENDA_ITENS     INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS                INT          NOT NULL,
  ID_PEDIDOS_VENDA           INT          NOT NULL,
  ID_LAB_PROPOSTAS_PRODUTOS  INT          NOT NULL,
  ID_PRODUTOS                INT          NOT NULL,
  QTDE_CONTRATADA            INT          NOT NULL,
  VR_TOTAL                   DECIMAL(15,2) NOT NULL DEFAULT 0,
  CONSTRAINT UQ_PEDITEM UNIQUE (ID_PEDIDOS_VENDA, ID_LAB_PROPOSTAS_PRODUTOS),
  CONSTRAINT FK_PEDITEM_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS),
  CONSTRAINT FK_PEDITEM_PED FOREIGN KEY (ID_PEDIDOS_VENDA) REFERENCES PEDIDOS_VENDA (ID_PEDIDOS_VENDA) ON DELETE CASCADE,
  CONSTRAINT FK_PEDITEM_PROPPROD FOREIGN KEY (ID_LAB_PROPOSTAS_PRODUTOS) REFERENCES LAB_PROPOSTAS_PRODUTOS (ID_LAB_PROPOSTAS_PRODUTOS),
  CONSTRAINT FK_PEDITEM_PROD FOREIGN KEY (ID_PRODUTOS) REFERENCES PRODUTOS (ID_PRODUTOS)
) ENGINE=InnoDB;
```
