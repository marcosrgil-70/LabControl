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

## Implementação (tarefa 0.6)
- **Alterações (I/A/E)** — `Data/AuditoriaAlteracoes.cs` + `LabDbContext.SaveChanges`: antes do save captura as entradas das tabelas da lista acima; grava as alterações e, **na mesma transação** (a do serviço ou uma aberta pelo próprio contexto), as linhas de `AUDITORIA`. Se a auditoria falhar, a alteração é desfeita.
  - I: linha completa em `DADOS_DEPOIS`, já com os valores gerados pelo banco (`NR_PROPOSTA`, `NR_AMOSTRA`…); E: linha completa em `DADOS_ANTES`; A: só as colunas que mudaram (antes e depois); alteração sem mudança real não gera linha.
  - Colunas = nomes do banco; enums com o valor do banco (`'BOLETIM'`); `SENHA_HASH` → `"***"`; blobs (`LOGO`, `ASSINATURA`) → `"[binário]"`; `ULTIMO_ACESSO` ignorada (o login tem evento próprio).
  - `ID_EMPRESAS` do registro (ou a empresa ativa, em tabela global), `ID_USUARIOS` e `IP` do `IEmpresaContexto`.
  - Optou-se pelo override do `SaveChanges` em vez de um `SaveChangesInterceptor`: o interceptor teria de chamar `SaveChanges` dentro de outro `SaveChanges` para gravar a auditoria na mesma transação.
- **Eventos** — `Infra/ServicoAuditoria.cs` (`IAuditoria.RegistrarAsync`): `Infra/Procedures.cs` já registra revisão (R), emissão (M), desfazer revisão (**D**), pedido gerado (I em `PEDIDOS_VENDA`), atualização do pedido e troca de produto (A). Login (**L**) entra na Fase 1. Os códigos `D` e `L` complementam o comentário da coluna `ACAO` (`AcoesAuditoria`).
- Testes: `tests/LabControl.Tests/AuditoriaTests.cs`.

## Cenários de teste
- Alterar uma amostra → uma linha em `LAB_HIST_AMOSTRAS_LOG` (A) com o usuário correto e uma em `AUDITORIA` com antes/depois.
- Emitir boletim → `AUDITORIA` M.
- Conexão reaproveitada do pool não carrega o usuário de outro request.
