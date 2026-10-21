using System.Globalization;
using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services;

/// <summary>
/// T14 — Produtos (nomes das amostras do cliente). Código vazio na inclusão → <c>TG_PRODUTOS_BI</c> gera os 6 dígitos
/// pela sequência PRODUTO da empresa e o EF relê; código digitado vale como está (único: <c>UQ_PROD_COD</c>).
/// O filtro global limita tudo à empresa ativa.
/// </summary>
public sealed class ProdutoService(LabDbContext db)
{
    public const string MensagemNaoEncontrado = "O produto não foi encontrado (pode ter sido excluído).";
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public async Task<ProdutoListaVM> ListarAsync(ProdutoFiltroVM f)
    {
        var q = db.Produtos.AsNoTracking();
        if (Textos.Limpo(f.Texto) is { } t) q = q.Where(p => p.Codigo.Contains(t) || p.Descricao.Contains(t));
        // inativos que a pesquisa acharia: a tela avisa (senão o produto "some" depois de inativado)
        var ocultos = f.Inativos ? 0 : await q.CountAsync(p => p.Inativo);
        if (!f.Inativos) q = q.Where(p => !p.Inativo);
        var total = await q.CountAsync();
        f.Pagina = Paginacao.Ajustar(f.Pagina, total, ProdutoFiltroVM.TamanhoPagina);
        var itens = await q.OrderBy(p => p.Descricao).Skip((f.Pagina - 1) * ProdutoFiltroVM.TamanhoPagina).Take(ProdutoFiltroVM.TamanhoPagina)
            .Select(p => new { p.IdProdutos, p.Codigo, p.Descricao, Unidade = p.Unidade.Sigla,
                Embalagem = p.EmbalagemTipo != null ? p.EmbalagemTipo.Descricao : null, p.QtdeEmbalagem, p.Inativo })
            .ToListAsync();
        return new ProdutoListaVM
        {
            Filtro = f, Total = total, InativosOcultos = ocultos,
            Itens = itens.Select(p => new ProdutoListaItemVM(p.IdProdutos, p.Codigo, p.Descricao, p.Unidade, p.Embalagem,
                Formatar(p.QtdeEmbalagem), p.Inativo)).ToList(),
        };
    }

    public async Task<ProdutoVM> NovoAsync()
    {
        var vm = new ProdutoVM();
        await PrepararAsync(vm);
        return vm;
    }

    public async Task<ProdutoVM?> CarregarAsync(int id)
    {
        var p = await db.Produtos.AsNoTracking().SingleOrDefaultAsync(x => x.IdProdutos == id);
        if (p is null) return null;
        var vm = new ProdutoVM
        {
            Id = p.IdProdutos, Codigo = p.Codigo, Descricao = p.Descricao, IdUnidades = p.IdUnidades,
            IdEmbalagensTipos = p.IdEmbalagensTipos, QtdeEmbalagem = Formatar(p.QtdeEmbalagem), Inativo = p.Inativo,
        };
        await PrepararAsync(vm);
        return vm;
    }

    /// <summary>Combos: unidades e embalagens ativas + as já escolhidas (registro antigo continua mostrando o valor).</summary>
    public async Task PrepararAsync(ProdutoVM vm)
    {
        vm.Unidades = await db.Unidades.AsNoTracking().Where(u => u.Ativo || u.IdUnidades == vm.IdUnidades)
            .OrderBy(u => u.Descricao).Select(u => new OpcaoVM(u.IdUnidades, u.Descricao)).ToListAsync();
        vm.Embalagens = await db.EmbalagensTipos.AsNoTracking().Where(e => e.Ativo || e.IdEmbalagensTipos == vm.IdEmbalagensTipos)
            .OrderBy(e => e.Descricao).Select(e => new OpcaoVM(e.IdEmbalagensTipos, e.Descricao)).ToListAsync();
    }

