# LabControl

Sistema de gestão laboratorial desenvolvido em **ASP.NET Core MVC (C#) + MySQL**, migrado do sistema legado em Delphi 7 + Firebird.

## Tecnologias

| Camada | Tecnologia |
|---|---|
| Backend | ASP.NET Core MVC (.NET 10) |
| ORM | Entity Framework Core 9 + Pomelo MySQL |
| Banco de dados | MySQL 8.0 |
| Frontend | Bootstrap 5 + Bootstrap Icons 1.11 |
| Autenticação | Sessão + SHA256 |

## Módulos

| Módulo | Descrição |
|---|---|
| **Login** | Autenticação com senha SHA256, controle de sessão |
| **Clientes** | Cadastro de PF e PJ com busca AJAX |
| **Produtos** | Cadastro de produtos analisados |
| **Parâmetros** | Parâmetros de análise com valor unitário |
| **Tabelas Auxiliares** | Tipos, status (com cor), prazos, unidades |
| **Amostras** | Registro, movimentações, saldo automático via trigger |
| **Propostas** | Itens, desconto %, recálculo automático, revisões |
| **Resultados** | Lançamento por parâmetro, satisfeito/insatisfeito |
| **Usuários** | Gestão com permissões por módulo (Incluir/Alterar/Consultar/Excluir/Imprimir) |
| **Relatórios** | Amostras e Propostas por período, com filtros e impressão |
| **Migração** | Importação de dados do Firebird (DBMAIS.FDB) para MySQL |

## Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [MySQL Server 8.0](https://dev.mysql.com/downloads/mysql/)

## Configuração

### 1. Banco de dados

Execute os scripts na ordem:

```bash
mysql -u root -p < Scripts/01_criar_banco.sql
mysql -u root -p labcontrol < Scripts/02_procedures_funcoes.sql
```

Isso cria o banco `labcontrol` com 37 tabelas, triggers, procedures, funções e um usuário `admin` / senha `admin123`.

### 2. String de conexão

Edite `appsettings.json` se necessário:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Port=3306;Database=labcontrol;User=root;Password=root;"
}
```

### 3. Executar

```bash
dotnet run
```

Acesse: `http://localhost:5050`

Login padrão: **admin** / **admin123**

## Migração de dados do Firebird

Se possui o banco legado `DBMAIS.FDB`, configure o caminho em `appsettings.json`:

```json
"Firebird": {
  "DatabasePath": "C:\\caminho\\para\\DBMAIS.FDB",
  "User": "SYSDBA",
  "Password": "masterkey"
}
```

Acesse **Sistema → Migração de Dados** na aplicação e clique em **Testar Conexão** para verificar, depois **Executar Migração**.

> Requer Firebird Server em execução ou libs embedded disponíveis.

## Estrutura do projeto

```
LabControl/
├── Controllers/          # Controllers MVC
│   └── Laboratorio/      # Amostras, Propostas, Resultados
├── Data/                 # ApplicationDbContext (EF Core)
├── Models/
│   ├── Entidades/        # Clientes, endereços, fones, e-mails
│   ├── Laboratorio/      # Amostras, propostas, resultados, tabelas
│   ├── Seguranca.cs      # Usuário, permissões, empresa
│   ├── RelatorioVM.cs    # ViewModels de relatórios
│   └── UsuarioEditVM.cs  # ViewModel de usuários
├── Views/                # Razor Views por módulo
├── Scripts/              # SQL: banco, procedures, funções, views
└── wwwroot/              # Bootstrap, jQuery, CSS, JS
```

## Regras de negócio principais

- Código de amostra: `TIPO + COD(3) + ANÁLISE + ANO` (ex: `QU001FQ2026`)
- Código de proposta: `001/2026-R0` (COD/ANO-REVISÃO)
- Entrada de amostra gera movimentação e saldo automaticamente
- Saldo atualizado por trigger após cada movimentação
- Desconto de proposta em % com recálculo automático

## Licença

Uso privado.
