# P26 — Testes de ponta a ponta no navegador (CRUD real das telas)

**Criticidade:** 🟠 · **Desde:** 20/10/2026 (pedido do usuário: "teste se está realmente fazendo um CRUD … em todos os já desenvolvidos")
**Código:** `tests/LabControl.Tests/Navegador/` (`AppNavegador.cs`, `CrudNavegadorTests.cs`, `CrudNavegadorTelasTests.cs`)
**Relacionados:** [P22 Erros e transações](P22-erros-transacoes.md) · [P25 Cadastro de entidades e contatos](P25-cadastro-entidades-contatos.md) · skill `/concluir-tarefa`

## 1. Por que
Os testes de serviço e os testes HTTP (`WebApplicationFactory`) provam regras, permissões e respostas do servidor, mas **não executam o JavaScript** das telas: modais, abas, combos de UF → cidade, busca de CEP, "principal" que troca sozinho, confirmações (`confirm()`), recarga das abas por AJAX. No primeiro teste do usuário (20/10/2026) o cadastro de clientes "sumiu" ao clicar num botão só com ícone (olho cortado = **Inativar**, sem confirmação; a lista esconde inativos). O dado estava no banco, mas a tela induziu ao erro — e nenhum teste olhava a tela como o usuário.

## 2. Como funciona
- `AppNavegador` (fixture xUnit da coleção `Navegador`): sobe o **app real** no Kestrel numa porta livre (`WebApplicationFactory.UseKestrel(0)`), apontando para o banco de testes `labcontrol_teste`, e abre o **Edge sem janela** pelo Playwright (`Channel = "msedge"` — usa o Edge instalado, não baixa navegador). A fonte de CEP é trocada por `CepDoTeste` (responde `01310-100`; o resto da cadeia — cidade pelo IBGE, tipo de logradouro — é a real).
- `EntrarAsync(cenario)`: cria um usuário com o perfil **ADMINISTRADOR** da empresa do cenário (ou ADMIN_GLOBAL), faz o login pela tela e devolve a `Sessao` (aba). Confirmações são aceitas e registradas em `Sessao.Dialogos`; erros de JavaScript e do console vão para `Sessao.ErrosJs` (respostas **422** — recusa de negócio esperada — não contam).
- Cada teste faz o CRUD **clicando**: incluir → pesquisar → alterar → inativar (confirmação) → "mostrar inativos" → reativar → excluir, conferindo a mensagem do topo, a lista e o banco; ao final, `Assert.Empty(s.ErrosJs)`.

