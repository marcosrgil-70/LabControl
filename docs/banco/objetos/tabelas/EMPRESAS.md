# EMPRESAS

Empresas usuárias do sistema (o laboratório). Dados do cabeçalho do laudo, logo e nº de acreditação.

**Grupo:** Núcleo global e segurança · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-01

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `EMPRESAS` + entidade marcada como `TIPO_EMPRESA_USUARIA` (`ENTIDADES`/`ENTIDADES_PJ`/endereço) | 1 |

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_EMPRESAS` | INT | não |  | auto-incremento |  |
| 2 | `COD_EMPRESA` | VARCHAR(3) | não |  | aplicação |  |
| 3 | `RAZAO_SOCIAL` | VARCHAR(150) | não |  | aplicação |  |
| 4 | `NOME_FANTASIA` | VARCHAR(150) | sim |  | aplicação |  |
| 5 | `CNPJ` | VARCHAR(18) | sim |  | aplicação |  |
| 6 | `INSC_ESTADUAL` | VARCHAR(20) | sim |  | aplicação |  |
| 7 | `INSC_MUNICIPAL` | VARCHAR(20) | sim |  | aplicação |  |
| 8 | `LOGRADOURO` | VARCHAR(150) | sim |  | aplicação |  |
| 9 | `NUMERO` | VARCHAR(20) | sim |  | aplicação |  |
| 10 | `COMPLEMENTO` | VARCHAR(50) | sim |  | aplicação |  |
| 11 | `BAIRRO` | VARCHAR(80) | sim |  | aplicação |  |
| 12 | `CEP` | VARCHAR(10) | sim |  | aplicação |  |
| 13 | `ID_CIDADES` | INT | sim |  | aplicação |  |
| 14 | `DDD` | VARCHAR(3) | sim |  | aplicação |  |
| 15 | `FONE` | VARCHAR(20) | sim |  | aplicação |  |
| 16 | `EMAIL` | VARCHAR(150) | sim |  | aplicação |  |
| 17 | `SITE` | VARCHAR(150) | sim |  | aplicação |  |
| 18 | `LOGO` | MEDIUMBLOB | sim |  | aplicação |  |
| 19 | `LOGO_MIME` | VARCHAR(50) | sim |  | aplicação |  |
| 20 | `CABECALHO_LAUDO` | TEXT | sim |  | aplicação |  |
| 21 | `RODAPE_LAUDO` | TEXT | sim |  | aplicação |  |
| 22 | `NR_ACREDITACAO` | VARCHAR(50) | sim |  | aplicação | Ex.: CRL INMETRO — exibido no boletim acreditado |
| 23 | `ATIVA` | TINYINT(1) | não | 1 | aplicação |  |
| 24 | `CRIADO_EM` | DATETIME | não | CURRENT_TIMESTAMP | banco |  |
| 25 | `ALTERADO_EM` | DATETIME | sim |  | banco |  |

## Chaves, restrições e índices
- **PK:** `ID_EMPRESAS`
- **Única** `UQ_EMPRESAS_COD` (COD_EMPRESA)
- **Única** `UQ_EMPRESAS_CNPJ` (CNPJ)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_CIDADES` | [`CIDADES`](../tabelas/CIDADES.md) | ID_CIDADES | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`SEQUENCIAS_DOCUMENTOS`](../tabelas/SEQUENCIAS_DOCUMENTOS.md) | ID_EMPRESAS | bloqueia |
| [`USUARIOS`](../tabelas/USUARIOS.md) | ID_EMPRESAS_ULTIMA | bloqueia |
| [`USUARIOS_EMPRESAS`](../tabelas/USUARIOS_EMPRESAS.md) | ID_EMPRESAS | bloqueia |
| [`PERFIS`](../tabelas/PERFIS.md) | ID_EMPRESAS | bloqueia |
| [`EMPRESAS_PARAMETROS`](../tabelas/EMPRESAS_PARAMETROS.md) | ID_EMPRESAS | bloqueia |
| [`TIPOS_LOGRADOURO`](../tabelas/TIPOS_LOGRADOURO.md) | ID_EMPRESAS | bloqueia |
| [`ENDERECOS_TIPOS`](../tabelas/ENDERECOS_TIPOS.md) | ID_EMPRESAS | bloqueia |
| [`FONES_TIPOS`](../tabelas/FONES_TIPOS.md) | ID_EMPRESAS | bloqueia |
| [`EMAILS_TIPOS`](../tabelas/EMAILS_TIPOS.md) | ID_EMPRESAS | bloqueia |
| [`TIPOS_REG_PROFISSIONAL`](../tabelas/TIPOS_REG_PROFISSIONAL.md) | ID_EMPRESAS | bloqueia |
| [`CARGOS_FUNCIONARIOS`](../tabelas/CARGOS_FUNCIONARIOS.md) | ID_EMPRESAS | bloqueia |
| [`IDIOMAS`](../tabelas/IDIOMAS.md) | ID_EMPRESAS | bloqueia |
| [`UNIDADES`](../tabelas/UNIDADES.md) | ID_EMPRESAS | bloqueia |
| [`EMBALAGENS_TIPOS`](../tabelas/EMBALAGENS_TIPOS.md) | ID_EMPRESAS | bloqueia |
| [`LAB_TIPOS_RESULTADOS`](../tabelas/LAB_TIPOS_RESULTADOS.md) | ID_EMPRESAS | bloqueia |
| [`CONDICOES_PAGTOS`](../tabelas/CONDICOES_PAGTOS.md) | ID_EMPRESAS | bloqueia |
| [`PRAZOS`](../tabelas/PRAZOS.md) | ID_EMPRESAS | bloqueia |
| [`MOEDAS`](../tabelas/MOEDAS.md) | ID_EMPRESAS | bloqueia |
| [`AMOSTRAS_TIPO`](../tabelas/AMOSTRAS_TIPO.md) | ID_EMPRESAS | bloqueia |
| [`AMOSTRAS_STATUS`](../tabelas/AMOSTRAS_STATUS.md) | ID_EMPRESAS | bloqueia |
| [`ANALISES_TIPO`](../tabelas/ANALISES_TIPO.md) | ID_EMPRESAS | bloqueia |
| [`ANALISES_STATUS`](../tabelas/ANALISES_STATUS.md) | ID_EMPRESAS | bloqueia |
| [`ANALISES_METODOS`](../tabelas/ANALISES_METODOS.md) | ID_EMPRESAS | bloqueia |
| [`BOLETINS_STATUS`](../tabelas/BOLETINS_STATUS.md) | ID_EMPRESAS | bloqueia |
| [`LAB_PROPOSTAS_STATUS`](../tabelas/LAB_PROPOSTAS_STATUS.md) | ID_EMPRESAS | bloqueia |
| [`ENTIDADES`](../tabelas/ENTIDADES.md) | ID_EMPRESAS | bloqueia |
| [`ENTIDADES_ENDERECOS`](../tabelas/ENTIDADES_ENDERECOS.md) | ID_EMPRESAS | bloqueia |
| [`ENTIDADES_FONES`](../tabelas/ENTIDADES_FONES.md) | ID_EMPRESAS | bloqueia |
| [`ENTIDADES_EMAILS`](../tabelas/ENTIDADES_EMAILS.md) | ID_EMPRESAS | bloqueia |
| [`ENTIDADES_FUNCIONARIOS`](../tabelas/ENTIDADES_FUNCIONARIOS.md) | ID_EMPRESAS | bloqueia |
| [`PRODUTOS`](../tabelas/PRODUTOS.md) | ID_EMPRESAS | bloqueia |
| [`LAB_PARAMETROS_ANALISES`](../tabelas/LAB_PARAMETROS_ANALISES.md) | ID_EMPRESAS | bloqueia |
| [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md) | ID_EMPRESAS | bloqueia |
| [`LAB_PROPOSTAS_TEXTOS`](../tabelas/LAB_PROPOSTAS_TEXTOS.md) | ID_EMPRESAS | bloqueia |
| [`LAB_PROPOSTAS_PRODUTOS`](../tabelas/LAB_PROPOSTAS_PRODUTOS.md) | ID_EMPRESAS | bloqueia |
| [`LAB_PROPOSTAS_PRODANALISES`](../tabelas/LAB_PROPOSTAS_PRODANALISES.md) | ID_EMPRESAS | bloqueia |
| [`PEDIDOS_VENDA`](../tabelas/PEDIDOS_VENDA.md) | ID_EMPRESAS | bloqueia |
| [`PEDIDOS_VENDA_ITENS`](../tabelas/PEDIDOS_VENDA_ITENS.md) | ID_EMPRESAS | bloqueia |
| [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) | ID_EMPRESAS | bloqueia |
| [`LAB_HIST_AMOSTRAS_TESTES`](../tabelas/LAB_HIST_AMOSTRAS_TESTES.md) | ID_EMPRESAS | bloqueia |
| [`LAB_MOV_AMOSTRAS`](../tabelas/LAB_MOV_AMOSTRAS.md) | ID_EMPRESAS | bloqueia |
| [`LAB_MOV_AMOSTRAS_PARAM`](../tabelas/LAB_MOV_AMOSTRAS_PARAM.md) | ID_EMPRESAS | bloqueia |
| [`LAB_HIST_AMOSTRAS_SALDO`](../tabelas/LAB_HIST_AMOSTRAS_SALDO.md) | ID_EMPRESAS | bloqueia |
| [`LAB_LOCAL_AMOSTRAS`](../tabelas/LAB_LOCAL_AMOSTRAS.md) | ID_EMPRESAS | bloqueia |
| [`LAB_RESULTADOS_ANALISES`](../tabelas/LAB_RESULTADOS_ANALISES.md) | ID_EMPRESAS | bloqueia |
| [`LAB_RESULTADOS_PARAM`](../tabelas/LAB_RESULTADOS_PARAM.md) | ID_EMPRESAS | bloqueia |
| [`LAB_BOLETIM_ANALISES`](../tabelas/LAB_BOLETIM_ANALISES.md) | ID_EMPRESAS | bloqueia |
| [`LAB_BOLETIM_PARAMETROS`](../tabelas/LAB_BOLETIM_PARAMETROS.md) | ID_EMPRESAS | bloqueia |
| [`LAB_RELATORIO_ENSAIO`](../tabelas/LAB_RELATORIO_ENSAIO.md) | ID_EMPRESAS | bloqueia |

