# ENTIDADES_FUNCIONARIOS

Dados de profissional (funcionário): registro, cargo, flags de assinatura/análise e imagem da assinatura.

**Grupo:** Cadastros · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-08

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `ENTIDADES_FUNCIONARIOS` + `ENTIDADES_FUNC_ASSINATURAS` (vazia) | 16 |

Comentário do script: _Profissionais (funcionários): registro profissional, cargo e assinatura do laudo._

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_ENTIDADES` | INT | não |  | aplicação |  |
| 2 | `ID_EMPRESAS` | INT | não |  | trigger TG_ENTFUN_BI |  |
| 3 | `ID_CARGOS_FUNCIONARIOS` | INT | sim |  | aplicação |  |
| 4 | `ID_TIPOS_REG_PROFISSIONAL` | INT | sim |  | aplicação |  |
| 5 | `NR_REGISTRO_PROFISSIONAL` | VARCHAR(25) | sim |  | aplicação |  |
| 6 | `UF_REGISTRO` | CHAR(2) | sim |  | aplicação |  |
| 7 | `ASSINA_BOLETIM` | TINYINT(1) | não | 0 | aplicação | Pode ser Responsável 1/2 do boletim |
| 8 | `EXECUTA_ANALISE` | TINYINT(1) | não | 1 | aplicação | Aparece como analista nos resultados |
| 9 | `ASSINATURA` | MEDIUMBLOB | sim |  | aplicação |  |
| 10 | `ASSINATURA_MIME` | VARCHAR(50) | sim |  | aplicação |  |
| 11 | `ASSINATURA_HASH` | CHAR(64) | sim |  | aplicação | SHA-256 da imagem |

## Chaves, restrições e índices
- **PK:** `ID_ENTIDADES`

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_ENTIDADES` | [`ENTIDADES`](../tabelas/ENTIDADES.md) | ID_ENTIDADES | CASCADE |
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |
| `ID_CARGOS_FUNCIONARIOS` | [`CARGOS_FUNCIONARIOS`](../tabelas/CARGOS_FUNCIONARIOS.md) | ID_CARGOS_FUNCIONARIOS | bloqueia |
| `ID_TIPOS_REG_PROFISSIONAL` | [`TIPOS_REG_PROFISSIONAL`](../tabelas/TIPOS_REG_PROFISSIONAL.md) | ID_TIPOS_REG_PROFISSIONAL | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`USUARIOS_EMPRESAS`](../tabelas/USUARIOS_EMPRESAS.md) | ID_ENTIDADES_FUNC | bloqueia |
| [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) | ID_ENTIDADES_FUNC_RESP | bloqueia |
| [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) | ID_ENTIDADES_FUNC_DIG | bloqueia |
| [`LAB_MOV_AMOSTRAS`](../tabelas/LAB_MOV_AMOSTRAS.md) | ID_ENTIDADES_FUNC | bloqueia |
| [`LAB_LOCAL_AMOSTRAS`](../tabelas/LAB_LOCAL_AMOSTRAS.md) | ID_ENTIDADES_FUNC_DESCARTE | bloqueia |
| [`LAB_RESULTADOS_ANALISES`](../tabelas/LAB_RESULTADOS_ANALISES.md) | ID_ENTIDADES_FUNC_DIG | bloqueia |
| [`LAB_RESULTADOS_PARAM`](../tabelas/LAB_RESULTADOS_PARAM.md) | ID_ENTIDADES_FUNC_ANALISE | bloqueia |
| [`LAB_RESULTADOS_PARAM`](../tabelas/LAB_RESULTADOS_PARAM.md) | ID_ENTIDADES_FUNC_DIG | bloqueia |
| [`LAB_BOLETIM_ANALISES`](../tabelas/LAB_BOLETIM_ANALISES.md) | ID_ENTIDADES_FUNC_GER | bloqueia |
| [`LAB_BOLETIM_ANALISES`](../tabelas/LAB_BOLETIM_ANALISES.md) | ID_ENTIDADES_FUNC_EMI | bloqueia |
| [`LAB_BOLETIM_ANALISES`](../tabelas/LAB_BOLETIM_ANALISES.md) | ID_ENTIDADES_FUNC_RESP | bloqueia |
| [`LAB_BOLETIM_ANALISES`](../tabelas/LAB_BOLETIM_ANALISES.md) | ID_ENTIDADES_FUNC_RESP2 | bloqueia |
| [`LAB_RELATORIO_ENSAIO`](../tabelas/LAB_RELATORIO_ENSAIO.md) | ID_ENTIDADES_FUNC_GER | bloqueia |
| [`LAB_RELATORIO_ENSAIO`](../tabelas/LAB_RELATORIO_ENSAIO.md) | ID_ENTIDADES_FUNC_EMI | bloqueia |

