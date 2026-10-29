using System.Globalization;
using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services.Relatorios;

/// <summary>Filtros que cada relatório mostra (além do período, sempre obrigatório).</summary>
[Flags]
public enum FiltrosRelatorio
{
    Nenhum = 0, Cliente = 1, Cobranca = 2, Situacao = 4, Produto = 8, Parametro = 16, TipoAnalise = 32,
    Acreditado = 64, Analista = 128, SomenteComPedido = 256, AgruparPorCliente = 512,
}

/// <param name="Nome">Action do controller (e rota <c>/Relatorios/{Nome}</c>).</param>
/// <param name="Periodo">O que a data do período filtra (rótulo da tela).</param>
public sealed record DefinicaoRelatorio(string Nome, string Titulo, string Icone, string Periodo, FiltrosRelatorio Filtros, string Descricao);

/// <summary>
/// Relatórios gerenciais T30-01…06 (tarefa 8.1; P20, RN-09). Sempre a empresa ativa (filtro global do contexto),
/// consultas parametrizadas pelo EF e só a revisão vigente: a amostra física (revisão 0) conta uma vez, o resultado é o
/// da última revisão e o pedido usa a revisão da proposta vigente — os três erros de contagem do legado.
/// </summary>
public sealed class RelatoriosService(LabDbContext db, IEmpresaContexto ctx)
{
    public const string PeriodoInvalido = "Período de datas inválido. Por favor, verificar.";
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static readonly IReadOnlyList<DefinicaoRelatorio> Catalogo =
    [
        new("Faturamento", "Faturamento", "bi-cash-coin", "Autorização do pedido",
            FiltrosRelatorio.Cliente | FiltrosRelatorio.Cobranca | FiltrosRelatorio.Situacao,
            "Pedidos de venda não cancelados autorizados no período, um por pedido, com os valores da revisão vigente da proposta."),
        new("AmostrasPeriodo", "Nº de amostras por período", "bi-calendar-range", "Entrega e resultado",
            FiltrosRelatorio.SomenteComPedido,
            "Por cliente: amostras físicas entregues no período e amostras com resultado (última revisão) no período."),
        new("AmostrasCliente", "Nº de amostras por cliente", "bi-person-lines-fill", "Entrega e resultado",
            FiltrosRelatorio.Cliente | FiltrosRelatorio.Produto | FiltrosRelatorio.SomenteComPedido,
            "Por cliente e produto: amostras físicas entregues no período e amostras com resultado (última revisão) no período."),
        new("ParametrosAnalisados", "Nº de parâmetros analisados", "bi-list-check", "Data do resultado",
            FiltrosRelatorio.Cliente | FiltrosRelatorio.Parametro | FiltrosRelatorio.TipoAnalise | FiltrosRelatorio.Acreditado | FiltrosRelatorio.AgruparPorCliente,
            "Parâmetros com resultado no período (última revisão do resultado), com o total por parâmetro."),
        new("TempoAnalises", "Tempo de duração das análises", "bi-stopwatch", "Data do resultado",
            FiltrosRelatorio.Parametro | FiltrosRelatorio.TipoAnalise,
            "Dias entre o início e o resultado (contando os dois dias, como o legado), com o prazo previsto e o atraso."),
        new("Producao", "Produção por período", "bi-bar-chart", "Data do resultado",
            FiltrosRelatorio.Parametro | FiltrosRelatorio.Produto | FiltrosRelatorio.Analista,
            "Análises com resultado no período, com a quantidade de amostra e o analista; totais por dia e por parâmetro."),
    ];

    public static DefinicaoRelatorio? Definicao(string? nome) =>
        Catalogo.FirstOrDefault(d => string.Equals(d.Nome, nome, StringComparison.OrdinalIgnoreCase));

    /// <summary>Período padrão: o mês corrente (relógio do banco).</summary>
    public async Task PreencherPadraoAsync(RelatorioFiltroVM f)
    {
        if (f.De is not null || f.Ate is not null) return;
        var hoje = await db.HojeAsync();
        f.De = new DateOnly(hoje.Year, hoje.Month, 1);
        f.Ate = f.De.Value.AddMonths(1).AddDays(-1);
    }

