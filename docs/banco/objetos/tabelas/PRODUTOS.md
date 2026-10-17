# PRODUTOS

Produtos = nomes das amostras do cliente (ex.: "QUEIJO MINAS"). Usados na proposta, amostra, etiqueta e boletim.

**Grupo:** Cadastros · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-10

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `PRODUTOS` (só a aba Principal era usada; código por `SYS_TABELAS_ID`) | 1.915 |

Comentário do script: _5. PRODUTOS E PARÂMETROS DE ANÁLISE_

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_PRODUTOS` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 3 | `CODIGO` | VARCHAR(10) | não |  | trigger TG_PRODUTOS_BI | Gerado por SEQUENCIAS_DOCUMENTOS(PRODUTO) se vazio |
| 4 | `DESCRICAO` | VARCHAR(150) | não |  | aplicação |  |
| 5 | `ID_EMBALAGENS_TIPOS` | INT | sim |  | aplicação |  |
| 6 | `ID_UNIDADES` | INT | não |  | aplicação |  |
| 7 | `QTDE_EMBALAGEM` | DECIMAL(15,4) | sim |  | aplicação |  |
| 8 | `INATIVO` | TINYINT(1) | não | 0 | aplicação |  |
| 9 | `ID_LEGADO` | INT | sim |  | aplicação |  |
| 10 | `CRIADO_EM` | DATETIME | não | CURRENT_TIMESTAMP | banco |  |
| 11 | `CRIADO_POR` | INT | sim |  | aplicação |  |
| 12 | `ALTERADO_EM` | DATETIME | sim |  | banco |  |
| 13 | `ALTERADO_POR` | INT | sim |  | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_PRODUTOS`
- **Única** `UQ_PROD_COD` (ID_EMPRESAS, CODIGO)

- **Índice** `IX_PROD_DESC` (ID_EMPRESAS, DESCRICAO)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |
| `ID_EMBALAGENS_TIPOS` | [`EMBALAGENS_TIPOS`](../tabelas/EMBALAGENS_TIPOS.md) | ID_EMBALAGENS_TIPOS | bloqueia |
| `ID_UNIDADES` | [`UNIDADES`](../tabelas/UNIDADES.md) | ID_UNIDADES | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`LAB_PROPOSTAS_PRODUTOS`](../tabelas/LAB_PROPOSTAS_PRODUTOS.md) | ID_PRODUTOS | bloqueia |
| [`PEDIDOS_VENDA_ITENS`](../tabelas/PEDIDOS_VENDA_ITENS.md) | ID_PRODUTOS | bloqueia |
| [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) | ID_PRODUTOS | bloqueia |

## Triggers
- [`TG_PRODUTOS_BI`](../triggers/TG_PRODUTOS_BI.md) — BEFORE INSERT: Código automático do produto (6 dígitos) quando vazio.

## Usada por
- **Views:** [`VW_LAB_PROPOSTAS_ANALISES`](../views/VW_LAB_PROPOSTAS_ANALISES.md), [`VW_LAB_AMOSTRAS`](../views/VW_LAB_AMOSTRAS.md), [`VW_LAB_TESTES_PENDENTES`](../views/VW_LAB_TESTES_PENDENTES.md), [`VW_LAB_BOLETINS`](../views/VW_LAB_BOLETINS.md), [`VW_PEDIDOS_VENDA_SALDO`](../views/VW_PEDIDOS_VENDA_SALDO.md)
- **Rotinas que alteram:** —
- **Rotinas que leem:** [`SP_TROCAR_PRODUTO_PROPOSTA`](../rotinas/SP_TROCAR_PRODUTO_PROPOSTA.md)
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T14 Produtos](../../../telas/T14-produtos.md) · [T20 Propostas](../../../telas/T20-propostas.md)
- **Processos:** [P03 Numeração de documentos](../../../processos/P03-numeracao-documentos.md)

## Regras de negócio
- Código vazio → automático, 6 dígitos (`TG_PRODUTOS_BI` + `SP_PROXIMO_NUMERO('PRODUTO')`).
- Código único por empresa.
- Cadastro rápido "+" na proposta.

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- **Preenchidas pelo banco (trigger):** `CODIGO` — não depender do valor enviado; reler a linha após gravar.
- Chave auto-incremento: `ID_PRODUTOS`.
- `TINYINT(1)` → `bool`: `INATIVO`.
- Datas mantidas pelo MySQL (`DEFAULT/ON UPDATE CURRENT_TIMESTAMP`): `CRIADO_EM`, `ALTERADO_EM` — mapear como gerado pelo banco.
- `CRIADO_POR`/`ALTERADO_POR` = `ID_USUARIOS` logado (preencher no `SaveChanges`).
- Enviar `CODIGO = ""` para deixar o banco gerar; reler após o INSERT.

## Definição (DDL)
```sql
CREATE TABLE PRODUTOS (
  ID_PRODUTOS        INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS        INT          NOT NULL,
  CODIGO             VARCHAR(10)  NOT NULL COMMENT 'Gerado por SEQUENCIAS_DOCUMENTOS(PRODUTO) se vazio',
  DESCRICAO          VARCHAR(150) NOT NULL,
  ID_EMBALAGENS_TIPOS INT         NULL,
  ID_UNIDADES        INT          NOT NULL,
  QTDE_EMBALAGEM     DECIMAL(15,4) NULL,
  INATIVO            TINYINT(1)   NOT NULL DEFAULT 0,
  ID_LEGADO          INT          NULL,
  CRIADO_EM          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CRIADO_POR         INT          NULL,
  ALTERADO_EM        DATETIME     NULL ON UPDATE CURRENT_TIMESTAMP,
  ALTERADO_POR       INT          NULL,
  CONSTRAINT UQ_PROD_COD UNIQUE (ID_EMPRESAS, CODIGO),
  INDEX IX_PROD_DESC (ID_EMPRESAS, DESCRICAO),
  CONSTRAINT FK_PROD_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS),
  CONSTRAINT FK_PROD_EMB FOREIGN KEY (ID_EMBALAGENS_TIPOS) REFERENCES EMBALAGENS_TIPOS (ID_EMBALAGENS_TIPOS),
  CONSTRAINT FK_PROD_UNI FOREIGN KEY (ID_UNIDADES) REFERENCES UNIDADES (ID_UNIDADES)
) ENGINE=InnoDB;
```
