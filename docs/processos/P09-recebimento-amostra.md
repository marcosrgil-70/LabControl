# P09 — Recebimento da Amostra (fluxo completo)

**Criticidade:** 🔴🔴 (tela mais crítica do sistema) · **Fase:** 4
**Telas:** [T22 Recebimento](../telas/T22-recebimento-amostras.md), [T25 Etiquetas](../telas/T25-etiquetas-amostras.md), [T29 Pesquisa](../telas/T29-pesquisa-amostras.md)
**Objetos:** [`LAB_HIST_AMOSTRAS`](../banco/objetos/tabelas/LAB_HIST_AMOSTRAS.md), [`LAB_HIST_AMOSTRAS_TESTES`](../banco/objetos/tabelas/LAB_HIST_AMOSTRAS_TESTES.md), [`LAB_MOV_AMOSTRAS`](../banco/objetos/tabelas/LAB_MOV_AMOSTRAS.md), [`LAB_HIST_AMOSTRAS_SALDO`](../banco/objetos/tabelas/LAB_HIST_AMOSTRAS_SALDO.md), [`LAB_HIST_AMOSTRAS_LOG`](../banco/objetos/tabelas/LAB_HIST_AMOSTRAS_LOG.md), [`TG_AMOSTRAS_BI`](../banco/objetos/triggers/TG_AMOSTRAS_BI.md), [`TG_AMOSTRAS_AI`](../banco/objetos/triggers/TG_AMOSTRAS_AI.md), [`TG_TESTES_BI`](../banco/objetos/triggers/TG_TESTES_BI.md), [`SP_AMOSTRA_GERAR_TESTES`](../banco/objetos/rotinas/SP_AMOSTRA_GERAR_TESTES.md), [`VW_LAB_AMOSTRAS`](../banco/objetos/views/VW_LAB_AMOSTRAS.md)
**Regras:** [RN-05](../regras/RN-05-amostras.md) · Relacionados: [P04 Número/ano](P04-numero-amostra-ano.md), [P10 Datas parciais](P10-datas-parciais.md), [P12 Estoque](P12-estoque-amostras.md)

## Pré-condições
- Usuário com `AMOSTRAS.INCLUIR` e **profissional vinculado** na empresa (digitador).
- Cliente com proposta na **última revisão**, status que permite amostra, com produtos e análises.

## Fluxo de inclusão
| # | Quem | O que acontece |
|---|---|---|
| 1 | Tela | Incluir → foco em Cliente; número "INCLUSÃO"; data/hora de entrega = agora (servidor); status = status inicial; local de recebimento = último usado |
| 2 | Tela | Cliente → mostra CPF/CNPJ, endereço e telefone; carrega **propostas do cliente** (última revisão, status `PERMITE_AMOSTRA`) |
| 3 | Tela | Proposta → carrega **itens** (nº item, código, descrição) e o **pedido de venda** (só leitura) |
| 4 | Tela | Item → define o produto e restringe **tipos de análise** aos tipos das análises do item; tipo de documento do item (BOLETIM/RELATORIO) |
| 5 | Tela | Tipo de amostra/análise → prévia `TT-?????-AA/YY` |
| 6 | Tela | Demais campos: embalagem, qtde de embalagens (> 0), lote, fabricação/validade parciais, nota do rótulo, aspecto, cor, temperatura, flags "acompanha…", responsável, coletor, data/hora da coleta (≤ entrega), observação (rich text) |
| 7 | Tela | Gravar: valida na ordem do Delphi — cliente ("Cliente é obrigatório"), proposta, data de entrega, hora, embalagem, tipo de amostra, produto, tipo de análise ("Tipo de Análise obrigatório."), responsável, status; data futura; data retroativa ([P04](P04-numero-amostra-ano.md)) |
| 8 | Serviço | `BeginTransaction`; se autorizado retroativo: `SET @LC_PERMITE_RETROATIVO = 1` |
| 9 | Banco | `TG_AMOSTRAS_BI`: zeros → NULL; proposta da empresa/cliente/última revisão/status; produto do item; ano do servidor + `SP_PROXIMO_NUMERO`; pedido de venda; datas parciais; `NR_AMOSTRA` |
| 10 | Banco | `TG_AMOSTRAS_AI`: entrada automática em `LAB_MOV_AMOSTRAS` (qtde entregue) → `PR_ATUALIZA_SALDO`; log I |
| 11 | Serviço | `CALL SP_AMOSTRA_GERAR_TESTES(id)` → testes = análises do item, com `DT_PREVISTA` = entrega + prazo |
| 12 | Serviço | pedido ABERTO → EM_EXECUCAO; aviso se recebida > contratada; `COMMIT`; `SET @LC_PERMITE_RETROATIVO = 0` |
| 13 | Tela | relê; "Código da Amostra: 04-01383-01/26"; oferece **imprimir etiquetas** (T25) e **comprovante** (PDF) |

## Fluxo de alteração
- Bloqueada em revisão antiga e com boletim emitido (banco) → oferecer **Gerar revisão** ([P11](P11-revisao-amostra.md)).
- **Sem resultados**: pode trocar proposta/item/tipos; testes regerados (`SP_AMOSTRA_GERAR_TESTES`).
- **Com resultados**: tipo/proposta/produto bloqueados; testes não são regerados — mensagem do legado: *"Por existir Resultados cadastrados para este Histórico de Amostra, os parâmetros NÃO serão alterados conforme a Proposta. Para alterá-los conforme a Proposta, é necessário excluir os Resultados da Amostra."*
- Quantidade entregue: muda a entrada automática (`TG_AMOSTRAS_AU`); bloqueada se houver movimentação manual.

## Exclusão
Só revisão 0, sem resultados e sem movimentação manual (confirmação "Deseja excluir este registro?"); cascade apaga testes, movimentos automáticos, saldo e localização; log E.

## Comprovante de recebimento (página de impressão — decisão do usuário na 4.3; layout do `Layout_ImpHistAmostra.fr3`)
Empresa (logo) · nº da amostra · cliente com **endereço do laudo/correspondência** (sem endereço → *"Cliente sem endereço de correspondência."*) · proposta e pedido · produto, lote, fabricação, validade, embalagem e quantidade · datas de entrega e coleta · responsável · testes solicitados (parâmetro, método, prazo, data prevista) · observação.
> Na 4.3 o comprovante é uma página HTML de impressão (`/Amostras/Comprovante/{id}`, botão Imprimir; o navegador imprime ou salva em PDF). A biblioteca de PDF fica para o boletim (5.3).

## Cenários de teste
Ver aceite da [T22](../telas/T22-recebimento-amostras.md#aceite) + [P04](P04-numero-amostra-ano.md#cenários-de-teste-aceite). Extras: proposta de outro cliente → "A proposta selecionada não é do cliente da amostra."; item de outra proposta → "O produto selecionado não pertence à proposta."; revisão antiga da proposta → "Selecione a última revisão da proposta.".