    public async Task<Resultado<RelatorioDados>> GerarAsync(DefinicaoRelatorio d, RelatorioFiltroVM f)
    {
        if (f.De is not { } de || f.Ate is not { } ate || ate < de) return Resultado.Erro(PeriodoInvalido, nameof(f.De));
        var r = new RelatorioDados { Nome = d.Nome, Titulo = d.Titulo, Subtitulo = Subtitulo(d, f) };
        switch (d.Nome)
        {
            case "Faturamento": await FaturamentoAsync(r, f, de, ate); break;
            case "AmostrasPeriodo": await AmostrasAsync(r, f, de, ate, porProduto: false); break;
            case "AmostrasCliente": await AmostrasAsync(r, f, de, ate, porProduto: true); break;
            case "ParametrosAnalisados": await ParametrosAnalisadosAsync(r, f, de, ate); break;
            case "TempoAnalises": await TempoAnalisesAsync(r, f, de, ate); break;
            case "Producao": await ProducaoAsync(r, f, de, ate); break;
            default: throw new ArgumentException($"Relatório desconhecido: {d.Nome}", nameof(d));
        }
        return Resultado.Ok(r);
    }

    /// <summary>Empresa ativa para o cabeçalho do PDF (logo que o QuestPDF não lê sai sem ele).</summary>
    public async Task<EmpresaRelatorio> EmpresaAsync()
    {
        var e = await db.Empresas.AsNoTracking().Where(x => x.IdEmpresas == ctx.EmpresaId)
            .Select(x => new { x.RazaoSocial, x.Cnpj, x.Logo }).SingleAsync();
        byte[]? logo = null;
        if (e.Logo is not null)
            try { QuestPDF.Infrastructure.Image.FromBinaryData(e.Logo).Dispose(); logo = e.Logo; }
            catch (Exception ex) when (ex is not OutOfMemoryException) { }
        return new EmpresaRelatorio(e.RazaoSocial, e.Cnpj is { Length: > 0 } c ? Documentos.FormatarCnpj(c) : null, logo);
    }

    // ------------------------------------------------------------------ T30-01

    private async Task FaturamentoAsync(RelatorioDados r, RelatorioFiltroVM f, DateOnly de, DateOnly ate)
    {
        // RN-09.07: uma linha por pedido (o pedido aponta para a revisão vigente da proposta); o legado lia todas as revisões
        var q = db.PedidosVenda.AsNoTracking().Where(p => p.Situacao != SituacaoPedido.Cancelado && p.DtAutorizacao >= de && p.DtAutorizacao <= ate);
        if (Textos.Limpo(f.Cliente) is { } c)
            q = q.Where(p => p.Entidade.Nome.Contains(c) || (p.Entidade.NomeFantasia != null && p.Entidade.NomeFantasia.Contains(c))
                             || (p.Entidade.CpfCnpj != null && p.Entidade.CpfCnpj.Contains(c)));
        if (Textos.Limpo(f.Cobranca) is { } cb)
            q = q.Where(p => p.EntCobranca.Nome.Contains(cb) || (p.EntCobranca.NomeFantasia != null && p.EntCobranca.NomeFantasia.Contains(cb))
                             || (p.EntCobranca.CpfCnpj != null && p.EntCobranca.CpfCnpj.Contains(cb)));
        if (f.Situacao is { } s) q = q.Where(p => p.Situacao == s);
        var pedidos = await q.Select(p => new
            {
                p.DtAutorizacao, p.NrPedido, p.LabProposta.NrProposta, p.IdEntidades, p.Entidade.Nome, p.Entidade.CpfCnpj,
                Cobranca = p.EntCobranca.Nome, Condicao = p.CondicaoPagto.Descricao,
                p.LabProposta.PorcDesconto, p.LabProposta.VrDesconto, p.LabProposta.VrTotalProposta,
            })
            .ToListAsync();

        var t = new TabelaRelatorio
        {
            Colunas =
            [
                new("Autorização"), new("Pedido"), new("Proposta"), new("CPF/CNPJ", Largura: 1.3f), new("Cliente", Largura: 2.6f),
                new("Cobrança", Largura: 2.2f), new("Condição", Largura: 1.3f), new("% desc.", true, 0.7f), new("Desconto (R$)", true), new("Total (R$)", true),
            ],
        };
        foreach (var g in pedidos.GroupBy(p => (p.IdEntidades, p.Nome)).OrderBy(g => g.Key.Nome, StringComparer.Create(PtBr, true)).ThenBy(g => g.Key.IdEntidades))
        {
            foreach (var p in g.OrderBy(p => p.DtAutorizacao).ThenBy(p => p.NrPedido))
                t.Dado(Data(p.DtAutorizacao), PedidoVendaService.Numero(p.NrPedido), p.NrProposta, p.CpfCnpj, p.Nome, p.Cobranca, p.Condicao,
                    Valor(p.PorcDesconto), Valor(p.VrDesconto), Valor(p.VrTotalProposta));
            t.Subtotal(null, null, null, null, $"Subtotal — {g.Key.Nome} ({Qtde(g.Count(), "pedido")})", null, null, null,
                Valor(g.Sum(p => p.VrDesconto)), Valor(g.Sum(p => p.VrTotalProposta)));
        }
        t.Total(null, null, null, null, $"Total geral ({Qtde(pedidos.Count, "pedido")})", null, null, null,
            Valor(pedidos.Sum(p => p.VrDesconto)), Valor(pedidos.Sum(p => p.VrTotalProposta)));
        r.Tabelas.Add(t);
        r.Notas.Add("Uma linha por pedido de venda não cancelado (o legado repetia o pedido em cada revisão da proposta). Valores da revisão vigente da proposta.");
    }

