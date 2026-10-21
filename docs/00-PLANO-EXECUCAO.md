# 00 — Plano de Execução do LabControl v2

> **Leia este arquivo no início de toda sessão.** Ele diz onde paramos e o que vem a seguir.
> Ao terminar uma tarefa: marque `[x]` aqui **e** no arquivo da fase, atualize o "Status" do MD da tela, a [matriz Delphi → C#](plano/MATRIZ-DELPHI-CSHARP.md) e registre no §8 (Diário).

## ▶ PONTO DE RETORNO (21/10/2026)
- **Concluído:** **Fase 0 completa** (0.1 a 0.9, revisão com 9 achados corrigidos) — `labcontrol_v2` recriável (`.\Scripts\v2\recriar.ps1`); `LabDbContext` com 69 entidades geradas de `Scripts/v2` (`node tools/gerador-csharp/gerar.mjs`); filtro da empresa ativa + trava no `SaveChanges`; interceptor `@LC_*` + READ COMMITTED, `Resultado<T>`, `ErrosMySql`, `Procedures`, `ErroNegocioFilter`; auditoria na mesma transação; **deadlock da numeração corrigido** (`SP_PROXIMO_NUMERO`); código v1 em `Legacy/`; connection string em user-secrets; `Services/` e `ViewModels/` com as convenções; layout v2 limpo (sem sessão e sem links para telas v1); auditoria das procedures gravada pelo banco (`PR_AUDITAR`), número/revisão/empresa dos filhos garantidos pelas triggers; roteiro SQL 46/46; `dotnet test tests/LabControl.Tests` 36/36 no banco persistente **`labcontrol_teste`** (recriado sozinho quando os scripts mudam), incluindo os 12 passos do roteiro 12 §7 pela aplicação (`RoteiroTests`).
- **Fase 1:** 1.1 concluída — login próprio (cookie + PBKDF2, bloqueio 5×/15 min, troca obrigatória, escolha/troca de empresa com claims, sair, evento L); `recriar.ps1 -Dev` cria a empresa 001 e o usuário `ADMIN` / `Admin123`; testes 51/51.
- **Fase 1:** 1.2 concluída — `[Permissao]` (403) + tag helper, menu da T01 filtrado por permissão e só com telas existentes, painel com 4 contadores, versão no menu; testes 57/57.
- **Fase 1:** 1.3 concluída — T02 Usuários (escopo por empresa, senha provisória exibida uma vez, vínculo com profissional por serviço único, exclusão só de quem nunca gravou); `CRIADO_POR`/`ALTERADO_POR` preenchidos pelo contexto; testes 65/65.
- **Fase 1:** 1.4 concluída — T04 Perfis (matriz, copiar entre empresas, ADMINISTRADOR protegido) + perfis sugeridos criados pelo banco em toda empresa nova; roteiro SQL 49/49.
- **Fase 1:** 1.5 concluída — T03 empresa ativa + nova empresa (ADMIN_GLOBAL) com inicialização, cópia de cadastros remapeada e acesso do criador; logo no PDF fica para a 5.4; testes 76/76.
- **Fase 1:** 1.6 concluída — T05 Parâmetros (status só da empresa, dias, textos; auditados) + numeração somente leitura com ajuste ADMIN_GLOBAL (trava a linha, nunca menor que o maior usado, motivo na auditoria); testes 80/80.
- **Fase 1:** 1.7 concluída — **Fase 1 completa**. Teste de reflexão das actions; `/security-review` (1 achado: ADMIN_GLOBAL só no cookie → sessão cai quando muda no banco) e `/code-review` (9 achados: 5 corrigidos, 4 registrados); testes 89/89. Roteiro SQL 49/49 (`recriar.ps1` agora lê a senha do user-secret). `labcontrol_v2` local recriado (`-Dev`).
- **Fase 2:** 2.1 concluída — CRUD genérico dos auxiliares (catálogo + serviço + controller + views únicos) aprovado pelo usuário com Idiomas; skill `/novo-auxiliar`; testes 94/94.
- **Cadastros com contatos (pedido do usuário, 20/10/2026):** empresa usuária vira **entidade** (`EMPRESAS.ID_ENTIDADES`, como o legado) e T03/T12/T13 têm **vários endereços, telefones e e-mails tipados** (componente único, [P25](processos/P25-cadastro-entidades-contatos.md)); T12 e T13 antecipadas da Fase 3; auxiliares T10-01…06; 5.571 cidades IBGE + busca de CEP. Roteiro SQL 62/62; `labcontrol_v2` recriado (`-Dev`).
- **Correção pós-teste do usuário (20/10/2026):** o cliente "apagado" estava só **inativo** (botão olho cortado = Inativar, sem texto nem confirmação). Listas agora com botões de **ícone + texto**, **confirmação ao inativar** e aviso de **inativos ocultos** na pesquisa; **testes no navegador** (Edge sem janela, Playwright — [P26](processos/P26-testes-ponta-a-ponta.md)) provam o CRUD real de T02, T03, T04, T05, T10, T12 e T13, inclusive a busca de CEP. Pendências de todas as tarefas consolidadas em [Pendências registradas](#pendências-registradas-a-executar).
- **2ª correção pós-teste (20/10/2026):** sessão vale só para a execução do app (reiniciou → **sempre login**); novo usuário já vem com a empresa ativa (aparece no combo do profissional); confirmação ao **reativar** com texto (não mais diálogo vazio); menu com **um item só "Empresas Usuárias do Sistema"** (antes *Empresa* + *Empresas*, que eram o mesmo cadastro); item *Cidades* saiu do menu até a 2.3 (era o JSON do combo). Testes 118/118. As 3 alterações em andamento quando o computador travou ficaram corrompidas (bytes nulos) e foram refeitas a partir do commit `9fbaed1`.
- **Fase 2:** 2.2 concluída — os 20 auxiliares no catálogo (T10-08…T10-20); código 1–99 travado após uso (T10-14/16), sempre um status que permite resultado (T10-15), parcelas da condição de pagamento em tela própria com Σ = 100% (T10-11); testes 144/144.
- **Fase 2:** 2.3 concluída — telas de países/estados/cidades (`/Geografia`, consulta para todos, gravação só ADMIN_GLOBAL) + importação do CSV de municípios IBGE com relatório; testes 149/149.
- **Fase 2:** 2.4 concluída — cadastro rápido "+" (`lc-rapido="TABELA"` no combo, `site.js`), também de dentro do modal de contatos; testes 151/151.
- **Fase 2:** 2.5 concluída — **Fase 2 completa.** `/code-review medium`: 10 achados, 6 corrigidos (contatos de profissional pela tela de clientes, "Inativar" oferecido em qualquer falha, CEP sem IBGE, regra do T10-15 ampla demais, gênero do aviso, cidade da lista de empresas), 4 registrados (pendências 5, 15 e 16); testes 153/153.
- **Próxima tarefa:** `/simplify` da pendência 5 (fim da Fase 2) e depois **Fase 3 → 3.3 T14 Produtos** — ver [F3-cadastros](plano/F3-cadastros.md#33-t14--produtos). Decidir as pendências 15 (CNPJ da empresa na entidade) e 16 (sessão por execução × hospedagem).
- ⚠️ A senha `root` do MySQL ficou no histórico do git (`appsettings.json` até a 0.3): trocar a senha do MySQL e atualizar o user-secret — passo a passo em [Scripts/v2/README.md › Senha do MySQL](../Scripts/v2/README.md#senha-do-mysql-onde-fica-e-como-trocar).
- Ao retomar, apague/atualize este bloco.

## 1. Ponto de partida (17/10/2026)

- ✅ Análise do Firebird, das procedures e das telas Delphi concluída.
- ✅ Script MySQL v2 (`Scripts/v2/01..05`, `06_cidades` desde 20/10/2026) **validado** no MySQL 8.0.46 local.
- ✅ **Documentação completa por objeto, tela, processo e regra** (17/10/2026):
  - [banco/objetos/](banco/objetos/README.md) — **138 MDs**, um por objeto do banco: 59 tabelas, 10 views, 27 rotinas e 42 triggers;
  - [telas/](README.md#telas) — **54 MDs**, um por tela do Delphi que vai para o C#, mais as telas novas;
  - [processos/](processos/README.md) — **24 processos** + **18 etapas de migração**;
  - [regras/](regras/README.md) — catálogo de regras de negócio `RN-xx.yy` em 9 domínios;
  - [plano/](plano/) — um arquivo detalhado por fase + [matriz de rastreabilidade Delphi → C#](plano/MATRIZ-DELPHI-CSHARP.md).
- ✅ Decisões do usuário: **banco recriado do zero**, **tudo por empresa**, **Pedido de Venda com tela própria**, **ano da amostra = ano do servidor**.
- ⚠️ Projeto C# atual (v1): modelo de dados errado. **Reaproveitar só layout, padrões visuais e a estrutura do projeto**; controllers/models serão reescritos sobre o schema v2.
- Ordem pedida pelo usuário: **cadastros básicos → cadastros → recebimento de amostras → inclusão de resultados → boletim**; depois o resto em qualquer ordem.

## 2. Como usar esta documentação numa tarefa

1. Abra o arquivo da **fase** em `docs/plano/F*.md` e localize a tarefa (passos, arquivos, aceite).
2. Leia o **MD da tela** (`docs/telas/`), os **processos** citados (`docs/processos/`) e as **regras** (`docs/regras/`).
3. Para cada tabela/rotina tocada, abra o MD do **objeto** (`docs/banco/objetos/`): colunas preenchidas pelo banco, mensagens de erro, como chamar no C#.
4. Na dúvida sobre o legado, consulte o Delphi indicado no MD (`C:\Project\Laboratorio\LabControl\un_*.pas`).
5. Implemente → teste → marque os checklists → diário.

## 3. Qual modelo e qual modo do Claude usar

### Modelos (`/model`)
| Modelo | Quando usar | Exemplos neste projeto |
|---|---|---|
| **Opus 5.5** (`claude-opus-5-5`) | Análise, arquitetura, regra de negócio complexa, banco, migração, revisão crítica, depuração difícil | Fases 0, 4, 5, 9; migração; PDF do boletim |
| **Sonnet 5.5** (`claude-sonnet-5-5`) | Implementação seguindo um MD já detalhado | Fases 1, 2, 3, 6, 7, 8 |
| **Haiku 5.5** (`claude-haiku-5-5`) | Tarefas mecânicas a partir de um modelo pronto | Replicar os 19 auxiliares (2.2), ajustes de texto/CSS, checklists |

Regra prática: **se a tarefa exige decidir algo que não está escrito no MD → Opus. Se o MD já decide tudo → Sonnet. Se é cópia de um padrão pronto → Haiku.**

### Modos do Claude Code (Shift+Tab alterna)
| Modo | Quando |
|---|---|
| **Plan mode** | Antes de cada tela complexa (T12, T20, T22, T26, T27) e de cada parte da migração |
| **Modo padrão** (pede aprovação) | Banco, segurança/login, migração, qualquer coisa que mexa em dados |
| **Accept edits** | CRUDs repetitivos depois do primeiro aprovado (auxiliares, relatórios) |
| **Auto** | Documentação, organização de arquivos, tarefas sem risco a dados |

### Comandos úteis
`/code-review` ao fim de cada fase (`high` nas fases 0, 4 e 5) · `/security-review` ao fim da fase 1 e antes da virada · `/simplify` depois de fases grandes · `/run` para ver a tela funcionando.

### Skills do projeto (`.claude/skills/`)
| Skill | Quando |
|---|---|
| `/concluir-tarefa X.Y` | ao terminar cada tarefa: build + testes, checklists (plano e fase), Status/Implementação da tela, matriz, Diário, ponto de retorno e commit |
| `/mudanca-banco` | qualquer mudança em `Scripts/v2` (tabela, view, rotina, trigger, seed): script → docs → roteiro 12 §7 → recriar → gerar entidades → testes |
| `/novo-auxiliar` | **a criar ao fim da 2.1**, a partir do CRUD de Idiomas aprovado; usada na 2.2 para os outros 19 auxiliares |

## 4. Visão geral das fases

Esforço: **S** = 1 sessão · **M** = 2–3 sessões · **G** = 4+ sessões (sessão ≈ meio dia de trabalho com o Claude).

| Fase | Arquivo detalhado | Entrega principal | Telas | Depende de | Esforço |
|---|---|---|---|---|---|
| 0 | [F0-fundacao](plano/F0-fundacao.md) | banco v2, DbContext, multiempresa, erros, auditoria, testes | — | — | G |
| 1 | [F1-login-permissoes](plano/F1-login-permissoes.md) | login, empresa ativa, usuários, perfis, empresa, parâmetros | T00–T05 | 0 | G |
| 2 | [F2-cadastros-basicos](plano/F2-cadastros-basicos.md) | 20 auxiliares, geografia, endereço por CEP, cadastro rápido | T10-xx, T11-xx | 1 | M |
| 3 | [F3-cadastros](plano/F3-cadastros.md) | clientes, profissionais, produtos, parâmetros + **migração 1** | T12–T15, T07 | 2 | G |
| 4 | [F4-recebimento-amostras](plano/F4-recebimento-amostras.md) | **recebimento**, proposta essencial, etiquetas, pesquisa + **migração 2** | T20 (parcial), T22, T25, T29 | 3 | G |
| 5 | [F5-resultados-boletim](plano/F5-resultados-boletim.md) | **resultados**, **boletim** com emissão/PDF + **migração 3** | T26, T27 | 4 | G |
| 6 | [F6-propostas-pedidos](plano/F6-propostas-pedidos.md) | proposta completa (revisão, PDF), **pedido de venda** | T20, T21 | 5 (ou 4) | G |
| 7 | [F7-estoque-arquivo](plano/F7-estoque-arquivo.md) | movimentação e localização | T23, T24 | 4 | M |
| 8 | [F8-relatorios-complementos](plano/F8-relatorios-complementos.md) | 6 relatórios, auditoria, e-mail, (RE) | T30-xx, T06, T28 | 5, 6 | G |
| 9 | [F9-homologacao-virada](plano/F9-homologacao-virada.md) | migração completa, homologação, virada | — | 0–8 | M |

Caminho crítico: **0 → 1 → 2 → 3 → 4 → 5** (pedido do usuário). As fases 6 e 7 podem andar em paralelo depois da 4; a 8 depois da 5/6.

## 5. Checklist resumido (detalhes, passos e aceite em cada arquivo de fase)

Legenda: 🔵 Opus · 🟢 Sonnet · ⚪ Haiku · 📐 Plan mode · ✋ modo padrão · ⚡ accept edits

### Fase 0 — Fundação 🔵 ✋ — [detalhes](plano/F0-fundacao.md)
- [x] 0.1 Criar o banco `labcontrol_v2` (backup do `labcontrol` antes) e rodar o roteiro do doc 12 §7.
- [x] 0.2 `Scripts/v2/README.md` + `recriar.ps1`.
- [x] 0.3 📐 `LabDbContext` v2 (Fluent API por tabela, sem migrations); remover patches do `Program.cs`; segredos em user-secrets.
- [x] 0.4 `IEmpresaContexto` + Global Query Filter + preenchimento de `ID_EMPRESAS`.
- [x] 0.5 `SessaoMySqlInterceptor`, `Resultado<T>`, tradução de erros MySQL (45000, 1062, 1451, 1452), helpers de procedure.
- [x] 0.6 `AuditoriaInterceptor` + `IAuditoria` (feito no `SaveChanges` do contexto — ver P21).
- [x] 0.7 Projeto xUnit com banco de teste recriado pelos scripts + roteiro 12 §7 automatizado (`RoteiroTests`; banco **persistente** `labcontrol_teste`, recriado pelo hash dos scripts — decisão do usuário).
- [x] 0.8 Estrutura de pastas e isolamento da v1 (`Legacy/` na 0.3; `Services/`, `ViewModels/` com README; CSS do layout em `site.css`; menu só com telas v2).
- [x] 0.9 `/code-review high` — 9 achados, todos corrigidos (ver Diário).

### Fase 1 — Login, multiempresa e permissões 🟢 (📐) ✋ — [detalhes](plano/F1-login-permissoes.md)
- [x] 1.1 T00 Login + troca de senha + seleção/troca de empresa.
- [x] 1.2 T01 Layout, menu por permissão, painel.
- [x] 1.3 T02 Usuários (empresas, perfis, profissional vinculado).
- [x] 1.4 T04 Perfis e matriz de permissões.
- [x] 1.5 🔵 T03 Empresa usuária + nova empresa + cópia de cadastros (logo no PDF → 5.4).
- [x] 1.6 T05 Parâmetros da empresa + numeração auditada.
- [x] 1.7 `/security-review` + `/code-review`.

### Fase 2 — Cadastros básicos 🟢 → ⚪ ⚡ — [detalhes](plano/F2-cadastros-basicos.md)
- [x] 2.1 🟢 CRUD genérico aprovado com Idiomas (T10-00/T10-07).
- [x] 2.2 ⚪⚡ Replicar para os outros 19 auxiliares (T10-01…T10-20; atenção a T10-14/T10-16). — T10-01…06 em 20/10/2026 (P25), T10-08…20 em 21/10/2026.
- [x] 2.3 🟢 Geografia (T11-01…03) + importação IBGE + componente Endereço (ViaCEP).
- [x] 2.4 🟢 Cadastro rápido "+".
- [x] 2.5 `/code-review`. — **Fase 2 completa.**

### Fase 3 — Cadastros 🟢 (📐 antes da T12) — [detalhes](plano/F3-cadastros.md)
- [x] 3.1 T12 Clientes/fornecedores. — antecipada (20/10/2026, P25).
- [x] 3.2 T13 Profissionais (+ vínculo com usuário, assinatura). — antecipada (20/10/2026, P25); assinatura no PDF na 5.3/5.4.
- [ ] 3.3 T14 Produtos.
- [ ] 3.4 T15 Parâmetros de análise.
- [ ] 3.5 🔵📐✋ Migração parte 1 (M-01…M-10) + T07 + conferências.
- [ ] 3.6 `/code-review`.

### Fase 4 — Recebimento de amostras 🔵 📐 ✋ — [detalhes](plano/F4-recebimento-amostras.md)
- [ ] 4.1 🔵 Migração parte 2 (M-11…M-15) + conferências (números idênticos ao legado).
- [ ] 4.2 🟢 T20 Proposta essencial.
- [ ] 4.3 🔵 T22 Recebimento completo (número/ano, retroativa, datas parciais, testes, comprovante, revisão).
- [ ] 4.4 🟢 T29 Pesquisa de amostras.
- [ ] 4.5 🟢 T25 Etiquetas.
- [ ] 4.6 Testes de aceite + `/code-review high`.

### Fase 5 — Resultados e boletim 🔵 📐 ✋ — [detalhes](plano/F5-resultados-boletim.md)
- [ ] 5.1 🔵 Migração parte 3 (M-16…M-18) + conferências.
- [ ] 5.2 🔵 T26 Resultados (+ fila de análises, revisão/desfazer).
- [ ] 5.3 🔵 T27 Boletim (seleção, metodologias, prévia, **emissão PDF + hash**, revisão/desfazer).
- [ ] 5.4 🔵 Layout do PDF do boletim validado com boletins reais.
- [ ] 5.5 Testes de aceite + `/code-review high`.

### Fase 6 — Propostas completas e pedido de venda 🟢 (📐) — [detalhes](plano/F6-propostas-pedidos.md)
- [ ] 6.1 T20 completa (seleção múltipla, revisão, contrato, PDF, trocar produto).
- [ ] 6.2 T21 Pedidos de venda.
- [ ] 6.3 `/code-review`.

### Fase 7 — Estoque e arquivo 🟢 ⚡ — [detalhes](plano/F7-estoque-arquivo.md)
- [ ] 7.1 T23 Movimentação.
- [ ] 7.2 T24 Localização (arquivo/descarte, lote).
- [ ] 7.3 `/code-review`.

### Fase 8 — Relatórios e complementos 🟢 ⚡ — [detalhes](plano/F8-relatorios-complementos.md)
- [ ] 8.1 T30-01…06 relatórios (tela + PDF + CSV) com regras do P20.
- [ ] 8.2 T28 Relatório de ensaio (**só se confirmado**).
- [ ] 8.3 Envio de boletim por e-mail.
- [ ] 8.4 T06 Auditoria.
- [ ] 8.5 `/simplify` + `/code-review`.

### Fase 9 — Homologação e virada 🔵 ✋ — [detalhes](plano/F9-homologacao-virada.md)
- [ ] 9.1 Migração completa em banco limpo + todas as conferências.
- [ ] 9.2 Homologação guiada com o laboratório.
- [ ] 9.3 `/security-review`, backup, virada, desligar o Delphi.

## 6. Como começar cada sessão (copie e cole para o Claude)

```
Leia CLAUDE.md e docs/00-PLANO-EXECUCAO.md. Continue a próxima tarefa não marcada
da fase atual, seguindo o arquivo da fase em docs/plano/. Antes de codificar, leia o MD
da tela (docs/telas/), os processos e regras citados e os MDs dos objetos do banco
(docs/banco/objetos/). Ao terminar, marque os checklists (plano e fase), atualize o
Status do MD da tela e a matriz Delphi → C#, e escreva uma linha no Diário (§8).
```

## 7. Dúvidas em aberto (perguntar ao usuário quando chegar a hora)
| # | Dúvida | Fase | Impacto |
|---|---|---|---|
| 1 | Relatório de Ensaio (T28) ainda é necessário? (tabela vazia no legado) | 8 | inclui ou não a T28 |
| 2 | Envio de boletim por e-mail: servidor SMTP e remetente? | 8 | 8.3 |
| 3 | Etiquetas: tamanho, modelo da impressora, **Code 39 (legado) ou Code 128**, PDF ou impressão direta (EPL2/ZPL)? | 4 | T25 |
| 4 | ~~Perfis padrão além de ADMINISTRADOR (Recepção, Analista, Responsável Técnico, Comercial)?~~ **Respondida em 19/10/2026: sim, os 4, criados automaticamente em toda empresa nova (`SP_INICIALIZAR_EMPRESA`).** | 1 | T04 |
| 5 | Modelos atuais (PDF/papel) do boletim, proposta e pedido para copiar o layout | 5/6 | PDFs |
| 6 | Onde guardar os PDFs emitidos (pasta local, servidor de arquivos, banco)? | 5 | P17 |
| 7 | Senha provisória dos usuários migrados: aleatória por usuário (recomendado) ou única? | 3 | M-09 |
| 8 | Prazos em **dias úteis** são necessários? (legado só dias corridos) | 2 | T10-12 |
| 9 | Relatórios: manter "somente amostras com pedido de venda" como padrão (comportamento do legado)? | 8 | T30-02/03 |

## 8. Riscos do projeto
| Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|
| Números de amostra migrados diferentes dos impressos | média | alto | `CODIGO` dos tipos = ID antigo; comparação de **todos** os números na M-14 |
| Regras críticas só na aplicação | baixa | alto | regras críticas no banco (triggers/procedures) + testes automatizados |
| Procedures com transação dentro de transação EF | média | alto | helper único com verificação ([P22](processos/P22-erros-transacoes.md)) |
| Layout do boletim rejeitado | média | médio | validar lado a lado na 5.4 antes de homologar |
| Etiquetas incompatíveis com a impressora atual | média | médio | confirmar dúvida 3 cedo; oferecer EPL2/ZPL |
| Relatórios com números diferentes do legado | alta | baixo | diferenças são correções; documentar no MD de cada relatório |
| Escopo crescer (módulos vazios do legado: compras/fiscal) | baixa | médio | fora do escopo; novo módulo só após a virada |

## Pendências registradas (a executar)
Itens adiados nas tarefas já feitas — cada um tem dono (tarefa) e está também no MD de origem. Ao concluir, risque aqui e no MD.

| # | Pendência | Onde fazer | Origem |
|---|---|---|---|
| 1 | ~~Auxiliares T10-08…T10-20 (13), com as regras especiais: T10-11 parcelas (Σ 100%), T10-14/T10-16 código imutável após uso, T10-15 ao menos um "permite resultado", cores dos status~~ **Feito em 21/10/2026 (2.2).** | 2.2 (skill `/novo-auxiliar`) | F2 |
| 2 | ~~Telas de manutenção de países/estados/cidades (T11-01…03; só ADMIN_GLOBAL)~~ **Feito em 21/10/2026 (2.3).** | 2.3 | T11-03 |
| 3 | ~~Botão "+" (cadastro rápido) nos combos~~ **Feito em 21/10/2026 (2.4)** — nas telas futuras basta `lc-rapido="TABELA"` no combo (proposta: método, idioma, prazo; resultados: tipo de resultado). Produto não é auxiliar: o "+" dele, se pedido, abre a T14 | 2.4 → 4.2, 5.2 | T10-00 |
| 4 | `/code-review` da Fase 2 (inclui T12/T13/contatos antecipados) | 2.5 | F2 |
| 5 | ~~`/simplify`: auditoria com um INSERT por linha; `SaveChanges` síncrono duplicando o assíncrono; **(2.5)** cidades da UF e combo de UF repetidos (controller + 3 serviços), paginação/contagem de inativos e normalização de texto copiadas em 4 serviços~~ **Feito em 21/10/2026** (ver Diário) | fim da Fase 2 | code-review 1.7 e 2.5 |
| 6 | T14 Produtos e T15 Parâmetros de análise | 3.3, 3.4 | F3 |
| 7 | Migração parte 1: M-01 (entidade da empresa = entidade 4 do legado, `EMPRESAS.ID_ENTIDADES` antes do `SP_INICIALIZAR_EMPRESA`), M-02 (190 cidades do legado → IBGE por nome + UF), M-05…M-08 (contatos com tipo; duplicidades de principal/endereço ativo listadas na conferência) | 3.5 | P25 §7, M-01/M-02 |
| 8 | Logo da empresa no PDF do boletim | 5.4 | T03 |
| 9 | Assinatura do profissional no PDF do boletim emitido | 5.3/5.4 | T13 |
| 10 | Carimbo de segurança (`SECURITY_STAMP` em `USUARIOS`) para derrubar sessões ao mudar perfis, vínculos ou senha (hoje só o ADMIN_GLOBAL derruba) | a agendar (exige `/mudanca-banco`) | security-review 1.7 |
| 11 | Retry de `SaveChanges` depois de falha com chaves geradas na entidade (nenhum serviço faz hoje) | só se algum serviço passar a repetir | code-review 1.7 |
| 12 | Trocar a senha `root` do MySQL (ficou no histórico do git até a 0.3) e atualizar o user-secret | usuário | ponto de retorno |
| 13 | Teste no navegador (P26) para toda tela nova; padrão das listas (botões com texto, confirmação com texto ao inativar **e ao reativar** — nunca `data-confirmar` vazio, aviso de inativos ocultos; item de menu só quando a tela existe) | toda tarefa de tela (skill `/concluir-tarefa`) | teste do usuário 20/10/2026 |
| 14 | Dúvidas em aberto do §7 (T28, SMTP, etiquetas, modelos de PDF, onde guardar PDFs, senha dos migrados, dias úteis, relatórios) | quando a fase chegar | §7 |
| 15 | Entidade da empresa usuária ocupa o `UQ_ENT_DOC` com o CNPJ do laboratório: cliente com o mesmo CNPJ é recusado sem registro visível; trocar o CNPJ da empresa para o de um cliente dá 1062 na `TG_EMPRESAS_AU` (opções: não copiar o CNPJ para a entidade, ou mensagem própria + permitir). **Decisão do usuário (21/10/2026): o mesmo CNPJ pode ser empresa usuária, cliente e fornecedor — não pode recusar** → a unicidade do documento deixa de valer para a entidade da empresa usuária | `/mudanca-banco` antes da 3.3 | code-review 2.5 |
| 16 | ~~Sessão presa à execução do app (GUID por processo, pedido do usuário): reciclagem do IIS derruba todos e duas instâncias se derrubam — rever com o tipo de hospedagem~~ **Decisão do usuário (21/10/2026): só um servidor por cópia do sistema; mantém como está** (reiniciar o app pede login de novo, inclusive na reciclagem do IIS) | — | code-review 2.5 |

## 9. Diário de execução
| Data | Fase/tarefa | O que foi feito | Modelo |
|---|---|---|---|
| 16/10/2026 | Análise | Análise completa do Firebird/Delphi, script MySQL v2 validado, MDs de banco, telas e este plano | Opus 5.5 |
| 17/10/2026 | Documentação | Projeto unificado em `C:\Project\LabControl`; MDs de **cada objeto do banco** (138, gerados dos scripts + regras de negócio), **54 telas** (auxiliares, geografia, relatórios e telas de sistema separadas), **24 processos + 18 etapas de migração**, **catálogo de regras RN-01…RN-09**, plano detalhado por fase e matriz Delphi → C#. Achados novos no legado: relatório de parâmetros analisados com JOIN errado, faturamento duplicando pedidos de revisões, `ID_LAB_PROPOSTAS_STATUS_ENVCLI` nulo (data de envio nunca gravada), etiquetas EPL2 com Code 39, desfazer descarte sem voltar o status, pesquisa de amostras e combo do RE sem filtro de empresa | Opus 5.5 |
| 17/10/2026 | Pausa | Ponto de retorno marcado no topo deste arquivo; próxima tarefa: 0.1 | Opus 5.5 |
| 19/10/2026 | 0.1 | Backup do `labcontrol` (v1, 52 tabelas) em `C:\Project\backup\`; `labcontrol_v2` criado pelos scripts 01..05 — 59 tabelas, 10 views, 8 funções, 19 procedures, 42 triggers, 109 permissões; roteiro 12 §7 virou script reexecutável (`Scripts/v2/teste/roteiro_12_7.sql`, 39 verificações) e passou 39/39 num banco temporário (removido depois); checksums do v1 iguais antes/depois | Opus 5.5 |
| 19/10/2026 | 0.2 | `Scripts/v2/recriar.ps1` (apaga/recria o banco trocando o nome nos scripts, para no 1º erro, confere contagens, recusa `labcontrol`; `-Teste` cria `labcontrol_teste_<id>`, roda o roteiro e apaga) + `Scripts/v2/README.md`. Testado: `-Teste` 39/39, recriação do `labcontrol_v2`, recusa do v1 e senha errada (exit 1) | Opus 5.5 |
| 19/10/2026 | 0.3 | Gerador `tools/gerador-csharp` (reusa o parser do gerador de docs): 59 tabelas + 10 views → `Models/*.g.cs`, `Data/Configurations/*.g.cs`, DbSets; enums com `[ValorBanco]`; colunas de trigger `CalculadaPeloBanco`/`PreenchidaPeloBancoNaInclusao` (confirmado no MySQL strict: INSERT omitindo coluna NOT NULL preenchida pela trigger funciona). `Program.cs` sem `EnsureCreated`/patches; connection string em user-secrets; v1 movida para `Legacy/` (decisão do usuário); `tests/LabControl.Tests`: modelo × `information_schema`, leitura de cada DbSet, gravação com triggers — 3/3; build 0 avisos; Home 200 | Opus 5.5 |
| 19/10/2026 | 0.4 | `IEmpresaContexto`/`EmpresaContexto` (claims `LabClaims`), interfaces `IPorEmpresa`/`IFiltradoPorEmpresa` marcadas pelo gerador, filtro global por convenção (+ filhos `CONDICOES_PAGTOS_PARC`/`PERFIS_PERMISSOES` pelo pai; sem filtro: `EMPRESAS`, `USUARIOS_EMPRESAS`, `USUARIOS_PERFIS`), `SaveChanges` grava/confere a empresa (fail-closed sem empresa), `ExigirEmpresaMiddleware`. Testes 9/9 (2 empresas: sem vazamento em tabelas, filhos e views; inclusão em outra empresa, troca de empresa e alteração/exclusão de outra empresa recusadas) | Opus 5.5 |
| 19/10/2026 | 0.5 | `ConexaoMySql` (`AllowUserVariables`, `ConnectionReset`), `SessaoMySqlInterceptor`, `Resultado`/`Resultado<T>`, `ErrosMySql` (45000; 1062 por constraint; 1451; 1452), `Procedures` (todas as `SP_*`, checagem de empresa, recusa dentro de transação), `ErroNegocioFilter` + extensões MVC. Testes passaram a usar o banco **`labcontrol_teste`** (criado por `recriar.ps1`, recriado quando o hash dos scripts muda; cada teste cria sua empresa). 15/15: pool sem herança de variáveis, mensagens exatas de trigger/procedure, OUT params de pedido e revisões, retroativa só com liberação na transação, procedure de outra empresa recusada | Opus 5.5 |
| 19/10/2026 | 0.6 | Auditoria: `AuditoriaAlteracoes` no `SaveChanges` (I/A/E das tabelas do P21 na mesma transação; só colunas alteradas na A; senha `***`, blob `[binário]`), `IAuditoria`/`ServicoAuditoria`, eventos R/M/D/I/A gravados pelo `Procedures`. **Achado crítico:** `SP_PROXIMO_NUMERO` (INSERT IGNORE + FOR UPDATE) dava deadlock com gravações simultâneas na mesma empresa (66/80 no teste novo `ConcorrenciaTests`) e o REPEATABLE READ travava intervalos entre documentos diferentes (`MAX()+1` de `TG_PROPPROD_BI`). Correção: procedure com `INSERT … ON DUPLICATE KEY UPDATE`, `TG_PROPPROD_BI` com `FOR UPDATE` na proposta, aplicação em READ COMMITTED (`SessaoMySqlInterceptor` + `IsolamentoInterceptor`, pois o `BeginTransaction()` do MySqlConnector força REPEATABLE READ). Roteiro 12 §7 39/39, `labcontrol_v2` recriado, MDs dos objetos regenerados; testes 20/20 (2 execuções) | Opus 5.5 |
| 19/10/2026 | 0.7 | Roteiro 12 §7 portado para C# (`tests/LabControl.Tests/RoteiroTests.cs`, 7 testes cobrindo os 12 passos: EF + `Procedures` + `ErrosMySql`; `SP_INICIALIZAR_EMPRESA` também pelo helper). **Decisão:** banco de testes persistente `labcontrol_teste` (não um banco por execução): recriado só quando o hash de `Scripts/v2/0*.sql` muda; cada teste usa a própria empresa. **Achado:** o EF enviava `ANO/COD` informados pela tela e não relia o valor gerado (a entidade ficava com 1999/999 enquanto o banco gravava o ano do servidor); e as triggers da proposta, do pedido e do relatório guardam um número ≠ 0 enviado. Correção no gerador: colunas `fixo` nunca são enviadas e sempre são relidas. Testes 27/27, build sem avisos | Opus 5.5 |
| 19/10/2026 | 0.8 | `Services/README.md` e `ViewModels/README.md` (convenções; `ErrorViewModel` movido para `ViewModels/`); `_Layout` sem `Session` (topo lê `User`), estilos inline e CSS do template movidos/limpos em `wwwroot/css/site.css` (com ajuste para celular); menu e Home sem links para telas v1 (só Início — menu da T01 na 1.2); `Privacy` e `_Layout.cshtml.css` do template removidos; página de erro em português; `AddSession`/`UseSession` saíram do `Program.cs`. Teste novo `LayoutTests` (WebApplicationFactory: Home 200 e todo link do layout responde). Testes 29/29, build sem avisos | Opus 5.5 |
| 19/10/2026 | 0.9 | `/code-review` high da Fase 0: 9 achados, todos verificados e corrigidos. **Banco:** (1–2) proposta/pedido/relatório guardavam número ≠ 0 enviado (risco de travar a sequência no `UNIQUE`) e revisão podia ser incluída fora das `SP_GERAR_REVISAO_*` → triggers sempre numeram fora da migração e exigem `@LC_REVISAO` (definida só pelas SPs); (3) filho com `ID_EMPRESAS` diferente do pai era trocado em silêncio → `PR_CONFERIR_EMPRESA` recusa; (6) eventos R/M/D/I/A gravados depois do COMMIT da procedure → `PR_AUDITAR` dentro da transação delas (`@LC_IP` novo no interceptor). **C#:** (4) rastreador aceito antes do commit/auditoria → `SaveChanges(false)` + INSERT direto + `AcceptAllChanges` após o commit; (5) `ServicoAuditoria` gravava alterações pendentes → INSERT direto; (7) `InicializarEmpresaAsync` exige ADMIN_GLOBAL; (8) gravação de `USUARIOS_EMPRESAS`/`USUARIOS_PERFIS` restrita à empresa ativa; (9) filtro sem empresa ativa mostrava linhas com `ID_EMPRESAS` NULL. Roteiro SQL 46/46 (passo 13 novo), `labcontrol_v2` recriado, MDs dos objetos regenerados (21 procedures), `RevisaoFase0Tests` (7 testes); testes 36/36, build sem avisos. **Fase 0 concluída** | Opus 5.5 |
| 19/10/2026 | 1.1 | T00: `ContaService` + `ContaController` + views de conta. Cookie `LabControl.Auth` (HttpOnly/Secure/Lax, 8 h deslizantes), política de fallback "autenticado", antiforgery global; PBKDF2 (`PasswordHasher`), login em maiúsculas, mensagem única, hash fictício para usuário inexistente, falhas por `UPDATE` atômico (5ª bloqueia 15 min), rehash; troca obrigatória antes de qualquer tela (middleware); escolha/troca de empresa com claims (empresa, profissional, admin, permissões dos perfis ativos — `IgnoreQueryFilters` só ali, com a empresa conferida); revalidação da sessão a cada request (usuário/vínculo inativado sai na hora; bloqueio não derruba sessão aberta); topo com empresa e aviso "sem profissional"; evento L. `recriar.ps1 -Dev` (`dev/admin_dev.sql`: empresa 001 + `ADMIN`/`Admin123`). Verificado também no app real (curl). Testes 51/51 (`ContaServiceTests`, `ContaFluxoTests`) | Opus 5.5 |
| 19/10/2026 | 1.2 | T01: `PermissaoAttribute` (403 com página "Acesso negado" mostrando o código; JSON em AJAX; anônimo → login; ADMIN_GLOBAL passa) e `PermissaoTagHelper`; menu da T01 em `Infra/Menu.cs` + `MenuViewComponent`, filtrado por permissão **e** por tela existente (`TelasExistentes`), para crescer sozinho sem link quebrado; painel (`PainelService`): amostras hoje/mês, testes pendentes/vencidos, boletins não emitidos, propostas aguardando cliente, cada um com a permissão de consulta; versão `v2.0.0` no menu (Sobre o Sistema). Senha do MySQL: documentado onde fica e como trocar (`Scripts/v2/README.md`), sem gravar senha no git (decisão do usuário). Achado: `string[].Contains` em consulta EF quebra no C# 14 (vira `ReadOnlySpan`) — usar `List`. Testes 57/57; app real verificado | Opus 5.5 |
| 19/10/2026 | 1.3 | T02: `UsuariosController` + `UsuarioService` + `VinculoUsuarioProfissionalService` (serviço único do vínculo, para a T13). Escopo: ADMIN_GLOBAL vê tudo; administrador de empresa só usuários da empresa ativa e só a linha dela (linhas forjadas de outras empresas ignoradas); dados globais/senha/exclusão só de usuário exclusivo da empresa ativa. Senha provisória exibida uma vez na resposta (no-store); combo de profissionais com os já vinculados desabilitados + mensagem clara; ao menos um perfil; proteções do próprio usuário; exclusão só de quem nunca gravou (AUDITORIA, log da amostra, `CRIADO_POR/ALTERADO_POR` via `information_schema`). **Lacuna corrigida:** `CRIADO_POR`/`ALTERADO_POR` não eram preenchidos — agora o `LabDbContext` preenche (e `ALTERADO_POR` sai do diff da auditoria). Testes 65/65 | Opus 5.5 |
| 19/10/2026 | 1.4 | T04: `PerfisController` + `PerfilService`; matriz TELA × AÇÃO a partir de `PERMISSOES` (atalhos linha/coluna/somente consulta; CONSULTAR automático também no servidor); gravação só da diferença (auditoria precisa); ADMINISTRADOR somente leitura; excluir recusado com usuários; copiar na empresa e, ADMIN_GLOBAL, entre empresas. **Dúvida 4 respondida pelo usuário:** os 4 perfis sugeridos nascem em toda empresa nova — `PR_CRIAR_PERFIL` (nova, 22 procedures) chamado pelo `SP_INICIALIZAR_EMPRESA`, só cria se não existir (reinicializar não devolve permissão retirada). Roteiro SQL 49/49, `labcontrol_v2` recriado (`-Dev`), MDs regenerados; gerador de docs passou a ignorar textos e comentários ao detectar referências (removeu falsos positivos antigos). Bug evitado: `String.replace` do JS com `$$` no texto novo vira `$` (quebrava os `END$$`) | Opus 5.5 |
| 19/10/2026 | 1.5 | T03: `EmpresaController` (`/Empresa/Dados`, logo, cidades) + `EmpresasController` (`[SomenteAdminGlobal]`, novo) + `EmpresaService` + `CopiaCadastros`. Nova empresa numa transação: EMPRESAS → `SP_INICIALIZAR_EMPRESA` → cópia → acesso do criador (ADMINISTRADOR) → auditoria com o resumo. Cópia genérica por `information_schema` (INSERT … SELECT, dedupe pelas chaves únicas, FKs remapeadas pela chave natural, dependências puxadas, sequência PRODUTO ajustada ao maior código copiado); teste garante que o modelo da cópia bate com as chaves/FKs do banco. CNPJ com DV, logo validado pela assinatura (PNG/JPG ≤ 1 MB), código imutável, topo atualizado ao gravar. Logo no PDF do boletim registrado na 5.4. Banco de testes persistente: testes passaram a sortear CNPJ/código livres | Opus 5.5 |
| 20/10/2026 | 1.6 | T05: `/Empresa/Parametros` (`ParametrosEmpresaService`) com abas Parâmetros e Numeração. Parâmetros: combos só com status da empresa ativa (ativos + o escolhido), validações (status distintos, dias 0–365 com aviso > 30), gravação pelo EF auditada, sem cache (as triggers leem na hora — teste prova que mudar os dias retroativos muda o recebimento). Numeração: grid de `SEQUENCIAS_DOCUMENTOS`; ajuste só ADMIN_GLOBAL (`[SomenteAdminGlobal]` + checagem no serviço) com `FOR UPDATE` na linha, recusa abaixo do maior número já usado, motivo obrigatório e evento `A` com antes/depois/motivo; a próxima amostra continua do valor ajustado. Cuidado: sub-VM com `[Required]` precisa de `[ValidateNever]` no VM pai. Testes 80/80 (4 novos em `ParametrosEmpresaServiceTests`) | Opus 5.5 |
| 20/10/2026 | 1.7 | Revisão da Fase 1. `AutorizacaoActionsTests` (reflexão: verbo HTTP explícito, escrita com `[Permissao]` de escrita ou `[SomenteAdminGlobal]`, anônimo só no login/erro) → 4 `Index` ganharam `[HttpGet]`. `/security-review`: ADMIN_GLOBAL vinha só da claim (rebaixado seguia global e podia se promover) → `SessaoValidaAsync` compara com o banco. `/code-review` medium: 9 achados — corrigidos `TG_MOV_BI` (trocava a empresa em silêncio → `PR_CONFERIR_EMPRESA`), `TG_PEDVEN_BI`/`TG_RE_BI` (empresa do pai antes de numerar), `SP_ATUALIZAR_PEDIDO_REVISAO` (sem revisão atual apagava os itens; sem mudança não audita mais), IP da auditoria limitado a 45 sem scope id, comentário do `SaveChangesAsync`; registrados os demais (tabela em F1 › 1.7). Skills do projeto `/concluir-tarefa` e `/mudanca-banco`. MDs dos objetos regenerados; roteiro SQL 49/49; testes 89/89. `recriar.ps1` passou a ler a senha do user-secret `ConnectionStrings:LabControl` (sem pergunta). **Fase 1 concluída** | Opus 5.5 |
| 20/10/2026 | 2.1 | CRUD genérico dos auxiliares: `AuxiliarDefinicao<T>` + `CatalogoAuxiliares` (uma entrada por tabela), `AuxiliarService` (tamanho/obrigatoriedade do modelo do EF; busca nos campos marcados; 20 por página; duplicidade pela `UNIQUE`; exclusão por `DELETE` filtrado — em uso o banco recusa e a tela oferece Inativar; combo = ativos + o escolhido), `AuxiliaresController` (`/Auxiliares/{tabela}`, modal, `Rapido` JSON para o "+" da 2.4, `Opcoes`), views `Index/Lista/_Form` (aviso ao fechar com alterações, como o legado). Idiomas aprovado pelo usuário. Skill `/novo-auxiliar` para a 2.2. `AuxiliaresTests` (5); testes 94/94 | Opus 5.5 |
| 20/10/2026 | P25 (3.1, 3.2, parte de 2.2/2.3) | **Pedido do usuário:** cadastros de Empresa usuária, Clientes/Fornecedores e Funcionários com **vários endereços (tipo de endereço + tipo de logradouro), telefones e e-mails tipados**, como o legado (`un_Entidades`). Decisões do usuário: empresa usuária = **entidade vinculada** (D-19) e escopo completo. **Banco:** `EMPRESAS` sem as colunas soltas de endereço/fone/e-mail + `ID_ENTIDADES`; `ENTIDADES.TIPO_EMPRESA_USUARIA`; `SP_INICIALIZAR_EMPRESA` cria a entidade e os tipos (fones/e-mails do legado); `TG_EMPRESAS_AU` espelha a identidade; `TG_ENT_BI/BU` (empresa usuária exclusiva); `TG_ENTEND/ENTFON/ENTEML_BI/BU` + `PR_CONFERIR_ENDERECO/FONE/EMAIL/TIPOS_ENTIDADE` (um endereço ativo por tipo, um principal, contato não muda de entidade, e-mail minúsculo); `06_cidades.sql` com 5.571 municípios IBGE (`tools/gerador-cidades`); roteiro SQL passo 14 → 62/62. **C#:** `ContatosService` + componente `_ContatosAbas`/`_ContatosModais`/`contatos.js` (AJAX, 422 com campo) usado por T03 (`/Empresa/Dados` com abas), T12 (`ClientesController`/`ClienteService`, PF/PJ, CPF/CNPJ com DV, tipos, observações) e T13 (`ProfissionaisController`/`ProfissionalService`: cargo, registro obrigatório para assinar, assinatura PNG ≤ 300 KB + SHA-256, vínculo com usuário, inativo desvincula); `GeografiaController` (cidades + CEP via ViaCEP no servidor, cache, tipo de logradouro reconhecido); auxiliares T10-01…06 no catálogo (rótulo do enum por `[Display]`). Cuidados: rota de contatos usa `{idEntidade}` (o `Id` do formulário venceria o `{id}` da rota no binder); `confirm()` com nome em string JS era XSS (corrigido também em Perfis). Docs: P25 novo, D-19…D-21, MDs dos objetos regenerados (34 rotinas, 48 triggers), T03/T12/T13/T10-01…06/T11-03, M-01/M-02/doc 13, matriz. Testes novos: `CadastrosEntidadesTests` (9), `CadastrosTelasTests` (4); `EmpresaServiceTests` ajustado (empresa nova nasce com a sua entidade; cidades em `/Geografia`); suíte 107/107 | Opus 5.5 |
| 20/10/2026 | Correção pós-teste | **Relato do usuário:** cliente "apagado" ao clicar no olho cortado; "erro ao cadastrar, pesquisar". **Diagnóstico** (log do app + banco): nenhuma exceção; o cliente estava gravado com os 2 endereços, 2 telefones e 2 e-mails, mas `INATIVO = 1` — o botão era **Inativar** (só ícone, sem confirmação) e a lista/pesquisa escondem inativos sem avisar; a busca de CEP tinha funcionado (ViaCEP respondeu). **Correções:** botões das listas com ícone + texto (Auxiliares, Clientes, Profissionais, Usuários, Perfis, Empresas, abas de contatos); confirmação ao inativar explicando que o registro não é apagado; mensagem pós-inativação com "marque Mostrar inativos"; rodapé avisa "N inativo(s) também correspondem à pesquisa — mostrar". **Prova do CRUD real:** testes de ponta a ponta no navegador (`tests/LabControl.Tests/Navegador/`, Playwright + Edge sem janela, app real no Kestrel, [P26](processos/P26-testes-ponta-a-ponta.md)) — 8 testes cobrindo T02, T03, T04, T05, T10 (Idiomas e Tipos de endereço), T12 (inclusive Buscar CEP, principal, recusa do 2º endereço do mesmo tipo) e T13; nenhum erro de JavaScript. Docs: P26 novo; T02/T03/T04/T05/T10-00/T12/T13, P25, F1/F2/F3 (definição de pronto), CLAUDE.md, skill `/concluir-tarefa` (teste no navegador obrigatório); seção **Pendências registradas** no plano. Testes 115/115 | Opus 5.5 |
| 20/10/2026 | 2ª correção pós-teste | **Relato do usuário:** entra no sistema sem pedir login; mensagem em branco ao reativar; usuário recém-cadastrado não aparece no combo do profissional; cadastro de cidades não funciona; dois cadastros de empresa ("Empresa" e "Empresas"). **Antes:** o computador travou no meio da correção e `Infra/EmpresaContexto.cs`, `Program.cs` e `Services/ContaService.cs` ficaram só com bytes nulos (nada commitado) — restaurados do `9fbaed1` (cópias corrompidas guardadas no scratchpad) e o trabalho refeito. **Causas e correções:** (1) cookie de 8 h + chaves do Data Protection sobrevivem ao reinício e o Edge restaura cookies de sessão → claim `lc:instancia` (GUID por execução) conferida no `ValidarSessao`: app reiniciado sempre pede login; (2) `data-confirmar` nulo vira `""` em form com tag helper → `confirm("")` vazio; Reativar ganhou texto e o script ignora confirmação vazia (Auxiliares, Clientes, Perfis, Profissionais); (3) inclusão de usuário por ADMIN_GLOBAL vinha sem empresa marcada → sem `USUARIOS_EMPRESAS` → fora do combo da T13; agora a empresa ativa vem marcada e padrão, e a T13 explica onde dar o acesso; (4) a action JSON `Geografia/Cidades` fazia o menu mostrar a tela T11-03 (2.3) → renomeada `CidadesDaUf`; (5) *Empresa* e *Empresas* eram o mesmo cadastro (EMPRESAS; ativa × todas) → um item **"Empresas Usuárias do Sistema"** (ADMIN_GLOBAL vê a lista, os demais caem na empresa ativa), títulos/mensagens "empresa usuária". Testes novos: sessão de outra execução, combo do profissional, menu real (sem Cidades, um item de empresa), diálogo de reativar no navegador. Docs: T00, T02, T03, T11-03, T12, P25, F2, matriz. Testes 118/118 | Opus 5.5 |
| 21/10/2026 | 2.2 | Usuário testou os cadastros com contatos (T03/T12/T13): ok. **13 auxiliares** T10-08…T10-20 no `CatalogoAuxiliares` (unidades, embalagens, tipos de resultado, condições de pagamento, prazos, moedas, tipos/status de amostra, tipos/status/métodos de análise, status de boletim e de proposta). Regras: T10-14/T10-16 código 1–99 travado (`PodeAlterarCampo`: amostra; na análise também parâmetro); T10-15 sempre um status ativo que permite resultado (`ValidarAntesDeGravar` + gancho novo `ValidarAntesDeExcluir`); T10-11 parcelas em tela própria (`CondicoesPagamentoController`/`CondicaoPagamentoService`/`Views/CondicoesPagamento/Parcelas`: dias 0–3650, % até 4 casas, Σ = 100%, regravadas no lugar por causa do `UNIQUE` do nº) aberta pelo botão da linha (gancho novo `Acao`). **Bugs evitados no genérico:** campo `short` virava `int` no ternário do `Converter` (InvalidCastException ao gravar código); duplicidade de sigla/código ia sempre para o 1º campo → agora vai para o campo citado na mensagem. Skill `/novo-auxiliar` atualizada com os ganchos novos. Testes novos: `AuxiliaresEspecificosTests` (24) e 2 no navegador; suíte 144/144 | Opus 5.5 |
| 21/10/2026 | 2.3 | T11-01…03: `GeografiaController` (Paises/Estados/Cidades com `GEOGRAFIA.CONSULTAR`; Gravar/Excluir/ImportarIbge com `[SomenteAdminGlobal]`) + `Services/Geografia/GeografiaService.cs` + uma view `Views/Geografia/Lista.cshtml` com abas. Nomes em maiúsculas (como o seed); estado: sigla 2 letras única por país, IBGE 2 dígitos; cidade: IBGE 7 dígitos **começando pelo código da UF** (decisão: a busca de CEP casa pelo código); excluir só sem filhos — a cidade conta endereços de **todas** as empresas (`IgnoreQueryFilters` só nessa checagem; a lista não mostra o uso). **Importar municípios IBGE**: CSV `código; nome; UF` (`;`/`,`/tab, cabeçalho opcional, UTF-8 ou Latin-1), UF pelo prefixo entre os estados do Brasil (o código de UF não é único no banco), inclui/atualiza pelo código, nunca exclui, até 20 erros por linha no relatório, transação + evento de auditoria. Itens Países/Estados/Cidades de volta ao menu. Usuário testou no app: ok. Testes `GeografiaTests` (4) + navegador (1); suíte 149/149 | Opus 5.5 |
| 21/10/2026 | 2.4 | Cadastro rápido "+": `Infra/CadastroRapidoTagHelper.cs` (`<select lc-rapido="TABELA">` → `data-rapido` só com AUXILIARES.INCLUIR) + `site.js` (botão no `input-group`, `_Form` do auxiliar num modal sem "Ativo", `POST /Rapido`, item em ordem alfabética em todos os combos da tabela e já selecionado; 422 no campo). Dentro de outro modal (contatos) o "+" fica **dentro** do `.modal` de baixo, sem backdrop (CSS `.modal-rapido-sobre`), para o foco preso do Bootstrap não brigar e o Esc fechar só o de cima. **Achado no teste:** o Bootstrap ignora `hide()` durante a animação de abertura → fechar depois de gravar espera o `shown` (`data-pronto`). Aplicado em T13 (cargo, registro) e nos modais de contatos (4 tipos). Sem combo de idioma ainda (proposta, 4.2): aceite provado com cargo e tipo de endereço. Usuário testou no app: ok. Testes `CadastroRapidoTests` + navegador; suíte 151/151 | Opus 5.5 |
| 21/10/2026 | 2.5 | `/code-review medium` da Fase 2 (`f0c1ee7^..HEAD`: 2.1–2.4 + T03/T12/T13/contatos antecipados) — **10 achados**, verificados no código. **Corrigidos (6):** contatos pela tela de clientes aceitavam um profissional (só `CLIENTES.ALTERAR` mexia nos contatos da T13) → `PertenceAsync` com o critério da lista da T12; "Inativar/está em uso" oferecido em qualquer falha ao excluir → só com `ErrosMySql.MensagemEmUso` (1451); CEP sem IBGE caía em cidade digitada sem código; regra do T10-15 bloqueava qualquer edição de status numa empresa sem status que permite resultado → só barra quando a operação tira o último (valores originais do EF); gênero no aviso de em uso; lista de empresas com a cidade do endereço de nota fiscal. **Registrados (4):** CNPJ da empresa na entidade ocupando o `UQ_ENT_DOC` (pendência 15, decisão + `/mudanca-banco`), sessão por execução × reciclagem do IIS (pendência 16, comportamento pedido pelo usuário — rever antes da virada), duplicações de combo de UF/cidades e de paginação/normalização (pendência 5, `/simplify`). Testes novos: 3 casos; suíte 153/153. **Fase 2 concluída** | Opus 5.5 |
| 21/10/2026 | `/simplify` (fim da Fase 2) | Pendência 5 + 4 revisores (reuso, simplificação, eficiência, altitude) sobre `f0c1ee7^..HEAD`. **Aplicado:** auditoria com **um INSERT de várias linhas** por lote de 500 (era um por linha) e prelúdio comum dos dois `SaveChanges` (`Preparar`); `Resultado`/`Falha` ganharam **`TipoFalha`** (Negócio, Duplicado, EmUso, ReferênciaAusente) preenchido pelo `ErrosMySql` e `r.Falha` para repassar o erro sem perder campo/tipo — fim das comparações de texto (`StartsWith("Já existe")`, `Contains("CPF/CNPJ")`, `== MensagemEmUso`); `Infra/Textos` (Limpo/Maiúsculas, 8 cópias removidas) e `Infra/Paginacao` (8 cópias); `ConsultasGeografia` (UFs e cidades da UF; o controller não consulta mais o contexto); `Entidade.DaTelaDeClientes` (critério da T12 num lugar só); confirmação `data-confirmar` num listener delegado do `site.js` (5 cópias nas views); parâmetros sem uso e expressões redundantes. **Não aplicado (registrado):** partial de paginação/aviso de inativos nas views, record por tela na Geografia, filtro dentro dos VMs de formulário, contagens das listas numa consulta só, cache de UFs/cidades, pré-checagens da exclusão da Geografia × mensagem por FK — ganhos pequenos ou mudança de comportamento. Suíte 153/153 (1ª rodada: 3 timeouts de contenção ao criar empresa de teste; isolados e na 2ª rodada passaram) | Opus 5.5 |
