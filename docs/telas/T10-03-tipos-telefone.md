# T10-03 — Tipos de Telefone

**Fase:** 2 · **Rota:** `/Auxiliares/FONES_TIPOS` · **Permissão:** `AUXILIARES.*` · **Criticidade:** ⚪
**Legado:** `Standards/un_CadTipoFone.pas` · menu *fora do menu do LabControl (cadastro do pacote Standards, aberto pelo "+" da aba Telefones)* · 5 registro(s) no legado
**Objetos do banco:** [`FONES_TIPOS`](../banco/objetos/tabelas/FONES_TIPOS.md)
**Padrão da tela:** [T10-00](T10-00-crud-auxiliares.md) · **Processos:** [P01 Multiempresa](../processos/P01-multiempresa.md) · **Regras:** [RN-03](../regras/RN-03-cadastros.md)

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Descrição* | `DESCRICAO` | até 50, única por empresa |
| Ativo | `ATIVO` | inativo some dos combos de inclusão |

## Regras específicas
- Seed: Comercial, Celular, Residencial, Fax.

## Onde é usado
Telefones de entidades (T12, T13).

## Critérios de aceite
- [x] Itens do [padrão T10-00](T10-00-crud-auxiliares.md#critérios-de-aceite-valem-para-todos).

**Status:** ✅ concluída em 20/10/2026 — entrada no `CatalogoAuxiliares` (CRUD genérico da 2.1), antecipada para os cadastros com contatos tipados ([P25](../processos/P25-cadastro-entidades-contatos.md)); padrão testado em `AuxiliaresTests`, tela em `CadastrosTelasTests`.
