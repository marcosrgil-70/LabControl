# T23 / T24 / T25 — Movimentação, Localização e Etiquetas de Amostras

**Fase:** 7 (Etiquetas: fase 4, junto do recebimento)

---

## T23 — Movimentação de Amostras
**Rota:** `/Movimentacao` · **Permissão:** `MOVIMENTACAO.*` · **Legado:** `un_MovimentacaoAmostras.pas`
**Tabelas:** `LAB_MOV_AMOSTRAS`, `LAB_MOV_AMOSTRAS_PARAM`, `LAB_HIST_AMOSTRAS_SALDO`; triggers `TG_MOV_*`.

### Busca de amostras (painel do Delphi)
Cliente; dados da proposta (nº / ano / revisão); dados da amostra (tipo / nº / tipo de análise / ano) → "Filtrar" / "Limpar". Grid "Amostras disponíveis": nº proposta, nº amostra, produto, lote, fabricação, validade, tipo de amostra, tipo de análise, status.

### Movimentação da amostra selecionada
- Mostra lote, fabricação, validade e **qtde atual (saldo)**.
- Nova movimentação: data*, **tipo Entrada/Saída***, quantidade* (> 0), observação/justificativa.
- **Parâmetros atendidos** pela saída (checklist dos testes da amostra) com indicação "existe na movimentação atual" e "existe em movimentações anteriores" (cores do Delphi).
- Histórico: data, quantidade, E/S, proposta, amostra, lote, datas; excluir (não a entrada automática).

### Regras
- Movimentos sempre na amostra **raiz** (revisão 0) — o banco redireciona.
- Saída maior que o saldo → recusada.
- A entrada automática do recebimento não pode ser excluída; sua quantidade muda pelo recebimento enquanto não houver movimento manual.

---

## T24 — Localização das Amostras (arquivo e descarte)
**Rota:** `/Localizacao` · **Permissão:** `LOCALIZACAO.*` · **Legado:** `un_LocalAmostra.pas` + `unDM_LocalAmostra.pas` · **Tabela:** `LAB_LOCAL_AMOSTRAS` (1 por amostra raiz)

| Grupo | Campos |
|---|---|
| Amostra | código da amostra, cliente, produto (só leitura) |
| Arquivo | status (Arquivada/Descartada), data/hora do arquivo*, armário, prateleira, caixa, observação |
| Descarte | data/hora do descarte, profissional do descarte (= usuário logado) |

Regras: descartar exige data/hora (o banco garante); ao descartar, sugerir mudar o status da amostra para "Reprovada – descartada" quando aplicável; "Arquivar em lote" (várias amostras na mesma caixa) — novo.
Consulta: cliente, produto, amostra, status; colunas cliente, CPF/CNPJ, produto, amostra, status, datas, armário/prateleira/caixa.

---

## T25 — Etiquetas de Amostras
**Rota:** `/Etiquetas` · **Permissão:** `ETIQUETAS.IMPRIMIR` · **Legado:** `un_EtqAmostra.pas` + `un_EtqAmostraQtde.pas`
- Filtros: cliente (nome / CPF-CNPJ), amostras a partir de uma data.
- Grid: amostra, produto, **quantidade de etiquetas** (padrão = qtde de embalagens; editável por linha — `un_EtqAmostraQtde`).
- Opção "Não imprimir o nome do produto" (sigilo).
- Saída: PDF no tamanho da etiqueta (configurável na empresa: largura × altura, colunas) com nº da amostra em texto grande + **código de barras Code128** do nº + data de entrega (+ produto).
- Chamada direta após gravar o recebimento (T22).

**Status:** ⬜ (v1 tem Movimentação/Localização parciais — refazer sobre v2)
