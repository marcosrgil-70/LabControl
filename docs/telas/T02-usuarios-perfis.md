# T02 — Usuários, Perfis e Permissões

**Fase:** 1 · **Rotas:** `/Usuarios`, `/Perfis` · **Permissões:** `USUARIOS.*`, `PERFIS.*`
**Legado:** `un_DireitoAcesso01.pas` + `unConfiguracaoAcesso` (MAIS_Acesso) e tabelas `SYS_*_ACESSOS` (direito por usuário × empresa × componente).
**Tabelas:** `USUARIOS`, `USUARIOS_EMPRESAS`, `USUARIOS_PERFIS`, `PERFIS`, `PERFIS_PERMISSOES`, `PERMISSOES`.

## Usuários
| Campo | Coluna | Regras |
|---|---|---|
| Login | `LOGIN` | obrigatório, único global, sem espaços, maiúsculas |
| Nome | `NOME` | obrigatório |
| E-mail | `EMAIL` | opcional, formato válido |
| Ativo | `ATIVO` | — |
| Administrador global | `ADMIN_GLOBAL` | só editável por outro ADMIN_GLOBAL |
| Trocar senha no próximo acesso | `TROCAR_SENHA` | marcado ao criar/resetar |
| **Empresas** (grid) | `USUARIOS_EMPRESAS` | por linha: empresa, **Profissional vinculado** (combo de `VW_PROFISSIONAIS` da empresa sem outro usuário), padrão, ativo, perfis (multi) |

Ações: Incluir, Alterar, **Resetar senha** (gera senha provisória exibida uma vez), Bloquear/Desbloquear, Inativar (não excluir se houver registros com `CRIADO_POR`).

### Vínculo Usuário ↔ Profissional (exigência do usuário)
- Um profissional (entidade com `TIPO_FUNCIONARIO = 1` e linha em `ENTIDADES_FUNCIONARIOS`) pode estar ligado a **no máximo um** usuário por empresa (`UQ_USUEMP_FUNC`).
- O vínculo também pode ser feito pela tela de Profissionais (T13), campo "Usuário do sistema".
- É o profissional vinculado que vai para `ID_ENTIDADES_FUNC_DIG` (amostra/resultado), `ID_ENTIDADES_FUNC_GER` (boletim) e "Digitador" da proposta.

## Perfis (por empresa)
- Nome, descrição, ativo + **matriz de permissões**: linhas = telas (agrupadas por módulo), colunas = Consultar/Incluir/Alterar/Excluir/Imprimir + ações especiais (Revisar, Emitir, Gerar pedido, Entrega retroativa, Desfazer revisão).
- "Copiar perfil" (de outra empresa ou da mesma).
- O perfil `ADMINISTRADOR` criado por `SP_INICIALIZAR_EMPRESA` não pode ser excluído.

## Aceite
- [ ] Usuário sem `AMOSTRAS.INCLUIR` não vê o botão Incluir nem consegue postar (403).
- [ ] Profissional vinculado aparece como digitador nos registros.
- [ ] Não é possível vincular o mesmo profissional a dois usuários na mesma empresa.

**Status:** ⬜ (v1 tem Usuários/Perfis/Permissões sem empresa — refazer)
