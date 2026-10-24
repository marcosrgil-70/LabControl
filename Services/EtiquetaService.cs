using System.Text;
using LabControl.Data;
using LabControl.Infra;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services;

/// <summary>
/// T25 — Etiquetas de amostras (tarefa 4.5; P19). Decisão do usuário (dúvida 3): etiqueta 80 × 50 mm na térmica
/// <b>Argox</b>, <b>Code 128</b>, impressão <b>direta</b> (PPLB em RAW no spooler do servidor) na impressora escolhida.
/// O layout segue o EPL2 do legado (<c>un_EtqAmostra.pas</c>: mesma inicialização e rotação de 180°), na área maior.
/// </summary>
public sealed class EtiquetaService(LabDbContext db, IImpressoraRaw impressoras)
{
    public const int Largura = 640, Altura = 400, Gap = 24; // 80 × 50 mm a 203 dpi; 3 mm entre etiquetas
    public const int MaxCopias = 99;
    public const int MaxLinhas = 200;
    private static readonly Encoding Cp1252 = CodePagesEncodingProvider.Instance.GetEncoding(1252)!;

    public async Task<EtiquetaPaginaVM> ListarAsync(EtiquetaFiltroVM f)
    {
        var q = db.VwLabAmostras.AsNoTracking();
        if (f.Amostra is { } id) q = q.Where(a => a.IdLabHistAmostras == id);
        else
        {
            // como o legado: só amostras com status que permite resultado (liberadas), da empresa ativa
            q = q.Where(a => a.PermiteResultado);
            if (f.SoUltima) q = q.Where(a => a.UltimaRevisao);
            if (Textos.Limpo(f.Cliente) is { } c)
                q = q.Where(a => a.ClienteNome.Contains(c) || (a.ClienteFantasia != null && a.ClienteFantasia.Contains(c))
                                 || (a.ClienteCpfCnpj != null && a.ClienteCpfCnpj.Contains(c)));
            if (Textos.Limpo(f.Numero) is { } n) q = q.Where(a => a.NrAmostra.Contains(n));
            if (f.APartirDe is { } de) q = q.Where(a => a.DtEntrega >= de);
        }
        // até 200 linhas: cada uma manda 4 campos no POST e o limite padrão do formulário é 1.024 valores (code-review 4.6)
        var itens = await q.OrderByDescending(a => a.DtEntrega).ThenByDescending(a => a.IdLabHistAmostras).Take(MaxLinhas)
            .Select(a => new EtiquetaLinhaVM(a.IdLabHistAmostras, a.NrAmostra, a.ClienteNome, a.ProdutoDescricao, a.DtEntrega, a.NrPedidoFmt,
                a.Fabricacao, a.Validade, (int)Math.Ceiling(a.QtdeEmbalagensEntregue)))
            .ToListAsync();
        return new EtiquetaPaginaVM { Filtro = f, Itens = itens, Impressoras = impressoras.Listar() };
    }

    /// <summary>Hoje − 7 dias (padrão do legado).</summary>
    public async Task<DateOnly> APartirDePadraoAsync() =>
        (await db.HojeAsync()).AddDays(-7);

