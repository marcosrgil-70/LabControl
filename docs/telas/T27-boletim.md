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
| Amostra* | `ID_LAB_HIST_AMOSTRAS` | amostras da empresa, **última revisão**, **com resultado** (última revisão do resultado), do período padrão de pesquisa da empresa (o legado usava 400 dias — pendência 33); colunas Nº amostra, Produto, Lote |
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
2. **"Emitir"** (`BOLETINS.EMITIR`): gera o PDF definitivo (com logo, rodapé e assinaturas), salva em `{Boletins:Pasta}/{empresa}/{ano}/{COD_BOLETIM com "/"→"-"} R{rev}.pdf` (padrão `storage/boletins`), calcula SHA-256 e chama `SP_EMITIR_BOLETIM(id, profissional, arquivo, hash)`.
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
- [x] Só parâmetros marcados vão para o boletim, na ordem escolhida. — `BoletinsTests.Monta_so_os_marcados…` + navegador `Boletim_monta_reordena_visualiza_e_emite`
- [x] Metodologias acreditadas/não acreditadas preenchidas automaticamente. — mesmo teste (`CONT. TOTAL: ISO 4833` / `SALMONELLA: ISO 6579`)
- [x] Emitir exige Responsável 1 e gera PDF com hash; depois disso nada é alterável. — `BoletinsTests.Emite_com_pdf_e_hash…` (parâmetros, exclusão, resultado vinculado e UPDATE direto recusados; só o status muda)
- [x] Revisão nasce não emitida, com o resultado vigente e os mesmos parâmetros. — `BoletinsTests.Revisao_nasce_nao_emitida…`
- [x] Impressão de boletim emitido reproduz exatamente o PDF gravado. — `BoletinsTests.Emite_com_pdf_e_hash…` (byte a byte; adulterado ou ausente → alerta de integridade)

