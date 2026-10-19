using System.Data;
using LabControl.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MySqlConnector;

namespace LabControl.Infra;

public sealed record RevisaoGerada(int IdNovo, int Revisao);
public sealed record RevisaoAmostra(int IdNova, int Revisao, int? IdResultadoNovo, int? IdBoletimNovo);
public sealed record RevisaoResultado(int IdNovo, int Revisao, int? IdBoletimNovo);
public sealed record PedidoGerado(int IdPedido, int NrPedido);

/// <summary>
/// Chamadas das procedures do banco (docs/processos/P22-erros-transacoes.md e MDs em docs/banco/objetos/rotinas).
/// - Confere antes que o registro de origem é da empresa ativa (as procedures recebem só IDs).
/// - Usuário (<c>p_id_usuario</c>) e profissional (<c>p_id_func</c>) vêm do <see cref="IEmpresaContexto"/>.
/// - SQLSTATE 45000 vira <see cref="Resultado"/> com a mensagem; erro técnico continua subindo.
/// - Procedures com transação própria são recusadas dentro de <c>BeginTransaction</c> (o START TRANSACTION
///   delas faria commit implícito da transação da aplicação).
/// - Cada procedure bem-sucedida gera um evento em AUDITORIA (R, M, D, I/A do pedido…).
/// Depois de chamar, recarregue as entidades afetadas (<c>Entry(x).ReloadAsync()</c> ou nova consulta).
/// </summary>
public sealed class Procedures(LabDbContext db, IEmpresaContexto empresa, IAuditoria auditoria)
{
    // ------------------------------------------------------------------ com transação própria

    public async Task<Resultado<RevisaoGerada>> GerarRevisaoPropostaAsync(int idOrigem, string motivo)
    {
        if (!await db.LabPropostas.AnyAsync(x => x.IdLabPropostas == idOrigem)) return Resultado.Erro("Proposta não encontrada.");
        var r = await ChamarAsync("SP_GERAR_REVISAO_PROPOSTA", true,
            [("p_id_origem", idOrigem), ("p_motivo", motivo), ("p_id_usuario", empresa.UsuarioId)], "p_id_nova", "p_revisao");
        if (!r.Sucesso) return Resultado.Erro(r.Mensagem!);
        var rev = new RevisaoGerada(Int(r.Valor["p_id_nova"]), Int(r.Valor["p_revisao"]));
        await auditoria.RegistrarAsync(AcoesAuditoria.Revisao, "LAB_PROPOSTAS", rev.IdNovo.ToString(),
            new { ID_ORIGEM = idOrigem }, new { REVISAO = rev.Revisao, MOTIVO = motivo });
        return Resultado.Ok(rev);
    }

    public async Task<Resultado<RevisaoAmostra>> GerarRevisaoAmostraAsync(int idOrigem, string motivo)
    {
        if (!await db.LabHistAmostras.AnyAsync(x => x.IdLabHistAmostras == idOrigem)) return Resultado.Erro("Amostra não encontrada.");
        if (empresa.ProfissionalId is not { } func) return SemProfissional();
        var r = await ChamarAsync("SP_GERAR_REVISAO_AMOSTRA", true,
            [("p_id_origem", idOrigem), ("p_motivo", motivo), ("p_id_func", func), ("p_id_usuario", empresa.UsuarioId)],
            "p_id_nova", "p_revisao", "p_id_resultado_novo", "p_id_boletim_novo");
        if (!r.Sucesso) return Resultado.Erro(r.Mensagem!);
        var rev = new RevisaoAmostra(Int(r.Valor["p_id_nova"]), Int(r.Valor["p_revisao"]),
                                     IntOuNulo(r.Valor["p_id_resultado_novo"]), IntOuNulo(r.Valor["p_id_boletim_novo"]));
        await auditoria.RegistrarAsync(AcoesAuditoria.Revisao, "LAB_HIST_AMOSTRAS", rev.IdNova.ToString(), new { ID_ORIGEM = idOrigem },
            new { REVISAO = rev.Revisao, MOTIVO = motivo, ID_RESULTADO_NOVO = rev.IdResultadoNovo, ID_BOLETIM_NOVO = rev.IdBoletimNovo });
        return Resultado.Ok(rev);
    }