    /// <summary>Imprime as amostras marcadas, cada uma com a sua quantidade; devolve o total de etiquetas.</summary>
    public async Task<Resultado<int>> ImprimirAsync(EtiquetaImprimirVM vm)
    {
        if (Textos.Limpo(vm.Impressora) is not { } impressora) return Resultado.Erro("Escolha a impressora.", nameof(vm.Impressora));
        if (!impressoras.Listar().Contains(impressora)) return Resultado.Erro($"A impressora “{impressora}” não está instalada no servidor.", nameof(vm.Impressora));
        var marcadas = vm.Itens.Where(i => i.Marcada).ToList();
        if (marcadas.Count == 0) return Resultado.Erro("Marque ao menos uma amostra.");
        if (marcadas.Any(i => i.Quantidade is < 1 or > MaxCopias)) return Resultado.Erro($"Quantidade de etiquetas: de 1 a {MaxCopias} por amostra.");

        var ids = marcadas.Select(i => i.Id).ToList();
        var dados = await db.VwLabAmostras.AsNoTracking().Where(a => ids.Contains(a.IdLabHistAmostras))
            .Select(a => new { a.IdLabHistAmostras, d = new EtiquetaDados(a.NrAmostra, a.ProdutoDescricao, a.NrPedidoFmt, a.DtEntrega, a.Fabricacao, a.Validade) })
            .ToDictionaryAsync(x => x.IdLabHistAmostras, x => x.d);
        if (dados.Count != ids.Distinct().Count()) return Resultado.Erro("Amostra não encontrada (pode ter sido excluída).");

        var comandos = Pplb(marcadas.Select(i => (dados[i.Id], i.Quantidade)), vm.SemProduto);
        try { impressoras.Enviar(impressora, "LabControl - etiquetas de amostras", comandos); }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or PlatformNotSupportedException)
        {
            return Resultado.Erro($"Não foi possível imprimir em “{impressora}”: {ex.Message}", nameof(vm.Impressora));
        }
        return Resultado.Ok(marcadas.Sum(i => i.Quantidade));
    }

    /// <summary>
    /// Comandos PPLB/EPL2 (Argox modo PPLB = EPL2 do legado): inicialização do legado (<c>I8,A</c> = 8 bits Windows-1252,
    /// <c>S3</c>, <c>OD</c>, <c>JF</c>, <c>ZT</c>), área <c>q640</c> × <c>Q400,24</c>, impressão girada 180° (como o
    /// legado imprimia nesta impressora) e Code 128 (<c>B…,1,…</c>) com o nº da amostra sem espaços. Uma etiqueta por
    /// amostra com <c>P{n}</c> cópias.
    /// </summary>
    public static byte[] Pplb(IEnumerable<(EtiquetaDados Dados, int Copias)> etiquetas, bool semProduto)
    {
        var s = new StringBuilder();
        void L(string linha) => s.Append(linha).Append("\r\n");
        // posição visual (a partir do canto superior esquerdo da etiqueta como é lida) → coordenada com rotação 180°
        void Texto(int vx, int vy, int fonte, int mult, string texto) =>
            L($"A{Largura - vx},{Altura - vy},2,{fonte},{mult},{mult},N,\"{Escapar(texto)}\"");

        foreach (var (d, copias) in etiquetas)
        {
            L("");
            foreach (var c in new[] { "I8,A", "ZN", $"q{Largura}", "S3", "OD", "JF", "ZT", $"Q{Altura},{Gap}", "N" }) L(c);
            Texto(24, 18, 4, 2, d.NrAmostra);
            L($"B{Largura - 24},{Altura - 76},2,1,2,2,100,N,\"{Escapar(d.NrAmostra.Replace(" ", ""))}\"");
            var produto = semProduto ? "" : d.Produto ?? "";
            Texto(24, 190, 3, 1, Parte(produto, 0, 48));
            Texto(24, 214, 3, 1, Parte(produto, 48, 48));
            Texto(24, 250, 3, 1, $"PV: {d.NrPedido}");
            Texto(24, 280, 3, 1, $"Recebimento: {d.DtEntrega:dd/MM/yyyy}");
            Texto(24, 310, 3, 1, $"Fabricação: {d.Fabricacao}");
            Texto(24, 340, 3, 1, $"Validade: {d.Validade ?? "---"}");
            L($"P{copias}");
        }
        return Cp1252.GetBytes(s.ToString());
    }

    private static string Parte(string texto, int inicio, int tamanho) => texto.Length <= inicio ? "" : texto.Substring(inicio, Math.Min(tamanho, texto.Length - inicio)).Trim();

    // aspas e barra invertida dentro do texto do comando
    private static string Escapar(string texto) => texto.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
