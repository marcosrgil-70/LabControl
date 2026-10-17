# P14 — Lançamento de Resultados

**Criticidade:** 🔴🔴 (o que vai para o laudo) · **Fase:** 5
**Telas:** [T26 Resultados](../telas/T26-resultados.md), [T27 Boletim](../telas/T27-boletim.md), [T01 Painel](../telas/T01-layout-menu.md)
**Objetos:** [`LAB_RESULTADOS_ANALISES`](../banco/objetos/tabelas/LAB_RESULTADOS_ANALISES.md), [`LAB_RESULTADOS_PARAM`](../banco/objetos/tabelas/LAB_RESULTADOS_PARAM.md), [`LAB_HIST_AMOSTRAS_TESTES`](../banco/objetos/tabelas/LAB_HIST_AMOSTRAS_TESTES.md), [`VW_LAB_TESTES_PENDENTES`](../banco/objetos/views/VW_LAB_TESTES_PENDENTES.md), [`VW_LAB_RESULTADOS`](../banco/objetos/views/VW_LAB_RESULTADOS.md), [`FN_FORMATA_RESULTADO`](../banco/objetos/rotinas/FN_FORMATA_RESULTADO.md), [`TG_RESULT_BI`](../banco/objetos/triggers/TG_RESULT_BI.md), [`TG_RESPAR_BI`](../banco/objetos/triggers/TG_RESPAR_BI.md), [`TG_RESPAR_BU`](../banco/objetos/triggers/TG_RESPAR_BU.md), [`TG_RESPAR_BD`](../banco/objetos/triggers/TG_RESPAR_BD.md)
**Regras:** [RN-07](../regras/RN-07-resultados.md)

## Seleção da amostra
Amostras da empresa ativa, **última revisão**, com status que permite resultado (`AMOSTRAS_STATUS.PERMITE_RESULTADO`), entregues nos últimos 400 dias (como o legado, ajustável); ou a partir da **fila de análises** (`VW_LAB_TESTES_PENDENTES`, ordenada por data prevista, vencidos em vermelho). O legado não filtrava a empresa aqui.

## Estrutura
- Um **cabeçalho** por revisão da amostra (`UQ_RES_REV`); se já existir, "Incluir" abre o existente.
- Uma **linha por teste** (`UQ_RESPAR_TESTE`); o teste precisa ser da amostra do cabeçalho.

## Campos de cada linha e habilitação (Delphi `cds_ResultadosTIPO_RESULTADOChange`)
| Grupo | Numérico (`TIPO_RESULTADO = 1`, padrão) | Descritivo (`= 0`) |
|---|---|---|
| Resultado | símbolo (`<`, `>`, `≤`, `≥`, `~` ou vazio) + valor (texto com vírgula, validado como número) + expoente "× 10" + unidade | tipo de resultado (`LAB_TIPOS_RESULTADOS`) |
| Especificação | idem com `*_ESPECIF` | tipo de resultado da especificação |
- Trocar o tipo do resultado **troca o tipo da especificação junto** (regra do legado), mas pode ser mudado depois.
- O banco limpa os campos incompatíveis (numérico × descritivo) e transforma 0 em NULL nos IDs.
- Datas: `DT_INICIAL` ≥ data de entrega; `DT_RESULTADO` ≥ inicial (CHECK) e ≤ hoje.
- Analista: profissionais com `EXECUTA_ANALISE`; quantidade analisada + unidade; "Conforme?" (novo).

## Formatação (fonte única: `FN_FORMATA_RESULTADO`, gravada pela trigger)
| Entrada | `RESULTADO_FORMATADO` | Impressão no boletim |
|---|---|---|
| `<`, `1,0`, exp 2, UFC/g | `< 1,0e2 UFC/g` | `< 1,0 × 10² UFC/g` |
| `3,5`, exp 0, mg/L | `3,5 mg/L` | `3,5 mg/L` |
| descritivo "Ausência em 25 g" | `Ausência em 25 g` | idem |
A tela mostra a **prévia** com o texto relido do banco após gravar.

## Fluxo
1. Escolher a amostra → mostra nome, tipo de amostra/análise, revisão.
2. "Lançar todos" cria uma linha por teste pendente (padrão novo) ou incluir um a um.
3. Gravar cabeçalho + linhas numa transação (digitador = profissional do usuário).
4. Alterar: bloqueado em revisão antiga ou com boletim emitido ("Resultado com boletim emitido: gere uma revisão do resultado.") → [P15](P15-revisao-resultado.md).
5. Excluir linha: bloqueado se a linha estiver num boletim. Excluir todos os resultados: só revisão 0 sem boletim (*"Deseja excluir todos os resultados desta Amostra?"*).

## Cenários de teste
- Numérico `<` `1,0` exp 2 UFC/g ⇒ `< 1,0e2 UFC/g` (roteiro doc 12 §7 item 6).
- Descritivo ⇒ texto do tipo; campos numéricos limpos.
- Teste de outra amostra → "O parâmetro não pertence à amostra do resultado."; resultado duplicado para o mesmo teste → violação de `UQ_RESPAR_TESTE` (mensagem amigável).
- Valor "3,5,1" ou "abc" recusado na tela.
