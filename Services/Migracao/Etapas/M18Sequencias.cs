using System.Data.Common;
using LabControl.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LabControl.Services.Migracao.Etapas;

/// <summary>
/// M-18 — sequências de numeração (M-18-sequencias.md). As linhas de <c>SEQUENCIAS_DOCUMENTOS</c> já foram gravadas
/// pelas etapas que trazem cada documento (produto M-10, proposta M-11, pedido M-13, amostra M-14); aqui só se confere
/// que nenhuma ficou abaixo do maior número usado e que o próximo número sai certo: com a migração desligada, o
/// <c>SP_PROXIMO_NUMERO</c> roda dentro de um savepoint desfeito em seguida (o "teste numa cópia" — nada fica gravado).
/// </summary>
public sealed class M18Sequencias : IEtapaMigracao
{
    public string Codigo => "M-18";
    public string Nome => "Sequências de numeração";

    public async Task<int> ExecutarAsync(ContextoMigracao c, CancellationToken ct)
    {
        var seq = await c.Db.SequenciasDocumentos.AsNoTracking()
            .ToDictionaryAsync(s => (s.TipoDocumento, (int)s.Ano), s => s.UltimoNumero, ct);
        // os provisórios do legado (código = ID) ficaram fora da sequência da proposta (M-11)
        var usados = (await c.Db.LabHistAmostras.GroupBy(a => (int)a.AnoAmostra).Select(g => new { Ano = g.Key, Max = g.Max(a => a.CodAmostra) }).ToListAsync(ct))
            .Select(x => ("AMOSTRA", x.Ano, x.Max))
            .Concat((await c.Db.LabPropostas.Where(p => p.CodProposta != p.IdLegado).GroupBy(p => (int)p.AnoProposta)
                .Select(g => new { Ano = g.Key, Max = g.Max(p => p.CodProposta) }).ToListAsync(ct)).Select(x => ("PROPOSTA", x.Ano, x.Max)))
            .Append(("PEDIDO_VENDA", 0, await c.Db.PedidosVenda.MaxAsync(p => (int?)p.NrPedido, ct) ?? 0))
            .ToList();
        var abaixo = usados.Where(u => seq.GetValueOrDefault((u.Item1, u.Item2)) < u.Item3).ToList();
        foreach (var (tipo, ano, max) in abaixo)
            c.Anomalia($"{tipo} {ano}: sequência em {seq.GetValueOrDefault((tipo, ano))}, abaixo do maior número usado ({max}).");
        c.Conferir("sequências abaixo do maior número usado", 0, abaixo.Count);
        foreach (var ((tipo, ano), ultimo) in seq.OrderBy(s => s.Key))
            c.Log($"{tipo}{(ano == 0 ? "" : $" {ano}")}: último {ultimo}.");

        var anoAtual = await Escalar(c, "SELECT YEAR(CURDATE())", ct);
        var conferir = new[] { ("AMOSTRA", anoAtual), ("PROPOSTA", anoAtual), ("PEDIDO_VENDA", 0), ("PRODUTO", 0) };
        var obtidos = new List<string>();
        await Escalar(c, "SAVEPOINT M18_PROXIMO", ct);
        try
        {
            await Escalar(c, "SET @LC_MIGRACAO = 0", ct);
            var procedures = new Procedures(c.Db, c.Empresa);
            foreach (var (tipo, ano) in conferir)
            {
                var n = await procedures.ProximoNumeroAsync(tipo, ano);
                obtidos.Add($"{tipo} {ano}: {(n.Sucesso ? n.Valor.ToString() : n.Mensagem)}");
            }
        }
        finally
        {
            await Escalar(c, "ROLLBACK TO SAVEPOINT M18_PROXIMO", CancellationToken.None);
            await c.LigarModoMigracaoAsync(CancellationToken.None);
        }
        c.Conferir("próximo número (teste desfeito em seguida)",
            string.Join("; ", conferir.Select(x => $"{x.Item1} {x.Item2}: {seq.GetValueOrDefault((x.Item1, x.Item2)) + 1}")), string.Join("; ", obtidos));
        c.Conferir("sequências intactas depois do teste", seq.Count, await c.Db.SequenciasDocumentos.CountAsync(ct));
        return 0;
    }

    private static async Task<int> Escalar(ContextoMigracao c, string sql, CancellationToken ct)
    {
        await using DbCommand cmd = c.Db.Database.GetDbConnection().CreateCommand();
        cmd.Transaction = c.Db.Database.CurrentTransaction?.GetDbTransaction();
        cmd.CommandText = sql;
        return await cmd.ExecuteScalarAsync(ct) is { } v and not DBNull ? Convert.ToInt32(v) : 0;
    }
}
