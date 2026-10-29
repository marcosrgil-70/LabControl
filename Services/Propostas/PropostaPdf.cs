using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LabControl.Services.Propostas;

public sealed record LinhaAnaliseProposta(string Parametro, string TipoAnalise, string Metodo, string Prazo, string Idioma, string Documento,
    int Amostras, decimal Unitario, decimal Desconto, decimal Total);

public sealed record ProdutoProposta(int Item, string Produto, int Amostras, IReadOnlyList<LinhaAnaliseProposta> Analises);

/// <summary>Dados do pedido de venda impresso sobre a revisão da proposta (<c>frx_PedVenda</c> do legado, 6.2).</summary>
public sealed record PedidoImpresso(string NrPedido, DateOnly DtPedido, DateOnly DtAutorizacao, string? AutorizadoPor,
    string? NrPedidoCliente, bool Cancelado);

/// <summary>Tudo o que a proposta impressa mostra (<c>Layout_ImpProposta.fr3</c> do legado), já reunido pelo serviço.</summary>
public sealed class PropostaDocumento
{
    // empresa
    public required string EmpresaNome { get; init; }
    public string? EmpresaCnpj { get; init; }
    public IReadOnlyList<string> EmpresaEndereco { get; init; } = [];
    public string? EmpresaFone { get; init; }
    public byte[]? Logo { get; init; }
    public string? Cabecalho { get; init; }
    public string? Rodape { get; init; }
    // proposta
    public required string NrProposta { get; init; }
    public bool RevisaoSubstituida { get; init; }
    public DateOnly DtSolicitacao { get; init; }
    public DateOnly? DtValidade { get; init; }
    public string? Comercial { get; init; }
    // cliente
    public required string ClienteNome { get; init; }
    public string? ClienteDocumento { get; init; }
    public string? Contato { get; init; }
    public string? Departamento { get; init; }
    public string? ContatoEmail { get; init; }
    public string? ClienteFone { get; init; }
    // laudo e nota fiscal
    public IReadOnlyList<string> EnderecoLaudo { get; init; } = [];
    public string? ContatoResultado { get; init; }
    public string? CobrancaNome { get; init; }
    public string? CobrancaDocumento { get; init; }
    public IReadOnlyList<string> EnderecoCobranca { get; init; } = [];
    public string? ContatoNf { get; init; }
    // valores
    public IReadOnlyList<ProdutoProposta> Produtos { get; init; } = [];
    public decimal TotalAnalises { get; init; }
    public decimal DescontoGeral { get; init; }
    public decimal ValorTotal { get; init; }
    public string? CondicaoPagamento { get; init; }
    public string? Moeda { get; init; }
    // textos (HTML simples dos editores)
    public string? ItensSolicitados { get; init; }
    public string? ItensAdquiridos { get; init; }
    public string? Observacao { get; init; }
    public string? Contrato { get; init; }
    // pedido de venda (só no PDF do pedido)
    public PedidoImpresso? Pedido { get; init; }
}

/// <summary>
/// Proposta técnica e comercial em PDF (QuestPDF, 6.1), com a página "Autorização de Serviço (AS)" do legado (decisão
/// do usuário). Layout fino a validar com os modelos reais (dúvida 5), como o boletim.
/// </summary>
public static partial class PropostaPdf
{
    private const string Cinza = "#666666";
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");


    /// <summary>
    /// Pedido de venda (6.2): o <c>frx_PedVenda</c> do legado é a proposta sem o contrato e sem a Autorização de Serviço,
    /// com o nº do pedido e a data de autorização no cabeçalho. Pedido cancelado sai marcado.
    /// </summary>
    public static byte[] GerarPedido(PropostaDocumento d)
    {
        var p = d.Pedido ?? throw new ArgumentException("Documento sem os dados do pedido.", nameof(d));
        return Document.Create(doc => doc.Page(page =>
            {
                Pagina(page, d);
                page.Content().PaddingVertical(8).Element(c => Proposta(c, d));
            }))
            .WithMetadata(new DocumentMetadata { Title = $"Pedido de venda {p.NrPedido}", Author = d.EmpresaNome, Creator = "LabControl" })
            .GeneratePdf();
    }

