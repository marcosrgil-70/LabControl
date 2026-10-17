# M-01 Empresa

**Parte:** 1 (fase 3.5) · **Criticidade:** 🟠 · **Depende de:** —
**Processo:** [P24 Migração](../P24-migracao.md) · **Tela:** [T07](../../telas/T07-migracao.md)

## Origem (Firebird `DBMAIS.FDB`)
| Tabela | Registros |
|---|---|
| `EMPRESAS` | 1 |
| `ENTIDADES (TIPO_EMPRESA_USUARIA, entidade 4)` | 1 |
| `ENTIDADES_PJ` | 1 |
| `ENTIDADES_ENDERECOS da empresa` | — |

## Destino (MySQL `labcontrol_v2`)
[`EMPRESAS`](../../banco/objetos/tabelas/EMPRESAS.md)

## Regras de transformação
- `ID_EMPRESAS = 1`, `COD_EMPRESA = COD_EMPRESAS` ('001').
- Razão social, fantasia, CNPJ, IE/IM e endereço vêm da entidade marcada como empresa usuária (PJ + endereço).
- `SP_INICIALIZAR_EMPRESA(1)` **só depois** de M-03 (para não duplicar status/idiomas já migrados) — chamar ao final de M-04.
- Logo/cabeçalho/rodapé não existem no legado: ficam vazios (preencher na T03).

## Regras gerais (todas as etapas)
- Conexão com `SET @LC_MIGRACAO = 1`; uma transação por etapa; `ID_LEGADO` + dicionário `idAntigo → idNovo`.
- Empresa 1; strings ISO8859_1 → UTF-8 com `Trim()`; 'S'/'N' → 1/0 (nulo → 0).
- Anomalias vão para o relatório da migração (não interrompem a etapa, salvo violação de integridade).

## Conferência ao final da etapa
- [ ] 1 empresa, CNPJ válido.

**Status:** ⬜
