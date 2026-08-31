# Entidades e Tabelas do Banco de Dados

> Todas as tabelas usam charset `utf8mb4` e collation `utf8mb4_unicode_ci`.  
> Decimais financeiros: precisão (15,4).

---

## Segurança

### USUARIO
| Coluna | Tipo | Descrição |
|--------|------|-----------|
| ID | int PK | |
| USUNOM | varchar | Nome completo |
| USULOG | varchar | Login (único) |
| USUSEN | varchar | Senha SHA256 uppercase |
| USUATIVO | bool | Usuário ativo? |
| ADMIN | bool | Acesso total? |

### ACOES
Chave composta `(ID_USUARIO, FORM)`.

| Coluna | Tipo | Descrição |
|--------|------|-----------|
| ID_USUARIO | int FK | Referência a USUARIO |
| FORM | varchar | Identificador do módulo |
| INCLUIR | bool | |
| ALTERAR | bool | |
| CONSULTAR | bool | |
| EXCLUIR | bool | |
| IMPRIMIR | bool | |

### EMPRESA
| Coluna | Tipo | Descrição |
|--------|------|-----------|
| ID | int PK | |
| NOME | varchar | Nome da empresa |
| CNPJ | varchar | |

---

## Entidades (Clientes, Funcionários, etc.)

### ENTIDADES (base)
| Coluna | Tipo | Descrição |
|--------|------|-----------|
| ID | int PK | |
| CATEGORIA | char(1) | `F` = Pessoa Física, `J` = Pessoa Jurídica |
| ATIVO | bool | |

### ENTIDADES_PF
| Coluna | Tipo | Descrição |
|--------|------|-----------|
| ID_ENTIDADE | int FK PK | |
| NOME | varchar | Nome |
| SOBRENOME | varchar | Apelido/nome social |
| CPF | varchar | |
| RG | varchar | |
| DT_NASC | date | |
| SEXO | char(1) | |

### ENTIDADES_PJ
| Coluna | Tipo | Descrição |
|--------|------|-----------|
| ID_ENTIDADE | int FK PK | |
| RAZAO_SOCIAL | varchar | |
| NOME_FANTASIA | varchar | Nome de exibição alternativo |
| CNPJ | varchar | |
| IE | varchar | Inscrição Estadual |

> **Regra:** `ENTIDADES.FANTASIA` não existe mais. Use sempre `ENTIDADES_PJ.NOME_FANTASIA` (PJ) ou `ENTIDADES_PF.SOBRENOME` (PF) como apelido.

### ENTIDADES_FUNCIONARIOS
| Coluna | Tipo | Descrição |
|--------|------|-----------|
| ID_ENTIDADE | int FK PK | |
| ID_CARGO_FUNCIONARIOS | int FK | Cargo |
| REG_PROF | varchar | Nº do registro profissional |
| ID_TIPO_REG_PROF | int FK | Tipo de registro (CREA, CRF...) |
| DT_ADMISSAO | date | |
| DT_DEMISSAO | date | nullable |

### CARGO_FUNCIONARIOS
Seedado automaticamente com 9 cargos padrão.

| Coluna | Tipo |
|--------|------|
| ID | int PK |
| DESCRICAO | varchar |

### ENTIDADES_FUNC_ASSINATURAS
| Coluna | Tipo | Descrição |
|--------|------|-----------|
| ID | int PK | |
| ID_ENTIDADE | int FK | |
| ARQUIVO | longblob | PNG da assinatura |
| MD5 | varchar | Hash do arquivo |
| DT_UPLOAD | datetime | |

---

## Contatos e Endereços

### ENTIDADES_FONES
| Coluna | Tipo | Descrição |
|--------|------|-----------|
| ID | int PK | |
| ID_ENTIDADE | int FK | |
| DDD | varchar | |
| FONE | varchar | |
| ID_FONE_TIPO | int FK | |

### FONE_TIPOS
`Celular`, `Residencial`, `Comercial`, `WhatsApp`, etc.

### ENTIDADES_EMAILS
| Coluna | Tipo | Descrição |
|--------|------|-----------|
| ID | int PK | |
| ID_ENTIDADE | int FK | |
| EMAIL | varchar | |
| PRINCIPAL | bool | |

### ENTIDADES_ENDERECOS
| Coluna | Tipo | Descrição |
|--------|------|-----------|
| ID | int PK | |
| ID_ENTIDADE | int FK | |
| ID_ENDERECO_TIPO | int FK | |
| LOGRADOURO | varchar | |
| NUMERO | varchar | |
| COMPLEMENTO | varchar | |
| BAIRRO | varchar | |
| CIDADE | varchar | |
| UF | char(2) | |
| CEP | varchar | |

### ENTIDADES_OBSERVACOES
| Coluna | Tipo | Descrição |
|--------|------|-----------|
| ID | int PK | |
| ID_ENTIDADE | int FK | |
| OBSERVACAO | text | |
| DT_CADASTRO | datetime | |

---

## Tabelas Auxiliares do Laboratório

### AMOSTRA_TIPOS
| ID | DESCRICAO |
|----|-----------|
| — | Tipos de amostra (ex: Água, Solo, Alimento) |

### AMOSTRA_STATUS
| Coluna | Descrição |
|--------|-----------|
| COR | Hex da cor para badge (ex: `#28a745`) |

### ANALISE_TIPOS
Tipos de análise (ex: Físico-Química, Microbiologia).

