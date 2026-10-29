using System.Text.RegularExpressions;
using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services;

/// <summary>
/// T26 — Resultados (tarefa 5.2; P14/P15, RN-07). Um cabeçalho por revisão da amostra e uma linha por teste. O banco
/// formata o texto (<c>FN_FORMATA_RESULTADO</c> nas triggers), limpa os campos incompatíveis (numérico × descritivo) e
/// trava revisão antiga, boletim emitido, linha em boletim e teste de outra amostra; o serviço valida antes, com as
/// mensagens no campo. Revisão e desfazer pelas procedures, fora da transação do EF.
/// </summary>
public sealed partial class ResultadoService(LabDbContext db, IEmpresaContexto ctx, Procedures procedures, AmostraService amostras)
{
    public const string MensagemNaoEncontrado = "O resultado não foi encontrado (pode ter sido excluído).";
    public const string MensagemBoletimEmitido = "Resultado com boletim emitido: para alterar, gere uma revisão do resultado.";
    public const string MensagemRevisaoAntiga = "Revisão antiga do resultado é somente leitura.";
    private static readonly System.Globalization.CultureInfo PtBr = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");

    // ------------------------------------------------------------------ consulta

    public async Task<ResultadoListaVM> ListarAsync(ResultadoFiltroVM f)
    {
        var q = from r in db.LabResultadosAnalises.AsNoTracking()
                join a in db.VwLabAmostras.AsNoTracking() on r.IdLabHistAmostras equals a.IdLabHistAmostras
                select new { r, a };
        if (f.SoUltima) q = q.Where(x => x.r.UltimaRevisao);
        if (Textos.Limpo(f.Cliente) is { } c)
            q = q.Where(x => x.a.ClienteNome.Contains(c) || (x.a.ClienteFantasia != null && x.a.ClienteFantasia.Contains(c))
                             || (x.a.ClienteCpfCnpj != null && x.a.ClienteCpfCnpj.Contains(c)));
        if (Textos.Limpo(f.Proposta) is { } p) q = q.Where(x => x.a.NrProposta.Contains(p));
        if (Textos.Limpo(f.Numero) is { } n) q = q.Where(x => x.a.NrAmostra.Contains(n));
        if (f.APartirDe is { } de) q = q.Where(x => x.a.DtEntrega >= de);

        var total = await q.CountAsync();
        f.Pagina = Paginacao.Ajustar(f.Pagina, total, ResultadoFiltroVM.TamanhoPagina);
        var itens = await q.OrderByDescending(x => x.a.DtEntrega).ThenByDescending(x => x.r.IdLabResultadosAnalises)
            .Skip((f.Pagina - 1) * ResultadoFiltroVM.TamanhoPagina).Take(ResultadoFiltroVM.TamanhoPagina)
            .Select(x => new ResultadoListaItemVM(x.r.IdLabResultadosAnalises, x.a.NrAmostra, x.r.Revisao, x.r.UltimaRevisao, x.a.DtEntrega,
                x.a.NrProposta, x.a.ProdutoDescricao, x.a.TipoAmostra, x.a.TipoAnalise, x.a.ClienteNome,
                db.LabResultadosParam.Count(p => p.IdLabResultadosAnalises == x.r.IdLabResultadosAnalises)))
            .ToListAsync();

        // a fila só é lida com a aba dela aberta; a contagem vai sempre (badge da aba)
        var fila = db.VwLabTestesPendentes.AsNoTracking();
        var filaItens = !f.Fila ? [] : await fila.OrderBy(t => t.DtPrevista == null).ThenBy(t => t.DtPrevista).ThenBy(t => t.NrAmostra).Take(200)
            .Select(t => new TestePendenteVM(t.IdLabHistAmostras, t.NrAmostra, t.Parametro, t.DtPrevista, t.DiasParaPrazo, t.ClienteNome, t.ProdutoDescricao))
            .ToListAsync();
        return new ResultadoListaVM { Filtro = f, Itens = itens, Total = total, Fila = filaItens, TotalFila = await fila.CountAsync() };
    }

    public Task<DateOnly> APartirDePadraoAsync() => amostras.APartirDePadraoAsync();

