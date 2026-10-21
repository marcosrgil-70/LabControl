# M-05 Entidades (clientes, fornecedores, funcionários)

**Parte:** 1 (fase 3.5) · **Criticidade:** 🟠 · **Depende de:** [M-01](M-01-empresa.md), [M-03](M-03-auxiliares.md)
**Processo:** [P24 Migração](../P24-migracao.md) · **Tela:** [T07](../../telas/T07-migracao.md)

## Origem (Firebird `DBMAIS.FDB`)
| Tabela | Registros |
|---|---|
| `ENTIDADES` | 527 |
| `ENTIDADES_PF` | 44 |
| `ENTIDADES_PJ` | 483 |
| `ENTIDADES_OBSERVACOES` | 1 |

## Destino (MySQL `labcontrol_v2`)
[`ENTIDADES`](../../banco/objetos/tabelas/ENTIDADES.md)

## Regras de transformação
- `CPF_CNPJ` = CPF (PF) ou CNPJ (PJ), só dígitos formatados; `NOME_FANTASIA` = sobrenome (PF) ou fantasia (PJ); `RG_IE` = RG ou inscrição estadual.
- `ESTADO_CIVIL` 0..4 → S, C, D, P, V.
- Flags `TIPO_*`, `INATIVO`, `OBS_INCLUIR_FATURAMENTO` 'S'/'N' → 1/0; `TIPO_EMPRESA_USUARIA`/`TIPO_COMPRADOR` não existem no v2 (empresa vira `EMPRESAS`).
- Documento duplicado na mesma empresa: o segundo fica com `CPF_CNPJ` NULL e vai para o relatório.
- `ID_LEGADO = ID_ENTIDADES`.

## Regras gerais (todas as etapas)
- Conexão com `SET @LC_MIGRACAO = 1`; uma transação por etapa; `ID_LEGADO` + dicionário `idAntigo → idNovo`.
- Empresa 1; strings ISO8859_1 → UTF-8 com `Trim()`; 'S'/'N' → 1/0 (nulo → 0).
- Anomalias vão para o relatório da migração (não interrompem a etapa, salvo violação de integridade).

## Conferência ao final da etapa
- [x] 527 entidades.
- [x] Lista de documentos duplicados/ inválidos.

**Status:** ✅ implementada na tarefa 3.5 (21/10/2026) — `Services/Migracao/Etapas/M05*.cs`; conferências verdes contra o `DBMAIS.FDB` real (`MigracaoParte1Tests`).
