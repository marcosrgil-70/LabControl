# T27 — Boletim (Laudo de Análise) ⚠️⚠️ (tela com particularidades)

**Fase:** 5 · **Rota:** `/Boletins` · **Permissões:** `BOLETINS.CONSULTAR/INCLUIR/ALTERAR/EXCLUIR/IMPRIMIR/EMITIR/REVISAR/DESFAZER_REVISAO`
**Legado:** `un_CadBoletim.pas` (menu *Boletim*, form "Manutenção de Boletim") + `undm_Boletim.pas` + relatório `frxRboletin` (`Layout_ImpBoletim.fr3`). `un_Boletim.pas` é uma versão antiga não usada no menu.
**Tabelas:** `LAB_BOLETIM_ANALISES` (cabeçalho + textos), `LAB_BOLETIM_PARAMETROS` (parâmetros escolhidos); views `VW_LAB_BOLETINS`, `VW_LAB_RESULTADOS`; funções `FN_OBS_METODOS`, `FN_ACREDITADO`; rotinas `SP_EMITIR_BOLETIM`, `SP_GERAR_REVISAO_BOLETIM`, `SP_EXCLUIR_REVISAO_BOLETIM`.

## Referências cruzadas
- **Delphi:** `LabControl/un_CadBoletim.pas` (1.278 linhas) + `undm_Boletim.pas` (503); `un_Boletim.pas` é versão antiga fora do menu
- **Objetos do banco:** [`LAB_BOLETIM_ANALISES`](../banco/objetos/tabelas/LAB_BOLETIM_ANALISES.md), [`LAB_BOLETIM_PARAMETROS`](../banco/objetos/tabelas/LAB_BOLETIM_PARAMETROS.md), [`VW_LAB_BOLETINS`](../banco/objetos/views/VW_LAB_BOLETINS.md), [`VW_LAB_RESULTADOS`](../banco/objetos/views/VW_LAB_RESULTADOS.md), [`SP_EMITIR_BOLETIM`](../banco/objetos/rotinas/SP_EMITIR_BOLETIM.md), [`SP_GERAR_REVISAO_BOLETIM`](../banco/objetos/rotinas/SP_GERAR_REVISAO_BOLETIM.md), [`SP_EXCLUIR_REVISAO_BOLETIM`](../banco/objetos/rotinas/SP_EXCLUIR_REVISAO_BOLETIM.md), [`FN_OBS_METODOS`](../banco/objetos/rotinas/FN_OBS_METODOS.md), [`FN_ACREDITADO`](../banco/objetos/rotinas/FN_ACREDITADO.md), [`TG_BOLETIM_BI`](../banco/objetos/triggers/TG_BOLETIM_BI.md), [`TG_BOLETIM_BU`](../banco/objetos/triggers/TG_BOLETIM_BU.md), [`TG_BOLETIM_BD`](../banco/objetos/triggers/TG_BOLETIM_BD.md), [`TG_BOLPAR_BI`](../banco/objetos/triggers/TG_BOLPAR_BI.md)
- **Processos:** [P16 Montagem do boletim](../processos/P16-montagem-boletim.md) · [P17 Emissão](../processos/P17-emissao-boletim.md) · [P18 Revisão de boletim](../processos/P18-revisao-boletim.md)
- **Regras de negócio:** [RN-08 Boletim](../regras/RN-08-boletim.md)
- **Plano:** [00-PLANO-EXECUCAO.md](../00-PLANO-EXECUCAO.md)

## 1. Cabeçalho (grupo "Amostra")
| Campo | Coluna | Regra |
|---|---|---|
| Amostra* | `ID_LAB_HIST_AMOSTRAS` | amostras da empresa, **última revisão**, **com resultado** (última revisão do resultado), dos últimos 400 dias; colunas Nº amostra, Produto, Lote |
| Nome da amostra / Lote | só leitura | — |
| Boletim nº | `COD_BOLETIM` | = número base da amostra (sem " Rnn"), gerado pelo banco; só leitura |
| Revisão | `REVISAO` | só leitura |
| Data do boletim* | `DT_BOLETIM` | padrão hoje |
| Resultado | `ID_LAB_RESULTADOS_ANALISES` | automático: última revisão do resultado da amostra |
| Status | `ID_BOLETINS_STATUS` | inicial / emitido (parâmetros da empresa) |

Uma amostra tem **um** boletim "vigente" (`ULTIMA_REVISAO = 1`); "Incluir" para amostra que já tem boletim abre o existente.

## 2. Responsáveis pelo boletim
Responsável 1 (`ID_ENTIDADES_FUNC_RESP`) e Responsável 2 (`ID_ENTIDADES_FUNC_RESP2`): profissionais com **Assina boletim**, colunas Profissional, Registro (tipo), Nº registro. O 1º é obrigatório para emitir; o 2º, se o parâmetro da empresa exigir. O PDF imprime nome + sobrenome, cargo e registro (ex.: "Farmacêutica — CRF-SP 12345") e a assinatura quando emitido.

## 3. Seleção de parâmetros — **particularidade**
Dois grids lado a lado (Delphi `grd_ParamResult` → `grd_ParamBoletim`):