    // ------------------------------------------------------------------ T30-02 e T30-03

    private sealed record ContagemAmostras(int IdCliente, string Cliente, string? CpfCnpj, int IdProduto, string ProdutoCodigo, string Produto, int Quantidade);

    private async Task AmostrasAsync(RelatorioDados r, RelatorioFiltroVM f, DateOnly de, DateOnly ate, bool porProduto)
    {
        // uma linha por amostra física: a última revisão (cliente, produto e pedido vigentes). Direto nas tabelas:
        // agrupar o ano inteiro pela VW_LAB_AMOSTRAS levava 3,6 s por consulta (0,15 s assim, 2017 do legado)
        var fisicas = db.LabHistAmostras.AsNoTracking().Where(a => a.UltimaRevisao);
        if (f.SomenteComPedido) fisicas = fisicas.Where(a => a.IdPedidosVenda != null);   // RN-09.08, padrão do legado
        if (porProduto && Textos.Limpo(f.Cliente) is { } c)
            fisicas = fisicas.Where(a => a.Entidade.Nome.Contains(c) || (a.Entidade.NomeFantasia != null && a.Entidade.NomeFantasia.Contains(c))
                                         || (a.Entidade.CpfCnpj != null && a.Entidade.CpfCnpj.Contains(c)));
        if (porProduto && Textos.Limpo(f.Produto) is { } pd) fisicas = fisicas.Where(a => a.Produto.Codigo.Contains(pd) || a.Produto.Descricao.Contains(pd));

        // RN-09.03: recebida = a amostra física (revisão 0) entregue no período; revisões não são entradas novas
        var entradas = await (from a in fisicas
                              join raiz in db.LabHistAmostras on a.IdAmostraRaiz ?? a.IdLabHistAmostras equals raiz.IdLabHistAmostras
                              where raiz.DtEntrega >= de && raiz.DtEntrega <= ate
                              group a by new { a.IdEntidades, a.Entidade.Nome, a.Entidade.CpfCnpj, a.IdProdutos, a.Produto.Codigo, a.Produto.Descricao } into g
                              select new ContagemAmostras(g.Key.IdEntidades, g.Key.Nome, g.Key.CpfCnpj, g.Key.IdProdutos, g.Key.Codigo, g.Key.Descricao, g.Count()))
            .ToListAsync();
        // RN-09.04: analisada = amostra física com algum parâmetro da última revisão do resultado no período, contada uma vez
        var raizesAnalisadas = db.LabResultadosParam
            .Where(p => p.LabResultadoAnalise.UltimaRevisao && p.DtResultado >= de && p.DtResultado <= ate)
            .Select(p => p.LabResultadoAnalise.LabHistAmostra.IdAmostraRaiz ?? p.LabResultadoAnalise.IdLabHistAmostras);
        var analisadas = await (from a in fisicas
                                where raizesAnalisadas.Contains(a.IdAmostraRaiz ?? a.IdLabHistAmostras)
                                group a by new { a.IdEntidades, a.Entidade.Nome, a.Entidade.CpfCnpj, a.IdProdutos, a.Produto.Codigo, a.Produto.Descricao } into g
                                select new ContagemAmostras(g.Key.IdEntidades, g.Key.Nome, g.Key.CpfCnpj, g.Key.IdProdutos, g.Key.Codigo, g.Key.Descricao, g.Count()))
            .ToListAsync();

        // junta as duas contagens pela chave (cliente, e produto na T30-03)
        var linhas = entradas.Select(e => (e, Analisada: false)).Concat(analisadas.Select(a => (e: a, Analisada: true)))
            .GroupBy(x => (x.e.IdCliente, Produto: porProduto ? x.e.IdProduto : 0))
            .Select(g =>
            {
                var k = g.First().e;
                return new
                {
                    k.IdCliente, k.Cliente, k.CpfCnpj, k.ProdutoCodigo, k.Produto,
                    Entradas = g.Where(x => !x.Analisada).Sum(x => x.e.Quantidade),
                    Analisadas = g.Where(x => x.Analisada).Sum(x => x.e.Quantidade),
                };
            })
            .OrderBy(x => x.Cliente, StringComparer.Create(PtBr, true)).ThenBy(x => x.IdCliente).ThenBy(x => x.ProdutoCodigo)
            .ToList();

        if (!porProduto)
        {
            var t = new TabelaRelatorio
            {
                Colunas = [new("Cliente", Largura: 3.5f), new("CPF/CNPJ", Largura: 1.4f), new("Entradas", true), new("Analisadas", true), new("% analisada", true)],
            };
            foreach (var l in linhas) t.Dado(l.Cliente, l.CpfCnpj, Inteiro(l.Entradas), Inteiro(l.Analisadas), Percentual(l.Analisadas, l.Entradas));
            int te = linhas.Sum(l => l.Entradas), ta = linhas.Sum(l => l.Analisadas);
            t.Total($"Total geral ({Qtde(linhas.Count, "cliente")})", null, Inteiro(te), Inteiro(ta), Percentual(ta, te));
            r.Tabelas.Add(t);
        }
        else
        {
            var t = new TabelaRelatorio
            {
                Colunas = [new("Cliente", Largura: 3f), new("CPF/CNPJ", Largura: 1.4f), new("Código", Largura: 0.9f), new("Produto", Largura: 3f), new("Entradas", true), new("Analisadas", true)],
            };
            foreach (var g in linhas.GroupBy(l => (l.IdCliente, l.Cliente)))
            {
                foreach (var l in g) t.Dado(l.Cliente, l.CpfCnpj, l.ProdutoCodigo, l.Produto, Inteiro(l.Entradas), Inteiro(l.Analisadas));
                t.Subtotal($"Subtotal — {g.Key.Cliente}", null, null, null, Inteiro(g.Sum(l => l.Entradas)), Inteiro(g.Sum(l => l.Analisadas)));
            }
            t.Total("Total geral", null, null, null, Inteiro(linhas.Sum(l => l.Entradas)), Inteiro(linhas.Sum(l => l.Analisadas)));
            r.Tabelas.Add(t);
        }
        r.Notas.Add("Entradas: amostras físicas (revisão 0) entregues no período. Analisadas: amostras físicas com resultado (última revisão) no período, "
                    + "contadas uma vez, mesmo que entregues antes. Revisões da amostra não contam como entradas novas.");
    }

