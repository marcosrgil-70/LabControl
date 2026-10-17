# RN-05 — Amostras (recebimento, número, testes, revisão, etiquetas)

Legenda: Camada **B** banco · **S** serviço · **T** tela | Origem **L** legado · **C** correção · **N** nova · **D** decisão. [Catálogo](README.md)

| ID | Regra | Camada | Origem | Crit. | Onde |
|---|---|---|---|---|---|
| RN-05.01 | Número `TT-CCCCC-AA/YY [Rnn]`: códigos do tipo de amostra e de análise, sequência anual, ano, revisão | B | L | 🔴 | [P04](../processos/P04-numero-amostra-ano.md) |
| RN-05.02 | **Ano da amostra = ano do servidor** na inclusão; nunca o da data digitada | B | C/D | 🔴 | [P04](../processos/P04-numero-amostra-ano.md) |
| RN-05.03 | Sequência por (empresa, ano) com bloqueio; nunca reinicia por causa de data digitada | B | C | 🔴 | [P04](../processos/P04-numero-amostra-ano.md) |
| RN-05.04 | Data de entrega futura é recusada | B T | L | 🔴 | [`TG_AMOSTRAS_BI`](../banco/objetos/triggers/TG_AMOSTRAS_BI.md) |
| RN-05.05 | Data de entrega mais antiga que `DIAS_RETROATIVOS_ENTREGA` exige confirmação explícita + permissão; mesmo liberada, não muda o ano | B S T | N/D | 🔴 | [P04](../processos/P04-numero-amostra-ano.md) |
| RN-05.06 | Data de entrega com ano diferente do ano atual mostra aviso antes de gravar | T | N | 🟠 | [T22](../telas/T22-recebimento-amostras.md) |
| RN-05.07 | Número exibido como "INCLUSÃO" até gravar; nenhum número é reservado antes do INSERT | S T | C | 🟠 | [P09](../processos/P09-recebimento-amostra.md) |
| RN-05.08 | Ano, código, revisão e empresa da amostra são imutáveis; TT/AA só mudam sem resultados | B | C | 🔴 | [`TG_AMOSTRAS_BU`](../banco/objetos/triggers/TG_AMOSTRAS_BU.md) |
| RN-05.09 | Proposta da amostra: mesma empresa, mesmo cliente, última revisão, status que permite amostra; produto vem do item | B | C | 🔴 | [`TG_AMOSTRAS_BI`](../banco/objetos/triggers/TG_AMOSTRAS_BI.md) |
| RN-05.10 | Tipo de análise restrito aos tipos das análises do item da proposta | S T | L | 🟠 | [T22](../telas/T22-recebimento-amostras.md) |
| RN-05.11 | Obrigatórios (ordem do Delphi): cliente, proposta, data, hora, embalagem, tipo de amostra, produto, tipo de análise, responsável, status; qtde de embalagens > 0; local de recebimento | S T | L | 🟠 | [P09](../processos/P09-recebimento-amostra.md) |
| RN-05.12 | Recebimento exige profissional vinculado ao usuário (digitador) | S | L | 🔴 | [P02](../processos/P02-autenticacao-permissoes.md) |
| RN-05.13 | Fabricação/validade parciais (ano, mês/ano ou completa); dia exige mês e ano; mês exige ano; dia válido para o mês | B T | L/C | 🟠 | [P10](../processos/P10-datas-parciais.md) |
| RN-05.14 | Data/hora da coleta ≤ data/hora da entrega | S T | N | ⚪ | [T22](../telas/T22-recebimento-amostras.md) |
| RN-05.15 | Testes = análises do item da proposta, com data prevista = entrega + prazo; regerados na alteração sem resultados | B S | L | 🔴 | [`SP_AMOSTRA_GERAR_TESTES`](../banco/objetos/rotinas/SP_AMOSTRA_GERAR_TESTES.md) |
| RN-05.16 | Com resultados, testes não são regerados (mensagem do legado) | B S | L | 🟠 | [P09](../processos/P09-recebimento-amostra.md) |
| RN-05.17 | Gravação da amostra + testes numa só transação; observação gravada junto (sem gravações parciais por aba) | S | C | 🔴 | [P09](../processos/P09-recebimento-amostra.md) |
| RN-05.18 | Recebimento cria a entrada automática no estoque (só revisão 0) | B | L/C | 🟠 | [P12](../processos/P12-estoque-amostras.md) |
| RN-05.19 | Alteração bloqueada em revisão antiga e com boletim emitido; com resultados, tipo/proposta/produto bloqueados | B | C | 🔴 | [`TG_AMOSTRAS_BU`](../banco/objetos/triggers/TG_AMOSTRAS_BU.md) |
| RN-05.20 | Exclusão só da revisão 0, sem resultados e sem movimentação manual | B | L | 🟠 | [`TG_AMOSTRAS_BD`](../banco/objetos/triggers/TG_AMOSTRAS_BD.md) |
| RN-05.21 | Revisão só com resultados, só da última, com motivo; cópia completa (inclusive coletor); cascata para resultado e boletim | B | C | 🔴 | [P11](../processos/P11-revisao-amostra.md) |
| RN-05.22 | Estoque, saldo e localização ficam sempre na amostra raiz (revisão 0) | B | C | 🔴 | [P12](../processos/P12-estoque-amostras.md) |
| RN-05.23 | Telas operacionais listam só a última revisão da amostra | S | C | 🟠 | [T26](../telas/T26-resultados.md), [T27](../telas/T27-boletim.md) |
| RN-05.24 | Comprovante de recebimento exige endereço do laudo/correspondência ("Cliente sem endereço de correspondência.") | S | L | ⚪ | [P09](../processos/P09-recebimento-amostra.md) |
| RN-05.25 | Etiquetas só de amostras com status que permite resultado; quantidade padrão = embalagens; opção sem nome do produto | S T | L | 🟠 | [P19](../processos/P19-etiquetas.md) |
| RN-05.26 | Log de inclusão/alteração/exclusão da amostra gravado pelo banco | B | L | 🟠 | [`LAB_HIST_AMOSTRAS_LOG`](../banco/objetos/tabelas/LAB_HIST_AMOSTRAS_LOG.md) |
