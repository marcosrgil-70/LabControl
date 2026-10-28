using System.Globalization;
using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services;

/// <summary>
/// T23 — Movimentação de amostras (tarefa 7.1; P12, RN-06). Os movimentos são sempre da amostra física (raiz, revisão 0):
/// o <c>TG_MOV_BI</c> redireciona e recusa saída sem saldo, e o <c>PR_ATUALIZA_SALDO</c> recalcula o saldo. Aqui ficam o
/// que o banco não faz: justificativa na saída, data não futura, testes da própria amostra, profissional do usuário e a
/// trava da linha do saldo (duas saídas ao mesmo tempo não passam juntas pela conferência do saldo).
/// </summary>
public sealed class MovimentacaoService(LabDbContext db, IEmpresaContexto ctx)
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static string Qtde(decimal valor) => valor.ToString("0.####", PtBr);

    public async Task<MovimentacaoListaVM> ListarAsync(MovimentacaoFiltroVM f)
    {
        if (!f.Informado) return new MovimentacaoListaVM { Filtro = f, Itens = [] };
        var q = db.VwLabAmostras.AsNoTracking().Where(a => a.UltimaRevisao);
        if (Textos.Limpo(f.Cliente) is { } c)
            q = q.Where(a => a.ClienteNome.Contains(c) || (a.ClienteFantasia != null && a.ClienteFantasia.Contains(c))
                             || (a.ClienteCpfCnpj != null && a.ClienteCpfCnpj.Contains(c)));
        if (Textos.Limpo(f.Proposta) is { } pr) q = q.Where(a => a.NrProposta.Contains(pr));
        if (Textos.Limpo(f.Numero) is { } n) q = q.Where(a => a.NrAmostra.Contains(n));

        var total = await q.CountAsync();
        f.Pagina = Paginacao.Ajustar(f.Pagina, total, MovimentacaoFiltroVM.TamanhoPagina);
        var itens = await q.OrderByDescending(a => a.AnoAmostra).ThenByDescending(a => a.CodAmostra)
            .Skip((f.Pagina - 1) * MovimentacaoFiltroVM.TamanhoPagina).Take(MovimentacaoFiltroVM.TamanhoPagina)
            .Select(a => new MovimentacaoAmostraItemVM(a.IdLabHistAmostras, a.NrAmostra, a.DtEntrega, a.ClienteNome, a.ProdutoDescricao, a.NrProposta,
                a.NrLote, a.Fabricacao, a.Validade, a.TipoAmostra, a.TipoAnalise, a.StatusAmostra, a.CorStatus, a.SaldoAtual ?? 0))
            .ToListAsync();
        return new MovimentacaoListaVM { Filtro = f, Itens = itens, Total = total };
    }

    /// <summary>Painel da amostra: histórico e saldo da amostra física; testes da última revisão para a saída.</summary>
    public async Task<MovimentacaoAmostraVM?> CarregarAsync(int id, MovimentoNovoVM? novo = null)
    {
        var a = await db.VwLabAmostras.AsNoTracking().Where(x => x.IdLabHistAmostras == id)
            .Select(x => new { x.NrAmostra, x.ClienteNome, x.ProdutoDescricao, x.NrLote, x.Fabricacao, x.Validade, x.Embalagem, x.SaldoAtual, x.UltimaRevisao, x.IdAmostraRaiz })
            .SingleOrDefaultAsync();
        if (a is null) return null;
        var raiz = (int)a.IdAmostraRaiz;

        var movimentos = await db.LabMovAmostras.AsNoTracking().Where(m => m.IdLabHistAmostras == raiz)
            .OrderByDescending(m => m.DataMov).ThenByDescending(m => m.IdLabMovAmostras)
            .Select(m => new { m.IdLabMovAmostras, m.DataMov, m.ES, m.Qtde, m.Justificativa, Profissional = m.EntidadesFunc.Entidade.Nome, m.Automatica })
            .ToListAsync();
        var idsMov = movimentos.Select(m => m.IdLabMovAmostras).ToList();
        var atendidos = await db.LabMovAmostrasParam.AsNoTracking().Where(p => idsMov.Contains(p.IdLabMovAmostras))
            .Select(p => new { p.IdLabMovAmostras, p.LabHistAmostraTeste.IdLabParametrosAnalises, p.LabHistAmostraTeste.LabParametroAnalise.Descricao })
            .ToListAsync();

        // testes da última revisão da amostra física (as revisões copiam os testes; o "já atendido" vale pelo parâmetro)
        var parametros = await db.LabHistAmostrasTestes.AsNoTracking()
            .Where(t => (t.LabHistAmostra.IdLabHistAmostras == raiz || t.LabHistAmostra.IdAmostraRaiz == raiz) && t.LabHistAmostra.UltimaRevisao)
            .OrderBy(t => t.LabParametroAnalise.Descricao)
            .Select(t => new { t.IdLabHistAmostrasTestes, t.IdLabParametrosAnalises, t.LabParametroAnalise.Descricao, Tipo = t.AnaliseTipo.Descricao })
            .ToListAsync();
        var jaAtendidos = atendidos.Select(p => p.IdLabParametrosAnalises).ToHashSet();

        return new MovimentacaoAmostraVM
        {
            Id = id, NrAmostra = a.NrAmostra, Cliente = a.ClienteNome, Produto = a.ProdutoDescricao, Lote = a.NrLote, Fabricacao = a.Fabricacao,
            Validade = a.Validade, Embalagem = a.Embalagem, Saldo = a.SaldoAtual ?? 0, UltimaRevisao = a.UltimaRevisao,
            Movimentos = movimentos.Select(m => new MovimentoVM(m.IdLabMovAmostras, m.DataMov, m.ES, m.Qtde, m.Justificativa, m.Profissional, m.Automatica,
                atendidos.Where(p => p.IdLabMovAmostras == m.IdLabMovAmostras).Select(p => p.Descricao).Order().ToList())).ToList(),
            Parametros = parametros.Select(t => new MovimentacaoParametroVM(t.IdLabHistAmostrasTestes, t.Descricao, t.Tipo, jaAtendidos.Contains(t.IdLabParametrosAnalises))).ToList(),
            Novo = novo ?? new MovimentoNovoVM { IdAmostra = id, DataMov = Minuto(await db.AgoraAsync()) },
        };
    }

    /// <summary>Entrada complementar ou saída na amostra física (com os parâmetros atendidos pela saída).</summary>
    public async Task<Resultado<int>> IncluirAsync(MovimentoNovoVM vm)
    {
        if (ctx.ProfissionalId is not { } profissional)
            return Resultado.Erro("Seu usuário não está vinculado a um profissional nesta empresa (cadastro de usuários): a movimentação registra quem fez.");
        var raiz = await RaizAsync(vm.IdAmostra);
        if (raiz is null) return Resultado.Erro(AmostraService.MensagemNaoEncontrada);
        if (vm.Tipo is not (EntradaSaida.Entrada or EntradaSaida.Saida)) return Resultado.Erro("Escolha entrada ou saída.", nameof(vm.Tipo));
        if (!Textos.Decimal(Textos.Limpo(vm.Qtde) ?? "", out var qtde) || qtde <= 0 || decimal.Round(qtde, 4) != qtde || qtde >= 100_000_000_000m)
            return Resultado.Erro("Quantidade: número maior que zero.", nameof(vm.Qtde));
        var agora = await db.AgoraAsync();
        var data = vm.DataMov ?? agora;
        if (data > agora.AddMinutes(1)) return Resultado.Erro("A data da movimentação não pode ser futura.", nameof(vm.DataMov));
        var justificativa = Textos.Limpo(vm.Justificativa);
        if (vm.Tipo == EntradaSaida.Saida && justificativa is null) return Resultado.Erro("Informe a justificativa da saída.", nameof(vm.Justificativa));
        if (justificativa?.Length > 250) return Resultado.Erro("Justificativa: até 250 caracteres.", nameof(vm.Justificativa));

        var testes = vm.Testes.Distinct().ToList();
        if (testes.Count > 0)
        {
            if (vm.Tipo != EntradaSaida.Saida) return Resultado.Erro("Os parâmetros atendidos são indicados só na saída.");
            var daAmostra = await db.LabHistAmostrasTestes.CountAsync(t => testes.Contains(t.IdLabHistAmostrasTestes)
                && (t.LabHistAmostra.IdLabHistAmostras == raiz || t.LabHistAmostra.IdAmostraRaiz == raiz));
            if (daAmostra != testes.Count) return Resultado.Erro("Parâmetro que não é desta amostra.");
        }

        var r = await ErrosMySql.CapturarAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            var saldo = await TravarSaldoAsync(raiz.Value);
            if (vm.Tipo == EntradaSaida.Saida && saldo < qtde)
                throw new ErroDeNegocioException($"Saldo insuficiente da amostra para esta saída (saldo atual: {Qtde(saldo)}).");
            var mov = new LabMovAmostra
            {
                IdLabHistAmostras = raiz.Value, DataMov = Minuto(data), Qtde = qtde, ES = vm.Tipo, Justificativa = justificativa,
                IdEntidadesFunc = profissional,
            };
            db.LabMovAmostras.Add(mov);
            foreach (var t in testes) db.LabMovAmostrasParam.Add(new LabMovAmostraParam { LabMovAmostra = mov, IdLabHistAmostrasTestes = t });
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return mov.IdLabMovAmostras;
        });
        if (!r.Sucesso) db.ChangeTracker.Clear();
        return r;
    }

    /// <summary>Exclui movimento manual; a entrada automática do recebimento fica (o banco também recusa).</summary>
    public async Task<Resultado> ExcluirAsync(int idMovimento)
    {
        var mov = await db.LabMovAmostras.SingleOrDefaultAsync(m => m.IdLabMovAmostras == idMovimento);
        if (mov is null) return Resultado.Erro("Movimentação não encontrada (pode ter sido excluída).");
        if (mov.Automatica) return Resultado.Erro("A entrada automática do recebimento não pode ser excluída.");

        var r = await ErrosMySql.CapturarAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            var saldo = await TravarSaldoAsync(mov.IdLabHistAmostras);
            // sem entrada, a saída que ela cobria deixaria o saldo negativo
            if (mov.ES == EntradaSaida.Entrada && saldo < mov.Qtde)
                throw new ErroDeNegocioException($"Esta entrada não pode ser excluída: o saldo da amostra ficaria negativo (saldo atual: {Qtde(saldo)}).");
            db.LabMovAmostras.Remove(mov);   // os parâmetros atendidos saem junto (ON DELETE CASCADE)
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        });
        if (!r.Sucesso) db.ChangeTracker.Clear();
        return r;
    }

    // amostra física da amostra (ou revisão) da empresa ativa
    private async Task<int?> RaizAsync(int idAmostra) =>
        await db.LabHistAmostras.Where(a => a.IdLabHistAmostras == idAmostra).Select(a => (int?)(a.IdAmostraRaiz ?? a.IdLabHistAmostras)).SingleOrDefaultAsync();

    /// <summary>Trava a linha do saldo até o fim da transação: a conferência e a gravação ficam juntas.</summary>
    private async Task<decimal> TravarSaldoAsync(int raiz) =>
        (await db.Database.SqlQueryRaw<decimal>(
            "SELECT SALDO_ATUAL AS Value FROM LAB_HIST_AMOSTRAS_SALDO WHERE ID_LAB_HIST_AMOSTRAS = {0} FOR UPDATE", raiz).ToListAsync())
        .SingleOrDefault();

    private static DateTime Minuto(DateTime d) => new(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0, d.Kind);
}
