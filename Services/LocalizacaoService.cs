using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services;

/// <summary>
/// T24 — Localização das amostras: arquivo e descarte (tarefa 7.2; P13, RN-06.11…14). Uma localização por amostra
/// física (raiz, <c>UQ_LOCAL_AMO</c>). A <c>LAB_LOCAL_AMOSTRAS</c> não tem trigger: é aqui que a revisão vira a raiz.
/// O banco só garante a data do descarte (<c>CK_LOCAL_DESC</c>); o resto (datas não futuras, profissional do descarte,
/// desfazer descarte limpando data e profissional) é do serviço.
/// </summary>
public sealed class LocalizacaoService(LabDbContext db, IEmpresaContexto ctx, AmostraService amostras)
{
    public const int MaxLote = 200;
    public const string StatusReprovadaDescartada = "REPROVADA - DESCARTADA";

    public async Task<LocalizacaoListaVM> ListarAsync(LocalizacaoFiltroVM f)
    {
        var lote = new LocalizacaoLoteVM { DtHrArquivo = Minuto(await db.AgoraAsync()) };
        if (!f.Informado) return new LocalizacaoListaVM { Filtro = f, Itens = [], Lote = lote };

        var q = from a in db.VwLabAmostras.AsNoTracking().Where(a => a.UltimaRevisao)
                join l in db.LabLocalAmostras.AsNoTracking() on a.IdAmostraRaiz equals (long)l.IdLabHistAmostras into ls
                from l in ls.DefaultIfEmpty()
                select new { a, l };
        if (Textos.Limpo(f.Cliente) is { } c)
            q = q.Where(x => x.a.ClienteNome.Contains(c) || (x.a.ClienteFantasia != null && x.a.ClienteFantasia.Contains(c))
                             || (x.a.ClienteCpfCnpj != null && x.a.ClienteCpfCnpj.Contains(c)));
        if (Textos.Limpo(f.Produto) is { } pd) q = q.Where(x => x.a.ProdutoCodigo.Contains(pd) || x.a.ProdutoDescricao.Contains(pd));
        if (Textos.Limpo(f.Numero) is { } n) q = q.Where(x => x.a.NrAmostra.Contains(n));
        q = f.Situacao switch
        {
            SituacaoLocalizacao.Arquivada => q.Where(x => x.l != null && x.l.Status == SituacaoArquivo.Arquivada),
            SituacaoLocalizacao.Descartada => q.Where(x => x.l != null && x.l.Status == SituacaoArquivo.Descartada),
            SituacaoLocalizacao.SemLocalizacao => q.Where(x => x.l == null),
            _ => q,
        };
        if (f.De is { } de) { var d = de.ToDateTime(TimeOnly.MinValue); q = q.Where(x => x.l != null && x.l.DtHrArquivo >= d); }
        if (f.Ate is { } ate) { var d = ate.AddDays(1).ToDateTime(TimeOnly.MinValue); q = q.Where(x => x.l != null && x.l.DtHrArquivo < d); }
        if (Textos.Limpo(f.Armario) is { } ar) q = q.Where(x => x.l != null && x.l.NrArmario != null && x.l.NrArmario.Contains(ar));
        if (Textos.Limpo(f.Prateleira) is { } pr) q = q.Where(x => x.l != null && x.l.NrPrateleira != null && x.l.NrPrateleira.Contains(pr));
        if (Textos.Limpo(f.Caixa) is { } cx) q = q.Where(x => x.l != null && x.l.NrCaixa != null && x.l.NrCaixa.Contains(cx));

        var total = await q.CountAsync();
        f.Pagina = Paginacao.Ajustar(f.Pagina, total, LocalizacaoFiltroVM.TamanhoPagina);
        var itens = await q.OrderByDescending(x => x.a.AnoAmostra).ThenByDescending(x => x.a.CodAmostra)
            .Skip((f.Pagina - 1) * LocalizacaoFiltroVM.TamanhoPagina).Take(LocalizacaoFiltroVM.TamanhoPagina)
            .Select(x => new LocalizacaoItemVM(x.a.IdLabHistAmostras, x.a.NrAmostra, x.a.ClienteNome, x.a.ClienteCpfCnpj, x.a.ProdutoDescricao,
                x.l == null ? null : x.l.Status, x.l == null ? null : x.l.DtHrArquivo, x.l == null ? null : x.l.NrArmario,
                x.l == null ? null : x.l.NrPrateleira, x.l == null ? null : x.l.NrCaixa, x.l == null ? null : x.l.DtHrDescarte,
                x.l == null || x.l.EntidadesFuncDescarte == null ? null : x.l.EntidadesFuncDescarte.Entidade.Nome))
            .ToListAsync();
        return new LocalizacaoListaVM { Filtro = f, Itens = itens, Total = total, Lote = lote };
    }

