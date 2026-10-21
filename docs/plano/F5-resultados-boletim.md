# Fase 5 — Resultados e Boletim

**Objetivo:** lançar resultados, montar e **emitir** boletins com PDF congelado (hash), revisões em cascata — sobre resultados e boletins reais migrados.
**Modelo/modo:** 🔵 Opus · 📐 antes de 5.2 e 5.3 · ✋.
**Ler antes:** [P14](../processos/P14-lancamento-resultados.md) · [P15](../processos/P15-revisao-resultado.md) · [P16](../processos/P16-montagem-boletim.md) · [P17](../processos/P17-emissao-boletim.md) · [P18](../processos/P18-revisao-boletim.md) · [RN-07](../regras/RN-07-resultados.md) · [RN-08](../regras/RN-08-boletim.md) · telas [T26](../telas/T26-resultados.md), [T27](../telas/T27-boletim.md)
**Depende de:** Fase 4.

---

### 5.1 Migração parte 3 (M-16 a M-18) 🔵
- **Passos:** resultados (revisão renumerada por amostra raiz, textos formatados recalculados em lote), boletins (código base, revisão por código, **congelados**, `RESULTADO_CONCAT` como impresso), sequências (máximos por ano; `@LC_MIGRACAO = 0`; teste de próxima numeração numa cópia).
- **Aceite:** 5.345/19.528 e 5.312/19.615; uma última revisão por documento; todos os boletins migrados emitidos e bloqueados.
- **Esforço:** G

### 5.2 T26 — Resultados 🔵
> Pendência 3 (2.4): combo de **tipo de resultado** com o "+" — `lc-rapido="LAB_TIPOS_RESULTADOS"` ([T10-00](../telas/T10-00-crud-auxiliares.md#implementação-tarefa-24--cadastro-rápido-)).
- **Passos:** seleção da amostra (empresa, última revisão, permite resultado, 400 dias) e **fila de análises**; cartão por teste com numérico × descritivo (habilitação do legado), especificação, datas, analista, quantidade, conforme; "lançar todos"; prévia do texto formatado (relido do banco); cadastro rápido de tipo de resultado; excluir linha/todos com travas; **criar revisão** (com "revisar boletim") e **desfazer**.
- **Aceite:** [aceite da T26](../telas/T26-resultados.md#aceite) + cenários do P14/P15.
- **Esforço:** G

### 5.3 T27 — Boletim 🔵
- **Passos:** amostra com resultado; cabeçalho; responsáveis (assina boletim); seleção de parâmetros em dois grids com ordem arrastável; metodologias automáticas (`FN_OBS_METODOS`), observação, legenda; "atualizar textos"; **visualizar** (prévia com marca d'água); **emitir** (PDF definitivo + SHA-256 + `SP_EMITIR_BOLETIM`, apagar PDF se falhar); reimpressão do PDF gravado; **criar revisão** e **desfazer**; consulta.
- **Aceite:** [aceite da T27](../telas/T27-boletim.md#aceite) + tabela "depois de emitido" do [P17](../processos/P17-emissao-boletim.md).
- **Esforço:** G · **Depende de:** 5.2

### 5.4 Layout do PDF do boletim (QuestPDF) 🔵
> Pendente da T03 (1.5): o **logo da empresa** (`EMPRESAS.LOGO`, servido por `EmpresaService.LogoAsync`) e o nº de acreditação no cabeçalho — último item do aceite da T03.
- **Passos:** reproduzir o layout do `frxRboletin` com os dados do [P16](../processos/P16-montagem-boletim.md#layout-do-pdf-dados-do-qry_impbolet-do-legado); expoente sobrescrito; coluna de especificação condicional; acreditação; assinaturas só no emitido; 2ª via de boletim migrado. Validar **lado a lado** com 3 boletins reais do legado (pedir PDFs/papel — dúvida 5).
- **Aceite:** usuário aprova o layout.
- **Esforço:** M · **Depende de:** 5.3

### 5.5 Testes de aceite + revisão
Cenários P14–P18 automatizados; cascata completa amostra → resultado → boletim (roteiro 12 §7 itens 6–9); `/code-review high`.

## Definição de pronto
- [ ] Boletim emitido imutável (banco) e reimpressão idêntica (hash).
- [ ] Revisões em cascata corretas; histórico migrado congelado.
