# LAB_PROPOSTAS_TEXTOS

Textos longos da proposta (itens solicitados/adquiridos, observação, contrato, autorização) — 1:1 com a proposta.

**Grupo:** Propostas e pedidos · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-11

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `LAB_PROPOSTAS_CONTRATOS` (2.219), `_OBSERVACAO` (284), `_ITENS_SOL` (10), `_ITENS_ADQ` (12), `_AUTORIZACAO` (0) — RTF | ≈2.300 |

Comentário do script: _Une LAB_PROPOSTAS_ITENS_SOL, _ITENS_ADQ, _OBSERVACAO, _CONTRATOS e _AUTORIZACAO (1:1)_

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_LAB_PROPOSTAS` | INT | não |  | aplicação |  |
| 2 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 3 | `TX_ITENS_SOLICITADOS` | MEDIUMTEXT | sim |  | aplicação |  |
| 4 | `TX_ITENS_ADQUIRIDOS` | MEDIUMTEXT | sim |  | aplicação |  |
| 5 | `TX_OBSERVACAO` | MEDIUMTEXT | sim |  | aplicação |  |
| 6 | `TX_CONTRATO` | MEDIUMTEXT | sim |  | aplicação |  |
| 7 | `TX_AUTORIZACAO` | MEDIUMTEXT | sim |  | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_LAB_PROPOSTAS`

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_LAB_PROPOSTAS` | [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md) | ID_LAB_PROPOSTAS | CASCADE |
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |

### É referenciada por
—

## Triggers
- —

## Usada por
- **Views:** —
- **Rotinas que alteram:** [`PR_REVISAO_PROPOSTA`](../rotinas/PR_REVISAO_PROPOSTA.md)
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T20 Propostas](../../../telas/T20-propostas.md)
- **Processos:** [P07 Revisão da proposta](../../../processos/P07-revisao-proposta.md)

## Regras de negócio
- Legado em RTF; v2 em HTML (migração converte RTF → HTML/texto).
- Copiados na revisão (exceto autorização).

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- Owned entity / tabela 1:1 com PK = FK.

## Definição (DDL)
```sql
CREATE TABLE LAB_PROPOSTAS_TEXTOS (
  ID_LAB_PROPOSTAS       INT        NOT NULL PRIMARY KEY,
  ID_EMPRESAS            INT        NOT NULL,
  TX_ITENS_SOLICITADOS   MEDIUMTEXT NULL,
  TX_ITENS_ADQUIRIDOS    MEDIUMTEXT NULL,
  TX_OBSERVACAO          MEDIUMTEXT NULL,
  TX_CONTRATO            MEDIUMTEXT NULL,
  TX_AUTORIZACAO         MEDIUMTEXT NULL,
  CONSTRAINT FK_PROPTXT_PROP FOREIGN KEY (ID_LAB_PROPOSTAS) REFERENCES LAB_PROPOSTAS (ID_LAB_PROPOSTAS) ON DELETE CASCADE,
  CONSTRAINT FK_PROPTXT_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS)
) ENGINE=InnoDB;
```
