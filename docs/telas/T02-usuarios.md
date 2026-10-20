# T02 — Usuários

**Fase:** 1 · **Rota:** `/Usuarios` · **Permissões:** `USUARIOS.CONSULTAR/INCLUIR/ALTERAR/EXCLUIR` · **Criticidade:** 🟠
**Legado:** `LabControl/un_DireitoAcesso01.pas` (cadastro de usuário + direitos por menu/form/componente) e `un_DirAcesso01.pas`; o login era feito pelo lançador externo `MAIS_Acesso`. Menu *Utilitários → Atualizar Direitos de Acesso*.
**Objetos do banco:** [`USUARIOS`](../banco/objetos/tabelas/USUARIOS.md), [`USUARIOS_EMPRESAS`](../banco/objetos/tabelas/USUARIOS_EMPRESAS.md), [`USUARIOS_PERFIS`](../banco/objetos/tabelas/USUARIOS_PERFIS.md), [`PERFIS`](../banco/objetos/tabelas/PERFIS.md), [`VW_PROFISSIONAIS`](../banco/objetos/views/VW_PROFISSIONAIS.md), [`AUDITORIA`](../banco/objetos/tabelas/AUDITORIA.md)
**Processos:** [P02 Autenticação e permissões](../processos/P02-autenticacao-permissoes.md) · [P01 Multiempresa](../processos/P01-multiempresa.md) · **Regras:** [RN-02 Segurança](../regras/RN-02-seguranca.md)

## Como era no Delphi
- Um form com abas "Usuário" (código, nome/login, senha + confirmação — *"A Confirmação da Senha não confere com a Senha digitada."*, flag administrador) e "Direitos" (árvore de menus/forms/componentes com habilitado/visível/incluir/alterar/excluir, **por empresa**).
- *"O Administrador tem seus direitos já definidos."*: o usuário administrador (`USUADM`) não tinha direitos editáveis.
- O vínculo com o funcionário ficava em `USUARIO.ID_ENTIDADES_FUNC` (um funcionário só, para todas as empresas).
- Senha com hash proprietário (`unClassCriptografia`) — **não recuperável** na migração.

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Login* | `LOGIN` | até 30, único global, sem espaços, gravado em maiúsculas |
| Nome* | `NOME` | até 100 |
| E-mail | `EMAIL` | formato válido (opcional) |
| Ativo | `ATIVO` | inativo não consegue logar |
| Administrador global | `ADMIN_GLOBAL` | só outro ADMIN_GLOBAL pode marcar/desmarcar; ignora permissões e acessa todas as empresas |
| Trocar senha no próximo acesso | `TROCAR_SENHA` | marcado ao criar e ao resetar |
| Bloqueado até | `BLOQUEADO_ATE` | só leitura + ação "Desbloquear" |
| Último acesso | `ULTIMO_ACESSO` | só leitura |

### Grid "Empresas do usuário" (`USUARIOS_EMPRESAS` + `USUARIOS_PERFIS`)
| Coluna | Regra |
|---|---|
| Empresa* | empresas ativas (ADMIN_GLOBAL vê todas; ADMINISTRADOR de empresa só a sua) |
| Profissional vinculado | combo `VW_PROFISSIONAIS` **da empresa da linha**, ativos e **sem outro usuário** (`UQ_USUEMP_FUNC`) |
| Perfis* (multi) | `PERFIS` ativos da empresa da linha |
| Padrão | uma empresa padrão por usuário (pré-seleção no login) |
| Ativo | acesso à empresa suspenso sem apagar o histórico |

## Ações
| Ação | Regra |
|---|---|
| Incluir | gera senha provisória forte, exibida **uma vez** (copiar), `TROCAR_SENHA = 1` |
| Alterar | dados + grid de empresas/perfis/profissional |
| Resetar senha | nova senha provisória exibida uma vez; zera tentativas e bloqueio; grava `AUDITORIA` |
| Desbloquear | `TENTATIVAS_FALHAS = 0`, `BLOQUEADO_ATE = NULL` |
| Inativar | preferido em vez de excluir |
| Excluir | só se o usuário nunca gravou nada (`CRIADO_POR`/`ALTERADO_POR` em qualquer tabela, `AUDITORIA`); senão inativar |

