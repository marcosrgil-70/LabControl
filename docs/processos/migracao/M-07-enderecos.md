# M-07 Endereços

**Parte:** 1 (fase 3.5) · **Criticidade:** 🟠 · **Depende de:** [M-02](M-02-geografia.md), [M-05](M-05-entidades.md)
**Processo:** [P24 Migração](../P24-migracao.md) · **Tela:** [T07](../../telas/T07-migracao.md)

## Origem (Firebird `DBMAIS.FDB`)
| Tabela | Registros |
|---|---|
| `ENTIDADES_ENDERECOS` | 538 |
| `LOGRADOUROS` | 552 |
| `CIDADES_BAIRROS` | 485 |

## Destino (MySQL `labcontrol_v2`)
[`ENTIDADES_ENDERECOS`](../../banco/objetos/tabelas/ENTIDADES_ENDERECOS.md)

## Regras de transformação
- `ENTIDADES_ENDERECOS ⨝ LOGRADOUROS ⨝ CIDADES_BAIRROS ⨝ CIDADES`.
- `LOGRADOURO = NOME_LOGRADOURO`, `BAIRRO` = descrição do bairro, `ID_TIPOS_LOGRADOURO` pelo tipo do logradouro, `ID_CIDADES` pelo IBGE (dicionário M-02).
- Manter o dicionário de IDs (endereço do laudo e da cobrança nas propostas).

## Regras gerais (todas as etapas)
- Conexão com `SET @LC_MIGRACAO = 1`; uma transação por etapa; `ID_LEGADO` + dicionário `idAntigo → idNovo`.
- Empresa 1; strings ISO8859_1 → UTF-8 com `Trim()`; 'S'/'N' → 1/0 (nulo → 0).
- Anomalias vão para o relatório da migração (não interrompem a etapa, salvo violação de integridade).

## Conferência ao final da etapa
- [x] 538 endereços; nenhum sem cidade (listar exceções).

**Status:** ✅ implementada na tarefa 3.5 (21/10/2026) — `Services/Migracao/Etapas/M07*.cs`; conferências verdes contra o `DBMAIS.FDB` real (`MigracaoTests`).
