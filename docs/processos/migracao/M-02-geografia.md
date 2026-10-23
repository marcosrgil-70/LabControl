# M-02 Geografia (países, estados, cidades)

**Parte:** 1 (fase 3.5) · **Criticidade:** ⚪ · **Depende de:** —
**Processo:** [P24 Migração](../P24-migracao.md) · **Tela:** [T07](../../telas/T07-migracao.md)

## Origem (Firebird `DBMAIS.FDB`)
| Tabela | Registros |
|---|---|
| `PAISES` | 1 |
| `ESTADOS` | 28 |
| `CIDADES` | 190 |

## Destino (MySQL `labcontrol_v2`)
[`PAISES`](../../banco/objetos/tabelas/PAISES.md), [`ESTADOS`](../../banco/objetos/tabelas/ESTADOS.md), [`CIDADES`](../../banco/objetos/tabelas/CIDADES.md)

## Regras de transformação
- A lista IBGE completa (5.571 municípios, nomes em maiúsculas) já vem no seed `Scripts/v2/06_cidades.sql` (desde 20/10/2026, D-21).
- País: Brasil já existe no seed (`ID_PAISES = 1`).
- UF: casar pela **sigla** com o seed (27 UFs); a 28ª linha do legado vai para o relatório.
- Cidade: casar pelo `CODIGO_IBGE`; sem código → casar por nome + UF; sem casamento → inserir e listar.

## Regras gerais (todas as etapas)
- Conexão com `SET @LC_MIGRACAO = 1`; uma transação por etapa; `ID_LEGADO` + dicionário `idAntigo → idNovo`.
- Empresa 1; strings ISO8859_1 → UTF-8 com `Trim()`; 'S'/'N' → 1/0 (nulo → 0).
- Anomalias vão para o relatório da migração (não interrompem a etapa, salvo violação de integridade).

## Conferência ao final da etapa
- [x] Toda cidade usada por endereço do legado tem correspondente.

**Status:** ✅ implementada na tarefa 3.5 (21/10/2026) — `Services/Migracao/Etapas/M02*.cs`; conferências verdes contra o `DBMAIS.FDB` real (`MigracaoTests`).