    /// <summary>Pesquisa de amostras (T29) para localizar: última revisão, ainda sem localização.</summary>
    public async Task<AmostraListaVM> PesquisarAmostrasAsync(AmostraFiltroVM f, bool inicial)
    {
        if (inicial) f.APartirDe = await amostras.APartirDePadraoAsync();
        (f.SoUltima, f.ApenasSemLocalizacao, f.OrdenarPorNumero) = (true, true, true);
        return await amostras.ListarAsync(f);
    }

    /// <summary>Localização da amostra (a existente, ou uma nova com a data de agora). <paramref name="digitado"/> = recusa a reexibir.</summary>
    public async Task<LocalizacaoVM?> CarregarAsync(int idAmostra, LocalizacaoVM? digitado = null)
    {
        var a = await db.VwLabAmostras.AsNoTracking().Where(x => x.IdLabHistAmostras == idAmostra)
            .Select(x => new { x.NrAmostra, x.ClienteNome, x.ClienteCpfCnpj, x.ProdutoDescricao, x.StatusAmostra, x.UltimaRevisao, x.IdAmostraRaiz })
            .SingleOrDefaultAsync();
        if (a is null) return null;
        var raiz = (int)a.IdAmostraRaiz;
        var l = await db.LabLocalAmostras.AsNoTracking().Where(x => x.IdLabHistAmostras == raiz)
            .Select(x => new { x.IdLabLocalAmostras, x.Status, x.DtHrArquivo, x.NrArmario, x.NrPrateleira, x.NrCaixa, x.Observacao, x.DtHrDescarte,
                Profissional = x.EntidadesFuncDescarte == null ? null : x.EntidadesFuncDescarte.Entidade.Nome })
            .SingleOrDefaultAsync();

        var vm = digitado ?? (l is null
            ? new LocalizacaoVM { DtHrArquivo = Minuto(await db.AgoraAsync()) }
            : new LocalizacaoVM
            {
                Id = l.IdLabLocalAmostras, Status = l.Status, DtHrArquivo = l.DtHrArquivo, Armario = l.NrArmario, Prateleira = l.NrPrateleira,
                Caixa = l.NrCaixa, Observacao = l.Observacao, DtHrDescarte = l.DtHrDescarte,
            });
        vm.IdAmostra = idAmostra;
        (vm.NrAmostra, vm.Cliente, vm.CpfCnpj, vm.Produto, vm.StatusAmostra, vm.UltimaRevisao) =
            (a.NrAmostra, a.ClienteNome, a.ClienteCpfCnpj, a.ProdutoDescricao, a.StatusAmostra, a.UltimaRevisao);
        vm.ProfissionalDescarte = l?.Profissional;
        vm.TemStatusReprovada = await StatusReprovadaAsync() is not null;
        return vm;
    }

