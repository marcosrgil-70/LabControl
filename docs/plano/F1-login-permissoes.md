# Fase 1 — Login, Multiempresa, Permissões e Empresa

**Objetivo:** usuário entra, escolhe a empresa, vê só o que pode e o sistema sabe qual profissional ele representa; empresa e parâmetros configuráveis.
**Modelo/modo:** 🟢 Sonnet (📐 plan mode antes de 1.1) · ✋ modo padrão (segurança) · 1.5 em 🔵 Opus.
**Ler antes:** [P02](../processos/P02-autenticacao-permissoes.md) · [P01](../processos/P01-multiempresa.md) · [P23](../processos/P23-nova-empresa.md) · [RN-02](../regras/RN-02-seguranca.md) · telas [T00](../telas/T00-login-empresa.md), [T01](../telas/T01-layout-menu.md), [T02](../telas/T02-usuarios.md), [T03](../telas/T03-empresa-usuaria.md), [T04](../telas/T04-perfis-permissoes.md), [T05](../telas/T05-parametros-empresa.md)
**Depende de:** Fase 0.

---

### 1.1 T00 — Login, troca de senha, seleção/troca de empresa
> ✅ **Concluída em 19/10/2026** — aceite da T00 coberto por `ContaServiceTests` e `ContaFluxoTests`; ver [P02 › Implementação](../processos/P02-autenticacao-permissoes.md#implementação-tarefa-11). Admin de desenvolvimento: `.\Scripts\v2\recriar.ps1 -Dev` → `ADMIN` / `Admin123` (troca obrigatória).
- **Passos:** `ContaController` + `ContaService`; Cookie Authentication; PBKDF2; bloqueio 5 tentativas/15 min; troca obrigatória; seleção de empresa com claims (`EmpresaId`, `ProfissionalId`, permissões); trocar empresa; sair; auditoria L. Criar o primeiro usuário ADMIN_GLOBAL por script de seed de desenvolvimento (fora do `05_seed.sql`).
- **Aceite:** itens do [aceite da T00](../telas/T00-login-empresa.md#aceite).
- **Esforço:** M

### 1.2 T01 — Layout, menu por permissão e painel
> ✅ **Concluída em 19/10/2026** — ver [T01 › Implementação](../telas/T01-layout-menu.md#implementação-tarefa-12); aceite em `PermissoesMenuPainelTests`.
- **Passos:** `_Layout` com empresa ativa no topo, aviso "sem profissional vinculado", menu da T01 com itens filtrados por `[Permissao]`; atributo `PermissaoAttribute` + tag helper; painel com contadores (pode começar com 2: recebidas hoje e testes pendentes).
- **Aceite:** menu respeita permissões; empresa visível em todas as páginas.
- **Esforço:** M · **Depende de:** 1.1

### 1.3 T02 — Usuários
> ✅ **Concluída em 19/10/2026** — ver [T02 › Implementação](../telas/T02-usuarios.md#implementação-tarefa-13); aceite em `UsuarioServiceTests`.
- **Passos:** CRUD + grid de empresas (profissional vinculado, perfis, padrão, ativo); resetar senha; desbloquear; inativar; serviço único de vínculo usuário ↔ profissional (reusado na T13).
- **Aceite:** [aceite da T02](../telas/T02-usuarios.md#critérios-de-aceite).
- **Esforço:** M · **Depende de:** 1.1

### 1.4 T04 — Perfis e permissões
> ✅ **Concluída em 19/10/2026** — ver [T04 › Implementação](../telas/T04-perfis-permissoes.md#implementação-tarefa-14); perfis sugeridos confirmados pelo usuário (dúvida 4) e criados pelo `SP_INICIALIZAR_EMPRESA`.
- **Passos:** CRUD de perfis com matriz `TELA × AÇÃO` a partir de `PERMISSOES`; copiar perfil; proteger ADMINISTRADOR. Criar os perfis sugeridos após confirmação do usuário (dúvida 4).
- **Aceite:** [aceite da T04](../telas/T04-perfis-permissoes.md#critérios-de-aceite).
- **Esforço:** M · **Depende de:** 1.2

### 1.5 T03 — Empresa usuária + nova empresa + cópia de cadastros 🔵
- **Passos:** dados da empresa ativa (com logo e componente de endereço — pode usar combo de cidade simples até a fase 2); lista de empresas e "nova empresa" (ADMIN_GLOBAL) com `SP_INICIALIZAR_EMPRESA` e `CopiarCadastros` ([P23](../processos/P23-nova-empresa.md)).
- **Aceite:** [aceite da T03](../telas/T03-empresa-usuaria.md#critérios-de-aceite).
- **Esforço:** G · **Depende de:** 1.3

### 1.6 T05 — Parâmetros da empresa + numeração
- **Passos:** aba parâmetros (status padrão, dias retroativos, validade, pesquisa, 2º responsável, textos padrão); aba numeração somente leitura + ajuste ADMIN_GLOBAL auditado.
- **Aceite:** [aceite da T05](../telas/T05-parametros-empresa.md#critérios-de-aceite).
- **Esforço:** M · **Depende de:** 1.5

### 1.7 Revisão da fase
`/security-review` + `/code-review`; testes de autorização (cada action com `[Permissao]` — teste de reflexão que falha se alguma action de escrita não tiver o atributo).

---

## Definição de pronto
- [ ] Login seguro, empresa ativa, permissões e profissional do usuário funcionando.
- [ ] Nova empresa nasce utilizável (status, perfis, parâmetros).
- [ ] Nenhuma action de escrita sem `[Permissao]`.