    /// <summary>Pesquisa de amostras (T29) restrita às que podem receber resultado: última revisão, status que permite.</summary>
    public async Task<AmostraListaVM> PesquisarAmostrasAsync(AmostraFiltroVM f, bool inicial)
    {
        // período padrão da empresa (Parâmetros da Empresa), como as outras listas — pendência 33 (era 400 dias fixos)
        if (inicial) f.APartirDe = await amostras.APartirDePadraoAsync();
        (f.SoUltima, f.ApenasPermiteResultado, f.OrdenarPorNumero) = (true, true, true);
        return await amostras.ListarAsync(f);
    }

    // ------------------------------------------------------------------ abrir

    /// <summary>
    /// Amostra escolhida para lançar: o resultado vigente dela, se já existe ("Incluir abre o existente"), ou null para um
    /// novo; recusa amostra que não pode receber resultado (RN-07.01).
    /// </summary>
    public async Task<Resultado<int?>> ResultadoDaAmostraAsync(int idAmostra)
    {
        var a = await db.VwLabAmostras.AsNoTracking().Where(x => x.IdLabHistAmostras == idAmostra)
            .Select(x => new { x.NrAmostra, x.UltimaRevisao, x.PermiteResultado }).SingleOrDefaultAsync();
        if (a is null) return Resultado.Erro(AmostraService.MensagemNaoEncontrada);
        var existente = await db.LabResultadosAnalises.Where(r => r.IdLabHistAmostras == idAmostra && r.UltimaRevisao)
            .Select(r => (int?)r.IdLabResultadosAnalises).FirstOrDefaultAsync();
        if (existente is not null) return Resultado.Ok(existente);
        if (!a.UltimaRevisao) return Resultado.Erro($"A amostra {a.NrAmostra} é uma revisão antiga: lance o resultado na última revisão.");
        if (!a.PermiteResultado) return Resultado.Erro($"O status da amostra {a.NrAmostra} não permite lançar resultado.");
        return Resultado.Ok<int?>(null);
    }

    public async Task<ResultadoVM?> NovoAsync(int idAmostra)
    {
        var vm = new ResultadoVM { IdAmostra = idAmostra };
        if (!await CabecalhoAsync(vm)) return null;
        await PrepararAsync(vm);
        return vm;
    }

    public async Task<ResultadoVM?> CarregarAsync(int id)
    {
        if (!await db.LabResultadosAnalises.AnyAsync(x => x.IdLabResultadosAnalises == id)) return null;
        var vm = new ResultadoVM { Id = id };
        await PrepararAsync(vm);
        return vm;
    }

