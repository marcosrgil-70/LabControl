# T28 — Relatório de Ensaio

**Fase:** 8 (**confirmar com o usuário se ainda é necessário** — dúvida 1 do plano) · **Rota:** `/RelatorioEnsaio` · **Permissões:** `RELATORIO_ENSAIO.CONSULTAR/INCLUIR/ALTERAR/EXCLUIR/IMPRIMIR` · **Criticidade:** ⚪
**Legado:** `LabControl/un_Relatorio_Ensaio.pas` (483 linhas), menu *Movimentações → Relatório de Ensaio* — editor RTF livre (barra de fonte/negrito/alinhamento, abrir/salvar arquivo) + `Layout_Relatorio_Ensaio.fr3`. Tabela `LAB_RELATORIO_ENSAIO` **vazia**: nunca usado em produção.
**Objetos do banco:** [`LAB_RELATORIO_ENSAIO`](../banco/objetos/tabelas/LAB_RELATORIO_ENSAIO.md), [`TG_RE_BI`](../banco/objetos/triggers/TG_RE_BI.md), [`SEQUENCIAS_DOCUMENTOS`](../banco/objetos/tabelas/SEQUENCIAS_DOCUMENTOS.md), [`VW_LAB_AMOSTRAS`](../banco/objetos/views/VW_LAB_AMOSTRAS.md), [`VW_LAB_RESULTADOS`](../banco/objetos/views/VW_LAB_RESULTADOS.md)
**Processos:** [P03 Numeração](../processos/P03-numeracao-documentos.md) · [P17 Emissão](../processos/P17-emissao-boletim.md) (mesma lógica de congelamento) · **Regras:** [RN-08](../regras/RN-08-boletim.md)

## Como era no Delphi
- Novo registro: funcionário gerador = funcionário ativo, data/hora de geração = agora.
- Combo de amostra filtrado pelo status de resultados (`ID_AMOSTRAS_STATUS_RESULTADOS`) — **sem filtrar a empresa** (erro).
- Consulta por cliente → propostas do cliente → amostras; *"Deseja excluir o Relatório de Ensaio?"*.
- O número (`ULT_COD_RE`) era gravado errado pela trigger (`ULT_COD_RE = NEW.ID_EMPRESAS`).

## Uso previsto no v2
Para amostras cujo item da proposta tem `TIPO_DOCUMENTO = RELATORIO` (em vez de boletim): documento técnico livre (validações, desenvolvimento de metodologia etc.).

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Nº do relatório | `NR_RELATORIO`/`ANO_RELATORIO` | gerado pelo banco (anual por empresa); "INCLUSÃO" até gravar |
| Amostra* | `ID_LAB_HIST_AMOSTRAS` | amostras da empresa, última revisão, status que permite resultado; mostra tipo de amostra, tipo de análise e produto |
| Texto* | `TX_RELATORIO` | editor HTML; botão "Montar modelo" preenche dados da amostra, testes e resultados para editar |
| Gerado por / em | `ID_ENTIDADES_FUNC_GER`, `DH_GERACAO` | profissional do usuário / agora |
| Emitido por / em | `ID_ENTIDADES_FUNC_EMI`, `DH_EMISSAO` | na emissão |

## Ações
Incluir · Alterar (só não emitido) · Excluir (só não emitido) · Visualizar (PDF com marca "PRÉVIA") · **Emitir** (PDF definitivo, data de emissão, bloqueio — mesma lógica do boletim; exigirá colunas de PDF/hash ou reaproveitar o padrão do boletim) · Consultar (cliente → proposta → amostra).

## Pendências antes de implementar
- Confirmar necessidade com o usuário (nunca usado).
- Se mantido: adicionar `ARQUIVO_PDF`/`HASH_PDF` e trigger de congelamento, como no boletim (alterar `Scripts/v2` e o doc 11).

**Status:** ⬜ (aguardando confirmação)
