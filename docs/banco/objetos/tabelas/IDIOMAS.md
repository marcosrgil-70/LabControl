# IDIOMAS

Idioma do laudo de cada análise da proposta.

**Grupo:** Cadastros auxiliares · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-03

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `IDIOMAS` | 4 |

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_IDIOMAS` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 3 | `DESCRICAO` | VARCHAR(40) | não |  | aplicação |  |
| 4 | `ATIVO` | TINYINT(1) | não | 1 | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_IDIOMAS`
- **Única** `UQ_IDIOMA` (ID_EMPRESAS, DESCRICAO)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`LAB_PROPOSTAS_PRODANALISES`](../tabelas/LAB_PROPOSTAS_PRODANALISES.md) | ID_IDIOMAS | bloqueia |
| [`LAB_HIST_AMOSTRAS_TESTES`](../tabelas/LAB_HIST_AMOSTRAS_TESTES.md) | ID_IDIOMAS | bloqueia |

## Triggers
- —

## Usada por
- **Views:** [`VW_LAB_PROPOSTAS_ANALISES`](../views/VW_LAB_PROPOSTAS_ANALISES.md)
- **Rotinas que alteram:** [`SP_INICIALIZAR_EMPRESA`](../rotinas/SP_INICIALIZAR_EMPRESA.md)
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T10-07 Idiomas](../../../telas/T10-07-idiomas.md) · [T10-00 Padrão dos cadastros auxiliares](../../../telas/T10-00-crud-auxiliares.md)
- **Processos:** [P01 Multiempresa e empresa ativa](../../../processos/P01-multiempresa.md)

## Regras de negócio
- Por empresa (`ID_EMPRESAS`): cada empresa tem a sua lista; filtro global obrigatório.
- `ATIVO = 0` some dos combos de inclusão, mas continua aparecendo nos registros antigos que o usam.
- Excluir só se não houver uso (FK); havendo uso, oferecer "Inativar".
- Cadastro rápido "+" na proposta.

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- Chave auto-incremento: `ID_IDIOMAS`.
- `TINYINT(1)` → `bool`: `ATIVO`.

## Definição (DDL)
```sql
CREATE TABLE IDIOMAS (
  ID_IDIOMAS       INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS      INT          NOT NULL,
  DESCRICAO        VARCHAR(40)  NOT NULL,
  ATIVO            TINYINT(1)   NOT NULL DEFAULT 1,
  CONSTRAINT UQ_IDIOMA UNIQUE (ID_EMPRESAS, DESCRICAO),
  CONSTRAINT FK_IDIOMA_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS)
) ENGINE=InnoDB;
```
