# T01 — Layout, Menu e Home

**Fase:** 1 · **Legado:** `un_Menu.pas` (MDI com `TAdvMainMenu`) · **Arquivo:** `Views/Shared/_Layout.cshtml`

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

**Status:** ⬜ (o layout v1 existe e será adaptado)
