namespace LabControl.Infra;

/// <summary>Contas das listas paginadas (sempre ao menos 1 página; página fora do intervalo vai para a mais próxima).</summary>
public static class Paginacao
{
    public static int Paginas(int total, int tamanho) => Math.Max(1, (total + tamanho - 1) / tamanho);

    public static int Ajustar(int pagina, int total, int tamanho) => Math.Clamp(pagina, 1, Paginas(total, tamanho));
}
