# RN-08 — Boletim (Laudo) e Relatório de Ensaio

Legenda: Camada **B** banco · **S** serviço · **T** tela | Origem **L** legado · **C** correção · **N** nova · **D** decisão. [Catálogo](README.md)

| ID | Regra | Camada | Origem | Crit. | Onde |
|---|---|---|---|---|---|
| RN-08.01 | Boletim só para amostra (última revisão) com resultado; um boletim vigente por amostra | S | L | 🔴 | [P16](../processos/P16-montagem-boletim.md) |
| RN-08.02 | `COD_BOLETIM` = número base da amostra (sem " Rnn"), constante entre revisões | B | C | 🔴 | [`TG_BOLETIM_BI`](../banco/objetos/triggers/TG_BOLETIM_BI.md) |
| RN-08.03 | O resultado do boletim é o da amostra do boletim (última revisão do resultado) | B | C | 🔴 | [`TG_BOLETIM_BI`](../banco/objetos/triggers/TG_BOLETIM_BI.md) |
| RN-08.04 | Só parâmetros do resultado vinculado, cada um uma vez; seleção explícita pelo usuário | B T | L/C | 🔴 | [`TG_BOLPAR_BI`](../banco/objetos/triggers/TG_BOLPAR_BI.md) |
| RN-08.05 | Texto dos parâmetros congelado no boletim (o impresso é o gravado); "Atualizar textos" só antes da emissão | S | L | 🔴 | [P16](../processos/P16-montagem-boletim.md) |
| RN-08.06 | Ordem dos parâmetros definida pelo usuário (padrão: acreditados primeiro) | S T | N/L | ⚪ | [T27](../telas/T27-boletim.md) |
| RN-08.07 | Metodologias acreditadas e não acreditadas preenchidas a partir dos testes da amostra (editáveis) | B S | C | 🟠 | [`FN_OBS_METODOS`](../banco/objetos/rotinas/FN_OBS_METODOS.md) |
| RN-08.08 | Nº de acreditação impresso quando há parâmetro acreditado; parâmetros acreditados marcados | S | L | 🟠 | [`FN_ACREDITADO`](../banco/objetos/rotinas/FN_ACREDITADO.md) |
| RN-08.09 | Coluna de especificação impressa só se algum parâmetro tiver especificação | S | L | ⚪ | [P16](../processos/P16-montagem-boletim.md) |
| RN-08.10 | Responsáveis entre profissionais que assinam boletim; Responsável 1 obrigatório para emitir; 2º se a empresa exigir | B S | N | 🔴 | [`SP_EMITIR_BOLETIM`](../banco/objetos/rotinas/SP_EMITIR_BOLETIM.md) |
| RN-08.11 | Prévia com marca d'água e sem assinaturas | S | N | 🟠 | [P17](../processos/P17-emissao-boletim.md) |
| RN-08.12 | Emissão: PDF definitivo + SHA-256 + `SP_EMITIR_BOLETIM` (data, emissor, status emitido) | B S | C | 🔴 | [P17](../processos/P17-emissao-boletim.md) |
| RN-08.13 | **Boletim emitido é congelado**: só o status muda; não pode ser excluído; parâmetros, resultado e amostra também ficam bloqueados | B | C | 🔴 | [`TG_BOLETIM_BU`](../banco/objetos/triggers/TG_BOLETIM_BU.md) |
| RN-08.14 | Reimpressão de emitido usa o PDF gravado (conferir hash) | S | N | 🔴 | [P17](../processos/P17-emissao-boletim.md) |
| RN-08.15 | Correção só por revisão: motivo, só da última, nasce não emitida, resultado vigente, mesmos parâmetros | B | C | 🔴 | [P18](../processos/P18-revisao-boletim.md) |
| RN-08.16 | Desfazer revisão só da última e não emitida | B | N | 🟠 | [`SP_EXCLUIR_REVISAO_BOLETIM`](../banco/objetos/rotinas/SP_EXCLUIR_REVISAO_BOLETIM.md) |
| RN-08.17 | Boletins migrados ficam congelados (emitidos na data de geração) | B | D | 🔴 | [M-17](../processos/migracao/M-17-boletins.md) |
| RN-08.18 | Gerador = profissional do usuário; data do boletim padrão hoje | B S | L | ⚪ | [T27](../telas/T27-boletim.md) |
| RN-08.19 | Relatório de ensaio: número anual pelo banco; mesma lógica de emissão/congelamento do boletim (se mantido) | B | C | ⚪ | [T28](../telas/T28-relatorio-ensaio.md) |
| RN-08.20 | Envio por e-mail ao contato do resultado da proposta, anexando o PDF emitido | S | N | ⚪ | [P17](../processos/P17-emissao-boletim.md) |
