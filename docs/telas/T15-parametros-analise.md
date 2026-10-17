# T15 — Parâmetros de Análise

**Fase:** 3 · **Rota:** `/ParametrosAnalise` · **Permissão:** `PARAMETROS_ANALISE.*` · **Tabela:** `LAB_PARAMETROS_ANALISES`
**Legado:** `un_ParametrosAnalises.pas` (Código, Parâmetro, Valor unitário, Tipo de análise, Descrição reduzida, **INMETRO** (`AUDITADO`), Método, Descrição do método).

## Campos
| Campo | Coluna | Regra / uso |
|---|---|---|
| Parâmetro* | `DESCRICAO` | único por empresa; usado na proposta e nos resultados |
| Descrição reduzida* | `DESC_REDUZIDA` | **texto impresso no boletim** (era opcional no legado e causava erro NOT NULL na revisão do boletim) |
| Tipo de análise* | `ID_ANALISES_TIPO` | define o tipo dos testes |
| Método padrão | `ID_ANALISES_METODOS` | na proposta, o combo de método fica **filtrado** por este método quando informado (regra `dbl_ParamAnaliPropertiesChange`) |
| Descrição do método | `DESCR_METODO` | prefixo da linha de metodologia do boletim (`FN_OBS_METODOS`: "DESCR_METODO: método") |
| Valor unitário | `VR_UNITARIO` | preço sugerido na proposta (só se a linha estiver com valor 0) |
| **Acreditado (INMETRO)** | `AUDITADO` | separa as metodologias "Acreditado" × "Não acreditado" no boletim e define `FN_ACREDITADO` (0/1/2) |
| Inativo | `INATIVO` | — |

## Consulta
Código, descrição, tipo de análise, método, valor, acreditado — filtros por tipo e acreditado.

**Status:** 🟨 v1 existe — ajustar (desc. reduzida obrigatória, método padrão, acreditado)
