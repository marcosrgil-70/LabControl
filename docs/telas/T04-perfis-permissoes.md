# T04 — Perfis e Permissões

**Fase:** 1 · **Rota:** `/Perfis` · **Permissões:** `PERFIS.CONSULTAR/INCLUIR/ALTERAR/EXCLUIR` · **Criticidade:** 🟠
**Legado:** direitos por usuário × empresa × componente em `SYS_MODULOS_ACESSOS` (30), `SYS_MENUS_ACESSOS` (825), `SYS_FORMS_ACESSOS` (645), `SYS_COMPONENTES_ACESSOS` (3.690), aplicados por `Library/unClassAcessos.pas` (habilita/oculta menu e botões). Triggers `SYS_*_AI0` criavam direitos para todos os usuários a cada tela nova; menu *Utilitários → Atualizar Componentes* recarregava o catálogo.
**Objetos do banco:** [`PERFIS`](../banco/objetos/tabelas/PERFIS.md), [`PERFIS_PERMISSOES`](../banco/objetos/tabelas/PERFIS_PERMISSOES.md), [`PERMISSOES`](../banco/objetos/tabelas/PERMISSOES.md), [`USUARIOS_PERFIS`](../banco/objetos/tabelas/USUARIOS_PERFIS.md)
**Processos:** [P02 Autenticação e permissões](../processos/P02-autenticacao-permissoes.md) · **Regras:** [RN-02 Segurança](../regras/RN-02-seguranca.md)

## Objetivo
Substituir milhares de linhas de direito por componente por **perfis por empresa** com uma matriz de permissões `TELA.ACAO` (catálogo fixo carregado pelo `05_seed.sql`).

## Campos do perfil
| Campo | Coluna | Regra |
|---|---|---|
| Nome* | `NOME` | até 60, único por empresa |
| Descrição | `DESCRICAO` | até 200 |
| Ativo | `ATIVO` | perfil inativo não concede permissões |

## Matriz de permissões
- Linhas = telas (`PERMISSOES.TELA`), agrupadas por módulo (`SISTEMA`, `CADASTROS`, `LABORATORIO`, `RELATORIOS`) na ordem de `PERMISSOES.ORDEM`.
- Colunas = Consultar · Incluir · Alterar · Excluir · Imprimir · **ações especiais** (Revisar, Emitir, Gerar pedido, Cancelar pedido, Entrega retroativa, Desfazer revisão) — célula vazia quando a ação não existe para a tela.
- Atalhos: marcar linha inteira, marcar coluna inteira, "somente consulta".
- Ao marcar qualquer ação, marcar também `CONSULTAR` da tela (sem consultar não se chega à tela).

## Ações
| Ação | Regra |
|---|---|
| Incluir / Alterar | nome + matriz, gravação em transação (apaga e regrava `PERFIS_PERMISSOES`) |
| Copiar perfil | da mesma empresa ou de outra (ADMIN_GLOBAL) — novo nome obrigatório |
| Excluir | proibido para o perfil `ADMINISTRADOR` (criado por `SP_INICIALIZAR_EMPRESA`) e para perfis com usuários (remover dos usuários antes ou inativar) |

## Perfis sugeridos (confirmar com o usuário — dúvida 4 do plano)
| Perfil | Permissões principais |
|---|---|
| ADMINISTRADOR | todas (criado automaticamente) |
| RECEPÇÃO | Clientes, Propostas (consulta), Amostras (incluir/alterar/imprimir), Etiquetas, Movimentação |
| ANALISTA | Amostras (consulta), Resultados (incluir/alterar), fila de análises |
| RESPONSÁVEL TÉCNICO | Resultados (revisar), Boletins (todas, inclusive EMITIR/REVISAR) |
| COMERCIAL | Clientes, Produtos, Propostas (todas), Pedidos de venda (todas), Relatórios |

## Regras
- Permissões efetivas do usuário na empresa = **união** dos perfis ativos dele naquela empresa; `ADMIN_GLOBAL` ignora tudo.
- Mudança de perfil vale no próximo login ou troca de empresa (as permissões vão para as claims).
- Toda alteração grava `AUDITORIA`.
- Nova tela no sistema = nova linha no `05_seed.sql` (o catálogo não é editável pela tela).

## Critérios de aceite
- [ ] Perfil sem `AMOSTRAS.INCLUIR`: botão oculto e POST retorna 403.
- [ ] Copiar perfil entre empresas.
- [ ] ADMINISTRADOR não pode ser excluído.

**Status:** ⬜ (v1 tem Perfis/Permissões sem empresa — refazer)
