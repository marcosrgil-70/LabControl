using System.ComponentModel;
using System.Runtime.InteropServices;

namespace LabControl.Infra;

/// <summary>
/// Impressão direta (T25, decisão do usuário): os comandos da impressora térmica (PPLB/EPL2) vão em RAW para o spooler
/// do Windows do servidor do LabControl, sem driver gráfico. A impressora precisa estar instalada (ou compartilhada)
/// nessa máquina. Nos testes a implementação é trocada por uma que só guarda os bytes.
/// </summary>
public interface IImpressoraRaw
{
    /// <summary>Impressoras instaladas no servidor (nome do Windows).</summary>
    IReadOnlyList<string> Listar();

    /// <summary>Envia os bytes como um trabalho RAW; erro do spooler vira exceção.</summary>
    void Enviar(string impressora, string documento, byte[] dados);
}

/// <summary>Spooler do Windows por <c>winspool.drv</c> (EnumPrinters / OpenPrinter / WritePrinter).</summary>
public sealed class ImpressoraRawWindows : IImpressoraRaw
{
    private const int PrinterEnumLocal = 2, PrinterEnumConnections = 4;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct PrinterInfo4 { public string pPrinterName; public string pServerName; public uint Attributes; }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private sealed class DocInfo1 { public string? pDocName; public string? pOutputFile; public string? pDataType; }

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool EnumPrinters(int flags, string? name, int level, IntPtr buffer, int tamanho, out int necessario, out int retornados);
    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool OpenPrinter(string nome, out IntPtr handle, IntPtr padrao);
    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr handle);
    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int StartDocPrinter(IntPtr handle, int nivel, [In] DocInfo1 doc);
    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr handle);
    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr handle);
    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr handle);
    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr handle, byte[] dados, int tamanho, out int escritos);

    public IReadOnlyList<string> Listar()
    {
        if (!OperatingSystem.IsWindows()) return [];
        const int flags = PrinterEnumLocal | PrinterEnumConnections;
        EnumPrinters(flags, null, 4, IntPtr.Zero, 0, out var necessario, out _);
        if (necessario == 0) return [];
        var buffer = Marshal.AllocHGlobal(necessario);
        try
        {
            if (!EnumPrinters(flags, null, 4, buffer, necessario, out _, out var quantas)) throw new Win32Exception();
            var tamanho = Marshal.SizeOf<PrinterInfo4>();
            return Enumerable.Range(0, quantas).Select(i => Marshal.PtrToStructure<PrinterInfo4>(buffer + i * tamanho).pPrinterName)
                .Order(StringComparer.CurrentCultureIgnoreCase).ToList();
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    public void Enviar(string impressora, string documento, byte[] dados)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Impressão direta só no servidor Windows.");
        if (!OpenPrinter(impressora, out var h, IntPtr.Zero)) throw new Win32Exception();
        try
        {
            if (StartDocPrinter(h, 1, new DocInfo1 { pDocName = documento, pDataType = "RAW" }) == 0) throw new Win32Exception();
            try
            {
                if (!StartPagePrinter(h)) throw new Win32Exception();
                if (!WritePrinter(h, dados, dados.Length, out var escritos) || escritos != dados.Length) throw new Win32Exception();
                EndPagePrinter(h);
            }
            finally { EndDocPrinter(h); }
        }
        finally { ClosePrinter(h); }
    }
}