**Esquerda — Resultados da amostra** (`VW_LAB_RESULTADOS` da última revisão): checkbox, parâmetro, data do resultado, Resultado 01 (numérico formatado), Resultado 02 (descritivo), unidade, qtde da amostra.
**Botão "Concluir seleção"** → copia os marcados para a direita.
**Direita — Parâmetros do boletim** (`LAB_BOLETIM_PARAMETROS`): parâmetro (`DESC_PARAMETRO` = descrição reduzida), Resultado 1 (`RESULTADO_CONCAT`), Resultado 2 (`TIPO_RESULTADO_DESC`), unidade, especificação, **ordem** (arrastar para reordenar — novo), excluir.
- O texto do resultado é **congelado** no boletim no momento da seleção (o que for impresso é o que está gravado).
- Um parâmetro só uma vez por boletim; só parâmetros do resultado vinculado (o banco garante).
- Botão "Atualizar textos" (enquanto não emitido) regrava os textos a partir dos resultados atuais.

## 4. Textos (grupos do Delphi)
| Grupo | Coluna | Regra |
|---|---|---|
| Observações | `TXT_OBSERVACAO` | livre; sugere `OBSERVACAO_VERIFICACAO` da amostra |
| Metodologias utilizadas — **Não acreditado** | `TXT_METODOLOGIA_UTIL` | preenchido automaticamente por `FN_OBS_METODOS(amostra, 0)` ("DESCR_METODO: método" dos parâmetros não acreditados) — editável |
| Metodologias utilizadas — **Acreditado** | `TXT_METODOLOGIA_ACRED` | `FN_OBS_METODOS(amostra, 1)` — editável |
| Legenda | `TXT_LEGENDA` | sugere `TEXTO_PADRAO_LEGENDA` |

## 5. Emissão (novo — no legado nunca funcionou)
1. "Visualizar" gera o PDF de prévia com marca d'água **"PRÉVIA — NÃO VÁLIDO"**, sem assinaturas.
2. **"Emitir"** (`BOLETINS.EMITIR`): gera o PDF definitivo (com logo, rodapé e assinaturas), salva em `storage/boletins/{empresa}/{ano}/{COD_BOLETIM} R{rev}.pdf`, calcula SHA-256 e chama `SP_EMITIR_BOLETIM(id, profissional, arquivo, hash)`.
3. Boletim emitido: **somente leitura** (o banco bloqueia alterar/excluir boletim, parâmetros e o resultado vinculado). Reimpressões usam **o PDF gravado**.
4. Para corrigir: **Criar revisão** (motivo) → `SP_GERAR_REVISAO_BOLETIM` → revisão nova não emitida, com os mesmos parâmetros escolhidos, apontando para o resultado vigente.
5. "Desfazer revisão" só para revisão não emitida (`SP_EXCLUIR_REVISAO_BOLETIM`).
6. Exportações (Delphi: PDF e RTF/Word): PDF; "Enviar por e-mail" ao contato do resultado da proposta (fase 8).

## 6. Layout do PDF (dados de `qry_ImpBolet` do legado)
Cabeçalho da empresa (logo, nº de acreditação se `FN_ACREDITADO` ≥ 1) · **Boletim nº** + revisão · Cliente (nome, CPF/CNPJ), endereço do laudo, contato · Proposta nº · **Pedido de venda nº** · Amostra: nº (com R), nome, lote, **fabricação e validade parciais** (dd/mm/aaaa, mm/aaaa ou aaaa), data de recebimento, coletor e data/hora da coleta, observação da verificação · Período de análise (início = menor `DT_INICIAL`, fim = maior `DT_RESULTADO`) · Tabela: parâmetro, resultado (expoente sobrescrito), unidade, especificação (coluna só se algum parâmetro tiver especificação — regra `ESPECIFICACAO` do legado), parâmetros acreditados marcados (ordenados acreditados primeiro, como o `ORDER BY AUDITADO DESC`) · Metodologias (acreditado / não acreditado) · Observações · Legenda · Responsáveis com assinatura · Local e data (cidade da empresa + data do boletim) · Rodapé.

## 7. Consulta
Filtros: cliente (filtra as amostras), amostra, "exibir a partir de" (−365 dias), emitido s/n, só última revisão. Grid: nº amostra, código do boletim, revisão, data do boletim, data de geração, data de emissão, produto, status.

## 8. Erros do legado a não repetir (doc 12 §5)
- Revisão do boletim criava um 2º boletim para o mesmo resultado (120 casos) e depois quebrava com "multiple rows".
- Revisão apontava para o resultado **antigo**, copiava **todos** os parâmetros e herdava a data de emissão.
- Boletim nunca era marcado como emitido ⇒ nada ficava protegido.

## Aceite
- [ ] Só parâmetros marcados vão para o boletim, na ordem escolhida.
- [ ] Metodologias acreditadas/não acreditadas preenchidas automaticamente.
- [ ] Emitir exige Responsável 1 e gera PDF com hash; depois disso nada é alterável.
- [ ] Revisão nasce não emitida, com o resultado vigente e os mesmos parâmetros.
- [ ] Impressão de boletim emitido reproduz exatamente o PDF gravado.

**Status:** ⬜ — tarefas 5.3 e 5.4 (PDF do boletim com hash: a biblioteca de PDF é escolhida aqui — decisão adiada na 4.3).
