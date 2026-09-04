# Página Inicial

**Controller:** `HomeController`
**Rota base:** `/` ou `/Home`
**Views:** `Views/Home/Index.cshtml`

---

## Telas

### Dashboard (`GET /Home/Index`)
- Página de boas-vindas após o login
- Exibe cards de acesso rápido aos principais módulos
- Não possui lógica de negócio ou consultas ao banco

---

## Layout Compartilhado (`_Layout.cshtml`)

Todos os módulos usam o mesmo layout com:

**Topbar:**
- Nome do sistema: `LabControl`
- Nome da empresa logada (lido de `Session["EmpresaNome"]`)
- Nome do usuário logado (lido de `Session["UsuarioNome"]`)
- Botão **Sair** → `GET /Login/Sair`

**Sidebar (menu lateral):**

| Seção | Item | Rota |
|---|---|---|
| Principal | Início | `/Home` |
| Laboratório | Amostras | `/HistAmostras` |
| Laboratório | Movimentação | `/Movimentacao` |
| Laboratório | Localização | `/LocalAmostras` |
| Laboratório | Propostas | `/Propostas` |
| Laboratório | Resultados | `/Resultados` |
| Laboratório | Laudos | *(não implementado)* |
| Cadastros | Clientes | `/Clientes` |
| Cadastros | Funcionários | `/Funcionarios` |
| Cadastros | Produtos | `/Produtos` |
| Cadastros | Parâmetros | `/Parametros` |
| Cadastros | Tabelas Auxiliares | `/TabelasAuxiliares` |
| Cadastros | Empresa Usuária | `/Empresa/Editar` |
| Sistema | Usuários | `/Usuarios` |
| Sistema | Perfis | `/Perfis` |
| Sistema | Permissões | `/Permissoes` |
| Sistema | Relatórios | `/Relatorios` |
| Sistema | Migração de Dados | `/Migracao` |

**Alertas globais:**
- `TempData["Sucesso"]` → alert verde com botão fechar (auto-dismiss)
- `TempData["Erro"]` → alert vermelho com botão fechar
