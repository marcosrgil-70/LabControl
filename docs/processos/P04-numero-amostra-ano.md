# P04 — Número da Amostra e Proteção do Ano

**Criticidade:** 🔴🔴 (requisito explícito do usuário) · **Fase:** 4
**Telas:** [T22 Recebimento](../telas/T22-recebimento-amostras.md), [T10-14 Tipos de amostra](../telas/T10-14-tipos-amostra.md), [T10-16 Tipos de análise](../telas/T10-16-tipos-analise.md), [T05 Parâmetros](../telas/T05-parametros-empresa.md)
**Objetos:** [`LAB_HIST_AMOSTRAS`](../banco/objetos/tabelas/LAB_HIST_AMOSTRAS.md), [`TG_AMOSTRAS_BI`](../banco/objetos/triggers/TG_AMOSTRAS_BI.md), [`TG_AMOSTRAS_BU`](../banco/objetos/triggers/TG_AMOSTRAS_BU.md), [`SP_PROXIMO_NUMERO`](../banco/objetos/rotinas/SP_PROXIMO_NUMERO.md), [`FN_NR_AMOSTRA`](../banco/objetos/rotinas/FN_NR_AMOSTRA.md), [`FN_NR_AMOSTRA_ID`](../banco/objetos/rotinas/FN_NR_AMOSTRA_ID.md), [`AMOSTRAS_TIPO`](../banco/objetos/tabelas/AMOSTRAS_TIPO.md), [`ANALISES_TIPO`](../banco/objetos/tabelas/ANALISES_TIPO.md), [`EMPRESAS_PARAMETROS`](../banco/objetos/tabelas/EMPRESAS_PARAMETROS.md)
**Regras:** [RN-05](../regras/RN-05-amostras.md) (RN-05.01 a RN-05.08)

## Formato
`TT-CCCCC-AA/YY [Rnn]` — ex.: `04-01382-01/26` e, na revisão 1, `04-01382-01/26 R01`.

| Parte | Origem | Observação |
|---|---|---|
| TT | `AMOSTRAS_TIPO.CODIGO` | 2 dígitos; imutável depois de usado |
| CCCCC | `COD_AMOSTRA` | sequencial **por empresa e por ano** (5 dígitos) |
| AA | `ANALISES_TIPO.CODIGO` | 2 dígitos; imutável depois de usado |
| YY | `ANO_AMOSTRA` mod 100 | ano **do servidor** (4 dígitos no banco) |
| Rnn | `REVISAO` > 0 | revisões mantêm TT-CCCCC-AA/YY |

O sequencial CCCCC é único no ano **independentemente** de TT e AA (que só "decoram" o número).

## Como era no legado (e por que falhava)
1. Ao sair da Data de Entrega, a tela calculava `ANO_AMOSTRA = FormatDateTime('YY', DT_ENTREGA)` → o ano vinha **da data digitada**.
2. `SP_GET_COD_AMOSTRA` lia o ano do `DT_ENTREGA` da **última amostra** (maior ID); se fosse menor que o ano atual, **zerava** `ULT_COD_AMOSTRA`.
3. Efeitos: uma data com o ano anterior digitada em janeiro fazia a próxima amostra voltar para 00001 e **duplicar** números do ano; amostras com ano diferente da entrega (3 casos) e ano 0 (1 caso); número consumido ao abrir a aba Observação (lacunas); sem bloqueio (duplicidade com 2 usuários); sem `UNIQUE`.

## Fluxo v2
1. **Tela** (T22): número exibido como "INCLUSÃO"; o usuário escolhe tipo de amostra (TT) e tipo de análise (AA) e a tela mostra a prévia `TT-?????-AA/YY` com o ano do servidor.
2. **Tela**: data de entrega padrão = hoje (data do servidor).
   - Futura → erro imediato "Data de entrega maior que a data de hoje".
   - Ano diferente do ano atual (ex.: 30/12 digitado em 02/01), mesmo dentro do limite → **aviso amarelo** antes de gravar.
   - Mais antiga que `DIAS_RETROATIVOS_ENTREGA` (padrão 7) → **confirmação explícita**: *"A data de entrega 15/01/2025 é de 640 dias atrás. Confirme que o ANO está correto."*
