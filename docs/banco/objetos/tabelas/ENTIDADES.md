# ENTIDADES

Pessoa física ou jurídica: clientes, fornecedores, profissionais (funcionários), comerciais e transportadores.

**Grupo:** Cadastros · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-05

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `ENTIDADES` + `ENTIDADES_PF` + `ENTIDADES_PJ` + `ENTIDADES_OBSERVACOES` | 527 (44 PF / 483 PJ) |

Comentário do script: _4. ENTIDADES (clientes, fornecedores, profissionais, comerciais)    ENTIDADES_PF e ENTIDADES_PJ do Firebird foram incorporadas aqui._

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_ENTIDADES` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | aplicação |  |
| 3 | `CATEGORIA` | ENUM('F','J') | não |  | aplicação | F = Física, J = Jurídica |
| 4 | `NOME` | VARCHAR(150) | não |  | aplicação | Nome (PF) ou Razão Social (PJ) |
| 5 | `NOME_FANTASIA` | VARCHAR(150) | sim |  | aplicação | PJ: Nome Fantasia \| PF: Sobrenome |
| 6 | `CPF_CNPJ` | VARCHAR(18) | sim |  | aplicação |  |
| 7 | `RG_IE` | VARCHAR(20) | sim |  | aplicação | PF: RG \| PJ: Inscrição Estadual |
| 8 | `INSC_MUNICIPAL` | VARCHAR(20) | sim |  | aplicação |  |
| 9 | `INSC_SUFRAMA` | VARCHAR(20) | sim |  | aplicação |  |
| 10 | `SEXO` | ENUM('M','F') | sim |  | aplicação |  |
| 11 | `DT_NASCIMENTO` | DATE | sim |  | aplicação |  |
| 12 | `ESTADO_CIVIL` | ENUM('S','C','D','P','V') | sim |  | aplicação | Solteiro, Casado, Divorciado, Separado, Viúvo |
| 13 | `TIPO_CLIENTE` | TINYINT(1) | não | 0 | aplicação |  |
| 14 | `TIPO_FORNECEDOR` | TINYINT(1) | não | 0 | aplicação |  |
| 15 | `TIPO_FUNCIONARIO` | TINYINT(1) | não | 0 | aplicação | Profissional do laboratório |
| 16 | `TIPO_VENDEDOR` | TINYINT(1) | não | 0 | aplicação | Comercial (ID_ENTIDADES_COMERC da proposta) |
| 17 | `TIPO_TRANSPORTADOR` | TINYINT(1) | não | 0 | aplicação |  |
| 18 | `AVALIACAO` | VARCHAR(20) | sim |  | aplicação |  |
| 19 | `OBSERVACAO` | TEXT | sim |  | aplicação |  |
| 20 | `OBS_INCLUIR_FATURAMENTO` | TINYINT(1) | não | 0 | aplicação |  |
| 21 | `DATA_CADASTRO` | DATE | não | (CURRENT_DATE) | banco |  |
| 22 | `INATIVO` | TINYINT(1) | não | 0 | aplicação |  |
| 23 | `ID_LEGADO` | INT | sim |  | aplicação | ID_ENTIDADES do Firebird |
| 24 | `CRIADO_EM` | DATETIME | não | CURRENT_TIMESTAMP | banco |  |
| 25 | `CRIADO_POR` | INT | sim |  | aplicação |  |
| 26 | `ALTERADO_EM` | DATETIME | sim |  | banco |  |
| 27 | `ALTERADO_POR` | INT | sim |  | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_ENTIDADES`
- **Única** `UQ_ENT_DOC` (ID_EMPRESAS, CPF_CNPJ)

- **Índice** `IX_ENT_NOME` (ID_EMPRESAS, NOME)
- **Índice** `IX_ENT_TIPOS` (ID_EMPRESAS, TIPO_CLIENTE, TIPO_FUNCIONARIO, INATIVO)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`ENTIDADES_ENDERECOS`](../tabelas/ENTIDADES_ENDERECOS.md) | ID_ENTIDADES | CASCADE |
| [`ENTIDADES_FONES`](../tabelas/ENTIDADES_FONES.md) | ID_ENTIDADES | CASCADE |
| [`ENTIDADES_EMAILS`](../tabelas/ENTIDADES_EMAILS.md) | ID_ENTIDADES | CASCADE |
| [`ENTIDADES_FUNCIONARIOS`](../tabelas/ENTIDADES_FUNCIONARIOS.md) | ID_ENTIDADES | CASCADE |
| [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md) | ID_ENTIDADES | bloqueia |
| [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md) | ID_ENTIDADES_COMERC | bloqueia |
| [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md) | ID_ENTIDADES_FUNC | bloqueia |
| [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md) | ID_ENT_COBRANCA | bloqueia |
| [`PEDIDOS_VENDA`](../tabelas/PEDIDOS_VENDA.md) | ID_ENTIDADES | bloqueia |
| [`PEDIDOS_VENDA`](../tabelas/PEDIDOS_VENDA.md) | ID_ENT_COBRANCA | bloqueia |
| [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) | ID_ENTIDADES | bloqueia |

## Triggers
- —