    /// <summary>
    /// Cabeçalho (amostra), travas, cartões e opções dos combos. Ao reexibir o formulário com erro, os cartões postados
    /// ficam com o que o usuário digitou e recebem do banco o que não vem no POST (método, prévia, "no boletim").
    /// </summary>
    public async Task PrepararAsync(ResultadoVM vm)
    {
        if (vm.Id != 0)
        {
            var r = await db.LabResultadosAnalises.AsNoTracking().Where(x => x.IdLabResultadosAnalises == vm.Id)
                .Select(x => new { x.IdLabHistAmostras, x.Revisao, x.MotivoRevisao, x.UltimaRevisao, x.IdResultadoOrigem }).SingleOrDefaultAsync();
            if (r is not null)
                (vm.IdAmostra, vm.Revisao, vm.MotivoRevisao, vm.UltimaRevisao, vm.IdResultadoAnterior) =
                    (r.IdLabHistAmostras, r.Revisao, r.MotivoRevisao, r.UltimaRevisao, r.IdResultadoOrigem);
            var boletins = await db.LabBoletimAnalises.AsNoTracking().Where(b => b.IdLabResultadosAnalises == vm.Id)
                .Select(b => b.DhEmissao != null).ToListAsync();
            vm.TemBoletim = boletins.Count > 0;
            vm.Bloqueio = !vm.UltimaRevisao ? MensagemRevisaoAntiga : boletins.Any(e => e) ? MensagemBoletimEmitido : null;
        }
        await CabecalhoAsync(vm);
        var doBanco = await LinhasAsync(vm.IdAmostra, vm.Id == 0 ? null : vm.Id);
        if (vm.Linhas.Count == 0) vm.Linhas = doBanco;
        else
        {
            var porTeste = doBanco.ToDictionary(l => l.IdTeste);
            foreach (var l in vm.Linhas.Where(l => porTeste.ContainsKey(l.IdTeste)))
            {
                var b = porTeste[l.IdTeste];
                (l.Parametro, l.Metodo, l.Acreditado, l.NoBoletim, l.ResultadoFormatado, l.EspecFormatada) =
                    (b.Parametro, b.Metodo, b.Acreditado, b.NoBoletim, b.ResultadoFormatado, b.EspecFormatada);
            }
        }

        var usados = vm.Linhas.SelectMany(l => new[] { l.IdUnidade, l.EspecIdUnidade, l.IdUnidadeAmostra }).OfType<int>().ToHashSet();
        var tipos = vm.Linhas.SelectMany(l => new[] { l.IdTipoResultado, l.EspecIdTipoResultado }).OfType<int>().ToHashSet();
        var analistas = vm.Linhas.Select(l => l.IdAnalista).OfType<int>().ToHashSet();
        vm.Opcoes = new ResultadoOpcoesVM
        {
            Hoje = await db.HojeAsync(),
            Unidades = await db.Unidades.AsNoTracking().Where(u => u.Ativo || usados.Contains(u.IdUnidades)).OrderBy(u => u.Sigla)
                .Select(u => new OpcaoVM(u.IdUnidades, u.Sigla)).ToListAsync(),
            TiposResultado = await db.LabTiposResultados.AsNoTracking().Where(t => t.Ativo || tipos.Contains(t.IdLabTiposResultados)).OrderBy(t => t.Descricao)
                .Select(t => new OpcaoVM(t.IdLabTiposResultados, t.Descricao)).ToListAsync(),
            Analistas = await db.VwProfissionais.AsNoTracking().Where(p => (p.ExecutaAnalise && !p.Inativo) || analistas.Contains(p.IdEntidades))
                .OrderBy(p => p.Nome).Select(p => new OpcaoVM(p.IdEntidades, p.NomeCompleto ?? p.Nome)).ToListAsync(),
        };
        vm.Opcoes.AnalistaPadrao = ctx.ProfissionalId is { } prof && vm.Opcoes.Analistas.Any(a => a.Id == prof) ? prof : null;
    }

    private async Task<bool> CabecalhoAsync(ResultadoVM vm)
    {
        var a = await db.VwLabAmostras.AsNoTracking().Where(x => x.IdLabHistAmostras == vm.IdAmostra)
            .Select(x => new { x.NrAmostra, x.ProdutoDescricao, x.TipoAmostra, x.TipoAnalise, x.ClienteNome, x.DtEntrega }).SingleOrDefaultAsync();
        if (a is null) return false;
        (vm.NrAmostra, vm.Produto, vm.TipoAmostra, vm.TipoAnalise, vm.Cliente, vm.DtEntrega) =
            (a.NrAmostra, a.ProdutoDescricao, a.TipoAmostra, a.TipoAnalise, a.ClienteNome, a.DtEntrega);
        return true;
    }

