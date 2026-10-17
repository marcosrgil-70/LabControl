# Arquitetura do LabControl

## Stack Técnica

| Camada | Tecnologia | Versão |
|--------|-----------|--------|
| Framework | ASP.NET Core MVC | .NET 10.0 |
| ORM | Entity Framework Core + Pomelo | 9.0.5 |
| Banco de dados | MySQL | 8.0+ |
| Frontend | Bootstrap + Bootstrap Icons | 5 / 1.11 |
| Autenticação | Sessão HTTP + SHA256 | — |
| Legado (migração) | Firebird Client | 10.3.4 |

---

## Estrutura de Pastas

```
LabControl/
├── Controllers/
│   ├── ClientesController.cs
│   ├── FuncionariosController.cs
│   ├── HomeController.cs
│   ├── LoginController.cs
│   ├── MigracaoController.cs
│   ├── ParametrosController.cs
│   ├── ProdutosController.cs
│   ├── RelatoriosController.cs
│   ├── TabelasAuxiliaresController.cs
│   ├── UsuariosController.cs
│   └── Laboratorio/
│       ├── HistAmostrasController.cs
│       ├── PropostasController.cs
│       └── ResultadosController.cs
├── Data/
│   └── ApplicationDbContext.cs       # EF Core — 36+ DbSets
├── Filters/
│   └── SessaoFilter.cs               # Filtro global de autenticação
├── Models/
│   ├── Entidades/
│   │   └── Entidade.cs               # Entidade, PF, PJ, Fone, Email, Endereco...
│   ├── Laboratorio/
│   │   ├── Tabelas.cs                # Lookup tables (tipos, status, unidades...)
│   │   ├── Proposta.cs               # Proposta + PropostaAnalise
│   │   ├── HistAmostra.cs            # HistAmostra, Testes, Saldo, Mov, Local
│   │   └── Resultado.cs              # ResultadoAnalise + ResultadoParam
│   ├── Seguranca.cs                  # Usuario, AcaoUsuario, Empresa
│   ├── RelatorioVM.cs                # ViewModels de relatórios
│   └── UsuarioEditVM.cs
├── Views/
│   ├── Clientes/
│   ├── Funcionarios/
│   ├── HistAmostras/
│   ├── Home/
│   ├── Login/
│   ├── Migracao/
│   ├── Parametros/
│   ├── Produtos/
│   ├── Propostas/
│   ├── Relatorios/
│   ├── Resultados/
│   ├── TabelasAuxiliares/
│   ├── Usuarios/
│   └── Shared/
│       ├── _Layout.cshtml            # Layout principal (topbar + sidebar)
│       └── _ValidationScriptsPartial.cshtml
├── Scripts/                          # SQL: banco, procedures, funções, views
├── wwwroot/                          # Bootstrap, jQuery, CSS customizado
├── appsettings.json
└── Program.cs                        # Configuração + patches de schema
```

---

## Program.cs — Inicialização

### Serviços configurados
- **EF Core** com Pomelo (MySQL, charset utf8mb4)
- **Session** com timeout de 8 horas e cookie HttpOnly
- **SessaoFilter** registrado como filtro global (exceto rota `/Login`)

### Patches de schema no startup
Executados via SQL direto a cada inicialização (idempotentes):

| Patch | O que faz |
|-------|-----------|
| DROP COLUMN FANTASIA | Remove coluna criada por engano em ENTIDADES |
| CREATE ENTIDADES_OBSERVACOES | Cria tabela de observações se não existir |
| CREATE CARGO_FUNCIONARIOS | Cria tabela de cargos e seed de 9 cargos |
| ADD COLUMN ID_CARGO_FUNCIONARIOS | Adiciona FK em ENTIDADES_FUNCIONARIOS |
| CREATE ENTIDADES_FUNC_ASSINATURAS | Cria tabela de assinaturas digitais |

> **Por que patches e não migrations EF?** O projeto opta por SQL direto no startup para manter controle total sobre DDL em produção sem depender do fluxo `dotnet ef migrations`.

---

## Autenticação e Segurança

### Fluxo de login
1. Usuário submete `login` + `senha` (form `name="login"` / `name="senha"`)
2. `LoginController` calcula `SHA256(senha).ToUpper()` e compara com `USUSEN`
3. Se válido: armazena `ID`, `Nome`, `Login`, `Admin` na sessão
4. `SessaoFilter` valida a sessão em cada request; redireciona para `/Login` se ausente

### Controle de permissões
- Tabela `ACOES` com chave composta `(ID_USUARIO, FORM)`
- 5 flags por módulo: `Incluir`, `Alterar`, `Consultar`, `Excluir`, `Imprimir`
- Usuários marcados como `ADMIN = true` têm acesso total independente das permissões

### Credenciais padrão
| Usuário | Senha |
|---------|-------|
| ADMINISTRADOR | administrador |

Usuários migrados do Firebird recebem como senha inicial o próprio login em minúsculas.

---

## Layout e Menu

**Arquivo:** `Views/Shared/_Layout.cshtml`

### Topbar
- Fundo `#1a2f4e`
- Logo LabControl + código da empresa (001)
- Nome do usuário logado (sessão)
- Link **Sair**

### Sidebar (230px, fundo `#1a2f4e`)

```
PRINCIPAL
└── Início

LABORATÓRIO
├── Amostras        → /HistAmostras
├── Propostas       → /Propostas
├── Resultados      → /Resultados
└── Laudos          (desabilitado)

CADASTROS
├── Clientes        → /Clientes
├── Funcionários    → /Funcionarios
├── Produtos        → /Produtos
├── Parâmetros      → /Parametros
└── Tabelas Auxiliares → /TabelasAuxiliares

SISTEMA
├── Usuários        → /Usuarios
├── Relatórios      → /Relatorios
└── Migração de Dados → /Migracao
```

### Alertas
`TempData["Sucesso"]` e `TempData["Erro"]` são exibidos automaticamente no layout como banners Bootstrap.

---

## Padrão AJAX para Abas de Entidade

Clientes e Funcionários compartilham os mesmos partials e endpoints AJAX, operando genericamente por `idEntidade`:

| Partial | Endpoint de atualização |
|---------|------------------------|
| `_GridFones.cshtml` | `POST /Clientes/AdicionarFone`, `ExcluirFone` |
| `_GridEmails.cshtml` | `POST /Clientes/AdicionarEmail`, `ExcluirEmail`, `MarcarEmailPrincipal` |
| `_GridEnderecos.cshtml` | `POST /Clientes/AdicionarEndereco`, `ExcluirEndereco` |

Cada ação retorna o partial renderizado via `PartialView(...)`, e o JS substitui o `innerHTML` da aba correspondente.

---

## Banco de Dados

- **Host:** localhost:3306
- **Database:** `labcontrol`
- **Charset:** utf8mb4
- **Collation:** utf8mb4_unicode_ci

A string de conexão está em `appsettings.json` → `ConnectionStrings:DefaultConnection`.

Para a migração Firebird: `appsettings.json` → seção `Firebird` (`DatabasePath`, `User`, `Password`).
