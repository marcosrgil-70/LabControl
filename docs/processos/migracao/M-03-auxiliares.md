# M-03 Cadastros auxiliares

**Parte:** 1 (fase 3.5) · **Criticidade:** 🔴 · **Depende de:** [M-01](M-01-empresa.md)
**Processo:** [P24 Migração](../P24-migracao.md) · **Tela:** [T07](../../telas/T07-migracao.md)

## Origem (Firebird `DBMAIS.FDB`)
| Tabela | Registros |
|---|---|
| `LOGRADOUROS_TIPOS` | 10 |
| `ENDERECOS_TIPOS` | 4 |
| `FONES_TIPOS` | 5 |
| `EMAILS_TIPOS` | 1 |
| `TIPOS_REG_PROFISSIONAL` | 2 |
| `CARGO_FUNCIONARIOS` | 9 |
| `IDIOMAS` | 4 |
| `UNIDADES` | 27 |
| `EMBALAGENS_TIPOS` | 19 |
| `LAB_TIPOS_RESULTADOS` | 61 |
| `CONDICOES_PAGTOS` | 112 |
| `PRAZOS` | 17 |
| `MOEDAS` | 2 |
| `AMOSTRAS_TIPO` | 13 |
| `AMOSTRAS_STATUS` | 4 |
| `ANALISES_TIPO` | 13 |
| `ANALISES_STATUS` | 3 |
| `BOLETINS_STATUS` | 3 |
| `LAB_PROPOSTAS_STATUS` | 3 |
| `ANALISES_METODOS` | 176 |

## Destino (MySQL `labcontrol_v2`)
[`TIPOS_LOGRADOURO`](../../banco/objetos/tabelas/TIPOS_LOGRADOURO.md), [`ENDERECOS_TIPOS`](../../banco/objetos/tabelas/ENDERECOS_TIPOS.md), [`FONES_TIPOS`](../../banco/objetos/tabelas/FONES_TIPOS.md), [`EMAILS_TIPOS`](../../banco/objetos/tabelas/EMAILS_TIPOS.md), [`TIPOS_REG_PROFISSIONAL`](../../banco/objetos/tabelas/TIPOS_REG_PROFISSIONAL.md), [`CARGOS_FUNCIONARIOS`](../../banco/objetos/tabelas/CARGOS_FUNCIONARIOS.md), [`IDIOMAS`](../../banco/objetos/tabelas/IDIOMAS.md), [`UNIDADES`](../../banco/objetos/tabelas/UNIDADES.md), [`EMBALAGENS_TIPOS`](../../banco/objetos/tabelas/EMBALAGENS_TIPOS.md), [`LAB_TIPOS_RESULTADOS`](../../banco/objetos/tabelas/LAB_TIPOS_RESULTADOS.md), [`CONDICOES_PAGTOS`](../../banco/objetos/tabelas/CONDICOES_PAGTOS.md), [`PRAZOS`](../../banco/objetos/tabelas/PRAZOS.md), [`MOEDAS`](../../banco/objetos/tabelas/MOEDAS.md), [`AMOSTRAS_TIPO`](../../banco/objetos/tabelas/AMOSTRAS_TIPO.md), [`AMOSTRAS_STATUS`](../../banco/objetos/tabelas/AMOSTRAS_STATUS.md), [`ANALISES_TIPO`](../../banco/objetos/tabelas/ANALISES_TIPO.md), [`ANALISES_STATUS`](../../banco/objetos/tabelas/ANALISES_STATUS.md), [`BOLETINS_STATUS`](../../banco/objetos/tabelas/BOLETINS_STATUS.md), [`LAB_PROPOSTAS_STATUS`](../../banco/objetos/tabelas/LAB_PROPOSTAS_STATUS.md), [`ANALISES_METODOS`](../../banco/objetos/tabelas/ANALISES_METODOS.md)

## Regras de transformação
- **`AMOSTRAS_TIPO.CODIGO = ID antigo` e `ANALISES_TIPO.CODIGO = ID antigo`** — é o que mantém os números das amostras iguais aos impressos.
- `ENDERECOS_TIPOS.FINALIDADE` pelo ID: 1 NOTA_FISCAL, 2 CORRESPONDENCIA, 3 ENTREGA, 4 COBRANCA.
- `CONDICOES_PAGTOS.INATIVO` (0/1) → `ATIVO = 1 − INATIVO`.
- `PRAZOS.QTDE` → `QTDE_DIAS`.
- `AMOSTRAS_STATUS.PERMITE_RESULTADO = 1` no ID igual a `EMPRESAS_PARAMETROS.ID_AMOSTRAS_STATUS_RESULTADOS` (= 1, Liberada).
- Cores `TColor` → `#RRGGBB` (`(c & 0xFF)`, `(c >> 8) & 0xFF`, `(c >> 16) & 0xFF`); negativas (cor de sistema) → NULL.
- Idioma "---" migra como está.
- Dicionário `idAntigo → idNovo` por tabela (usado nas etapas seguintes).

## Regras gerais (todas as etapas)
- Conexão com `SET @LC_MIGRACAO = 1`; uma transação por etapa; `ID_LEGADO` + dicionário `idAntigo → idNovo`.
- Empresa 1; strings ISO8859_1 → UTF-8 com `Trim()`; 'S'/'N' → 1/0 (nulo → 0).
- Anomalias vão para o relatório da migração (não interrompem a etapa, salvo violação de integridade).

## Conferência ao final da etapa
- [ ] Quantidades iguais às do legado por tabela.
- [ ] Códigos de tipo de amostra/análise = IDs antigos.

**Status:** ⬜