### ANALISE_METODOS
Métodos de análise vinculados a tipos.

### IDIOMAS
Idiomas disponíveis para laudos (ex: Português, Inglês).

### PRAZOS
| Coluna | Descrição |
|--------|-----------|
| DESCRICAO | Nome do prazo |
| QTDE | Quantidade de dias |

### UNIDADES
| Coluna | Descrição |
|--------|-----------|
| DESCRICAO | Nome |
| SIGLA | Ex: `mg/L`, `UFC/mL` |

### MOEDAS
Ex: BRL, USD.

### CONDICOES_PAGAMENTO
Condições de pagamento (ex: À vista, 30 dias).

### EMBALAGEM_TIPOS
Tipos de embalagem de amostras.

---

## Produtos e Parâmetros

### PRODUTOS
| Coluna | Tipo | Descrição |
|--------|------|-----------|
| ID | int PK | |
| DESCRICAO | varchar | |
| ID_UNIDADE | int FK | |
| ID_EMBALAGEM_TIPO | int FK | |

### PARAMETROS_ANALISES
| Coluna | Tipo | Descrição |
|--------|------|-----------|
| ID | int PK | |
| DESCRICAO | varchar | |
| CODIGO | varchar | |
| ID_ANALISE_TIPO | int FK | |
| ID_ANALISE_METODO | int FK | |
| ID_UNIDADE | int FK | |
| VR_UNITARIO | decimal(15,4) | |

---

## Propostas

### PROPOSTAS
| Coluna | Tipo | Descrição |
|--------|------|-----------|
| ID | int PK | |
| CODIGO | varchar | Formato `001/2026-R0` |
| ID_ENTIDADE | int FK | Cliente |
| DT_PROPOSTA | date | |
| ID_PROPOSTA_STATUS | int FK | |
| ID_CONDICAO_PAGAMENTO | int FK | |
| ID_MOEDA | int FK | |
| ID_IDIOMA | int FK | |
| PORC_DESCONTO | decimal | Desconto geral % |
| VR_TOTAL | decimal(15,4) | |
| REVISAO | int | 0 = R0 |

### PROPOSTAS_ANALISES
| Coluna | Tipo | Descrição |
|--------|------|-----------|
| ID | int PK | |
| ID_PROPOSTA | int FK | |
| ID_PRODUTO | int FK | |
| ID_ANALISE_TIPO | int FK | |
| ID_ANALISE_METODO | int FK | |
| ID_PARAMETRO | int FK | |
| ID_IDIOMA | int FK | |
| ID_PRAZO | int FK | |
| QTDE | int | |
| VR_UNITARIO | decimal(15,4) | |
| VR_DESCONTO | decimal(15,4) | |
| VR_TOTAL | decimal(15,4) | Calculado |

---

## Amostras

### HIST_AMOSTRAS
| Coluna | Tipo | Descrição |
|--------|------|-----------|
| ID | int PK | |
| CODIGO | varchar | Formato `QU001FQ2026` |
| ID_ENTIDADE | int FK | Cliente |
| ID_AMOSTRA_TIPO | int FK | |
| ID_ANALISE_TIPO | int FK | |
| ID_AMOSTRA_STATUS | int FK | |
| ID_PRAZO | int FK | |
| DT_ENTRADA | datetime | |
| DT_PREVISTA | date | Calculada pelo prazo |
| OBS | text | |

### HIST_AMOSTRAS_TESTES
Análises/testes associados à amostra.

| Coluna | Tipo | Descrição |
|--------|------|-----------|
| ID | int PK | |
| ID_AMOSTRA | int FK | |
| ID_ANALISE_TIPO | int FK | |
| ID_ANALISE_METODO | int FK | |

### HIST_AMOSTRAS_SALDOS
Saldo atual da amostra (atualizado automaticamente).

| Coluna | Tipo | Descrição |
|--------|------|-----------|
| ID_AMOSTRA | int FK PK | |
| SALDO | decimal | |
| DT_ATUALIZACAO | datetime | |

### MOV_AMOSTRAS
Movimentações (entrada, saída, descarte).

| Coluna | Tipo | Descrição |
|--------|------|-----------|
| ID | int PK | |
| ID_AMOSTRA | int FK | |
| TIPO_MOV | char | `E`=Entrada, `S`=Saída, `D`=Descarte |
| QTDE | decimal | |
| DT_MOV | datetime | |
| OBS | text | |

### LOCAL_AMOSTRAS
| Coluna | Tipo | Descrição |
|--------|------|-----------|
| ID | int PK | |
| ID_AMOSTRA | int FK | |
| LOCALIZACAO | varchar | Localização física |
| DT_DESCARTE | date | nullable |

---

## Resultados

### RESULTADOS_ANALISES
| Coluna | Tipo | Descrição |
|--------|------|-----------|
| ID | int PK | |
| ID_AMOSTRA | int FK | |
| REVISAO | int | 0, 1, 2... |
| DT_RESULTADO | datetime | |
| ID_USUARIO | int FK | Responsável |

### RESULTADOS_PARAM
| Coluna | Tipo | Descrição |
|--------|------|-----------|
| ID | int PK | |
| ID_RESULTADO | int FK | |
| ID_PARAMETRO | int FK | |
| VALOR | varchar | Valor medido |
| SATISFEITO | bool | Dentro do limite? |
| OBS | text | |
