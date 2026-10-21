using LabControl.Data;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services.Geografia;

/// <summary>Combos de geografia (tabelas globais) usados pelo endereço dos contatos, pela T13 e pela T11.</summary>
public static class ConsultasGeografia
{
    /// <summary>UFs pela sigla (<c>{ id, "SP" }</c>).</summary>
    public static Task<List<OpcaoVM>> UfsAsync(this LabDbContext db) =>
        db.Estados.AsNoTracking().OrderBy(e => e.Sigla).Select(e => new OpcaoVM(e.IdEstados, e.Sigla)).ToListAsync();

    /// <summary>Cidades da UF pelo nome.</summary>
    public static Task<List<OpcaoVM>> CidadesDaUfAsync(this LabDbContext db, int idEstado) =>
        db.Cidades.AsNoTracking().Where(c => c.IdEstados == idEstado).OrderBy(c => c.Nome).Select(c => new OpcaoVM(c.IdCidades, c.Nome)).ToListAsync();
}
