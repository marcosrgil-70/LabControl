# P16 — Montagem do Boletim (Laudo de Análise)

**Criticidade:** 🔴🔴 · **Fase:** 5
**Telas:** [T27 Boletim](../telas/T27-boletim.md)
**Objetos:** [`LAB_BOLETIM_ANALISES`](../banco/objetos/tabelas/LAB_BOLETIM_ANALISES.md), [`LAB_BOLETIM_PARAMETROS`](../banco/objetos/tabelas/LAB_BOLETIM_PARAMETROS.md), [`VW_LAB_RESULTADOS`](../banco/objetos/views/VW_LAB_RESULTADOS.md), [`VW_LAB_BOLETINS`](../banco/objetos/views/VW_LAB_BOLETINS.md), [`FN_OBS_METODOS`](../banco/objetos/rotinas/FN_OBS_METODOS.md), [`FN_ACREDITADO`](../banco/objetos/rotinas/FN_ACREDITADO.md), [`TG_BOLETIM_BI`](../banco/objetos/triggers/TG_BOLETIM_BI.md), [`TG_BOLPAR_BI`](../banco/objetos/triggers/TG_BOLPAR_BI.md), [`LAB_PARAMETROS_ANALISES`](../banco/objetos/tabelas/LAB_PARAMETROS_ANALISES.md) (`DESC_REDUZIDA`, `AUDITADO`, `DESCR_METODO`)
**Regras:** [RN-08](../regras/RN-08-boletim.md)

## Fluxo
1. **Amostra**: da empresa, última revisão, **com resultado** (última revisão do resultado), últimos 400 dias. Se já houver boletim vigente (`ULTIMA_REVISAO = 1`), "Incluir" abre o existente.
2. **Cabeçalho**: `COD_BOLETIM` = número base da amostra sem " Rnn" (trigger), data do boletim (hoje), resultado = última revisão do resultado da amostra, status inicial, gerador = profissional do usuário.
3. **Responsáveis**: Responsável 1 (obrigatório para emitir) e 2 (obrigatório se `EXIGE_RESP2_BOLETIM`), entre profissionais com `ASSINA_BOLETIM`, mostrando nome, tipo e nº de registro.
4. **Seleção de parâmetros** (Delphi `grd_ParamResult` → `grd_ParamBoletim`):
   - esquerda: resultados (`VW_LAB_RESULTADOS` do resultado vinculado) com checkbox, parâmetro, data, resultado 01 (numérico formatado), resultado 02 (descritivo), unidade, qtde analisada;
   - "Concluir seleção" (*"Nenhum Parâmetro foi selecionado."* se vazio) → grava em `LAB_BOLETIM_PARAMETROS` com o **texto congelado**: `DESC_PARAMETRO` = descrição reduzida, `RESULTADO_CONCAT` (numérico) ou `TIPO_RESULTADO_DESC` (descritivo), `UNIDADE_SG`, `ESPECIFICACAO`, `ACREDITADO`;
   - direita: parâmetros do boletim, **reordenáveis** (arrastar → `ORDEM`), excluir; padrão de ordem: acreditados primeiro (legado `ORDER BY AUDITADO DESC`).
5. **Textos**:
   - Observações (sugere `OBSERVACAO_VERIFICACAO` da amostra);
   - Metodologias **não acreditadas** = `FN_OBS_METODOS(amostra, 0)`; **acreditadas** = `FN_OBS_METODOS(amostra, 1)` ("DESCR_METODO: método") — editáveis;
   - Legenda (sugere `TEXTO_PADRAO_LEGENDA`).
6. **Atualizar textos** (enquanto não emitido): regrava os textos dos parâmetros a partir dos resultados atuais.
7. **Visualizar**: PDF de prévia com marca d'água "PRÉVIA — NÃO VÁLIDO" e sem assinaturas. Emissão: [P17](P17-emissao-boletim.md).

## Layout do PDF (dados do `qry_ImpBolet` do legado)
> **Layout final (5.4, 30/10/2026):** ver [T27 › Layout do PDF](../telas/T27-boletim.md#layout-do-pdf-tarefa-54-30102026) — selo só nas páginas dos acreditados e boletim misto em duas partes (A/B), como o legado. A lista abaixo é a dos dados reunidos na 5.3.

Cabeçalho da empresa (logo, endereço, nº de acreditação se `FN_ACREDITADO` ≥ 1) · Boletim nº + revisão · cliente (nome, CPF/CNPJ), endereço do laudo, contato do resultado · proposta · **pedido de venda** · amostra: nº (com R), produto, lote, fabricação e validade parciais, data de recebimento, coletor e data/hora da coleta, observação da verificação · período de análise (menor `DT_INICIAL` → maior `DT_RESULTADO`) · tabela: parâmetro, resultado (expoente sobrescrito), unidade, especificação (coluna só se algum parâmetro tiver especificação), marca de acreditado · metodologias (acreditado / não acreditado) · observações · legenda · responsáveis (nome + sobrenome, cargo, registro "CRF-SP 12345", assinatura **só no emitido**) · local e data (cidade da empresa + data do boletim) · rodapé.

## Regras
- Só parâmetros do resultado vinculado; cada um uma vez (`UQ_BOLPAR`); banco recusa os de outro resultado.
- O que é impresso é o que está gravado em `LAB_BOLETIM_PARAMETROS` (texto congelado).
- Boletim vigente por amostra: um (`ULTIMA_REVISAO = 1`).

## Cenários de teste
- Roteiro doc 12 §7 item 7: boletim com **1** dos 2 parâmetros; metodologia acreditada `CONT. TOTAL: ISO 4833`.
- Ordem escolhida respeitada no PDF.
- Parâmetro de outro resultado → "O parâmetro não pertence ao resultado do boletim."
- Validar o PDF lado a lado com um boletim real do legado (fase 5.4).

## Implementação (tarefa 5.3, 27/10/2026)
Tela [T27](../telas/T27-boletim.md#implementação-tarefa-53-27102026): `BoletinsController` + `BoletimService`. Na inclusão, cabeçalho + parâmetros marcados numa transação; depois "Concluir seleção", ▲/▼ (ordem 1..n), "Retirar" e "Atualizar textos". Texto congelado pela **mesma regra do `PR_REVISAO_BOLETIM`** (numérico → `RESULTADO_CONCAT`, descritivo → `TIPO_RESULTADO_DESC`, unidade, especificação, acreditado). PDF em `Services/Boletins/BoletimPdf.cs` com todas as seções do layout acima; no PDF a unidade sai do texto do resultado e vai para a coluna própria. Cenários em `BoletinsTests`. **Status:** ✅ montagem; 🟨 layout lado a lado com boletins reais na 5.4.
