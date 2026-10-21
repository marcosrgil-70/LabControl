using System.Globalization;
using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services;

/// <summary>
/// T15 — Parâmetros de análise. A descrição reduzida é obrigatória (é o texto impresso no boletim; opcional no legado,
/// causava erro na revisão do boletim); o método padrão filtra o combo de método na proposta; "acreditado" separa as
/// metodologias do boletim (<c>FN_ACREDITADO</c>). Descrição única por empresa (<c>UQ_PARAM_DESC</c>).
/// </summary>
public sealed class ParametroAnaliseService(LabDbContext db)
{
    public const string MensagemNaoEncontrado = "O parâmetro de análise não foi encontrado (pode ter sido excluído).";
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public async Task<ParametroAnaliseListaVM> ListarAsync(ParametroAnaliseFiltroVM f)
    {
        var q = db.LabParametrosAnalises.AsNoTracking();
        if (Textos.Limpo(f.Texto) is { } t) q = q.Where(p => p.Descricao.Contains(t) || p.DescReduzida.Contains(t));
        if (f.IdAnalisesTipo is { } tipo) q = q.Where(p => p.IdAnalisesTipo == tipo);
        if (f.Acreditado is "sim" or "nao") { var sim = f.Acreditado == "sim"; q = q.Where(p => p.Auditado == sim); }
        // inativos que a pesquisa acharia: a tela avisa (senão o parâmetro "some" depois de inativado)
        var ocultos = f.Inativos ? 0 : await q.CountAsync(p => p.Inativo);
        if (!f.Inativos) q = q.Where(p => !p.Inativo);
        var total = await q.CountAsync();
        f.Pagina = Paginacao.Ajustar(f.Pagina, total, ParametroAnaliseFiltroVM.TamanhoPagina);
        var itens = await q.OrderBy(p => p.Descricao).Skip((f.Pagina - 1) * ParametroAnaliseFiltroVM.TamanhoPagina).Take(ParametroAnaliseFiltroVM.TamanhoPagina)
            .Select(p => new { p.IdLabParametrosAnalises, p.Descricao, p.DescReduzida, TipoCodigo = p.AnaliseTipo.Codigo, Tipo = p.AnaliseTipo.Descricao,
                Metodo = p.AnaliseMetodo != null ? p.AnaliseMetodo.Descricao : null, p.VrUnitario, p.Auditado, p.Inativo })
            .ToListAsync();
        return new ParametroAnaliseListaVM
        {
            Filtro = f, Total = total, InativosOcultos = ocultos, Tipos = await TiposAsync(f.IdAnalisesTipo),
            Itens = itens.Select(p => new ParametroAnaliseListaItemVM(p.IdLabParametrosAnalises, p.Descricao, p.DescReduzida,
                $"{p.TipoCodigo:00} — {p.Tipo}", p.Metodo, Moeda(p.VrUnitario), p.Auditado, p.Inativo)).ToList(),
        };
    }

    public async Task<ParametroAnaliseVM> NovoAsync()
    {
        var vm = new ParametroAnaliseVM();
        await PrepararAsync(vm);
        return vm;
    }

    public async Task<ParametroAnaliseVM?> CarregarAsync(int id)
    {
        var p = await db.LabParametrosAnalises.AsNoTracking().SingleOrDefaultAsync(x => x.IdLabParametrosAnalises == id);
        if (p is null) return null;
        var vm = new ParametroAnaliseVM
        {
            Id = p.IdLabParametrosAnalises, Descricao = p.Descricao, DescReduzida = p.DescReduzida, IdAnalisesTipo = p.IdAnalisesTipo,
            IdAnalisesMetodos = p.IdAnalisesMetodos, DescrMetodo = p.DescrMetodo, VrUnitario = Moeda(p.VrUnitario),
            Auditado = p.Auditado, Inativo = p.Inativo,
        };
        await PrepararAsync(vm);
        return vm;
    }

    /// <summary>Combos: tipos de análise e métodos ativos + os já escolhidos.</summary>
    public async Task PrepararAsync(ParametroAnaliseVM vm)
    {
        vm.Tipos = await TiposAsync(vm.IdAnalisesTipo);
        vm.Metodos = await db.AnalisesMetodos.AsNoTracking().Where(m => m.Ativo || m.IdAnalisesMetodos == vm.IdAnalisesMetodos)
            .OrderBy(m => m.Descricao).Select(m => new OpcaoVM(m.IdAnalisesMetodos, m.Descricao)).ToListAsync();
    }

