# LAB_PROPOSTAS_PRODUTOS

Produtos (itens) da proposta, com quantidade de amostras.

**Grupo:** Propostas e pedidos · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-12

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `LAB_PROPOSTAS_PRODUTOS` (PK composta proposta+empresa+item) | 5.761 |

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_LAB_PROPOSTAS_PRODUTOS` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | trigger TG_PROPPROD_BI |  |
| 3 | `ID_LAB_PROPOSTAS` | INT | não |  | aplicação |  |
| 4 | `ITEM` | INT | não |  | trigger TG_PROPPROD_BI | Nº do item na proposta (antigo ID_LAB_PROPOSTAS_PRODUTOS) |
| 5 | `ID_PRODUTOS` | INT | não |  | aplicação |  |
| 6 | `QTDE_AMOSTRAS` | INT | não | 1 | aplicação |  |
| 7 | `ID_ORIGEM` | INT | sim |  | aplicação | Item da revisão de origem |

## Chaves, restrições e índices
- **PK:** `ID_LAB_PROPOSTAS_PRODUTOS`
- **Única** `UQ_PROPPROD_ITEM` (ID_LAB_PROPOSTAS, ITEM)
- **Check** `CK_PROPPROD_QTDE`: `QTDE_AMOSTRAS > 0`

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |
| `ID_LAB_PROPOSTAS` | [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md) | ID_LAB_PROPOSTAS | CASCADE |
| `ID_PRODUTOS` | [`PRODUTOS`](../tabelas/PRODUTOS.md) | ID_PRODUTOS | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`LAB_PROPOSTAS_PRODANALISES`](../tabelas/LAB_PROPOSTAS_PRODANALISES.md) | ID_LAB_PROPOSTAS_PRODUTOS | CASCADE |
| [`PEDIDOS_VENDA_ITENS`](../tabelas/PEDIDOS_VENDA_ITENS.md) | ID_LAB_PROPOSTAS_PRODUTOS | bloqueia |
| [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) | ID_LAB_PROPOSTAS_PRODUTOS | bloqueia |

## Triggers
- [`TG_PROPPROD_BI`](../triggers/TG_PROPPROD_BI.md) — BEFORE INSERT: Item herda a empresa da proposta; bloqueia revisão antiga; numera o `ITEM`.
- [`TG_PROPPROD_BU`](../triggers/TG_PROPPROD_BU.md) — BEFORE UPDATE: Bloqueia alteração de item em revisão antiga.
- [`TG_PROPPROD_AU`](../triggers/TG_PROPPROD_AU.md) — AFTER UPDATE: Quantidade do produto propaga para todas as análises do item.

## Usada por
- **Views:** [`VW_LAB_PROPOSTAS_ANALISES`](../views/VW_LAB_PROPOSTAS_ANALISES.md), [`VW_LAB_AMOSTRAS`](../views/VW_LAB_AMOSTRAS.md), [`VW_PEDIDOS_VENDA_SALDO`](../views/VW_PEDIDOS_VENDA_SALDO.md)
- **Rotinas que alteram:** [`PR_REVISAO_PROPOSTA`](../rotinas/PR_REVISAO_PROPOSTA.md), [`SP_TROCAR_PRODUTO_PROPOSTA`](../rotinas/SP_TROCAR_PRODUTO_PROPOSTA.md)
- **Rotinas que leem:** [`PR_ATUALIZAR_SITUACAO_PEDIDO`](../rotinas/PR_ATUALIZAR_SITUACAO_PEDIDO.md), [`SP_GERAR_PEDIDO_VENDA`](../rotinas/SP_GERAR_PEDIDO_VENDA.md), [`SP_ATUALIZAR_PEDIDO_REVISAO`](../rotinas/SP_ATUALIZAR_PEDIDO_REVISAO.md)
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** [`TG_PRODAN_BI`](../triggers/TG_PRODAN_BI.md), [`TG_AMOSTRAS_BI`](../triggers/TG_AMOSTRAS_BI.md), [`TG_AMOSTRAS_BU`](../triggers/TG_AMOSTRAS_BU.md)

## Telas e processos
- **Telas:** [T20 Propostas](../../../telas/T20-propostas.md) · [T22 Recebimento de amostras](../../../telas/T22-recebimento-amostras.md)
- **Processos:** [P06 Cálculo dos valores da proposta](../../../processos/P06-valores-proposta.md) · [P07 Revisão da proposta](../../../processos/P07-revisao-proposta.md)

## Regras de negócio
- `ITEM` automático (1, 2, 3…) quando enviado 0.
- `QTDE_AMOSTRAS > 0`; alterar a quantidade altera a quantidade de todas as análises do item (`TG_PROPPROD_AU`).
- Excluir produto exclui as análises (cascade); bloquear se já houver amostra recebida para o item.
- A amostra aponta para este ID (no legado guardava só o número do item).

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- **Preenchidas pelo banco (trigger):** `ID_EMPRESAS`, `ITEM` — não depender do valor enviado; reler a linha após gravar.
- Chave auto-incremento: `ID_LAB_PROPOSTAS_PRODUTOS`.

## Definição (DDL)
```sql
CREATE TABLE LAB_PROPOSTAS_PRODUTOS (
  ID_LAB_PROPOSTAS_PRODUTOS INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS        INT          NOT NULL,
  ID_LAB_PROPOSTAS   INT          NOT NULL,
  ITEM               INT          NOT NULL COMMENT 'Nº do item na proposta (antigo ID_LAB_PROPOSTAS_PRODUTOS)',
  ID_PRODUTOS        INT          NOT NULL,
  QTDE_AMOSTRAS      INT          NOT NULL DEFAULT 1,
  ID_ORIGEM          INT          NULL COMMENT 'Item da revisão de origem',
  CONSTRAINT UQ_PROPPROD_ITEM UNIQUE (ID_LAB_PROPOSTAS, ITEM),
  CONSTRAINT CK_PROPPROD_QTDE CHECK (QTDE_AMOSTRAS > 0),
  CONSTRAINT FK_PROPPROD_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS),
  CONSTRAINT FK_PROPPROD_PROP FOREIGN KEY (ID_LAB_PROPOSTAS) REFERENCES LAB_PROPOSTAS (ID_LAB_PROPOSTAS) ON DELETE CASCADE,
  CONSTRAINT FK_PROPPROD_PROD FOREIGN KEY (ID_PRODUTOS) REFERENCES PRODUTOS (ID_PRODUTOS)
) ENGINE=InnoDB;
```