## Triggers
- [`TG_ENTFUN_BI`](../triggers/TG_ENTFUN_BI.md) — BEFORE INSERT: Profissional herda `ID_EMPRESAS` da entidade.

## Usada por
- **Views:** [`VW_PROFISSIONAIS`](../views/VW_PROFISSIONAIS.md)
- **Rotinas que alteram:** —
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T13 Profissionais](../../../telas/T13-profissionais.md) · [T27 Boletim](../../../telas/T27-boletim.md) · [T26 Resultados](../../../telas/T26-resultados.md) · [T22 Recebimento de amostras](../../../telas/T22-recebimento-amostras.md)
- **Processos:** [P02 Autenticação, sessão e permissões](../../../processos/P02-autenticacao-permissoes.md) · [P17 Emissão do boletim](../../../processos/P17-emissao-boletim.md)

## Regras de negócio
- `ASSINA_BOLETIM = 1` exige tipo/nº/UF de registro; só esses aparecem como Responsável 1/2 do boletim.
- `EXECUTA_ANALISE = 1` → aparece como analista nos resultados.
- Assinatura PNG até 300 KB, com SHA-256; impressa só no PDF **emitido**.
- É o alvo de todas as FKs de "profissional" (responsável, digitador, analista, gerador/emissor do boletim, descarte).

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- **Preenchidas pelo banco (trigger):** `ID_EMPRESAS` — não depender do valor enviado; reler a linha após gravar.
- `TINYINT(1)` → `bool`: `ASSINA_BOLETIM`, `EXECUTA_ANALISE`.
- Blob → `byte[]` carregado só quando necessário (não incluir nas listas): `ASSINATURA`.
- PK = `ID_ENTIDADES` (1:1 com `ENTIDADES`); `ID_EMPRESAS` herdado por `TG_ENTFUN_BI`.

## Definição (DDL)
```sql
CREATE TABLE ENTIDADES_FUNCIONARIOS (
  ID_ENTIDADES               INT          NOT NULL PRIMARY KEY,
  ID_EMPRESAS                INT          NOT NULL,
  ID_CARGOS_FUNCIONARIOS     INT          NULL,
  ID_TIPOS_REG_PROFISSIONAL  INT          NULL,
  NR_REGISTRO_PROFISSIONAL   VARCHAR(25)  NULL,
  UF_REGISTRO                CHAR(2)      NULL,
  ASSINA_BOLETIM             TINYINT(1)   NOT NULL DEFAULT 0 COMMENT 'Pode ser Responsável 1/2 do boletim',
  EXECUTA_ANALISE            TINYINT(1)   NOT NULL DEFAULT 1 COMMENT 'Aparece como analista nos resultados',
  ASSINATURA                 MEDIUMBLOB   NULL,
  ASSINATURA_MIME            VARCHAR(50)  NULL,
  ASSINATURA_HASH            CHAR(64)     NULL COMMENT 'SHA-256 da imagem',
  CONSTRAINT FK_ENTFUN_ENT FOREIGN KEY (ID_ENTIDADES) REFERENCES ENTIDADES (ID_ENTIDADES) ON DELETE CASCADE,
  CONSTRAINT FK_ENTFUN_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS),
  CONSTRAINT FK_ENTFUN_CARGO FOREIGN KEY (ID_CARGOS_FUNCIONARIOS) REFERENCES CARGOS_FUNCIONARIOS (ID_CARGOS_FUNCIONARIOS),
  CONSTRAINT FK_ENTFUN_TIPREG FOREIGN KEY (ID_TIPOS_REG_PROFISSIONAL) REFERENCES TIPOS_REG_PROFISSIONAL (ID_TIPOS_REG_PROFISSIONAL)
) ENGINE=InnoDB;
```
