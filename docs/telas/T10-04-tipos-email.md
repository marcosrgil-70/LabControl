# T10-04 — Tipos de E-mail

**Fase:** 2 · **Rota:** `/Auxiliares/EMAILS_TIPOS` · **Permissão:** `AUXILIARES.*` · **Criticidade:** ⚪
**Legado:** `Standards/un_cadEmails_Tipos.pas` · menu *fora do menu do LabControl (pacote Standards)* · 1 registro(s) no legado
**Objetos do banco:** [`EMAILS_TIPOS`](../banco/objetos/tabelas/EMAILS_TIPOS.md)
**Padrão da tela:** [T10-00](T10-00-crud-auxiliares.md) · **Processos:** [P01 Multiempresa](../processos/P01-multiempresa.md) · **Regras:** [RN-03](../regras/RN-03-cadastros.md)

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Descrição* | `DESCRICAO` | até 30, única por empresa |
| Ativo | `ATIVO` | inativo some dos combos de inclusão |

## Regras específicas
- Seed: Comercial.
- Opcional no e-mail (`ID_EMAILS_TIPOS` aceita nulo).

## Onde é usado
E-mails/contatos (T12).

## Critérios de aceite
- [x] Itens do [padrão T10-00](T10-00-crud-auxiliares.md#critérios-de-aceite-valem-para-todos).

**Status:** ✅ concluída em 20/10/2026 — entrada no `CatalogoAuxiliares` (CRUD genérico da 2.1), antecipada para os cadastros com contatos tipados ([P25](../processos/P25-cadastro-entidades-contatos.md)); padrão testado em `AuxiliaresTests`, tela em `CadastrosTelasTests`.