    public static byte[] Gerar(PropostaDocumento d) => Document.Create(doc =>
    {
        doc.Page(page =>
        {
            Pagina(page, d);
            page.Content().PaddingVertical(8).Element(c => Proposta(c, d));
        });
        doc.Page(page =>
        {
            Pagina(page, d);
            page.Content().PaddingVertical(8).Element(c => Autorizacao(c, d));
        });
    }).WithMetadata(new DocumentMetadata { Title = $"Proposta {d.NrProposta}", Author = d.EmpresaNome, Creator = "LabControl" })
      .GeneratePdf();

    private static void Pagina(PageDescriptor page, PropostaDocumento d)
    {
        page.Size(PageSizes.A4);
        page.Margin(1.5f, Unit.Centimetre);
        page.DefaultTextStyle(t => t.FontSize(8.5f));
        page.Header().Element(c => Cabecalho(c, d));
        page.Footer().Element(c => Rodape(c, d));
        // no fundo da página: a marca não cobre o texto
        if (d.Pedido is { Cancelado: true })
            page.Background().AlignCenter().AlignMiddle().Rotate(-40).Text("PEDIDO CANCELADO").FontSize(48).Bold().FontColor("#F2C4C4");
        else if (d.RevisaoSubstituida && d.Pedido is null)
            page.Background().AlignCenter().AlignMiddle().Rotate(-40).Text("REVISÃO SUBSTITUÍDA").FontSize(48).Bold().FontColor("#D9D9D9");
    }

    private static void Cabecalho(IContainer c, PropostaDocumento d) => c.Column(col =>
    {
        col.Item().Row(row =>
        {
            if (d.Logo is not null) row.ConstantItem(100).Height(50).AlignMiddle().Image(d.Logo).FitArea();
            row.RelativeItem().PaddingLeft(d.Logo is null ? 0 : 10).Column(e =>
            {
                e.Item().Text(d.EmpresaNome).FontSize(11).Bold();
                if (d.EmpresaCnpj is not null) e.Item().Text($"CNPJ {d.EmpresaCnpj}").FontColor(Cinza);
                foreach (var l in d.EmpresaEndereco) e.Item().Text(l).FontColor(Cinza);
                if (d.EmpresaFone is not null) e.Item().Text($"Fone {d.EmpresaFone}").FontColor(Cinza);
                if (d.Cabecalho is not null) e.Item().Text(d.Cabecalho).FontColor(Cinza);
            });
            row.ConstantItem(170).AlignRight().Column(e =>
            {
                if (d.Pedido is { } p)
                {
                    e.Item().AlignRight().Text(t => { t.Span("P.V. nº ").FontColor(Cinza); t.Span(p.NrPedido).Bold().FontSize(10); });
                    e.Item().AlignRight().Text($"Ref. proposta nº {d.NrProposta}");
                    e.Item().AlignRight().Text($"Autorizado em: {p.DtAutorizacao:dd/MM/yyyy}");
                    return;
                }
                e.Item().AlignRight().Text(t => { t.Span("Proposta nº ").FontColor(Cinza); t.Span(d.NrProposta).Bold().FontSize(10); });
                e.Item().AlignRight().Text($"Solicitação: {d.DtSolicitacao:dd/MM/yyyy}");
                if (d.DtValidade is { } v) e.Item().AlignRight().Text($"Validade: {v:dd/MM/yyyy}");
            });
        });
        col.Item().PaddingTop(4).LineHorizontal(1);
    });

