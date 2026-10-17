# CONDICOES_PAGTOS

Condições de pagamento da proposta/pedido.

**Grupo:** Cadastros auxiliares · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-03

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `CONDICOES_PAGTOS` (`INATIVO` invertido para `ATIVO`) | 112 |

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_CONDICOES_PAGTOS` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 3 | `CODIGO` | VARCHAR(10) | não |  | aplicação |  |
| 4 | `DESCRICAO` | VARCHAR(250) | não |  | aplicação |  |
| 5 | `ATIVO` | TINYINT(1) | não | 1 | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_CONDICOES_PAGTOS`
- **Única** `UQ_CONDPAG` (ID_EMPRESAS, CODIGO)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`CONDICOES_PAGTOS_PARC`](../tabelas/CONDICOES_PAGTOS_PARC.md) | ID_CONDICOES_PAGTOS | CASCADE |
| [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md) | ID_CONDICOES_PAGTOS | bloqueia |
| [`PEDIDOS_VENDA`](../tabelas/PEDIDOS_VENDA.md) | ID_CONDICOES_PAGTOS | bloqueia |

## Triggers
- —

## Usada por
- **Views:** [`VW_LAB_PROPOSTAS`](../views/VW_LAB_PROPOSTAS.md)
- **Rotinas que alteram:** —
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T10-11 Condições de pagamento](../../../telas/T10-11-condicoes-pagamento.md) · [T10-00 Padrão dos cadastros auxiliares](../../../telas/T10-00-crud-auxiliares.md)
- **Processos:** [P01 Multiempresa e empresa ativa](../../../processos/P01-multiempresa.md)

## Regras de negócio
- Por empresa (`ID_EMPRESAS`): cada empresa tem a sua lista; filtro global obrigatório.
- `ATIVO = 0` some dos combos de inclusão, mas continua aparecendo nos registros antigos que o usam.
- Excluir só se não houver uso (FK); havendo uso, oferecer "Inativar".
- Código obrigatório e único por empresa ("O Código da Condição de Pagamento é obrigatório.").
- A proposta lista só as ativas.

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- Chave auto-incremento: `ID_CONDICOES_PAGTOS`.
- `TINYINT(1)` → `bool`: `ATIVO`.

## Definição (DDL)
```sql
CREATE TABLE CONDICOES_PAGTOS (
  ID_CONDICOES_PAGTOS INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS      INT          NOT NULL,
  CODIGO           VARCHAR(10)  NOT NULL,
  DESCRICAO        VARCHAR(250) NOT NULL,
  ATIVO            TINYINT(1)   NOT NULL DEFAULT 1,
  CONSTRAINT UQ_CONDPAG UNIQUE (ID_EMPRESAS, CODIGO),
  CONSTRAINT FK_CONDPAG_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS)
) ENGINE=InnoDB;
```