    // ------------------------------------------------------------------ T30-04, 05 e 06

    /// <summary>Uma linha por parâmetro da última revisão do resultado (RN-09.05), com o que os três relatórios mostram.</summary>
    private sealed class LinhaResultado
    {
        public DateOnly DtInicial { get; init; }
        public DateOnly DtResultado { get; init; }
        public DateOnly? DtPrevista { get; init; }
        public int IdParametro { get; init; }
        public string Parametro { get; init; } = "";
        public string NrAmostra { get; init; } = "";
        public int IdCliente { get; init; }
        public string Cliente { get; init; } = "";
        public string? CpfCnpj { get; init; }
        public string Produto { get; init; } = "";
        public string? QtAmostra { get; init; }
        public string? Analista { get; init; }
    }

    private Task<List<LinhaResultado>> ResultadosAsync(RelatorioFiltroVM f, DateOnly de, DateOnly ate, FiltrosRelatorio filtros)
    {
        // o legado (T30-04) ligava o ID do parâmetro ao ID do teste; aqui o parâmetro vem do teste do resultado
        var q = from r in db.VwLabResultados.AsNoTracking()
                where r.UltimaRevisao && r.DtResultado >= de && r.DtResultado <= ate
                join t in db.LabHistAmostrasTestes on r.IdLabHistAmostrasTestes equals t.IdLabHistAmostrasTestes
                join a in db.LabHistAmostras on r.IdLabHistAmostras equals a.IdLabHistAmostras
                select new { r, t, a };
        if (filtros.HasFlag(FiltrosRelatorio.Cliente) && Textos.Limpo(f.Cliente) is { } c)
            q = q.Where(x => x.a.Entidade.Nome.Contains(c) || (x.a.Entidade.NomeFantasia != null && x.a.Entidade.NomeFantasia.Contains(c))
                             || (x.a.Entidade.CpfCnpj != null && x.a.Entidade.CpfCnpj.Contains(c)));
        if (filtros.HasFlag(FiltrosRelatorio.Parametro) && Textos.Limpo(f.Parametro) is { } p)
            q = q.Where(x => x.r.Parametro.Contains(p) || x.r.DescReduzida.Contains(p));
        if (filtros.HasFlag(FiltrosRelatorio.TipoAnalise) && Textos.Limpo(f.TipoAnalise) is { } ta)
            q = q.Where(x => x.t.LabParametroAnalise.AnaliseTipo.Descricao.Contains(ta));
        if (filtros.HasFlag(FiltrosRelatorio.Acreditado) && f.Acreditado is { } acr) q = q.Where(x => x.r.Auditado == acr);
        if (filtros.HasFlag(FiltrosRelatorio.Produto) && Textos.Limpo(f.Produto) is { } pd)
            q = q.Where(x => x.a.Produto.Codigo.Contains(pd) || x.a.Produto.Descricao.Contains(pd));
        if (filtros.HasFlag(FiltrosRelatorio.Analista) && Textos.Limpo(f.Analista) is { } an)
            q = q.Where(x => x.r.AnalistaNome != null && x.r.AnalistaNome.Contains(an));
        return q.Select(x => new LinhaResultado
            {
                DtInicial = x.r.DtInicial, DtResultado = x.r.DtResultado, DtPrevista = x.t.DtPrevista,
                IdParametro = x.r.IdLabParametrosAnalises, Parametro = x.r.Parametro, NrAmostra = x.a.NrAmostra,
                IdCliente = x.a.IdEntidades, Cliente = x.a.Entidade.Nome, CpfCnpj = x.a.Entidade.CpfCnpj, Produto = x.a.Produto.Descricao,
                QtAmostra = x.r.QtAmostraTxt, Analista = x.r.AnalistaNome,
            })
            .ToListAsync();
    }

