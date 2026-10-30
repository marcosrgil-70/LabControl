using LabControl.Infra;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LabControl.Services.Boletins;

/// <summary>Para que o PDF é gerado: prévia (marca d'água, sem assinaturas), emissão (definitivo) ou 2ª via de boletim migrado.</summary>
public enum ModoBoletim { Previa, Emitido, SegundaViaMigrado }

/// <summary>Uma linha da tabela; <paramref name="Resultado"/> e <paramref name="Especificacao"/> já com a unidade ("7,6e1 UFC/mL").</summary>
public sealed record LinhaBoletim(string Parametro, string? Resultado, string? Especificacao, bool Acreditado);

/// <summary>Uma parte do boletim (página nova): os parâmetros, se leva o selo, o sufixo do número ("A"/"B") e os métodos.</summary>
public sealed record ParteBoletim(IReadOnlyList<LinhaBoletim> Linhas, bool Acreditada, string? Sufixo, string? Metodos);

public sealed record ResponsavelBoletim(string Nome, string? Cargo, string? Registro, byte[]? Assinatura);

/// <summary>Tudo o que o laudo imprime (P16 › Layout do PDF; dados do <c>qry_ImpBolet</c> do legado), já reunido pelo serviço.</summary>
public sealed class BoletimDocumento
{
    public required ModoBoletim Modo { get; init; }
    // empresa
    public required string EmpresaNome { get; init; }
    /// <summary>Cidade da empresa, impressa antes da data ("SANTA BÁRBARA D'OESTE, 29 de outubro de 2026.").</summary>
    public string? EmpresaCidade { get; init; }
    public byte[]? Logo { get; init; }
    /// <summary>Selo da acreditação (D-28): só nas páginas dos parâmetros acreditados.</summary>
    public byte[]? SeloAcreditacao { get; init; }
    /// <summary>Nº de acreditação: impresso no lugar do selo quando a empresa não cadastrou a imagem.</summary>
    public string? NrAcreditacao { get; init; }
    public string? Cabecalho { get; init; }
    public string? Rodape { get; init; }
    // boletim
    public required string CodBoletim { get; init; }
    public int Revisao { get; init; }
    public DateOnly DtBoletim { get; init; }
    // cliente, proposta
    public required string ClienteNome { get; init; }
    public IReadOnlyList<string> EnderecoLaudo { get; init; } = [];
    /// <summary>Nome do contato do resultado ("Solicitante").</summary>
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
/// Laudo em PDF (QuestPDF, dúvida 10) no layout do <c>frxRboletin</c> do legado, conferido com os boletins reais do
/// laboratório (5.4). Como no legado, os parâmetros acreditados saem primeiro: boletim misto vira duas partes em páginas
/// separadas — "A" (acreditados, com o selo) e "B" (não acreditados, sem o selo), cada uma com os seus métodos.
/// </summary>
public static class BoletimPdf
{
    private const string CinzaFaixa = "#A6A6A6";
    private const string CinzaCabecalhoTabela = "#D9D9D9";

    private static readonly System.Globalization.CultureInfo PtBr = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");

