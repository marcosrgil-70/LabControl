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

## Critérios de aceite
- [x] Sem descrição reduzida não grava (mensagem no campo) — `ParametrosAnaliseTests.Grava_com_descricao_reduzida_metodo_valor_e_acreditado` e navegador.
- [x] Tipo de análise obrigatório; método padrão, descrição do método, valor unitário (R$ "1.234,56") e acreditado gravados — mesmo teste.
- [x] Descrição única por empresa, recusada no campo — mesmo teste.
- [x] Consulta com filtros por tipo e acreditado; inativo some com aviso; em uso não exclui — `Em_uso_nao_exclui_inativo_some_e_outra_empresa_nao_ve`.
- [x] Método padrão disponível para a proposta (gravado em `ID_ANALISES_METODOS`; o filtro do combo é da T20, 4.2).
- [x] CRUD pela tela, no navegador (método novo pelo "+") — `CrudNavegadorTelasTests.Parametro_de_analise_crud_com_metodo_pelo_mais`.

## Implementação (tarefa 3.4)
- `Controllers/ParametrosAnaliseController.cs`, `Services/ParametroAnaliseService.cs`, `ViewModels/ParametroAnaliseVM.cs`, `Views/ParametrosAnalise/{Index,Form}.cshtml` — mesmo visual e fluxo da T14: lista com busca (parâmetro ou descrição reduzida), filtros por tipo e acreditado, "mostrar inativos", 20 por página; depois de gravar **volta para a lista** ("Parâmetro X gravado."), como o usuário pediu na T14.
- Parâmetro, descrição reduzida e descrição do método em maiúsculas (150). Tipo de análise obrigatório (combo "01 — DESCRIÇÃO", ordem do código = AA do número da amostra); método padrão opcional; os dois com ativos + o já escolhido e com o "+" (`lc-rapido`).
- Valor unitário em reais: "1.234,56", "1.000" (ponto com grupos de 3 = milhar, 3.6) ou "1234.56", ≥ 0, 2 casas (`Textos.Decimal`); vazio = 0,00 (sugerido na proposta só quando a linha estiver com 0).
- Excluir só sem uso (1451 → inativar); inativar/reativar com confirmação.

**Status:** ✅ concluída na tarefa 3.4 (21/10/2026).
