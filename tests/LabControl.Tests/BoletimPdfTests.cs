using LabControl.Services.Boletins;
using QuestPDF.Helpers;

namespace LabControl.Tests;

/// <summary>
/// Layout do boletim (5.4), sem banco: como o <c>frxRboletin</c> do legado, o selo da acreditação sai só nas páginas dos
/// parâmetros acreditados e o boletim misto vira duas partes ("A" acreditados com o selo, "B" os demais).
/// Com a variável <c>LABCONTROL_PDF_SAIDA</c> (pasta) os PDFs dos cenários ficam gravados para conferência visual;
/// <c>LABCONTROL_SELO</c> (arquivo PNG/JPG) troca a imagem genérica pelo selo real.
/// </summary>
public class BoletimPdfTests
{
    private static readonly byte[] Selo = Environment.GetEnvironmentVariable("LABCONTROL_SELO") is { Length: > 0 } arq
        ? File.ReadAllBytes(arq) : Placeholders.Image(400, 670);

    private static BoletimDocumento Documento(IReadOnlyList<LinhaBoletim> linhas, int responsaveis = 1) => new()
    {
        Modo = ModoBoletim.Emitido,
        EmpresaNome = "LABORATÓRIO DE TESTE S.A.", EmpresaCidade = "Santa Bárbara d'Oeste",
        SeloAcreditacao = Selo, NrAcreditacao = "CRL 0000",
        Rodape = "FORM 0069 - R2\nEmitente: Gerência da Qualidade",
        CodBoletim = "05-00001-01/26", DtBoletim = new DateOnly(2026, 10, 27),
        ClienteNome = "CLIENTE DE TESTE LTDA.", EnderecoLaudo = ["AVENIDA DE TESTE, 100 - CENTRO", "SANTA BÁRBARA D'OESTE/SP - 13.455-000"],
        Contato = "FULANO DE TAL", NrProposta = "000001/26-R0",
        NrAmostra = "05-00001-01/26", Produto = "ÁGUA", Recebimento = new DateTime(2026, 10, 22, 9, 30, 0), Coletor = "Interessado",
        Coleta = new DateTime(2026, 10, 22), InicioAnalise = new DateOnly(2026, 10, 22), FimAnalise = new DateOnly(2026, 10, 24),
        Linhas = linhas,
        MetodologiaAcreditada = "Bactérias heterotróficas: SMWW, 9215 A e B. 24ª ed. 2023",
        MetodologiaNaoAcreditada = "Coliformes totais: SMWW, 9221 D. 24ª ed. 2023",
        Observacao = "O(s) resultado(s) contido(s) neste Boletim de Análise refere(m)-se somente à amostra enviada pelo interessado.",
        Legenda = "UFC/mL = Unidade Formadora de Colônia por mililitro.",
        Responsaveis = Enumerable.Range(1, responsaveis).Select(i => new ResponsavelBoletim($"RESPONSÁVEL {i}", "COORDENADORA DE LABORATÓRIO", $"CRN-3 {i}0000", null)).ToList(),
    };

    private static readonly LinhaBoletim Acreditada = new("Bactérias heterotróficas - Determinação quantitativa", "7,6e1 UFC/mL", "5,0e2 UFC/mL", true);
    private static readonly LinhaBoletim NaoAcreditada = new("Coliformes totais - Determinação pela técnica de Presença/Ausência", "Ausência em 100 mL", null, false);

    [Fact]
    public void So_acreditados_uma_parte_com_selo_e_metodos_dos_dois_campos()
    {
        var partes = BoletimPdf.Partes(Documento([Acreditada]));
        var p = Assert.Single(partes);
        Assert.True(p.Acreditada);
        Assert.Null(p.Sufixo);
        Assert.Equal("Bactérias heterotróficas: SMWW, 9215 A e B. 24ª ed. 2023\nColiformes totais: SMWW, 9221 D. 24ª ed. 2023", p.Metodos);
        Gravar("acreditado", Documento([Acreditada]));
    }

    [Fact]
    public void Sem_acreditados_uma_parte_sem_selo()
    {
        var p = Assert.Single(BoletimPdf.Partes(Documento([NaoAcreditada])));
        Assert.False(p.Acreditada);
        Assert.Null(p.Sufixo);
        Gravar("nao-acreditado", Documento([NaoAcreditada]));
    }

    [Fact]
    public void Misto_vira_partes_A_com_selo_e_B_sem_selo_cada_uma_com_os_seus_metodos()
    {
        // ordem do boletim com o não acreditado antes: o legado imprime os acreditados primeiro
        var partes = BoletimPdf.Partes(Documento([NaoAcreditada, Acreditada]));
        Assert.Equal([("A", true, Acreditada.Parametro), ("B", false, NaoAcreditada.Parametro)],
            partes.Select(p => (p.Sufixo!, p.Acreditada, Assert.Single(p.Linhas).Parametro)));
        Assert.Equal(["Bactérias heterotróficas: SMWW, 9215 A e B. 24ª ed. 2023", "Coliformes totais: SMWW, 9221 D. 24ª ed. 2023"], partes.Select(p => p.Metodos!));
        Gravar("misto", Documento([NaoAcreditada, Acreditada]));
    }

    [Fact]
    public void Muitos_parametros_dois_responsaveis_e_previa_geram_o_pdf()
    {
        var linhas = Enumerable.Range(1, 12).Select(i => NaoAcreditada with { Parametro = $"PESQUISA {i}", Especificacao = "Ausência em 1 mL" }).ToList();
        Gravar("duas-paginas", Documento(linhas, responsaveis: 2));
        Gravar("previa", new BoletimDocumento
        {
            Modo = ModoBoletim.Previa, EmpresaNome = "X", CodBoletim = "1", ClienteNome = "C", NrAmostra = "1", Produto = "P", Linhas = [NaoAcreditada],
        });
    }

    [Theory]
    [InlineData("7,6e1", "UFC/mL", "7,6e1 UFC/mL")]          // migrado: unidade à parte
    [InlineData("<  10", "UFC/g", "< 10 UFC/g")]             // migrado: espaços repetidos
    [InlineData("< 1,0e2 UFC/g", "UFC/g", "< 1,0e2 UFC/g")]  // v2: já traz a unidade
    [InlineData("12", null, "12")]
    [InlineData(null, "UFC/g", null)]                       // descritivo: sai o texto do tipo de resultado
    public void Resultado_sai_com_a_unidade_uma_vez_so(string? resultado, string? unidade, string? esperado) =>
        Assert.Equal(esperado, LabControl.Services.BoletimService.ComUnidade(resultado, unidade));

    private static void Gravar(string nome, BoletimDocumento d)
    {
        var pdf = BoletimPdf.Gerar(d);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        if (Environment.GetEnvironmentVariable("LABCONTROL_PDF_SAIDA") is { Length: > 0 } pasta)
            File.WriteAllBytes(Path.Combine(pasta, $"boletim-{nome}.pdf"), pdf);
    }
}
