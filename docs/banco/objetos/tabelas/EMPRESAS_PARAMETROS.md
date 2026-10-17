# EMPRESAS_PARAMETROS

Parâmetros de funcionamento de cada empresa: status padrão, limites de dias, textos padrão.

**Grupo:** Núcleo global e segurança · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-04

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `EMPRESAS_PARAMETROS` (os `ULT_COD_*` foram para `SEQUENCIAS_DOCUMENTOS`) | 1 |

Comentário do script: _2. PARÂMETROS DA EMPRESA_

Notas do script: _FKs de status criadas no fim do script_

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 2 | `ID_LAB_PROPOSTAS_STATUS_INICIAL` | INT | sim |  | aplicação | Status da proposta nova (Em elaboração) |
| 3 | `ID_LAB_PROPOSTAS_STATUS_ENVCLI` | INT | sim |  | aplicação | Status que grava DT_ENVIO_CLIENTE |
| 4 | `ID_AMOSTRAS_STATUS_INICIAL` | INT | sim |  | aplicação | Status da amostra recebida (Liberada) |
| 5 | `ID_AMOSTRAS_STATUS_RESULTADOS` | INT | sim |  | aplicação | Status que permite lançar resultados |
| 6 | `ID_BOLETINS_STATUS_INICIAL` | INT | sim |  | aplicação |  |
| 7 | `ID_BOLETINS_STATUS_EMITIDO` | INT | sim |  | aplicação |  |
| 8 | `DIAS_RETROATIVOS_ENTREGA` | INT | não | 7 | aplicação | Máx. dias no passado para data de entrega da amostra |
| 9 | `DIAS_VALIDADE_PROPOSTA` | INT | não | 30 | aplicação |  |
| 10 | `DIAS_PESQUISA_PADRAO` | INT | não | 365 | aplicação |  |
| 11 | `EXIGE_RESP2_BOLETIM` | TINYINT(1) | não | 0 | aplicação |  |
| 12 | `TEXTO_PADRAO_LEGENDA` | TEXT | sim |  | aplicação |  |
| 13 | `TEXTO_PADRAO_CONTRATO` | MEDIUMTEXT | sim |  | aplicação |  |
| 14 | `ALTERADO_EM` | DATETIME | sim |  | banco |  |

## Chaves, restrições e índices
- **PK:** `ID_EMPRESAS`

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |
| `ID_LAB_PROPOSTAS_STATUS_INICIAL` | [`LAB_PROPOSTAS_STATUS`](../tabelas/LAB_PROPOSTAS_STATUS.md) | ID_LAB_PROPOSTAS_STATUS | bloqueia (FK adiada, fim do script) |
| `ID_LAB_PROPOSTAS_STATUS_ENVCLI` | [`LAB_PROPOSTAS_STATUS`](../tabelas/LAB_PROPOSTAS_STATUS.md) | ID_LAB_PROPOSTAS_STATUS | bloqueia (FK adiada, fim do script) |
| `ID_AMOSTRAS_STATUS_INICIAL` | [`AMOSTRAS_STATUS`](../tabelas/AMOSTRAS_STATUS.md) | ID_AMOSTRAS_STATUS | bloqueia (FK adiada, fim do script) |
| `ID_AMOSTRAS_STATUS_RESULTADOS` | [`AMOSTRAS_STATUS`](../tabelas/AMOSTRAS_STATUS.md) | ID_AMOSTRAS_STATUS | bloqueia (FK adiada, fim do script) |
| `ID_BOLETINS_STATUS_INICIAL` | [`BOLETINS_STATUS`](../tabelas/BOLETINS_STATUS.md) | ID_BOLETINS_STATUS | bloqueia (FK adiada, fim do script) |
| `ID_BOLETINS_STATUS_EMITIDO` | [`BOLETINS_STATUS`](../tabelas/BOLETINS_STATUS.md) | ID_BOLETINS_STATUS | bloqueia (FK adiada, fim do script) |

### É referenciada por
—

## Triggers
- —

