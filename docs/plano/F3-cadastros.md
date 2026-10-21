# Fase 3 — Cadastros (clientes, profissionais, produtos, parâmetros) + Migração parte 1

**Objetivo:** cadastros principais completos e a primeira carga real do legado (empresa, auxiliares, entidades, contatos, endereços, profissionais, usuários, produtos e parâmetros).
**Modelo/modo:** 🟢 Sonnet (📐 antes da T12) · migração 🔵 Opus + 📐 + ✋.
**Ler antes:** [T12](../telas/T12-clientes.md) · [T13](../telas/T13-profissionais.md) · [T14](../telas/T14-produtos.md) · [T15](../telas/T15-parametros-analise.md) · [RN-03](../regras/RN-03-cadastros.md) · [P24](../processos/P24-migracao.md) · [M-01…M-10](../processos/migracao/README.md) · [T07](../telas/T07-migracao.md)
**Depende de:** Fase 2.

---

### 3.1 T12 — Clientes/Fornecedores
> ✅ **Antecipada e concluída em 20/10/2026** (pedido do usuário: cadastros com vários endereços/telefones/e-mails tipados, como o legado) — ver [T12 › Implementação](../telas/T12-clientes.md#implementação-antecipada-em-20102026--pedido-do-usuário) e [P25](../processos/P25-cadastro-entidades-contatos.md); aceite em `CadastrosEntidadesTests`/`CadastrosTelasTests`.
- **Passos:** Index (filtros nome/fantasia/documento, tipo, inativos); formulário com abas Dados básicos (F/J, CPF/CNPJ com DV e unicidade — mensagens do legado), Endereços (componente da 2.3; um ativo por tipo), Telefones (um principal), E-mails/Contatos (únicos; flags principal, NF-e, laudo), Observações; endpoints AJAX por aba; excluir só sem movimento.
- **Aceite:** checklist da T12; cliente com contatos pronto para proposta.
- **Esforço:** G

### 3.2 T13 — Profissionais
> ✅ **Antecipada e concluída em 20/10/2026** — ver [T13 › Implementação](../telas/T13-profissionais.md#implementação-antecipada-em-20102026--pedido-do-usuário); falta só a assinatura no PDF do boletim (5.3/5.4).
- **Passos:** entidade PF com `TIPO_FUNCIONARIO` + `ENTIDADES_FUNCIONARIOS` (cargo, registro/UF, assina boletim, executa análise, assinatura PNG ≤ 300 KB com SHA-256); campo "Usuário do sistema" (serviço único da 1.3); abas de e-mail/telefone reaproveitadas da T12.
- **Aceite:** [aceite da T13](../telas/T13-profissionais.md#aceite).
- **Esforço:** M · **Depende de:** 3.1

### 3.3 T14 — Produtos
> ✅ **Concluída em 21/10/2026** — ver [T14 › Implementação](../telas/T14-produtos.md#implementação-tarefa-33); aceite em `ProdutosTests` + navegador `CrudNavegadorTelasTests.Produto_crud_com_codigo_automatico_e_unidade_pelo_mais`.
> ✅ **Concluída em 21/10/2026** — ver [T14 › Implementação](../telas/T14-produtos.md#implementação-tarefa-33); aceite em `ProdutosTests` + navegador `CrudNavegadorTelasTests.Produto_crud_com_codigo_automatico_e_unidade_pelo_mais`.
- **Passos:** CRUD (código vazio → banco gera; reler); unidade, embalagem, qtde por embalagem; cadastro rápido para a proposta.
- **Aceite:** código automático de 6 dígitos; duplicidade recusada.
- **Esforço:** S

### 3.4 T15 — Parâmetros de análise
> ✅ **Concluída em 21/10/2026** — ver [T15 › Implementação](../telas/T15-parametros-analise.md#implementação-tarefa-34); aceite em `ParametrosAnaliseTests` + navegador `CrudNavegadorTelasTests.Parametro_de_analise_crud_com_metodo_pelo_mais`.
- **Passos:** CRUD com descrição reduzida obrigatória, tipo de análise, método padrão, descrição do método, valor unitário, acreditado; filtros por tipo e acreditado.
- **Aceite:** sem descrição reduzida não grava; método padrão disponível para a proposta.
- **Esforço:** S

### 3.5 Migração parte 1 (M-01 a M-10) 🔵📐✋
> ✅ **Concluída em 21/10/2026** (plano aprovado no plan mode) — ver [T07 › Implementação](../telas/T07-migracao.md#implementação--parte-1-tarefa-35-21102026); aceite em `MigracaoParte1Tests` (conferências verdes e login com troca de senha contra o `DBMAIS.FDB` real), `MigracaoConversoesTests`, `MigracaoTelaTests`. **Pendente:** passar o teste de navegador da T07 e a suíte completa com a máquina livre (pendência 17).
- **Passos:**
  1. `Services/Migracao/MigracaoService.cs` (estrutura de etapas, dicionários `idAntigo → idNovo`, log, transação por etapa, `@LC_MIGRACAO = 1`).
  2. Tela T07 com seleção de etapas, log e conferências.
  3. Implementar M-01 … M-10 conforme cada MD (atenção: M-03 `CODIGO = ID antigo`; M-06 mapa de e-mails; M-09 senhas provisórias aleatórias — confirmar dúvida 7).
  4. Rodar num banco limpo; conferir contagens (527 entidades, 664 e-mails, 695 fones, 538 endereços, 1.915 produtos, 462 parâmetros, 176 métodos).
- **Aceite:** conferências das etapas verdes; login de um usuário migrado com troca de senha.
- **Esforço:** G · **Depende de:** 3.1–3.4

### 3.6 Revisão
`/code-review`; atualizar diário e a [matriz](MATRIZ-DELPHI-CSHARP.md).

## Definição de pronto
- [ ] Cadastros principais com regras do legado e correções.
- [ ] Cadastros reais do legado carregados num banco limpo, conferidos.
- [ ] CRUD de cada tela provado no navegador (P26) — T12 e T13 já cobertos (20/10/2026).
