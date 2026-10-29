using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LabControl.Services.Relatorios;

/// <summary>Cabeçalho comum: empresa ativa e logo (T30-00).</summary>
public sealed record EmpresaRelatorio(string Nome, string? Cnpj, byte[]? Logo);

/// <summary>Relatório gerencial em PDF (QuestPDF, T30-00): A4 deitado, cabeçalho com logo, título e filtros, tabelas e rodapé com página.</summary>
public static class RelatorioPdf
{
    private const string Cinza = "#666666";
    private const string FundoTotal = "#E6E6E6";


    public static byte[] Gerar(RelatorioDados r, EmpresaRelatorio empresa, DateTime emitidoEm) => Document.Create(doc => doc.Page(page =>
    {
        page.Size(PageSizes.A4.Landscape());
        page.Margin(1.2f, Unit.Centimetre);
        page.DefaultTextStyle(t => t.FontSize(8));
        page.Header().Element(c => Cabecalho(c, r, empresa));
        page.Content().PaddingVertical(6).Column(col =>
        {
            col.Spacing(10);
            foreach (var t in r.Tabelas) col.Item().Element(c => Tabela(c, t));
            foreach (var n in r.Notas) col.Item().Text(n).FontSize(7).FontColor(Cinza);
        });
        page.Footer().Row(row =>
        {
            row.RelativeItem().Text($"Emitido em {emitidoEm:dd/MM/yyyy HH:mm} — LabControl").FontSize(7).FontColor(Cinza);
            row.RelativeItem().AlignRight().Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(7).FontColor(Cinza));
                t.Span("Página ");
                t.CurrentPageNumber();
                t.Span(" de ");
                t.TotalPages();
            });
        });
    })).WithMetadata(new DocumentMetadata { Title = r.Titulo, Author = empresa.Nome, Creator = "LabControl" }).GeneratePdf();

    private static void Cabecalho(IContainer c, RelatorioDados r, EmpresaRelatorio e) => c.Column(col =>
    {
        col.Item().Row(row =>
        {
            if (e.Logo is not null) row.ConstantItem(80).Height(40).AlignMiddle().Image(e.Logo).FitArea();
            row.RelativeItem().PaddingLeft(e.Logo is null ? 0 : 10).Column(x =>
            {
                x.Item().Text(e.Nome).FontSize(10).Bold();
                if (e.Cnpj is not null) x.Item().Text($"CNPJ {e.Cnpj}").FontColor(Cinza);
                x.Item().PaddingTop(2).Text(r.Titulo).FontSize(12).Bold();
                x.Item().Text(r.Subtitulo).FontColor(Cinza);
            });
        });
        col.Item().PaddingTop(4).LineHorizontal(1);
    });

    private static void Tabela(IContainer c, TabelaRelatorio t) => c.Column(col =>
    {
        if (t.Titulo is not null) col.Item().PaddingBottom(3).Text(t.Titulo).FontSize(9).Bold();
        col.Item().Table(tab =>
        {
            tab.ColumnsDefinition(d => { foreach (var coluna in t.Colunas) d.RelativeColumn(coluna.Largura); });
            tab.Header(h =>
            {
                foreach (var coluna in t.Colunas)
                    h.Cell().BorderBottom(1).PaddingVertical(2).PaddingHorizontal(2)
                        .Element(x => coluna.Numero ? x.AlignRight() : x).Text(coluna.Titulo).Bold();
            });
            foreach (var l in t.Linhas)
            {
                for (var i = 0; i < t.Colunas.Count; i++)
                {
                    var cel = tab.Cell().BorderBottom(0.3f).BorderColor("#DDDDDD").PaddingVertical(1.5f).PaddingHorizontal(2);
                    if (l.Tipo != TipoLinha.Dado) cel = cel.Background(l.Tipo == TipoLinha.Total ? FundoTotal : "#F5F5F5");
                    if (t.Colunas[i].Numero) cel = cel.AlignRight();
                    var texto = cel.Text(i < l.Celulas.Count ? l.Celulas[i] ?? "" : "");
                    if (l.Tipo != TipoLinha.Dado) texto.Bold();
                }
            }
        });
    });
}
