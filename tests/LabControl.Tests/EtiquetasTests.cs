using System.Text;
using LabControl.Infra;
using LabControl.Services;
using LabControl.ViewModels;

namespace LabControl.Tests;

/// <summary>Impressora falsa: lista nomes fixos e guarda o que seria enviado ao spooler.</summary>
public sealed class ImpressoraDoTeste : IImpressoraRaw
{
    public const string Argox = "ARGOX OS-214 (TESTE)";
    public List<(string Impressora, string Documento, byte[] Dados)> Enviados { get; } = [];
    public IReadOnlyList<string> Listar() => [Argox, "HP LASERJET (TESTE)"];
    public void Enviar(string impressora, string documento, byte[] dados) { lock (Enviados) Enviados.Add((impressora, documento, dados)); }
}

/// <summary>Aceite da 4.5 (T25): comandos PPLB (Argox, 80 × 50 mm, Code 128), quantidade por amostra, sigilo do produto e impressão na escolhida.</summary>
public class EtiquetasTests
{
    private static readonly Encoding Cp1252 = CodePagesEncodingProvider.Instance.GetEncoding(1252)!;

    [Fact]
    public void Comandos_pplb_80x50_code128_com_copias_e_sigilo()
    {
        var d = new EtiquetaDados("04-01382-01/26 R01", "QUEIJO MINAS FRESCAL \"ESPECIAL\" COM UM NOME BEM COMPRIDO QUE OCUPA DUAS LINHAS", "000000123",
            new DateOnly(2026, 10, 23), "10/2026", null);
        var bytes = EtiquetaService.Pplb([(d, 3)], semProduto: false);
        var t = Cp1252.GetString(bytes);
        var linhas = t.Split("\r\n");

        // inicialização do legado na área nova (640 × 400 pontos, gap 24) — 8 bits Windows-1252
        Assert.Equal(["", "I8,A", "ZN", "q640", "S3", "OD", "JF", "ZT", "Q400,24", "N"], linhas[..10]);
        // Code 128 (tipo 1) com o número sem espaços, girado 180° como o legado
        Assert.Contains("B616,324,2,1,2,2,100,N,\"04-01382-01/26R01\"", linhas);
        Assert.Contains("A616,382,2,4,2,2,N,\"04-01382-01/26 R01\"", linhas);
        Assert.Contains("A616,120,2,3,1,1,N,\"Recebimento: 23/10/2026\"", linhas);
        Assert.Contains("A616,60,2,3,1,1,N,\"Validade: ---\"", linhas); // sem validade, como o legado
        Assert.Contains(linhas, l => l.Contains("\\\"ESPECIAL\\\"")); // aspas escapadas
        Assert.Equal("P3", linhas[^2]);
        Assert.Contains((byte)0xE7, bytes); // "ç" de Fabricação em 1 byte (cp1252)

        // sigilo: as duas linhas do produto vão vazias
        var sem = Cp1252.GetString(EtiquetaService.Pplb([(d, 1)], semProduto: true));
        Assert.DoesNotContain("QUEIJO", sem);
        Assert.Contains("A616,210,2,3,1,1,N,\"\"", sem);
        // duas amostras = duas etiquetas, cada uma com as suas cópias
        var duas = Cp1252.GetString(EtiquetaService.Pplb([(d, 2), (d with { NrAmostra = "04-01383-01/26" }, 5)], false));
        Assert.Equal(["P2", "P5"], duas.Split("\r\n").Where(l => l.StartsWith('P')).ToArray());
    }

    [Fact]
    public async Task Imprime_na_escolhida_so_as_marcadas_com_a_quantidade()
    {
        await using var c = await Cenario.CriarAsync();
        var amostra = c.NovaAmostra(await c.HojeAsync());
        c.Db.Add(amostra);
        await c.Db.SaveChangesAsync();
        var impressora = new ImpressoraDoTeste();
        var s = new EtiquetaService(c.Db, impressora);

        // a amostra vinda do recebimento aparece com a quantidade de embalagens (3)
        var pagina = await s.ListarAsync(new EtiquetaFiltroVM { Amostra = amostra.IdLabHistAmostras });
        Assert.Equal([(amostra.NrAmostra, 3)], pagina.Itens.Select(i => (i.NrAmostra, i.Quantidade)));
        Assert.Contains(ImpressoraDoTeste.Argox, pagina.Impressoras);

        EtiquetaImprimirVM Pedido(string? imp, bool marcada = true, int qtde = 3) => new()
            { Impressora = imp, Itens = [new() { Id = amostra.IdLabHistAmostras, Marcada = marcada, Quantidade = qtde }] };
        Assert.Equal("Escolha a impressora.", (await s.ImprimirAsync(Pedido(null))).Mensagem);
        Assert.StartsWith("A impressora “OUTRA” não está instalada", (await s.ImprimirAsync(Pedido("OUTRA"))).Mensagem);
        Assert.Equal("Marque ao menos uma amostra.", (await s.ImprimirAsync(Pedido(ImpressoraDoTeste.Argox, marcada: false))).Mensagem);
        Assert.StartsWith("Quantidade de etiquetas", (await s.ImprimirAsync(Pedido(ImpressoraDoTeste.Argox, qtde: 0))).Mensagem);
        Assert.Empty(impressora.Enviados);

        var r = await s.ImprimirAsync(Pedido(ImpressoraDoTeste.Argox));
        Assert.Equal(3, r.Valor);
        var (nome, _, dados) = Assert.Single(impressora.Enviados);
        Assert.Equal(ImpressoraDoTeste.Argox, nome);
        var texto = Cp1252.GetString(dados);
        Assert.Contains($"\"{amostra.NrAmostra.Replace(" ", "")}\"", texto);
        Assert.Contains("QUEIJO MINAS", texto);
        Assert.EndsWith("P3\r\n", texto);
    }

    [Fact]
    public void Spooler_do_windows_lista_as_impressoras_instaladas()
    {
        if (!OperatingSystem.IsWindows()) return;
        // toda instalação do Windows 10/11 tem a impressora virtual de PDF; a lista vem do EnumPrinters
        var lista = new ImpressoraRawWindows().Listar();
        Assert.NotEmpty(lista);
        Assert.All(lista, n => Assert.False(string.IsNullOrWhiteSpace(n)));
    }
}
