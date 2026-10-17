# LAB_TIPOS_RESULTADOS

Resultados descritivos ("Ausência em 25 g", "Satisfatório"…).

**Grupo:** Cadastros auxiliares · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-03

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `LAB_TIPOS_RESULTADOS` | 61 |

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_LAB_TIPOS_RESULTADOS` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 3 | `DESCRICAO` | VARCHAR(80) | não |  | aplicação | Ex.: Ausência em 25 g, Satisfatório |
| 4 | `ATIVO` | TINYINT(1) | não | 1 | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_LAB_TIPOS_RESULTADOS`
- **Única** `UQ_TIPRES` (ID_EMPRESAS, DESCRICAO)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`LAB_RESULTADOS_PARAM`](../tabelas/LAB_RESULTADOS_PARAM.md) | ID_LAB_TIPOS_RESULTADOS | bloqueia |
| [`LAB_RESULTADOS_PARAM`](../tabelas/LAB_RESULTADOS_PARAM.md) | ID_LAB_TIPO_RESULTADOS_ESP | bloqueia |

## Triggers
- —

## Usada por
- **Views:** —
- **Rotinas que alteram:** —
- **Rotinas que leem:** [`FN_FORMATA_RESULTADO`](../rotinas/FN_FORMATA_RESULTADO.md)
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T10-10 Tipos de resultado](../../../telas/T10-10-tipos-resultado.md) · [T10-00 Padrão dos cadastros auxiliares](../../../telas/T10-00-crud-auxiliares.md)
- **Processos:** [P01 Multiempresa e empresa ativa](../../../processos/P01-multiempresa.md)

## Regras de negócio
- Por empresa (`ID_EMPRESAS`): cada empresa tem a sua lista; filtro global obrigatório.
- `ATIVO = 0` some dos combos de inclusão, mas continua aparecendo nos registros antigos que o usam.
- Excluir só se não houver uso (FK); havendo uso, oferecer "Inativar".
- Texto impresso no boletim quando o resultado é descritivo.
- Cadastro rápido na tela de resultados (tecla Insert no legado).

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- Chave auto-incremento: `ID_LAB_TIPOS_RESULTADOS`.
- `TINYINT(1)` → `bool`: `ATIVO`.

## Definição (DDL)
```sql
CREATE TABLE LAB_TIPOS_RESULTADOS (
  ID_LAB_TIPOS_RESULTADOS INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS      INT          NOT NULL,
  DESCRICAO        VARCHAR(80)  NOT NULL COMMENT 'Ex.: Ausência em 25 g, Satisfatório',
  ATIVO            TINYINT(1)   NOT NULL DEFAULT 1,
  CONSTRAINT UQ_TIPRES UNIQUE (ID_EMPRESAS, DESCRICAO),
  CONSTRAINT FK_TIPRES_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS)
) ENGINE=InnoDB;
```
