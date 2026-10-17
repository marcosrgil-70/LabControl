# P21 — Auditoria e Log

**Criticidade:** 🟠 (rastreabilidade exigida pela ISO/IEC 17025) · **Fase:** 0 (infra) / 8 (tela)
**Telas:** [T06 Auditoria](../telas/T06-auditoria.md)
**Objetos:** [`AUDITORIA`](../banco/objetos/tabelas/AUDITORIA.md), [`LAB_HIST_AMOSTRAS_LOG`](../banco/objetos/tabelas/LAB_HIST_AMOSTRAS_LOG.md), [`TG_AMOSTRAS_AI`](../banco/objetos/triggers/TG_AMOSTRAS_AI.md), [`TG_AMOSTRAS_AU`](../banco/objetos/triggers/TG_AMOSTRAS_AU.md), [`TG_AMOSTRAS_AD`](../banco/objetos/triggers/TG_AMOSTRAS_AD.md)
**Regras:** [RN-02](../regras/RN-02-seguranca.md)

## Duas trilhas
| Trilha | Quem grava | O quê |
|---|---|---|
| `LAB_HIST_AMOSTRAS_LOG` | **banco** (triggers da amostra) | inclusão/alteração/exclusão da amostra em JSON (nº, cliente, proposta, produto, datas, quantidade, lote, validade/fabricação, status anterior e novo, responsável); `ID_USUARIOS` = `@LC_ID_USUARIOS` |
| `AUDITORIA` | **aplicação** | antes/depois em JSON de tabelas críticas + eventos (login, revisão, emissão, ajuste de numeração, reset de senha) |

## Tabelas com auditoria obrigatória (aplicação)
`LAB_PROPOSTAS` (+ produtos/análises/textos), `PEDIDOS_VENDA`, `LAB_HIST_AMOSTRAS`, `LAB_RESULTADOS_*`, `LAB_BOLETIM_*`, `LAB_MOV_AMOSTRAS`, `LAB_LOCAL_AMOSTRAS`, `SEQUENCIAS_DOCUMENTOS`, `EMPRESAS`, `EMPRESAS_PARAMETROS`, `USUARIOS*`, `PERFIS*`, `ENTIDADES_FUNCIONARIOS`.

## Implementação
1. `AuditoriaInterceptor : SaveChangesInterceptor` — antes do save captura `OriginalValues`/`CurrentValues` das entidades auditáveis; depois do save grava `AUDITORIA` (I/A/E) na **mesma** transação.
2. Chamadas explícitas após procedures (revisão R, emissão M, pedido, desfazer) com os IDs devolvidos e o motivo.
3. Login (L) no `ContaService` (sucesso e falha, sem gravar a senha).
4. `SessaoMySqlInterceptor` define `@LC_ID_USUARIOS` (para o log das triggers).
5. Nunca gravar blobs (logo, assinatura) nem hash de senha no JSON.

## Cenários de teste
- Alterar uma amostra → uma linha em `LAB_HIST_AMOSTRAS_LOG` (A) com o usuário correto e uma em `AUDITORIA` com antes/depois.
- Emitir boletim → `AUDITORIA` M.
- Conexão reaproveitada do pool não carrega o usuário de outro request.
