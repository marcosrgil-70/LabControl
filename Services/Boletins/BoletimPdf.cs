using LabControl.Infra;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LabControl.Services.Boletins;

/// <summary>Para que o PDF é gerado: prévia (marca d'água, sem assinaturas), emissão (definitivo) ou 2ª via de boletim migrado.</summary>
public enum ModoBoletim { Previa, Emitido, SegundaViaMigrado }

public sealed record LinhaBoletim(string Parametro, string? Resultado, string? Unidade, string? Especificacao, bool Acreditado);

public sealed record ResponsavelBoletim(string Nome, string? Cargo, string? Registro, byte[]? Assinatura);

/// <summary>Tudo o que o laudo imprime (P16 › Layout do PDF; dados do <c>qry_ImpBolet</c> do legado), já reunido pelo serviço.</summary>
public sealed class BoletimDocumento
{
    public required ModoBoletim Modo { get; init; }
    // empresa
    public required string EmpresaNome { get; init; }
    public string? EmpresaCnpj { get; init; }
    public IReadOnlyList<string> EmpresaEndereco { get; init; } = [];
    public string? EmpresaCidade { get; init; }
    public byte[]? Logo { get; init; }
    /// <summary>Nº de acreditação, só quando a amostra tem parâmetro acreditado (<c>FN_ACREDITADO ≥ 1</c>).</summary>
    public string? NrAcreditacao { get; init; }
    public string? Cabecalho { get; init; }
    public string? Rodape { get; init; }
    // boletim
    public required string CodBoletim { get; init; }
    public int Revisao { get; init; }
    public DateOnly DtBoletim { get; init; }
    // cliente, proposta
    public required string ClienteNome { get; init; }
    public string? ClienteDocumento { get; init; }
    public IReadOnlyList<string> EnderecoLaudo { get; init; } = [];
    public string? Contato { get; init; }
    public string? NrProposta { get; init; }
    public string? NrPedido { get; init; }
    // amostra
    public required string NrAmostra { get; init; }
    public required string Produto { get; init; }
    public string? Lote { get; init; }
    public string? Fabricacao { get; init; }
    public string? Validade { get; init; }
    public DateTime Recebimento { get; init; }
    public string? Coletor { get; init; }
    public DateTime? Coleta { get; init; }
    public string? ObservacaoVerificacao { get; init; }
    public DateOnly? InicioAnalise { get; init; }
    public DateOnly? FimAnalise { get; init; }
    // resultados e textos
    public IReadOnlyList<LinhaBoletim> Linhas { get; init; } = [];
    public string? MetodologiaAcreditada { get; init; }
    public string? MetodologiaNaoAcreditada { get; init; }
    public string? Observacao { get; init; }
    public string? Legenda { get; init; }
    public IReadOnlyList<ResponsavelBoletim> Responsaveis { get; init; } = [];
}

/// <summary>
/// Laudo em PDF (QuestPDF, dúvida 10). A 5.3 entrega todas as seções com os dados certos; a 5.4 ajusta o visual lado a lado
/// com boletins reais do legado (<c>frxRboletin</c>).
/// </summary>
public static class BoletimPdf
{
    private const string Cinza = "#666666";

    private static readonly System.Globalization.CultureInfo PtBr = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");