3. **Serviço** (`AmostraService.Incluir`):
   - se a data está além do limite: exige confirmação **e** permissão `AMOSTRAS.ENTREGA_RETROATIVA`; com as duas, `SET @LC_PERMITE_RETROATIVO = 1` **na mesma conexão/transação**; sem permissão → recusa;
   - envia `ANO_AMOSTRA = 0`, `COD_AMOSTRA = 0`, `NR_AMOSTRA = ''`.
4. **Banco** (`TG_AMOSTRAS_BI`, revisão 0):
   - repete as validações (futura; retroativa sem `@LC_PERMITE_RETROATIVO`);
   - `ANO_AMOSTRA = YEAR(CURDATE())` — **mesmo que a data seja de outro ano**;
   - `COD_AMOSTRA = SP_PROXIMO_NUMERO('AMOSTRA', ano)` com bloqueio;
   - `NR_AMOSTRA = FN_NR_AMOSTRA_ID(...)`.
5. **Serviço**: `SP_AMOSTRA_GERAR_TESTES`, COMMIT, `SET @LC_PERMITE_RETROATIVO = 0`, relê a amostra.
6. **Tela**: "Código da Amostra: 04-01383-01/26" + oferta de etiquetas.

## Alterações posteriores
- Trocar TT/AA recalcula `NR_AMOSTRA` (`TG_AMOSTRAS_BU`), **só sem resultados** ("Existem resultados: tipo, proposta e produto da amostra não podem ser alterados (gere revisão).").
- `ANO_AMOSTRA`, `COD_AMOSTRA`, `REVISAO` e empresa são imutáveis.
- Revisão: `PR_REVISAO_AMOSTRA` mantém ano/código e grava `REVISAO = máx + 1` (com bloqueio).

## Cenários de teste (aceite)
| # | Dado | Quando | Então |
|---|---|---|---|
| 1 | hoje = 16/10/2026 | entrega 16/10/2026 | `…/26`, sequência +1 |
| 2 | hoje = 16/10/2026 | entrega 17/10/2026 | recusa "Data de entrega maior que a data de hoje" |
| 3 | limite 7 dias, sem permissão | entrega 16/10/2025 | recusa (tela e banco) |
| 4 | limite 7 dias, com permissão + confirmação | entrega 16/10/2025 | grava com **ano 2026**, sem reiniciar a sequência |
| 5 | hoje = 02/01/2027 | entrega 30/12/2026 | aviso amarelo; grava `…/27` com sequência de 2027 |
| 6 | 1ª amostra de 2027 | inclusão | `CCCCC = 00001`; amostras de 2026 intactas |
| 7 | duas inclusões simultâneas | — | números diferentes |
| 8 | app envia `ANO = 1999, COD = 999` | inclusão | ignorados (roteiro doc 12 §7 item 5) |
| 9 | tipo de amostra com código já usado | alterar código | recusado |

## Implementação (tarefa 4.3, 23/10/2026)
- **T22** (`AmostraService`):
  - número sempre do banco; prévia `TT-?????-AA/YY` na tela;
  - "hoje" é o do **servidor do MySQL** (`Infra/RelogioBanco`).
- **Data de entrega:**
  - futura → recusada;
  - mais antiga que `DIAS_RETROATIVOS_ENTREGA` → a tela pergunta "Confirme que o ANO está correto." e grava só com `AMOSTRAS.ENTREGA_RETROATIVA` (`@LC_PERMITE_RETROATIVO` na transação);
  - a confirmação vale só para a data confirmada (code-review 4.6);
  - ano diferente do atual → aviso amarelo.
- **Cenários:**
  - 1, 2, 3, 4 e 7 → `AmostrasTests`/navegador;
  - 5 (aviso) → só na tela;
  - 6 e 8 → roteiro SQL / `RoteiroTests` (dependem do relógio do servidor);
  - 9 → `AuxiliaresEspecificosTests`.