    private async Task ParametrosAnalisadosAsync(RelatorioDados r, RelatorioFiltroVM f, DateOnly de, DateOnly ate)
    {
        var linhas = await ResultadosAsync(f, de, ate, FiltrosRelatorio.Cliente | FiltrosRelatorio.Parametro | FiltrosRelatorio.TipoAnalise | FiltrosRelatorio.Acreditado);
        var texto = StringComparer.Create(PtBr, true);
        var t = new TabelaRelatorio
        {
            Colunas = [new("Resultado", Largura: 0.9f), new("Parâmetro", Largura: 3f), new("Amostra", Largura: 1.4f), new("Cliente", Largura: 3f), new("CPF/CNPJ", Largura: 1.4f)],
        };
        void Linha(LinhaResultado l) => t.Dado(Data(l.DtResultado), l.Parametro, l.NrAmostra, l.Cliente, l.CpfCnpj);
        if (f.AgruparPorCliente)
        {
            foreach (var g in linhas.GroupBy(l => (l.IdCliente, l.Cliente)).OrderBy(g => g.Key.Cliente, texto).ThenBy(g => g.Key.IdCliente))
            {
                foreach (var l in g.OrderBy(l => l.Parametro, texto).ThenBy(l => l.DtResultado).ThenBy(l => l.NrAmostra)) Linha(l);
                t.Subtotal(null, $"Subtotal — {g.Key.Cliente}: {Qtde(g.Count(), "parâmetro analisado", "parâmetros analisados")}", null, null, null);
            }
        }
        else
        {
            foreach (var g in linhas.GroupBy(l => (l.IdParametro, l.Parametro)).OrderBy(g => g.Key.Parametro, texto))
            {
                foreach (var l in g.OrderBy(l => l.DtResultado).ThenBy(l => l.NrAmostra)) Linha(l);
                t.Subtotal(null, $"Total — {g.Key.Parametro}: {Inteiro(g.Count())}", null, null, null);
            }
        }
        t.Total(null, $"Total geral: {Qtde(linhas.Count, "parâmetro analisado", "parâmetros analisados")}", null, null, null);
        r.Tabelas.Add(t);

        var resumo = new TabelaRelatorio { Titulo = "Total por parâmetro", Colunas = [new("Parâmetro", Largura: 4f), new("Analisados", true)] };
        foreach (var g in linhas.GroupBy(l => (l.IdParametro, l.Parametro)).OrderBy(g => g.Key.Parametro, texto)) resumo.Dado(g.Key.Parametro, Inteiro(g.Count()));
        resumo.Total("Total geral", Inteiro(linhas.Count));
        if (f.AgruparPorCliente) r.Tabelas.Add(resumo);
        r.Notas.Add("Cada parâmetro da última revisão do resultado conta uma vez (o relatório do legado ligava o parâmetro ao teste errado e não é comparável).");
    }

