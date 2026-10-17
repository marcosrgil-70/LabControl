# T05 — Parâmetros da Empresa (antigo "Parâmetros do Sistema")

**Fase:** 1 · **Rota:** `/Empresa/Parametros` · **Permissões:** `PARAMETROS_EMPRESA.CONSULTAR/ALTERAR` · **Criticidade:** 🔴 (controla a proteção do número da amostra)
**Legado:** menu *Cadastros → Parâmetros do Sistema* = `Standards/un_Parametros.pas` + `unDM_cadParametros`, que mostrava/editava `ULT_COD_PROPOSTA`, `ULT_COD_AMOSTRA`, `ULT_PED_VENDA`, `ULT_COD_BOLETIM`, `ULT_COD_RE`. Os IDs de status ficavam na tabela sem tela (valores do legado: `ULT_COD_PROPOSTA = 1799`, `ULT_COD_AMOSTRA = 1402`, `ULT_PED_VENDA = 1471`, `ID_AMOSTRAS_STATUS_RESULTADOS = 1`, `ID_LAB_PROPOSTAS_STATUS_ENVCLI = NULL`).
**Objetos do banco:** [`EMPRESAS_PARAMETROS`](../banco/objetos/tabelas/EMPRESAS_PARAMETROS.md), [`SEQUENCIAS_DOCUMENTOS`](../banco/objetos/tabelas/SEQUENCIAS_DOCUMENTOS.md), [`AUDITORIA`](../banco/objetos/tabelas/AUDITORIA.md)
**Processos:** [P03 Numeração](../processos/P03-numeracao-documentos.md) · [P04 Número da amostra](../processos/P04-numero-amostra-ano.md) · [P05 Ciclo da proposta](../processos/P05-ciclo-proposta.md) · [P17 Emissão do boletim](../processos/P17-emissao-boletim.md) · **Regras:** [RN-01](../regras/RN-01-gerais-multiempresa.md)

## Aba "Parâmetros" (`EMPRESAS_PARAMETROS`)
| Campo | Coluna | Padrão | Uso |
|---|---|---|---|
| Status inicial da proposta | `ID_LAB_PROPOSTAS_STATUS_INICIAL` | Em elaboração | nova proposta e nova revisão (`PR_REVISAO_PROPOSTA`) |
| Status "Enviado ao cliente" | `ID_LAB_PROPOSTAS_STATUS_ENVCLI` | Enviado ao cliente | `TG_PROPOSTAS_BU` grava `DT_ENVIO_CLIENTE` ao entrar nesse status |
| Status inicial da amostra | `ID_AMOSTRAS_STATUS_INICIAL` | Liberada para análise | recebimento (T22) |
| Status que permite resultados (legado) | `ID_AMOSTRAS_STATUS_RESULTADOS` | Liberada | informativo; no v2 vale a flag `AMOSTRAS_STATUS.PERMITE_RESULTADO` |
| Status inicial do boletim | `ID_BOLETINS_STATUS_INICIAL` | Em elaboração | boletim novo e revisão |
| Status do boletim emitido | `ID_BOLETINS_STATUS_EMITIDO` | Enviado ao cliente | `SP_EMITIR_BOLETIM` |
| **Dias retroativos para a data de entrega** | `DIAS_RETROATIVOS_ENTREGA` | **7** | `TG_AMOSTRAS_BI`: data mais antiga exige confirmação + permissão |
| Validade padrão da proposta (dias) | `DIAS_VALIDADE_PROPOSTA` | 30 | sugere `DT_VALIDADE` |
| Período padrão de pesquisa (dias) | `DIAS_PESQUISA_PADRAO` | 365 | "exibir a partir de" das consultas |
| Exige 2º responsável no boletim | `EXIGE_RESP2_BOLETIM` | não | `SP_EMITIR_BOLETIM` |
| Legenda padrão do boletim | `TEXTO_PADRAO_LEGENDA` | — | sugerida no boletim novo |
| Contrato padrão da proposta | `TEXTO_PADRAO_CONTRATO` | — | botão "Usar contrato padrão" (T20) |

Validações: dias ≥ 0 (`DIAS_RETROATIVOS_ENTREGA` entre 0 e 365, com aviso se > 30); combos só com status **da empresa**; alteração grava `AUDITORIA`.

## Aba "Numeração" (`SEQUENCIAS_DOCUMENTOS`) — substitui os `ULT_*` do legado
- Grid somente leitura: tipo (Amostra, Proposta, Pedido de venda, Produto, Relatório de ensaio), ano (0 = não anual), último número, data da última alteração.
- **Ajustar** (só ADMIN_GLOBAL): novo valor + motivo obrigatório. Recusar valor **menor** que o maior número já usado (`MAX(COD_AMOSTRA)` do ano, `MAX(COD_PROPOSTA)` do ano, `MAX(NR_PEDIDO)`, maior código numérico de produto). Grava `AUDITORIA` com antes/depois.
- Explicação na tela: "A numeração reinicia sozinha a cada ano (linha nova por ano). Não é preciso zerar nada na virada do ano."

## Erros do legado a não repetir
- Editar `ULT_COD_AMOSTRA` livremente na tela podia **duplicar** números (não havia `UNIQUE`).
- `ULT_COD_BOLETIM` e `ULT_COD_RE` eram mostrados, mas nunca usados (o boletim usa o número da amostra; o RE gravava o ID da empresa).
- `ID_LAB_PROPOSTAS_STATUS_ENVCLI` nulo: a data de envio ao cliente nunca era gravada automaticamente.

## Critérios de aceite
- [ ] Mudar `DIAS_RETROATIVOS_ENTREGA` altera imediatamente a regra do recebimento.
- [ ] Ajuste de numeração para valor menor é recusado; ajuste válido aparece na Auditoria.
- [ ] Combos mostram só status da empresa ativa.

**Status:** ⬜
