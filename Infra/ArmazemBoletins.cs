using System.Security.Cryptography;

namespace LabControl.Infra;

/// <summary>
/// Pasta dos PDFs emitidos (dúvida 6: pasta no servidor; P17). Configurada em <c>Boletins:Pasta</c> (relativa ao
/// ContentRoot ou absoluta; padrão <c>storage/boletins</c>). O banco guarda o caminho <b>relativo</b> a ela
/// (<c>ARQUIVO_PDF</c>) e o SHA-256 (<c>HASH_PDF</c>): a pasta pode mudar de lugar sem tocar no banco. Entra no backup.
/// </summary>
public sealed class ArmazemBoletins(string raiz)
{
    // sem a barra final ("D:\Boletins\", "E:\"): a conferência de "dentro da pasta" acrescenta a dela
    public string Raiz { get; } = Path.TrimEndingDirectorySeparator(Path.GetFullPath(raiz));

    /// <summary>Pasta de <c>Boletins:Pasta</c> (relativa ao ContentRoot ou absoluta; padrão <c>storage/boletins</c>).</summary>
    public static ArmazemBoletins DaConfiguracao(IConfiguration config, IWebHostEnvironment env) =>
        new(Path.Combine(env.ContentRootPath, config["Boletins:Pasta"] is { Length: > 0 } p ? p : "storage/boletins"));

    /// <summary><c>{empresa}/{ano}/{código com "/"→"-"} R{rev}.pdf</c>, ex.: <c>001/2026/04-00012-01-26 R00.pdf</c>.</summary>
    public static string CaminhoRelativo(string codEmpresa, int ano, string codBoletim, int revisao)
    {
        var invalidos = Path.GetInvalidFileNameChars();
        var nome = string.Concat(codBoletim.Select(c => c == '/' || invalidos.Contains(c) ? '-' : c));
        return $"{codEmpresa}/{ano}/{nome} R{revisao:00}.pdf";
    }

    private string Completo(string relativo)
    {
        var completo = Path.GetFullPath(Path.Combine(Raiz, relativo));
        // nunca fora da pasta (o caminho vem do banco)
        if (!completo.StartsWith(Raiz + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Caminho de boletim fora da pasta configurada: {relativo}");
        return completo;
    }

    /// <summary>
    /// 1º passo da emissão: grava o PDF com um nome provisório e único ao lado do definitivo e devolve esse nome e o SHA-256.
    /// Duas emissões ao mesmo tempo nunca escrevem no mesmo arquivo; só quem o banco aceitar <see cref="Confirmar"/>.
    /// </summary>
    public async Task<(string Provisorio, string Hash)> PrepararAsync(string relativo, byte[] pdf)
    {
        var provisorio = $"{relativo}.{Guid.NewGuid():N}.tmp";
        var completo = Completo(provisorio);
        Directory.CreateDirectory(Path.GetDirectoryName(completo)!);
        await File.WriteAllBytesAsync(completo, pdf);
        return (provisorio, Hash(pdf));
    }

    /// <summary>2º passo, depois que o banco registrou a emissão: o provisório vira o arquivo definitivo.</summary>
    public void Confirmar(string provisorio, string relativo) => File.Move(Completo(provisorio), Completo(relativo), overwrite: true);

    /// <summary>O arquivo gravado, ou null se não existe.</summary>
    public async Task<byte[]?> LerAsync(string relativo)
    {
        var completo = Completo(relativo);
        return File.Exists(completo) ? await File.ReadAllBytesAsync(completo) : null;
    }

    public void Apagar(string relativo)
    {
        var completo = Completo(relativo);
        if (File.Exists(completo)) File.Delete(completo);
    }

    public static string Hash(byte[] dados) => Convert.ToHexStringLower(SHA256.HashData(dados));
}
