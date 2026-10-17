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
- [ ] Senha provisória exibida uma vez e troca obrigatória no 1º acesso.
- [ ] Não é possível vincular o mesmo profissional a dois usuários na mesma empresa (mensagem clara).
- [ ] Usuário sem `USUARIOS.INCLUIR` não vê o botão e recebe 403 ao postar.
- [ ] Alterações aparecem na Auditoria (T06).

**Status:** ⬜ (v1 tem Usuários sem empresa — refazer)
