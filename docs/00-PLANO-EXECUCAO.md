# 00 — Plano de Execução do LabControl v2

> **Leia este arquivo no início de toda sessão.** Ele diz onde paramos e o que vem a seguir.
> Ao terminar uma tarefa: marque `[x]`, atualize o "Status" do MD da tela e registre em §6 (Diário).

## 1. Ponto de partida (16/10/2026)

- ✅ Análise do Firebird, das procedures e das telas Delphi concluída (docs `banco/` e `telas/`).
- ✅ Script MySQL v2 (`Scripts/v2/01..05`) **validado** no MySQL 8.0.46 local (banco temporário, depois removido).
- ✅ Decisões do usuário: **banco recriado do zero**, **tudo por empresa**, **Pedido de Venda com tela própria**, **ano da amostra = ano do servidor**.
- ⚠️ Projeto C# atual (v1): funciona parcialmente, mas com modelo de dados errado (doc `_obsoleto_v1/`). **Reaproveitar só layout, padrões visuais e a estrutura do projeto**; controllers/models serão reescritos sobre o schema v2.
- Ordem pedida pelo usuário: **cadastros básicos → cadastros → recebimento de amostras → inclusão de resultados → boletim**, depois o resto em qualquer ordem.

## 2. Qual modelo e qual modo do Claude usar

### Modelos (`/model`)
| Modelo | Quando usar | Exemplos neste projeto |
|---|---|---|
| **Opus 5.5** (`claude-opus-5-5`) | Análise, arquitetura, regra de negócio complexa, banco (procedures/triggers), migração, revisão de código crítica, depuração difícil | Fases 0, 4 (amostras), 5 (resultados/boletim), revisões, migração, `SP_*`, PDF do boletim |
| **Sonnet 5.5** (`claude-sonnet-5-5`) | Implementação do dia a dia seguindo um MD já detalhado: telas CRUD com regras, controllers, views, services comuns, testes | Fases 1, 2, 3, 6, 7, 8 |
| **Haiku 5.5** (`claude-haiku-5-5`) | Tarefas mecânicas e repetitivas, já com um modelo pronto para copiar | Replicar o CRUD genérico para os 20 auxiliares, ajustar textos/labels, atualizar checklists, pequenos ajustes de CSS |

Regra prática: **se a tarefa exige decidir algo que não está escrito no MD → Opus. Se o MD já decide tudo → Sonnet. Se é cópia de um padrão pronto → Haiku.**

### Modos do Claude Code (Shift+Tab alterna)
| Modo | Quando |
|---|---|
| **Plan mode** (planejar sem editar) | Antes de **cada tela complexa** (T20, T22, T26, T27, migração): Claude lê o MD da tela, os arquivos atuais e propõe o plano; você aprova |
| **Modo padrão** (pede aprovação a cada edição) | Banco (scripts SQL), segurança/login, migração, qualquer coisa que mexa em dados |
| **Accept edits** (aceita edições automaticamente) | CRUDs repetitivos depois que o primeiro foi aprovado (auxiliares, relatórios) |

### Comandos úteis
- `/code-review` ao final de **cada fase** (nível `high` nas fases 4 e 5).
- `/security-review` ao final da fase 1 (login/permissões) e antes da virada.
- `/simplify` depois de fases grandes, para limpar duplicação.
- `/run` para subir o sistema e conferir a tela no navegador.
- Esforço de raciocínio (`/effort`, se disponível): alto para Opus nas fases 4/5/migração; médio para Sonnet.

## 3. Fases e tarefas

Legenda: 🔵 Opus · 🟢 Sonnet · ⚪ Haiku · 📐 Plan mode · ✋ modo padrão · ⚡ accept edits

### Fase 0 — Fundação (banco + projeto) 🔵 ✋
- [ ] 0.1 Criar o banco `labcontrol_v2` com `Scripts/v2/01..05` (fazer backup do `labcontrol` antes; o antigo não é apagado).
- [ ] 0.2 Criar `Scripts/v2/README.md` com o comando de criação/recriação (`mysqlsh --sql … --file`).
- [ ] 0.3 Novo `LabDbContext` mapeando o schema v2 (Fluent API por tabela, sem migrations EF). Remover os "patches de schema" do `Program.cs`.
- [ ] 0.4 `IEmpresaContexto` + Global Query Filter por `ID_EMPRESAS` + preenchimento automático no `SaveChanges`.
- [ ] 0.5 `SessaoMySqlInterceptor` (`SET @LC_ID_USUARIOS…`) e tratamento de `MySqlException` SQLSTATE 45000 → mensagem ao usuário.
- [ ] 0.6 Projeto de testes (xUnit) que recria um banco de teste com os scripts e roda o roteiro do doc 12 §7.
- [ ] 0.7 `/code-review`.

### Fase 1 — Login, multiempresa e permissões 🟢 (📐 antes) ✋
- [ ] 1.1 T00 Login + troca de senha + seleção/troca de empresa (Cookie Auth, PBKDF2).
- [ ] 1.2 T01 Layout/menu por permissão + painel inicial (pode começar vazio).
- [ ] 1.3 T02 Usuários (com empresas e vínculo com profissional) e Perfis (matriz de permissões).
- [ ] 1.4 T03 Empresas + parâmetros da empresa + "copiar cadastros de outra empresa" (rotina nova; 🔵).
- [ ] 1.5 `/security-review` + `/code-review`.

### Fase 2 — Cadastros básicos (auxiliares) 🟢 → ⚪ ⚡
- [ ] 2.1 🟢 CRUD genérico de auxiliares (T10) implementado e aprovado com **um** auxiliar (Idiomas).
- [ ] 2.2 ⚪⚡ Replicar para os outros 19 auxiliares (atenção às regras específicas da tabela da T10: código imutável de tipos, finalidade de endereço, prazos, parcelas).
- [ ] 2.3 🟢 T11 Geografia + importação IBGE + componente de endereço com ViaCEP.
- [ ] 2.4 🟢 Componente "+" (cadastro rápido) para os lookups.

