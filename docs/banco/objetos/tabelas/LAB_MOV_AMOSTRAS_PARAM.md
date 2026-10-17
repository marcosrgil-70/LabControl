# LAB_MOV_AMOSTRAS_PARAM

Parâmetros (testes) atendidos por uma saída de amostra.

**Grupo:** Amostras e estoque · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-15

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `LAB_MOV_AMOSTRAS_PARAM` | 0 |

Comentário do script: _Parâmetros (testes) a que se destina uma saída de amostra_

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_LAB_MOV_AMOSTRAS_PARAM` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 3 | `ID_LAB_MOV_AMOSTRAS` | INT | não |  | aplicação |  |
| 4 | `ID_LAB_HIST_AMOSTRAS_TESTES` | INT | não |  | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_LAB_MOV_AMOSTRAS_PARAM`
- **Única** `UQ_MOVPAR` (ID_LAB_MOV_AMOSTRAS, ID_LAB_HIST_AMOSTRAS_TESTES)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |
| `ID_LAB_MOV_AMOSTRAS` | [`LAB_MOV_AMOSTRAS`](../tabelas/LAB_MOV_AMOSTRAS.md) | ID_LAB_MOV_AMOSTRAS | CASCADE |
| `ID_LAB_HIST_AMOSTRAS_TESTES` | [`LAB_HIST_AMOSTRAS_TESTES`](../tabelas/LAB_HIST_AMOSTRAS_TESTES.md) | ID_LAB_HIST_AMOSTRAS_TESTES | bloqueia |

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
- **Telas:** [T23 Movimentação de amostras](../../../telas/T23-movimentacao-amostras.md)
- **Processos:** [P12 Estoque e saldo das amostras](../../../processos/P12-estoque-amostras.md)

## Regras de negócio
- Um teste uma vez por movimento (`UQ_MOVPAR`).

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- Chave auto-incremento: `ID_LAB_MOV_AMOSTRAS_PARAM`.

## Definição (DDL)
```sql
CREATE TABLE LAB_MOV_AMOSTRAS_PARAM (
  ID_LAB_MOV_AMOSTRAS_PARAM   INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS                 INT NOT NULL,
  ID_LAB_MOV_AMOSTRAS         INT NOT NULL,
  ID_LAB_HIST_AMOSTRAS_TESTES INT NOT NULL,
  CONSTRAINT UQ_MOVPAR UNIQUE (ID_LAB_MOV_AMOSTRAS, ID_LAB_HIST_AMOSTRAS_TESTES),
  CONSTRAINT FK_MOVPAR_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS),
  CONSTRAINT FK_MOVPAR_MOV FOREIGN KEY (ID_LAB_MOV_AMOSTRAS) REFERENCES LAB_MOV_AMOSTRAS (ID_LAB_MOV_AMOSTRAS) ON DELETE CASCADE,
  CONSTRAINT FK_MOVPAR_TESTE FOREIGN KEY (ID_LAB_HIST_AMOSTRAS_TESTES) REFERENCES LAB_HIST_AMOSTRAS_TESTES (ID_LAB_HIST_AMOSTRAS_TESTES)
) ENGINE=InnoDB;
```
