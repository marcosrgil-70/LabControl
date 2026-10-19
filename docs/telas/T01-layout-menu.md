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
- [ ] Menu respeita permissões.
- [ ] Empresa ativa visível em todas as páginas.
- [ ] Painel usa só dados da empresa ativa.

**Status:** ⬜ — base pronta na tarefa 0.8 (layout v2 sem sessão, CSS em `site.css`, menu só com Início); menu por permissão, empresa no topo e painel na 1.2.
