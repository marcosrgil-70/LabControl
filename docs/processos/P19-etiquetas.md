# P19 — Etiquetas de Amostras

**Criticidade:** 🟠 · **Fase:** 4
**Telas:** [T25 Etiquetas](../telas/T25-etiquetas-amostras.md), [T22 Recebimento](../telas/T22-recebimento-amostras.md)
**Objetos:** [`VW_LAB_AMOSTRAS`](../banco/objetos/views/VW_LAB_AMOSTRAS.md), [`FN_DATA_PARCIAL`](../banco/objetos/rotinas/FN_DATA_PARCIAL.md), [`AMOSTRAS_STATUS`](../banco/objetos/tabelas/AMOSTRAS_STATUS.md)
**Regras:** [RN-05](../regras/RN-05-amostras.md)

## Fluxo
1. Origem: menu Etiquetas ou "Imprimir etiquetas" logo após gravar o recebimento (amostra já selecionada, quantidade = embalagens entregues).
2. Filtro: cliente **ou** nº da amostra + data de entrega ≥ (padrão hoje − 7); só última revisão e status com `PERMITE_RESULTADO`; empresa ativa.
3. Seleciona as amostras e ajusta a quantidade de cada uma; opção "não imprimir o nome do produto".
4. Escolhe a **impressora de destino** numa lista com as impressoras instaladas no servidor. A última usada pelo usuário já vem marcada.
5. O servidor gera os comandos **PPLB** (Argox, compatível com o EPL2 do legado) e envia em **RAW** à impressora escolhida. Não há PDF. Etiqueta **80 × 50 mm** (640 × 400 pontos a 203 dpi), **Code 128**, `P<n>` cópias.
6. Conteúdo: código de barras do nº da amostra; nº em texto grande; produto em 2 linhas de até 32 caracteres (ou vazio); `PV: <pedido>`; `Recebimento: dd/mm/aaaa`; `Fabricação: <parcial>`; `Validade: <parcial ou --->`.

## Decisões do usuário (dúvida 3, 23/10/2026)
- Etiqueta de 5 cm de altura × 8 cm de largura.
- Impressora Argox.
- Code 128.
- Impressão direta, pedindo para escolher a impressora de destino.

Implementado na 4.5 (23/10/2026): `EtiquetaService.Pplb` + `IImpressoraRaw` (RAW no spooler do servidor). **Ainda falta** a impressão de teste na Argox real para confirmar o modelo, o modo PPLB e o gap, e a leitura do Code 128 pelo leitor — roteiro em [T25 › Impressão de teste](../telas/T25-etiquetas-amostras.md#impressão-de-teste-a-fazer-com-a-argox).

## Cenários de teste
- Etiqueta lida pelo leitor do laboratório devolve exatamente o nº da amostra.
- Quantidade 3 → 3 etiquetas da mesma amostra.
- "Não imprimir produto" → linhas do produto em branco.
