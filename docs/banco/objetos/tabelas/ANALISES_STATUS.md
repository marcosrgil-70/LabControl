# ANALISES_STATUS

Status de cada teste da amostra (Aguardando, Em análise, Concluída).

**Grupo:** Cadastros auxiliares · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-03

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `ANALISES_STATUS` | 3 |

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_ANALISES_STATUS` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 3 | `DESCRICAO` | VARCHAR(60) | não |  | aplicação |  |
| 4 | `COR` | VARCHAR(7) | sim |  | aplicação |  |
| 5 | `ATIVO` | TINYINT(1) | não | 1 | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_ANALISES_STATUS`
- **Única** `UQ_ANASTA` (ID_EMPRESAS, DESCRICAO)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`LAB_HIST_AMOSTRAS_TESTES`](../tabelas/LAB_HIST_AMOSTRAS_TESTES.md) | ID_ANALISES_STATUS | bloqueia |

## Triggers
- —

## Usada por
- **Views:** —
- **Rotinas que alteram:** [`SP_INICIALIZAR_EMPRESA`](../rotinas/SP_INICIALIZAR_EMPRESA.md)
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T10-17 Status da análise](../../../telas/T10-17-status-analise.md) · [T10-00 Padrão dos cadastros auxiliares](../../../telas/T10-00-crud-auxiliares.md)
- **Processos:** [P01 Multiempresa e empresa ativa](../../../processos/P01-multiempresa.md)

## Regras de negócio
- Por empresa (`ID_EMPRESAS`): cada empresa tem a sua lista; filtro global obrigatório.
- `ATIVO = 0` some dos combos de inclusão, mas continua aparecendo nos registros antigos que o usam.
- Excluir só se não houver uso (FK); havendo uso, oferecer "Inativar".

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- Chave auto-incremento: `ID_ANALISES_STATUS`.
- `TINYINT(1)` → `bool`: `ATIVO`.

## Definição (DDL)
```sql
CREATE TABLE ANALISES_STATUS (
  ID_ANALISES_STATUS INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS      INT          NOT NULL,
  DESCRICAO        VARCHAR(60)  NOT NULL,
  COR              VARCHAR(7)   NULL,
  ATIVO            TINYINT(1)   NOT NULL DEFAULT 1,
  CONSTRAINT UQ_ANASTA UNIQUE (ID_EMPRESAS, DESCRICAO),
  CONSTRAINT FK_ANASTA_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS)
) ENGINE=InnoDB;
```
