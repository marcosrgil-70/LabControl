# RN-06 — Estoque, Arquivo e Descarte

Legenda: Camada **B** banco · **S** serviço · **T** tela | Origem **L** legado · **C** correção · **N** nova · **D** decisão. [Catálogo](README.md)

| ID | Regra | Camada | Origem | Crit. | Onde |
|---|---|---|---|---|---|
| RN-06.01 | Movimento sempre na amostra raiz (o banco redireciona) | B | C | 🔴 | [`TG_MOV_BI`](../banco/objetos/triggers/TG_MOV_BI.md) |
| RN-06.02 | Saída maior que o saldo é recusada | B T | C | 🔴 | [P12](../processos/P12-estoque-amostras.md) |
| RN-06.03 | Quantidade > 0 | B | L | ⚪ | [`LAB_MOV_AMOSTRAS`](../banco/objetos/tabelas/LAB_MOV_AMOSTRAS.md) |
| RN-06.04 | Entrada automática não pode ser excluída; sua quantidade acompanha o recebimento enquanto não houver movimento manual | B | L | 🟠 | [`TG_MOV_BD`](../banco/objetos/triggers/TG_MOV_BD.md), [`TG_AMOSTRAS_AU`](../banco/objetos/triggers/TG_AMOSTRAS_AU.md) |
| RN-06.05 | Com movimento manual, a quantidade entregue no recebimento não pode mudar (as demais alterações são permitidas) | B | C | 🟠 | [`TG_AMOSTRAS_BU`](../banco/objetos/triggers/TG_AMOSTRAS_BU.md) |
| RN-06.06 | Saldo recalculado pelo banco a cada movimento | B | L | 🟠 | [`PR_ATUALIZA_SALDO`](../banco/objetos/rotinas/PR_ATUALIZA_SALDO.md) |
| RN-06.07 | Busca de amostras para movimentar exige cliente (ou número) | T | L | ⚪ | [T23](../telas/T23-movimentacao-amostras.md) |
| RN-06.08 | Saída pode indicar os parâmetros atendidos (um teste uma vez por movimento) | B T | L | ⚪ | [`LAB_MOV_AMOSTRAS_PARAM`](../banco/objetos/tabelas/LAB_MOV_AMOSTRAS_PARAM.md) |
| RN-06.09 | Profissional do movimento = profissional do usuário | S | L | 🟠 | [T23](../telas/T23-movimentacao-amostras.md) |
| RN-06.10 | Justificativa obrigatória na saída | S T | N | ⚪ | [T23](../telas/T23-movimentacao-amostras.md) |
| RN-06.11 | Uma localização por amostra física (raiz) | B | C | 🟠 | [`LAB_LOCAL_AMOSTRAS`](../banco/objetos/tabelas/LAB_LOCAL_AMOSTRAS.md) |
| RN-06.12 | Descartada exige data/hora do descarte; profissional do descarte = do usuário | B S | L | 🟠 | [P13](../processos/P13-arquivo-descarte.md) |
| RN-06.13 | Desfazer descarte volta para Arquivada e limpa data/profissional | S | C | ⚪ | [P13](../processos/P13-arquivo-descarte.md) |
| RN-06.14 | Arquivar em lote (várias amostras na mesma caixa) | S T | N | ⚪ | [T24](../telas/T24-localizacao-amostras.md) |
