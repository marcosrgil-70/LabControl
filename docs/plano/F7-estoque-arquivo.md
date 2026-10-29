# Fase 7 — Estoque e Arquivo de Amostras

**Objetivo:** movimentação manual com saldo protegido e localização/descarte das amostras.
**Modelo/modo:** 🟢 Sonnet · ⚡ accept edits depois da primeira tela aprovada.
**Ler antes:** [P12](../processos/P12-estoque-amostras.md) · [P13](../processos/P13-arquivo-descarte.md) · [RN-06](../regras/RN-06-estoque-arquivo.md) · telas [T23](../telas/T23-movimentacao-amostras.md), [T24](../telas/T24-localizacao-amostras.md)
**Depende de:** Fase 4.

---

### 7.1 T23 — Movimentação ✅
- **Passos:** busca por cliente/número (última revisão); painel da amostra com saldo; nova movimentação (entrada complementar/saída, justificativa, parâmetros atendidos com indicação dos já atendidos); histórico; excluir só manual.
- **Aceite:** [aceite da T23](../telas/T23-movimentacao-amostras.md#critérios-de-aceite).
- **Esforço:** M
- **Feito em 28/10/2026** — ver [T23 › Implementação](../telas/T23-movimentacao-amostras.md#implementação-tarefa-71-28102026).

### 7.2 T24 — Localização ✅
- **Passos:** consulta com um filtro só; formulário arquivo/descarte; desfazer descarte (volta para ARQUIVADA); arquivar em lote; pesquisa (T29).
- **Aceite:** [aceite da T24](../telas/T24-localizacao-amostras.md#critérios-de-aceite).
- **Esforço:** M
- **Feito em 28/10/2026** — ver [T24 › Implementação](../telas/T24-localizacao-amostras.md#implementação-tarefa-72-28102026). Antes, com aprovação do usuário: pendências 31 (trava do saldo no `TG_MOV_BI`/`TG_MOV_BD`) e 32 (RECEPÇÃO com `LOCALIZACAO.*`) — D-25, roteiro 74/74.
- **Antes de começar:** commit + push do pendente; decidir com o usuário a pendência 32 (perfil RECEPÇÃO) e, se ainda não decidida, a 31.
- **Como fazer** (mesmo desenho da T23 — `MovimentacaoController`/`MovimentacaoService` servem de modelo):
  1. **Serviço `LocalizacaoService`:**
     - consulta pela `VW_LAB_AMOSTRAS` (última revisão), com `SITUACAO_ARQUIVO` e os dados da `LAB_LOCAL_AMOSTRAS` (armário/prateleira/caixa, datas, profissional do descarte); basta **um** filtro (cliente, produto, nº, status Arquivada/Descartada/sem localização, período do arquivo, armário/prateleira/caixa); 25 por página;
     - **raiz no serviço:** não existe trigger na `LAB_LOCAL_AMOSTRAS` (ao contrário do `TG_MOV_BI`) → o serviço usa `IdAmostraRaiz ?? Id` antes de gravar (ou, com aprovação, uma trigger nova `TG_LOCAL_BI/BU` que redirecione e herde a empresa — opção mais segura, mudança no banco);
     - incluir: se a raiz já tem localização (`UQ_LOCAL_AMO`), abrir a existente para alterar em vez de dar erro;
     - descartar: `STATUS = DESCARTADA`, `DT_HR_DESCARTE` (padrão agora, nunca futura) e `ID_ENTIDADES_FUNC_DESCARTE` = profissional do usuário (sem vínculo → recusa com mensagem, como a T23); o banco recusa sem data (`CK_LOCAL_DESC`) — dar mensagem própria antes de chegar ao banco;
     - pergunta opcional ao descartar: mudar o status da amostra para **"REPROVADA - DESCARTADA"** (status criado pelo `SP_INICIALIZAR_EMPRESA`; procurar pela descrição na empresa ativa);
     - desfazer descarte: volta para `ARQUIVADA` e limpa data e profissional (corrige o legado); a auditoria é automática pelo EF (`LAB_LOCAL_AMOSTRAS` já é tabela crítica);
     - **arquivar em lote** (novo): várias amostras (marcadas na lista "sem localização" ou pela T29 com `ApenasSemLocalizacao`) + armário/prateleira/caixa/data uma vez só, **numa transação**; amostra que já tem localização: recomendação é listar como recusada e não sobrescrever;
     - excluir, com confirmação.
  2. **`LocalizacaoController`** com `[Permissao("LOCALIZACAO.*")]` em toda action (o `AutorizacaoActionsTests` confere); o item do menu já existe em `Menu.cs` e aparece sozinho quando a action existir.
  3. **Views** `Index` (consulta + lote) e `Form` (grupos Amostra / Arquivo / Descarte), no padrão das listas: botões com texto, `data-confirmar` com texto.
  4. **Testes:** `LocalizacaoTests` (descartar sem data recusado; revisão grava na raiz e mostra a mesma; desfazer volta para ARQUIVADA e limpa; lote numa transação; empresa ativa) + um teste de navegador em `CrudNavegadorTelasTests`; suíte inteira com o sistema fechado.
  5. **Docs:** T24 (Implementação + Status), P13, RN-06 (06.11 a 06.14), matriz, plano (checklist, ponto de retorno, Diário).
- **Dados reais para conferir:** o `labcontrol_migracao` tem as localizações migradas do legado (M-15); pesquisar com "Entrega de" 01/01/2013.

### 7.3 Revisão ✅
- `/code-review` nível high (como nas Fases 5 e 6) de `3e7e3ba..HEAD` (7.1 + 7.2); corrigir os achados ou registrá-los como pendência.
- Suíte inteira (`dotnet test tests/LabControl.Tests`, ~8 min) com o sistema fechado.
- Conferir a definição de pronto abaixo e marcar a Fase 7 como completa no plano.
- **Feito em 28/10/2026:** 10 achados, 9 corrigidos e 1 sem mudança — lista em [T23 › Revisão](../telas/T23-movimentacao-amostras.md#revisão-tarefa-73-28102026); suíte 275/276 (o teste da migração não rodou: serviço do Firebird parado — pendência 35); pendência 34 nova (status da amostra com boletim emitido no descarte).

## Definição de pronto
- [x] Saldo nunca negativo — pela aplicação (7.1) e pelo próprio banco (pendência 31, D-25).
- [x] Movimentos sempre na raiz (`TG_MOV_BI`, 7.1).
- [x] Localização sempre na raiz (7.2, no serviço).
- [x] `/code-review` da Fase 7 sem achado aberto (7.3).