    public static byte[] Gerar(BoletimDocumento d) => Document.Create(doc => doc.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.Margin(1.5f, Unit.Centimetre);
        page.DefaultTextStyle(t => t.FontSize(9));
        page.Header().Element(c => Cabecalho(c, d));
        page.Content().PaddingVertical(8).Element(c => Corpo(c, d));
        page.Footer().Element(c => Rodape(c, d));
        if (d.Modo == ModoBoletim.Previa)
            page.Foreground().AlignCenter().AlignMiddle().Rotate(-40)
                .Text("PRÉVIA — NÃO VÁLIDO").FontSize(54).Bold().FontColor("#D9D9D9");
    })).WithMetadata(new DocumentMetadata { Title = $"Boletim {d.CodBoletim} R{d.Revisao:00}", Author = d.EmpresaNome, Creator = "LabControl" })
      .GeneratePdf();

    private static void Cabecalho(IContainer c, BoletimDocumento d) => c.Column(col =>
    {
        col.Item().Row(row =>
        {
            if (d.Logo is not null) row.ConstantItem(110).Height(55).AlignMiddle().Image(d.Logo).FitArea();
            row.RelativeItem().PaddingLeft(d.Logo is null ? 0 : 10).Column(e =>
            {
                e.Item().Text(d.EmpresaNome).FontSize(12).Bold();
                if (d.EmpresaCnpj is not null) e.Item().Text($"CNPJ {d.EmpresaCnpj}").FontColor(Cinza);
                foreach (var l in d.EmpresaEndereco) e.Item().Text(l).FontColor(Cinza);
                if (d.Cabecalho is not null) e.Item().Text(d.Cabecalho).FontColor(Cinza);
            });
            row.ConstantItem(150).AlignRight().Column(e =>
            {
                e.Item().AlignRight().Text("BOLETIM DE ANÁLISE").FontSize(11).Bold();
                e.Item().AlignRight().Text(t => { t.Span("Nº ").FontColor(Cinza); t.Span(d.CodBoletim).Bold(); });
                e.Item().AlignRight().Text($"Revisão {d.Revisao:00}");
                if (d.NrAcreditacao is not null) e.Item().AlignRight().Text($"Acreditação {d.NrAcreditacao}").FontColor(Cinza);
            });
        });
        if (d.Modo == ModoBoletim.SegundaViaMigrado)
            col.Item().PaddingTop(4).Background("#F2F2F2").Padding(3).AlignCenter()
                .Text("2ª VIA — BOLETIM MIGRADO DO SISTEMA ANTERIOR (reimpresso a partir dos dados gravados)").FontSize(8).Bold();
        col.Item().PaddingTop(6).LineHorizontal(1);
    });

    private static void Corpo(IContainer c, BoletimDocumento d) => c.Column(col =>
    {
        col.Spacing(8);
        col.Item().Element(x => Bloco(x, "Cliente", b =>
        {
            Campo(b, "Nome", d.ClienteNome);
            Campo(b, "CPF/CNPJ", d.ClienteDocumento);
            Campo(b, "Endereço", d.EnderecoLaudo.Count == 0 ? null : string.Join(" — ", d.EnderecoLaudo));
            Campo(b, "Contato", d.Contato);
            Campo(b, "Proposta", d.NrProposta);
            Campo(b, "Pedido de venda", d.NrPedido);
        }));
        col.Item().Element(x => Bloco(x, "Amostra", b =>
        {
            Campo(b, "Nº da amostra", d.NrAmostra);
            Campo(b, "Amostra", d.Produto);
            Campo(b, "Lote", d.Lote);
            Campo(b, "Fabricação", d.Fabricacao);
            Campo(b, "Validade", d.Validade);
            Campo(b, "Recebimento", d.Recebimento.ToString("dd/MM/yyyy HH:mm"));
            Campo(b, "Coletor", d.Coletor);
            Campo(b, "Coleta", d.Coleta?.ToString("dd/MM/yyyy HH:mm"));
            Campo(b, "Observação", d.ObservacaoVerificacao);
            Campo(b, "Período de análise", d.InicioAnalise is null ? null : $"{d.InicioAnalise:dd/MM/yyyy} a {d.FimAnalise:dd/MM/yyyy}");
        }));
        col.Item().Element(x => Resultados(x, d));
        if (d.Linhas.Any(l => l.Acreditado))
            col.Item().Text("(*) Parâmetro acreditado.").FontSize(8).FontColor(Cinza);
        Texto(col, "Metodologias — acreditadas", d.MetodologiaAcreditada);
        Texto(col, "Metodologias — não acreditadas", d.MetodologiaNaoAcreditada);
        Texto(col, "Observações", d.Observacao);
        Texto(col, "Legenda", d.Legenda);
        col.Item().PaddingTop(6).Text($"{(d.EmpresaCidade is null ? "" : d.EmpresaCidade + ", ")}{d.DtBoletim.ToString("dd 'de' MMMM 'de' yyyy", PtBr)}.");
        col.Item().PaddingTop(10).Element(x => Assinaturas(x, d));
    });

    private static void Bloco(IContainer c, string titulo, Action<TableDescriptor> campos) => c.Column(col =>
    {
        col.Item().Text(titulo.ToUpperInvariant()).Bold().FontSize(9);
        col.Item().Border(0.5f).BorderColor("#BBBBBB").Padding(4).Table(t =>
        {
            t.ColumnsDefinition(cd => { cd.ConstantColumn(95); cd.RelativeColumn(); cd.ConstantColumn(95); cd.RelativeColumn(); });
            campos(t);
        });
    });

    private static void Campo(TableDescriptor t, string rotulo, string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return;
        t.Cell().Text(rotulo + ":").FontColor(Cinza);
        t.Cell().PaddingRight(6).Text(valor);
    }

    private static void Resultados(IContainer c, BoletimDocumento d)
    {
        var comEspecificacao = d.Linhas.Any(l => !string.IsNullOrWhiteSpace(l.Especificacao)); // RN-08.09
        c.Column(col =>
        {
            col.Item().Text("RESULTADOS").Bold();
            col.Item().Table(t =>
            {
                t.ColumnsDefinition(cd =>
                {
                    cd.RelativeColumn(4);
                    cd.RelativeColumn(2.2f);
                    cd.RelativeColumn(1.2f);
                    if (comEspecificacao) cd.RelativeColumn(2.2f);
                });
                t.Header(h =>
                {
                    foreach (var titulo in new[] { "Parâmetro", "Resultado", "Unidade" }.Concat(comEspecificacao ? ["Especificação"] : []))
                        h.Cell().Background("#EEEEEE").BorderBottom(0.5f).Padding(3).Text(titulo).Bold();
                });
                foreach (var l in d.Linhas)
                {
                    Celula(t).Text(l.Parametro + (l.Acreditado ? " (*)" : ""));
                    Celula(t).Text(x => Expoente(x, l.Resultado));
                    Celula(t).Text(l.Unidade ?? "");
                    if (comEspecificacao) Celula(t).Text(x => Expoente(x, l.Especificacao));
                }
            });
        });
    }

    private static IContainer Celula(TableDescriptor t) => t.Cell().BorderBottom(0.25f).BorderColor("#DDDDDD").Padding(3);

    // "< 1,0e2" → "< 1,0 × 10" + "2" sobrescrito
    private static void Expoente(TextDescriptor t, string? texto)
    {
        var p = ResultadoTexto.Partes(texto);
        if (p is null) return;
        t.Span(p.Antes);
        if (p.Expoente is null) return;
        t.Span(" × 10");
        t.Span(p.Expoente).Superscript();
        t.Span(p.Depois);
    }

    private static void Texto(ColumnDescriptor col, string titulo, string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return;
        col.Item().Column(c =>
        {
            c.Item().Text(titulo.ToUpperInvariant()).Bold();
            c.Item().Text(texto.Replace("\r\n", "\n"));
        });
    }

    private static void Assinaturas(IContainer c, BoletimDocumento d) => c.Row(row =>
    {
        foreach (var r in d.Responsaveis)
            row.RelativeItem().PaddingHorizontal(10).Column(col =>
            {
                // assinatura só no emitido (RN-08.11)
                if (d.Modo == ModoBoletim.Emitido && r.Assinatura is not null) col.Item().Height(40).AlignCenter().Image(r.Assinatura).FitArea();
                else col.Item().Height(40);
                col.Item().LineHorizontal(0.5f);
                col.Item().AlignCenter().Text(r.Nome).Bold();
                if (r.Cargo is not null) col.Item().AlignCenter().Text(r.Cargo);
                if (r.Registro is not null) col.Item().AlignCenter().Text(r.Registro).FontColor(Cinza);
            });
    });

    private static void Rodape(IContainer c, BoletimDocumento d) => c.Column(col =>
    {
        col.Item().LineHorizontal(0.5f);
        if (d.Rodape is not null) col.Item().Text(d.Rodape).FontSize(7).FontColor(Cinza);
        col.Item().Row(row =>
        {
            row.RelativeItem();
            row.ConstantItem(160).AlignRight().Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(7).FontColor(Cinza));
                t.Span($"{d.CodBoletim} R{d.Revisao:00} — página ");
                t.CurrentPageNumber();
                t.Span(" de ");
                t.TotalPages();
            });
        });
    });
}
