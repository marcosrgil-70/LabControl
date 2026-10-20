using LabControl.Data;
using LabControl.Infra;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services;

/// <summary>
/// Painel da Home (T01): contadores da empresa ativa (filtro global), cada um só com a permissão de consulta
/// da tela correspondente. "Hoje" é a data do servidor MySQL (a mesma que as triggers usam).
/// </summary>
public sealed class PainelService(LabDbContext db, IEmpresaContexto usuario, TelasExistentes telas)
{
    public async Task<IReadOnlyList<ContadorPainel>> ContadoresAsync()
    {
        var lista = new List<ContadorPainel>();
        if (usuario.EmpresaId is null) return lista;
        var hoje = await db.Database.SqlQueryRaw<DateOnly>("SELECT CURDATE() AS Value").SingleAsync();
        var inicioMes = new DateOnly(hoje.Year, hoje.Month, 1);

        if (usuario.TemPermissao("AMOSTRAS.CONSULTAR"))
        {
            // amostra física = revisão 0 (as revisões não são novas entregas)
            var fisicas = db.LabHistAmostras.Where(a => a.Revisao == 0);
            var hojeQt = await fisicas.CountAsync(a => a.DtEntrega == hoje);
            var mesQt = await fisicas.CountAsync(a => a.DtEntrega >= inicioMes && a.DtEntrega <= hoje);
            lista.Add(new("Amostras recebidas hoje", "bi-droplet", "primary", hojeQt, $"{mesQt} no mês", Link("Amostras")));
        }
        if (usuario.TemPermissao("RESULTADOS.CONSULTAR"))
        {
            var pendentes = await db.VwLabTestesPendentes.CountAsync();
            var vencidos = await db.VwLabTestesPendentes.CountAsync(t => t.DiasParaPrazo < 0);
            lista.Add(new("Testes pendentes", "bi-hourglass-split", vencidos > 0 ? "danger" : "warning", pendentes,
                vencidos > 0 ? $"{vencidos} com prazo vencido" : "nenhum vencido", Link("Resultados")));
        }
        if (usuario.TemPermissao("BOLETINS.CONSULTAR"))
        {
            var naoEmitidos = await db.VwLabBoletins.CountAsync(b => b.Emitido == 0 && b.UltimaRevisao);
            lista.Add(new("Boletins não emitidos", "bi-file-earmark-check", "info", naoEmitidos, "última revisão", Link("Boletins")));
        }
        if (usuario.TemPermissao("PROPOSTAS.CONSULTAR"))
        {
            var enviado = await db.EmpresasParametros.Select(p => p.IdLabPropostasStatusEnvcli).SingleOrDefaultAsync();
            var aguardando = enviado is null ? 0
                : await db.LabPropostas.CountAsync(p => p.UltimaRevisao && p.IdLabPropostasStatus == enviado);
            lista.Add(new("Propostas aguardando cliente", "bi-file-text", "success", aguardando, "enviadas ao cliente", Link("Propostas")));
        }
        return lista;
    }

    private string? Link(string controller) => telas.Existe(controller) ? $"/{controller}" : null;
}
