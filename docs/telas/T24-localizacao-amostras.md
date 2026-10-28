# T24 — Localização das Amostras (arquivo e descarte)

**Fase:** 7 · **Rota:** `/Localizacao` · **Permissões:** `LOCALIZACAO.CONSULTAR/INCLUIR/ALTERAR/EXCLUIR` · **Criticidade:** ⚪
**Legado:** `LabControl/un_LocalAmostra.pas` + `unDM_LocalAmostra.pas`, menu *Movimentações → Localização das Amostras*. Tabela `LAB_LOCAL_AMOSTRAS` (2.686; `STATUS` 0 = Arquivada, 1 = "DESCATADA" [sic]; coluna `DT_HR_DESCATE`).
**Objetos do banco:** [`LAB_LOCAL_AMOSTRAS`](../banco/objetos/tabelas/LAB_LOCAL_AMOSTRAS.md), [`VW_LAB_AMOSTRAS`](../banco/objetos/views/VW_LAB_AMOSTRAS.md), [`LAB_HIST_AMOSTRAS`](../banco/objetos/tabelas/LAB_HIST_AMOSTRAS.md)
**Processos:** [P13 Arquivo e descarte](../processos/P13-arquivo-descarte.md) · **Regras:** [RN-06](../regras/RN-06-estoque-arquivo.md)

## Como era no Delphi
- **Incluir**: digitar o código da amostra (ou "Pesquisar" → `un_Lab_Hist_Amostras_Pesq`, [T29](T29-pesquisa-amostras.md)); o código era quebrado por posição (`TT`, `CCCCC`, `AA`, `YY`) para localizar a amostra (*"Amostra não encontrada."*); mostrava CPF/CNPJ, cliente, código e descrição do produto.
- Novo registro: status Arquivada, data/hora do arquivo = agora.
- Grupo **Arquivo**: status, data/hora, armário, prateleira, caixa, observação.
- Grupo **Descarte**: escolher o status "Descartada" preenche a data/hora do descarte com agora e o **funcionário do descarte** com o funcionário ativo (tecla Delete limpa a data).
- Com descarte gravado, "Alterar" só libera o grupo Descarte e o botão **"Apagar indicação de descarte"** (*"Deseja apagar a indicação de Descarte?"*) — que limpava data e funcionário, **mas não voltava o status para Arquivada**.
- **Consultar**: exigia cliente **e** produto (*"Preencher os campos Cliente e Produto para Pesquisar."*).

## Tela web
### Consulta
Filtros: cliente, produto, nº da amostra, status (Arquivada/Descartada/sem localização), período do arquivo, armário/prateleira/caixa. Basta **um** filtro (o legado exigia cliente + produto). Colunas: cliente, CPF/CNPJ, produto, amostra, status, data do arquivo, armário/prateleira/caixa, data do descarte, profissional do descarte.

### Formulário
| Grupo | Campo | Coluna | Regra |
|---|---|---|---|
| Amostra | Nº da amostra* | `ID_LAB_HIST_AMOSTRAS` | busca por número (texto completo) ou pesquisa; grava **sempre a raiz**; só leitura depois de gravar |
| | Cliente / produto | — | só leitura |
| Arquivo | Status* | `STATUS` | ARQUIVADA / DESCARTADA |
| | Data/hora do arquivo* | `DT_HR_ARQUIVO` | padrão agora |
| | Armário / Prateleira / Caixa | `NR_ARMARIO`, `NR_PRATELEIRA`, `NR_CAIXA` | até 15 cada |
| | Observação | `OBSERVACAO` | até 250 |
| Descarte | Data/hora do descarte | `DT_HR_DESCARTE` | obrigatória se DESCARTADA (o banco garante — `CK_LOCAL_DESC`) |
| | Profissional do descarte | `ID_ENTIDADES_FUNC_DESCARTE` | profissional do usuário logado (só leitura) |

### Ações
| Ação | Regra |
|---|---|
| Incluir | uma localização por amostra raiz (`UQ_LOCAL_AMO`): se já existir, abrir para alterar |
| Descartar | status DESCARTADA + data/hora + profissional; sugerir mudar o status da amostra para "Reprovada – descartada" quando o descarte for por reprovação (pergunta) |
| Desfazer descarte | `STATUS = ARQUIVADA`, limpa data e profissional, grava `AUDITORIA` (corrige o legado) |
| **Arquivar em lote** (novo) | selecionar várias amostras e informar armário/prateleira/caixa uma vez |
| Excluir | remove a localização (confirmação) |

## Critérios de aceite
- [ ] Descartar sem data é recusado.
- [ ] Localização de uma revisão grava na raiz (uma só por amostra física).
- [ ] Desfazer descarte volta para ARQUIVADA.
- [ ] Arquivar em lote.

## Observações para a implementação (levantadas em 28/10/2026, depois da 7.1)
- **Sem trigger na `LAB_LOCAL_AMOSTRAS`:** ao contrário do movimento (`TG_MOV_BI`), o banco **não** redireciona para a raiz nem herda a empresa. A gravação na raiz fica no serviço (`IdAmostraRaiz ?? Id`), ou numa trigger nova com aprovação do usuário (mudança no banco).
- **Permissões:** `LOCALIZACAO.CONSULTAR/INCLUIR/ALTERAR/EXCLUIR` existem (seed), mas o perfil sugerido RECEPÇÃO não tem nenhuma delas — pendência 32.
- **Status "REPROVADA - DESCARTADA"** existe em toda empresa (`SP_INICIALIZAR_EMPRESA`): usar na pergunta opcional do descarte.
- **Profissional do descarte** = o do usuário; sem vínculo, recusar com a mesma mensagem da T23.
- **Modelo de código:** `MovimentacaoController`/`MovimentacaoService` (busca com filtro obrigatório, tela da amostra, modal, `ErrosMySql.CapturarAsync` + transação).
- Passo a passo em [F7 › 7.2](../plano/F7-estoque-arquivo.md#72-t24--localização-próxima).

**Status:** ⬜ — tarefa 7.2 (próxima) (a v1 parcial foi descartada; as 2.494 localizações do legado já estão migradas, M-15). Usar a pesquisa de amostras (T29) com `ApenasSemLocalizacao`.