    public static byte[] Gerar(BoletimDocumento d) => Document.Create(doc =>
    {
        foreach (var parte in Partes(d))
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginLeft(10, Unit.Millimetre);
                page.MarginRight(8, Unit.Millimetre);
                page.MarginTop(10, Unit.Millimetre);
                page.MarginBottom(5, Unit.Millimetre);
                page.DefaultTextStyle(t => (Pdf.FonteBoletim is null ? t : t.FontFamily(Pdf.FonteBoletim)).FontSize(9));
                page.Header().Element(c => Cabecalho(c, d, parte));
                page.Content().Element(c => Corpo(c, d, parte));
                page.Footer().Element(c => Rodape(c, d));
                if (d.Modo == ModoBoletim.Previa)
                    page.Foreground().AlignCenter().AlignMiddle().Rotate(-40)
                        .Text("PRÉVIA — NÃO VÁLIDO").FontSize(54).Bold().FontColor("#D9D9D9");
            });
    }).WithMetadata(new DocumentMetadata { Title = $"Boletim {d.CodBoletim} R{d.Revisao:00}", Author = d.EmpresaNome, Creator = "LabControl" })
      .GeneratePdf();

    // legado: grupo por ACREDITADO com página nova; misto = "BA … A" com o selo e "BA … B" sem o selo
    public static IReadOnlyList<ParteBoletim> Partes(BoletimDocumento d)
    {
        var acreditadas = d.Linhas.Where(l => l.Acreditado).ToList();
        var outras = d.Linhas.Where(l => !l.Acreditado).ToList();
        if (acreditadas.Count > 0 && outras.Count > 0)
            return [new(acreditadas, true, "A", d.MetodologiaAcreditada), new(outras, false, "B", d.MetodologiaNaoAcreditada)];
        // parte única: os dois textos (acreditado primeiro), para não perder o que foi digitado no outro campo
        var metodos = string.Join("\n", new[] { d.MetodologiaAcreditada, d.MetodologiaNaoAcreditada }.Where(t => !string.IsNullOrWhiteSpace(t)));
        return [new(d.Linhas, acreditadas.Count > 0, null, metodos)];
    }

    private static void Cabecalho(IContainer c, BoletimDocumento d, ParteBoletim parte) => c.Column(col =>
    {
        col.Item().Height(100).Row(row =>
        {
            row.ConstantItem(120).AlignMiddle().Element(x =>
            {
                if (d.Logo is not null) x.Height(90).AlignCenter().AlignMiddle().Image(d.Logo).FitArea();
            });
            row.RelativeItem().PaddingTop(48).Column(t =>
            {
                t.Item().AlignCenter().Text("BOLETIM DE ANÁLISE").FontSize(14);
                var numero = $"BA {d.CodBoletim}{(d.Revisao > 0 ? $" R{d.Revisao}" : "")}{(parte.Sufixo is null ? "" : " " + parte.Sufixo)}";
                t.Item().PaddingTop(2).AlignCenter().Text(numero);
                if (d.Cabecalho is not null) t.Item().PaddingTop(2).AlignCenter().Text(d.Cabecalho.Replace("\r\n", "\n")).FontSize(7.5f);
            });
            row.ConstantItem(120).PaddingTop(8).Element(x =>
            {
                if (!parte.Acreditada) return;
                if (d.SeloAcreditacao is not null) x.Height(85).AlignCenter().Image(d.SeloAcreditacao).FitArea();
                else if (d.NrAcreditacao is not null) x.AlignCenter().AlignMiddle().Text($"Acreditação {d.NrAcreditacao}").Bold();
            });
        });
        if (d.Modo == ModoBoletim.SegundaViaMigrado)
            col.Item().PaddingTop(4).Background("#F2F2F2").Padding(3).AlignCenter()
                .Text("2ª VIA — BOLETIM MIGRADO DO SISTEMA ANTERIOR (reimpresso a partir dos dados gravados)").FontSize(8).Bold();
        col.Item().Height(14);
    });

    private static void Corpo(IContainer c, BoletimDocumento d, ParteBoletim parte) => c.Column(col =>
    {
        col.Item().Element(x => Faixa(x, "Dados da Empresa"));
        col.Item().PaddingLeft(4).PaddingVertical(2).Table(t =>
        {
            t.ColumnsDefinition(cd => { cd.ConstantColumn(52); cd.RelativeColumn(); });
            Par(t, "Empresa:", d.ClienteNome);
            for (var i = 0; i < d.EnderecoLaudo.Count; i++) Par(t, i == 0 ? "Endereço:" : "", d.EnderecoLaudo[i], recuo: i > 0);
            Par(t, "Solicitante:", d.Contato);
        });

        col.Item().Element(x => Faixa(x, "Dados da Amostra"));
        col.Item().PaddingLeft(4).PaddingVertical(2).Table(t =>
        {
            t.ColumnsDefinition(cd => { cd.ConstantColumn(78); cd.RelativeColumn(); cd.RelativeColumn(1.2f); });
            Inteira(t, "Nome da Amostra:", d.Produto);
            Dupla(t, "Nr. Lote:", Informado(d.Lote, "Não Informado"), "Data Recebimento:", d.Recebimento.ToString("dd/MM/yyyy"));
            Dupla(t, "Código Amostra:", d.NrAmostra, "Data Início Análise:", d.InicioAnalise?.ToString("dd/MM/yyyy"));
            Dupla(t, "Proposta:", d.NrProposta, "Data Término Análise:", d.FimAnalise?.ToString("dd/MM/yyyy"));
            Dupla(t, "Data Fabricação:", Informado(d.Fabricacao, "Não Informada"), d.NrPedido is null ? "" : "Pedido de Venda:", d.NrPedido);
            Dupla(t, "Data Validade:", Informado(d.Validade, "Não Informada"), "", null);
            var hora = d.Coleta is { TimeOfDay.Ticks: > 0 } h ? h.ToString("HH:mm") : "Não Informada";
            Dupla(t, "Coletor:", Informado(d.Coletor, "Não Informado"),
                "Data Coleta:", $"{d.Coleta?.ToString("dd/MM/yyyy") ?? "Não Informada"}      Hora Coleta: {hora}");
            if (!string.IsNullOrWhiteSpace(d.ObservacaoVerificacao)) Inteira(t, "Observação:", d.ObservacaoVerificacao);
        });

        col.Item().PaddingTop(10).Element(x => Faixa(x, "Análises"));
        col.Item().PaddingTop(4).PaddingHorizontal(4).Element(x => Tabela(x, parte.Linhas));
        if (!string.IsNullOrWhiteSpace(d.Legenda) && !d.Legenda.Trim().Equals("NA", StringComparison.OrdinalIgnoreCase)) // legado: "NA" esconde
            col.Item().PaddingTop(3).PaddingLeft(6).Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(7));
                t.Span("Legenda:  ");
                t.Span(d.Legenda.Replace("\r\n", "\n").Trim());
            });

        col.Item().PaddingTop(3).LineHorizontal(0.75f);
        col.Item().PaddingVertical(4).PaddingLeft(6).Row(r =>
        {
            r.ConstantItem(70).Text("Métodos:");
            // textos do legado às vezes começam com uma linha em branco: sem ela, o texto fica ao lado do rótulo
            r.RelativeItem().Text((parte.Metodos ?? "").Replace("\r\n", "\n").Trim()).FontSize(7.5f);
        });
        col.Item().LineHorizontal(0.75f);
        col.Item().PaddingVertical(4).Column(o =>
        {
            o.Item().Text("Observações:").FontSize(8);
            if (!string.IsNullOrWhiteSpace(d.Observacao)) o.Item().Text(d.Observacao.Replace("\r\n", "\n").Trim()).FontSize(7.5f);
        });
        col.Item().LineHorizontal(0.75f);

        // data e responsáveis não se separam
        col.Item().ShowEntire().Column(a =>
        {
            a.Item().PaddingTop(4).PaddingLeft(8)
                .Text($"{(d.EmpresaCidade is null ? "" : d.EmpresaCidade.ToUpper(PtBr) + ", ")}{d.DtBoletim.ToString("dd 'de' MMMM 'de' yyyy", PtBr)}.");
            a.Item().PaddingTop(6).PaddingLeft(8).Element(x => Assinaturas(x, d));
            a.Item().LineHorizontal(0.75f);
        });
    });

    // faixa cinza com o título centralizado (o legado usa um degradê do branco ao cinza)
    private static void Faixa(IContainer c, string titulo) => c.Height(12).Layers(l =>
    {
        l.PrimaryLayer().Row(r =>
        {
            const int passos = 48;
            for (var i = 0; i < passos; i++)
            {
                var v = (int)Math.Round(255 - (255 - 0xA6) * (i + 1) / (double)passos);
                r.RelativeItem().Background($"#{v:X2}{v:X2}{v:X2}");
            }
            r.RelativeItem(90).Background(CinzaFaixa);
        });
        l.Layer().AlignCenter().AlignMiddle().Text(titulo).Bold();
    });

    private static void Par(TableDescriptor t, string rotulo, string? valor, bool recuo = false)
    {
        if (string.IsNullOrWhiteSpace(valor)) return;
        t.Cell().PaddingVertical(1).Text(rotulo);
        t.Cell().PaddingVertical(1).PaddingLeft(recuo ? 24 : 0).Text(valor);
    }

    private static void Inteira(TableDescriptor t, string rotulo, string? valor)
    {
        t.Cell().PaddingVertical(1).Text(rotulo);
        t.Cell().ColumnSpan(2).PaddingVertical(1).Text(valor ?? "");
    }

    private static void Dupla(TableDescriptor t, string rotulo1, string? valor1, string rotulo2, string? valor2)
    {
        t.Cell().PaddingVertical(1).Text(rotulo1);
        t.Cell().PaddingVertical(1).Text(valor1 ?? "");
        // à direita o valor vem logo depois do rótulo, como no legado
        t.Cell().PaddingVertical(1).Text(rotulo2.Length == 0 ? "" : $"{rotulo2}  {valor2}");
    }

    private static string Informado(string? valor, string vazio) => string.IsNullOrWhiteSpace(valor) ? vazio : valor;

    private static void Tabela(IContainer c, IReadOnlyList<LinhaBoletim> linhas)
    {
        var comEspecificacao = linhas.Any(l => !string.IsNullOrWhiteSpace(l.Especificacao)); // RN-08.09
        c.Border(0.75f).Table(t =>
        {
            t.ColumnsDefinition(cd =>
            {
                if (comEspecificacao) { cd.RelativeColumn(3.9f); cd.RelativeColumn(3); cd.RelativeColumn(3.1f); }
                else { cd.RelativeColumn(7); cd.RelativeColumn(3); }
            });
            t.Header(h =>
            {
                // legado: "Especificação *" — o asterisco remete à legenda/observação digitada pelo laboratório
                foreach (var titulo in new[] { "Parâmetros", "Resultado" }.Concat(comEspecificacao ? ["Especificação *"] : []))
                    h.Cell().Border(0.75f).Background(CinzaCabecalhoTabela).PaddingVertical(2).AlignCenter()
                        .Text(titulo).FontSize(11).Bold().Italic();
            });
            foreach (var l in linhas)
            {
                Celula(t).Text(l.Parametro);
                Celula(t).Text(x => Expoente(x, l.Resultado));
                if (comEspecificacao) Celula(t).Text(x => Expoente(x, l.Especificacao));
            }
        });
    }

    private static IContainer Celula(TableDescriptor t) =>
        t.Cell().Border(0.75f).MinHeight(32).PaddingHorizontal(4).PaddingVertical(4).AlignCenter().AlignMiddle();

    // "7,6e1 UFC/mL" → "7,6 x 10" + "1" sobrescrito + " UFC/mL" (como o legado)
    private static void Expoente(TextDescriptor t, string? texto)
    {
        t.AlignCenter();
        var p = ResultadoTexto.Partes(texto);
        if (p is null) return;
        t.Span(p.Antes);
        if (p.Expoente is null) return;
        t.Span(" x 10");
        t.Span(p.Expoente).Superscript();
        t.Span(p.Depois);
    }

    private static void Assinaturas(IContainer c, BoletimDocumento d) => c.Row(row =>
    {
        foreach (var r in d.Responsaveis)
            row.RelativeItem().PaddingRight(10).Column(col =>
            {
                // assinatura só no emitido (RN-08.11)
                if (d.Modo == ModoBoletim.Emitido && r.Assinatura is not null) col.Item().Height(36).AlignLeft().Image(r.Assinatura).FitArea();
                else col.Item().Height(24);
                col.Item().Text(r.Nome);
                if (r.Cargo is not null) col.Item().Text(r.Cargo);
                if (r.Registro is not null) col.Item().Text(r.Registro);
            });
        if (d.Responsaveis.Count == 1) row.RelativeItem();
    });

    // RODAPE_LAUDO (T03): 1ª linha à esquerda do nº da página, 2ª à direita, as demais centralizadas abaixo
    private static void Rodape(IContainer c, BoletimDocumento d) => c.PaddingTop(6).Column(col =>
    {
        var linhas = (d.Rodape ?? "").Replace("\r\n", "\n").Split('\n', StringSplitOptions.TrimEntries);
        col.Item().Row(row =>
        {
            row.RelativeItem().AlignBottom().PaddingLeft(30).Text(linhas.ElementAtOrDefault(0) ?? "").FontSize(7);
            row.ConstantItem(120).AlignCenter().Text(t =>
            {
                t.Span("Página ");
                t.CurrentPageNumber();
                t.Span(" de ");
                t.TotalPages();
            });
            row.RelativeItem().AlignBottom().Text(linhas.ElementAtOrDefault(1) ?? "").FontSize(7);
        });
        foreach (var l in linhas.Skip(2).Where(l => l.Length > 0))
            col.Item().AlignCenter().Text(l).FontSize(7);
    });
}
