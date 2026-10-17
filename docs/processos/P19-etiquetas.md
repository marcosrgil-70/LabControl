# P19 — Etiquetas de Amostras

**Criticidade:** 🟠 · **Fase:** 4
**Telas:** [T25 Etiquetas](../telas/T25-etiquetas-amostras.md), [T22 Recebimento](../telas/T22-recebimento-amostras.md)
**Objetos:** [`VW_LAB_AMOSTRAS`](../banco/objetos/views/VW_LAB_AMOSTRAS.md), [`FN_DATA_PARCIAL`](../banco/objetos/rotinas/FN_DATA_PARCIAL.md), [`AMOSTRAS_STATUS`](../banco/objetos/tabelas/AMOSTRAS_STATUS.md)
**Regras:** [RN-05](../regras/RN-05-amostras.md)

## Fluxo
1. Origem: menu Etiquetas ou "Imprimir etiquetas" logo após gravar o recebimento (amostra já selecionada, quantidade = embalagens entregues).
2. Filtro: cliente **ou** nº da amostra + data de entrega ≥ (padrão hoje − 7); só última revisão e status com `PERMITE_RESULTADO`; empresa ativa.
3. Seleciona as amostras e ajusta a quantidade de cada uma; opção "não imprimir o nome do produto".
4. Gera a saída:
   - **PDF** do tamanho da etiqueta (configuração da empresa: largura, altura, margens, colunas por linha), ou
   - **EPL2/ZPL** compatível com o legado (`q450`, `Q240,25`; Code 39; `P<n>` cópias).
5. Conteúdo: código de barras do nº da amostra; nº em texto grande; produto em 2 linhas de até 32 caracteres (ou vazio); `PV: <pedido>`; `Recebimento: dd/mm/aaaa`; `Fabricação: <parcial>`; `Validade: <parcial ou --->`.

## Pendências com o usuário (dúvida 3)
Tamanho da etiqueta, modelo da impressora, Code 39 × Code 128, PDF × impressão direta.

## Cenários de teste
- Etiqueta lida pelo leitor do laboratório devolve exatamente o nº da amostra.
- Quantidade 3 → 3 etiquetas da mesma amostra.
- "Não imprimir produto" → linhas do produto em branco.
