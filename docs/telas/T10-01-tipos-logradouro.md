# T10-01 — Tipos de Logradouro

**Fase:** 2 · **Rota:** `/Auxiliares/TIPOS_LOGRADOURO` · **Permissão:** `AUXILIARES.*` · **Criticidade:** ⚪
**Legado:** `Standards/un_Logradouros_Tipos.pas` · menu *Cadastros → Cadastros Básicos → Tipos de Logradouros* · 10 registro(s) no legado
**Objetos do banco:** [`TIPOS_LOGRADOURO`](../banco/objetos/tabelas/TIPOS_LOGRADOURO.md)
**Padrão da tela:** [T10-00](T10-00-crud-auxiliares.md) · **Processos:** [P01 Multiempresa](../processos/P01-multiempresa.md) · **Regras:** [RN-03](../regras/RN-03-cadastros.md)

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Descrição* | `DESCRICAO` | até 60 |
| Abreviatura* | `ABREVIATURA` | até 10, **única** por empresa (ex.: "R.", "AV.") |
| Ativo | `ATIVO` | inativo some dos combos de inclusão |

## Regras específicas
- A abreviatura aparece na linha 1 do endereço (`VW_ENTIDADES_ENDERECOS.ENDERECO_LINHA1`).
- Seed por empresa (`SP_INICIALIZAR_EMPRESA`): Rua, Avenida, Alameda, Travessa, Rodovia, Estrada, Praça.

## Onde é usado
Endereços de clientes e da empresa (T12, T03).

## Critérios de aceite
- [ ] Itens do [padrão T10-00](T10-00-crud-auxiliares.md#critérios-de-aceite-valem-para-todos).

**Status:** ⬜
