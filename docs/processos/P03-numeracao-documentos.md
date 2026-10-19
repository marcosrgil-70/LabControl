# P03 — Numeração de Documentos

**Criticidade:** 🔴 · **Fase:** 0 (banco) — usada nas fases 3 a 8
**Telas:** [T05 Parâmetros/Numeração](../telas/T05-parametros-empresa.md), [T14 Produtos](../telas/T14-produtos.md), [T20 Propostas](../telas/T20-propostas.md), [T21 Pedidos](../telas/T21-pedidos-venda.md), [T22 Amostras](../telas/T22-recebimento-amostras.md), [T28 RE](../telas/T28-relatorio-ensaio.md)
**Objetos:** [`SEQUENCIAS_DOCUMENTOS`](../banco/objetos/tabelas/SEQUENCIAS_DOCUMENTOS.md), [`SP_PROXIMO_NUMERO`](../banco/objetos/rotinas/SP_PROXIMO_NUMERO.md), [`TG_AMOSTRAS_BI`](../banco/objetos/triggers/TG_AMOSTRAS_BI.md), [`TG_PROPOSTAS_BI`](../banco/objetos/triggers/TG_PROPOSTAS_BI.md), [`TG_PEDVEN_BI`](../banco/objetos/triggers/TG_PEDVEN_BI.md), [`TG_PRODUTOS_BI`](../banco/objetos/triggers/TG_PRODUTOS_BI.md), [`TG_RE_BI`](../banco/objetos/triggers/TG_RE_BI.md), [`FN_NR_PROPOSTA`](../banco/objetos/rotinas/FN_NR_PROPOSTA.md), [`FN_NR_AMOSTRA_ID`](../banco/objetos/rotinas/FN_NR_AMOSTRA_ID.md)
**Regras:** [RN-01](../regras/RN-01-gerais-multiempresa.md) · Detalhes técnicos: [banco/12 §1](../banco/12-PROCEDURES-REVISOES.md)

## Documentos numerados
| Documento | Tipo em `SEQUENCIAS_DOCUMENTOS` | Anual? | Formato | Gerado por |
|---|---|---|---|---|
| Amostra | `AMOSTRA` | sim (ano do servidor) | `TT-CCCCC-AA/YY [Rnn]` | `TG_AMOSTRAS_BI` (revisão 0) — ver [P04](P04-numero-amostra-ano.md) |
| Proposta | `PROPOSTA` | sim | `000123/26-R0` | `TG_PROPOSTAS_BI` (revisão 0, `COD_PROPOSTA = 0`) |
| Pedido de venda | `PEDIDO_VENDA` | não (`ANO = 0`) | `000000123` (9 dígitos na tela) | `TG_PEDVEN_BI` |
| Produto | `PRODUTO` | não | `000123` (6 dígitos) | `TG_PRODUTOS_BI` (se o código vier vazio) |
| Relatório de ensaio | `RELATORIO_ENSAIO` | sim | nº/ano | `TG_RE_BI` |
| Boletim | — | — | = número base da amostra | `TG_BOLETIM_BI` (`COD_BOLETIM`) |

## Fluxo (dentro da transação do INSERT do documento)
1. Aplicação faz o INSERT **sem** número (0 / vazio).
2. Trigger `BEFORE INSERT` chama `SP_PROXIMO_NUMERO(empresa, tipo, ano, @n)`:
   - `INSERT … VALUES (…, 1) ON DUPLICATE KEY UPDATE ULTIMO_NUMERO = ULTIMO_NUMERO + 1` cria a linha com 1 ou incrementa, numa única instrução com trava **exclusiva** até o COMMIT;
   - `SELECT ULTIMO_NUMERO` devolve o número (a própria transação enxerga o valor que acabou de gravar).
   - (Até 19/10/2026 era `INSERT IGNORE` + `SELECT … FOR UPDATE` + `UPDATE`: o IGNORE pegava trava compartilhada e duas gravações simultâneas na mesma empresa davam **deadlock** — 66 de 80 no teste `ConcorrenciaTests`.)
3. Trigger monta o texto formatado (`NR_PROPOSTA`, `NR_AMOSTRA`).
4. COMMIT libera o bloqueio; um segundo usuário simultâneo espera e recebe o número seguinte.
5. Aplicação **relê** a linha e mostra o número ("Código da Amostra: …", "Nr. da Proposta: …").

## Regras
- O ano nunca vem de data digitada: sempre `YEAR(CURDATE())` do servidor.
- Número/ano enviados no INSERT são **ignorados** fora da migração (proposta, amostra, pedido, relatório de ensaio): a trigger sempre numera. Até a revisão 0.9 a proposta, o pedido e o relatório guardavam um número ≠ 0 enviado, o que podia travar a sequência (o próximo número gerado colidiria no `UNIQUE`). O EF também nunca envia essas colunas (gerador: `fixo`).
- Nada é "zerado": a primeira amostra de 2027 cria a linha `(emp, AMOSTRA, 2027)` com 1.
- Rollback da transação devolve o número (sem lacunas por erro de validação; lacunas só se um documento gravado for excluído).
- `UNIQUE` no banco impede duplicidade mesmo com bug na aplicação: `UQ_AMO_NUM`, `UQ_PROP_NUM`, `UQ_PEDVEN_NR`, `UQ_PROD_COD`, `UQ_RE_NR`.
- Ajuste manual só pela T05 (ADMIN_GLOBAL, nunca menor que o máximo usado, com motivo em `AUDITORIA`).
- Migração: `@LC_MIGRACAO = 1` preserva os números do legado; ao final M-18 grava os máximos por ano.

## Erros do legado corrigidos (doc 10 §4)
A1 ano da data digitada · A2 reinício pela data da última amostra · A3 contador sem ano · A4 número consumido ao abrir a aba Observação · A5 sem bloqueio · A6 sem `UNIQUE` · `COD_PROPOSTA = ID` provisório (12 propostas) · `ULT_COD_RE = ID_EMPRESAS`.

## Cenários de teste
- Dois INSERTs simultâneos de amostra (duas conexões, transações abertas) → números diferentes, o segundo espera o primeiro.
- Virada de ano simulada (inserir sequência do ano anterior) → primeira amostra do ano = 00001; números antigos intactos.
- Erro de validação depois do número reservado → rollback, número reaproveitado.
- Roteiro do [doc 12 §7](../banco/12-PROCEDURES-REVISOES.md#7-roteiro-de-teste-executado-em-16102026--reexecutar-após-qualquer-mudança) itens 2–5 e 12.
