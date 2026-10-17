# CONDICOES_PAGTOS_PARC

Parcelas de uma condição de pagamento (opcional; nunca usado no legado).

**Grupo:** Cadastros auxiliares · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** —

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `CONDICOES_PAGTOS_PARC` (vazia) | 0 |

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_CONDICOES_PAGTOS_PARC` | INT | não |  | auto-incremento |  |
| 2 | `ID_CONDICOES_PAGTOS` | INT | não |  | aplicação |  |
| 3 | `NR_PARCELA` | INT | não |  | aplicação |  |
| 4 | `DIAS_VENCIMENTO` | INT | não | 0 | aplicação |  |
| 5 | `PORC_VENCIMENTO` | DECIMAL(7,4) | não | 0 | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_CONDICOES_PAGTOS_PARC`
- **Única** `UQ_CONDPARC` (ID_CONDICOES_PAGTOS, NR_PARCELA)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_CONDICOES_PAGTOS` | [`CONDICOES_PAGTOS`](../tabelas/CONDICOES_PAGTOS.md) | ID_CONDICOES_PAGTOS | CASCADE |

### É referenciada por
—

## Triggers
- —

## Usada por
- **Views:** —
- **Rotinas que alteram:** —
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T10-11 Condições de pagamento](../../../telas/T10-11-condicoes-pagamento.md)
- **Processos:** —

## Regras de negócio
- Se houver parcelas, Σ `PORC_VENCIMENTO` = 100.
- Herda a empresa da condição (sem `ID_EMPRESAS` próprio).

## Implementação no C# (EF Core)
- Chave auto-incremento: `ID_CONDICOES_PAGTOS_PARC`.
- Filtro de empresa via navegação para `CONDICOES_PAGTOS`.

## Definição (DDL)
```sql
CREATE TABLE CONDICOES_PAGTOS_PARC (
  ID_CONDICOES_PAGTOS_PARC INT AUTO_INCREMENT PRIMARY KEY,
  ID_CONDICOES_PAGTOS INT        NOT NULL,
  NR_PARCELA       INT          NOT NULL,
  DIAS_VENCIMENTO  INT          NOT NULL DEFAULT 0,
  PORC_VENCIMENTO  DECIMAL(7,4) NOT NULL DEFAULT 0,
  CONSTRAINT UQ_CONDPARC UNIQUE (ID_CONDICOES_PAGTOS, NR_PARCELA),
  CONSTRAINT FK_CONDPARC_COND FOREIGN KEY (ID_CONDICOES_PAGTOS) REFERENCES CONDICOES_PAGTOS (ID_CONDICOES_PAGTOS) ON DELETE CASCADE
) ENGINE=InnoDB;
```
