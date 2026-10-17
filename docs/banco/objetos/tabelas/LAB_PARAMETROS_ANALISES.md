# LAB_PARAMETROS_ANALISES

Catálogo de parâmetros de análise (o que o laboratório mede), com preço, método padrão e acreditação.

**Grupo:** Cadastros · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-10

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `LAB_PARAMETROS_ANALISES` | 462 (52 acreditados) |

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_LAB_PARAMETROS_ANALISES` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 3 | `DESCRICAO` | VARCHAR(150) | não |  | aplicação |  |
| 4 | `DESC_REDUZIDA` | VARCHAR(150) | não |  | aplicação | Texto impresso no boletim |
| 5 | `ID_ANALISES_TIPO` | INT | não |  | aplicação |  |
| 6 | `ID_ANALISES_METODOS` | INT | sim |  | aplicação | Método padrão (filtra o combo na proposta) |
| 7 | `DESCR_METODO` | VARCHAR(150) | sim |  | aplicação | Prefixo do texto de metodologia no boletim |
| 8 | `VR_UNITARIO` | DECIMAL(15,2) | não | 0 | aplicação |  |
| 9 | `AUDITADO` | TINYINT(1) | não | 0 | aplicação | 1 = Acreditado (INMETRO) |
| 10 | `INATIVO` | TINYINT(1) | não | 0 | aplicação |  |
| 11 | `ID_LEGADO` | INT | sim |  | aplicação |  |
| 12 | `CRIADO_EM` | DATETIME | não | CURRENT_TIMESTAMP | banco |  |
| 13 | `ALTERADO_EM` | DATETIME | sim |  | banco |  |

## Chaves, restrições e índices
- **PK:** `ID_LAB_PARAMETROS_ANALISES`
- **Única** `UQ_PARAM_DESC` (ID_EMPRESAS, DESCRICAO)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |
| `ID_ANALISES_TIPO` | [`ANALISES_TIPO`](../tabelas/ANALISES_TIPO.md) | ID_ANALISES_TIPO | bloqueia |
| `ID_ANALISES_METODOS` | [`ANALISES_METODOS`](../tabelas/ANALISES_METODOS.md) | ID_ANALISES_METODOS | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`LAB_PROPOSTAS_PRODANALISES`](../tabelas/LAB_PROPOSTAS_PRODANALISES.md) | ID_LAB_PARAMETROS_ANALISES | bloqueia |
| [`LAB_HIST_AMOSTRAS_TESTES`](../tabelas/LAB_HIST_AMOSTRAS_TESTES.md) | ID_LAB_PARAMETROS_ANALISES | bloqueia |

## Triggers
- —

## Usada por
- **Views:** [`VW_LAB_PROPOSTAS_ANALISES`](../views/VW_LAB_PROPOSTAS_ANALISES.md), [`VW_LAB_TESTES_PENDENTES`](../views/VW_LAB_TESTES_PENDENTES.md), [`VW_LAB_RESULTADOS`](../views/VW_LAB_RESULTADOS.md)
- **Rotinas que alteram:** —
- **Rotinas que leem:** [`FN_ACREDITADO`](../rotinas/FN_ACREDITADO.md), [`FN_OBS_METODOS`](../rotinas/FN_OBS_METODOS.md), [`SP_AMOSTRA_GERAR_TESTES`](../rotinas/SP_AMOSTRA_GERAR_TESTES.md), [`PR_REVISAO_BOLETIM`](../rotinas/PR_REVISAO_BOLETIM.md)
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T15 Parâmetros de análise](../../../telas/T15-parametros-analise.md) · [T20 Propostas](../../../telas/T20-propostas.md)
- **Processos:** [P16 Montagem do boletim](../../../processos/P16-montagem-boletim.md)

## Regras de negócio
- `DESC_REDUZIDA` obrigatória — é o texto impresso no boletim (opcional no legado causava erro na revisão do boletim).
- `AUDITADO = 1` (acreditado INMETRO) separa as metodologias "acreditado × não acreditado" e define `FN_ACREDITADO`.
- `ID_ANALISES_METODOS` filtra o combo de método na proposta; `VR_UNITARIO` é o preço sugerido.
- Descrição única por empresa.

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- Chave auto-incremento: `ID_LAB_PARAMETROS_ANALISES`.
- `TINYINT(1)` → `bool`: `AUDITADO`, `INATIVO`.
- Datas mantidas pelo MySQL (`DEFAULT/ON UPDATE CURRENT_TIMESTAMP`): `CRIADO_EM`, `ALTERADO_EM` — mapear como gerado pelo banco.

## Definição (DDL)
```sql
CREATE TABLE LAB_PARAMETROS_ANALISES (
  ID_LAB_PARAMETROS_ANALISES INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS        INT          NOT NULL,
  DESCRICAO          VARCHAR(150) NOT NULL,
  DESC_REDUZIDA      VARCHAR(150) NOT NULL COMMENT 'Texto impresso no boletim',
  ID_ANALISES_TIPO   INT          NOT NULL,
  ID_ANALISES_METODOS INT         NULL COMMENT 'Método padrão (filtra o combo na proposta)',
  DESCR_METODO       VARCHAR(150) NULL COMMENT 'Prefixo do texto de metodologia no boletim',
  VR_UNITARIO        DECIMAL(15,2) NOT NULL DEFAULT 0,
  AUDITADO           TINYINT(1)   NOT NULL DEFAULT 0 COMMENT '1 = Acreditado (INMETRO)',
  INATIVO            TINYINT(1)   NOT NULL DEFAULT 0,
  ID_LEGADO          INT          NULL,
  CRIADO_EM          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  ALTERADO_EM        DATETIME     NULL ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT UQ_PARAM_DESC UNIQUE (ID_EMPRESAS, DESCRICAO),
  CONSTRAINT FK_PARAM_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS),
  CONSTRAINT FK_PARAM_TIPO FOREIGN KEY (ID_ANALISES_TIPO) REFERENCES ANALISES_TIPO (ID_ANALISES_TIPO),
  CONSTRAINT FK_PARAM_MET FOREIGN KEY (ID_ANALISES_METODOS) REFERENCES ANALISES_METODOS (ID_ANALISES_METODOS)
) ENGINE=InnoDB;
```
