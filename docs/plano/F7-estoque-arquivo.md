# Fase 7 — Estoque e Arquivo de Amostras

**Objetivo:** movimentação manual com saldo protegido e localização/descarte das amostras.
**Modelo/modo:** 🟢 Sonnet · ⚡ accept edits depois da primeira tela aprovada.
**Ler antes:** [P12](../processos/P12-estoque-amostras.md) · [P13](../processos/P13-arquivo-descarte.md) · [RN-06](../regras/RN-06-estoque-arquivo.md) · telas [T23](../telas/T23-movimentacao-amostras.md), [T24](../telas/T24-localizacao-amostras.md)
**Depende de:** Fase 4.

---

### 7.1 T23 — Movimentação ✅
- **Passos:** busca por cliente/número (última revisão); painel da amostra com saldo; nova movimentação (entrada complementar/saída, justificativa, parâmetros atendidos com indicação dos já atendidos); histórico; excluir só manual.
- **Aceite:** [aceite da T23](../telas/T23-movimentacao-amostras.md#critérios-de-aceite).
- **Esforço:** M
- **Feito em 28/10/2026** — ver [T23 › Implementação](../telas/T23-movimentacao-amostras.md#implementação-tarefa-71-28102026).

### 7.2 T24 — Localização
- **Passos:** consulta com um filtro só; formulário arquivo/descarte; desfazer descarte (volta para ARQUIVADA); arquivar em lote; pesquisa (T29).
- **Aceite:** [aceite da T24](../telas/T24-localizacao-amostras.md#critérios-de-aceite).
- **Esforço:** M

### 7.3 Revisão
`/code-review`.

## Definição de pronto
- [ ] Saldo nunca negativo; movimentos e localização sempre na raiz.
