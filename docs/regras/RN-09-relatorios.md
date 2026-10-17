# RN-09 — Relatórios Gerenciais

Legenda: Camada **B** banco · **S** serviço · **T** tela | Origem **L** legado · **C** correção · **N** nova · **D** decisão. [Catálogo](README.md)

| ID | Regra | Camada | Origem | Crit. | Onde |
|---|---|---|---|---|---|
| RN-09.01 | Período obrigatório, fim ≥ início ("Período de datas inválido. Por favor, verificar.") | S T | L | ⚪ | [T30-00](../telas/T30-00-relatorios-padrao.md) |
| RN-09.02 | Sempre a empresa ativa; consultas parametrizadas | S | L/C | 🔴 | [P20](../processos/P20-relatorios-gerenciais.md) |
| RN-09.03 | Amostra recebida = amostra física (revisão 0) com entrega no período | S | C | 🟠 | [P20](../processos/P20-relatorios-gerenciais.md) |
| RN-09.04 | Amostra analisada = amostra física com resultado (última revisão) no período, contada uma vez | S | C | 🟠 | [P20](../processos/P20-relatorios-gerenciais.md) |
| RN-09.05 | Parâmetro analisado = linha da última revisão do resultado (corrige o JOIN errado do legado) | S | C | 🔴 | [T30-04](../telas/T30-04-rel-parametros-analisados.md) |
| RN-09.06 | Tempo de análise = dias entre início e resultado, inclusivo (+1) | S | L | ⚪ | [T30-05](../telas/T30-05-rel-tempo-analises.md) |
| RN-09.07 | Faturamento = 1 linha por pedido não cancelado autorizado no período (sem repetir revisões) | S | C | 🔴 | [T30-01](../telas/T30-01-rel-faturamento.md) |
| RN-09.08 | Relatórios de amostras por padrão consideram só amostras com pedido de venda (como o legado), com opção de incluir todas | S T | L | ⚪ | [T30-02](../telas/T30-02-rel-amostras-periodo.md) |
| RN-09.09 | Saída em tela, PDF e CSV a partir do mesmo conjunto de dados | S | L | ⚪ | [T30-00](../telas/T30-00-relatorios-padrao.md) |
| RN-09.10 | Diferenças em relação ao legado documentadas no MD do relatório após a homologação | — | N | 🟠 | [P20](../processos/P20-relatorios-gerenciais.md) |
