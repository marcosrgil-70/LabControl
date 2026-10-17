# P02 — Autenticação, Sessão e Permissões

**Criticidade:** 🔴 · **Fase:** 1
**Telas:** [T00 Login](../telas/T00-login-empresa.md), [T01 Menu](../telas/T01-layout-menu.md), [T02 Usuários](../telas/T02-usuarios.md), [T04 Perfis](../telas/T04-perfis-permissoes.md), [T13 Profissionais](../telas/T13-profissionais.md)
**Objetos:** [`USUARIOS`](../banco/objetos/tabelas/USUARIOS.md), [`USUARIOS_EMPRESAS`](../banco/objetos/tabelas/USUARIOS_EMPRESAS.md), [`USUARIOS_PERFIS`](../banco/objetos/tabelas/USUARIOS_PERFIS.md), [`PERFIS`](../banco/objetos/tabelas/PERFIS.md), [`PERFIS_PERMISSOES`](../banco/objetos/tabelas/PERFIS_PERMISSOES.md), [`PERMISSOES`](../banco/objetos/tabelas/PERMISSOES.md), [`AUDITORIA`](../banco/objetos/tabelas/AUDITORIA.md)
**Regras:** [RN-02](../regras/RN-02-seguranca.md)

## Como era no legado
- Sem login próprio: o EXE recebia funcionário, empresa e usuário por parâmetro do lançador `MAIS_Acesso`.
- Direitos por usuário × empresa × componente (`SYS_*_ACESSOS`), aplicados por `unClassAcessos` (habilita/oculta menus e botões).
- Funcionário ativo = `USUARIO.ID_ENTIDADES_FUNC` (mesmo em todas as empresas).

## Fluxo de login
1. `POST /Conta/Login` (login, senha, antiforgery).
2. Usuário inexistente, inativo ou bloqueado (`BLOQUEADO_ATE > agora`) → mensagem única "Usuário ou senha inválidos" (não revelar qual).
3. `PasswordHasher.VerifyHashedPassword` falhou → `TENTATIVAS_FALHAS + 1`; na 5ª → `BLOQUEADO_ATE = agora + 15 min`; `AUDITORIA` ação `L` (falha).
4. Sucesso → zera tentativas, `ULTIMO_ACESSO = agora`, `AUDITORIA` `L` (sucesso). Se o hash precisar de rehash, regravar.
5. `TROCAR_SENHA = 1` → `/Conta/TrocarSenha` (mín. 8, letra + número, diferente da atual) antes de qualquer outra tela.
6. Seleção de empresa ([P01](P01-multiempresa.md)); uma só → entra direto; grava `ID_EMPRESAS_ULTIMA`.
7. Claims: `UsuarioId`, `Login`, `Nome`, `EmpresaId`, `EmpresaCodigo`, `EmpresaNome`, `ProfissionalId` (`USUARIOS_EMPRESAS.ID_ENTIDADES_FUNC`), `AdminGlobal`, `Permissao` (uma claim por código, união dos perfis ativos da empresa).
8. Cookie: HttpOnly, Secure, SameSite=Lax, expiração deslizante 8 h.

## Autorização
- `[Permissao("AMOSTRAS.INCLUIR")]` (filtro de autorização) em cada action; sem a claim → 403. ADMIN_GLOBAL passa sempre.
- Menu e botões renderizados só com a permissão (tag helper `<permissao codigo="…">`).
- Telas que gravam como profissional (recebimento, resultados, boletim, movimentação, descarte) exigem `ProfissionalId` → sem ele, aviso e bloqueio da ação.

## Profissional do usuário
- Vínculo por empresa (`UQ_USUEMP_FUNC`: um profissional ↔ um usuário por empresa), editável na T02 e na T13 (mesmo serviço).
- `ProfissionalId` vai para: `ID_ENTIDADES_FUNC_DIG` (amostra/resultado), `ID_ENTIDADES_FUNC` (proposta e movimento), `ID_ENTIDADES_FUNC_GER/EMI` (boletim, RE), `ID_ENTIDADES_FUNC_DESCARTE`.

## Variáveis de sessão MySQL
`SessaoMySqlInterceptor` em **toda** abertura de conexão: `SET @LC_ID_USUARIOS = {UsuarioId}, @LC_MIGRACAO = 0, @LC_PERMITE_RETROATIVO = 0;` (o pool reutiliza conexões — sempre zerar).

## Cenários de teste
- 5 senhas erradas bloqueiam por 15 min; a 6ª tentativa correta dentro do prazo é recusada.
- Usuário migrado obrigado a trocar a senha.
- Sem `AMOSTRAS.INCLUIR`: botão oculto e POST 403.
- Usuário sem profissional não grava amostra.
- `/security-review` ao final da fase 1.
