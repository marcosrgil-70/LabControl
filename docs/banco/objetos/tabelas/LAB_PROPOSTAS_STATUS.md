# LAB_PROPOSTAS_STATUS

Status da proposta, com cor e flag "permite receber amostra".

**Grupo:** Cadastros auxiliares · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-03

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `LAB_PROPOSTAS_STATUS` | 3 |

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_LAB_PROPOSTAS_STATUS` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 3 | `DESCRICAO` | VARCHAR(60) | não |  | aplicação |  |
| 4 | `COR` | VARCHAR(7) | sim |  | aplicação |  |
| 5 | `PERMITE_AMOSTRA` | TINYINT(1) | não | 1 | aplicação | Proposta neste status pode receber amostras |
| 6 | `ATIVO` | TINYINT(1) | não | 1 | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_LAB_PROPOSTAS_STATUS`
- **Única** `UQ_PROPSTA` (ID_EMPRESAS, DESCRICAO)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`EMPRESAS_PARAMETROS`](../tabelas/EMPRESAS_PARAMETROS.md) | ID_LAB_PROPOSTAS_STATUS_INICIAL | bloqueia |
| [`EMPRESAS_PARAMETROS`](../tabelas/EMPRESAS_PARAMETROS.md) | ID_LAB_PROPOSTAS_STATUS_ENVCLI | bloqueia |
| [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md) | ID_LAB_PROPOSTAS_STATUS | bloqueia |

## Triggers
- —

## Usada por
- **Views:** [`VW_LAB_PROPOSTAS`](../views/VW_LAB_PROPOSTAS.md)
- **Rotinas que alteram:** [`SP_INICIALIZAR_EMPRESA`](../rotinas/SP_INICIALIZAR_EMPRESA.md)
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** [`TG_AMOSTRAS_BI`](../triggers/TG_AMOSTRAS_BI.md)

## Telas e processos
- **Telas:** [T10-20 Status da proposta](../../../telas/T10-20-status-proposta.md) · [T10-00 Padrão dos cadastros auxiliares](../../../telas/T10-00-crud-auxiliares.md)
- **Processos:** [P01 Multiempresa e empresa ativa](../../../processos/P01-multiempresa.md)

## Regras de negócio
- Por empresa (`ID_EMPRESAS`): cada empresa tem a sua lista; filtro global obrigatório.
- `ATIVO = 0` some dos combos de inclusão, mas continua aparecendo nos registros antigos que o usam.
- Excluir só se não houver uso (FK); havendo uso, oferecer "Inativar".
- `PERMITE_AMOSTRA = 0` impede o recebimento de amostras para propostas nesse status (`TG_AMOSTRAS_BI`).

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- Chave auto-incremento: `ID_LAB_PROPOSTAS_STATUS`.
- `TINYINT(1)` → `bool`: `PERMITE_AMOSTRA`, `ATIVO`.

## Definição (DDL)
```sql
CREATE TABLE LAB_PROPOSTAS_STATUS (
  ID_LAB_PROPOSTAS_STATUS INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS      INT          NOT NULL,
  DESCRICAO        VARCHAR(60)  NOT NULL,
  COR              VARCHAR(7)   NULL,
  PERMITE_AMOSTRA  TINYINT(1)   NOT NULL DEFAULT 1 COMMENT 'Proposta neste status pode receber amostras',
  ATIVO            TINYINT(1)   NOT NULL DEFAULT 1,
  CONSTRAINT UQ_PROPSTA UNIQUE (ID_EMPRESAS, DESCRICAO),
  CONSTRAINT FK_PROPSTA_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS)
) ENGINE=InnoDB;
```
