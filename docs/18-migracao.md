# Migração de Dados (Firebird → MySQL)

**Controller:** `MigracaoController`
**Rota base:** `/Migracao`
**Views:** `Views/Migracao/Index.cshtml`

---

## Telas

### Painel de Migração (`GET /Migracao`)
- Exibe o caminho do banco Firebird configurado (`appsettings.json` → `Firebird:DatabasePath`)
- Botão **Testar Conexão** — verifica conectividade e lista as tabelas com suas contagens
- Botão **Executar Migração** — realiza a migração completa
- Checkbox **Limpar dados existentes** — se marcado, apaga todas as tabelas (exceto PERFIS e PERMISSOES) antes de inserir

---

## Regras de Negócio

### Configuração
Parâmetros lidos de `appsettings.json`:
```json
"Firebird": {
  "DatabasePath": "C:\\...\\DBMAIS.FDB",
  "User": "SYSDBA",
  "Password": "masterkey"
}
```

Caminho real do banco Firebird em uso: `C:\Project\Laboratorio\DBMAIS.FDB`

### Conexão Firebird
- Tenta primeiro no modo **embedded** (ServerType=1, sem servidor)
- Se falhar, tenta via **localhost:3050** (servidor Firebird local)
- Charset: `ISO8859_1`

### Testar Conexão (`POST /Migracao/Testar`)
- Verifica se consegue abrir a conexão
- Para cada tabela mapeada: executa `SELECT COUNT(*)` e informa se existe e quantos registros tem

### Executar Migração (`POST /Migracao/Executar`)
Executa em **uma única transação MySQL**. Sequência completa:

| # | Origem (Firebird) | Destino (MySQL) | Observações |
|---|---|---|---|
| 1 | `CARGO_FUNCIONARIOS` | `CARGO_FUNCIONARIOS` | `ON DUPLICATE KEY UPDATE` (seed do Program.cs tem 9 cargos fixos) |
| 2 | `TIPOS_REG_PROFISSIONAL` | `TIPOS_REG_PROFISSIONAL` | `INSERT IGNORE` — coluna `DESCRICAO_REG_PROFISSIONAL` |
| 3 | `MOEDAS` | `MOEDAS` | `INSERT IGNORE` |
| 4 | `LAB_PROPOSTAS_STATUS` | `LAB_PROPOSTAS_STATUS` | `INSERT IGNORE` |
| 5 | `AMOSTRAS_STATUS` | `AMOSTRAS_STATUS` | `INSERT IGNORE` |
| 6 | `AMOSTRAS_TIPO` | `AMOSTRAS_TIPO` | `INSERT IGNORE` |
| 7 | `ANALISES_METODOS` | `ANALISES_METODOS` | `INSERT IGNORE` |
| 8 | `ANALISES_STATUS` | `ANALISES_STATUS` | `INSERT IGNORE` |
| 9 | `ANALISES_TIPO` | `ANALISES_TIPO` | `INSERT IGNORE` |
| 10 | `BOLETINS_STATUS` | `BOLETINS_STATUS` | `INSERT IGNORE` |
| 11 | `EMBALAGENS_TIPOS` | `EMBALAGENS_TIPOS` | `INSERT IGNORE` |
| 12 | `ENDERECOS_TIPOS` | `ENDERECOS_TIPOS` | `INSERT IGNORE` |
| 13 | `FONES_TIPOS` | `FONES_TIPOS` | `INSERT IGNORE` |
| 14 | `IDIOMAS` | `IDIOMAS` | `INSERT IGNORE` |
| 15 | `UNIDADES` | `UNIDADES` | com campo SIGLA |
| 16 | `ENTIDADES` | `ENTIDADES` | `ON DUPLICATE KEY UPDATE` — `INATIVO` "S"/"N" → 1/0; sobrescreve seed 'Minha Empresa' |
| 17 | `ENTIDADES_TIPOS` (ou `ENTIDADES_TIPO`) | `ENTIDADES` | UPDATE de flags tipo (Cliente, Fornecedor, Vendedor, Funcionário, Empresa) |
| 18 | `EMPRESAS` | `EMPRESAS` | `ON DUPLICATE KEY UPDATE` — sobrescreve seed ID=1 |
| 19 | `ENTIDADES_PF` | `ENTIDADES_PF` | Estado civil: 0→S, 1→C, 2→D, 3→P, 4→V |
| 20 | `ENTIDADES_PJ` | `ENTIDADES_PJ` | CNPJ, Nome Fantasia, Inscrições |
| 21 | `ENTIDADES_FUNCIONARIOS` | `ENTIDADES_FUNCIONARIOS` | `ID_TIPOS_REG_PROFISSIONAL = NULL` |
| 22 | `ENTIDADES_FONES` | `ENTIDADES_FONES` | |
| 23 | `ENTIDADES_EMAILS` | `ENTIDADES_EMAILS` | `PRINCIPAL` "S" → 1 |
| 24 | `ENTIDADES_ENDERECOS` | `ENTIDADES_ENDERECOS` | JOIN desnormalizado: logradouro+bairro+cidade+UF |
| 25 | `USUARIO` | `USUARIO` | `ON DUPLICATE KEY UPDATE`; `USULOG = USUNOM`; Senha: SHA256(nome minúsculo) |
| 26 | — | `ENTIDADES` | Inferência de tipos se tabela ENTIDADES_TIPOS não existir |
| 27 | `CONDICOES_PAGTOS` | `CONDICOES_PAGTOS` | |
| 28 | `PRAZOS` | `PRAZOS` | |
| 29 | `PRODUTOS` | `PRODUTOS` | |
| 30 | `LAB_PARAMETROS_ANALISES` | `LAB_PARAMETROS_ANALISES` | |
| 31 | `LAB_PROPOSTAS` | `LAB_PROPOSTAS` | |
| 32 | `LAB_PROPOSTAS_ANALISES` (JOIN PRODANALISES + PRODUTOS) | `LAB_PROPOSTAS_ANALISES` | `HR_ENTREGA` TIMESTAMP→VARCHAR "HH:mm" |
| 33 | `LAB_HIST_AMOSTRAS` | `LAB_HIST_AMOSTRAS` | Mapeamento de colunas renomeadas |
| 34 | `LAB_HIST_AMOSTRAS_TESTES` | `LAB_HIST_AMOSTRAS_TESTES` | |
| 35 | `LAB_HIST_AMOSTRAS_SALDO` | `LAB_HIST_AMOSTRAS_SALDO` | |
| 36 | `LAB_MOV_AMOSTRAS` | `LAB_MOV_AMOSTRAS` | |
| 37 | `LAB_MOV_AMOSTRAS_PARAM` | `LAB_MOV_AMOSTRAS_PARAM` | |
| 38 | `LAB_LOCAL_AMOSTRAS` | `LAB_LOCAL_AMOSTRAS` | |

