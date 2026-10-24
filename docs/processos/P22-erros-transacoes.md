# P22 — Erros de Negócio, Transações e Chamada de Procedures

**Criticidade:** 🔴 (padrão técnico usado por todas as telas críticas) · **Fase:** 0
**Objetos:** todas as [rotinas](../banco/objetos/README.md#funções-e-procedures) e [triggers](../banco/objetos/README.md#triggers-por-tabela)
**Regras:** [RN-01](../regras/RN-01-gerais-multiempresa.md)

## Mensagens de negócio (SQLSTATE 45000)
- Triggers e procedures recusam operações com `SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = '…'` (texto em português, pronto para o usuário). A lista de cada objeto está no MD dele.
- Aplicação: capturar `MySqlException` com `SqlState == "45000"` → `Resultado.Erro(ex.Message)` → `ModelState`/`TempData` → mensagem na tela. **Não** é erro técnico (não logar como erro, não mostrar página 500).
- Outros erros MySQL com tradução amigável: 1062 (duplicidade em `UNIQUE`) → "Já existe um registro com …"; 1451 (FK ao excluir) → "Registro em uso; inative em vez de excluir."; 1452 (FK ao gravar) → "Registro relacionado não encontrado."

## Dois tipos de procedure
| Prefixo | Transação | Como chamar |
|---|---|---|
| `SP_GERAR_REVISAO_*`, `SP_EXCLUIR_REVISAO_*`, `SP_EMITIR_BOLETIM`, `SP_GERAR_PEDIDO_VENDA`, `SP_ATUALIZAR_PEDIDO_REVISAO`, `SP_TROCAR_PRODUTO_PROPOSTA` | **própria** (`START TRANSACTION … COMMIT`, `ROLLBACK + RESIGNAL` no erro) | **fora** de `BeginTransaction` do EF — um `START TRANSACTION` dentro de outra faz commit implícito da transação da aplicação |
| `SP_AMOSTRA_GERAR_TESTES`, `SP_RECALCULAR_PROPOSTA`, `SP_INICIALIZAR_EMPRESA`, `PR_*`, `SP_PROXIMO_NUMERO` | sem transação | podem (e às vezes devem) rodar dentro da transação da aplicação |

## Implementação (tarefa 0.5)
| Peça | Arquivo | O que faz |
|---|---|---|
| Conexão | `Data/ConexaoMySql.cs` | `options.Configurar(cs, empresa)`: força `AllowUserVariables=True` (sem ele o MySqlConnector trata `@LC_*` como parâmetro) e `ConnectionReset=True`; registra o interceptor |
| Sessão | `Data/Interceptors/SessaoMySqlInterceptor.cs` | em toda abertura: `SET SESSION transaction_isolation = 'READ-COMMITTED', @LC_ID_USUARIOS = <usuário do request>, @LC_IP = <IP>, @LC_REVISAO = NULL, @LC_MIGRACAO = 0, @LC_PERMITE_RETROATIVO = 0, @LC_DESFAZENDO = NULL` |
| Isolamento | `Data/Interceptors/IsolamentoInterceptor.cs` | `BeginTransaction()` sem nível abre em READ COMMITTED (o MySqlConnector mandaria `REPEATABLE READ`) |
| Resultado | `Infra/Resultado.cs` | `Resultado.Ok()`, `Resultado.Ok(valor)`, `Resultado.Erro(msg, campo?)` (serve para `Resultado` e `Resultado<T>`) |
| Erros | `Infra/ErrosMySql.cs` | `MensagemDeNegocio(ex)` (45000 = texto da trigger; 1062 por constraint; 1451; 1452), `CapturarAsync(...)` |
| Procedures | `Infra/Procedures.cs` (scoped) | um método por `SP_*`; confere que a origem é da empresa ativa; usuário/profissional do contexto; lê OUT params; 45000 → `Resultado.Erro`; **recusa** procedure com transação própria dentro de `BeginTransaction`; `DefinirPermiteRetroativoAsync` só dentro de transação |
| MVC | `Infra/ResultadoMvc.cs` | `this.AplicarNoFormulario(r)` (ModelState) / `this.AplicarNoTempData(r)`; `ErroNegocioFilter` global: erro de negócio que escapou vira mensagem (TempData + volta à página; AJAX → 422 `{ erro }`) em vez de página 500 |

⚠️ Fora de transação o EF abre/fecha a conexão a cada comando, e cada abertura **zera** as variáveis: `SET @LC_PERMITE_RETROATIVO = 1` só vale dentro da transação da gravação.
Testes: `tests/LabControl.Tests/SessaoErrosProceduresTests.cs`.

## Padrão de serviço (C#)
```csharp
public async Task<Resultado<int>> IncluirAmostra(AmostraVM vm) {
    var erro = Validar(vm); if (erro != null) return Resultado.Erro(erro);       // validações de tela
    var amostra = Mapear(vm);
    await using var tx = await db.Database.BeginTransactionAsync();
    try {
        if (vm.RetroativaAutorizada) await procedures.DefinirPermiteRetroativoAsync(true);
        db.LabHistAmostras.Add(amostra); await db.SaveChangesAsync();               // trigger numera; EF relê NR/COD/ANO
        var testes = await procedures.AmostraGerarTestesAsync(amostra.IdLabHistAmostras);
        if (!testes.Sucesso) { await tx.RollbackAsync(); return Resultado.Erro(testes.Mensagem!); }
        await tx.CommitAsync();
    } catch (Exception ex) when (ErrosMySql.MensagemDeNegocio(ex) is { } msg) { await tx.RollbackAsync(); return Resultado.Erro(msg); }
    return Resultado.Ok(amostra.IdLabHistAmostras);
}
```

## Variáveis de sessão
| Variável | Quem define | Quando |
|---|---|---|
| `@LC_ID_USUARIOS` | `SessaoMySqlInterceptor` | toda abertura de conexão |
| `@LC_MIGRACAO` | interceptor (= 0) / MigracaoService (= 1) | sempre 0, exceto na migração |
| `@LC_PERMITE_RETROATIVO` | interceptor (= 0) / AmostraService (= 1) | só no request autorizado |
| `@LC_DESFAZENDO` | as próprias `SP_EXCLUIR_REVISAO_*` | interno |
| `@LC_REVISAO` | as próprias `SP_GERAR_REVISAO_*` (interceptor zera) | interno: só elas incluem revisão (`REVISAO` > 0) |
| `@LC_IP` | `SessaoMySqlInterceptor` | toda abertura de conexão (AUDITORIA das procedures) |

## Concorrência
- Numeração: `SP_PROXIMO_NUMERO` com `INSERT … ON DUPLICATE KEY UPDATE` (trava exclusiva numa instrução só); revisões: `SELECT … FOR UPDATE` nas revisões existentes; item da proposta: `TG_PROPPROD_BI` trava a proposta (`FOR UPDATE`).
- A aplicação usa **READ COMMITTED** (definido pelo `SessaoMySqlInterceptor` em toda conexão): sem as travas de intervalo do REPEATABLE READ, que faziam documentos **diferentes** gravados ao mesmo tempo darem deadlock (o `MAX()+1` da trigger e o `MAX(REVISAO) … FOR UPDATE` travavam o fim do índice). A unicidade continua garantida por `UNIQUE` + `FOR UPDATE` nas linhas existentes.
- Coberto por `tests/LabControl.Tests/ConcorrenciaTests.cs` (8 gravações simultâneas × 10, mesma empresa: numeração e primeiro item de propostas diferentes).
- Telas de edição: controle otimista por `ALTERADO_EM` (token de concorrência) para avisar "registro alterado por outro usuário".

## Cenários de teste (projeto xUnit da fase 0)
- Recriar banco de teste pelos scripts e rodar o roteiro do doc 12 §7 via serviço.
- Chamar `SP_GERAR_REVISAO_AMOSTRA` dentro de `BeginTransaction` em teste → documentar o efeito (commit implícito) e garantir que o serviço não faz isso.
- Mensagem 45000 chega à tela com o texto exato.

## Recusa de procedure dentro da transação (code-review 4.6, 24/10/2026)
Quando uma procedure sem transação própria (`SP_AMOSTRA_GERAR_TESTES`, `SP_RECALCULAR_PROPOSTA`) recusa dentro da gravação, o `Procedures` já devolve `Resultado.Erro`. O serviço lança `ErroDeNegocioException(mensagem)` para desfazer a transação, e o `ErrosMySql.CapturarAsync` a reconhece como erro de negócio: a mensagem chega à tela, sem erro 500. Teste: `AceiteFase4Tests.Recusa_de_negocio_lancada_dentro_da_transacao_vira_mensagem`.
