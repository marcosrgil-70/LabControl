# Fase 4 — Recebimento de Amostras (fase mais crítica)

**Objetivo:** receber amostras com numeração segura (ano do servidor), testes da proposta, estoque automático, comprovante, etiquetas e revisão — sobre propostas e amostras reais migradas.
**Modelo/modo:** 🔵 Opus · 📐 plan mode antes de 4.1 e 4.3 · ✋ modo padrão.
**Ler antes:** [P04](../processos/P04-numero-amostra-ano.md) · [P09](../processos/P09-recebimento-amostra.md) · [P10](../processos/P10-datas-parciais.md) · [P11](../processos/P11-revisao-amostra.md) · [P12](../processos/P12-estoque-amostras.md) · [P19](../processos/P19-etiquetas.md) · [RN-05](../regras/RN-05-amostras.md) · telas [T20](../telas/T20-propostas.md) (parte essencial), [T22](../telas/T22-recebimento-amostras.md), [T25](../telas/T25-etiquetas-amostras.md), [T29](../telas/T29-pesquisa-amostras.md) · objetos [`LAB_HIST_AMOSTRAS`](../banco/objetos/tabelas/LAB_HIST_AMOSTRAS.md), [`TG_AMOSTRAS_BI`](../banco/objetos/triggers/TG_AMOSTRAS_BI.md), [`SP_AMOSTRA_GERAR_TESTES`](../banco/objetos/rotinas/SP_AMOSTRA_GERAR_TESTES.md)
**Depende de:** Fase 3.

---