## Usada por
- **Views:** [`VW_CLIENTES`](../views/VW_CLIENTES.md), [`VW_PROFISSIONAIS`](../views/VW_PROFISSIONAIS.md), [`VW_LAB_PROPOSTAS`](../views/VW_LAB_PROPOSTAS.md), [`VW_LAB_AMOSTRAS`](../views/VW_LAB_AMOSTRAS.md), [`VW_LAB_TESTES_PENDENTES`](../views/VW_LAB_TESTES_PENDENTES.md), [`VW_LAB_RESULTADOS`](../views/VW_LAB_RESULTADOS.md), [`VW_LAB_BOLETINS`](../views/VW_LAB_BOLETINS.md)
- **Rotinas que alteram:** —
- **Rotinas que leem:** —
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** [`TG_ENTEND_BI`](../triggers/TG_ENTEND_BI.md), [`TG_ENTFON_BI`](../triggers/TG_ENTFON_BI.md), [`TG_ENTEML_BI`](../triggers/TG_ENTEML_BI.md), [`TG_ENTFUN_BI`](../triggers/TG_ENTFUN_BI.md)

## Telas e processos
- **Telas:** [T12 Clientes/fornecedores](../../../telas/T12-clientes.md) · [T13 Profissionais](../../../telas/T13-profissionais.md)
- **Processos:** [P01 Multiempresa e empresa ativa](../../../processos/P01-multiempresa.md)

## Regras de negócio
- `CPF_CNPJ` único por empresa, com dígito verificador ("Este CNPJ não é válido!", "CNPJ já cadastrado para: …").
- Ao menos um tipo marcado ("É obrigatório ter um Tipo de Cadastro preenchido.").
- PF: `NOME_FANTASIA` = sobrenome, `RG_IE` = RG. PJ: `NOME_FANTASIA` = fantasia, `RG_IE` = inscrição estadual (aceita "ISENTO").
- Trocar a categoria F↔J limpa os campos da outra categoria (o legado excluía o registro PF/PJ após confirmação).
- Excluir só sem movimento (proposta/amostra); senão inativar.

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- Chave auto-incremento: `ID_ENTIDADES`.
- `TINYINT(1)` → `bool`: `TIPO_CLIENTE`, `TIPO_FORNECEDOR`, `TIPO_FUNCIONARIO`, `TIPO_VENDEDOR`, `TIPO_TRANSPORTADOR`, `OBS_INCLUIR_FATURAMENTO`, `INATIVO`.
- `ENUM` → enum C# com conversão para string: `CATEGORIA` ('F','J'); `SEXO` ('M','F'); `ESTADO_CIVIL` ('S','C','D','P','V').
- `DATE` → `DateOnly`: `DT_NASCIMENTO`, `DATA_CADASTRO`.
- Datas mantidas pelo MySQL (`DEFAULT/ON UPDATE CURRENT_TIMESTAMP`): `CRIADO_EM`, `ALTERADO_EM` — mapear como gerado pelo banco.
- `CRIADO_POR`/`ALTERADO_POR` = `ID_USUARIOS` logado (preencher no `SaveChanges`).
- Uma entidade C# só (sem herança PF/PJ); validação de CPF/CNPJ no ViewModel e no serviço.

## Definição (DDL)
```sql
CREATE TABLE ENTIDADES (
  ID_ENTIDADES       INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS        INT          NOT NULL,
  CATEGORIA          ENUM('F','J') NOT NULL COMMENT 'F = Física, J = Jurídica',
  NOME               VARCHAR(150) NOT NULL COMMENT 'Nome (PF) ou Razão Social (PJ)',
  NOME_FANTASIA      VARCHAR(150) NULL COMMENT 'PJ: Nome Fantasia | PF: Sobrenome',
  CPF_CNPJ           VARCHAR(18)  NULL,
  RG_IE              VARCHAR(20)  NULL COMMENT 'PF: RG | PJ: Inscrição Estadual',
  INSC_MUNICIPAL     VARCHAR(20)  NULL,
  INSC_SUFRAMA       VARCHAR(20)  NULL,
  SEXO               ENUM('M','F') NULL,
  DT_NASCIMENTO      DATE         NULL,
  ESTADO_CIVIL       ENUM('S','C','D','P','V') NULL COMMENT 'Solteiro, Casado, Divorciado, Separado, Viúvo',
  TIPO_CLIENTE       TINYINT(1)   NOT NULL DEFAULT 0,
  TIPO_FORNECEDOR    TINYINT(1)   NOT NULL DEFAULT 0,
  TIPO_FUNCIONARIO   TINYINT(1)   NOT NULL DEFAULT 0 COMMENT 'Profissional do laboratório',
  TIPO_VENDEDOR      TINYINT(1)   NOT NULL DEFAULT 0 COMMENT 'Comercial (ID_ENTIDADES_COMERC da proposta)',
  TIPO_TRANSPORTADOR TINYINT(1)   NOT NULL DEFAULT 0,
  AVALIACAO          VARCHAR(20)  NULL,
  OBSERVACAO         TEXT         NULL,
  OBS_INCLUIR_FATURAMENTO TINYINT(1) NOT NULL DEFAULT 0,
  DATA_CADASTRO      DATE         NOT NULL DEFAULT (CURRENT_DATE),
  INATIVO            TINYINT(1)   NOT NULL DEFAULT 0,
  ID_LEGADO          INT          NULL COMMENT 'ID_ENTIDADES do Firebird',
  CRIADO_EM          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CRIADO_POR         INT          NULL,
  ALTERADO_EM        DATETIME     NULL ON UPDATE CURRENT_TIMESTAMP,
  ALTERADO_POR       INT          NULL,
  CONSTRAINT UQ_ENT_DOC UNIQUE (ID_EMPRESAS, CPF_CNPJ),
  INDEX IX_ENT_NOME (ID_EMPRESAS, NOME),
  INDEX IX_ENT_TIPOS (ID_EMPRESAS, TIPO_CLIENTE, TIPO_FUNCIONARIO, INATIVO),
  CONSTRAINT FK_ENT_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS)
) ENGINE=InnoDB;
```