    /// <summary>Inclui (Id = 0) ou altera; devolve o ID. Erro de campo traz a propriedade em <c>Campo</c>.</summary>
    public async Task<Resultado<int>> GravarAsync(ProdutoVM vm)
    {
        var descricao = Textos.Maiusculas(vm.Descricao);
        if (descricao is null) return Resultado.Erro("Informe a descrição.", nameof(vm.Descricao));
        if (descricao.Length > 150) return Resultado.Erro("Descrição: no máximo 150 caracteres.", nameof(vm.Descricao));
        var codigo = Textos.Maiusculas(vm.Codigo);
        if (codigo?.Length > 10) return Resultado.Erro("Código: no máximo 10 caracteres.", nameof(vm.Codigo));
        if (vm.Id != 0 && codigo is null) return Resultado.Erro("Informe o código (só na inclusão ele é gerado sozinho).", nameof(vm.Codigo));

        var atual = vm.Id == 0 ? null : await db.Produtos.SingleOrDefaultAsync(p => p.IdProdutos == vm.Id);
        if (vm.Id != 0 && atual is null) return Resultado.Erro(MensagemNaoEncontrado);

        // unidade/embalagem da empresa ativa; inativa só se já era a do produto
        if (vm.IdUnidades is not { } unidade
            || !await db.Unidades.AnyAsync(u => u.IdUnidades == unidade && (u.Ativo || u.IdUnidades == (atual == null ? 0 : atual.IdUnidades))))
            return Resultado.Erro("Escolha a unidade de estoque.", nameof(vm.IdUnidades));
        if (vm.IdEmbalagensTipos is { } emb
            && !await db.EmbalagensTipos.AnyAsync(e => e.IdEmbalagensTipos == emb && (e.Ativo || e.IdEmbalagensTipos == (atual == null ? 0 : atual.IdEmbalagensTipos))))
            return Resultado.Erro("Escolha uma embalagem da lista.", nameof(vm.IdEmbalagensTipos));

        decimal? qtde = null;
        if (Textos.Limpo(vm.QtdeEmbalagem) is { } textoQtde)
        {
            if (!decimal.TryParse(textoQtde.Replace(',', '.'), NumberStyles.Number & ~NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var q)
                || q <= 0 || q >= 100_000_000_000m || decimal.Round(q, 4) != q)
                return Resultado.Erro("Qtde. por embalagem: número maior que zero, com até 4 casas decimais.", nameof(vm.QtdeEmbalagem));
            qtde = q;
        }

        var p = atual ?? db.Produtos.Add(new Produto()).Entity;
        // na inclusão sem código o EF omite a coluna e a trigger gera; com código digitado ele é enviado
        if (codigo is not null) p.Codigo = codigo;
        (p.Descricao, p.IdUnidades, p.IdEmbalagensTipos, p.QtdeEmbalagem, p.Inativo) = (descricao, unidade, vm.IdEmbalagensTipos, qtde, vm.Inativo);

        var r = await ErrosMySql.CapturarAsync(() => db.SaveChangesAsync());
        if (!r.Sucesso)
        {
            db.ChangeTracker.Clear();
            return Resultado.Erro(r.Mensagem!, r.Tipo == TipoFalha.Duplicado ? nameof(vm.Codigo) : null, r.Tipo);
        }
        return Resultado.Ok(p.IdProdutos);
    }

    /// <summary>Cadastro rápido (proposta): descrição + unidade, código automático. Devolve <c>(id, texto)</c> para o combo.</summary>
    public async Task<Resultado<OpcaoVM>> IncluirRapidoAsync(ProdutoRapidoVM vm)
    {
        var r = await GravarAsync(new ProdutoVM { Descricao = vm.Descricao ?? "", IdUnidades = vm.IdUnidades });
        if (!r.Sucesso) return r.Falha;
        var texto = await db.Produtos.AsNoTracking().Where(p => p.IdProdutos == r.Valor).Select(p => p.Descricao).SingleAsync();
        return Resultado.Ok(new OpcaoVM(r.Valor, texto));
    }

    /// <summary>Exclui só sem uso: proposta, pedido ou amostra com o produto → o banco recusa (1451) e a tela sugere inativar.</summary>
    public async Task<Resultado> ExcluirAsync(int id)
    {
        var r = await ErrosMySql.CapturarAsync(() => db.Produtos.Where(p => p.IdProdutos == id).ExecuteDeleteAsync());
        if (!r.Sucesso) return r.Falha;
        return r.Valor == 0 ? Resultado.Erro(MensagemNaoEncontrado) : Resultado.Ok();
    }

    public async Task<Resultado> DefinirInativoAsync(int id, bool inativo)
    {
        var n = await db.Produtos.Where(p => p.IdProdutos == id).ExecuteUpdateAsync(s => s.SetProperty(p => p.Inativo, inativo));
        return n == 0 ? Resultado.Erro(MensagemNaoEncontrado) : Resultado.Ok();
    }

    private static string? Formatar(decimal? v) => v?.ToString("0.####", PtBr);
}
