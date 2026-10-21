# Fase 4 — Recebimento de Amostras (fase mais crítica)

**Objetivo:** receber amostras com numeração segura (ano do servidor), testes da proposta, estoque automático, comprovante, etiquetas e revisão — sobre propostas e amostras reais migradas.
**Modelo/modo:** 🔵 Opus · 📐 plan mode antes de 4.1 e 4.3 · ✋ modo padrão.
**Ler antes:** [P04](../processos/P04-numero-amostra-ano.md) · [P09](../processos/P09-recebimento-amostra.md) · [P10](../processos/P10-datas-parciais.md) · [P11](../processos/P11-revisao-amostra.md) · [P12](../processos/P12-estoque-amostras.md) · [P19](../processos/P19-etiquetas.md) · [RN-05](../regras/RN-05-amostras.md) · telas [T20](../telas/T20-propostas.md) (parte essencial), [T22](../telas/T22-recebimento-amostras.md), [T25](../telas/T25-etiquetas-amostras.md), [T29](../telas/T29-pesquisa-amostras.md) · objetos [`LAB_HIST_AMOSTRAS`](../banco/objetos/tabelas/LAB_HIST_AMOSTRAS.md), [`TG_AMOSTRAS_BI`](../banco/objetos/triggers/TG_AMOSTRAS_BI.md), [`SP_AMOSTRA_GERAR_TESTES`](../banco/objetos/rotinas/SP_AMOSTRA_GERAR_TESTES.md)
**Depende de:** Fase 3.

---

### 4.1 Migração parte 2 (M-11 a M-15) 🔵
- **Passos:** propostas (+ textos RTF→HTML), produtos/análises (valores copiados), pedidos de venda (1 por proposta), amostras (`NR_AMOSTRA` conferido com o legado **em todas**), testes (`ID_TESTE_ORIGEM`), estoque (descartar movimentos de revisões, recalcular saldo), localização, log.
- **Aceite:** conferências do [doc 13 §3](../banco/13-MIGRACAO-DADOS.md) para estas etapas: 2.358 / 5.761 / 19.252; ≈1.435 pedidos; 5.770 amostras com números idênticos; 20.551 testes; amostras por ano 62/611/511/663/2.482/1.440 (+1).
- **Esforço:** G

### 4.2 T20 — Proposta essencial 🟢
> Pendência 3 (2.4): combos de **método, idioma e prazo** com o "+" — `lc-rapido="ANALISES_METODOS"`, `"IDIOMAS"`, `"PRAZOS"` ([T10-00](../telas/T10-00-crud-auxiliares.md#implementação-tarefa-24--cadastro-rápido-)); **produto** pelo `POST /Produtos/Rapido` (T14, 3.3).
- **Passos:** consulta (filtros, só última revisão), inclusão/alteração **sem** revisão/PDF: cabeçalho, comercial, cliente/contato, painéis de endereço do laudo e da cobrança (com sugestões por finalidade/flags), produtos (item automático, qtde), análises (parâmetro → método filtrado, idioma, prazo, tipo de documento, valores com recálculo na tela e no banco), textos, totais; mensagens obrigatórias do legado.
- **Aceite:** roteiro de valores do [P06](../processos/P06-valores-proposta.md) (300,00 − 10% = 270,00); proposta nova recebe `000001/26-R0` do banco.
- **Esforço:** G · **Depende de:** 4.1

### 4.3 T22 — Recebimento de amostras completo 🔵
- **Passos:** seguir o fluxo do [P09](../processos/P09-recebimento-amostra.md) passo a passo: cadeia cliente → proposta → item → tipos; prévia do número; datas (futura, retroativa com confirmação + permissão + `@LC_PERMITE_RETROATIVO`, aviso de ano diferente); datas parciais ([P10](../processos/P10-datas-parciais.md)); gravação em transação com `SP_AMOSTRA_GERAR_TESTES`; aba testes (só leitura); observação rich text; consulta com filtros e "só última revisão"; alteração com travas; exclusão; **comprovante PDF**; **gerar revisão** (`SP_GERAR_REVISAO_AMOSTRA`, motivo); aba histórico (log).
- **Aceite:** todos os itens do [aceite da T22](../telas/T22-recebimento-amostras.md#aceite) + tabela de cenários do [P04](../processos/P04-numero-amostra-ano.md#cenários-de-teste-aceite), incluindo teste automatizado de duas inclusões simultâneas.
- **Esforço:** G · **Depende de:** 4.2

### 4.4 T29 — Pesquisa de amostras (componente) 🟢
- **Passos:** modal reutilizável com filtros e devolução `{id, nrAmostra}`.
- **Aceite:** usado pela T22 (e depois T24/T26/T27); nunca mostra outra empresa.
- **Esforço:** S · **Depende de:** 4.3

### 4.5 T25 — Etiquetas 🟢
- **Passos:** confirmar com o usuário tamanho/impressora/código de barras (dúvida 3); filtros, grid com quantidade, opção sem produto; saída PDF (e EPL2/ZPL se pedido); chamada direta após gravar a amostra.
- **Aceite:** [aceite da T25](../telas/T25-etiquetas-amostras.md#critérios-de-aceite) com leitura real pelo leitor do laboratório.
- **Esforço:** M · **Depende de:** 4.3

### 4.6 Testes de aceite + revisão
- Testes automatizados (xUnit) dos cenários do P04/P09/P10/P11 (via serviço); teste manual guiado com um usuário do laboratório recebendo 5 amostras reais.
- `/code-review high` sobre a fase.

## Definição de pronto
- [ ] Recebimento idêntico ao Delphi nas regras mantidas e com todas as correções (número/ano, revisão, estoque).
- [ ] Números das amostras migradas idênticos ao legado.
- [ ] Etiquetas lidas pelo leitor.