    /// <summary>Inclui (Id = 0) ou altera; devolve o ID. Erro de campo traz a propriedade em <c>Campo</c>.</summary>
    public async Task<Resultado<int>> GravarAsync(ParametroAnaliseVM vm)
    {
        var descricao = Textos.Maiusculas(vm.Descricao);
        if (descricao is null) return Resultado.Erro("Informe o parâmetro.", nameof(vm.Descricao));
        var reduzida = Textos.Maiusculas(vm.DescReduzida);
        if (reduzida is null) return Resultado.Erro("Informe a descrição reduzida (é o texto impresso no boletim).", nameof(vm.DescReduzida));
        var descrMetodo = Textos.Maiusculas(vm.DescrMetodo);
        foreach (var (texto, campo, rotulo) in new[] { (descricao, nameof(vm.Descricao), "Parâmetro"), (reduzida, nameof(vm.DescReduzida), "Descrição reduzida"),
                     (descrMetodo, nameof(vm.DescrMetodo), "Descrição do método") })
            if (texto?.Length > 150) return Resultado.Erro($"{rotulo}: no máximo 150 caracteres.", campo);

        var atual = vm.Id == 0 ? null : await db.LabParametrosAnalises.SingleOrDefaultAsync(p => p.IdLabParametrosAnalises == vm.Id);
        if (vm.Id != 0 && atual is null) return Resultado.Erro(MensagemNaoEncontrado);

        // tipo/método da empresa ativa; inativo só se já era o do parâmetro
        if (vm.IdAnalisesTipo is not { } tipo
            || !await db.AnalisesTipo.AnyAsync(t => t.IdAnalisesTipo == tipo && (t.Ativo || t.IdAnalisesTipo == (atual == null ? 0 : atual.IdAnalisesTipo))))
            return Resultado.Erro("Escolha o tipo de análise.", nameof(vm.IdAnalisesTipo));
        if (vm.IdAnalisesMetodos is { } metodo
            && !await db.AnalisesMetodos.AnyAsync(m => m.IdAnalisesMetodos == metodo && (m.Ativo || m.IdAnalisesMetodos == (atual == null ? 0 : atual.IdAnalisesMetodos))))
            return Resultado.Erro("Escolha um método da lista.", nameof(vm.IdAnalisesMetodos));

        if (!LerMoeda(vm.VrUnitario, out var valor))
            return Resultado.Erro("Valor unitário: em reais, maior ou igual a zero, com até 2 casas (ex.: 1.234,56).", nameof(vm.VrUnitario));

        var p = atual ?? db.LabParametrosAnalises.Add(new LabParametroAnalise()).Entity;
        (p.Descricao, p.DescReduzida, p.IdAnalisesTipo, p.IdAnalisesMetodos, p.DescrMetodo, p.VrUnitario, p.Auditado, p.Inativo) =
            (descricao, reduzida, tipo, vm.IdAnalisesMetodos, descrMetodo, valor, vm.Auditado, vm.Inativo);

        var r = await ErrosMySql.CapturarAsync(() => db.SaveChangesAsync());
        if (!r.Sucesso)
        {
            db.ChangeTracker.Clear();
            return Resultado.Erro(r.Mensagem!, r.Tipo == TipoFalha.Duplicado ? nameof(vm.Descricao) : null, r.Tipo);
        }
        return Resultado.Ok(p.IdLabParametrosAnalises);
    }

    /// <summary>Exclui só sem uso: proposta, teste da amostra ou resultado com o parâmetro → o banco recusa (1451) e a tela sugere inativar.</summary>
    public async Task<Resultado> ExcluirAsync(int id)
    {
        var r = await ErrosMySql.CapturarAsync(() => db.LabParametrosAnalises.Where(p => p.IdLabParametrosAnalises == id).ExecuteDeleteAsync());
        if (!r.Sucesso) return r.Falha;
        return r.Valor == 0 ? Resultado.Erro(MensagemNaoEncontrado) : Resultado.Ok();
    }

    public async Task<Resultado> DefinirInativoAsync(int id, bool inativo)
    {
        var n = await db.LabParametrosAnalises.Where(p => p.IdLabParametrosAnalises == id).ExecuteUpdateAsync(s => s.SetProperty(p => p.Inativo, inativo));
        return n == 0 ? Resultado.Erro(MensagemNaoEncontrado) : Resultado.Ok();
    }

    // "01 — CONTAGEM / PESQUISA" (o código é o AA do número da amostra)
    private async Task<IReadOnlyList<OpcaoVM>> TiposAsync(int? escolhido) =>
        (await db.AnalisesTipo.AsNoTracking().Where(t => t.Ativo || t.IdAnalisesTipo == escolhido).OrderBy(t => t.Codigo)
            .Select(t => new { t.IdAnalisesTipo, t.Codigo, t.Descricao }).ToListAsync())
        .Select(t => new OpcaoVM(t.IdAnalisesTipo, $"{t.Codigo:00} — {t.Descricao}")).ToList();

    private static string Moeda(decimal v) => v.ToString("#,##0.00", PtBr);

    // "1.234,56" (tela) ou "1234.56"; vazio = 0
    private static bool LerMoeda(string? texto, out decimal valor)
    {
        valor = 0;
        if (Textos.Limpo(texto) is not { } t) return true;
        var ok = t.Contains(',')
            ? decimal.TryParse(t, NumberStyles.Number, PtBr, out valor)
            : decimal.TryParse(t, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out valor);
        return ok && valor >= 0 && valor < 10_000_000_000_000m && decimal.Round(valor, 2) == valor;
    }
}
