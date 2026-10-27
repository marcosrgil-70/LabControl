# P17 — Emissão do Boletim (congelamento com PDF + hash)

**Criticidade:** 🔴🔴 (no legado nunca funcionou) · **Fase:** 5
**Telas:** [T27 Boletim](../telas/T27-boletim.md), [T13 Profissionais](../telas/T13-profissionais.md) (assinatura), [T05 Parâmetros](../telas/T05-parametros-empresa.md)
**Objetos:** [`SP_EMITIR_BOLETIM`](../banco/objetos/rotinas/SP_EMITIR_BOLETIM.md), [`LAB_BOLETIM_ANALISES`](../banco/objetos/tabelas/LAB_BOLETIM_ANALISES.md) (`DH_EMISSAO`, `ID_ENTIDADES_FUNC_EMI`, `ARQUIVO_PDF`, `HASH_PDF`), [`TG_BOLETIM_BU`](../banco/objetos/triggers/TG_BOLETIM_BU.md), [`TG_BOLETIM_BD`](../banco/objetos/triggers/TG_BOLETIM_BD.md), [`TG_BOLPAR_BI`](../banco/objetos/triggers/TG_BOLPAR_BI.md)/[`BU`](../banco/objetos/triggers/TG_BOLPAR_BU.md)/[`BD`](../banco/objetos/triggers/TG_BOLPAR_BD.md), [`TG_RESPAR_BU`](../banco/objetos/triggers/TG_RESPAR_BU.md), [`TG_AMOSTRAS_BU`](../banco/objetos/triggers/TG_AMOSTRAS_BU.md), [`ENTIDADES_FUNCIONARIOS`](../banco/objetos/tabelas/ENTIDADES_FUNCIONARIOS.md)
**Regras:** [RN-08](../regras/RN-08-boletim.md) · **Decisão:** D-11

## Problema do legado
`LAB_BOLETIM_ANALISES_BU0/BD0` testavam `OLD.DH_EMISSAO > 0` (timestamp × inteiro) e **nenhum** dos 5.312 boletins tinha `DH_EMISSAO`: a exceção `ERR_BOLETIM_EMITIDO` nunca disparou — qualquer laudo enviado podia ser alterado.

## Fluxo
1. Usuário com `BOLETINS.EMITIR` clica **Emitir** na última revisão, não emitida.
2. Serviço valida antes (mesmas regras da procedure, para mensagem rápida): Responsável 1 (e 2 se exigido), ≥ 1 parâmetro, profissional do usuário.
3. Gera o **PDF definitivo** (QuestPDF): logo, rodapé, assinaturas dos responsáveis (imagem de `ENTIDADES_FUNCIONARIOS.ASSINATURA`), sem marca d'água.
4. Salva em `{Boletins:Pasta}/{empresa}/{ano}/{COD_BOLETIM com "/"→"-"} R{rev}.pdf` (padrão `storage/boletins`) (pasta no servidor, decisão do usuário de 27/10/2026 — dúvida 6) e calcula **SHA-256**.
5. `CALL SP_EMITIR_BOLETIM(id, profissional, arquivo, hash)` fora de transação EF → grava `DH_EMISSAO = NOW()`, emissor, status "emitido" (`ID_BOLETINS_STATUS_EMITIDO`), caminho e hash.
6. Se a procedure falhar: apagar o PDF gerado (não deixar arquivo órfão).
7. `AUDITORIA` ação M; pedido de venda: verificar conclusão automática ([P08](P08-pedido-venda.md)).

## Depois de emitido (garantido pelo banco)
| Tentativa | Resultado |
|---|---|
| Alterar qualquer campo do boletim (exceto status) | "Este boletim foi emitido. Não é permitido alterar ou excluir." |
| Excluir o boletim | idem |
| Incluir/alterar/excluir parâmetro do boletim | idem |
| Alterar resultado vinculado | "Resultado com boletim emitido: gere uma revisão do resultado." |
| Alterar a amostra | "Amostra com boletim emitido: gere uma revisão da amostra." |
- **Reimpressão** usa o arquivo gravado (conferir o hash; divergência = alerta de integridade).
- Correção: só por revisão ([P18](P18-revisao-boletim.md)).
- Envio por e-mail ao contato do resultado da proposta (fase 8; dúvida 2 do plano) anexa o PDF gravado.

## Migração
Todo boletim migrado recebe `DH_EMISSAO = DH_GERACAO` (histórico congelado); sem PDF (`ARQUIVO_PDF` nulo) — reimpressão dos antigos gera o PDF a partir dos dados gravados, com a indicação "2ª via — boletim migrado".

## Cenários de teste
- Emitir sem Responsável 1 → "Informe o(s) responsável(is) técnico(s) do boletim."
- Emitir sem parâmetros → "Boletim sem parâmetros selecionados."
- Emitir duas vezes → "Boletim já emitido."
- Depois de emitido, todas as tentativas da tabela acima recusadas.
- Reimpressão = arquivo idêntico (hash igual).

## Implementação (tarefa 5.3, 27/10/2026)
- `BoletimService.EmitirAsync`: passos 1–6 acima. Arquivo em `Boletins:Pasta` (padrão `storage/boletins`, fora do git) como `{empresa}/{ano}/{código com "/"→"-"} R{rev}.pdf`; caminho **relativo** no banco (a pasta pode mudar de lugar).
- **Concorrência (`/code-review` 5.5):** o PDF é gravado primeiro com um **nome provisório único** (`… R00.pdf.{guid}.tmp`) e só vira o definitivo (`ArmazemBoletins.Confirmar`) depois que o `SP_EMITIR_BOLETIM` aceita — um duplo clique ou dois usuários emitindo ao mesmo tempo não sobrescrevem nem apagam o arquivo de quem venceu. Antes de chamar a procedure, o conteúdo do laudo é relido e comparado com o que entrou no PDF: se outro usuário alterou o boletim nesse meio-tempo, a emissão é recusada ("O boletim foi alterado por outro usuário enquanto era emitido").
- Falha na procedure (de negócio ou técnica) apaga o provisório antes de devolver/propagar o erro.
- Depois de emitido, a tela só deixa mudar o **status** ("Gravar status"), única coisa que o `TG_BOLETIM_BU` permite.
- `ImprimirAsync`: lê o arquivo e confere o SHA-256; divergente, ausente ou **ilegível** (pasta fora do ar, sem permissão, caminho fora da pasta) → **"ALERTA DE INTEGRIDADE"** na tela + log de erro, nunca página de erro. Boletim migrado (sem arquivo) → 2ª via gerada dos dados, sem gravar.
- **Passo 7 (pedido de venda concluído na emissão) não foi feito:** pendência 24 (6.2). Na migração foi feito na 5.5 (pendência 23).
- Cenários acima em `BoletinsTests`. **Status:** ✅ (menos o passo 7)
