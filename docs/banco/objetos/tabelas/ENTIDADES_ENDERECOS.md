# ENTIDADES_ENDERECOS

Endereços da entidade (logradouro/bairro em texto + cidade IBGE).

**Grupo:** Cadastros · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/01_schema.sql` · **Migração:** M-07

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Origem no legado (Firebird)
| Origem | Registros no legado |
|---|---|
| `ENTIDADES_ENDERECOS` ⨝ `LOGRADOUROS` ⨝ `CIDADES_BAIRROS` ⨝ `CIDADES` | 538 |

Comentário do script: _Endereço desnormalizado (logradouro/bairro em texto + cidade IBGE). Substitui LOGRADOUROS/CIDADES_BAIRROS do Firebird (ver doc 11, decisão D-07)._

## Colunas
| # | Coluna | Tipo | Nulo | Padrão | Preenchida por | Descrição |
|---:|---|---|---|---|---|---|
| 1 | `ID_ENTIDADES_ENDERECOS` | INT | não |  | auto-incremento |  |
| 2 | `ID_EMPRESAS` | INT | não |  | trigger TG_ENTEND_BI |  |
| 3 | `ID_ENTIDADES` | INT | não |  | aplicação |  |
| 4 | `ID_ENDERECOS_TIPOS` | INT | não |  | aplicação |  |
| 5 | `ID_TIPOS_LOGRADOURO` | INT | sim |  | aplicação |  |
| 6 | `LOGRADOURO` | VARCHAR(150) | não |  | aplicação |  |
| 7 | `NUMERO` | VARCHAR(20) | sim |  | aplicação |  |
| 8 | `COMPLEMENTO` | VARCHAR(50) | sim |  | aplicação |  |
| 9 | `BAIRRO` | VARCHAR(80) | sim |  | aplicação |  |
| 10 | `CEP` | VARCHAR(10) | sim |  | aplicação |  |
| 11 | `ID_CIDADES` | INT | não |  | aplicação |  |
| 12 | `INATIVO` | TINYINT(1) | não | 0 | aplicação |  |
| 13 | `ID_LEGADO` | INT | sim |  | aplicação |  |

## Chaves, restrições e índices
- **PK:** `ID_ENTIDADES_ENDERECOS`

- **Índice** `IX_ENTEND_ENT` (ID_ENTIDADES, ID_ENDERECOS_TIPOS)

## Relacionamentos
### Referencia
| Coluna | Tabela | Coluna destino | Ao excluir o pai |
|---|---|---|---|
| `ID_EMPRESAS` | [`EMPRESAS`](../tabelas/EMPRESAS.md) | ID_EMPRESAS | bloqueia |
| `ID_ENTIDADES` | [`ENTIDADES`](../tabelas/ENTIDADES.md) | ID_ENTIDADES | CASCADE |
| `ID_ENDERECOS_TIPOS` | [`ENDERECOS_TIPOS`](../tabelas/ENDERECOS_TIPOS.md) | ID_ENDERECOS_TIPOS | bloqueia |
| `ID_TIPOS_LOGRADOURO` | [`TIPOS_LOGRADOURO`](../tabelas/TIPOS_LOGRADOURO.md) | ID_TIPOS_LOGRADOURO | bloqueia |
| `ID_CIDADES` | [`CIDADES`](../tabelas/CIDADES.md) | ID_CIDADES | bloqueia |

### É referenciada por
| Tabela | Coluna | Ao excluir esta |
|---|---|---|
| [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md) | ID_ENT_END_LAUDO | bloqueia |
| [`LAB_PROPOSTAS`](../tabelas/LAB_PROPOSTAS.md) | ID_ENT_END_COBRANCA | bloqueia |

## Triggers
- [`TG_ENTEND_BI`](../triggers/TG_ENTEND_BI.md) — BEFORE INSERT: Endereço herda `ID_EMPRESAS` da entidade; um endereço ativo por tipo (`PR_CONFERIR_ENDERECO`).
- [`TG_ENTEND_BU`](../triggers/TG_ENTEND_BU.md) — BEFORE UPDATE: Endereço não muda de entidade/empresa; um endereço ativo por tipo.

## Usada por
- **Views:** [`VW_ENTIDADES_ENDERECOS`](../views/VW_ENTIDADES_ENDERECOS.md)
- **Rotinas que alteram:** —
- **Rotinas que leem:** [`PR_CONFERIR_ENDERECO`](../rotinas/PR_CONFERIR_ENDERECO.md)
- **Triggers de outras tabelas que alteram:** —
- **Triggers de outras tabelas que leem:** —

## Telas e processos
- **Telas:** [T12 Clientes/fornecedores](../../../telas/T12-clientes.md) · [T13 Profissionais](../../../telas/T13-profissionais.md) · [T03 Empresa usuária](../../../telas/T03-empresa-usuaria.md)
- **Processos:** [P01 Multiempresa e empresa ativa](../../../processos/P01-multiempresa.md) · `P25-cadastro-entidades-contatos`

## Regras de negócio
- No máximo um endereço **ativo** por tipo e entidade (no Firebird era `UNIQUE (entidade, tipo)`) — garantido pelo banco (`PR_CONFERIR_ENDERECO`); o antigo fica inativo como histórico.
- Tipo de endereço obrigatório; tipo de logradouro opcional; não muda de entidade (`TG_ENTEND_BU`).
- Usado na proposta como endereço do laudo e endereço da cobrança.
- CEP → ViaCEP (pelo servidor) preenche logradouro, tipo de logradouro, bairro e cidade (pelo IBGE).

## Implementação no C# (EF Core)
- **Multiempresa:** entra no Global Query Filter por `ID_EMPRESAS` (empresa ativa) e o `SaveChanges` preenche `ID_EMPRESAS` na inclusão.
- **Preenchidas pelo banco (trigger):** `ID_EMPRESAS` — não depender do valor enviado; reler a linha após gravar.
- Chave auto-incremento: `ID_ENTIDADES_ENDERECOS`.
- `TINYINT(1)` → `bool`: `INATIVO`.
- `ID_EMPRESAS` herdado da entidade pela trigger `TG_ENTEND_BI` (não precisa enviar).
- Gravado pelo `ContatosService` (textos em maiúsculas, CEP `00000-000`).

## Definição (DDL)
```sql
CREATE TABLE ENTIDADES_ENDERECOS (
  ID_ENTIDADES_ENDERECOS INT AUTO_INCREMENT PRIMARY KEY,
  ID_EMPRESAS        INT          NOT NULL,
  ID_ENTIDADES       INT          NOT NULL,
  ID_ENDERECOS_TIPOS INT          NOT NULL,
  ID_TIPOS_LOGRADOURO INT         NULL,
  LOGRADOURO         VARCHAR(150) NOT NULL,
  NUMERO             VARCHAR(20)  NULL,
  COMPLEMENTO        VARCHAR(50)  NULL,
  BAIRRO             VARCHAR(80)  NULL,
  CEP                VARCHAR(10)  NULL,
  ID_CIDADES         INT          NOT NULL,
  INATIVO            TINYINT(1)   NOT NULL DEFAULT 0,
  ID_LEGADO          INT          NULL,
  INDEX IX_ENTEND_ENT (ID_ENTIDADES, ID_ENDERECOS_TIPOS),
  CONSTRAINT FK_ENTEND_EMP FOREIGN KEY (ID_EMPRESAS) REFERENCES EMPRESAS (ID_EMPRESAS),
  CONSTRAINT FK_ENTEND_ENT FOREIGN KEY (ID_ENTIDADES) REFERENCES ENTIDADES (ID_ENTIDADES) ON DELETE CASCADE,
  CONSTRAINT FK_ENTEND_TIPO FOREIGN KEY (ID_ENDERECOS_TIPOS) REFERENCES ENDERECOS_TIPOS (ID_ENDERECOS_TIPOS),
  CONSTRAINT FK_ENTEND_TIPLOG FOREIGN KEY (ID_TIPOS_LOGRADOURO) REFERENCES TIPOS_LOGRADOURO (ID_TIPOS_LOGRADOURO),
  CONSTRAINT FK_ENTEND_CID FOREIGN KEY (ID_CIDADES) REFERENCES CIDADES (ID_CIDADES)
) ENGINE=InnoDB;
```
