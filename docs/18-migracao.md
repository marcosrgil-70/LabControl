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
- Checkbox **Limpar dados existentes** — se marcado, faz `DELETE FROM tabela` antes de inserir

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

### Conexão Firebird
- Tenta primeiro no modo **embedded** (ServerType=1, sem servidor)
- Se falhar, tenta via **localhost:3050** (servidor Firebird local)
- Charset: `ISO8859_1`

### Testar Conexão (`POST /Migracao/Testar`)
- Verifica se consegue abrir a conexão
- Para cada uma das 19 tabelas mapeadas: executa `SELECT COUNT(*)` e informa se existe e quantos registros tem

### Executar Migração (`POST /Migracao/Executar`)
Executa em **uma única transação MySQL**. Sequência:

| # | Origem (Firebird) | Destino (MySQL) | Observações |
|---|---|---|---|
| 1 | `AMOSTRAS_STATUS` | `AMOSTRAS_STATUS` | `INSERT IGNORE` |
| 2 | `AMOSTRAS_TIPO` | `AMOSTRAS_TIPO` | `INSERT IGNORE` |
| 3 | `ANALISES_METODOS` | `ANALISES_METODOS` | `INSERT IGNORE` |
| 4 | `ANALISES_STATUS` | `ANALISES_STATUS` | `INSERT IGNORE` |
| 5 | `ANALISES_TIPO` | `ANALISES_TIPO` | `INSERT IGNORE` |
| 6 | `BOLETINS_STATUS` | `BOLETINS_STATUS` | `INSERT IGNORE` |
| 7 | `EMBALAGENS_TIPOS` | `EMBALAGENS_TIPOS` | `INSERT IGNORE` |
| 8 | `ENDERECOS_TIPOS` | `ENDERECOS_TIPOS` | `INSERT IGNORE` |
| 9 | `FONES_TIPOS` | `FONES_TIPOS` | `INSERT IGNORE` |
| 10 | `IDIOMAS` | `IDIOMAS` | `INSERT IGNORE` |
| 11 | `UNIDADES` | `UNIDADES` | com campo SIGLA |
| 12 | `ENTIDADES` | `ENTIDADES` | `INATIVO` "S"/"N" → 1/0 |
| 13 | `ENTIDADES_TIPOS` (ou `ENTIDADES_TIPO`) | `ENTIDADES` | UPDATE de flags tipo (Cliente, Fornecedor, Vendedor, Funcionário, Empresa) |
| 14 | `ENTIDADES_PF` | `ENTIDADES_PF` | Estado civil: 0→S, 1→C, 2→D, 3→P, 4→V |
| 15 | `ENTIDADES_PJ` | `ENTIDADES_PJ` | CNPJ, Nome Fantasia, Inscrições |
| 16 | `ENTIDADES_FUNCIONARIOS` | `ENTIDADES_FUNCIONARIOS` | `ID_TIPOS_REG_PROFISSIONAL = NULL` |
| 17 | `ENTIDADES_FONES` | `ENTIDADES_FONES` | |
| 18 | `ENTIDADES_EMAILS` | `ENTIDADES_EMAILS` | `PRINCIPAL` "S" → 1 |
| 19 | `ENTIDADES_ENDERECOS` | `ENTIDADES_ENDERECOS` | JOIN desnormalizado: logradouro+bairro+cidade+UF |
| 20 | `USUARIO` | `USUARIOS` | Senha: SHA256(login minúsculo) — senha original não recuperada |
| 21 | — | `ENTIDADES` | Inferência de tipos se tabela ENTIDADES_TIPOS não existir |

### Senhas dos Usuários Migrados
- A senha original do Firebird **não é recuperável** (hash proprietário)
- Nova senha = SHA256(código do usuário em minúsculas)
- Exemplo: usuário `ADMINISTRADOR` → senha `administrador`

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

## Tabelas de Origem (Firebird)
`AMOSTRAS_STATUS`, `AMOSTRAS_TIPO`, `ANALISES_METODOS`, `ANALISES_STATUS`, `ANALISES_TIPO`, `BOLETINS_STATUS`, `EMBALAGENS_TIPOS`, `ENDERECOS_TIPOS`, `FONES_TIPOS`, `IDIOMAS`, `UNIDADES`, `ENTIDADES`, `ENTIDADES_TIPOS`/`ENTIDADES_TIPO`, `ENTIDADES_PF`, `ENTIDADES_PJ`, `ENTIDADES_FUNCIONARIOS`, `ENTIDADES_FONES`, `ENTIDADES_EMAILS`, `ENTIDADES_ENDERECOS`, `USUARIO`