    private static void Proposta(IContainer c, PropostaDocumento d) => c.Column(col =>
    {
        col.Spacing(7);
        col.Item().AlignCenter().Text(d.Pedido is null ? "Proposta Técnica e Comercial para Execução de Serviços Analíticos" : "Pedido de Venda")
            .FontSize(11).Bold();
        col.Item().Element(x => Bloco(x, "Dados comerciais", t =>
        {
            Campo(t, "Comercial", d.Comercial);
            Campo(t, "Cliente", d.ClienteNome);
            Campo(t, "CNPJ/CPF", d.ClienteDocumento);
            Campo(t, "Contato", d.Contato);
            Campo(t, "Departamento", d.Departamento);
            Campo(t, "E-mail", d.ContatoEmail);
            Campo(t, "Fone", d.ClienteFone);
            if (d.Pedido is { } p)
            {
                Campo(t, "Solicitação", d.DtSolicitacao.ToString("dd/MM/yyyy"));
                Campo(t, "Validade", d.DtValidade?.ToString("dd/MM/yyyy"));
                Campo(t, "Autorizado por", p.AutorizadoPor);
                Campo(t, "Pedido do cliente", p.NrPedidoCliente);
            }
        }));
        col.Item().Row(r =>
        {
            r.RelativeItem().PaddingRight(4).Element(x => Bloco(x, "Dados para emissão do(s) laudo(s)", t =>
            {
                Campo(t, "Endereço", d.EnderecoLaudo.Count == 0 ? null : string.Join(" — ", d.EnderecoLaudo));
                Campo(t, "Contato", d.ContatoResultado);
            }, duasColunas: false));
            r.RelativeItem().PaddingLeft(4).Element(x => Bloco(x, "Dados para emissão da nota fiscal", t =>
            {
                Campo(t, "Empresa", d.CobrancaNome);
                Campo(t, "CNPJ/CPF", d.CobrancaDocumento);
                Campo(t, "Endereço", d.EnderecoCobranca.Count == 0 ? null : string.Join(" — ", d.EnderecoCobranca));
                Campo(t, "Contato", d.ContatoNf);
            }, duasColunas: false));
        });

        col.Item().Text("RELAÇÃO DAS ANÁLISES").Bold();
        foreach (var p in d.Produtos) col.Item().Element(x => Produto(x, p));
        col.Item().AlignRight().Width(230).Table(t =>
        {
            t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.ConstantColumn(90); });
            void Linha(string rotulo, decimal valor, bool destaque = false)
            {
                t.Cell().PaddingVertical(1).Text(rotulo).FontColor(destaque ? Colors.Black : Cinza);
                var v = t.Cell().PaddingVertical(1).AlignRight().Text(Moeda(valor, d.Moeda));
                if (destaque) v.Bold().FontSize(10);
            }
            Linha("Total das análises", d.TotalAnalises);
            if (d.DescontoGeral > 0) Linha("Desconto", d.DescontoGeral);
            Linha("Valor total", d.ValorTotal, destaque: true);
        });
        if (d.CondicaoPagamento is not null) col.Item().Text(t => { t.Span("Condição de pagamento: ").FontColor(Cinza); t.Span(d.CondicaoPagamento); });