## Implementação (tarefa 5.3, 27/10/2026)
- **Decisões do usuário (27/10/2026):** QuestPDF (dúvida 10); PDFs numa pasta do servidor por empresa/ano (dúvida 6); layout a partir do `frxRboletin`, validado com boletins reais na 5.4 (dúvida 5).
- **Arquivos:** `Controllers/BoletinsController.cs`, `Services/BoletimService.cs`, `Services/Boletins/BoletimPdf.cs` (`BoletimDocumento` + layout QuestPDF; licença Community definida aqui), `Infra/ArmazemBoletins.cs`, `ViewModels/BoletimVM.cs`, `Views/Boletins/Index.cshtml`, `Form.cshtml`, `_Disponiveis.cshtml`, `wwwroot/js/boletins.js`. Pacote `QuestPDF 2026.9.1`.
- **Consulta** (`VW_LAB_BOLETINS`: cliente, amostra/boletim, a partir de, emitido sim/não, só última revisão). **Novo boletim** pelo componente T29 (`data-url="/Boletins/Pesquisa"`, permissão `BOLETINS.CONSULTAR`, só última revisão com resultado, período padrão da empresa — pendência 33); `/Boletins/Amostra/{id}` abre o boletim vigente da amostra física, se existe.
- **Montagem:** na inclusão, cabeçalho + parâmetros marcados numa transação (gerador = profissional do usuário; código pela trigger). Depois: "Concluir seleção" (grid da esquerda = `VW_LAB_RESULTADOS` do resultado vinculado, fora do boletim), ▲/▼ (ordem renumerada 1..n; novos no fim, acreditados primeiro), "Retirar", "Atualizar textos". Texto congelado com a mesma regra do `PR_REVISAO_BOLETIM`. Textos sugeridos: observação da verificação, `FN_OBS_METODOS(amostra, 1/0)`, `TEXTO_PADRAO_LEGENDA`.
- **Responsáveis:** profissionais com `ASSINA_BOLETIM`; diferentes entre si; 2º exigido para emitir se `EXIGE_RESP2_BOLETIM`.
- **PDF:** cabeçalho da empresa (logo, CNPJ, endereço, `CABECALHO_LAUDO`, nº de acreditação se `FN_ACREDITADO ≥ 1`), boletim nº + revisão, cliente, endereço do laudo, contato, proposta, pedido, amostra (lote, fabricação/validade parciais, recebimento, coletor/coleta, observação), período de análise, tabela na ordem (expoente sobrescrito; a unidade sai da coluna do resultado para a própria; especificação só se houver; acreditados com "(*)"), metodologias, observações, legenda, cidade e data, responsáveis com assinatura (**só no emitido**), rodapé (`RODAPE_LAUDO`, código e página). Imagem inválida (logo/assinatura) não derruba a emissão: sai sem ela e vai para o log.
- **Prévia** ("Visualizar"): marca d'água "PRÉVIA — NÃO VÁLIDO", sem assinaturas, nada gravado.
- **Emissão:** valida (responsáveis, parâmetros, profissional) → PDF definitivo → arquivo em `Boletins:Pasta` (`storage/boletins`, fora do git) como `{empresa}/{ano}/{código} R{rev}.pdf` → SHA-256 → `SP_EMITIR_BOLETIM`; se a procedure falhar, o arquivo é apagado.
- **Depois de emitido:** só o status muda ("Gravar status"); "Imprimir" devolve o arquivo gravado conferido pelo hash (divergente ou ausente → **alerta de integridade** + log); boletim migrado (sem arquivo) imprime "2ª via" gerada dos dados, sem gravar.
- **Revisão / desfazer** pelas procedures (motivo obrigatório; desfazer só não emitida).
- **Depois do `/code-review` (5.5):** emissão com arquivo provisório + conferência do conteúdo antes da procedure (P17 › Implementação); pasta configurada com barra no fim aceita; reimpressão ilegível vira alerta de integridade; responsáveis precisam estar **ativos**; parâmetros do boletim lidos numa consulta só.
- **Testes:** `BoletinsTests` (8) e `AceiteFase5Tests` (cascata pela revisão da amostra; dois boletins para o mesmo resultado) e navegador `Boletim_monta_reordena_visualiza_e_emite`.
- **Para a 5.4:** comparar com 3 boletins reais (dúvida 5) e ajustar o visual; o rodapé não tem frase fixa — o texto legal é o `RODAPE_LAUDO` da empresa.

## Envio por e-mail (tarefa 8.3, 29/10/2026)
- Botão **"Enviar por e-mail"** no boletim emitido (`BOLETINS.IMPRIMIR` — quem pode imprimir pode enviar) → `/Boletins/Email/{id}`: "Para" sugerido = e-mail do **contato do resultado da proposta**, "Cópia" opcional, assunto e texto do modelo da empresa (T05 › E-mail) com os marcadores preenchidos; vários endereços separados por ponto e vírgula (até 10).
- Anexo = **o PDF gravado na emissão, conferido pelo SHA-256** (o mesmo do "Imprimir"; alerta de integridade não envia); boletim migrado vai como 2ª via. Cópia oculta automática se configurada.
- Cada envio que deu certo vira um evento `N` em `AUDITORIA` (destinatários, assunto, anexo, servidor); a tela de envio lista o histórico. Sem configuração: "O envio de e-mail não está configurado nesta empresa: preencha Parâmetros da Empresa › E-mail."
- O status do boletim **não** muda sozinho ao enviar (continua pelo combo de status).
- Arquivos: `Services/Email/EnvioBoletimService.cs`, `Infra/EnvioEmail.cs` (MailKit; `IEnvioEmail` trocado por um falso nos testes), `Views/Boletins/Email.cshtml`. Teste: `EmailTests.Boletim_emitido_vai_com_o_pdf_gravado_e_o_envio_fica_no_historico` + navegador.

**Status:** ✅ concluída na tarefa 5.3 (27/10/2026); envio por e-mail na 8.3 (29/10/2026); layout validado com boletins reais na 5.4.