## Usada por
- **Views:** —
- **Rotinas que alteram:** [`SP_INICIALIZAR_EMPRESA`](../rotinas/SP_INICIALIZAR_EMPRESA.md)
- **Rotinas que leem:** [`PR_REVISAO_PROPOSTA`](../rotinas/PR_REVISAO_PROPOSTA.md), [`PR_REVISAO_BOLETIM`](../rotinas/PR_REVISAO_BOLETIM.md), [`SP_EMITIR_BOLETIM`](../rotinas/SP_EMITIR_BOLETIM.md)
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** [`TG_PROPOSTAS_BU`](../triggers/TG_PROPOSTAS_BU.md), [`TG_AMOSTRAS_BI`](../triggers/TG_AMOSTRAS_BI.md)

## Telas e processos
- **Telas:** [T05 Parâmetros da empresa](../../../telas/T05-parametros-empresa.md)
- **Processos:** [P04 Número da amostra e proteção do ano](../../../processos/P04-numero-amostra-ano.md) · [P05 Ciclo de vida da proposta](../../../processos/P05-ciclo-proposta.md) · [P17 Emissão do boletim](../../../processos/P17-emissao-boletim.md) · [P23 Nova empresa e cópia de cadastros](../../../processos/P23-nova-empresa.md)

## Regras de negócio
- 1:1 com `EMPRESAS` (PK = `ID_EMPRESAS`), criada por `SP_INICIALIZAR_EMPRESA`.
- `DIAS_RETROATIVOS_ENTREGA` (padrão 7) é a proteção contra ano digitado errado na data de entrega da amostra (`TG_AMOSTRAS_BI`).
- `ID_LAB_PROPOSTAS_STATUS_ENVCLI`: ao mudar a proposta para esse status a trigger grava `DT_ENVIO_CLIENTE`.
- `ID_AMOSTRAS_STATUS_RESULTADOS`: legado do filtro da tela de Resultados/Etiquetas (no v2 vale `AMOSTRAS_STATUS.PERMITE_RESULTADO`).
- `EXIGE_RESP2_BOLETIM` é verificado por `SP_EMITIR_BOLETIM`.
- `DIAS_PESQUISA_PADRAO` (365) é o período inicial dos filtros de consulta (Delphi: `Data_Servidor - 365`).

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- `TINYINT(1)` → `bool`: `EXIGE_RESP2_BOLETIM`.
- Datas mantidas pelo MySQL (`DEFAULT/ON UPDATE CURRENT_TIMESTAMP`): `ALTERADO_EM` — mapear como gerado pelo banco.
- Ler uma vez por request (cache curto por empresa) — usado em várias telas.

## Definição (DDL)
```sql
CREATE TABLE EMPRESAS_PARAMETROS (
  ID_EMPRESAS                      INT          NOT NULL PRIMARY KEY,
  ID_LAB_PROPOSTAS_STATUS_INICIAL  INT          NULL COMMENT 'Status da proposta nova (Em elaboração)',
  ID_LAB_PROPOSTAS_STATUS_ENVCLI   INT          NULL COMMENT 'Status que grava DT_ENVIO_CLIENTE',
  ID_AMOSTRAS_STATUS_INICIAL       INT          NULL COMMENT 'Status da amostra recebida (Liberada)',
  ID_AMOSTRAS_STATUS_RESULTADOS    INT          NULL COMMENT 'Status que permite lançar resultados',
  ID_BOLETINS_STATUS_INICIAL       INT          NULL,
  ID_BOLETINS_STATUS_EMITIDO       INT          NULL,
  DIAS_RETROATIVOS_ENTREGA         INT          NOT NULL DEFAULT 7  COMMENT 'Máx. dias no passado para data de entrega da amostra',
  DIAS_VALIDADE_PROPOSTA           INT          NOT NULL DEFAULT 30,
  DIAS_PESQUISA_PADRAO             INT          NOT NULL DEFAULT 365,
  EXIGE_RESP2_BOLETIM              TINYINT(1)   NOT NULL DEFAULT 0,
  TEXTO_PADRAO_LEGENDA             TEXT         NULL,
  TEXTO_PADRAO_CONTRATO            MEDIUMTEXT   NULL,
  ALTERADO_EM                      DATETIME     NULL ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT FK_EMPPARAM_EMPRESAS FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS)
  -- FKs de status criadas no fim do script
) ENGINE=InnoDB;
```
