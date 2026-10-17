# T26 — Inclusão de Resultados ⚠️ (tela com particularidades)

**Fase:** 5 · **Rota:** `/Resultados` · **Permissões:** `RESULTADOS.CONSULTAR/INCLUIR/ALTERAR/EXCLUIR/REVISAR/DESFAZER_REVISAO`
**Legado:** `un_Resultados.pas` + `unDM_Resultados.pas` (grid `TDBCtrlGrid` com um "cartão" por parâmetro).
**Tabelas:** `LAB_RESULTADOS_ANALISES` (cabeçalho: amostra + revisão), `LAB_RESULTADOS_PARAM` (1 linha por teste), `LAB_HIST_AMOSTRAS_TESTES`, `UNIDADES`, `LAB_TIPOS_RESULTADOS`; views `VW_LAB_RESULTADOS`, `VW_LAB_TESTES_PENDENTES`; rotinas `SP_GERAR_REVISAO_RESULTADO`, `SP_EXCLUIR_REVISAO_RESULTADO`; triggers `TG_RESULT_*`, `TG_RESPAR_*`.

## 1. Cabeçalho (grupo "Amostra")
| Campo | Regra |
|---|---|
| Nº da amostra* | só amostras da empresa, **última revisão**, com status que **permite resultado** (`AMOSTRAS_STATUS.PERMITE_RESULTADO`; legado: `EMPRESAS_PARAMETROS.ID_AMOSTRAS_STATUS_RESULTADOS`) e entregues nos últimos 400 dias (como o legado; filtro ajustável). Colunas: amostra (com R), produto, tipo de análise. **O legado não filtrava a empresa aqui.** |
| Nome da amostra, Tipo da amostra, Tipo de análise | só leitura |
| Revisão | só leitura |

Uma amostra tem **um** resultado por revisão (`UNIQUE (amostra, revisão)`). Se já existir, "Incluir" abre o existente.

## 2. Resultado por parâmetro (cada teste da amostra) — **particularidade**
Botão "Lançar todos" cria uma linha para cada teste pendente (padrão do novo sistema) ou incluir um a um (como o Delphi).

| Campo (rótulo) | Coluna | Regra |
|---|---|---|
| Parâmetro* | `ID_LAB_HIST_AMOSTRAS_TESTES` | testes **desta** amostra (o banco recusa teste de outra amostra); um resultado por teste |
| Responsável (analista) | `ID_ENTIDADES_FUNC_ANALISE` | profissionais com `EXECUTA_ANALISE` (colunas Nome, Registro, Nº registro) |
| Data início* | `DT_INICIAL` | ≥ data de entrega da amostra |
| Data resultado* | `DT_RESULTADO` | ≥ data início; ≤ hoje |
| Qtde. amostra / Unidade | `QT_AMOSTRA`, `ID_UNIDADES_AMOSTRA` | quantidade de amostra analisada (ex.: 25 g) |
| **Resultado numérico?** (checkbox) | `TIPO_RESULTADO` 1/0 | padrão **marcado** |
| — se numérico: Símbolo | `SIMBOLO_GRANDEZA` | `<`, `>`, `≤`, `≥`, `~` ou vazio (2 caracteres) |
| — Valor do resultado | `VR_RESULTADO` | **texto com vírgula** (ex.: `3,5`), validado como número |
| — "× 10" expoente | `VR_EXP_RESULTADO` | inteiro (ex.: `4` → 3,5 × 10⁴); exibido sobrescrito |
| — Unidade | `ID_UNIDADES` | ex.: UFC/g |
| — se descritivo: Tipo de resultado | `ID_LAB_TIPOS_RESULTADOS` | ex.: "Ausência em 25 g" (+ cadastro rápido, tecla Insert no legado) |
| **Especificação numérica?** | `TIPO_RESULTADO_ESPECIF` | **acompanha o tipo do resultado ao trocar** (regra do legado), mas pode ser mudada |
| — Símbolo/Valor/× 10/Unidade da especificação | `SIMBOLO_GR_ESPECIF`, `VR_RESULTADO_ESPECIF`, `VR_EXP_ESPECIF`, `ID_UNIDADES_ESPECIF` | limite (ex.: `≤ 1,0 × 10³ UFC/g`) |
| — Resultado da especificação (descritivo) | `ID_LAB_TIPO_RESULTADOS_ESP` | ex.: "Ausência em 25 g" |
| Conforme? | `VR_SATISFEITO` | Sim / Não / — (novo; o legado nunca preencheu) |
| Prévia | `RESULTADO_FORMATADO`, `ESPECIF_FORMATADA` | calculados pelo banco; a tela mostra a prévia como sairá no boletim |

Regras de habilitação (Delphi `cds_ResultadosTIPO_RESULTADOChange`):
- Numérico ⇒ habilita símbolo, valor, expoente e unidade; desabilita e limpa o tipo de resultado.
- Descritivo ⇒ o contrário. A mesma regra vale para a especificação. O banco também limpa os campos incompatíveis.

## 3. Consulta
Filtros: cliente → filtra propostas → filtra amostras; proposta; amostra; "exibir a partir de" (padrão −365 dias); "só última revisão".
Grid: amostra, revisão, proposta, produto, tipo de amostra, tipo de análise, cliente. Duplo clique abre.
Atalho novo: **Fila de análises** (`VW_LAB_TESTES_PENDENTES`) — testes sem resultado, ordenados por data prevista, com vencidos em vermelho.

## 4. Ações
| Ação | Regra |
|---|---|
| Incluir / Gravar | cabeçalho + linhas na mesma transação; digitador = profissional do usuário |
| Alterar | bloqueado em revisão antiga ou se houver **boletim emitido** para este resultado → "Gerar revisão" |
| Excluir parâmetro | bloqueado se estiver em um boletim |
| Excluir todos os resultados da amostra | só revisão 0, sem boletim (*"Deseja excluir todos os resultados desta Amostra?"*) |
| **Criar revisão** | motivo + "revisar também o boletim" (padrão sim) → `SP_GERAR_REVISAO_RESULTADO` → abre a revisão nova em edição |
| **Desfazer última revisão** | `SP_EXCLUIR_REVISAO_RESULTADO` (sem boletim) |

## 5. Erros do legado a não repetir
- Lista de amostras sem filtro de empresa.
- Revisão do resultado gerava boletim com os valores antigos (doc 12 §4).
- Resultado alterável depois do boletim enviado.

## Aceite
- [ ] Numérico `<` `1,0` exp 2 UFC/g ⇒ prévia `< 1,0e2 UFC/g` (impresso `< 1,0 × 10² UFC/g`).
- [ ] Descritivo ⇒ texto do tipo de resultado.
- [ ] Teste de outra amostra recusado; resultado duplicado para o mesmo teste recusado.
- [ ] Com boletim emitido, alterar exige revisão; a revisão leva todos os valores.

**Status:** 🟨 v1 existe incompleta — **refazer**
