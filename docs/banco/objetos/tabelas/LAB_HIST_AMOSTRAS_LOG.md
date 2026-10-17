# LAB_HIST_AMOSTRAS_LOG

Log de inclusão/alteração/exclusão da amostra (JSON), gravado pelas triggers.

**Grupo:** Amostras e estoque · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-15 (opcional)

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `LAB_HIST_AMOSTRAS_LOG` (cópia das 45 colunas) | 10.509 |

Comentário do script: _Log de alterações da amostra (antiga LAB_HIST_AMOSTRAS_LOG com 45 colunas → JSON)_

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_LAB_HIST_AMOSTRAS_LOG` | BIGINT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 3 | `ID_LAB_HIST_AMOSTRAS` | INT | não |  | aplicação |  |
| 4 | `ACAO` | CHAR(1) | não |  | aplicação | I=Inclusão, A=Alteração, E=Exclusão |
| 5 | `DADOS` | JSON | não |  | aplicação |  |
| 6 | `ID_USUARIOS` | INT | sim |  | aplicação |  |
| 7 | `DT_LOG` | DATETIME(3) | não | CURRENT_TIMESTAMP(3) | banco |  |

## Chaves, restrições e índices
- **PK:** `ID_LAB_HIST_AMOSTRAS_LOG`

- **Índice** `IX_AMOLOG_AMO` (ID_LAB_HIST_AMOSTRAS, DT_LOG)

## Relacionamentos
### Referencia
—

### É referenciada por
—

## Triggers
- —

## Usada por
- **Views:** —
- **Rotinas que alteram:** —
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** [`TG_AMOSTRAS_AI`](../triggers/TG_AMOSTRAS_AI.md), [`TG_AMOSTRAS_AU`](../triggers/TG_AMOSTRAS_AU.md), [`TG_AMOSTRAS_AD`](../triggers/TG_AMOSTRAS_AD.md)
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T06 Auditoria](../../../telas/T06-auditoria.md) · [T22 Recebimento de amostras](../../../telas/T22-recebimento-amostras.md)
- **Processos:** [P21 Auditoria e log](../../../processos/P21-auditoria-log.md)

## Regras de negócio
- Gravado só por `TG_AMOSTRAS_AI/AU/AD`.
- `ID_USUARIOS` vem de `@LC_ID_USUARIOS` (o interceptor da conexão precisa definir).

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- Chave auto-incremento: `ID_LAB_HIST_AMOSTRAS_LOG`.
- `JSON` → `string` (ou `JsonDocument`): `DADOS`.
- Datas mantidas pelo MySQL (`DEFAULT/ON UPDATE CURRENT_TIMESTAMP`): `DT_LOG` — mapear como gerado pelo banco.
- Somente leitura na aplicação (aba "Histórico" da amostra / auditoria).

## Definição (DDL)
```sql
CREATE TABLE LAB_HIST_AMOSTRAS_LOG (
  ID_LAB_HIST_AMOSTRAS_LOG   BIGINT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS                INT          NOT NULL,
  ID_LAB_HIST_AMOSTRAS       INT          NOT NULL,
  ACAO                       CHAR(1)      NOT NULL COMMENT 'I=Inclusão, A=Alteração, E=Exclusão',
  DADOS                      JSON         NOT NULL,
  ID_USUARIOS                INT          NULL,
  DT_LOG                     DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  INDEX IX_AMOLOG_AMO (ID_LAB_HIST_AMOSTRAS, DT_LOG)
) ENGINE=InnoDB;
```