    private async Task TempoAnalisesAsync(RelatorioDados r, RelatorioFiltroVM f, DateOnly de, DateOnly ate)
    {
        var linhas = await ResultadosAsync(f, de, ate, FiltrosRelatorio.Parametro | FiltrosRelatorio.TipoAnalise);
        // RN-09.06: dias inclusivos, como o legado (início e resultado no mesmo dia = 1)
        static int Dias(LinhaResultado l) => l.DtResultado.DayNumber - l.DtInicial.DayNumber + 1;
        static int? Atraso(LinhaResultado l) => l.DtPrevista is { } p ? Math.Max(0, l.DtResultado.DayNumber - p.DayNumber) : null;
        var t = new TabelaRelatorio
        {
            Colunas =
            [
                new("Início", Largura: 0.9f), new("Resultado", Largura: 0.9f), new("Dias", true, 0.6f), new("Prazo previsto", Largura: 1f),
                new("Atraso (dias)", true, 0.8f), new("Parâmetro", Largura: 3f), new("Amostra", Largura: 1.4f),
            ],
        };
        foreach (var l in linhas.OrderBy(l => l.DtInicial).ThenBy(l => l.DtResultado).ThenBy(l => l.NrAmostra).ThenBy(l => l.Parametro))
            t.Dado(Data(l.DtInicial), Data(l.DtResultado), Inteiro(Dias(l)), l.DtPrevista is { } p ? Data(p) : null,
                Atraso(l) is { } a ? Inteiro(a) : null, l.Parametro, l.NrAmostra);
        t.Total(null, null, linhas.Count == 0 ? null : Decimal(linhas.Average(Dias)), null, null, $"Média geral — {Qtde(linhas.Count, "análise")}", null);
        r.Tabelas.Add(t);

        var resumo = new TabelaRelatorio
        {
            Titulo = "Por parâmetro (dias)",
            Colunas = [new("Parâmetro", Largura: 4f), new("Análises", true), new("Média", true), new("Mínimo", true), new("Máximo", true), new("Em atraso", true)],
        };
        foreach (var g in linhas.GroupBy(l => (l.IdParametro, l.Parametro)).OrderBy(g => g.Key.Parametro, StringComparer.Create(PtBr, true)))
            resumo.Dado(g.Key.Parametro, Inteiro(g.Count()), Decimal(g.Average(Dias)), Inteiro(g.Min(Dias)), Inteiro(g.Max(Dias)),
                Inteiro(g.Count(l => Atraso(l) > 0)));
        if (linhas.Count > 0)
            resumo.Total("Geral", Inteiro(linhas.Count), Decimal(linhas.Average(Dias)), Inteiro(linhas.Min(Dias)), Inteiro(linhas.Max(Dias)),
                Inteiro(linhas.Count(l => Atraso(l) > 0)));
        r.Tabelas.Add(resumo);
        r.Notas.Add("Dias = da data inicial à do resultado, contando os dois dias (como o legado). Atraso = dias do resultado além do prazo previsto do teste.");
    }

