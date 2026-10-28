# LAB_HIST_AMOSTRAS_SALDO

Saldo físico atual da amostra raiz (mantido por trigger).

**Grupo:** Amostras e estoque · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-15 (recalculado)

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `LAB_HIST_AMOSTRAS_SALDO` | 5.770 |

Comentário do script: _Saldo mantido por trigger de LAB_MOV_AMOSTRAS (tabela separada porque o MySQL não permite que a trigger de MOV altere LAB_HIST_AMOSTRAS dentro da trigger dela)._

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_LAB_HIST_AMOSTRAS` | INT | não |  | aplicação |  |
| 2 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 3 | `SALDO_ATUAL` | DECIMAL(15,4) | não | 0 | aplicação |  |
| 4 | `DATA_ATUALIZACAO` | DATETIME | não | CURRENT_TIMESTAMP | banco |  |

## Chaves, restrições e índices
- **PK:** `ID_LAB_HIST_AMOSTRAS`

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_LAB_HIST_AMOSTRAS` | [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) | ID_LAB_HIST_AMOSTRAS | CASCADE |
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |

### É referenciada por
—

## Triggers
- —

## Usada por
- **Views:** [`VW_LAB_AMOSTRAS`](../views/VW_LAB_AMOSTRAS.md)
- **Rotinas que alteram:** [`PR_ATUALIZA_SALDO`](../rotinas/PR_ATUALIZA_SALDO.md)
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** [`TG_MOV_BI`](../triggers/TG_MOV_BI.md), [`TG_MOV_BD`](../triggers/TG_MOV_BD.md)

## Telas e processos
- **Telas:** [T23 Movimentação de amostras](../../../telas/T23-movimentacao-amostras.md) · [T22 Recebimento de amostras](../../../telas/T22-recebimento-amostras.md)
- **Processos:** [P12 Estoque e saldo das amostras](../../../processos/P12-estoque-amostras.md)

## Regras de negócio
- Só `PR_ATUALIZA_SALDO` grava (chamada pelas triggers de movimento).
- Existe porque o MySQL não deixa a trigger de movimento alterar `LAB_HIST_AMOSTRAS`.

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- Datas mantidas pelo MySQL (`DEFAULT/ON UPDATE CURRENT_TIMESTAMP`): `DATA_ATUALIZACAO` — mapear como gerado pelo banco.
- Somente leitura.

## Definição (DDL)
```sql
CREATE TABLE LAB_HIST_AMOSTRAS_SALDO (
  ID_LAB_HIST_AMOSTRAS       INT          NOT NULL PRIMARY KEY,
  ID_EMPRESAS                INT          NOT NULL,
  SALDO_ATUAL                DECIMAL(15,4) NOT NULL DEFAULT 0,
  DATA_ATUALIZACAO           DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT FK_SALDO_AMO FOREIGN KEY (ID_LAB_HIST_AMOSTRAS) REFERENCES LAB_HIST_AMOSTRAS (ID_LAB_HIST_AMOSTRAS) ON DELETE CASCADE,
  CONSTRAINT FK_SALDO_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS)
) ENGINE=InnoDB;
```
