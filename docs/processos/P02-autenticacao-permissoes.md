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
- Implementado na tarefa 1.2: `Infra/Permissao.cs` (`PermissaoAttribute` → 403 "Acesso negado" ou JSON em AJAX; `PermissaoTagHelper`), menu por permissão em `Infra/Menu.cs` — ver [T01 › Implementação](../telas/T01-layout-menu.md#implementação-tarefa-12). O cookie manda `Forbid` para `/Conta/AcessoNegado` (também 403).

## Profissional do usuário
- Vínculo por empresa (`UQ_USUEMP_FUNC`: um profissional ↔ um usuário por empresa), editável na T02 e na T13 (mesmo serviço).
- `ProfissionalId` vai para: `ID_ENTIDADES_FUNC_DIG` (amostra/resultado), `ID_ENTIDADES_FUNC` (proposta e movimento), `ID_ENTIDADES_FUNC_GER/EMI` (boletim, RE), `ID_ENTIDADES_FUNC_DESCARTE`.

## Variáveis de sessão MySQL
`SessaoMySqlInterceptor` em **toda** abertura de conexão: `SET @LC_ID_USUARIOS = {UsuarioId}, @LC_MIGRACAO = 0, @LC_PERMITE_RETROATIVO = 0;` (o pool reutiliza conexões — sempre zerar).

## Implementação (tarefa 1.1)
| Peça | Arquivo | O que faz |
|---|---|---|
| Serviço | `Services/ContaService.cs` | `EntrarAsync` (login em maiúsculas; mensagem única; hash fictício quando o usuário não existe, para o tempo não revelar o login; falha = um `UPDATE` atômico que incrementa e bloqueia na 5ª; rehash quando necessário; evento L de sucesso/falha com o motivo, nunca a senha), `TrocarSenhaAsync`, `EmpresasDoUsuarioAsync` (pré-seleção: última → padrão), `MontarIdentidadeAsync` (claims), `SessaoValidaAsync` |
| Tela | `Controllers/ContaController.cs`, `Views/Conta/*`, `Views/Shared/_LayoutConta.cshtml` | Login → troca obrigatória → empresa (uma só: entra direto; nenhuma: recusa) → Home; trocar empresa pelo topo sem senha; Sair por POST |
| Cookie | `Program.cs` | `LabControl.Auth`: HttpOnly, Secure, SameSite=Lax, 8 h deslizantes (`Autenticacao:ExpiracaoHoras`); política de fallback = autenticado (só o login, a página de erro e os estáticos são anônimos); antiforgery automático em todo POST |
| Etapas obrigatórias | `Infra/ExigirEmpresaMiddleware.cs` | claim `lc:trocar_senha` → só TrocarSenha/Sair; sem empresa → `/Conta/Empresa` |
| Revalidação | `Infra/ValidarSessao.cs` | a cada request: usuário ativo, `ADMIN_GLOBAL` igual ao do cookie (mudou no banco → sai; desde a 1.7) e vínculo/empresa ativos, senão encerra a sessão. Perfis/permissões alterados valem na próxima entrada (carimbo de segurança registrado como melhoria). **O bloqueio por tentativas não derruba sessão aberta** (errar 5 vezes a senha de alguém não pode encerrar a sessão dessa pessoa) |
| Claims | `Infra/EmpresaContexto.cs` (`LabClaims`) | `UsuarioId`, `Name` (nome), `lc:login`, `lc:empresa`, `lc:empresa_codigo`, `lc:empresa_nome`, `lc:profissional`, `lc:admin_global`, `lc:trocar_senha`, `lc:permissao` (uma por código); `IEmpresaContexto.TemPermissao(codigo)` |
| Topo | `Views/Shared/_Layout.cshtml` | código + nome da empresa (link para trocar), menu do usuário (trocar empresa/senha, sair) e aviso "sem profissional vinculado" |
| Admin de desenvolvimento | `Scripts/v2/dev/admin_dev.sql` (`recriar.ps1 -Dev`) | empresa `001` + usuário `ADMINISTRADOR` / senha `Admin123` (ADMIN_GLOBAL, sem troca obrigatória, profissional "USUÁRIO ESPECIAL DO SISTEMA"; também na migração com `Desenvolvimento:AdministradorComSenhaPadrao` — CLAUDE.md). Nunca em produção: lá os usuários vêm da migração (M-09) com a senha provisória |

- As permissões são lidas na entrada/troca de empresa: mudança de perfil vale a partir da próxima entrada ou troca de empresa.
- Ainda não há empresa ativa quando as permissões são montadas: `PERFIS_PERMISSOES` é lido com `IgnoreQueryFilters()` e a empresa **conferida** fixada no `Where` (exceção registrada no [P01](P01-multiempresa.md)).
- A troca de senha é auditada pelo `SaveChanges` (A em `USUARIOS`, `SENHA_HASH = "***"`); o contador de falhas e o último acesso são gravados por SQL direto, cobertos pelo evento L.
- Produção (F9): chaves do Data Protection persistidas (senão o cookie se perde a cada reinício do servidor).

## Cenários de teste
- 5 senhas erradas bloqueiam por 15 min; a 6ª tentativa correta dentro do prazo é recusada.
- Usuário migrado obrigado a trocar a senha.
- Sem `AMOSTRAS.INCLUIR`: botão oculto e POST 403.
- Usuário sem profissional não grava amostra.
- `/security-review` ao final da fase 1.