    /// <summary>Um cartão por teste da amostra, com o resultado dele (se houver) neste cabeçalho.</summary>
    private async Task<List<ResultadoLinhaVM>> LinhasAsync(int idAmostra, int? idResultado)
    {
        var testes = await db.LabHistAmostrasTestes.AsNoTracking().Where(t => t.IdLabHistAmostras == idAmostra).OrderBy(t => t.IdLabHistAmostrasTestes)
            .Select(t => new { t.IdLabHistAmostrasTestes, t.LabParametroAnalise.Descricao, Metodo = t.AnaliseMetodo.Descricao, t.LabParametroAnalise.Auditado })
            .ToListAsync();
        var parametros = idResultado is null ? [] : await db.LabResultadosParam.AsNoTracking().Where(p => p.IdLabResultadosAnalises == idResultado).ToListAsync();
        var noBoletim = idResultado is null ? [] : await db.LabBoletimParametros.AsNoTracking()
            .Where(b => b.LabResultadoParam.IdLabResultadosAnalises == idResultado).Select(b => b.IdLabResultadosParam).Distinct().ToListAsync();
        var porTeste = parametros.ToDictionary(p => p.IdLabHistAmostrasTestes);
        // teste do resultado que já não está na amostra (migração do legado): aparece também, para não sumir da tela
        var ids = testes.Select(t => t.IdLabHistAmostrasTestes).ToHashSet();
        var forasDaAmostra = parametros.Where(p => !ids.Contains(p.IdLabHistAmostrasTestes)).Select(p => p.IdLabHistAmostrasTestes).ToList();
        if (forasDaAmostra.Count > 0)
            testes.AddRange(await db.LabHistAmostrasTestes.AsNoTracking().Where(t => forasDaAmostra.Contains(t.IdLabHistAmostrasTestes))
                .Select(t => new { t.IdLabHistAmostrasTestes, t.LabParametroAnalise.Descricao, Metodo = t.AnaliseMetodo.Descricao, t.LabParametroAnalise.Auditado })
                .ToListAsync());

        return testes.Select(t =>
        {
            var l = new ResultadoLinhaVM { IdTeste = t.IdLabHistAmostrasTestes, Parametro = t.Descricao, Metodo = t.Metodo, Acreditado = t.Auditado };
            if (porTeste.TryGetValue(t.IdLabHistAmostrasTestes, out var p))
            {
                l.IdParam = p.IdLabResultadosParam;
                l.NoBoletim = noBoletim.Contains(p.IdLabResultadosParam);
                (l.IdAnalista, l.DtInicial, l.DtResultado, l.QtAmostra, l.IdUnidadeAmostra) =
                    (p.IdEntidadesFuncAnalise, p.DtInicial, p.DtResultado, p.QtAmostra?.ToString("0.####", PtBr), p.IdUnidadesAmostra);
                (l.Numerico, l.Simbolo, l.Valor, l.Expoente, l.IdUnidade, l.IdTipoResultado) =
                    (p.TipoResultado, p.SimboloGrandeza, p.VrResultado, p.VrExpResultado, p.IdUnidades, p.IdLabTiposResultados);
                (l.EspecNumerica, l.EspecSimbolo, l.EspecValor, l.EspecExpoente, l.EspecIdUnidade, l.EspecIdTipoResultado) =
                    (p.TipoResultadoEspecif, p.SimboloGrEspecif, p.VrResultadoEspecif, p.VrExpEspecif, p.IdUnidadesEspecif, p.IdLabTipoResultadosEsp);
                (l.Conforme, l.ResultadoFormatado, l.EspecFormatada) = (p.VrSatisfeito, p.ResultadoFormatado, p.EspecifFormatada);
            }
            return l;
        }).ToList();
    }

    // ------------------------------------------------------------------ gravar

