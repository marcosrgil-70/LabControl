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
}