        Texto(col, "Itens solicitados", d.ItensSolicitados);
        Texto(col, "Itens adquiridos", d.ItensAdquiridos);
        Texto(col, "Observação", d.Observacao);
        if (d.Pedido is null) Texto(col, "Contrato", d.Contrato);
    });

    private static void Produto(IContainer c, ProdutoProposta p) => c.Column(col =>
    {
        col.Item().Background("#F2F2F2").Padding(3).Text(t =>
        {
            t.Span($"Item {p.Item} — ").FontColor(Cinza);
            t.Span(p.Produto).Bold();
            t.Span($"   ({p.Amostras} amostra{(p.Amostras == 1 ? "" : "s")})").FontColor(Cinza);
        });
        var comDesconto = p.Analises.Any(a => a.Desconto != 0); // coluna só quando há desconto no produto
        col.Item().Table(t =>
        {
            t.ColumnsDefinition(cd =>
            {
                cd.RelativeColumn(3f); cd.RelativeColumn(2f); cd.RelativeColumn(1.8f); cd.RelativeColumn(1.1f); cd.RelativeColumn(1.3f);
                cd.RelativeColumn(1.3f); cd.ConstantColumn(40); cd.ConstantColumn(52);
                if (comDesconto) cd.ConstantColumn(45);
                cd.ConstantColumn(55);
            });
            t.Header(h =>
            {
                foreach (var (titulo, direita) in new[] { ("Parâmetro", false), ("Tipo análise", false), ("Método", false), ("Prazo", false), ("Idioma", false),
                             ("Documento", false), ("Amostras", true), ("Vr. unitário", true), ("Desconto", true), ("Vr. total", true) }
                             .Where(x => comDesconto || x.Item1 != "Desconto"))
                {
                    var cel = h.Cell().BorderBottom(0.5f).Padding(2);
                    (direita ? cel.AlignRight() : cel).Text(titulo).Bold().FontSize(7.5f);
                }
            });
            foreach (var a in p.Analises)
            {
                Cel(t).Text(a.Parametro);
                Cel(t).Text(a.TipoAnalise).FontSize(7.5f);
                Cel(t).Text(a.Metodo).FontSize(7.5f);
                Cel(t).Text(a.Prazo).FontSize(7.5f);
                Cel(t).Text(a.Idioma).FontSize(7.5f);
                Cel(t).Text(a.Documento).FontSize(7.5f);
                Cel(t).AlignRight().Text(a.Amostras.ToString(PtBr));
                Cel(t).AlignRight().Text(a.Unitario.ToString("N2", PtBr));
                if (comDesconto) Cel(t).AlignRight().Text(a.Desconto == 0 ? "" : a.Desconto.ToString("N2", PtBr));
                Cel(t).AlignRight().Text(a.Total.ToString("N2", PtBr));
            }
        });
    });

    private static IContainer Cel(TableDescriptor t) => t.Cell().BorderBottom(0.25f).BorderColor("#DDDDDD").Padding(2);

    private static string Moeda(decimal valor, string? moeda) => $"{(moeda is null ? "R$ " : "")}{valor.ToString("N2", PtBr)}{(moeda is null ? "" : " " + moeda)}";

    private static void Autorizacao(IContainer c, PropostaDocumento d) => c.Column(col =>
    {
        col.Spacing(10);
        col.Item().AlignCenter().Text("Autorização de Serviço \"AS\"").FontSize(13).Bold();
        col.Item().Element(x => Bloco(x, "Proposta", t =>
        {
            Campo(t, "Proposta nº", d.NrProposta);
            Campo(t, "Valor total", Moeda(d.ValorTotal, d.Moeda));
            Campo(t, "Cliente", d.ClienteNome);
            Campo(t, "CNPJ/CPF", d.ClienteDocumento);
            Campo(t, "Contato", d.Contato);
            Campo(t, "Fone", d.ClienteFone);
        }));
        // campos para o cliente preencher, como no legado (sem texto de autorização inventado)
        col.Item().PaddingTop(6).Text("Enviar correspondências aos cuidados de (nome / cargo):");
        col.Item().PaddingTop(12).LineHorizontal(0.5f);
        col.Item().Element(x => Bloco(x, "Enviar amostras para", t =>
        {
            Campo(t, "Laboratório", d.EmpresaNome);
            Campo(t, "Endereço", d.EmpresaEndereco.Count == 0 ? null : string.Join(" — ", d.EmpresaEndereco));
            Campo(t, "Fone", d.EmpresaFone);
        }, duasColunas: false));
        col.Item().PaddingTop(30).Row(r =>
        {
            r.RelativeItem().PaddingRight(20).Column(x =>
            {
                x.Item().LineHorizontal(0.5f);
                x.Item().AlignCenter().Text("Assinatura / Carimbo");
            });
            r.ConstantItem(160).Column(x =>
            {
                x.Item().LineHorizontal(0.5f);
                x.Item().AlignCenter().Text("Data:      /      /");
            });
        });
        col.Item().PaddingTop(20).Border(1).Padding(8).AlignCenter()
            .Text("ANEXAR NO RECIPIENTE DA AMOSTRA DE FORMA BEM VISÍVEL").Bold().FontSize(11);
    });

    private static void Bloco(IContainer c, string titulo, Action<TableDescriptor> campos, bool duasColunas = true) => c.Column(col =>
    {
        col.Item().Text(titulo.ToUpperInvariant()).Bold();
        col.Item().Border(0.5f).BorderColor("#BBBBBB").Padding(4).Table(t =>
        {
            t.ColumnsDefinition(cd =>
            {
                cd.ConstantColumn(70); cd.RelativeColumn();
                if (duasColunas) { cd.ConstantColumn(70); cd.RelativeColumn(); }
            });
            campos(t);
        });
    });

    private static void Campo(TableDescriptor t, string rotulo, string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return;
        t.Cell().Text(rotulo + ":").FontColor(Cinza);
        t.Cell().PaddingRight(6).Text(valor);
    }

    // ------------------------------------------------------------------ HTML simples dos editores → texto do PDF

    private static void Texto(ColumnDescriptor col, string titulo, string? html)
    {
        var paragrafos = Paragrafos(html);
        if (paragrafos.Count == 0) return;
        col.Item().Column(c =>
        {
            c.Spacing(2);
            c.Item().Text(titulo.ToUpperInvariant()).Bold();
            foreach (var p in paragrafos)
                c.Item().Text(t =>
                {
                    if (p.Marcador is { } marcador) t.Span(marcador + "  ");
                    foreach (var s in p.Trechos)
                    {
                        var span = t.Span(s.Texto);
                        if (s.Negrito) span.Bold();
                        if (s.Italico) span.Italic();
                        if (s.Sublinhado) span.Underline();
                    }
                });
        });
    }

    private sealed record Trecho(string Texto, bool Negrito, bool Italico, bool Sublinhado);
    private sealed record Paragrafo(string? Marcador, List<Trecho> Trechos);

    /// <summary>
    /// O HTML já limpo pelo <c>HtmlSimples</c> (p, br, div, strong/b, em/i, u, ul/ol/li) vira parágrafos com trechos
    /// formatados (o teste confere por <see cref="TextoSimples"/>). Item de &lt;ul&gt; leva "•"; de &lt;ol&gt;, o número
    /// ("2." e "2.1." na lista dentro de lista — cláusulas numeradas do contrato importado do Word, 6.3).
    /// </summary>
    public static List<(string? Marcador, string Texto)> TextoSimples(string? html) =>
        Paragrafos(html).Select(p => (p.Marcador, string.Concat(p.Trechos.Select(t => t.Texto)))).ToList();

    private static List<Paragrafo> Paragrafos(string? html)
    {
        var lista = new List<Paragrafo>();
        if (string.IsNullOrWhiteSpace(html)) return lista;
        int negrito = 0, italico = 0, sublinhado = 0;
        var listas = new List<(bool Numerada, int Contador)>(); // listas abertas, da mais externa à mais interna
        var atual = new Paragrafo(null, []);
        void Fechar()
        {
            if (atual.Trechos.Any(t => !string.IsNullOrWhiteSpace(t.Texto))) lista.Add(atual);
            atual = new Paragrafo(null, []);
        }
        string Marcador()
        {
            if (listas.Count == 0 || !listas[^1].Numerada) return "•";
            listas[^1] = (true, listas[^1].Contador + 1);
            // numeração das listas numeradas abertas: 2. / 2.1.
            return string.Concat(listas.Where(l => l.Numerada).Select(l => l.Contador + "."));
        }
        foreach (Match m in Token().Matches(html))
        {
            if (m.Groups["texto"].Success)
            {
                var texto = WebUtility.HtmlDecode(m.Groups["texto"].Value).Replace(' ', ' ');
                if (texto.Length > 0) atual.Trechos.Add(new Trecho(texto, negrito > 0, italico > 0, sublinhado > 0));
                continue;
            }
            var fecha = m.Groups["fecha"].Success;
            switch (m.Groups["nome"].Value.ToLowerInvariant())
            {
                case "strong" or "b": negrito += fecha ? -1 : 1; break;
                case "em" or "i": italico += fecha ? -1 : 1; break;
                case "u": sublinhado += fecha ? -1 : 1; break;
                case "br": Fechar(); break;
                case "li":
                    Fechar();
                    if (!fecha) atual = new Paragrafo(Marcador(), []);
                    break;
                case "ul" or "ol":
                    Fechar();
                    if (!fecha) listas.Add((m.Groups["nome"].Value.Equals("ol", StringComparison.OrdinalIgnoreCase), 0));
                    else if (listas.Count > 0) listas.RemoveAt(listas.Count - 1);
                    break;
                case "p" or "div": Fechar(); break;
            }
            (negrito, italico, sublinhado) = (Math.Max(0, negrito), Math.Max(0, italico), Math.Max(0, sublinhado));
        }
        Fechar();
        return lista;
    }

    [GeneratedRegex(@"<(?<fecha>/)?(?<nome>[a-zA-Z]+)[^>]*>|(?<texto>[^<]+)")]
    private static partial Regex Token();

    private static void Rodape(IContainer c, PropostaDocumento d) => c.Column(col =>
    {
        col.Item().LineHorizontal(0.5f);
        if (d.Rodape is not null) col.Item().Text(d.Rodape).FontSize(7).FontColor(Cinza);
        col.Item().AlignRight().Text(t =>
        {
            t.DefaultTextStyle(s => s.FontSize(7).FontColor(Cinza));
            t.Span(d.Pedido is { } p ? $"Pedido de venda {p.NrPedido} — página " : $"Proposta {d.NrProposta} — página ");
            t.CurrentPageNumber();
            t.Span(" de ");
            t.TotalPages();
        });
    });
}