    /// <summary>Grava o cabeçalho (na 1ª vez) e as linhas lançadas, numa transação. Devolve o ID do resultado.</summary>
    public async Task<Resultado<int>> GravarAsync(ResultadoVM vm)
    {
        LabResultadoAnalise? cabecalho = null;
        if (vm.Id == 0)
        {
            var abrir = await ResultadoDaAmostraAsync(vm.IdAmostra);
            if (!abrir.Sucesso) return Resultado.Erro(abrir.Mensagem!);
            if (abrir.Valor is not null) return Resultado.Erro("Esta amostra já tem resultado — abra o existente.");
        }
        else
        {
            cabecalho = await db.LabResultadosAnalises.SingleOrDefaultAsync(r => r.IdLabResultadosAnalises == vm.Id);
            if (cabecalho is null) return Resultado.Erro(MensagemNaoEncontrado);
            if (!cabecalho.UltimaRevisao) return Resultado.Erro(MensagemRevisaoAntiga);
            if (await db.LabBoletimAnalises.AnyAsync(b => b.IdLabResultadosAnalises == vm.Id && b.DhEmissao != null)) return Resultado.Erro(MensagemBoletimEmitido);
            vm.IdAmostra = cabecalho.IdLabHistAmostras;
        }
        if (ctx.ProfissionalId is not { } digitador)
            return Resultado.Erro("Seu usuário não está vinculado a um profissional (Usuários › profissional): o resultado registra quem digitou.");

        var entrega = await db.LabHistAmostras.Where(a => a.IdLabHistAmostras == vm.IdAmostra).Select(a => a.DtEntrega).SingleAsync();
        var hoje = await db.HojeAsync();
        var testes = (await db.LabHistAmostrasTestes.Where(t => t.IdLabHistAmostras == vm.IdAmostra).Select(t => t.IdLabHistAmostrasTestes).ToListAsync()).ToHashSet();
        var existentes = cabecalho is null ? [] : await db.LabResultadosParam.Where(p => p.IdLabResultadosAnalises == cabecalho.IdLabResultadosAnalises).ToListAsync();
        // quem executa análise e está ativo (o combo só oferece esses; o POST pode vir de um formulário antigo)
        var analistas = (await db.VwProfissionais.Where(p => p.ExecutaAnalise && !p.Inativo).Select(p => p.IdEntidades).ToListAsync()).ToHashSet();

        var gravar = new List<(ResultadoLinhaVM Linha, LabResultadoParam? Atual, Valores V)>();
        for (var i = 0; i < vm.Linhas.Count; i++)
        {
            var l = vm.Linhas[i];
            var atual = l.IdParam > 0 ? existentes.SingleOrDefault(p => p.IdLabResultadosParam == l.IdParam) : null;
            if (l.IdParam > 0 && atual is null) return Resultado.Erro($"{l.Parametro}: o resultado deste parâmetro não foi encontrado (pode ter sido excluído).");
            if (atual is null && !l.Lancar) continue;
            // linha gravada e não mexida: não revalida (dados migrados do legado podem não seguir as regras novas e não
            // podem impedir que o usuário lance os outros parâmetros) nem regrava
            if (atual is not null && !Mudou(l, atual)) continue;
            if (atual is null && !testes.Contains(l.IdTeste)) return Resultado.Erro("O parâmetro não pertence à amostra do resultado.");
            if (atual is null && existentes.Any(p => p.IdLabHistAmostrasTestes == l.IdTeste)) return Resultado.Erro($"{l.Parametro}: este parâmetro já tem resultado nesta revisão.");
            if (l.IdAnalista is { } an && !analistas.Contains(an) && an != atual?.IdEntidadesFuncAnalise)
                return Resultado.Erro($"{l.Parametro}: o responsável precisa ser um profissional ativo que executa análise.", Campo(i, nameof(l.IdAnalista)));
            var v = Validar(l, i, entrega, hoje);
            if (!v.Sucesso) return Resultado.Erro(v.Mensagem!, v.Campo);
            gravar.Add((l, atual, v.Valor));
        }
        if (gravar.Count == 0)
            return cabecalho is not null ? Resultado.Ok(cabecalho.IdLabResultadosAnalises) // nada mudou
                : Resultado.Erro("Lance o resultado de ao menos um parâmetro (\"Lançar\" ou \"Lançar todos\").");

        return await ErrosMySql.CapturarAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            if (cabecalho is null)
            {
                cabecalho = db.LabResultadosAnalises.Add(new LabResultadoAnalise { IdLabHistAmostras = vm.IdAmostra, IdEntidadesFuncDig = digitador }).Entity;
                await db.SaveChangesAsync();
            }
            foreach (var (l, atual, v) in gravar)
            {
                var p = atual ?? db.LabResultadosParam.Add(new LabResultadoParam
                {
                    IdLabResultadosAnalises = cabecalho.IdLabResultadosAnalises, IdLabHistAmostrasTestes = l.IdTeste, IdEntidadesFuncDig = digitador,
                }).Entity;
                (p.IdEntidadesFuncAnalise, p.DtInicial, p.DtResultado, p.QtAmostra, p.IdUnidadesAmostra) = (l.IdAnalista, v.Inicio, v.Fim, v.Qtde, l.IdUnidadeAmostra);
                (p.TipoResultado, p.SimboloGrandeza, p.VrResultado, p.VrExpResultado, p.IdUnidades, p.IdLabTiposResultados) =
                    (l.Numerico, v.Simbolo, v.Valor, l.Numerico ? l.Expoente : null, l.Numerico ? l.IdUnidade : null, l.Numerico ? null : l.IdTipoResultado);
                (p.TipoResultadoEspecif, p.SimboloGrEspecif, p.VrResultadoEspecif, p.VrExpEspecif, p.IdUnidadesEspecif, p.IdLabTipoResultadosEsp) =
                    (l.EspecNumerica, v.EspecSimbolo, v.EspecValor, l.EspecNumerica ? l.EspecExpoente : null, l.EspecNumerica ? l.EspecIdUnidade : null,
                     l.EspecNumerica ? null : l.EspecIdTipoResultado);
                p.VrSatisfeito = l.Conforme;
            }
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return cabecalho.IdLabResultadosAnalises;
        });
    }

    private sealed record Valores(DateOnly Inicio, DateOnly Fim, decimal? Qtde, string? Simbolo, string? Valor, string? EspecSimbolo, string? EspecValor);

    /// <summary>O formulário traz algo diferente do gravado? Compara o que vale para o tipo (o banco limpa o resto).</summary>
    private static bool Mudou(ResultadoLinhaVM l, LabResultadoParam p)
    {
        static bool Igual(string? a, string? b) => Textos.Limpo(a) == Textos.Limpo(b);
        if (l.IdAnalista != p.IdEntidadesFuncAnalise || l.DtInicial != p.DtInicial || l.DtResultado != p.DtResultado
            || !Igual(l.QtAmostra, p.QtAmostra?.ToString("0.####", PtBr)) || l.IdUnidadeAmostra != p.IdUnidadesAmostra
            || l.Numerico != p.TipoResultado || l.EspecNumerica != p.TipoResultadoEspecif || l.Conforme != p.VrSatisfeito)
            return true;
        var resultado = l.Numerico
            ? !Igual(l.Simbolo, p.SimboloGrandeza) || !Igual(l.Valor, p.VrResultado) || (l.Expoente ?? 0) != (p.VrExpResultado ?? 0) || l.IdUnidade != p.IdUnidades
            : l.IdTipoResultado != p.IdLabTiposResultados;
        var especificacao = l.EspecNumerica
            ? !Igual(l.EspecSimbolo, p.SimboloGrEspecif) || !Igual(l.EspecValor, p.VrResultadoEspecif) || (l.EspecExpoente ?? 0) != (p.VrExpEspecif ?? 0) || l.EspecIdUnidade != p.IdUnidadesEspecif
            : l.EspecIdTipoResultado != p.IdLabTipoResultadosEsp;
        return resultado || especificacao;
    }

    private static string Campo(int i, string campo) => $"{nameof(ResultadoVM.Linhas)}[{i}].{campo}";

    /// <summary>Regras da linha antes do banco (RN-07.04/06/08); mensagens com o nome do parâmetro, no campo.</summary>
    private static Resultado<Valores> Validar(ResultadoLinhaVM l, int i, DateOnly entrega, DateOnly hoje)
    {
        var nome = l.Parametro ?? "Parâmetro";
        if (l.DtInicial is not { } inicio) return Resultado.Erro($"{nome}: data de início é obrigatória.", Campo(i, nameof(l.DtInicial)));
        if (l.DtResultado is not { } fim) return Resultado.Erro($"{nome}: data do resultado é obrigatória.", Campo(i, nameof(l.DtResultado)));
        if (inicio < entrega) return Resultado.Erro($"{nome}: a data de início não pode ser antes da entrega da amostra ({entrega:dd/MM/yyyy}).", Campo(i, nameof(l.DtInicial)));
        if (fim < inicio) return Resultado.Erro($"{nome}: a data do resultado não pode ser antes da data de início.", Campo(i, nameof(l.DtResultado)));
        if (fim > hoje) return Resultado.Erro($"{nome}: a data do resultado não pode ser depois de hoje.", Campo(i, nameof(l.DtResultado)));

        decimal? qtde = null;
        if (Textos.Limpo(l.QtAmostra) is { } q)
        {
            if (!Textos.Decimal(q, out var d) || d < 0 || decimal.Round(d, 4) != d)
                return Resultado.Erro($"{nome}: quantidade de amostra deve ser um número (até 4 casas decimais).", Campo(i, nameof(l.QtAmostra)));
            qtde = d;
        }

        var simbolo = Textos.Limpo(l.Simbolo);
        var valor = Textos.Limpo(l.Valor);
        if (l.Numerico)
        {
            if (valor is null) return Resultado.Erro($"{nome}: informe o valor do resultado.", Campo(i, nameof(l.Valor)));
            if (Erro(valor, simbolo, l.Expoente) is { } e) return Resultado.Erro($"{nome}: {e}", Campo(i, nameof(l.Valor)));
        }
        else if (l.IdTipoResultado is null) return Resultado.Erro($"{nome}: escolha o tipo de resultado (descritivo).", Campo(i, nameof(l.IdTipoResultado)));

        var especSimbolo = Textos.Limpo(l.EspecSimbolo);
        var especValor = Textos.Limpo(l.EspecValor);
        if (l.EspecNumerica && (especValor is not null || especSimbolo is not null || l.EspecExpoente is not null)
            && Erro(especValor, especSimbolo, l.EspecExpoente) is { } ee)
            return Resultado.Erro($"{nome}: especificação — {ee}", Campo(i, nameof(l.EspecValor)));

        return Resultado.Ok(new Valores(inicio, fim, qtde, l.Numerico ? simbolo : null, l.Numerico ? valor : null,
            l.EspecNumerica ? especSimbolo : null, l.EspecNumerica ? especValor : null));
    }

    /// <summary>Valor com vírgula ("3,5"), símbolo da lista e expoente pequeno; null = ok.</summary>
    public static string? Erro(string? valor, string? simbolo, short? expoente)
    {
        if (valor is null) return "informe o valor.";
        if (valor.Length > 20 || !NumeroComVirgula().IsMatch(valor)) return $"\"{valor}\" não é um número (use vírgula para decimais, ex.: 3,5).";
        if (simbolo is not null && !ResultadoOpcoesVM.Simbolos.Contains(simbolo)) return $"símbolo \"{simbolo}\" inválido.";
        if (expoente is < -99 or > 99) return "o expoente (× 10) deve estar entre −99 e 99.";
        return null;
    }

    [GeneratedRegex(@"^-?\d+(,\d+)?$")]
    private static partial Regex NumeroComVirgula();

    // ------------------------------------------------------------------ excluir, revisão

    /// <summary>Exclui o resultado de um parâmetro (o banco recusa se estiver num boletim ou em revisão antiga).</summary>
    public async Task<Resultado> ExcluirLinhaAsync(int id, int idParam)
    {
        var p = await db.LabResultadosParam.SingleOrDefaultAsync(x => x.IdLabResultadosParam == idParam && x.IdLabResultadosAnalises == id);
        if (p is null) return Resultado.Erro(MensagemNaoEncontrado);
        if (await db.LabBoletimParametros.AnyAsync(b => b.IdLabResultadosParam == idParam))
            return Resultado.Erro("Resultado usado em boletim: retire-o do boletim antes de excluir.");
        return await ErrosMySql.CapturarAsync(async () =>
        {
            db.LabResultadosParam.Remove(p);
            await db.SaveChangesAsync();
        });
    }

    /// <summary>"Excluir todos os resultados desta amostra": só a revisão 0 e sem boletim (RN-07.12).</summary>
    public async Task<Resultado> ExcluirTodosAsync(int id)
    {
        var r = await db.LabResultadosAnalises.SingleOrDefaultAsync(x => x.IdLabResultadosAnalises == id);
        if (r is null) return Resultado.Erro(MensagemNaoEncontrado);
        if (r.Revisao > 0) return Resultado.Erro("Para excluir uma revisão use \"Desfazer última revisão\".");
        if (await db.LabBoletimAnalises.AnyAsync(b => b.IdLabResultadosAnalises == id)) return Resultado.Erro("Resultado possui boletim e não pode ser excluído.");
        return await ErrosMySql.CapturarAsync(async () =>
        {
            db.LabResultadosAnalises.Remove(r); // as linhas vão junto (FK em cascata)
            await db.SaveChangesAsync();
        });
    }

    /// <summary>Gera a revisão (P15) e devolve o ID do resultado novo; com <paramref name="revisarBoletim"/>, o boletim também.</summary>
    public async Task<Resultado<int>> GerarRevisaoAsync(int id, string? motivo, bool revisarBoletim)
    {
        if (Textos.Limpo(motivo) is not { } m) return Resultado.Erro("Informe o motivo da revisão.", "motivo");
        var r = await procedures.GerarRevisaoResultadoAsync(id, m.Length > 500 ? m[..500] : m, revisarBoletim);
        return r.Sucesso ? Resultado.Ok(r.Valor.IdNovo) : Resultado.Erro(r.Mensagem!);
    }

    /// <summary>Desfaz a última revisão (sem boletim) e devolve o ID da revisão anterior, que volta a ser a vigente.</summary>
    public async Task<Resultado<int>> DesfazerRevisaoAsync(int id)
    {
        var anterior = await db.LabResultadosAnalises.Where(x => x.IdLabResultadosAnalises == id).Select(x => x.IdResultadoOrigem).SingleOrDefaultAsync();
        if (anterior is null) return Resultado.Erro("Este resultado não é uma revisão: não há o que desfazer.");
        var r = await procedures.ExcluirRevisaoResultadoAsync(id);
        return r.Sucesso ? Resultado.Ok(anterior.Value) : Resultado.Erro(r.Mensagem!);
    }
}
