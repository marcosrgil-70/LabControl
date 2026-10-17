# SEQUENCIAS_DOCUMENTOS

Último número usado por (empresa, tipo de documento, ano). É a base da numeração segura de amostras, propostas, pedidos, produtos e relatórios de ensaio.

**Grupo:** Núcleo global e segurança · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-18

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `EMPRESAS_PARAMETROS.ULT_COD_AMOSTRA/ULT_COD_PROPOSTA/ULT_PED_VENDA/ULT_COD_BOLETIM/ULT_COD_RE` + `SYS_TABELAS_ID` | — (criada na migração, M-18) |

Comentário do script: _Numeração de documentos por empresa/tipo/ano (substitui EMPRESAS_PARAMETROS.ULT_*, SYS_TABELAS_ID e os SP_GET_COD_*). ANO = 0 para numeração não anual._

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 2 | `TIPO_DOCUMENTO` | VARCHAR(30) | não |  | aplicação | AMOSTRA, PROPOSTA, PEDIDO_VENDA, RELATORIO_ENSAIO, PRODUTO |
| 3 | `ANO` | SMALLINT | não | 0 | aplicação |  |
| 4 | `ULTIMO_NUMERO` | INT | não | 0 | aplicação |  |
| 5 | `ALTERADO_EM` | DATETIME | não | CURRENT_TIMESTAMP | banco |  |

## Chaves, restrições e índices
- **PK:** `ID_EMPRESAS`, `TIPO_DOCUMENTO`, `ANO`

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |

### É referenciada por
—

## Triggers
- —

## Usada por
- **Views:** —
- **Rotinas que alteram:** [`SP_PROXIMO_NUMERO`](../rotinas/SP_PROXIMO_NUMERO.md)
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T05 Parâmetros da empresa](../../../telas/T05-parametros-empresa.md)
- **Processos:** [P03 Numeração de documentos](../../../processos/P03-numeracao-documentos.md) · [P04 Número da amostra e proteção do ano](../../../processos/P04-numero-amostra-ano.md)

## Regras de negócio
- Só é alterada por `SP_PROXIMO_NUMERO` (com `SELECT … FOR UPDATE`).
- Tipos: `AMOSTRA`, `PROPOSTA`, `RELATORIO_ENSAIO` (anuais) e `PEDIDO_VENDA`, `PRODUTO` (`ANO = 0`).
- Nunca é "zerada": o ano novo cria uma linha nova começando em 1.
- Ajuste manual só por ADMIN_GLOBAL, nunca para um número menor que o máximo já usado, com motivo em `AUDITORIA` (tela T05).

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- Datas mantidas pelo MySQL (`DEFAULT/ON UPDATE CURRENT_TIMESTAMP`): `ALTERADO_EM` — mapear como gerado pelo banco.
- A aplicação **não** grava nesta tabela (exceto o ajuste auditado da T05).

## Definição (DDL)
```sql
CREATE TABLE SEQUENCIAS_DOCUMENTOS (
  ID_EMPRESAS      INT          NOT NULL,
  TIPO_DOCUMENTO   VARCHAR(30)  NOT NULL COMMENT 'AMOSTRA, PROPOSTA, PEDIDO_VENDA, RELATORIO_ENSAIO, PRODUTO',
  ANO              SMALLINT     NOT NULL DEFAULT 0,
  ULTIMO_NUMERO    INT          NOT NULL DEFAULT 0,
  ALTERADO_EM      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (ID_EMPRESAS, TIPO_DOCUMENTO, ANO),
  CONSTRAINT FK_SEQ_EMPRESAS FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS)
) ENGINE=InnoDB;
```
