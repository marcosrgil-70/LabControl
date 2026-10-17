# AMOSTRAS_STATUS

Status da amostra (Liberada, Bloqueada, Reprovada…), com cor e flag "permite resultado".

**Grupo:** Cadastros auxiliares · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-03

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `AMOSTRAS_STATUS` (cor `TColor`) | 4 |

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_AMOSTRAS_STATUS` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 3 | `DESCRICAO` | VARCHAR(60) | não |  | aplicação |  |
| 4 | `COR` | VARCHAR(7) | sim |  | aplicação | #RRGGBB |
| 5 | `PERMITE_RESULTADO` | TINYINT(1) | não | 0 | aplicação |  |
| 6 | `ATIVO` | TINYINT(1) | não | 1 | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_AMOSTRAS_STATUS`
- **Única** `UQ_AMOSTA` (ID_EMPRESAS, DESCRICAO)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`EMPRESAS_PARAMETROS`](../tabelas/EMPRESAS_PARAMETROS.md) | ID_AMOSTRAS_STATUS_INICIAL | bloqueia |
| [`EMPRESAS_PARAMETROS`](../tabelas/EMPRESAS_PARAMETROS.md) | ID_AMOSTRAS_STATUS_RESULTADOS | bloqueia |
| [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) | ID_AMOSTRAS_STATUS | bloqueia |

## Triggers
- —

## Usada por
- **Views:** [`VW_LAB_AMOSTRAS`](../views/VW_LAB_AMOSTRAS.md), [`VW_LAB_TESTES_PENDENTES`](../views/VW_LAB_TESTES_PENDENTES.md)
- **Rotinas que alteram:** [`SP_INICIALIZAR_EMPRESA`](../rotinas/SP_INICIALIZAR_EMPRESA.md)
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T10-15 Status da amostra](../../../telas/T10-15-status-amostra.md) · [T10-00 Padrão dos cadastros auxiliares](../../../telas/T10-00-crud-auxiliares.md)
- **Processos:** [P01 Multiempresa e empresa ativa](../../../processos/P01-multiempresa.md)

## Regras de negócio
- Por empresa (`ID_EMPRESAS`): cada empresa tem a sua lista; filtro global obrigatório.
- `ATIVO = 0` some dos combos de inclusão, mas continua aparecendo nos registros antigos que o usam.
- Excluir só se não houver uso (FK); havendo uso, oferecer "Inativar".
- `PERMITE_RESULTADO = 1` libera a amostra para Resultados/Boletim/fila de análises.
- Deve existir ao menos um status com "permite resultado".
- Cor em `#RRGGBB` (o legado guardava `TColor` BGR).

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- Chave auto-incremento: `ID_AMOSTRAS_STATUS`.
- `TINYINT(1)` → `bool`: `PERMITE_RESULTADO`, `ATIVO`.

## Definição (DDL)
```sql
CREATE TABLE AMOSTRAS_STATUS (
  ID_AMOSTRAS_STATUS INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS      INT          NOT NULL,
  DESCRICAO        VARCHAR(60)  NOT NULL,
  COR              VARCHAR(7)   NULL COMMENT '#RRGGBB',
  PERMITE_RESULTADO TINYINT(1)  NOT NULL DEFAULT 0,
  ATIVO            TINYINT(1)   NOT NULL DEFAULT 1,
  CONSTRAINT UQ_AMOSTA UNIQUE (ID_EMPRESAS, DESCRICAO),
  CONSTRAINT FK_AMOSTA_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS)
) ENGINE=InnoDB;
```
