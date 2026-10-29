using LabControl.Data;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Infra;

/// <summary>
/// "Agora" do servidor MySQL: é dele o ano do número (triggers) e o limite das datas, então as telas sugerem e
/// validam com o mesmo relógio — nunca o do servidor da aplicação (fuso ou relógio diferentes, code-review 4.6).
/// </summary>
public static class RelogioBanco
{
    public static Task<DateTime> AgoraAsync(this LabDbContext db) =>
        db.Database.SqlQueryRaw<DateTime>("SELECT NOW() AS Value").SingleAsync();

    public static async Task<DateOnly> HojeAsync(this LabDbContext db) => DateOnly.FromDateTime(await db.AgoraAsync());

    /// <summary>Data/hora sem segundos (o campo <c>datetime-local</c> das telas trabalha no minuto).</summary>
    public static DateTime NoMinuto(this DateTime d) => new(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0, d.Kind);

    /// <summary>Amostra física (raiz, revisão 0) da amostra ou revisão, na empresa ativa; null se não existir.</summary>
    public static Task<int?> AmostraFisicaAsync(this LabDbContext db, int idAmostra) =>
        db.LabHistAmostras.Where(a => a.IdLabHistAmostras == idAmostra).Select(a => (int?)(a.IdAmostraRaiz ?? a.IdLabHistAmostras)).SingleOrDefaultAsync();
}
