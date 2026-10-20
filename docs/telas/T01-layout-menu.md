# T01 — Layout, Menu e Home

**Fase:** 1 · **Legado:** `un_Menu.pas` (MDI com `TAdvMainMenu`) · **Arquivo:** `Views/Shared/_Layout.cshtml`

## Referências cruzadas
- **Delphi:** `un_Menu.pas`/`.dfm` (TAdvMainMenu, MDI), `un_About.pas` (Sobre o sistema)
- **Objetos do banco:** [`VW_LAB_TESTES_PENDENTES`](../banco/objetos/views/VW_LAB_TESTES_PENDENTES.md), [`VW_LAB_BOLETINS`](../banco/objetos/views/VW_LAB_BOLETINS.md), [`VW_LAB_AMOSTRAS`](../banco/objetos/views/VW_LAB_AMOSTRAS.md), [`VW_LAB_PROPOSTAS`](../banco/objetos/views/VW_LAB_PROPOSTAS.md), [`PERMISSOES`](../banco/objetos/tabelas/PERMISSOES.md)
- **Processos:** [P02 Autenticação e permissões](../processos/P02-autenticacao-permissoes.md) · [P01 Multiempresa](../processos/P01-multiempresa.md)
- **Regras de negócio:** [RN-02 Segurança](../regras/RN-02-seguranca.md)
- **Plano:** [00-PLANO-EXECUCAO.md](../00-PLANO-EXECUCAO.md)

## Menu (espelha o Delphi, reorganizado)

| Seção | Itens (rota) | Permissão |
|---|---|---|
| Início | Painel (`/`) | — |
| Laboratório | Propostas (`/Propostas`), Pedidos de Venda (`/PedidosVenda`), Recebimento de Amostras (`/Amostras`), Movimentação (`/Movimentacao`), Localização (`/Localizacao`), Etiquetas (`/Etiquetas`), Resultados (`/Resultados`), Boletins (`/Boletins`), Relatório de Ensaio (`/RelatorioEnsaio`) | `*.CONSULTAR` |
| Cadastros | Clientes/Fornecedores, Profissionais, Produtos, Parâmetros de Análise, **Auxiliares** (submenu: todos da T10), Geografia (Países/Estados/Cidades) | idem |
| Relatórios | Faturamento, Amostras por período, Amostras por cliente, Parâmetros analisados, Tempo de análise, Produção por período | `RELATORIOS.CONSULTAR` |
| Sistema | Empresa (dados + parâmetros), Usuários, Perfis, Auditoria, Migração (só ADMIN_GLOBAL) | `EMPRESAS.*`, `USUARIOS.*`… |

Itens sem permissão **não aparecem** (Delphi: `vvo_Acesso.Menu_Acessos`). Itens do Delphi descartados: "Lado a lado/Cascata" (MDI), "Atualizar Componentes" e "Atualizar Direitos de Acesso" (substituídos por Perfis).

## Topo
Logo · **Empresa ativa (código + nome)** com "trocar" · nome do usuário/profissional · Sair · versão (`Sobre o Sistema`).

## Painel (Home) — novo, útil ao laboratório
- Amostras recebidas hoje / no mês.
- **Testes pendentes** (`VW_LAB_TESTES_PENDENTES`) com destaque para prazo vencido (`DIAS_PARA_PRAZO < 0`).
- Boletins não emitidos (`VW_LAB_BOLETINS` com `EMITIDO = 0 AND ULTIMA_REVISAO = 1`).
- Propostas aguardando cliente.
Todos os números são links para a tela filtrada.

## Aceite
- [x] Menu respeita permissões.
- [x] Empresa ativa visível em todas as páginas.
- [x] Painel usa só dados da empresa ativa.

Testes: `tests/LabControl.Tests/PermissoesMenuPainelTests.cs` (403 com/sem permissão, AJAX, ADMIN_GLOBAL, menu, tag helper, painel por empresa e por perfil) e `LayoutTests.cs`.

## Implementação (tarefa 1.2)
- **Menu** — `Infra/Menu.cs` (catálogo acima, com a permissão de cada item) + `MenuViewComponent`: o item aparece se o usuário tem a permissão **e** a tela já existe (`TelasExistentes`: controller/action registrados). O menu cresce sozinho a cada tela reescrita, sem link quebrado; seção sem itens some. Ao criar uma tela, confira se a rota e a permissão do item no catálogo batem com o controller.
- **Permissão** — `[Permissao("TELA.ACAO")]` (`Infra/Permissao.cs`) na classe ou na action: sem a permissão → 403 com a página "Acesso negado" (que mostra o código exigido) ou JSON `{ erro }` em AJAX; anônimo → login. Tag helper `<permissao codigo="…">…</permissao>` ou `lc-permissao="…"` num elemento para esconder botões (esconder **não** dispensa o atributo na action).
- **Topo** — empresa (código + nome, link para trocar), menu do usuário e aviso "sem profissional" (feitos na 1.1).
- **Painel** — `Services/PainelService.cs`: amostras recebidas hoje/no mês (revisão 0), testes pendentes (com vencidos), boletins não emitidos (última revisão), propostas aguardando cliente (status "enviada ao cliente" dos parâmetros da empresa). Cada contador só com a permissão `*.CONSULTAR` da tela; o número vira link quando a tela existir.
- **Sobre o sistema** — versão (`<Version>` do `LabControl.csproj`) no rodapé do menu (`Infra/Versao.cs`).

**Status:** ✅ concluída na tarefa 1.2 (19/10/2026).
