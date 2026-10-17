# Fase 8 — Relatórios e Complementos

**Objetivo:** relatórios gerenciais corrigidos, auditoria consultável, envio de boletim por e-mail e (se confirmado) relatório de ensaio.
**Modelo/modo:** 🟢 Sonnet · ⚡ accept edits para os relatórios depois do primeiro.
**Ler antes:** [P20](../processos/P20-relatorios-gerenciais.md) · [RN-09](../regras/RN-09-relatorios.md) · [T30-00…06](../telas/T30-00-relatorios-padrao.md) · [T06](../telas/T06-auditoria.md) · [T28](../telas/T28-relatorio-ensaio.md) · [P17](../processos/P17-emissao-boletim.md) (e-mail)
**Depende de:** Fases 5 e 6.

---

### 8.1 Seis relatórios gerenciais (T30-01 a T30-06)
- **Passos:** infraestrutura do [T30-00](../telas/T30-00-relatorios-padrao.md) (filtros, tela, PDF, CSV); um serviço por relatório com as regras de contagem do [P20](../processos/P20-relatorios-gerenciais.md); comparar com o legado num ano fechado e registrar as diferenças esperadas no MD de cada relatório.
- **Aceite:** totais conferidos; diferenças explicadas.
- **Esforço:** G

### 8.2 T28 — Relatório de ensaio (**confirmar antes**)
- **Passos:** se confirmado: alterar `Scripts/v2` (PDF/hash/trigger de congelamento) + doc 11; tela conforme o MD.
- **Esforço:** M (se confirmado)

### 8.3 Envio de boletim por e-mail
- **Passos:** configurar SMTP (dúvida 2); botão "Enviar por e-mail" no boletim emitido → contato do resultado da proposta (+ cópia opcional), anexando o PDF gravado; registrar envio em `AUDITORIA`.
- **Esforço:** S

### 8.4 T06 — Auditoria (consulta)
- **Passos:** filtros, grid, diferença antes/depois, aba histórico da amostra, CSV.
- **Esforço:** M

### 8.5 Revisão
`/simplify` + `/code-review`.

## Definição de pronto
- [ ] Relatórios com números corretos (sem duplicar revisões).
- [ ] Auditoria consultável; e-mail de boletim funcionando.
