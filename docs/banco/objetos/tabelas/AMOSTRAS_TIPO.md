# AMOSTRAS_TIPO

Tipos de amostra. O **CODIGO** compõe o número da amostra (TT de `TT-CCCCC-AA/YY`).

**Grupo:** Cadastros auxiliares · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-03

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `AMOSTRAS_TIPO` (o ID era usado no número) | 13 |

Comentário do script: _CODIGO compõe o número da amostra (TT de TT-CCCCC-AA/YY). Imutável após uso._

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_AMOSTRAS_TIPO` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 3 | `CODIGO` | SMALLINT | não |  | aplicação |  |
| 4 | `DESCRICAO` | VARCHAR(60) | não |  | aplicação |  |
| 5 | `ATIVO` | TINYINT(1) | não | 1 | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_AMOSTRAS_TIPO`
- **Única** `UQ_AMOTIP_COD` (ID_EMPRESAS, CODIGO)
- **Única** `UQ_AMOTIP_DESC` (ID_EMPRESAS, DESCRICAO)
- **Check** `CK_AMOTIP_COD`: `CODIGO BETWEEN 1 AND 99`

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) | ID_AMOSTRAS_TIPO | bloqueia |

## Triggers
- —

## Usada por
- **Views:** [`VW_LAB_AMOSTRAS`](../views/VW_LAB_AMOSTRAS.md)
- **Rotinas que alteram:** —
- **Rotinas que leem:** [`FN_NR_AMOSTRA_ID`](../rotinas/FN_NR_AMOSTRA_ID.md)
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T10-14 Tipos de amostra](../../../telas/T10-14-tipos-amostra.md) · [T10-00 Padrão dos cadastros auxiliares](../../../telas/T10-00-crud-auxiliares.md)
- **Processos:** [P01 Multiempresa e empresa ativa](../../../processos/P01-multiempresa.md) · [P04 Número da amostra e proteção do ano](../../../processos/P04-numero-amostra-ano.md)

## Regras de negócio
- Por empresa (`ID_EMPRESAS`): cada empresa tem a sua lista; filtro global obrigatório.
- `ATIVO = 0` some dos combos de inclusão, mas continua aparecendo nos registros antigos que o usam.
- Excluir só se não houver uso (FK); havendo uso, oferecer "Inativar".
- `CODIGO` 1–99, único por empresa (`CK_AMOTIP_COD`).
- **`CODIGO` não pode mudar depois de usado em amostra** (o número impresso mudaria) — validar na tela/serviço.
- Na migração `CODIGO = ID antigo` para manter os números iguais.

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- Chave auto-incremento: `ID_AMOSTRAS_TIPO`.
- `TINYINT(1)` → `bool`: `ATIVO`.

## Definição (DDL)
```sql
CREATE TABLE AMOSTRAS_TIPO (
  ID_AMOSTRAS_TIPO INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS      INT          NOT NULL,
  CODIGO           SMALLINT     NOT NULL,
  DESCRICAO        VARCHAR(60)  NOT NULL,
  ATIVO            TINYINT(1)   NOT NULL DEFAULT 1,
  CONSTRAINT UQ_AMOTIP_COD UNIQUE (ID_EMPRESAS, CODIGO),
  CONSTRAINT UQ_AMOTIP_DESC UNIQUE (ID_EMPRESAS, DESCRICAO),
  CONSTRAINT CK_AMOTIP_COD CHECK (CODIGO BETWEEN 1 AND 99),
  CONSTRAINT FK_AMOTIP_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS)
) ENGINE=InnoDB;
```
