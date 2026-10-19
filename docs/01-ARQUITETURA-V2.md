# 01 — Arquitetura do LabControl v2 (C# + MySQL)

## 1. Stack

| Camada | Tecnologia |
|---|---|
| Web | ASP.NET Core MVC (.NET 10), Razor, Bootstrap 5, jQuery (mantido do projeto atual) |
| Dados | EF Core 9 + Pomelo MySQL; **sem migrations EF**: o schema é dos scripts `Scripts/v2/*.sql` (fonte da verdade) |
| Banco | MySQL 8.0, banco `labcontrol_v2` |
| Autenticação | Cookie Authentication (não mais sessão "manual"); senha PBKDF2 (`PasswordHasher<Usuario>`) |
| Relatórios/PDF | QuestPDF (boletim, proposta, pedido, etiquetas) — substitui FastReport `.fr3` |
| Testes | xUnit + banco MySQL de teste recriado pelos scripts |

## 2. Estrutura de pastas (alvo)

```
LabControl/
├── Data/            LabDbContext.cs, Configurations/ (IEntityTypeConfiguration por tabela),
│                    Interceptors/SessaoMySqlInterceptor.cs (SET @LC_ID_USUARIOS etc.)
├── Models/          Entidades (1 classe por tabela), Enums
├── ViewModels/      por tela
├── Services/        regras de negócio (AmostraService, PropostaService, BoletimService, NumeracaoService…)
│   └── Migracao/    MigracaoService (Firebird → MySQL)
├── Infra/           EmpresaContexto (empresa/usuário/profissional do request), Permissoes, Pdf/
├── Controllers/     finos: validam, chamam Service, devolvem View/JSON
├── Views/           por controller + Shared/_Layout, Shared/Components (CRUD genérico)
├── Scripts/v2/      DDL + rotinas + triggers + views + seed  ← FONTE DA VERDADE DO SCHEMA
└── docs/            esta documentação
```

## 3. Multiempresa (regra de ouro)

1. Após o login o usuário escolhe a **empresa ativa** (entre `USUARIOS_EMPRESAS` ativas). Guardada na claim `EmpresaId`.
2. `IEmpresaContexto` (scoped) expõe `EmpresaId`, `UsuarioId`, `ProfissionalId` (= `USUARIOS_EMPRESAS.ID_ENTIDADES_FUNC`) e `TemPermissao(codigo)`.
3. **Global Query Filter** do EF em toda entidade com `ID_EMPRESAS`: `e => e.IdEmpresa == contexto.EmpresaId`. Proibido `IgnoreQueryFilters()` fora de Migração/Admin.
4. Em todo `Add`, o `SaveChanges` preenche `ID_EMPRESAS` com a empresa ativa (e as triggers do banco garantem a herança nos filhos).
5. Combos/lookups só listam registros da empresa ativa e **ativos**.
6. Trocar de empresa = menu do topo → nova claim → redireciona para a Home (descarta telas abertas).
7. Criar empresa (ADMIN_GLOBAL) chama `SP_INICIALIZAR_EMPRESA` e oferece **copiar cadastros auxiliares/parâmetros/produtos** de outra empresa (rotina a criar na fase 1).

## 4. Segurança

- Permissão = `TELA.ACAO` (catálogo `PERMISSOES`). Atributo `[Permissao("AMOSTRAS.INCLUIR")]` nas actions; menu e botões ocultos sem permissão (equivale ao `unClassAcessos` do Delphi).
- `ADMIN_GLOBAL` ignora permissões.
- Toda ação de escrita grava `CRIADO_POR/ALTERADO_POR` e, para tabelas críticas, `AUDITORIA` (antes/depois em JSON).
- Bloqueio após 5 tentativas de login (15 min); troca de senha obrigatória no 1º acesso.

## 5. Conexão e variáveis de sessão MySQL

`SessaoMySqlInterceptor.ConnectionOpened`: `SET @LC_ID_USUARIOS = {UsuarioId}, @LC_MIGRACAO = 0, @LC_PERMITE_RETROATIVO = 0;`
O `AmostraService`, quando o usuário confirmou a data retroativa **e** tem `AMOSTRAS.ENTREGA_RETROATIVA`, executa `SET @LC_PERMITE_RETROATIVO = 1` na mesma conexão/transação do INSERT.

## 6. Padrões de UI (iguais em todas as telas)

| Delphi | Web v2 |
|---|---|
| Aba **Consultar** com filtros + grid | `Index` com filtros (cliente, período padrão = `DIAS_PESQUISA_PADRAO`, número) e tabela paginada (server-side) |
| Aba **Dados** em modo Incluir/Alterar | `Criar` / `Editar` (form), botões Gravar/Cancelar; abas Bootstrap quando a tela do Delphi tem abas |
| Lookups `cxDBLookupComboBox` com várias colunas | `select` com busca (Tom Select) carregado via AJAX e mostrando as mesmas colunas |
| Botão "+" ao lado do lookup (cadastro rápido de método/idioma/prazo/tipo de resultado) | modal de cadastro rápido que devolve o novo item ao combo |
| Mensagem `Atencao(...)` | validação no servidor (ModelState) + `TempData` + mensagens das triggers (SQLSTATE 45000) exibidas ao usuário |
| Cores de status | badge com `COR` do status |
| Impressão FastReport | PDF (QuestPDF) aberto em nova aba |

Erros SQLSTATE `45000` das triggers/procedures são **mensagens de negócio**: capturar `MySqlException` com `SqlState == "45000"` e mostrar `Message` ao usuário.

## 7. Convenções de código

- C#: entidades e configurações Fluent (uma por tabela/view) **geradas** de `Scripts/v2` por `node tools/gerador-csharp/gerar.mjs` (`*.g.cs`, não editar; ajustes em parciais). Classe no singular (`LabHistAmostra`), DbSet = tabela em PascalCase (`LabHistAmostras`), propriedade = coluna em PascalCase (`IdLabHistAmostras`). Ver [tools/gerador-csharp/README.md](../tools/gerador-csharp/README.md).
- Connection string só em **user-secrets** (`ConnectionStrings:LabControl`), nunca no `appsettings.json`.
- Services retornam `Resultado<T>` (sucesso/erro com mensagem); controllers não têm regra de negócio.
- Datas: `DateOnly` para `DATE`, `TimeOnly` para `TIME`.
- Nada de SQL concatenado: parâmetros sempre.
- Toda tela nova: atualizar o checklist em `docs/00-PLANO-EXECUCAO.md` e o "Status" do MD da tela.
