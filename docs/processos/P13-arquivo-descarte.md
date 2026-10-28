# P13 — Arquivo e Descarte de Amostras

**Criticidade:** ⚪ · **Fase:** 7
**Telas:** [T24 Localização](../telas/T24-localizacao-amostras.md), [T29 Pesquisa](../telas/T29-pesquisa-amostras.md)
**Objetos:** [`LAB_LOCAL_AMOSTRAS`](../banco/objetos/tabelas/LAB_LOCAL_AMOSTRAS.md), [`VW_LAB_AMOSTRAS`](../banco/objetos/views/VW_LAB_AMOSTRAS.md) (`SITUACAO_ARQUIVO`), [`AMOSTRAS_STATUS`](../banco/objetos/tabelas/AMOSTRAS_STATUS.md)
**Regras:** [RN-06](../regras/RN-06-estoque-arquivo.md)

## Fluxo
1. **Arquivar**: amostra (raiz) → status ARQUIVADA, data/hora do arquivo (padrão agora), armário/prateleira/caixa, observação. Uma localização por amostra física (`UQ_LOCAL_AMO`); se já existe, a tela abre para alterar.
2. **Arquivar em lote** (novo): seleção de várias amostras (ex.: todas as de uma caixa) → mesmos armário/prateleira/caixa e data para todas, numa transação.
3. **Descartar**: status DESCARTADA → data/hora do descarte (padrão agora) e profissional do descarte = profissional do usuário (o banco exige a data: `CK_LOCAL_DESC`). Pergunta opcional: mudar o status da amostra para "Reprovada – descartada".
4. **Desfazer descarte**: volta para ARQUIVADA e limpa data/profissional (o legado limpava a data mas deixava o status "descartada" — inconsistência) + `AUDITORIA`.
5. **Consultar**: por cliente, produto, amostra, status ou caixa.

## Regras
- Sempre gravar no ID da **raiz** (`COALESCE(ID_AMOSTRA_RAIZ, ID)`) — revisões compartilham a localização.
- Migração (M-15): `STATUS 0 → ARQUIVADA, 1 → DESCARTADA`; aponta para a raiz; se houver 2 registros para a mesma raiz, fica o mais recente (listar no relatório).

## Cenários de teste
- Descartar sem data → recusado (serviço; o banco também, `CK_LOCAL_DESC`).
- Localização de uma revisão grava/mostra a da raiz.
- Desfazer descarte → ARQUIVADA.

## Implementação (tarefa 7.2, 28/10/2026)
- T24 em `/Localizacao` (`LocalizacaoService`); detalhes em [T24 › Implementação](../telas/T24-localizacao-amostras.md#implementação-tarefa-72-28102026).
- **Raiz no serviço:** a `LAB_LOCAL_AMOSTRAS` não tem trigger; o serviço usa `ID_AMOSTRA_RAIZ ?? ID` antes de gravar.
- Datas não futuras; descarte não antes do arquivo; profissional do descarte = o do usuário (mantido quando a amostra já estava descartada); descarte por reprovação muda o status da última revisão para "REPROVADA - DESCARTADA" na mesma gravação.
- Lote numa transação; as amostras que já têm localização não são sobrescritas (listadas na mensagem).
- Testes: `LocalizacaoTests` + navegador.
