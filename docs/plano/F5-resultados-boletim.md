# Fase 5 — Resultados e Boletim

**Objetivo:** lançar resultados, montar e **emitir** boletins com PDF congelado (hash), revisões em cascata — sobre resultados e boletins reais migrados.
**Modelo/modo:** 🔵 Opus · 📐 antes de 5.2 e 5.3 · ✋.
**Ler antes:** [P14](../processos/P14-lancamento-resultados.md) · [P15](../processos/P15-revisao-resultado.md) · [P16](../processos/P16-montagem-boletim.md) · [P17](../processos/P17-emissao-boletim.md) · [P18](../processos/P18-revisao-boletim.md) · [RN-07](../regras/RN-07-resultados.md) · [RN-08](../regras/RN-08-boletim.md) · telas [T26](../telas/T26-resultados.md), [T27](../telas/T27-boletim.md)
**Depende de:** Fase 4.

---

### 5.1 Migração parte 3 (M-16 a M-18) 🔵
> ✅ **Concluída em 27/10/2026** — ver [T07 › Implementação — parte 3](../telas/T07-migracao.md#implementação--parte-3-tarefa-51-27102026) e [M-16](../processos/migracao/M-16-resultados.md)…[M-18](../processos/migracao/M-18-sequencias.md); aceite em `MigracaoTests`. Números finais: 5.345 / 19.497 e 5.312 / 19.469 (fusões e linhas antigas do legado listadas nas anomalias).
- **Passos:** resultados (revisão renumerada por amostra raiz, textos formatados recalculados em lote), boletins (código base, revisão por código, **congelados**, `RESULTADO_CONCAT` como impresso), sequências (máximos por ano; `@LC_MIGRACAO = 0`; teste de próxima numeração numa cópia).
- **Aceite:** 5.345/19.528 e 5.312/19.615; uma última revisão por documento; todos os boletins migrados emitidos e bloqueados.
- **Esforço:** G

### 5.2 T26 — Resultados 🔵
> ✅ **Concluída em 27/10/2026** — ver [T26 › Implementação](../telas/T26-resultados.md#implementação-tarefa-52-27102026); aceite em `ResultadosTests` e no teste de navegador `Resultado_lanca_todos_com_previa_e_exclui_parametro`.
> Pendência 3 (2.4): combo de **tipo de resultado** com o "+" — `lc-rapido="LAB_TIPOS_RESULTADOS"` ([T10-00](../telas/T10-00-crud-auxiliares.md#implementação-tarefa-24--cadastro-rápido-)).
- **Passos:** seleção da amostra (empresa, última revisão, permite resultado, 400 dias) e **fila de análises**; cartão por teste com numérico × descritivo (habilitação do legado), especificação, datas, analista, quantidade, conforme; "lançar todos"; prévia do texto formatado (relido do banco); cadastro rápido de tipo de resultado; excluir linha/todos com travas; **criar revisão** (com "revisar boletim") e **desfazer**.
- **Aceite:** [aceite da T26](../telas/T26-resultados.md#aceite) + cenários do P14/P15.
- **Esforço:** G

### 5.3 T27 — Boletim 🔵
> **Decidido em 27/10/2026:** biblioteca **QuestPDF** (dúvida 10); PDFs numa **pasta do servidor** por empresa/ano, com caminho + SHA-256 no banco (dúvida 6); layout a partir do `frxRboletin` do Delphi, validado com 3 boletins reais na 5.4 (dúvida 5).
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
