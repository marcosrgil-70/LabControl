# LabControl — Documentação do Sistema

Migração do sistema legado **Delphi 7 + Firebird** para **ASP.NET Core MVC (.NET 10) + MySQL**.

- **Diretório do projeto:** `C:\Project\LabControl`
- **Repositório GitHub:** `marcosrgil-70/LabControl` (branch `master`)
- **Sistema legado (referência):** `C:\Project\Laboratorio`

---

## Índice de Módulos

### Autenticação e Sistema
| Arquivo | Módulo | Rota |
|---|---|---|
| [01-login.md](01-login.md) | Login / Autenticação | `/Login` |
| [09-usuarios.md](09-usuarios.md) | Usuários | `/Usuarios` |
| [10-perfis.md](10-perfis.md) | Perfis de Acesso | `/Perfis` |
| [11-permissoes.md](11-permissoes.md) | Permissões | `/Permissoes` |

### Cadastros
| Arquivo | Módulo | Rota |
|---|---|---|
| [03-clientes.md](03-clientes.md) | Clientes | `/Clientes` |
| [04-funcionarios.md](04-funcionarios.md) | Funcionários | `/Funcionarios` |
| [05-produtos.md](05-produtos.md) | Produtos | `/Produtos` |
| [06-parametros.md](06-parametros.md) | Parâmetros de Análise | `/Parametros` |
| [07-empresa-usuaria.md](07-empresa-usuaria.md) | Empresa Usuária | `/Empresa` |
| [08-tabelas-auxiliares.md](08-tabelas-auxiliares.md) | Tabelas Auxiliares (21 cadastros) | `/TabelasAuxiliares` |

### Laboratório
| Arquivo | Módulo | Rota |
|---|---|---|
| [12-propostas.md](12-propostas.md) | Propostas | `/Propostas` |
| [13-amostras.md](13-amostras.md) | Amostras (HistAmostras) | `/HistAmostras` |
| [14-resultados.md](14-resultados.md) | Resultados de Análise | `/Resultados` |
| [15-movimentacao.md](15-movimentacao.md) | Movimentação de Amostras | `/Movimentacao` |
| [16-localizacao.md](16-localizacao.md) | Localização de Amostras | `/LocalAmostras` |

### Utilitários
| Arquivo | Módulo | Rota |
|---|---|---|
| [17-relatorios.md](17-relatorios.md) | Relatórios | `/Relatorios` |
| [18-migracao.md](18-migracao.md) | Migração de Dados (Firebird → MySQL) | `/Migracao` |

---

## Arquitetura Geral

### Stack Tecnológica
- **Backend:** ASP.NET Core MVC (.NET 10), C#
- **ORM:** Entity Framework Core 9 + Pomelo (MySQL)
- **Banco de dados:** MySQL
- **Frontend:** Bootstrap 5, Bootstrap Icons, jQuery, Razor Views
- **Autenticação:** Session-based (`SessaoFilter` global)

### Padrões Globais
- Autenticação por sessão: qualquer rota exige login, exceto `/Login`
- Chaves de sessão: `UsuarioId`, `UsuarioNome`, `UsuarioCodigo`, `UsuarioAdmin`, `EmpresaId`, `EmpresaNome`
- Hash de senha: `SHA256` → hex minúsculo
- Feedback pós-ação: `TempData["Sucesso"]` (verde) e `TempData["Erro"]` (vermelho)
- Schema patches idempotentes no startup (`Program.cs`): `CREATE TABLE IF NOT EXISTS`, `ALTER TABLE` com tratamento de erro MySQL 1060

### Layout
- Topbar: nome da empresa (sessão) + nome do usuário + botão Sair
- Sidebar: menu lateral com seções Principal, Laboratório, Cadastros, Sistema
- Código formatado de Amostra: `TT-SSSSS-AA/YY` (ex: `01-00001-02/26`)
- Código formatado de Proposta: `000001/26-R0`

### Credenciais padrão (após migração)
- Login: `ADMINISTRADOR` / Senha: `administrador`
- Usuários migrados do Firebird: senha = SHA256(login em minúsculas)