## Triggers
- —

## Usada por
- **Views:** —
- **Rotinas que alteram:** —
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T03 Empresa usuária](../../../telas/T03-empresa-usuaria.md) · [T00 Login e seleção de empresa](../../../telas/T00-login-empresa.md)
- **Processos:** [P01 Multiempresa e empresa ativa](../../../processos/P01-multiempresa.md) · [P23 Nova empresa e cópia de cadastros](../../../processos/P23-nova-empresa.md)

## Regras de negócio
- `COD_EMPRESA` (3) único e imutável; CNPJ único.
- Criar empresa = INSERT + `CALL SP_INICIALIZAR_EMPRESA(id)` + vincular o usuário criador com perfil ADMINISTRADOR.
- Logo (PNG/JPG até 1 MB) e `NR_ACREDITACAO` são impressos no boletim/proposta/pedido.

## Implementação no C# (EF Core)
- Chave auto-incremento: `ID_EMPRESAS`.
- `TINYINT(1)` → `bool`: `ATIVA`.
- Blob → `byte[]` carregado só quando necessário (não incluir nas listas): `LOGO`.
- Datas mantidas pelo MySQL (`DEFAULT/ON UPDATE CURRENT_TIMESTAMP`): `CRIADO_EM`, `ALTERADO_EM` — mapear como gerado pelo banco.
- Tabela global (não recebe o filtro de empresa); a lista de empresas só aparece para ADMIN_GLOBAL.

