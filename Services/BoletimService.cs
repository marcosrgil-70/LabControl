using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.Services.Boletins;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services;

/// <summary>PDF pronto para enviar ao navegador.</summary>
public sealed record ArquivoPdf(byte[] Conteudo, string Nome);

/// <summary>
/// T27 — Boletim (tarefa 5.3; P16/P17/P18, RN-08). O banco dá o código (número base da amostra), garante o resultado da
/// amostra e os parâmetros do resultado, e congela o emitido; o serviço monta, congela os textos dos parâmetros (mesma
/// regra do <c>PR_REVISAO_BOLETIM</c>), gera o PDF (QuestPDF), guarda o arquivo com SHA-256 e emite pelo
/// <c>SP_EMITIR_BOLETIM</c> — apagando o arquivo se a emissão falhar. Reimpressão confere o hash.
/// </summary>
public sealed class BoletimService(LabDbContext db, IEmpresaContexto ctx, Procedures procedures, AmostraService amostras,
    ArmazemBoletins armazem, ILogger<BoletimService> log)
{
    public const string MensagemNaoEncontrado = "O boletim não foi encontrado (pode ter sido excluído).";
    public const string MensagemEmitido = "Este boletim foi emitido. Não é permitido alterar ou excluir.";
    public const string MensagemRevisaoAntiga = "Revisão antiga do boletim é somente leitura.";
    /// <summary>Amostras oferecidas para um boletim novo: entregues nos últimos 400 dias, como o legado (ajustável na pesquisa).</summary>
    public const int DiasAmostras = 400;

    // ------------------------------------------------------------------ consulta

    public async Task<BoletimListaVM> ListarAsync(BoletimFiltroVM f)
    {
        var q = db.VwLabBoletins.AsNoTracking();
        if (f.SoUltima) q = q.Where(b => b.UltimaRevisao);
        if (Textos.Limpo(f.Cliente) is { } c) q = q.Where(b => b.ClienteNome.Contains(c));
        if (Textos.Limpo(f.Numero) is { } n) q = q.Where(b => b.NrAmostra.Contains(n) || b.CodBoletim.Contains(n));
        if (f.APartirDe is { } de) q = q.Where(b => b.DtBoletim >= de);
        if (f.Emitido is { } emitido) q = q.Where(b => (b.DhEmissao != null) == emitido);

        var total = await q.CountAsync();
        f.Pagina = Paginacao.Ajustar(f.Pagina, total, BoletimFiltroVM.TamanhoPagina);
        var itens = await q.OrderByDescending(b => b.DtBoletim).ThenByDescending(b => b.IdLabBoletimAnalises)
            .Skip((f.Pagina - 1) * BoletimFiltroVM.TamanhoPagina).Take(BoletimFiltroVM.TamanhoPagina)
            .Select(b => new BoletimListaItemVM(b.IdLabBoletimAnalises, b.NrAmostra, b.CodBoletim, b.Revisao, b.UltimaRevisao, b.DtBoletim,
                b.DhGeracao, b.DhEmissao, b.ProdutoDescricao, b.ClienteNome, b.StatusBoletim))
            .ToListAsync();
        return new BoletimListaVM { Filtro = f, Itens = itens, Total = total };
    }

    public Task<DateOnly> APartirDePadraoAsync() => amostras.APartirDePadraoAsync();

    /// <summary>Pesquisa de amostras (T29) restrita às que podem ter boletim: última revisão, com resultado.</summary>
    public async Task<AmostraListaVM> PesquisarAmostrasAsync(AmostraFiltroVM f, bool inicial)
    {
        if (inicial) f.APartirDe = (await db.HojeAsync()).AddDays(-DiasAmostras);
        (f.SoUltima, f.ApenasComResultado, f.OrdenarPorNumero) = (true, true, true);
        return await amostras.ListarAsync(f);
    }

    // ------------------------------------------------------------------ abrir

    /// <summary>
    /// Amostra escolhida: o boletim vigente da amostra física, se já existe ("Incluir abre o existente"), ou null para um
    /// novo; recusa revisão antiga da amostra ou amostra sem resultado (RN-08.01).
    /// </summary>
    public async Task<Resultado<int?>> BoletimDaAmostraAsync(int idAmostra)
    {
        var a = await db.LabHistAmostras.AsNoTracking().Where(x => x.IdLabHistAmostras == idAmostra)
            .Select(x => new { x.NrAmostra, x.UltimaRevisao, Raiz = x.IdAmostraRaiz ?? x.IdLabHistAmostras }).SingleOrDefaultAsync();
        if (a is null) return Resultado.Erro(AmostraService.MensagemNaoEncontrada);
        var vigente = await db.LabBoletimAnalises.Where(b => b.UltimaRevisao && (b.LabHistAmostra.IdAmostraRaiz ?? b.IdLabHistAmostras) == a.Raiz)
            .Select(b => (int?)b.IdLabBoletimAnalises).FirstOrDefaultAsync();
        if (vigente is not null) return Resultado.Ok(vigente);
        if (!a.UltimaRevisao) return Resultado.Erro($"A amostra {a.NrAmostra} é uma revisão antiga: faça o boletim na última revisão.");
        if (await ResultadoVigenteAsync(idAmostra) is null) return Resultado.Erro($"A amostra {a.NrAmostra} ainda não tem resultado lançado.");
        return Resultado.Ok<int?>(null);
    }

    private Task<int?> ResultadoVigenteAsync(int idAmostra) =>
        db.LabResultadosAnalises.Where(r => r.IdLabHistAmostras == idAmostra && r.UltimaRevisao).Select(r => (int?)r.IdLabResultadosAnalises).FirstOrDefaultAsync();

    /// <summary>Boletim novo: data de hoje, status inicial e os textos sugeridos (observação da verificação, metodologias, legenda).</summary>
    public async Task<BoletimVM?> NovoAsync(int idAmostra)
    {
        var a = await db.LabHistAmostras.AsNoTracking().Where(x => x.IdLabHistAmostras == idAmostra)
            .Select(x => new { x.ObservacaoVerificacao }).SingleOrDefaultAsync();
        if (a is null) return null;
        var par = await db.EmpresasParametros.AsNoTracking().SingleOrDefaultAsync();
        var vm = new BoletimVM
        {
            IdAmostra = idAmostra, DtBoletim = await db.HojeAsync(), IdStatus = par?.IdBoletinsStatusInicial,
            Observacao = a.ObservacaoVerificacao, Legenda = par?.TextoPadraoLegenda,
            MetodologiaAcreditada = await MetodosAsync(idAmostra, true), MetodologiaNaoAcreditada = await MetodosAsync(idAmostra, false),
        };
        await PrepararAsync(vm);
        return vm;
    }

    private Task<string?> MetodosAsync(int idAmostra, bool acreditado) =>
        db.Database.SqlQueryRaw<string?>("SELECT FN_OBS_METODOS({0}, {1}) AS Value", idAmostra, acreditado ? 1 : 0).SingleAsync();

    public async Task<BoletimVM?> CarregarAsync(int id)
    {
        var b = await db.LabBoletimAnalises.AsNoTracking().SingleOrDefaultAsync(x => x.IdLabBoletimAnalises == id);
        if (b is null) return null;
        var vm = new BoletimVM
        {
            Id = id, IdAmostra = b.IdLabHistAmostras, DtBoletim = b.DtBoletim, IdStatus = b.IdBoletinsStatus,
            IdResponsavel1 = b.IdEntidadesFuncResp, IdResponsavel2 = b.IdEntidadesFuncResp2, Observacao = b.TxtObservacao,
            MetodologiaAcreditada = b.TxtMetodologiaAcred, MetodologiaNaoAcreditada = b.TxtMetodologiaUtil, Legenda = b.TxtLegenda,
        };
        await PrepararAsync(vm);
        return vm;
    }

    /// <summary>Cabeçalho, travas, os dois grids e as opções — também ao reexibir o formulário com erro.</summary>
    public async Task PrepararAsync(BoletimVM vm)
    {
        LabBoletimAnalise? b = null;
        if (vm.Id != 0)
        {
            b = await db.LabBoletimAnalises.AsNoTracking().SingleOrDefaultAsync(x => x.IdLabBoletimAnalises == vm.Id);
            if (b is not null)
            {
                (vm.IdAmostra, vm.CodBoletim, vm.Revisao, vm.MotivoRevisao, vm.UltimaRevisao, vm.IdResultado, vm.DhEmissao, vm.TemArquivo) =
                    (b.IdLabHistAmostras, b.CodBoletim, b.Revisao, b.MotivoRevisao, b.UltimaRevisao, b.IdLabResultadosAnalises, b.DhEmissao, b.ArquivoPdf is not null);
                vm.Emissor = b.IdEntidadesFuncEmi is { } emi ? await db.VwProfissionais.Where(p => p.IdEntidades == emi).Select(p => p.NomeCompleto ?? p.Nome).FirstOrDefaultAsync() : null;
                vm.Bloqueio = b.DhEmissao is not null ? MensagemEmitido : !b.UltimaRevisao ? MensagemRevisaoAntiga : null;
            }
        }
        else vm.IdResultado = await ResultadoVigenteAsync(vm.IdAmostra) ?? 0;

        var a = await db.VwLabAmostras.AsNoTracking().Where(x => x.IdLabHistAmostras == vm.IdAmostra)
            .Select(x => new { x.NrAmostra, x.ProdutoDescricao, x.NrLote, x.ClienteNome }).SingleOrDefaultAsync();
        if (a is not null) (vm.NrAmostra, vm.Produto, vm.Lote, vm.Cliente) = (a.NrAmostra, a.ProdutoDescricao, a.NrLote, a.ClienteNome);
        vm.RevisaoResultado = await db.LabResultadosAnalises.Where(r => r.IdLabResultadosAnalises == vm.IdResultado).Select(r => r.Revisao).FirstOrDefaultAsync();

        vm.Parametros = vm.Id == 0 ? [] : await db.LabBoletimParametros.AsNoTracking().Where(p => p.IdLabBoletimAnalises == vm.Id)
            .OrderBy(p => p.Ordem).ThenBy(p => p.IdLabBoletimParametros)
            .Select(p => new ParametroBoletimVM(p.IdLabBoletimParametros, p.Ordem, p.DescParametro, p.ResultadoConcat, p.TipoResultadoDesc,
                p.UnidadeSg, p.Especificacao, p.Acreditado))
            .ToListAsync();
        var noBoletim = vm.Id == 0 ? [] : await db.LabBoletimParametros.Where(p => p.IdLabBoletimAnalises == vm.Id).Select(p => p.IdLabResultadosParam).ToListAsync();
        vm.Disponiveis = (await db.VwLabResultados.AsNoTracking().Where(r => r.IdLabResultadosAnalises == vm.IdResultado)
                .OrderByDescending(r => r.Auditado).ThenBy(r => r.DescReduzida).ToListAsync())
            .Where(r => !noBoletim.Contains(r.IdLabResultadosParam))
            .Select(r => new ResultadoDisponivelVM(r.IdLabResultadosParam, r.DescReduzida, r.Auditado, r.DtResultado,
                r.TipoResultado ? r.ResultadoFormatado : null, r.TipoResultado ? null : r.ResultadoFormatado, r.Unidade, r.QtAmostraTxt))
            .ToList();

        var par = await db.EmpresasParametros.AsNoTracking().SingleOrDefaultAsync();
        vm.ExigeResponsavel2 = par?.ExigeResp2Boletim ?? false;
        var atuais = new[] { vm.IdResponsavel1, vm.IdResponsavel2 }.OfType<int>().ToList();
        vm.Opcoes = new BoletimOpcoesVM
        {
            Responsaveis = await db.VwProfissionais.AsNoTracking().Where(p => (p.AssinaBoletim && !p.Inativo) || atuais.Contains(p.IdEntidades))
                .OrderBy(p => p.Nome).Select(p => new OpcaoVM(p.IdEntidades, (p.NomeCompleto ?? p.Nome) + (p.RegistroCompleto == null ? "" : " — " + p.RegistroCompleto)))
                .ToListAsync(),
            Status = await db.BoletinsStatus.AsNoTracking().OrderBy(s => s.Descricao).Select(s => new OpcaoVM(s.IdBoletinsStatus, s.Descricao)).ToListAsync(),
        };
    }

    // ------------------------------------------------------------------ gravar e parâmetros

    /// <summary>
    /// Inclusão: cabeçalho + parâmetros marcados numa transação. Alteração: data, status, responsáveis e textos; emitido
    /// só muda o status (o banco recusa o resto).
    /// </summary>
    public async Task<Resultado<int>> GravarAsync(BoletimVM vm)
    {
        LabBoletimAnalise? b = null;
        if (vm.Id == 0)
        {
            var abrir = await BoletimDaAmostraAsync(vm.IdAmostra);
            if (!abrir.Sucesso) return Resultado.Erro(abrir.Mensagem!);
            if (abrir.Valor is not null) return Resultado.Erro("Esta amostra já tem boletim — abra o existente.");
            if (vm.Selecionados.Count == 0) return Resultado.Erro("Nenhum Parâmetro foi selecionado.");
        }
        else
        {
            b = await db.LabBoletimAnalises.SingleOrDefaultAsync(x => x.IdLabBoletimAnalises == vm.Id);
            if (b is null) return Resultado.Erro(MensagemNaoEncontrado);
            if (!b.UltimaRevisao) return Resultado.Erro(MensagemRevisaoAntiga);
            if (b.DhEmissao is not null)
            {
                // emitido: só o status (ex.: "Enviado ao cliente")
                b.IdBoletinsStatus = vm.IdStatus;
                return await ErrosMySql.CapturarAsync(async () => { await db.SaveChangesAsync(); return b.IdLabBoletimAnalises; });
            }
        }
        if (vm.DtBoletim is not { } data) return Resultado.Erro("Data do boletim é obrigatória.", nameof(vm.DtBoletim));
        if (data > await db.HojeAsync()) return Resultado.Erro("A data do boletim não pode ser depois de hoje.", nameof(vm.DtBoletim));
        var assinam = await db.EntidadesFuncionarios.Where(f => f.AssinaBoletim).Select(f => f.IdEntidades).ToListAsync();
        foreach (var (resp, campo, atual) in new[] { (vm.IdResponsavel1, nameof(vm.IdResponsavel1), b?.IdEntidadesFuncResp), (vm.IdResponsavel2, nameof(vm.IdResponsavel2), b?.IdEntidadesFuncResp2) })
            if (resp is { } r && !assinam.Contains(r) && r != atual)
                return Resultado.Erro("O responsável precisa ser um profissional que assina boletim.", campo);
        if (vm.IdResponsavel2 is not null && vm.IdResponsavel2 == vm.IdResponsavel1)
            return Resultado.Erro("Os dois responsáveis precisam ser profissionais diferentes.", nameof(vm.IdResponsavel2));
        if (ctx.ProfissionalId is not { } gerador)
            return Resultado.Erro("Seu usuário não está vinculado a um profissional (Usuários › profissional): o boletim registra quem gerou.");

        return await ErrosMySql.CapturarAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            if (b is null)
            {
                var resultado = await ResultadoVigenteAsync(vm.IdAmostra) ?? throw new ErroDeNegocioException("A amostra não tem resultado lançado.");
                b = db.LabBoletimAnalises.Add(new LabBoletimAnalise
                {
                    IdLabHistAmostras = vm.IdAmostra, IdLabResultadosAnalises = resultado, CodBoletim = "", IdEntidadesFuncGer = gerador,
                    DhGeracao = await db.AgoraAsync(),
                }).Entity;
            }
            (b.DtBoletim, b.IdBoletinsStatus, b.IdEntidadesFuncResp, b.IdEntidadesFuncResp2) = (data, vm.IdStatus, vm.IdResponsavel1, vm.IdResponsavel2);
            (b.TxtObservacao, b.TxtLegenda, b.TxtMetodologiaAcred, b.TxtMetodologiaUtil) =
                (Textos.Limpo(vm.Observacao), Textos.Limpo(vm.Legenda), Textos.Limpo(vm.MetodologiaAcreditada), Textos.Limpo(vm.MetodologiaNaoAcreditada));
            await db.SaveChangesAsync();
            await db.Entry(b).ReloadAsync(); // código (número base da amostra) e empresa vêm da trigger
            if (vm.Selecionados.Count > 0) await IncluirAsync(b, vm.Selecionados);
            await tx.CommitAsync();
            return b.IdLabBoletimAnalises;
        });
    }

    /// <summary>"Concluir seleção": os parâmetros marcados entram no fim da lista (acreditados primeiro), com o texto congelado.</summary>
    public async Task<Resultado> IncluirParametrosAsync(int id, IReadOnlyList<int> idsParam)
    {
        if (idsParam.Count == 0) return Resultado.Erro("Nenhum Parâmetro foi selecionado.");
        var b = await EditavelAsync(id);
        if (!b.Sucesso) return Resultado.Erro(b.Mensagem!);
        return await ErrosMySql.CapturarAsync(() => IncluirAsync(b.Valor, idsParam));
    }

    private async Task IncluirAsync(LabBoletimAnalise b, IReadOnlyList<int> idsParam)
    {
        var resultados = await db.VwLabResultados.AsNoTracking()
            .Where(r => idsParam.Contains(r.IdLabResultadosParam) && r.IdLabResultadosAnalises == b.IdLabResultadosAnalises).ToListAsync();
        if (resultados.Count != idsParam.Distinct().Count()) throw new ErroDeNegocioException("O parâmetro não pertence ao resultado do boletim.");
        var ordem = await db.LabBoletimParametros.Where(p => p.IdLabBoletimAnalises == b.IdLabBoletimAnalises).MaxAsync(p => (int?)p.Ordem) ?? 0;
        foreach (var r in resultados.OrderByDescending(r => r.Auditado).ThenBy(r => r.DescReduzida))
        {
            var p = new LabBoletimParametro { IdLabBoletimAnalises = b.IdLabBoletimAnalises, IdLabResultadosParam = r.IdLabResultadosParam, Ordem = ++ordem };
            Congelar(p, r);
            db.LabBoletimParametros.Add(p);
        }
        await db.SaveChangesAsync();
    }

    /// <summary>Texto congelado — a mesma regra do <c>PR_REVISAO_BOLETIM</c> (RN-08.05).</summary>
    private static void Congelar(LabBoletimParametro p, VwLabResultado r)
    {
        p.DescParametro = r.DescReduzida;
        p.ResultadoConcat = r.TipoResultado ? r.ResultadoFormatado : null;
        p.TipoResultadoDesc = r.TipoResultado ? null : r.ResultadoFormatado;
        (p.UnidadeSg, p.Especificacao, p.Acreditado) = (r.Unidade, r.EspecifFormatada, r.Auditado);
    }

    private Task<Resultado> SalvarAsync() => ErrosMySql.CapturarAsync(async () => { await db.SaveChangesAsync(); });

    /// <summary>"Atualizar textos": regrava os textos dos parâmetros a partir dos resultados atuais (só antes de emitir).</summary>
    public async Task<Resultado> AtualizarTextosAsync(int id)
    {
        var b = await EditavelAsync(id);
        if (!b.Sucesso) return Resultado.Erro(b.Mensagem!);
        var linhas = await db.LabBoletimParametros.Where(p => p.IdLabBoletimAnalises == id).ToListAsync();
        var ids = linhas.Select(l => l.IdLabResultadosParam).ToList();
        var resultados = await db.VwLabResultados.AsNoTracking().Where(r => ids.Contains(r.IdLabResultadosParam)).ToDictionaryAsync(r => r.IdLabResultadosParam);
        foreach (var l in linhas.Where(l => resultados.ContainsKey(l.IdLabResultadosParam))) Congelar(l, resultados[l.IdLabResultadosParam]);
        return await SalvarAsync();
    }

    public async Task<Resultado> ExcluirParametroAsync(int id, int idParametroBoletim)
    {
        var b = await EditavelAsync(id);
        if (!b.Sucesso) return Resultado.Erro(b.Mensagem!);
        var p = await db.LabBoletimParametros.SingleOrDefaultAsync(x => x.IdLabBoletimParametros == idParametroBoletim && x.IdLabBoletimAnalises == id);
        if (p is null) return Resultado.Erro("O parâmetro não está neste boletim.");
        db.LabBoletimParametros.Remove(p);
        return await SalvarAsync();
    }

    /// <summary>▲ (−1) / ▼ (+1): troca de lugar com o vizinho; a ordem é renumerada 1..n.</summary>
    public async Task<Resultado> MoverAsync(int id, int idParametroBoletim, int direcao)
    {
        var b = await EditavelAsync(id);
        if (!b.Sucesso) return Resultado.Erro(b.Mensagem!);
        var linhas = await db.LabBoletimParametros.Where(p => p.IdLabBoletimAnalises == id).OrderBy(p => p.Ordem).ThenBy(p => p.IdLabBoletimParametros).ToListAsync();
        var i = linhas.FindIndex(p => p.IdLabBoletimParametros == idParametroBoletim);
        if (i < 0) return Resultado.Erro("O parâmetro não está neste boletim.");
        var j = i + Math.Sign(direcao);
        if (j >= 0 && j < linhas.Count) (linhas[i], linhas[j]) = (linhas[j], linhas[i]);
        for (var k = 0; k < linhas.Count; k++) linhas[k].Ordem = k + 1;
        return await SalvarAsync();
    }

    /// <summary>Boletim que ainda aceita mudança nos parâmetros: última revisão e não emitido.</summary>
    private async Task<Resultado<LabBoletimAnalise>> EditavelAsync(int id)
    {
        var b = await db.LabBoletimAnalises.SingleOrDefaultAsync(x => x.IdLabBoletimAnalises == id);
        if (b is null) return Resultado.Erro(MensagemNaoEncontrado);
        if (b.DhEmissao is not null) return Resultado.Erro(MensagemEmitido);
        if (!b.UltimaRevisao) return Resultado.Erro(MensagemRevisaoAntiga);
        return Resultado.Ok(b);
    }

    /// <summary>Exclui o boletim (só revisão 0 e não emitido; revisão se desfaz).</summary>
    public async Task<Resultado> ExcluirAsync(int id)
    {
        var b = await EditavelAsync(id);
        if (!b.Sucesso) return Resultado.Erro(b.Mensagem!);
        if (b.Valor.Revisao > 0) return Resultado.Erro("Para excluir uma revisão use \"Desfazer última revisão\".");
        db.LabBoletimAnalises.Remove(b.Valor); // os parâmetros vão junto (FK em cascata)
        return await SalvarAsync();
    }

    // ------------------------------------------------------------------ revisão

    public async Task<Resultado<int>> GerarRevisaoAsync(int id, string? motivo)
    {
        if (Textos.Limpo(motivo) is not { } m) return Resultado.Erro("Informe o motivo da revisão.", "motivo");
        var r = await procedures.GerarRevisaoBoletimAsync(id, m.Length > 500 ? m[..500] : m);
        return r.Sucesso ? Resultado.Ok(r.Valor.IdNovo) : Resultado.Erro(r.Mensagem!);
    }

    public async Task<Resultado<int>> DesfazerRevisaoAsync(int id)
    {
        var anterior = await db.LabBoletimAnalises.Where(x => x.IdLabBoletimAnalises == id).Select(x => x.IdBoletimOrigem).SingleOrDefaultAsync();
        if (anterior is null) return Resultado.Erro("Este boletim não é uma revisão: não há o que desfazer.");
        var r = await procedures.ExcluirRevisaoBoletimAsync(id);
        return r.Sucesso ? Resultado.Ok(anterior.Value) : Resultado.Erro(r.Mensagem!);
    }

    // ------------------------------------------------------------------ PDF: prévia, emissão, reimpressão

    /// <summary>Prévia: marca d'água e sem assinaturas; nada é gravado.</summary>
    public async Task<Resultado<ArquivoPdf>> PreviaAsync(int id)
    {
        var d = await DocumentoAsync(id, ModoBoletim.Previa);
        if (d is null) return Resultado.Erro(MensagemNaoEncontrado);
        return Resultado.Ok(new ArquivoPdf(BoletimPdf.Gerar(d), $"Previa {NomeArquivo(d)}"));
    }

    /// <summary>Emissão (P17): valida, gera o PDF definitivo, grava com SHA-256 e chama o SP_EMITIR_BOLETIM; se falhar, apaga o arquivo.</summary>
    public async Task<Resultado> EmitirAsync(int id)
    {
        var b = await EditavelAsync(id);
        if (!b.Sucesso) return Resultado.Erro(b.Mensagem!);
        var exige2 = await db.EmpresasParametros.Select(p => (bool?)p.ExigeResp2Boletim).SingleOrDefaultAsync() ?? false;
        if (b.Valor.IdEntidadesFuncResp is null || (exige2 && b.Valor.IdEntidadesFuncResp2 is null))
            return Resultado.Erro("Informe o(s) responsável(is) técnico(s) do boletim.");
        if (!await db.LabBoletimParametros.AnyAsync(p => p.IdLabBoletimAnalises == id)) return Resultado.Erro("Boletim sem parâmetros selecionados.");
        if (ctx.ProfissionalId is null)
            return Resultado.Erro("Seu usuário não está vinculado a um profissional (Usuários › profissional): a emissão registra quem emitiu.");

        var d = (await DocumentoAsync(id, ModoBoletim.Emitido))!;
        var codEmpresa = await db.Empresas.Where(e => e.IdEmpresas == b.Valor.IdEmpresas).Select(e => e.CodEmpresa).SingleAsync();
        // código e revisão relidos do banco (a trigger dá o código; a entidade rastreada pode estar desatualizada)
        var gravado = await db.LabBoletimAnalises.AsNoTracking().Where(x => x.IdLabBoletimAnalises == id).Select(x => new { x.CodBoletim, x.Revisao }).SingleAsync();
        var relativo = ArmazemBoletins.CaminhoRelativo(codEmpresa, (await db.HojeAsync()).Year, gravado.CodBoletim, gravado.Revisao);
        var hash = await armazem.GravarAsync(relativo, BoletimPdf.Gerar(d));
        try
        {
            var r = await procedures.EmitirBoletimAsync(id, relativo, hash);
            if (!r.Sucesso) armazem.Apagar(relativo);
            return r;
        }
        catch
        {
            armazem.Apagar(relativo); // nada de arquivo órfão
            throw;
        }
    }

    /// <summary>
    /// Reimpressão do emitido: o PDF gravado, conferido pelo SHA-256 (divergente ou ausente = alerta de integridade).
    /// Boletim migrado (emitido sem arquivo) sai como "2ª via" gerada dos dados gravados.
    /// </summary>
    public async Task<Resultado<ArquivoPdf>> ImprimirAsync(int id)
    {
        var b = await db.LabBoletimAnalises.AsNoTracking().Where(x => x.IdLabBoletimAnalises == id)
            .Select(x => new { x.DhEmissao, x.ArquivoPdf, x.HashPdf, x.CodBoletim, x.Revisao }).SingleOrDefaultAsync();
        if (b is null) return Resultado.Erro(MensagemNaoEncontrado);
        if (b.DhEmissao is null) return Resultado.Erro("Boletim ainda não emitido: use \"Visualizar\" para a prévia.");
        if (b.ArquivoPdf is null)
        {
            var d = (await DocumentoAsync(id, ModoBoletim.SegundaViaMigrado))!;
            return Resultado.Ok(new ArquivoPdf(BoletimPdf.Gerar(d), $"2a via {NomeArquivo(d)}"));
        }
        var pdf = await armazem.LerAsync(b.ArquivoPdf);
        if (pdf is null || ArmazemBoletins.Hash(pdf) != b.HashPdf)
        {
            log.LogError("Integridade do boletim {Id} ({Arquivo}): {Problema}", id, b.ArquivoPdf, pdf is null ? "arquivo ausente" : "hash diferente do gravado na emissão");
            return Resultado.Erro(pdf is null
                ? $"ALERTA DE INTEGRIDADE: o PDF emitido do boletim {b.CodBoletim} R{b.Revisao:00} não foi encontrado na pasta dos boletins. Avise o administrador."
                : $"ALERTA DE INTEGRIDADE: o PDF do boletim {b.CodBoletim} R{b.Revisao:00} foi alterado depois da emissão (hash diferente). Avise o administrador.");
        }
        return Resultado.Ok(new ArquivoPdf(pdf, Path.GetFileName(b.ArquivoPdf)));
    }

    private static string NomeArquivo(BoletimDocumento d) => $"{d.CodBoletim.Replace('/', '-')} R{d.Revisao:00}.pdf";

    /// <summary>Reúne o que o laudo imprime (P16 › Layout).</summary>
    public async Task<BoletimDocumento?> DocumentoAsync(int id, ModoBoletim modo)
    {
        var b = await db.LabBoletimAnalises.AsNoTracking().SingleOrDefaultAsync(x => x.IdLabBoletimAnalises == id);
        if (b is null) return null;
        var a = await db.VwLabAmostras.AsNoTracking().SingleAsync(x => x.IdLabHistAmostras == b.IdLabHistAmostras);
        var p = await db.LabPropostas.AsNoTracking().Where(x => x.IdLabPropostas == a.IdLabPropostas)
            .Select(x => new { x.IdEntEndLaudo, x.IdEntContatoResultado }).SingleAsync();
        var laudo = await db.VwEntidadesEnderecos.AsNoTracking().Where(e => e.IdEntidadesEnderecos == p.IdEntEndLaudo)
            .Select(e => new { e.EnderecoLinha1, e.EnderecoLinha2 }).SingleOrDefaultAsync();
        var contato = await db.EntidadesEmails.AsNoTracking().Where(e => e.IdEntidadesEmails == p.IdEntContatoResultado)
            .Select(e => new { e.NomeContato, e.Email }).SingleOrDefaultAsync();
        var emp = await db.Empresas.AsNoTracking().Where(e => e.IdEmpresas == b.IdEmpresas)
            .Select(e => new { e.RazaoSocial, e.Cnpj, e.Logo, e.NrAcreditacao, e.CabecalhoLaudo, e.RodapeLaudo, e.IdEntidades }).SingleAsync();
        var endEmp = await db.VwEntidadesEnderecos.AsNoTracking().Where(e => e.IdEntidades == emp.IdEntidades && !e.Inativo)
            .OrderBy(e => e.IdEntidadesEnderecos).Select(e => new { e.EnderecoLinha1, e.EnderecoLinha2, e.Cidade, e.Uf }).FirstOrDefaultAsync();

        var linhas = await db.LabBoletimParametros.AsNoTracking().Where(x => x.IdLabBoletimAnalises == id)
            .OrderBy(x => x.Ordem).ThenBy(x => x.IdLabBoletimParametros).ToListAsync();
        var idsParam = linhas.Select(l => l.IdLabResultadosParam).ToList();
        var periodo = await db.LabResultadosParam.AsNoTracking().Where(r => idsParam.Contains(r.IdLabResultadosParam))
            .GroupBy(_ => 1).Select(g => new { Inicio = g.Min(r => r.DtInicial), Fim = g.Max(r => r.DtResultado) }).SingleOrDefaultAsync();

        var idsResp = new[] { b.IdEntidadesFuncResp, b.IdEntidadesFuncResp2 }.OfType<int>().ToList();
        var profissionais = await db.VwProfissionais.AsNoTracking().Where(x => idsResp.Contains(x.IdEntidades))
            .Select(x => new { x.IdEntidades, Nome = x.NomeCompleto ?? x.Nome, x.Cargo, x.RegistroCompleto }).ToListAsync();
        var assinaturas = modo == ModoBoletim.Emitido
            ? await db.EntidadesFuncionarios.AsNoTracking().Where(f => idsResp.Contains(f.IdEntidades) && f.Assinatura != null)
                .ToDictionaryAsync(f => f.IdEntidades, f => f.Assinatura!)
            : [];

        return new BoletimDocumento
        {
            Modo = modo,
            EmpresaNome = emp.RazaoSocial, EmpresaCnpj = emp.Cnpj is { Length: > 0 } cnpj ? Documentos.FormatarCnpj(cnpj) : null,
            EmpresaEndereco = new[] { endEmp?.EnderecoLinha1, endEmp?.EnderecoLinha2 }.OfType<string>().ToList(),
            EmpresaCidade = endEmp is null ? null : $"{endEmp.Cidade}/{endEmp.Uf}",
            Logo = ImagemValida(emp.Logo, "logo da empresa", id),
            NrAcreditacao = a.TipoAcreditacao >= 1 ? Textos.Limpo(emp.NrAcreditacao) : null,
            Cabecalho = Textos.Limpo(emp.CabecalhoLaudo), Rodape = Textos.Limpo(emp.RodapeLaudo),
            CodBoletim = b.CodBoletim, Revisao = b.Revisao, DtBoletim = b.DtBoletim,
            ClienteNome = a.ClienteNome, ClienteDocumento = a.ClienteCpfCnpj,
            EnderecoLaudo = new[] { laudo?.EnderecoLinha1, laudo?.EnderecoLinha2 }.OfType<string>().ToList(),
            Contato = contato is null ? null : string.Join(" — ", new[] { contato.NomeContato, contato.Email }.OfType<string>()),
            NrProposta = a.NrProposta, NrPedido = a.NrPedidoFmt,
            NrAmostra = a.NrAmostra, Produto = a.ProdutoDescricao, Lote = a.NrLote, Fabricacao = a.Fabricacao, Validade = a.Validade,
            Recebimento = a.DtEntrega.ToDateTime(a.HrEntrega), Coletor = a.NomeColetor, Coleta = a.DtHrColeta, ObservacaoVerificacao = a.ObservacaoVerificacao,
            InicioAnalise = periodo?.Inicio, FimAnalise = periodo?.Fim,
            Linhas = linhas.Select(l => new LinhaBoletim(l.DescParametro, SemUnidade(l.ResultadoConcat ?? l.TipoResultadoDesc, l.UnidadeSg),
                l.UnidadeSg, l.Especificacao, l.Acreditado)).ToList(),
            MetodologiaAcreditada = b.TxtMetodologiaAcred, MetodologiaNaoAcreditada = b.TxtMetodologiaUtil,
            Observacao = b.TxtObservacao, Legenda = b.TxtLegenda,
            Responsaveis = idsResp.Select(r => profissionais.FirstOrDefault(x => x.IdEntidades == r) is { } x
                ? new ResponsavelBoletim(x.Nome, x.Cargo, x.RegistroCompleto, ImagemValida(assinaturas.GetValueOrDefault(r), "assinatura", id))
                : null).OfType<ResponsavelBoletim>().ToList(),
        };
    }

    // o texto formatado do banco já traz a unidade ("< 1,0e2 UFC/g"); no laudo ela tem coluna própria
    private static string? SemUnidade(string? resultado, string? unidade) =>
        resultado is not null && unidade is { Length: > 0 } u && resultado.EndsWith(" " + u, StringComparison.Ordinal) ? resultado[..^(u.Length + 1)] : resultado;

    // imagem que o QuestPDF não consegue ler não pode derrubar a emissão: sai sem ela e fica no log
    private byte[]? ImagemValida(byte[]? imagem, string qual, int id)
    {
        if (imagem is null) return null;
        try { QuestPDF.Infrastructure.Image.FromBinaryData(imagem).Dispose(); return imagem; }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            log.LogWarning(ex, "Boletim {Id}: {Qual} não é uma imagem válida — impresso sem ela.", id, qual);
            return null;
        }
    }
}