## 3. Cobertura atual
| Tela | Teste | O que o navegador faz |
|---|---|---|
| T10 auxiliares (Idiomas) | `Auxiliar_idiomas_crud_completo` | modal incluir/alterar, busca, inativar com confirmação, mostrar inativos, reativar, excluir |
| T10-02 Tipos de endereço | `Auxiliar_com_opcoes_tipos_de_endereco` | campo de opções (Finalidade) com rótulo acentuado, alteração mantém a opção |
| T12 Clientes | `Cliente_crud_completo_com_enderecos_telefones_e_emails` | incluir PJ → aba Endereços; endereço por UF/cidade; recusa do 2º ativo do mesmo tipo (mensagem no modal); alterar endereço (cidade pré-selecionada); **busca de CEP** (preenche logradouro, tipo, bairro, cidade) e CEP inexistente; dois telefones "principal" (só o último fica); excluir telefone; e-mail incluir/alterar; dados básicos + observações; pesquisa por parte do CNPJ; inativar → aviso "inativo(s) também correspondem" → mostrar → reativar; excluir com os contatos |
| T13 Profissionais | `Profissional_crud_com_registro_e_contatos` | recusa de "assina boletim" sem registro; registro CRQ/UF; e-mail; alterar; pesquisar; excluir |
| T03 Empresa + T05 Parâmetros | `Empresa_dados_contatos_e_parametros` | alterar fantasia (topo atualiza); telefone da empresa (persistido após recarregar); dias retroativos |
| T02 Usuários | `Usuario_crud_com_senha_provisoria` | incluir com perfil → senha provisória; pesquisar; alterar; resetar senha; excluir |
| T04 Perfis | `Perfil_crud_matriz_e_copia` | matriz (CONSULTAR automático), alterar, copiar, excluir |
| T03 Empresas (ADMIN_GLOBAL) | `Empresas_admin_global_cria_e_altera` | escolha de empresa no login, nova empresa (nasce com a entidade), alterar |
| T10-14/15/16 auxiliares especiais | `Auxiliares_codigo_travado_cor_e_status_que_permite_resultado` | código travado após uso, cor do status, sempre um status que permite resultado |
| T10-11 Condições de pagamento | `Condicao_de_pagamento_com_parcelas` | parcelas com Σ = 100% |
| T11 Geografia | `Geografia_pais_estado_cidade_e_importacao_ibge` | países, estados, cidades e importação do CSV do IBGE |
| Cadastro rápido "+" | `Cadastro_rapido_mais_inclui_e_seleciona_no_combo` | "+" no combo (também dentro de outro modal) inclui e já seleciona |
| T14 Produtos | `Produto_crud_com_codigo_automatico_e_unidade_pelo_mais` | código automático, unidade pelo "+", alterar, pesquisar, inativar/reativar, excluir |
| T15 Parâmetros de análise | `Parametro_de_analise_crud_com_metodo_pelo_mais` | método pelo "+", filtros, excluir |
| T07 Migração | `Migracao_tela_valida_o_pedido_sem_executar` | validação do pedido (a execução real está em `MigracaoTests`) |
| T20 Propostas (4.2) | `Proposta_crud_com_produto_analises_e_valores` | cliente → sugestões de endereço/contato; produto e análises nos modais; 300,00 − 10% = 270,00; alterar qtde (405,00); pesquisar; excluir |
| T22 Recebimento (4.3) | `Amostra_recebimento_testes_comprovante_e_exclusao` | cascata cliente → proposta → item → tipo; número do banco; testes; histórico; data retroativa confirmada; comprovante; pesquisar; excluir |
| T29 Pesquisa de amostras (4.4) | `Pesquisa_de_amostras_seleciona_e_abre` | modal da T22, filtros, seleção abre a amostra |
| T25 Etiquetas (4.5) | `Etiquetas_a_partir_do_recebimento_imprimem_na_escolhida` | do recebimento com a amostra marcada; impressora escolhida (impressora falsa `ImpressoraDoTeste` guarda os comandos); quantidade; sigilo; última impressora lembrada |
| T30 Relatórios (8.1) | `Relatorios_visualizam_filtram_e_exportam` | período padrão, "somente com pedido" marcado e desmarcado (totais mudam), período invertido com a mensagem do legado, tabelas de resumo, PDF e CSV por requisição |

## 4. Como rodar
```powershell
dotnet test tests/LabControl.Tests --filter "FullyQualifiedName~Navegador"   # só os de navegador (~6 min)
dotnet test tests/LabControl.Tests                                            # suíte inteira (~17 min)
```
> **Pendência 18 resolvida na 3.6 (23/10/2026):** o `MigracaoTests` (migração real, ~5 min de carga) rodava em paralelo com os outros e 16 testes estouravam 30 s esperando lock ao criar a empresa de teste. Agora ele está na coleção `ColecaoMigracao` (`DisableParallelization = true`): o xUnit o roda sozinho, depois das demais. Suíte inteira de uma vez: 186/186 em 17 min.
> Com 8 GB de RAM, feche o Chrome antes: com pouca memória livre o Claude Code chegou a interromper a suíte.
Requisitos: Microsoft Edge instalado; MySQL local com o user-secret da connection string (o banco `labcontrol_teste` é recriado sozinho quando `Scripts/v2` muda). O app de desenvolvimento (`dotnet run`) **não** pode estar rodando durante o build dos testes (trava o `LabControl.exe`).

## 5. Regra para as próximas telas (a executar)
- Toda tela nova (Fases 2 a 8) ganha **um teste de navegador** com o CRUD completo (ou o fluxo principal, nas telas de processo: recebimento, resultados, emissão do boletim), além dos testes de serviço/HTTP. Item incluído na skill `/concluir-tarefa` e na "Definição de pronto" das fases.
- Botões de ação das listas: **ícone + texto** (Abrir/Alterar, Inativar/Reativar, Excluir); **Inativar pede confirmação** explicando que o registro sai da lista; a lista avisa quando a pesquisa encontrou **inativos ocultos** (link para mostrá-los). Padrão aplicado em 20/10/2026 a Auxiliares, Clientes, Profissionais, Usuários, Perfis, Empresas e abas de contatos.
- **Não baixar arquivos pelo navegador** (`RunAndWaitForDownloadAsync`): no Edge sem janela isso derrubou o navegador compartilhado e os testes de navegador seguintes caíram com `TargetClosedException: Process exited` (8.1, 29/10/2026). Confira PDF/CSV com `p.APIRequest.GetAsync(...)` (mesmos cookies da sessão) e o botão pelo `href`/`formaction`.
- Seletores dos testes por `id`/`name`/`title`/`data-*` (não por posição ou classe de layout), para o teste não quebrar com ajuste visual.
