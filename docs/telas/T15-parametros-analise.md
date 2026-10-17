# T15 — Parâmetros de Análise

**Fase:** 3 · **Rota:** `/ParametrosAnalise` · **Permissão:** `PARAMETROS_ANALISE.*` · **Tabela:** `LAB_PARAMETROS_ANALISES`
**Legado:** `un_ParametrosAnalises.pas` (Código, Parâmetro, Valor unitário, Tipo de análise, Descrição reduzida, **INMETRO** (`AUDITADO`), Método, Descrição do método).

## Referências cruzadas
- **Delphi:** `LabControl/un_ParametrosAnalises.pas` (menu *Cadastros → Parâmetros para Análises*)
- **Objetos do banco:** [`LAB_PARAMETROS_ANALISES`](../banco/objetos/tabelas/LAB_PARAMETROS_ANALISES.md), [`ANALISES_TIPO`](../banco/objetos/tabelas/ANALISES_TIPO.md), [`ANALISES_METODOS`](../banco/objetos/tabelas/ANALISES_METODOS.md), [`FN_ACREDITADO`](../banco/objetos/rotinas/FN_ACREDITADO.md), [`FN_OBS_METODOS`](../banco/objetos/rotinas/FN_OBS_METODOS.md)
- **Processos:** [P16 Montagem do boletim](../processos/P16-montagem-boletim.md) · [P06 Valores da proposta](../processos/P06-valores-proposta.md)
- **Regras de negócio:** [RN-03 Cadastros](../regras/RN-03-cadastros.md) · [RN-08 Boletim](../regras/RN-08-boletim.md)
- **Plano:** [00-PLANO-EXECUCAO.md](../00-PLANO-EXECUCAO.md)

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
