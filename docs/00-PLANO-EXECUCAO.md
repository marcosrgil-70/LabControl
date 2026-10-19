# 00 — Plano de Execução do LabControl v2

> **Leia este arquivo no início de toda sessão.** Ele diz onde paramos e o que vem a seguir.
> Ao terminar uma tarefa: marque `[x]` aqui **e** no arquivo da fase, atualize o "Status" do MD da tela, a [matriz Delphi → C#](plano/MATRIZ-DELPHI-CSHARP.md) e registre no §8 (Diário).

## ▶ PONTO DE RETORNO (19/10/2026)
- **Concluído:** 0.1 a 0.6 — `labcontrol_v2` recriável (`.\Scripts\v2\recriar.ps1`); `LabDbContext` com 69 entidades geradas de `Scripts/v2` (`node tools/gerador-csharp/gerar.mjs`); filtro da empresa ativa + trava no `SaveChanges`; interceptor `@LC_*` + READ COMMITTED, `Resultado<T>`, `ErrosMySql`, `Procedures`, `ErroNegocioFilter`; auditoria na mesma transação; **deadlock da numeração corrigido** (`SP_PROXIMO_NUMERO`); código v1 em `Legacy/`; connection string em user-secrets; `dotnet test tests/LabControl.Tests` 20/20 no banco **`labcontrol_teste`** (recriado sozinho quando os scripts mudam).
- **Próxima tarefa:** **Fase 0 → 0.7 Projeto de testes** ([F0-fundacao](plano/F0-fundacao.md#07-projeto-de-testes)) — já adiantada: falta portar os 12 passos do roteiro 12 §7 para C# (pelos helpers) e decidir banco por execução × persistente.
- ⚠️ A senha `root` do MySQL ficou no histórico do git (`appsettings.json` até a 0.3): trocar a senha do MySQL e atualizar o user-secret.
- Ao retomar, apague/atualize este bloco.

## 1. Ponto de partida (17/10/2026)

- ✅ Análise do Firebird, das procedures e das telas Delphi concluída.
- ✅ Script MySQL v2 (`Scripts/v2/01..05`) **validado** no MySQL 8.0.46 local.
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
- [ ] 0.7 Projeto xUnit com banco de teste recriado pelos scripts + roteiro 12 §7 automatizado (adiantado na 0.5: banco `labcontrol_teste` por hash dos scripts, `Cenario` com o roteiro até o boletim; falta portar os 12 passos e decidir banco por execução × persistente).
- [ ] 0.8 Estrutura de pastas e isolamento da v1 (isolamento feito na 0.3: `Legacy/`; falta `Services/`, `ViewModels/`, `Infra/` e CSS).
- [ ] 0.9 `/code-review high`.

### Fase 1 — Login, multiempresa e permissões 🟢 (📐) ✋ — [detalhes](plano/F1-login-permissoes.md)
- [ ] 1.1 T00 Login + troca de senha + seleção/troca de empresa.
- [ ] 1.2 T01 Layout, menu por permissão, painel.
- [ ] 1.3 T02 Usuários (empresas, perfis, profissional vinculado).
- [ ] 1.4 T04 Perfis e matriz de permissões.
- [ ] 1.5 🔵 T03 Empresa usuária + nova empresa + cópia de cadastros.
- [ ] 1.6 T05 Parâmetros da empresa + numeração auditada.
- [ ] 1.7 `/security-review` + `/code-review`.

### Fase 2 — Cadastros básicos 🟢 → ⚪ ⚡ — [detalhes](plano/F2-cadastros-basicos.md)
- [ ] 2.1 🟢 CRUD genérico aprovado com Idiomas (T10-00/T10-07).
- [ ] 2.2 ⚪⚡ Replicar para os outros 19 auxiliares (T10-01…T10-20; atenção a T10-14/T10-16).
- [ ] 2.3 🟢 Geografia (T11-01…03) + importação IBGE + componente Endereço (ViaCEP).
- [ ] 2.4 🟢 Cadastro rápido "+".
- [ ] 2.5 `/code-review`.

### Fase 3 — Cadastros 🟢 (📐 antes da T12) — [detalhes](plano/F3-cadastros.md)
- [ ] 3.1 T12 Clientes/fornecedores.
- [ ] 3.2 T13 Profissionais (+ vínculo com usuário, assinatura).
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
| 4 | Perfis padrão além de ADMINISTRADOR (Recepção, Analista, Responsável Técnico, Comercial)? | 1 | T04 |
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
