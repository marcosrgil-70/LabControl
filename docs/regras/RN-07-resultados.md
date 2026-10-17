# RN-07 — Resultados

Legenda: Camada **B** banco · **S** serviço · **T** tela | Origem **L** legado · **C** correção · **N** nova · **D** decisão. [Catálogo](README.md)

| ID | Regra | Camada | Origem | Crit. | Onde |
|---|---|---|---|---|---|
| RN-07.01 | Só amostras da empresa, última revisão, com status que permite resultado | S | C | 🔴 | [P14](../processos/P14-lancamento-resultados.md) |
| RN-07.02 | Um resultado (cabeçalho) por revisão da amostra; "Incluir" abre o existente | B S | C | 🟠 | [`LAB_RESULTADOS_ANALISES`](../banco/objetos/tabelas/LAB_RESULTADOS_ANALISES.md) |
| RN-07.03 | Uma linha por teste; o teste precisa ser da amostra do resultado | B | C | 🔴 | [`TG_RESPAR_BI`](../banco/objetos/triggers/TG_RESPAR_BI.md) |
| RN-07.04 | Resultado numérico (símbolo + valor + expoente + unidade) **ou** descritivo (tipo de resultado) — exclusivos; idem para a especificação | B T | L | 🔴 | [P14](../processos/P14-lancamento-resultados.md) |
| RN-07.05 | Trocar o tipo do resultado troca o tipo da especificação (pode ser mudado depois) | T | L | ⚪ | [T26](../telas/T26-resultados.md) |
| RN-07.06 | Valor do resultado digitado com vírgula e validado como número | S T | L | 🟠 | [T26](../telas/T26-resultados.md) |
| RN-07.07 | Texto formatado (`< 1,0e2 UFC/g` / "Ausência em 25 g") calculado pelo banco; prévia na tela | B | C | 🔴 | [`FN_FORMATA_RESULTADO`](../banco/objetos/rotinas/FN_FORMATA_RESULTADO.md) |
| RN-07.08 | Data inicial ≥ data de entrega; data do resultado ≥ inicial e ≤ hoje | B S | N | 🟠 | [`LAB_RESULTADOS_PARAM`](../banco/objetos/tabelas/LAB_RESULTADOS_PARAM.md) |
| RN-07.09 | Analista entre profissionais que executam análise; digitador = profissional do usuário | S | L/N | 🟠 | [T26](../telas/T26-resultados.md) |
| RN-07.10 | Com boletim emitido o resultado não pode ser alterado (só por revisão) | B | C | 🔴 | [`TG_RESPAR_BU`](../banco/objetos/triggers/TG_RESPAR_BU.md) |
| RN-07.11 | Linha usada em boletim não pode ser excluída | B | N | 🟠 | [`TG_RESPAR_BD`](../banco/objetos/triggers/TG_RESPAR_BD.md) |
| RN-07.12 | Excluir todos os resultados: só revisão 0, sem boletim | B | L | 🟠 | [`TG_RESULT_BD`](../banco/objetos/triggers/TG_RESULT_BD.md) |
| RN-07.13 | Revisão: motivo, só da última, numeração contínua por amostra física, opção de revisar o boletim (apontando para o resultado novo) | B | C | 🔴 | [P15](../processos/P15-revisao-resultado.md) |
| RN-07.14 | Desfazer revisão: só a última e sem boletim | B | L/C | 🟠 | [`SP_EXCLUIR_REVISAO_RESULTADO`](../banco/objetos/rotinas/SP_EXCLUIR_REVISAO_RESULTADO.md) |
| RN-07.15 | Revisão antiga do resultado somente leitura | B | C | 🔴 | [`TG_RESULT_BU`](../banco/objetos/triggers/TG_RESULT_BU.md) |
| RN-07.16 | Fila de análises: testes sem resultado, ordenados pela data prevista, vencidos destacados | S T | N | ⚪ | [`VW_LAB_TESTES_PENDENTES`](../banco/objetos/views/VW_LAB_TESTES_PENDENTES.md) |
