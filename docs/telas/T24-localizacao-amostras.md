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
- [x] Descartar sem data é recusado.
- [x] Localização de uma revisão grava na raiz (uma só por amostra física).
- [x] Desfazer descarte volta para ARQUIVADA.
- [x] Arquivar em lote.

## Observações para a implementação (levantadas em 28/10/2026, depois da 7.1)
- **Sem trigger na `LAB_LOCAL_AMOSTRAS`:** ao contrário do movimento (`TG_MOV_BI`), o banco **não** redireciona para a raiz nem herda a empresa. A gravação na raiz fica no serviço (`IdAmostraRaiz ?? Id`), ou numa trigger nova com aprovação do usuário (mudança no banco).
- **Permissões:** `LOCALIZACAO.CONSULTAR/INCLUIR/ALTERAR/EXCLUIR` existem (seed); o perfil sugerido RECEPÇÃO não tinha nenhuma — **pendência 32 feita** (D-25: `CONSULTAR/INCLUIR/ALTERAR`).
- **Status "REPROVADA - DESCARTADA"** existe em toda empresa (`SP_INICIALIZAR_EMPRESA`): usar na pergunta opcional do descarte.
- **Profissional do descarte** = o do usuário; sem vínculo, recusar com a mesma mensagem da T23.
- **Modelo de código:** `MovimentacaoController`/`MovimentacaoService` (busca com filtro obrigatório, tela da amostra, modal, `ErrosMySql.CapturarAsync` + transação).
- Passo a passo em [F7 › 7.2](../plano/F7-estoque-arquivo.md#72-t24--localização-).

## Implementação (tarefa 7.2, 28/10/2026)
- **Arquivos:** `LocalizacaoController`, `LocalizacaoService`, `ViewModels/LocalizacaoVM.cs`, `Views/Localizacao/Index` e `Amostra`, `wwwroot/js/localizacao.js`.
- **Consulta** (`/Localizacao`): cliente (nome/CPF/CNPJ), produto, nº, situação (Arquivada / Descartada / Sem localização), data do arquivo de/até, armário, prateleira, caixa — **basta um** (sem nenhum, a lista não aparece); só a última revisão; 25 por página.
- **"Arquivar amostra"**: abre a pesquisa de amostras (T29, `/Localizacao/Pesquisa`) só com as **sem localização** e leva à tela da amostra.
- **Tela da amostra** (`/Localizacao/Amostra/{id}`): grupos **Arquivo** (situação, data/hora, armário, prateleira, caixa, observação) e **Descarte** (só aparece com a situação "Descartada"; a data/hora vem com agora). A localização é sempre da **amostra física**: aberta por uma revisão antiga, avisa; a raiz é calculada **no serviço** (não há trigger na `LAB_LOCAL_AMOSTRAS`).
- **Regras do serviço:** data do arquivo e do descarte nunca futuras; descarte nunca antes do arquivo; **profissional do descarte** = o do usuário (sem vínculo → recusa); já descartada e gravada de novo → mantém quem descartou; "Incluir" de uma amostra que outro usuário acabou de localizar → recusa ("abra de novo para alterar"); quem só tem `INCLUIR` não altera a existente (`Incluir` zera o ID).
- **Descarte por reprovação** (pergunta opcional): marca o status da **última revisão** da amostra como "REPROVADA - DESCARTADA", na mesma gravação. Só aparece se o status existir na empresa.
- **Desfazer descarte**: botão próprio → volta para ARQUIVADA e **limpa data e profissional**, mantendo armário/prateleira/caixa (corrige o legado). Voltar a situação para "Arquivada" no formulário faz o mesmo.
- **Arquivar em lote** (novo): na consulta, marcar as amostras **sem localização** e informar data, armário/prateleira/caixa (ao menos um) e observação; numa transação; amostras repetidas ou revisões da mesma amostra contam uma vez; as que já têm localização **não são alteradas** e aparecem na mensagem; até 200 por vez.
- **Excluir**: com confirmação; a amostra volta a ficar sem localização.
- **Auditoria:** automática pelo EF (`LAB_LOCAL_AMOSTRAS` é tabela crítica).
- **Permissões:** `LOCALIZACAO.CONSULTAR` (consulta, tela, pesquisa), `INCLUIR` (incluir, lote), `ALTERAR` (alterar, descartar, desfazer), `EXCLUIR`. O perfil sugerido RECEPÇÃO passou a ter `CONSULTAR/INCLUIR/ALTERAR` (pendência 32, D-25).
- **Testes:** `LocalizacaoTests` (4) e navegador `Localizacao_lote_descarte_desfazer_e_exclusao`.

## Revisão (tarefa 7.3, 28/10/2026)
`/code-review high` de `3e7e3ba..HEAD` — 10 achados, 9 corrigidos e 1 sem mudança (detalhes no Diário do plano):
1. **Descarte por reprovação de amostra com boletim emitido** falhava inteiro (o `TG_AMOSTRAS_BU` não deixa mudar amostra com boletim emitido) → a opção fica desabilitada na tela e o serviço recusa antes, com mensagem; o descarte sozinho passa. Permitir mudar o status nesse caso é mudança no banco → pendência 34.
2. **Saída com parâmetros atendidos travava a amostra no recebimento:** o `SP_AMOSTRA_GERAR_TESTES` apaga e regera os testes a cada gravação e a FK do `LAB_MOV_AMOSTRAS_PARAM` recusava (1451) → com saída que indicou parâmetros os testes não são regerados, e trocar proposta/produto/tipo de análise é recusado com mensagem ("exclua essas saídas antes…").
3. **Desfazer descarte** não devolve o status anterior da amostra (não é guardado) → a mensagem avisa que o status continua "REPROVADA - DESCARTADA".
4. **Localização simultânea** da mesma amostra dava "Já existe um registro… (UQ_LOCAL_AMO)" → mensagem própria no `ErrosMySql`.
5. Perfil RECEPÇÃO nas empresas já existentes — **sem mudança**: a permissão foi dada direto no `labcontrol_v2` e no `labcontrol_migracao` (7.2); bancos novos e a migração da virada passam pelo `SP_INICIALIZAR_EMPRESA` novo.
6–7. **Trava do saldo duplicada** (serviço + trigger) e SQL cru sem filtro de empresa → o serviço deixou a trava e a transação para o banco (D-25); só confere o saldo antes, pela entidade (filtro da empresa), para dar a mensagem com o saldo atual.
8. `Minuto`/`RaizAsync` copiados nos dois serviços → `NoMinuto()` e `AmostraFisicaAsync()` em `Infra/RelogioBanco.cs`.
9. Parâmetros por movimento com busca quadrática → `ToLookup`; a T24 sem filtro não consulta mais o relógio do banco.
10. "Arquivar em lote" voltava para a página 1 sem as datas do filtro → página e datas mantidas.

## Descarte por reprovação com boletim emitido (29/10/2026, pendência 34)
Decisão do usuário: o banco aceita, em amostra com boletim emitido, a troca **só do status** (D-27). A opção "mudar o status para REPROVADA - DESCARTADA" voltou a valer também nesse caso (antes ficava desabilitada); o boletim emitido não muda. Teste: `LocalizacaoTests.Descarte_por_reprovacao_de_amostra_com_boletim_emitido_muda_so_o_status`.

**Status:** ✅ — tarefa 7.2 concluída em 28/10/2026; revisada na 7.3 (as 2.494 localizações do legado já estavam migradas, M-15); descarte com boletim emitido liberado em 29/10/2026.
