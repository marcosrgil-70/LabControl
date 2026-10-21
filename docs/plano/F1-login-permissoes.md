# Fase 1 — Login, Multiempresa, Permissões e Empresa

**Objetivo:** usuário entra, escolhe a empresa, vê só o que pode e o sistema sabe qual profissional ele representa; empresa e parâmetros configuráveis.
**Modelo/modo:** 🟢 Sonnet (📐 plan mode antes de 1.1) · ✋ modo padrão (segurança) · 1.5 em 🔵 Opus.
**Ler antes:** [P02](../processos/P02-autenticacao-permissoes.md) · [P01](../processos/P01-multiempresa.md) · [P23](../processos/P23-nova-empresa.md) · [RN-02](../regras/RN-02-seguranca.md) · telas [T00](../telas/T00-login-empresa.md), [T01](../telas/T01-layout-menu.md), [T02](../telas/T02-usuarios.md), [T03](../telas/T03-empresa-usuaria.md), [T04](../telas/T04-perfis-permissoes.md), [T05](../telas/T05-parametros-empresa.md)
**Depende de:** Fase 0.

---

### 1.1 T00 — Login, troca de senha, seleção/troca de empresa
> ✅ **Concluída em 19/10/2026** — aceite da T00 coberto por `ContaServiceTests` e `ContaFluxoTests`; ver [P02 › Implementação](../processos/P02-autenticacao-permissoes.md#implementação-tarefa-11). Admin de desenvolvimento: `.\Scripts\v2\recriar.ps1 -Dev` → `ADMIN` / `Admin123` (troca obrigatória).
> 🔧 **Correção pós-teste (20/10/2026):** app reiniciado sempre pede login — claim `lc:instancia` por execução, conferida no `ValidarSessao` (antes o cookie de 8 h sobrevivia ao reinício). Teste `ContaFluxoTests.Cookie_de_outra_execucao_do_app_volta_para_o_login`. Ver [T00](../telas/T00-login-empresa.md).
- **Passos:** `ContaController` + `ContaService`; Cookie Authentication; PBKDF2; bloqueio 5 tentativas/15 min; troca obrigatória; seleção de empresa com claims (`EmpresaId`, `ProfissionalId`, permissões); trocar empresa; sair; auditoria L. Criar o primeiro usuário ADMIN_GLOBAL por script de seed de desenvolvimento (fora do `05_seed.sql`).
- **Aceite:** itens do [aceite da T00](../telas/T00-login-empresa.md#aceite).
- **Esforço:** M

### 1.2 T01 — Layout, menu por permissão e painel
> ✅ **Concluída em 19/10/2026** — ver [T01 › Implementação](../telas/T01-layout-menu.md#implementação-tarefa-12); aceite em `PermissoesMenuPainelTests`.
> 🔧 **Correção pós-teste (20/10/2026):** menu com um item só "Empresas Usuárias do Sistema"; *Cidades* fora do menu até a 2.3 (a action JSON `Geografia/Cidades` virou `CidadesDaUf`). Teste `PermissoesMenuPainelTests.Menu_real_sem_tela_inexistente_e_com_uma_entrada_de_empresa`.
- **Passos:** `_Layout` com empresa ativa no topo, aviso "sem profissional vinculado", menu da T01 com itens filtrados por `[Permissao]`; atributo `PermissaoAttribute` + tag helper; painel com contadores (pode começar com 2: recebidas hoje e testes pendentes).
- **Aceite:** menu respeita permissões; empresa visível em todas as páginas.
- **Esforço:** M · **Depende de:** 1.1

### 1.3 T02 — Usuários
> ✅ **Concluída em 19/10/2026** — ver [T02 › Implementação](../telas/T02-usuarios.md#implementação-tarefa-13); aceite em `UsuarioServiceTests`.
> 🔧 **Correção pós-teste (20/10/2026):** novo usuário já vem com a empresa ativa marcada e padrão (também para ADMIN_GLOBAL) — sem isso não aparecia no combo do profissional (T13). Ver [T02](../telas/T02-usuarios.md).
- **Passos:** CRUD + grid de empresas (profissional vinculado, perfis, padrão, ativo); resetar senha; desbloquear; inativar; serviço único de vínculo usuário ↔ profissional (reusado na T13).
- **Aceite:** [aceite da T02](../telas/T02-usuarios.md#critérios-de-aceite).
- **Esforço:** M · **Depende de:** 1.1

### 1.4 T04 — Perfis e permissões
> ✅ **Concluída em 19/10/2026** — ver [T04 › Implementação](../telas/T04-perfis-permissoes.md#implementação-tarefa-14); perfis sugeridos confirmados pelo usuário (dúvida 4) e criados pelo `SP_INICIALIZAR_EMPRESA`.
- **Passos:** CRUD de perfis com matriz `TELA × AÇÃO` a partir de `PERMISSOES`; copiar perfil; proteger ADMINISTRADOR. Criar os perfis sugeridos após confirmação do usuário (dúvida 4).
- **Aceite:** [aceite da T04](../telas/T04-perfis-permissoes.md#critérios-de-aceite).
- **Esforço:** M · **Depende de:** 1.2

### 1.5 T03 — Empresa usuária + nova empresa + cópia de cadastros 🔵
> ✅ **Concluída em 19/10/2026** — ver [T03 › Implementação](../telas/T03-empresa-usuaria.md#implementação-tarefa-15); aceite em `EmpresaServiceTests` (o logo no PDF do boletim fica para a 5.4).
> 🔧 **Correção pós-teste (20/10/2026):** *Empresa* (`/Empresa/Dados`) e *Empresas* (`/Empresas`) eram o mesmo cadastro → um item **"Empresas Usuárias do Sistema"** (ADMIN_GLOBAL vê a lista; os demais caem na empresa ativa); títulos e mensagens "empresa usuária". Ver [T03 › Nomenclatura](../telas/T03-empresa-usuaria.md#nomenclatura-20102026--pedido-do-usuário).
- **Passos:** dados da empresa ativa (com logo e componente de endereço — pode usar combo de cidade simples até a fase 2); lista de empresas e "nova empresa" (ADMIN_GLOBAL) com `SP_INICIALIZAR_EMPRESA` e `CopiarCadastros` ([P23](../processos/P23-nova-empresa.md)).
- **Aceite:** [aceite da T03](../telas/T03-empresa-usuaria.md#critérios-de-aceite).
- **Esforço:** G · **Depende de:** 1.3

### 1.6 T05 — Parâmetros da empresa + numeração
> ✅ **Concluída em 20/10/2026** — ver [T05 › Implementação](../telas/T05-parametros-empresa.md#implementação-tarefa-16); aceite em `ParametrosEmpresaServiceTests`.
- **Passos:** aba parâmetros (status padrão, dias retroativos, validade, pesquisa, 2º responsável, textos padrão); aba numeração somente leitura + ajuste ADMIN_GLOBAL auditado.
- **Aceite:** [aceite da T05](../telas/T05-parametros-empresa.md#critérios-de-aceite).
- **Esforço:** M · **Depende de:** 1.5

### 1.7 Revisão da fase
> ✅ **Concluída em 20/10/2026** — `AutorizacaoActionsTests` (verbo HTTP explícito em toda action; escrita só com `[Permissao]` de escrita ou `[SomenteAdminGlobal]`; anônimo só no login e na página de erro), `RevisaoFase1Tests` e correções abaixo.

`/security-review` + `/code-review`; testes de autorização (cada action com `[Permissao]` — teste de reflexão que falha se alguma action de escrita não tiver o atributo).

**Achados e destino:**
| Origem | Achado | Destino |
|---|---|---|
| reflexão | 4 `Index` sem verbo HTTP (aceitavam POST) | `[HttpGet]` |
| security | ADMIN_GLOBAL só na claim: rebaixado continuava global e podia se promover de novo | `SessaoValidaAsync` compara com o banco e derruba a sessão (`ContaFluxoTests`) |
| code-review | `TG_MOV_BI` trocava a empresa pela da amostra em silêncio (movimento em outra empresa) | `PR_CONFERIR_EMPRESA` |
| code-review | `TG_PEDVEN_BI`/`TG_RE_BI` numeravam com a empresa informada (podia ser 0/NULL) | empresa vem do pai antes de numerar |
| code-review | `SP_ATUALIZAR_PEDIDO_REVISAO` sem revisão atual apagava os itens; sem mudança auditava | erro 45000 / saída sem alterar |
| code-review | IP com scope id/longo estourava `AUDITORIA.IP` (VARCHAR 45) e abortava a operação | `EmpresaContexto.IpParaAuditoria` |
| code-review | comentário do `SaveChangesAsync` prometia aceitar o rastreador só após o commit também na transação do serviço | comentário corrigido (comportamento = EF) |
| code-review | retry de SaveChanges após falha com chaves geradas na entidade | registrado: nenhum serviço repete o SaveChanges no mesmo contexto |
| code-review | `PR_CONFERIR_EMPRESA` sem desvio na migração | mantido de propósito: empresa divergente deve parar a carga (fase 3/M-xx) |
| code-review | auditoria com um INSERT por linha; SaveChanges síncrono duplicado | otimização/simplificação → `/simplify` ao fim da fase 2 |
| futuro | carimbo de segurança (`SECURITY_STAMP`) para derrubar sessões ao mudar perfis, vínculos ou senha | melhoria registrada (exige coluna em `USUARIOS`) |

---

## Definição de pronto
- [x] Login seguro, empresa ativa, permissões e profissional do usuário funcionando.
- [x] Nova empresa nasce utilizável (status, perfis, parâmetros).
- [x] Nenhuma action de escrita sem `[Permissao]`.
- [x] CRUD de T02, T03, T04 e T05 provado no navegador (P26, 20/10/2026).
