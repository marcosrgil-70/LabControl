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
> ✅ **Concluída em 21/10/2026** (plano aprovado no plan mode) — ver [T07 › Implementação](../telas/T07-migracao.md#implementação--parte-1-tarefa-35-21102026); aceite em `MigracaoParte1Tests` (conferências verdes e login com troca de senha contra o `DBMAIS.FDB` real), `MigracaoConversoesTests`, `MigracaoTelaTests`. Teste de navegador da T07 e suíte completa (175/175, em duas partes) passaram em 22/10/2026 (pendência 17).
- **Passos:**
  1. `Services/Migracao/MigracaoService.cs` (estrutura de etapas, dicionários `idAntigo → idNovo`, log, transação por etapa, `@LC_MIGRACAO = 1`).
  2. Tela T07 com seleção de etapas, log e conferências.
  3. Implementar M-01 … M-10 conforme cada MD (atenção: M-03 `CODIGO = ID antigo`; M-06 mapa de e-mails; M-09 senhas provisórias aleatórias — confirmar dúvida 7).
  4. Rodar num banco limpo; conferir contagens (527 entidades, 664 e-mails, 695 fones, 538 endereços, 1.915 produtos, 462 parâmetros, 176 métodos).
- **Aceite:** conferências das etapas verdes; login de um usuário migrado com troca de senha.
- **Esforço:** G · **Depende de:** 3.1–3.4

### 3.6 Revisão
> ✅ **Concluída em 23/10/2026** — `/code-review medium` (`d76118a..HEAD`): 10 achados, 9 corrigidos, 1 registrado (tabela abaixo); pendências 18 e 19 resolvidas; suíte inteira de uma vez 186/186 (17 min). **Fase 3 completa.**

`/code-review`; atualizar diário e a [matriz](MATRIZ-DELPHI-CSHARP.md).
- ~~**Antes:** pendência 17~~ — feita em 22/10/2026 (175/175 rodando a migração separada).
- **Incluir:** pendência 18 — `SP_INICIALIZAR_EMPRESA` estoura 30 s quando a suíte roda inteira junto com `MigracaoParte1Tests` (sozinha leva 0,04 s: é espera de lock, não lentidão; esperas vistas no `SP_PROXIMO_NUMERO`/`SEQUENCIAS_DOCUMENTOS` e em `commit` lento) → isolar o teste da migração (coleção xUnit sem paralelismo) e medir de novo ([P23](../processos/P23-nova-empresa.md)).
- **Incluir:** pendência 19 — `recriar.ps1` falhou ao ler o próprio `saida.txt` ("usado por outro processo", linha 100) ao ser chamado pelo `MigracaoParte1Tests`.
- **Feito:** 18 → `ColecaoMigracao` (`DisableParallelization = true`) no `MigracaoParte1Tests`: roda sozinho, depois das outras; suíte inteira 186/186. 19 → `LerSaida` no `recriar.ps1` (abre com `FileShare.ReadWrite` e tenta de novo por até 10 s); roteiro SQL 65/65.

| # | Achado do `/code-review` | Situação |
|---|---|---|
| 1 | T14/T15: "1.000" lido como 1 (valor unitário, qtde. por embalagem) | ✅ `Textos.Decimal` (grupos de 3 = milhar) + `TextosTests` |
| 2 | M-10: descrição de parâmetro repetida " (2)" podia repetir de novo / ser cortada de volta | ✅ `ContextoMigracao.Unico` |
| 3 | M-10: código de produto repetido `{código}-{ID}` passava de 10 caracteres | ✅ `ContextoMigracao.Unico` |
| 4 | M-03: descrição de tipo de amostra/análise " ({ID})" passava de 60 caracteres | ✅ `ContextoMigracao.Unico` |
| 5 | M-10: tipo "NÃO INFORMADO" com código 99 fixo podia colidir com o M-03 | ✅ reaproveita o existente ou maior código livre |
| 6 | M-01/M-05: CPF/CNPJ fora de 11/14 dígitos sem corte (VARCHAR 18) | ✅ `Cortar` |
| 7 | M-08: entidade que é só a empresa, com linha de funcionário, virava funcionário → 45000 | ✅ fica de fora, com anomalia |
| 8 | M-04: `SP_INICIALIZAR_EMPRESA` por `ExecuteSqlRaw`, fora do `Procedures` | ✅ `Procedures.InicializarEmpresaAsync` |
| 9 | T07: CSV com situação + erro da etapa em campo inválido | ✅ um campo só, entre aspas |
| 10 | M-03 conferência N+1; M-09 `SaveChanges` por usuário | ✅ conferência numa consulta; M-09 mantido (15 usuários) |

## Definição de pronto
- [ ] Cadastros principais com regras do legado e correções.
- [ ] Cadastros reais do legado carregados num banco limpo, conferidos.
- [ ] CRUD de cada tela provado no navegador (P26) — T12 e T13 já cobertos (20/10/2026).
