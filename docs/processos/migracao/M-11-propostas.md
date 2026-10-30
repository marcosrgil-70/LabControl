# M-11 Propostas

**Parte:** 2 (fase 4.1) · **Criticidade:** 🔴 · **Depende de:** [M-06](M-06-contatos.md), [M-07](M-07-enderecos.md), [M-08](M-08-profissionais.md), [M-03](M-03-auxiliares.md)
**Processo:** [P24 Migração](../P24-migracao.md) · **Tela:** [T07](../../telas/T07-migracao.md)

## Origem (Firebird `DBMAIS.FDB`)
| Tabela | Registros |
|---|---|
| `LAB_PROPOSTAS` | 2.358 |
| `LAB_PROPOSTAS_CONTRATOS` | 2.219 |
| `LAB_PROPOSTAS_OBSERVACAO` | 284 |
| `LAB_PROPOSTAS_ITENS_SOL` | 10 |
| `LAB_PROPOSTAS_ITENS_ADQ` | 12 |
| `LAB_PROPOSTAS_AUTORIZACAO` | 0 |

## Destino (MySQL `labcontrol_v2`)
[`LAB_PROPOSTAS`](../../banco/objetos/tabelas/LAB_PROPOSTAS.md), [`LAB_PROPOSTAS_TEXTOS`](../../banco/objetos/tabelas/LAB_PROPOSTAS_TEXTOS.md)

## Regras de transformação
- `ANO_PROPOSTA = 2000 + ANO`; `COD_PROPOSTA`, `REV_PROPOSTA` mantidos; `NR_PROPOSTA = FN_NR_PROPOSTA(...)`.
- `ID_STATUS_PROPOSTAS` → `ID_LAB_PROPOSTAS_STATUS`.
- `ID_ENT_END_COBRANCA = COALESCE(ID_END_ENT_COBRANCA, ID_ENT_END_NF)` (validar que é endereço da entidade de cobrança).
- Campos NOT NULL ausentes (`ID_ENT_END_LAUDO`, `ID_ENT_CONTATO_RESULTADO`, `ID_ENT_COBRANCA`) → primeiro endereço/contato do cliente / o próprio cliente; listar.
- Por (empresa, ano, código), ordenado por revisão: `ID_LAB_PROPOSTAS_ORIGEM` = revisão anterior; `ULTIMA_REVISAO = 1` só na maior.
- 5 tabelas RTF → `LAB_PROPOSTAS_TEXTOS` (RTF → HTML/texto; guardar o RTF original se a conversão falhar).
- 12 propostas com `COD_PROPOSTA = ID` (número provisório): migrar como estão e listar.
- `ID_PEDIDOS_VENDAS` **não** vai para a proposta (M-13).

## Regras gerais (todas as etapas)
- Conexão com `SET @LC_MIGRACAO = 1`; uma transação por etapa; `ID_LEGADO` + dicionário `idAntigo → idNovo`.
- Empresa 1; strings ISO8859_1 → UTF-8 com `Trim()`; 'S'/'N' → 1/0 (nulo → 0).
- Anomalias vão para o relatório da migração (não interrompem a etapa, salvo violação de integridade).

## Conferência ao final da etapa
- [x] 2.358 propostas; soma de `VR_TOTAL_PROPOSTA` por ano igual (centavos).
- [x] 1 `ULTIMA_REVISAO = 1` por proposta.

## Implementação (tarefa 4.1, 23/10/2026)
- `Services/Migracao/Etapas/M11Propostas.cs`.
- **INSERT direto em lote** (`ContextoMigracao.InserirEmLoteAsync`): o EF não envia ano/código nem os totais.
- Inclusão por revisão (0, 1, 2…) para a origem já ter ID; `NR_PROPOSTA` calculado pela trigger.
- Textos RTF → HTML (`Conversoes.RtfParaHtml`); 2.223 propostas com algum texto.
- Sequência `PROPOSTA` por ano = maior código, sem os provisórios.
- **Legado real:**
  - **33** propostas com código = ID (o doc 10 citava 12) — todas listadas;
  - 15 contatos do resultado inexistentes → 1º e-mail do cliente;
  - 13 contatos e 9 contatos da NF inexistentes → NULL;
  - 2 endereços de cobrança de outra entidade → NULL; o laudo dessas 2 também é de outra entidade e foi mantido (listado);
  - 1 validade anterior à solicitação → igual à solicitação (a trigger recusa mesmo na migração).
- **Cópia do legado de 29/10/2026 (11.517 propostas):** a migração parava aqui. Ajustes:
  - **3 propostas sem endereço do laudo** (IDs 5045, 8075 e 13153) → 1º endereço do cliente, como esta especificação já previa (listado). Cliente sem endereço continua parando a etapa, com a mensagem;
  - o substituto do contato do resultado inexistente é o 1º e-mail **migrado** do cliente (antes era o 1º do legado, que podia ter sido descartado na M-06).

**Status:** ✅ implementada na tarefa 4.1 (23/10/2026); conferências verdes contra o `DBMAIS.FDB` real (`MigracaoTests`).