    public async Task<Resultado<RevisaoResultado>> GerarRevisaoResultadoAsync(int idOrigem, string motivo, bool gerarBoletim = true)
    {
        if (!await db.LabResultadosAnalises.AnyAsync(x => x.IdLabResultadosAnalises == idOrigem)) return Resultado.Erro("Resultado não encontrado.");
        if (empresa.ProfissionalId is not { } func) return SemProfissional();
        var r = await ChamarAsync("SP_GERAR_REVISAO_RESULTADO", true,
            [("p_id_origem", idOrigem), ("p_motivo", motivo), ("p_id_func", func), ("p_id_usuario", empresa.UsuarioId),
             ("p_gerar_boletim", gerarBoletim ? 1 : 0)],
            "p_id_novo", "p_revisao", "p_id_boletim_novo");
        if (!r.Sucesso) return Resultado.Erro(r.Mensagem!);
        var rev = new RevisaoResultado(Int(r.Valor["p_id_novo"]), Int(r.Valor["p_revisao"]), IntOuNulo(r.Valor["p_id_boletim_novo"]));
        await auditoria.RegistrarAsync(AcoesAuditoria.Revisao, "LAB_RESULTADOS_ANALISES", rev.IdNovo.ToString(), new { ID_ORIGEM = idOrigem },
            new { REVISAO = rev.Revisao, MOTIVO = motivo, ID_BOLETIM_NOVO = rev.IdBoletimNovo });
        return Resultado.Ok(rev);
    }

    public async Task<Resultado<RevisaoGerada>> GerarRevisaoBoletimAsync(int idOrigem, string motivo)
    {
        if (!await db.LabBoletimAnalises.AnyAsync(x => x.IdLabBoletimAnalises == idOrigem)) return Resultado.Erro("Boletim não encontrado.");
        if (empresa.ProfissionalId is not { } func) return SemProfissional();
        var r = await ChamarAsync("SP_GERAR_REVISAO_BOLETIM", true,
            [("p_id_origem", idOrigem), ("p_motivo", motivo), ("p_id_func", func), ("p_id_usuario", empresa.UsuarioId)],
            "p_id_novo", "p_revisao");
        if (!r.Sucesso) return Resultado.Erro(r.Mensagem!);
        var rev = new RevisaoGerada(Int(r.Valor["p_id_novo"]), Int(r.Valor["p_revisao"]));
        await auditoria.RegistrarAsync(AcoesAuditoria.Revisao, "LAB_BOLETIM_ANALISES", rev.IdNovo.ToString(),
            new { ID_ORIGEM = idOrigem }, new { REVISAO = rev.Revisao, MOTIVO = motivo });
        return Resultado.Ok(rev);
    }

    public async Task<Resultado> ExcluirRevisaoResultadoAsync(int idResultado)
    {
        if (!await db.LabResultadosAnalises.AnyAsync(x => x.IdLabResultadosAnalises == idResultado)) return Resultado.Erro("Resultado não encontrado.");
        return await Auditado(await ChamarSemSaidaAsync("SP_EXCLUIR_REVISAO_RESULTADO", true, ("p_id_resultado", idResultado)),
            AcoesAuditoria.DesfazerRevisao, "LAB_RESULTADOS_ANALISES", idResultado, null, null);
    }

    public async Task<Resultado> ExcluirRevisaoBoletimAsync(int idBoletim)
    {
        if (!await db.LabBoletimAnalises.AnyAsync(x => x.IdLabBoletimAnalises == idBoletim)) return Resultado.Erro("Boletim não encontrado.");
        return await Auditado(await ChamarSemSaidaAsync("SP_EXCLUIR_REVISAO_BOLETIM", true, ("p_id_boletim", idBoletim)),
            AcoesAuditoria.DesfazerRevisao, "LAB_BOLETIM_ANALISES", idBoletim, null, null);
    }

