# T10-02 — Tipos de Endereço

**Fase:** 2 · **Rota:** `/Auxiliares/ENDERECOS_TIPOS` · **Permissão:** `AUXILIARES.*` · **Criticidade:** ⚪
**Legado:** `Standards/un_Enderecos_Tipos.pas` · menu *Cadastros → Cadastros Básicos → Tipos de Endereço* · 4 registro(s) no legado
**Objetos do banco:** [`ENDERECOS_TIPOS`](../banco/objetos/tabelas/ENDERECOS_TIPOS.md)
**Padrão da tela:** [T10-00](T10-00-crud-auxiliares.md) · **Processos:** [P01 Multiempresa](../processos/P01-multiempresa.md) · **Regras:** [RN-03](../regras/RN-03-cadastros.md)

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Descrição* | `DESCRICAO` | até 30, única por empresa |
| Finalidade* | `FINALIDADE` | **novo**: Geral / Nota fiscal / Correspondência / Entrega / Cobrança |
| Ativo | `ATIVO` | inativo some dos combos de inclusão |

## Regras específicas
- No legado o código usava IDs fixos (1 NF, **2 Correspondência**, 3 Entrega, 4 Cobrança) — a view de amostras exigia o tipo 2. No v2 a **finalidade** substitui o ID fixo.
- Na proposta: endereço do laudo sugere finalidade CORRESPONDENCIA; endereço da cobrança sugere COBRANCA ou NOTA_FISCAL.
- Na migração: IDs 1..4 → finalidades NOTA_FISCAL, CORRESPONDENCIA, ENTREGA, COBRANCA.

## Onde é usado
Endereços (T12) e sugestões da Proposta (T20).

## Critérios de aceite
- [x] Itens do [padrão T10-00](T10-00-crud-auxiliares.md#critérios-de-aceite-valem-para-todos).

**Status:** ✅ concluída em 20/10/2026 — entrada no `CatalogoAuxiliares` (CRUD genérico da 2.1), antecipada para os cadastros com contatos tipados ([P25](../processos/P25-cadastro-entidades-contatos.md)); padrão testado em `AuxiliaresTests`, tela em `CadastrosTelasTests`.