    /// <summary>Inclui ou altera a localização (arquivo e descarte). Voltar de descartada para arquivada desfaz o descarte.</summary>
    public async Task<Resultado<int>> GravarAsync(LocalizacaoVM vm)
    {
        var raiz = await RaizAsync(vm.IdAmostra);
        if (raiz is null) return Resultado.Erro(AmostraService.MensagemNaoEncontrada);
        var atual = await db.LabLocalAmostras.SingleOrDefaultAsync(l => l.IdLabHistAmostras == raiz);
        if (vm.Id == 0 && atual is not null)
            return Resultado.Erro("Esta amostra já foi localizada por outro usuário. Abra a amostra de novo para alterar.");
        if (vm.Id != 0 && atual?.IdLabLocalAmostras != vm.Id) return Resultado.Erro("Localização não encontrada (pode ter sido excluída).");
        if (vm.Status is not (SituacaoArquivo.Arquivada or SituacaoArquivo.Descartada)) return Resultado.Erro("Escolha a situação.", nameof(vm.Status));

        var agora = await db.AgoraAsync();
        if (vm.DtHrArquivo is not { } arquivo) return Resultado.Erro("Informe a data e a hora do arquivo.", nameof(vm.DtHrArquivo));
        if (arquivo > agora.AddMinutes(1)) return Resultado.Erro("A data do arquivo não pode ser futura.", nameof(vm.DtHrArquivo));
        var (armario, prateleira, caixa, obs) = (Textos.Limpo(vm.Armario), Textos.Limpo(vm.Prateleira), Textos.Limpo(vm.Caixa), Textos.Limpo(vm.Observacao));
        if (armario?.Length > 15 || prateleira?.Length > 15 || caixa?.Length > 15) return Resultado.Erro("Armário, prateleira e caixa: até 15 caracteres cada.");
        if (obs?.Length > 250) return Resultado.Erro("Observação: até 250 caracteres.", nameof(vm.Observacao));

        DateTime? descarte = null;
        int? profissional = null;
        if (vm.Status == SituacaoArquivo.Descartada)
        {
            if (vm.DtHrDescarte is not { } d) return Resultado.Erro("Informe a data e a hora do descarte.", nameof(vm.DtHrDescarte));
            if (d > agora.AddMinutes(1)) return Resultado.Erro("A data do descarte não pode ser futura.", nameof(vm.DtHrDescarte));
            if (Minuto(d) < Minuto(arquivo)) return Resultado.Erro("A data do descarte não pode ser anterior à do arquivo.", nameof(vm.DtHrDescarte));
            descarte = Minuto(d);
            // quem descartou: o profissional do usuário; se já estava descartada, fica o de antes
            profissional = atual is { Status: SituacaoArquivo.Descartada, IdEntidadesFuncDescarte: { } antes } ? antes : ctx.ProfissionalId;
            if (profissional is null)
                return Resultado.Erro("Seu usuário não está vinculado a um profissional nesta empresa (cadastro de usuários): o descarte registra quem fez.");
        }
        int? statusReprovada = null;
        if (vm.MarcarReprovada && vm.Status == SituacaoArquivo.Descartada)
            statusReprovada = await StatusReprovadaAsync() ?? throw new InvalidOperationException("Status REPROVADA - DESCARTADA não existe na empresa.");

        var r = await ErrosMySql.CapturarAsync(async () =>
        {
            var l = atual ?? db.LabLocalAmostras.Add(new LabLocalAmostra { IdLabHistAmostras = raiz.Value }).Entity;
            (l.Status, l.DtHrArquivo, l.NrArmario, l.NrPrateleira, l.NrCaixa, l.Observacao) = (vm.Status, Minuto(arquivo), armario, prateleira, caixa, obs);
            // arquivada: sem data nem profissional do descarte (desfazer descarte — o legado deixava o status "descartada")
            (l.DtHrDescarte, l.IdEntidadesFuncDescarte) = (descarte, profissional);
            if (statusReprovada is { } st)
            {
                var ultima = await db.LabHistAmostras.SingleAsync(x => (x.IdLabHistAmostras == raiz || x.IdAmostraRaiz == raiz) && x.UltimaRevisao);
                ultima.IdAmostrasStatus = st;
            }
            await db.SaveChangesAsync();   // uma transação: localização + status da amostra
            return l.IdLabLocalAmostras;
        });
        if (!r.Sucesso) db.ChangeTracker.Clear();
        return r;
    }