## Regras
- O profissional vinculado é quem aparece como **digitador** (amostra, resultado, proposta) e **gerador** do boletim (`ProfissionalId` da claim, ver T00).
- O mesmo vínculo pode ser feito pela T13 (campo "Usuário do sistema"): **um serviço só** (`VinculoUsuarioProfissionalService`) para as duas telas.
- Toda alteração de usuário, perfil ou vínculo grava `AUDITORIA` (antes/depois).
- Usuário não pode remover o próprio ADMIN_GLOBAL nem a própria última empresa ativa.

## Erros da v1 a corrigir
- Usuários sem empresa e senha SHA-256 sem salt → PBKDF2 + `USUARIOS_EMPRESAS`.

## Critérios de aceite
- [x] Senha provisória exibida uma vez e troca obrigatória no 1º acesso.
- [x] Não é possível vincular o mesmo profissional a dois usuários na mesma empresa (mensagem clara).
- [x] Usuário sem `USUARIOS.INCLUIR` não vê o botão e recebe 403 ao postar.
- [x] Alterações aparecem na Auditoria (T06) — gravadas em `AUDITORIA` (`USUARIOS`, `USUARIOS_EMPRESAS`, `USUARIOS_PERFIS`); a tela T06 é da Fase 8.

Testes: `tests/LabControl.Tests/UsuarioServiceTests.cs`.

## Implementação (tarefa 1.3)
- `Controllers/UsuariosController.cs` (`[Permissao]` por ação) · `Services/UsuarioService.cs` · `Services/VinculoUsuarioProfissionalService.cs` (o serviço único do vínculo, a reutilizar na T13) · `Views/Usuarios/*`.
- **Quem vê e edita o quê** (`USUARIOS` é global, o acesso é por empresa):
  - ADMIN_GLOBAL: todos os usuários e todas as empresas (grid com uma linha por empresa ativa);
  - administrador de empresa (`USUARIOS.*`, sem ADMIN_GLOBAL): só usuários com acesso à empresa ativa e só a linha dessa empresa; linhas de outras empresas no POST são ignoradas;
  - **dados globais** (login, nome, e-mail, ativo, padrão, resetar senha, excluir) só se o usuário tiver acesso **apenas** à empresa ativa e não for ADMIN_GLOBAL — senão um administrador local mudaria (ou descobriria a senha de) um usuário de outra empresa. Desbloquear é permitido.
- Senha provisória de 12 caracteres (letras e números, sem ambíguos), mostrada **na própria resposta** do POST (sem redirect/TempData, `Cache-Control: no-store`) com botão "Copiar".
- Profissional: combo com os profissionais ativos da empresa da linha; os já vinculados a outro usuário aparecem desabilitados com o login; recusa com "Este profissional já está vinculado a outro usuário nesta empresa." (e o `UQ_USUEMP_FUNC` continua valendo).
- Perfis: ao menos um perfil ativo por empresa marcada (exceto para ADMIN_GLOBAL).
- Proteções: não inativar o próprio usuário, não remover o próprio ADMIN_GLOBAL nem o próprio acesso à empresa ativa, não se excluir.
- Excluir: só quem nunca gravou nada — sem `AUDITORIA` como autor, sem `LAB_HIST_AMOSTRAS_LOG` e sem `CRIADO_POR`/`ALTERADO_POR` em nenhuma tabela (busca no `information_schema`); senão "inative-o em vez de excluir".
- `CRIADO_POR`/`ALTERADO_POR` passaram a ser preenchidos pelo `LabDbContext` (usuário do request) em toda inclusão/alteração real.

> **20/10/2026 (relato do usuário: usuário recém-cadastrado não aparecia no combo do profissional):** o combo "Usuário do sistema" da T13 lista só usuários com acesso à empresa ativa, e a inclusão feita por ADMIN_GLOBAL vinha **sem nenhuma empresa marcada** (o usuário foi gravado sem `USUARIOS_EMPRESAS`). Agora o novo usuário já vem com a **empresa ativa marcada e como padrão** (também para ADMIN_GLOBAL; desmarca quem não quiser) e a T13 explica no campo onde dar o acesso. Teste: `UsuarioServiceTests.Novo_usuario_ja_vem_com_a_empresa_ativa_e_aparece_no_combo_do_profissional`.

**Status:** ✅ concluída na tarefa 1.3 (19/10/2026).

> **Teste no navegador (20/10/2026, [P26](../processos/P26-testes-ponta-a-ponta.md)):** `Usuario_crud_com_senha_provisoria` — incluir com perfil (senha provisória), pesquisar, alterar, resetar senha, excluir. Botão da lista com texto ("Alterar").
