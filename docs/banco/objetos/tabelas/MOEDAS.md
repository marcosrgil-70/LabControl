# MOEDAS

Moedas da proposta.

**Grupo:** Cadastros auxiliares · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-03

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `MOEDAS` | 2 |

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_MOEDAS` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 3 | `DESCRICAO` | VARCHAR(40) | não |  | aplicação |  |
| 4 | `SIGLA` | VARCHAR(10) | não |  | aplicação |  |
| 5 | `ATIVO` | TINYINT(1) | não | 1 | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_MOEDAS`
- **Única** `UQ_MOEDA` (ID_EMPRESAS, SIGLA)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md) | ID_MOEDAS | bloqueia |
| [`PEDIDOS_VENDA`](../tabelas/PEDIDOS_VENDA.md) | ID_MOEDAS | bloqueia |

## Triggers
- —

## Usada por
- **Views:** [`VW_LAB_PROPOSTAS`](../views/VW_LAB_PROPOSTAS.md)
- **Rotinas que alteram:** [`SP_INICIALIZAR_EMPRESA`](../rotinas/SP_INICIALIZAR_EMPRESA.md)
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T10-13 Moedas](../../../telas/T10-13-moedas.md) · [T10-00 Padrão dos cadastros auxiliares](../../../telas/T10-00-crud-auxiliares.md)
- **Processos:** [P01 Multiempresa e empresa ativa](../../../processos/P01-multiempresa.md)

## Regras de negócio
- Por empresa (`ID_EMPRESAS`): cada empresa tem a sua lista; filtro global obrigatório.
- `ATIVO = 0` some dos combos de inclusão, mas continua aparecendo nos registros antigos que o usam.
- Excluir só se não houver uso (FK); havendo uso, oferecer "Inativar".
- Sigla única por empresa.

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- Chave auto-incremento: `ID_MOEDAS`.
- `TINYINT(1)` → `bool`: `ATIVO`.

## Definição (DDL)
```sql
CREATE TABLE MOEDAS (
  ID_MOEDAS        INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS      INT          NOT NULL,
  DESCRICAO        VARCHAR(40)  NOT NULL,
  SIGLA            VARCHAR(10)  NOT NULL,
  ATIVO            TINYINT(1)   NOT NULL DEFAULT 1,
  CONSTRAINT UQ_MOEDA UNIQUE (ID_EMPRESAS, SIGLA),
  CONSTRAINT FK_MOEDA_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS)
) ENGINE=InnoDB;
```