    public async Task<Resultado> EmitirBoletimAsync(int idBoletim, string arquivoPdf, string hashPdf)
    {
        if (!await db.LabBoletimAnalises.AnyAsync(x => x.IdLabBoletimAnalises == idBoletim)) return Resultado.Erro("Boletim não encontrado.");
        if (empresa.ProfissionalId is not { } func) return SemProfissional();
        return await Auditado(await ChamarSemSaidaAsync("SP_EMITIR_BOLETIM", true,
                ("p_id_boletim", idBoletim), ("p_id_func", func), ("p_arquivo", arquivoPdf), ("p_hash", hashPdf)),
            AcoesAuditoria.Emissao, "LAB_BOLETIM_ANALISES", idBoletim, null, new { ARQUIVO_PDF = arquivoPdf, HASH_PDF = hashPdf });
    }

    public async Task<Resultado<PedidoGerado>> GerarPedidoVendaAsync(int idProposta, DateOnly? dtAutorizacao, string? autorizadoPor, string? nrPedidoCliente)
    {
        if (!await db.LabPropostas.AnyAsync(x => x.IdLabPropostas == idProposta)) return Resultado.Erro("Proposta não encontrada.");
        var r = await ChamarAsync("SP_GERAR_PEDIDO_VENDA", true,
            [("p_id_proposta", idProposta), ("p_dt_autorizacao", dtAutorizacao?.ToDateTime(TimeOnly.MinValue)),
             ("p_autorizado_por", autorizadoPor), ("p_nr_pedido_cliente", nrPedidoCliente), ("p_id_usuario", empresa.UsuarioId)],
            "p_id_pedido", "p_nr_pedido");
        if (!r.Sucesso) return Resultado.Erro(r.Mensagem!);
        var ped = new PedidoGerado(Int(r.Valor["p_id_pedido"]), Int(r.Valor["p_nr_pedido"]));
        await auditoria.RegistrarAsync(AcoesAuditoria.Inclusao, "PEDIDOS_VENDA", ped.IdPedido.ToString(), null,
            new { NR_PEDIDO = ped.NrPedido, ID_LAB_PROPOSTAS = idProposta, DT_AUTORIZACAO = dtAutorizacao, AUTORIZADO_POR = autorizadoPor, NR_PEDIDO_CLIENTE = nrPedidoCliente });
        return Resultado.Ok(ped);
    }

    public async Task<Resultado> AtualizarPedidoRevisaoAsync(int idPedido)
    {
        if (!await db.PedidosVenda.AnyAsync(x => x.IdPedidosVenda == idPedido)) return Resultado.Erro("Pedido de venda não encontrado.");
        return await Auditado(await ChamarSemSaidaAsync("SP_ATUALIZAR_PEDIDO_REVISAO", true, ("p_id_pedido", idPedido), ("p_id_usuario", empresa.UsuarioId)),
            AcoesAuditoria.Alteracao, "PEDIDOS_VENDA", idPedido, null, new { ATUALIZADO_PARA = "ULTIMA_REVISAO_DA_PROPOSTA" });
    }

    public async Task<Resultado> TrocarProdutoPropostaAsync(int idProposta, int idProdutoVelho, int idProdutoNovo)
    {
        if (!await db.LabPropostas.AnyAsync(x => x.IdLabPropostas == idProposta)) return Resultado.Erro("Proposta não encontrada.");
        return await Auditado(await ChamarSemSaidaAsync("SP_TROCAR_PRODUTO_PROPOSTA", true,
                ("p_id_proposta", idProposta), ("p_id_prod_velho", idProdutoVelho), ("p_id_prod_novo", idProdutoNovo)),
            AcoesAuditoria.Alteracao, "LAB_PROPOSTAS", idProposta, new { ID_PRODUTOS = idProdutoVelho }, new { ID_PRODUTOS = idProdutoNovo });
    }

    // ------------------------------------------------------------------ sem transação (podem rodar dentro da transação da aplicação)

    /// <summary>Copia os parâmetros do item da proposta para a amostra. Chamar na mesma transação da gravação da amostra.</summary>
    public async Task<Resultado> AmostraGerarTestesAsync(int idAmostra)
    {
        if (!await db.LabHistAmostras.AnyAsync(x => x.IdLabHistAmostras == idAmostra)) return Resultado.Erro("Amostra não encontrada.");
        return await ChamarSemSaidaAsync("SP_AMOSTRA_GERAR_TESTES", false, ("p_id_amostra", idAmostra));
    }

