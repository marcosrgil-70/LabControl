# T00 — Login e Seleção de Empresa

**Fase:** 1 · **Rota:** `/Conta/Login`, `/Conta/Empresa`, `/Conta/TrocarSenha`, `/Conta/Sair` · **Permissão:** pública (login)
**Legado:** não existe no LabControl.exe — era o lançador externo `MAIS_Acesso` que chamava o EXE com `ParamStr(1)=funcionário, (2)=empresa, (3)=usuário`. Troca de empresa: menu *Utilitários → Ativar Empresa*.
**Tabelas:** `USUARIOS`, `USUARIOS_EMPRESAS`, `EMPRESAS`, `USUARIOS_PERFIS`, `PERFIS_PERMISSOES`, `PERMISSOES`.

## Referências cruzadas
- **Delphi:** não existe no LabControl.exe (lançador externo `MAIS_Acesso`); `uAcesso.pas` só valida os parâmetros recebidos
- **Objetos do banco:** [`USUARIOS`](../banco/objetos/tabelas/USUARIOS.md), [`USUARIOS_EMPRESAS`](../banco/objetos/tabelas/USUARIOS_EMPRESAS.md), [`EMPRESAS`](../banco/objetos/tabelas/EMPRESAS.md), [`USUARIOS_PERFIS`](../banco/objetos/tabelas/USUARIOS_PERFIS.md), [`PERFIS_PERMISSOES`](../banco/objetos/tabelas/PERFIS_PERMISSOES.md), [`PERMISSOES`](../banco/objetos/tabelas/PERMISSOES.md), [`AUDITORIA`](../banco/objetos/tabelas/AUDITORIA.md)
- **Processos:** [P02 Autenticação e permissões](../processos/P02-autenticacao-permissoes.md) · [P01 Multiempresa](../processos/P01-multiempresa.md)
- **Regras de negócio:** [RN-02 Segurança](../regras/RN-02-seguranca.md) · [RN-01 Gerais e multiempresa](../regras/RN-01-gerais-multiempresa.md)
- **Plano:** [00-PLANO-EXECUCAO.md](../00-PLANO-EXECUCAO.md)

## Fluxo
1. **Login**: campos `Login` (30) e `Senha`. Valida `ATIVO = 1`, `BLOQUEADO_ATE < agora`, senha via `PasswordHasher.VerifyHashedPassword`.
   - Falha: `TENTATIVAS_FALHAS + 1`; na 5ª, `BLOQUEADO_ATE = agora + 15 min`. Mensagem única "Usuário ou senha inválidos".
   - Sucesso: zera tentativas, grava `ULTIMO_ACESSO`.
2. **Troca de senha obrigatória** se `TROCAR_SENHA = 1` (mínimo 8 caracteres, letra e número; diferente da atual).
3. **Empresa**: lista `USUARIOS_EMPRESAS` ativas (+ todas se `ADMIN_GLOBAL`), pré-seleciona `ID_EMPRESAS_ULTIMA` ou `PADRAO`. Se houver **uma só**, entra direto.
4. Grava as claims: `UsuarioId`, `Login`, `Nome`, `EmpresaId`, `EmpresaCodigo`, `EmpresaNome`, `ProfissionalId` (`ID_ENTIDADES_FUNC` da empresa; pode ser nulo), `AdminGlobal`, e as permissões do usuário **naquela empresa** (união dos perfis).
5. Se `ProfissionalId` for nulo: aviso permanente no topo "Usuário sem profissional vinculado nesta empresa — não poderá receber amostras, lançar resultados nem gerar boletins" (essas telas exigem profissional).
6. **Trocar empresa** (menu do usuário no topo): volta ao passo 3 sem pedir senha.
7. **Sair**: encerra o cookie.

## Regras
- O nome da empresa ativa e o código aparecem sempre no topo (como `lbDBEmpresaAtiva` do Delphi).
- Expiração por inatividade: 8 h (configurável).
- Todos os logins (sucesso/falha) vão para `AUDITORIA` (`TABELA='USUARIOS'`, ação `L`).

## Erros da v1 a corrigir
- SHA-256 sem salt e comparação em maiúsculas → PBKDF2.
- Empresa fixa `?? 1` em controllers (`HttpContext.Session.GetInt32("EmpresaId") ?? 1`) → sem empresa na claim = redireciona para seleção.

## Aceite
- [ ] Usuário com 2 empresas escolhe a empresa; com 1 entra direto.
- [ ] Usuário migrado é obrigado a trocar a senha.
- [ ] 5 erros bloqueiam por 15 min.
- [ ] Trocar a empresa muda todos os dados exibidos (filtro global).

**Status:** ⬜ não iniciado
