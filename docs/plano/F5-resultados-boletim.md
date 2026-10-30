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
> ✅ **Concluída em 27/10/2026** — ver [T27 › Implementação](../telas/T27-boletim.md#implementação-tarefa-53-27102026); aceite em `BoletinsTests` e no teste de navegador `Boletim_monta_reordena_visualiza_e_emite`.
> **Decidido em 27/10/2026:** biblioteca **QuestPDF** (dúvida 10); PDFs numa **pasta do servidor** por empresa/ano, com caminho + SHA-256 no banco (dúvida 6); layout a partir do `frxRboletin` do Delphi, validado com 3 boletins reais na 5.4 (dúvida 5).
- **Passos:** amostra com resultado; cabeçalho; responsáveis (assina boletim); seleção de parâmetros em dois grids com ordem arrastável; metodologias automáticas (`FN_OBS_METODOS`), observação, legenda; "atualizar textos"; **visualizar** (prévia com marca d'água); **emitir** (PDF definitivo + SHA-256 + `SP_EMITIR_BOLETIM`, apagar PDF se falhar); reimpressão do PDF gravado; **criar revisão** e **desfazer**; consulta.
- **Aceite:** [aceite da T27](../telas/T27-boletim.md#aceite) + tabela "depois de emitido" do [P17](../processos/P17-emissao-boletim.md).
- **Esforço:** G · **Depende de:** 5.2

### 5.4 Layout do PDF do boletim (QuestPDF) 🔵
> **30/10/2026 — layout refeito, aguardando a aprovação do usuário:** 6 boletins reais em `exemplos/Boletins` (fora do git). Layout do `frxRboletin` (ver [T27 › Layout do PDF](../telas/T27-boletim.md#layout-do-pdf-tarefa-54-30102026)); **selo da acreditação** por empresa (`EMPRESAS.SELO_ACREDITACAO`, D-28) só nas páginas dos acreditados; boletim misto em duas partes "A"/"B" como o legado. Falta: o usuário cadastrar o selo e o rodapé na T03 e aprovar.
> **Situação (27/10/2026):** a 5.3 já entrega o PDF com **todas as seções e os dados certos** — logo e nº de acreditação no cabeçalho (pendências 8 da T03 e 9 da T13 feitas), expoente sobrescrito, coluna de especificação só se houver, "(*)" nos acreditados, assinaturas só no emitido, 2ª via do migrado, prévia com marca d'água. Falta **o visual igual ao do laboratório**, que depende dos modelos reais (dúvida 5, pendência 25).
- **Antes de começar:** os **3 boletins reais** (PDF ou foto do papel) numa pasta **fora do repositório** (ex.: `C:\Project\Modelos\`; têm dados de clientes): um com resultado numérico com expoente, um descritivo, um com revisão ou dois responsáveis.
- **Como fazer:**
  1. Ler o layout do legado: o relatório `frxRboletin` está **dentro do `un_CadBoletim.dfm`** (`C:\Project\Laboratorio\LabControl`, objetos `TfrxMemoView` — posições, fontes, textos fixos e a ordem das bandas). Anotar no P16 os textos fixos que o laudo do legado imprime (títulos, frases legais) — **não inventar texto legal**: o que for da empresa vai em `EMPRESAS.CABECALHO_LAUDO`/`RODAPE_LAUDO` (T03).
  2. Para cada modelo, achar o boletim migrado correspondente (mesmo código) e gerar a 2ª via (`/Boletins/Imprimir/{id}`) no `labcontrol_migracao` recriado (pendência 26); comparar lado a lado.
  3. Ajustar só `Services/Boletins/BoletimPdf.cs` (o `BoletimDocumento` já tem os dados; se faltar algum, acrescentar no `BoletimService.DocumentoAsync`): margens, fontes/tamanhos, ordem dos blocos, larguras das colunas, rótulos, posição do logo e das assinaturas, quebra de página da tabela (cabeçalho repetido), rodapé.
  4. Gerar os PDFs de exemplo para o usuário aprovar: copiar a prévia e o emitido do `BoletinsTests` para o scratchpad (como feito na 5.3) ou pela tela; abrir e conferir.
  5. Se o usuário pedir uma mudança que altere o que já foi emitido: boletim emitido **nunca** é regerado (o arquivo gravado é o oficial); vale só para as próximas emissões e para a 2ª via dos migrados.
- **Aceite:** usuário aprova o layout com os 3 modelos.
- **Esforço:** M · **Depende de:** 5.3 e dos modelos do laboratório

### 5.5 Testes de aceite + revisão
> ✅ **Concluída em 27/10/2026** — pendência 23 (pedidos concluídos na migração), `AceiteFase5Tests` (cascata pela revisão da amostra; dois boletins para o mesmo resultado), `/code-review high` com 10 achados corrigidos (ver Diário); teste guiado com o laboratório na pendência 28. Fica aberta só a 5.4.
- **Como fazer:**
  1. **Pendência 23** primeiro (pedidos concluídos na migração): é a última peça da migração parte 3; rodar `MigracaoTests` depois.
  2. **`AceiteFase5Tests`** (como o `AceiteFase4Tests`): os cenários do P14–P18 que ainda não têm teste e a **cascata completa** do roteiro 12 §7 itens 6–9 pela aplicação — amostra → resultado → boletim emitido → revisão da **amostra** (gera resultado e boletim novos não emitidos, apontando para os novos) → editar → emitir de novo → desfazer onde permitido. Incluir: boletim migrado com 2 boletins para o mesmo resultado (120 casos) revisando sem erro (P18) e "Alterar a amostra com boletim emitido" recusado (P17).
  3. Teste manual guiado com o laboratório, como a pendência 22 da Fase 4: lançar resultados e emitir 3 boletins reais.
  4. **`/code-review high`** de `9159f42^..HEAD` (5.1–5.5); corrigir os achados ou registrar como pendência com dono.
  5. `/concluir-tarefa 5.5` — checklists, Definição de pronto abaixo, Diário, commit + push. Lembrar `/simplify` (fase grande).
- **Esforço:** M
## Definição de pronto
- [x] Boletim emitido imutável (banco) e reimpressão idêntica (hash). — 5.3 (`BoletinsTests`)
- [x] Revisões em cascata corretas (resultado e boletim na 5.2/5.3; a partir da amostra no `AceiteFase5Tests`, 5.5); histórico migrado congelado (5.1).
