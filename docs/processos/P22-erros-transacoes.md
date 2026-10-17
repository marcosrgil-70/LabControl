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

## Padrão de serviço (C#)
```csharp
public async Task<Resultado<int>> IncluirAmostra(AmostraVM vm) {
    var erro = Validar(vm); if (erro != null) return Resultado.Erro(erro);       // validações de tela
    await using var tx = await db.Database.BeginTransactionAsync();
    try {
        if (vm.RetroativaAutorizada) await db.Database.ExecuteSqlRawAsync("SET @LC_PERMITE_RETROATIVO = 1");
        db.Amostras.Add(map(vm)); await db.SaveChangesAsync();                       // trigger numera
        await db.Database.ExecuteSqlInterpolatedAsync($"CALL SP_AMOSTRA_GERAR_TESTES({id})");
        await tx.CommitAsync();
    } catch (MySqlException ex) when (ex.SqlState == "45000") { await tx.RollbackAsync(); return Resultado.Erro(ex.Message); }
    finally { await db.Database.ExecuteSqlRawAsync("SET @LC_PERMITE_RETROATIVO = 0"); }
    await db.Entry(amostra).ReloadAsync();                                         // número gerado pelo banco
    return Resultado.Ok(amostra.Id);
}
```

## Variáveis de sessão
| Variável | Quem define | Quando |
|---|---|---|
| `@LC_ID_USUARIOS` | `SessaoMySqlInterceptor` | toda abertura de conexão |
| `@LC_MIGRACAO` | interceptor (= 0) / MigracaoService (= 1) | sempre 0, exceto na migração |
| `@LC_PERMITE_RETROATIVO` | interceptor (= 0) / AmostraService (= 1) | só no request autorizado |
| `@LC_DESFAZENDO` | as próprias `SP_EXCLUIR_REVISAO_*` | interno |

## Concorrência
Numeração e revisões usam `SELECT … FOR UPDATE` no banco; telas de edição usam controle otimista por `ALTERADO_EM` (token de concorrência) para avisar "registro alterado por outro usuário".

## Cenários de teste (projeto xUnit da fase 0)
- Recriar banco de teste pelos scripts e rodar o roteiro do doc 12 §7 via serviço.
- Chamar `SP_GERAR_REVISAO_AMOSTRA` dentro de `BeginTransaction` em teste → documentar o efeito (commit implícito) e garantir que o serviço não faz isso.
- Mensagem 45000 chega à tela com o texto exato.