    public async Task<Resultado> RecalcularPropostaAsync(int idProposta)
    {
        if (!await db.LabPropostas.AnyAsync(x => x.IdLabPropostas == idProposta)) return Resultado.Erro("Proposta não encontrada.");
        return await ChamarSemSaidaAsync("SP_RECALCULAR_PROPOSTA", false, ("p_id_proposta", idProposta));
    }

    /// <summary>Cadastros padrão de uma empresa nova (operação de ADMIN_GLOBAL: a empresa ainda não é a ativa).</summary>
    public Task<Resultado> InicializarEmpresaAsync(int idEmpresa) =>
        ChamarSemSaidaAsync("SP_INICIALIZAR_EMPRESA", false, ("p_id_empresa", idEmpresa));

    /// <summary>
    /// Libera (ou volta a bloquear) a data de entrega além de DIAS_RETROATIVOS_ENTREGA. Só depois da confirmação do
    /// usuário E da permissão AMOSTRAS.ENTREGA_RETROATIVA. Exige transação aberta: a variável vale para a conexão,
    /// e fora de transação o EF pode abrir outra conexão (o interceptor zera a variável).
    /// </summary>
    public async Task DefinirPermiteRetroativoAsync(bool permitir)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("@LC_PERMITE_RETROATIVO só pode ser definido dentro da transação da gravação da amostra.");
        await db.Database.ExecuteSqlRawAsync(permitir ? "SET @LC_PERMITE_RETROATIVO = 1" : "SET @LC_PERMITE_RETROATIVO = 0");
    }

    // ------------------------------------------------------------------ núcleo

    private async Task<Resultado> Auditado(Resultado r, string acao, string tabela, int id, object? antes, object? depois)
    {
        if (r.Sucesso) await auditoria.RegistrarAsync(acao, tabela, id.ToString(), antes, depois);
        return r;
    }

    private static Falha SemProfissional() =>
        Resultado.Erro("Seu usuário não está vinculado a um profissional nesta empresa (cadastro de usuários).");

    private async Task<Resultado> ChamarSemSaidaAsync(string procedure, bool transacaoPropria, params (string Nome, object? Valor)[] entrada)
    {
        var r = await ChamarAsync(procedure, transacaoPropria, entrada);
        return r.Sucesso ? Resultado.Ok() : Resultado.Erro(r.Mensagem!);
    }

    private async Task<Resultado<Dictionary<string, object?>>> ChamarAsync(
        string procedure, bool transacaoPropria, (string Nome, object? Valor)[] entrada, params string[] saida)
    {
        if (transacaoPropria && db.Database.CurrentTransaction is not null)
            throw new InvalidOperationException(
                $"{procedure} abre transação própria e não pode ser chamada dentro de BeginTransaction (docs/processos/P22-erros-transacoes.md).");

        await db.Database.OpenConnectionAsync(); // passa pelo SessaoMySqlInterceptor
        try
        {
            await using var cmd = db.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = procedure;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
            foreach (var (nome, valor) in entrada)
                cmd.Parameters.Add(new MySqlParameter(nome, valor ?? DBNull.Value));
            var outs = saida.Select(n => new MySqlParameter(n, MySqlDbType.Int32) { Direction = ParameterDirection.Output }).ToList();
            cmd.Parameters.AddRange(outs.ToArray());

            await cmd.ExecuteNonQueryAsync();
            return Resultado.Ok(outs.ToDictionary(p => p.ParameterName, p => p.Value is DBNull ? null : p.Value));
        }
        catch (MySqlException ex) when (ex.SqlState == ErrosMySql.SqlStateNegocio)
        {
            return Resultado.Erro(ex.Message);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private static int Int(object? v) => Convert.ToInt32(v ?? throw new InvalidOperationException("Procedure não devolveu o valor de saída."));
    private static int? IntOuNulo(object? v) => v is null ? null : Convert.ToInt32(v);
}
