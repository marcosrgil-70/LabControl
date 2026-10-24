# P06 — Cálculo dos Valores da Proposta

**Criticidade:** 🔴 (valor faturado) · **Fase:** 4/6
**Telas:** [T20 Propostas](../telas/T20-propostas.md), [T21 Pedidos](../telas/T21-pedidos-venda.md), [T30-01 Faturamento](../telas/T30-01-rel-faturamento.md)
**Objetos:** [`LAB_PROPOSTAS_PRODANALISES`](../banco/objetos/tabelas/LAB_PROPOSTAS_PRODANALISES.md), [`LAB_PROPOSTAS_PRODUTOS`](../banco/objetos/tabelas/LAB_PROPOSTAS_PRODUTOS.md), [`LAB_PROPOSTAS`](../banco/objetos/tabelas/LAB_PROPOSTAS.md), [`TG_PRODAN_BI`](../banco/objetos/triggers/TG_PRODAN_BI.md), [`TG_PRODAN_BU`](../banco/objetos/triggers/TG_PRODAN_BU.md), [`TG_PRODAN_AI`](../banco/objetos/triggers/TG_PRODAN_AI.md), [`TG_PROPPROD_AU`](../banco/objetos/triggers/TG_PROPPROD_AU.md), [`SP_RECALCULAR_PROPOSTA`](../banco/objetos/rotinas/SP_RECALCULAR_PROPOSTA.md)
**Regras:** [RN-04](../regras/RN-04-propostas-pedidos.md)

## Fórmulas (iguais ao Delphi `cds_ProdAnalises*Change` + `TotalizarAnalises`)
**Linha de análise** (`LAB_PROPOSTAS_PRODANALISES`):
```
QTDE_AMOSTRAS = QTDE_AMOSTRAS do produto (propagada pela TG_PROPPROD_AU)
VR_SUBTOTAL   = ROUND(QTDE_AMOSTRAS × VR_UNITARIO, 2)
VR_DESCONTO   = ROUND(VR_SUBTOTAL × PORC_DESCONTO / 100, 2)   -- se PORC_DESCONTO > 0; senão o valor digitado
VR_TOTAL      = VR_SUBTOTAL − VR_DESCONTO                       -- desconto > subtotal é recusado
```
**Cabeçalho** (`LAB_PROPOSTAS`, por `SP_RECALCULAR_PROPOSTA`):
```
VR_SOMA_VR_ANALIS     = Σ VR_SUBTOTAL
VR_SOMA_DESC_ANALIS   = Σ VR_DESCONTO
VR_SOMA_VR_TOT_ANALIS = Σ VR_TOTAL
VR_DESCONTO           = ROUND(VR_SOMA_VR_TOT_ANALIS × PORC_DESCONTO / 100, 2)  -- se PORC_DESCONTO > 0; senão o digitado
VR_TOTAL_PROPOSTA     = VR_SOMA_VR_TOT_ANALIS − VR_DESCONTO
```

## Quem calcula
- **Banco** (fonte da verdade): triggers da linha + `SP_RECALCULAR_PROPOSTA` a cada inclusão/alteração/exclusão de análise e na revisão.
- **Tela**: recalcula em JavaScript **só para exibir** enquanto o usuário digita; ao gravar, relê os valores do banco.
- Mudança só de `PORC_DESCONTO`/`VR_DESCONTO` do cabeçalho (sem tocar nas análises): o serviço grava o cabeçalho e chama `CALL SP_RECALCULAR_PROPOSTA(id)` na mesma transação.

## Valor unitário sugerido
Ao escolher o parâmetro, se a linha estiver com `VR_UNITARIO = 0`, sugerir `LAB_PARAMETROS_ANALISES.VR_UNITARIO` (regra do Delphi).

## Seleção múltipla de parâmetros
Linhas criadas de uma vez copiam método, idioma, prazo e tipo de documento da linha base; cada uma recebe o próprio valor unitário; o recálculo do cabeçalho roda a cada linha (ou uma vez no fim, se gravadas em lote numa transação).

## Cenário de teste (roteiro doc 12 §7 item 2)
2 análises: 100,00 × 2 e 50,00 × 2, desconto geral 10% → análises 300,00 · desconto 30,00 · **total 270,00**.
Outros: desconto da linha maior que o subtotal → "Desconto maior que o subtotal da análise."; mudar a quantidade do produto de 2 para 3 → todas as análises do item com qtde 3 e totais recalculados.

## Implementação (tarefa 4.2, 23/10/2026)
- **Quem calcula:** o banco (`TG_PRODAN_BI/BU`, `TG_PROPPROD_AU`, `SP_RECALCULAR_PROPOSTA` chamado também no fim da gravação, para o desconto geral). A tela (`propostas.js`) recalcula só para exibir.
- **Na tela:** o parâmetro sugere o valor unitário quando a linha está com 0; a análise nova copia método/idioma/prazo/documento da anterior do produto.
- **Cenário 300,00 − 10% = 270,00 e qtde 2 → 3 (405,00):** `PropostasTests` e navegador. Valores digitados aceitam "1.234,56", "1.000" (milhar) ou "12.5" (`Textos.Decimal`).
