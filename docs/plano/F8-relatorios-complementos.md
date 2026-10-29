# Fase 8 — Relatórios e Complementos

**Objetivo:** relatórios gerenciais corrigidos, auditoria consultável, envio de boletim por e-mail e (se confirmado) relatório de ensaio.
**Modelo/modo:** 🟢 Sonnet · ⚡ accept edits para os relatórios depois do primeiro.
**Ler antes:** [P20](../processos/P20-relatorios-gerenciais.md) · [RN-09](../regras/RN-09-relatorios.md) · [T30-00…06](../telas/T30-00-relatorios-padrao.md) · [T06](../telas/T06-auditoria.md) · [T28](../telas/T28-relatorio-ensaio.md) · [P17](../processos/P17-emissao-boletim.md) (e-mail)
**Depende de:** Fases 5 e 6.

---

### 8.1 Seis relatórios gerenciais (T30-01 a T30-06)
> ✅ **Concluída em 29/10/2026** — ver [T30-00 › Implementação](../telas/T30-00-relatorios-padrao.md#implementação-tarefa-81) e a comparação com o legado em 2017; aceite em `RelatoriosTests` e no navegador `Relatorios_visualizam_filtram_e_exportam`. Dúvida 9: "somente com pedido" marcado por padrão (decisão do usuário).

- **Passos:** infraestrutura do [T30-00](../telas/T30-00-relatorios-padrao.md) (filtros, tela, PDF, CSV); um serviço por relatório com as regras de contagem do [P20](../processos/P20-relatorios-gerenciais.md); comparar com o legado num ano fechado e registrar as diferenças esperadas no MD de cada relatório.
- **Aceite:** totais conferidos; diferenças explicadas.
- **Esforço:** G

### 8.2 T28 — Relatório de ensaio (**confirmar antes**)
> ⛔ **Dispensada em 29/10/2026** — dispensada pelo usuário em 29/10/2026 (dúvida 1): a tabela está vazia no legado, ninguém emitiu relatório de ensaio.

- **Passos:** se confirmado: alterar `Scripts/v2` (PDF/hash/trigger de congelamento) + doc 11; tela conforme o MD.
- **Esforço:** M (se confirmado)

### 8.3 Envio de boletim por e-mail
> ✅ **Concluída em 29/10/2026** — dúvida 2 respondida pelo usuário: a configuração do servidor fica numa **aba "E-mail" em Parâmetros da Empresa**, por empresa e editável pelo usuário (D-26). Ver [T05 › Aba E-mail](../telas/T05-parametros-empresa.md#aba-e-mail-tarefa-83-29102026--pedido-do-usuário) e [T27 › Envio por e-mail](../telas/T27-boletim.md#envio-por-e-mail-tarefa-83-29102026); aceite em `EmailTests` e no navegador `Email_configurado_na_empresa_envia_o_boletim_emitido`.

- **Passos:** configurar SMTP (dúvida 2); botão "Enviar por e-mail" no boletim emitido → contato do resultado da proposta (+ cópia opcional), anexando o PDF gravado; registrar envio em `AUDITORIA`.
- **Esforço:** S

### 8.4 T06 — Auditoria (consulta)
- **Passos:** filtros, grid, diferença antes/depois, aba histórico da amostra, CSV.
- **Esforço:** M

### 8.5 Revisão
`/simplify` + `/code-review`.

## Definição de pronto
- [x] Relatórios com números corretos (sem duplicar revisões). — 8.1, comparados com o legado em 2017.
- [ ] Auditoria consultável (8.4); e-mail de boletim funcionando — ✅ 8.3 (com servidor falso nos testes; envio real a conferir com a conta do laboratório, pendência 38).