### Colunas renomeadas Firebird → MySQL (LAB_HIST_AMOSTRAS)
| Firebird | MySQL |
|---|---|
| `COLETOR` | `NOME_COLETOR` |
| `DATAHORACOLETA` | `DT_HR_COLETA` |
| `TEMPERATURA_VERFIFICACAO` | `TEMPERATURA_VERIFICACAO` |
| `ACOMPANHA_PADRA_ANALITICO` | `ACOMPANHA_PADRAO_ANALITICO` |
| `ID_STATUS_PROPOSTAS` | `ID_LAB_PROPOSTAS_STATUS` |
| `DT_HR_DESCATE` | `DT_HR_DESCARTE` |

### Senhas dos Usuários Migrados
- A senha original do Firebird **não é recuperável** (hash proprietário)
- Nova senha = SHA256(nome do usuário em minúsculas)
- Exemplo: usuário `ADMINISTRADOR` → senha `administrador`

### Pós-migração necessário (executado manualmente via MySQL CLI)
```sql
-- USULOG já é gravado durante a migração; este UPDATE serve de segurança
UPDATE USUARIO SET USULOG = USUNOM WHERE USULOG IS NULL OR USULOG = '';

-- Atribuir todos os usuários ao perfil MASTER
SET @idMaster = (SELECT ID_PERFIS FROM PERFIS WHERE COD_PERFIL = 'MASTER' LIMIT 1);
INSERT IGNORE INTO USUARIOS_PERFIS (USUCOD, ID_PERFIS) SELECT USUCOD, @idMaster FROM USUARIO;
```

### Limpar banco (antes de nova migração)
```sql
SET FOREIGN_KEY_CHECKS = 0;
-- Apagar todas as tabelas EXCETO PERFIS, PERMISSOES e PERFIS_PERMISSOES
-- (lista completa no controller: limpar=true)
SET FOREIGN_KEY_CHECKS = 1;
```

### Tratamento de Erros
- Erros individuais por linha **não interrompem** a migração — são contabilizados e logados
- Erros de abertura da tabela de origem: registrado como falha no `MigResult`
- Se qualquer etapa lançar exceção crítica: **Rollback** de toda a transação MySQL
- O resultado final retorna JSON com status de cada tabela (`{ tabela, inseridos, erros, detalhe, ok }`)

### Resultado da Migração
- Exibido em tela como grid com: Tabela, Inseridos, Erros, Detalhe
- Linha verde: sucesso (`ok = true`)
- Linha vermelha: falha (`ok = false`)

---

## Volumes migrados (última execução — 2026-09-03)
| Tabela | Registros |
|---|---|
| Cargos Funcionários | 9 |
| Tipos Reg. Profissional | 2 (CRF, CRQ) |
| Moedas | 2 |
| Propostas Status | 3 |
| Amostras Status | 4 |
| Amostras Tipo | 13 |
| Análises Métodos | 176 |
| Unidades | 27 |
| Entidades | 527 |
| Empresas | 1 |
| Entidades PF/PJ/Func | 44 / 483 / 16 |
| Usuários | 15 |
| Condições Pagamento / Prazos | 112 / 17 |
| Produtos / Parâmetros | 1.915 / 462 |
| Propostas / Propostas Análises | 2.358 / 19.252 |
| Hist. Amostras / Testes / Saldo | 5.770 / 20.551 / 5.770 |
| Movimentações / Localização | 5.770 / 2.686 |

---

## Tabelas de Origem (Firebird)
Banco: `C:\Project\Laboratorio\DBMAIS.FDB` (Firebird 2.5 embedded ou localhost:3050)

Todas as tabelas listadas na sequência acima, mais as tabelas de sistema Firebird (`RDB$*`) consultadas para verificação de existência.
