# T10-18 — Métodos de Análise

**Fase:** 2 · **Rota:** `/Auxiliares/ANALISES_METODOS` · **Permissão:** `AUXILIARES.*` · **Criticidade:** ⚪
**Legado:** `LabControl/un_CadMetodoAnalise.pas` · menu *Cadastros → Métodos de Análise* · 176 registro(s) no legado
**Objetos do banco:** [`ANALISES_METODOS`](../banco/objetos/tabelas/ANALISES_METODOS.md)
**Padrão da tela:** [T10-00](T10-00-crud-auxiliares.md) · **Processos:** [P01 Multiempresa](../processos/P01-multiempresa.md) · **Regras:** [RN-03](../regras/RN-03-cadastros.md)

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Descrição* | `DESCRICAO` | até 250 (ex.: "ISO 4833-1:2013") |
| Ativo | `ATIVO` | inativo some dos combos de inclusão |

## Regras específicas
- Vai para as "Metodologias utilizadas" do boletim (`FN_OBS_METODOS`: "DESCR_METODO: método").
- Método padrão do parâmetro filtra o combo na proposta.
- Cadastro rápido "+" na proposta (`Btn_IncCadMetodo`).

## Onde é usado
Parâmetros (T15), Proposta (T20), Boletim (T27).

## Critérios de aceite
- [ ] Itens do [padrão T10-00](T10-00-crud-auxiliares.md#critérios-de-aceite-valem-para-todos).

**Status:** ⬜