    private async Task ProducaoAsync(RelatorioDados r, RelatorioFiltroVM f, DateOnly de, DateOnly ate)
    {
        var linhas = await ResultadosAsync(f, de, ate, FiltrosRelatorio.Parametro | FiltrosRelatorio.Produto | FiltrosRelatorio.Analista);
        var texto = StringComparer.Create(PtBr, true);
        var t = new TabelaRelatorio
        {
            Colunas = [new("Resultado", Largura: 0.9f), new("Parâmetro", Largura: 3f), new("Produto", Largura: 2.6f), new("Qtde. amostra", true, 1.1f), new("Analista", Largura: 2f), new("Amostra", Largura: 1.4f)],
        };
        foreach (var l in linhas.OrderBy(l => l.DtResultado).ThenBy(l => l.Parametro, texto).ThenBy(l => l.NrAmostra))
            t.Dado(Data(l.DtResultado), l.Parametro, l.Produto, l.QtAmostra, l.Analista, l.NrAmostra);
        t.Total(null, $"Total geral: {Qtde(linhas.Count, "análise")}", null, null, null, null);
        r.Tabelas.Add(t);

        var porDia = new TabelaRelatorio { Titulo = "Análises por dia", Colunas = [new("Data do resultado", Largura: 2f), new("Análises", true)] };
        foreach (var g in linhas.GroupBy(l => l.DtResultado).OrderBy(g => g.Key)) porDia.Dado(Data(g.Key), Inteiro(g.Count()));
        porDia.Total("Total", Inteiro(linhas.Count));
        r.Tabelas.Add(porDia);

        var porParametro = new TabelaRelatorio { Titulo = "Análises por parâmetro", Colunas = [new("Parâmetro", Largura: 4f), new("Análises", true)] };
        foreach (var g in linhas.GroupBy(l => (l.IdParametro, l.Parametro)).OrderBy(g => g.Key.Parametro, texto)) porParametro.Dado(g.Key.Parametro, Inteiro(g.Count()));
        porParametro.Total("Total", Inteiro(linhas.Count));
        r.Tabelas.Add(porParametro);
        r.Notas.Add("Cada parâmetro da última revisão do resultado com data do resultado no período conta como uma análise.");
    }

    // ------------------------------------------------------------------ apoio

    private static string Subtitulo(DefinicaoRelatorio d, RelatorioFiltroVM f)
    {
        var partes = new List<string> { $"{d.Periodo}: {Data(f.De!.Value)} a {Data(f.Ate!.Value)}" };
        void Se(FiltrosRelatorio filtro, string rotulo, string? valor)
        {
            if (d.Filtros.HasFlag(filtro) && Textos.Limpo(valor) is { } v) partes.Add($"{rotulo}: {v}");
        }
        Se(FiltrosRelatorio.Cliente, "Cliente", f.Cliente);
        Se(FiltrosRelatorio.Cobranca, "Cobrança", f.Cobranca);
        Se(FiltrosRelatorio.Situacao, "Situação", f.Situacao is { } s ? RelatorioFiltroVM.NomeSituacao(s) : null);
        Se(FiltrosRelatorio.Produto, "Produto", f.Produto);
        Se(FiltrosRelatorio.Parametro, "Parâmetro", f.Parametro);
        Se(FiltrosRelatorio.TipoAnalise, "Tipo de análise", f.TipoAnalise);
        Se(FiltrosRelatorio.Acreditado, "Acreditado", f.Acreditado is { } a ? (a ? "sim" : "não") : null);
        Se(FiltrosRelatorio.Analista, "Analista", f.Analista);
        if (d.Filtros.HasFlag(FiltrosRelatorio.SomenteComPedido))
            partes.Add(f.SomenteComPedido ? "Somente amostras com pedido de venda" : "Todas as amostras (com e sem pedido de venda)");
        if (d.Filtros.HasFlag(FiltrosRelatorio.AgruparPorCliente) && f.AgruparPorCliente) partes.Add("Agrupado por cliente");
        return string.Join(" · ", partes);
    }

    private static string Data(DateOnly d) => d.ToString("dd/MM/yyyy", PtBr);
    private static string Inteiro(int n) => n.ToString("N0", PtBr);
    private static string Decimal(double n) => n.ToString("N1", PtBr);
    private static string Valor(decimal n) => n.ToString("N2", PtBr);
    private static string Percentual(int parte, int total) => total == 0 ? "—" : (100.0 * parte / total).ToString("N1", PtBr) + "%";
    private static string Qtde(int n, string singular, string? plural = null) => $"{Inteiro(n)} {(n == 1 ? singular : plural ?? singular + "s")}";
}