## Definição (DDL)
```sql
CREATE TABLE EMPRESAS (
  ID_EMPRESAS      INT AUTO_INCREMENT PRIMARY KEY,
  COD_EMPRESA      VARCHAR(3)   NOT NULL,
  RAZAO_SOCIAL     VARCHAR(150) NOT NULL,
  NOME_FANTASIA    VARCHAR(150) NULL,
  CNPJ             VARCHAR(18)  NULL,
  INSC_ESTADUAL    VARCHAR(20)  NULL,
  INSC_MUNICIPAL   VARCHAR(20)  NULL,
  LOGRADOURO       VARCHAR(150) NULL,
  NUMERO           VARCHAR(20)  NULL,
  COMPLEMENTO      VARCHAR(50)  NULL,
  BAIRRO           VARCHAR(80)  NULL,
  CEP              VARCHAR(10)  NULL,
  ID_CIDADES       INT          NULL,
  DDD              VARCHAR(3)   NULL,
  FONE             VARCHAR(20)  NULL,
  EMAIL            VARCHAR(150) NULL,
  SITE             VARCHAR(150) NULL,
  LOGO             MEDIUMBLOB   NULL,
  LOGO_MIME        VARCHAR(50)  NULL,
  CABECALHO_LAUDO  TEXT         NULL,
  RODAPE_LAUDO     TEXT         NULL,
  NR_ACREDITACAO   VARCHAR(50)  NULL COMMENT 'Ex.: CRL INMETRO — exibido no boletim acreditado',
  ATIVA            TINYINT(1)   NOT NULL DEFAULT 1,
  CRIADO_EM        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  ALTERADO_EM      DATETIME     NULL ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT UQ_EMPRESAS_COD UNIQUE (COD_EMPRESA),
  CONSTRAINT UQ_EMPRESAS_CNPJ UNIQUE (CNPJ),
  CONSTRAINT FK_EMPRESAS_CIDADES FOREIGN KEY (ID_CIDADES) REFERENCES CIDADES (ID_CIDADES)
) ENGINE=InnoDB;
```