### Fase 3 — Cadastros 🟢 (📐 antes da T12)
- [ ] 3.1 T12 Clientes/Fornecedores (abas: básicos, endereços, fones, e-mails/contatos, observações).
- [ ] 3.2 T13 Profissionais (cargo, registro, assina boletim, executa análise, assinatura, **usuário vinculado**).
- [ ] 3.3 T14 Produtos.
- [ ] 3.4 T15 Parâmetros de análise.
- [ ] 3.5 🔵📐✋ **Migração parte 1** (doc 13: M-01 a M-10 — empresa, geografia, auxiliares, entidades, contatos, endereços, profissionais, usuários, produtos, parâmetros) + conferências.
- [ ] 3.6 `/code-review`.

### Fase 4 — Recebimento de amostras 🔵 📐 ✋
- [ ] 4.1 🔵 **Migração parte 2** (M-11 a M-15: propostas, produtos/análises, pedidos, amostras, testes, estoque, arquivo) + conferências (nº de amostras idêntico ao legado).
- [ ] 4.2 🟢 T20 Proposta **essencial** (cabeçalho, endereços/contatos, produtos, análises, totais — sem revisão/PDF ainda), para criar propostas novas.
- [ ] 4.3 🔵 T22 Recebimento de amostras completo (numeração/ano do servidor, data retroativa com permissão, validade/fabricação parciais, testes da proposta, observação, consulta, comprovante PDF, revisão).
- [ ] 4.4 🟢 T25 Etiquetas (código de barras).
- [ ] 4.5 Testes de aceite da T22 (todos os itens) + `/code-review high`.

### Fase 5 — Resultados e Boletim 🔵 📐 ✋
- [ ] 5.1 🔵 **Migração parte 3** (M-16 a M-18: resultados, boletins, sequências) + conferências.
- [ ] 5.2 🔵 T26 Inclusão de resultados (cartão por parâmetro, numérico × descritivo, especificação, fila de análises, revisão/desfazer).
- [ ] 5.3 🔵 T27 Boletim (seleção de parâmetros, metodologias automáticas, responsáveis, prévia, **emissão com PDF + hash**, revisão/desfazer).
- [ ] 5.4 🔵 Layout do PDF do boletim (QuestPDF) — validar com um boletim real do legado lado a lado.
- [ ] 5.5 Testes de aceite T26/T27 + `/code-review high`.

### Fase 6 — Propostas completas e Pedido de Venda 🟢 (📐 antes)
- [ ] 6.1 T20 completa: seleção múltipla de parâmetros, revisão (escolha da origem, motivo, destaque das linhas), textos e contrato, bloqueio de alteração com pedido/amostra, PDF da proposta, trocar produto.
- [ ] 6.2 T21 Pedidos de venda (gerar, situação, itens contratado × recebido, atualizar revisão, cancelar, PDF).
- [ ] 6.3 `/code-review`.

### Fase 7 — Estoque e arquivo 🟢 ⚡
- [ ] 7.1 T23 Movimentação (com parâmetros atendidos).
- [ ] 7.2 T24 Localização (arquivo/descarte, em lote).

### Fase 8 — Relatórios e complementos 🟢 ⚡
- [ ] 8.1 T30 seis relatórios gerenciais (tela + PDF + CSV).
- [ ] 8.2 T28 Relatório de Ensaio (**confirmar necessidade com o usuário antes**).
- [ ] 8.3 Envio de boletim por e-mail ao contato do resultado.
- [ ] 8.4 Auditoria (consulta).
- [ ] 8.5 `/simplify` + `/code-review`.

### Fase 9 — Homologação e virada 🔵 ✋
- [ ] 9.1 Migração completa em banco limpo + todas as conferências (doc 13 §3).
- [ ] 9.2 Homologação com os usuários do laboratório (roteiro: receber → resultado → boletim → revisar).
- [ ] 9.3 `/security-review`, backup, apontar `appsettings` para `labcontrol_v2`, desligar o Delphi.

## 4. Como começar cada sessão (copie e cole para o Claude)

```
Leia CLAUDE.md e docs/00-PLANO-EXECUCAO.md. Continue a próxima tarefa não marcada
da fase atual. Antes de codificar, leia o MD da tela correspondente em docs/telas/
e as tabelas em docs/banco/11-MODELO-MYSQL.md. Ao terminar, marque o checklist,
atualize o Status do MD da tela e escreva uma linha no Diário (§6).
```

## 5. Dúvidas em aberto (perguntar ao usuário quando chegar a hora)
1. Relatório de Ensaio (T28): ainda é necessário? (tabela vazia no legado)
2. Envio de boletim por e-mail: qual servidor SMTP / remetente?
3. Tamanho da etiqueta e impressora (T25).
4. Perfis padrão desejados além de ADMINISTRADOR (ex.: Recepção, Analista, Responsável Técnico, Comercial).
5. Layout definitivo do boletim/proposta/pedido: há modelos atuais em PDF/papel para copiar?
6. Retenção dos PDFs emitidos (pasta local, servidor de arquivos ou banco).
7. Senha provisória dos usuários migrados: gerar aleatória por usuário (recomendado) ou senha única informada pelo administrador?

## 6. Diário de execução
| Data | Fase/tarefa | O que foi feito | Modelo |
|---|---|---|---|
| 16/10/2026 | Análise | Análise completa do Firebird/Delphi, script MySQL v2 validado, MDs de banco, telas e este plano | Opus 5.5 |