### 4.1 Migração parte 2 (M-11 a M-15) 🔵
> ✅ **Concluída em 23/10/2026** (plano aprovado no plan mode) — ver [T07 › Implementação parte 2](../telas/T07-migracao.md#implementação--parte-2-tarefa-41-23102026); aceite em `MigracaoTests` (todas as conferências verdes contra o `DBMAIS.FDB` real; os 5.767 números de amostra da visão do legado idênticos), `MigracaoConversoesTests`. Decisões do usuário: uma execução só (M-01→M-15) e log do legado migrado marcado "sem data". Números reais: 5.728/19.168 itens/análises (33/84 de propostas excluídas no legado) + 41 itens criados para amostras; 1.433 pedidos.
- **Passos:** propostas (+ textos RTF→HTML), produtos/análises (valores copiados), pedidos de venda (1 por proposta), amostras (`NR_AMOSTRA` conferido com o legado **em todas**), testes (`ID_TESTE_ORIGEM`), estoque (descartar movimentos de revisões, recalcular saldo), localização, log.
- **Aceite:** conferências do [doc 13 §3](../banco/13-MIGRACAO-DADOS.md) para estas etapas: 2.358 / 5.761 / 19.252; ≈1.435 pedidos; 5.770 amostras com números idênticos; 20.551 testes; amostras por ano 62/611/511/663/2.482/1.440 (+1).
- **Esforço:** G

### 4.2 T20 — Proposta essencial 🟢
> ✅ **Concluída em 23/10/2026** — ver [T20 › Implementação](../telas/T20-propostas.md#implementação-tarefa-42--parte-essencial-23102026); aceite em `PropostasTests` (300,00 − 10% = 270,00, número `00000N/AA-R0` do banco, mensagens do legado, bloqueio com amostra) + navegador `CrudNavegadorTelasTests.Proposta_crud_com_produto_analises_e_valores`. Pendência 3 cumprida aqui ("+" de método/idioma/prazo e produto).
> Pendência 3 (2.4): combos de **método, idioma e prazo** com o "+" — `lc-rapido="ANALISES_METODOS"`, `"IDIOMAS"`, `"PRAZOS"` ([T10-00](../telas/T10-00-crud-auxiliares.md#implementação-tarefa-24--cadastro-rápido-)); **produto** pelo `POST /Produtos/Rapido` (T14, 3.3).
- **Passos:** consulta (filtros, só última revisão), inclusão/alteração **sem** revisão/PDF: cabeçalho, comercial, cliente/contato, painéis de endereço do laudo e da cobrança (com sugestões por finalidade/flags), produtos (item automático, qtde), análises (parâmetro → método filtrado, idioma, prazo, tipo de documento, valores com recálculo na tela e no banco), textos, totais; mensagens obrigatórias do legado.
- **Aceite:** roteiro de valores do [P06](../processos/P06-valores-proposta.md) (300,00 − 10% = 270,00); proposta nova recebe `000001/26-R0` do banco.
- **Esforço:** G · **Depende de:** 4.1

### 4.3 T22 — Recebimento de amostras completo 🔵
> ✅ **Concluída em 23/10/2026** (plano aprovado no plan mode) — ver [T22 › Implementação](../telas/T22-recebimento-amostras.md#implementação-tarefa-43-23102026); aceite em `AmostrasTests` (número do banco no ano do servidor, testes da proposta, estoque, pedido, data futura/retroativa, revisão, 6 inclusões simultâneas) + navegador `Amostra_recebimento_testes_comprovante_e_exclusao`. Decisão do usuário: comprovante como página de impressão (PDF fica para o boletim, 5.3). Virada de ano só no roteiro SQL (depende do relógio do servidor).
- **Passos:** seguir o fluxo do [P09](../processos/P09-recebimento-amostra.md) passo a passo: cadeia cliente → proposta → item → tipos; prévia do número; datas (futura, retroativa com confirmação + permissão + `@LC_PERMITE_RETROATIVO`, aviso de ano diferente); datas parciais ([P10](../processos/P10-datas-parciais.md)); gravação em transação com `SP_AMOSTRA_GERAR_TESTES`; aba testes (só leitura); observação rich text; consulta com filtros e "só última revisão"; alteração com travas; exclusão; **comprovante PDF**; **gerar revisão** (`SP_GERAR_REVISAO_AMOSTRA`, motivo); aba histórico (log).
- **Aceite:** todos os itens do [aceite da T22](../telas/T22-recebimento-amostras.md#aceite) + tabela de cenários do [P04](../processos/P04-numero-amostra-ano.md#cenários-de-teste-aceite), incluindo teste automatizado de duas inclusões simultâneas.
- **Esforço:** G · **Depende de:** 4.2

### 4.4 T29 — Pesquisa de amostras (componente) 🟢
> ✅ **Concluída em 23/10/2026** — ver [T29 › Implementação](../telas/T29-pesquisa-amostras.md#implementação-tarefa-44-23102026); aceite em `AmostrasTests.Pesquisa_filtra_e_nunca_mostra_outra_empresa` + navegador `Pesquisa_de_amostras_seleciona_e_abre`. Usado pela T22 ("Pesquisa avançada"); T24/T26/T27 o adotam quando forem feitas.
- **Passos:** modal reutilizável com filtros e devolução `{id, nrAmostra}`.
- **Aceite:** usado pela T22 (e depois T24/T26/T27); nunca mostra outra empresa.
- **Esforço:** S · **Depende de:** 4.3

### 4.5 T25 — Etiquetas 🟢
> ✅ **Concluída em 23/10/2026** (exceto a impressão de teste na Argox real, que não está no computador de desenvolvimento — pendência 21) — ver [T25 › Implementação](../telas/T25-etiquetas-amostras.md#implementação-tarefa-45-23102026); aceite em `EtiquetasTests` (comandos PPLB 80 × 50 mm, Code 128, cópias, sigilo, impressão na escolhida, spooler real do Windows) + navegador `Etiquetas_a_partir_do_recebimento_imprimem_na_escolhida`.
- **Passos:** filtros, grid com quantidade, opção sem produto; escolha da impressora de destino (lista das impressoras instaladas no servidor, a última usada já marcada); saída **PPLB em RAW** para a **Argox**, etiqueta **80 × 50 mm**, **Code 128** (dúvida 3 respondida em 23/10/2026; sem PDF); impressão de teste para confirmar o modelo, o modo PPLB/PPLA e o gap; chamada direta após gravar a amostra.
- **Aceite:** [aceite da T25](../telas/T25-etiquetas-amostras.md#critérios-de-aceite) com leitura real pelo leitor do laboratório.
- **Esforço:** M · **Depende de:** 4.3

### 4.6 Testes de aceite + revisão
> ✅ **Concluída em 23/10/2026**, exceto o teste manual guiado com o laboratório (pendência 22).
> - **Aceite pelo serviço:** `AceiteFase4Tests` cobre o que faltava dos cenários de P04/P09/P10/P11 (P09 extras, 29/02 bissexto, revisão copiando as datas parciais); o resto já estava em `AmostrasTests`, `PropostasTests`, `EtiquetasTests`, navegador e roteiro SQL.
> - **`/code-review high`** (`d57e9a4^..HEAD`): 10 achados, todos corrigidos (tabela abaixo).
- Testes automatizados (xUnit) dos cenários do P04/P09/P10/P11 (via serviço); teste manual guiado com um usuário do laboratório recebendo 5 amostras reais.
- `/code-review high` sobre a fase.

| # | Achado do `/code-review high` | Situação |
|---|---|---|
| 1 | Recusa (45000) do `SP_AMOSTRA_GERAR_TESTES`/`SP_RECALCULAR_PROPOSTA` dentro da transação virava erro 500 (`InvalidOperationException`) | ✅ `ErroDeNegocioException`, reconhecida pelo `ErrosMySql` → mensagem na tela |
| 2 | Etiquetas: até 500 linhas × 4 campos passava do limite de 1.024 valores do formulário → 500 ao imprimir | ✅ grade limitada a 200 linhas (`EtiquetaService.MaxLinhas`), com aviso para refinar o filtro |
| 3 | Histórico: o status das linhas do log do legado (ID do Firebird) era procurado nos status do v2 | ✅ status só nas linhas do v2 |
| 4 | M-15 descartava todo movimento de revisão, inclusive saída manual real | ✅ descarta só a entrada automática da revisão; manual vai para a raiz (no legado real todos eram automáticos) |
| 5 | Confirmação de data retroativa ficava "true" depois de um erro e valia para outra data | ✅ campo sempre `false` ao abrir a tela |
| 6 | M-15 (localização) e M-13 (propostas com amostra) quebravam com registro órfão | ✅ órfão vira anomalia |
| 7 | Data do banco misturada com a hora do servidor da aplicação (amostra) e `DateTime.Today` na proposta | ✅ `Infra/RelogioBanco` (`NOW()`/data do MySQL) em amostra, proposta e etiquetas |
| 8 | M-13 percorria todos os itens para cada pedido (pedidos × itens) | ✅ `ToLookup` |
| 9 | Três consultas às análises do item ao gravar a amostra; regra RELATÓRIO/BOLETIM em dois lugares | ✅ uma consulta + `DocumentoDoItem` único |
| 10 | CLAUDE.md › Comandos ainda citava `ADMIN` com troca obrigatória | ✅ `ADMINISTRADOR` / `Admin123` |

## Definição de pronto
- [x] Recebimento idêntico ao Delphi nas regras mantidas e com todas as correções (número/ano, revisão, estoque).
- [x] Números das amostras migradas idênticos ao legado — 5.767 conferidos (4.1).
- [ ] Etiquetas lidas pelo leitor — pendência 21 (impressão de teste na Argox real).
