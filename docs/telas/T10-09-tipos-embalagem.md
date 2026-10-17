# T10-09 — Tipos de Embalagem

**Fase:** 2 · **Rota:** `/Auxiliares/EMBALAGENS_TIPOS` · **Permissão:** `AUXILIARES.*` · **Criticidade:** ⚪
**Legado:** `LabControl/un_CadTipoEmbalagem.pas` · menu *Cadastros → Cadastros Básicos → Tipo de Embalagens* · 19 registro(s) no legado
**Objetos do banco:** [`EMBALAGENS_TIPOS`](../banco/objetos/tabelas/EMBALAGENS_TIPOS.md)
**Padrão da tela:** [T10-00](T10-00-crud-auxiliares.md) · **Processos:** [P01 Multiempresa](../processos/P01-multiempresa.md) · **Regras:** [RN-03](../regras/RN-03-cadastros.md)

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Descrição* | `DESCRICAO` | até 40, única por empresa |
| Ativo | `ATIVO` | inativo some dos combos de inclusão |

## Regras específicas
- Obrigatório no recebimento da amostra ("Tipo de Embalagem é obrigatório").

## Onde é usado
Recebimento de amostras (T22), Produtos (T14).

## Critérios de aceite
- [ ] Itens do [padrão T10-00](T10-00-crud-auxiliares.md#critérios-de-aceite-valem-para-todos).

**Status:** ⬜
