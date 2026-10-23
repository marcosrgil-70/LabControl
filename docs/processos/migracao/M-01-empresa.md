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
[`EMPRESAS`](../../banco/objetos/tabelas/EMPRESAS.md) + a entidade da empresa em [`ENTIDADES`](../../banco/objetos/tabelas/ENTIDADES.md) (`TIPO_EMPRESA_USUARIA = 1`) com os contatos em `ENTIDADES_ENDERECOS/FONES/EMAILS` (D-19, [P25](../P25-cadastro-entidades-contatos.md))

## Regras de transformação
- `ID_EMPRESAS = 1`, `COD_EMPRESA = COD_EMPRESAS` ('001').
- Razão social, fantasia, CNPJ, IE/IM vêm da entidade marcada como empresa usuária (PJ) e vão para `EMPRESAS`.
- **Desde 20/10/2026:** a entidade 4 do legado migra (em M-05, com `@LC_MIGRACAO = 1`) como a **entidade da empresa** (`TIPO_EMPRESA_USUARIA = 1`, sem outros tipos) e `EMPRESAS.ID_ENTIDADES` aponta para ela **antes** do `SP_INICIALIZAR_EMPRESA` (que então não cria outra). Seus endereços, telefones e e-mails migram em M-06/M-07 como os de qualquer entidade — não há mais colunas de endereço em `EMPRESAS`.
- `SP_INICIALIZAR_EMPRESA(1)` **só depois** de M-03 (para não duplicar status/idiomas já migrados) — chamar ao final de M-04.
- Logo/cabeçalho/rodapé não existem no legado: ficam vazios (preencher na T03).

## Regras gerais (todas as etapas)
- Conexão com `SET @LC_MIGRACAO = 1`; uma transação por etapa; `ID_LEGADO` + dicionário `idAntigo → idNovo`.
- Empresa 1; strings ISO8859_1 → UTF-8 com `Trim()`; 'S'/'N' → 1/0 (nulo → 0).
- Anomalias vão para o relatório da migração (não interrompem a etapa, salvo violação de integridade).

## Conferência ao final da etapa
- [x] 1 empresa, CNPJ válido.

**Status:** ✅ implementada na tarefa 3.5 (21/10/2026) — `Services/Migracao/Etapas/M01*.cs`; conferências verdes contra o `DBMAIS.FDB` real (`MigracaoTests`).