    /// <summary>Arquivar em lote: mesma data, armário, prateleira e caixa; as que já têm localização ficam como estão.</summary>
    public async Task<Resultado<LoteArquivado>> ArquivarLoteAsync(LocalizacaoLoteVM vm)
    {
        var ids = vm.Amostras.Distinct().ToList();
        if (ids.Count == 0) return Resultado.Erro("Marque ao menos uma amostra.");
        if (ids.Count > MaxLote) return Resultado.Erro($"No máximo {MaxLote} amostras por vez.");
        var agora = await db.AgoraAsync();
        var arquivo = vm.DtHrArquivo ?? agora;
        if (arquivo > agora.AddMinutes(1)) return Resultado.Erro("A data do arquivo não pode ser futura.", nameof(vm.DtHrArquivo));
        var (armario, prateleira, caixa, obs) = (Textos.Limpo(vm.Armario), Textos.Limpo(vm.Prateleira), Textos.Limpo(vm.Caixa), Textos.Limpo(vm.Observacao));
        if (armario is null && prateleira is null && caixa is null) return Resultado.Erro("Informe o armário, a prateleira ou a caixa.");
        if (armario?.Length > 15 || prateleira?.Length > 15 || caixa?.Length > 15) return Resultado.Erro("Armário, prateleira e caixa: até 15 caracteres cada.");
        if (obs?.Length > 250) return Resultado.Erro("Observação: até 250 caracteres.");

        var marcadas = await db.LabHistAmostras.AsNoTracking().Where(a => ids.Contains(a.IdLabHistAmostras))
            .Select(a => new { a.NrAmostra, Raiz = a.IdAmostraRaiz ?? a.IdLabHistAmostras }).ToListAsync();
        if (marcadas.Count != ids.Count) return Resultado.Erro("Amostra não encontrada (pode ter sido excluída).");
        var raizes = marcadas.Select(m => m.Raiz).Distinct().ToList();
        var localizadas = (await db.LabLocalAmostras.Where(l => raizes.Contains(l.IdLabHistAmostras)).Select(l => l.IdLabHistAmostras).ToListAsync()).ToHashSet();
        var jaLocalizadas = marcadas.Where(m => localizadas.Contains(m.Raiz)).Select(m => m.NrAmostra).Order().ToList();
        var novas = raizes.Where(r => !localizadas.Contains(r)).ToList();
        if (novas.Count == 0) return Resultado.Erro($"Todas as amostras marcadas já têm localização: {string.Join(", ", jaLocalizadas)}.");

        var r = await ErrosMySql.CapturarAsync(async () =>
        {
            foreach (var raiz in novas)
                db.LabLocalAmostras.Add(new LabLocalAmostra
                {
                    IdLabHistAmostras = raiz, Status = SituacaoArquivo.Arquivada, DtHrArquivo = Minuto(arquivo),
                    NrArmario = armario, NrPrateleira = prateleira, NrCaixa = caixa, Observacao = obs,
                });
            await db.SaveChangesAsync();   // todas ou nenhuma
            return new LoteArquivado(novas.Count, jaLocalizadas);
        });
        if (!r.Sucesso) db.ChangeTracker.Clear();
        return r;
    }

    public async Task<Resultado> ExcluirAsync(int id)
    {
        var l = await db.LabLocalAmostras.SingleOrDefaultAsync(x => x.IdLabLocalAmostras == id);
        if (l is null) return Resultado.Erro("Localização não encontrada (pode ter sido excluída).");
        var r = await ErrosMySql.CapturarAsync(async () =>
        {
            db.LabLocalAmostras.Remove(l);
            await db.SaveChangesAsync();
        });
        if (!r.Sucesso) db.ChangeTracker.Clear();
        return r;
    }

    // amostra física da amostra (ou revisão) da empresa ativa
    private async Task<int?> RaizAsync(int idAmostra) =>
        await db.LabHistAmostras.Where(a => a.IdLabHistAmostras == idAmostra).Select(a => (int?)(a.IdAmostraRaiz ?? a.IdLabHistAmostras)).SingleOrDefaultAsync();

    private async Task<int?> StatusReprovadaAsync() =>
        await db.AmostrasStatus.Where(s => s.Descricao == StatusReprovadaDescartada).Select(s => (int?)s.IdAmostrasStatus).FirstOrDefaultAsync();

    private static DateTime Minuto(DateTime d) => new(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0, d.Kind);
}
