using LabControl.Infra;
using LabControl.Models;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services.Migracao.Etapas;

/// <summary>
/// M-04 — parâmetros da empresa (M-04-parametros-empresa.md) e, ao final, <c>SP_INICIALIZAR_EMPRESA</c>: completa o
/// que o legado não tinha (status/tipos padrão por INSERT IGNORE, perfis com permissões). <c>ULT_*</c> não migram
/// (sequências na M-10/M-18); o status "enviado ao cliente", nulo no legado, aponta para o de mesmo nome.
/// </summary>
public sealed class M04ParametrosEmpresa : IEtapaMigracao
{
    public string Codigo => "M-04";
    public string Nome => "Parâmetros da empresa";

    public async Task<int> ExecutarAsync(ContextoMigracao c, CancellationToken ct)
    {
        var legado = (await c.Fb.LerAsync("SELECT ID_LAB_PROPOSTAS_STATUS_ENVCLI, ID_AMOSTRAS_STATUS_RESULTADOS FROM EMPRESAS_PARAMETROS", ct)).SingleOrDefault();
        var envcli = c.Novo("LAB_PROPOSTAS_STATUS", legado?.Int("ID_LAB_PROPOSTAS_STATUS_ENVCLI"), "parâmetros");
        if (envcli is null)
        {
            var enviado = (await c.Db.LabPropostasStatus.ToListAsync(ct)).FirstOrDefault(s => Conversoes.NomeComparavel(s.Descricao).Contains("ENVIAD"));
            envcli = enviado?.IdLabPropostasStatus;
            c.Anomalia(enviado is null
                ? "Status \"enviado ao cliente\" nulo no legado e nenhum status com esse nome: a data de envio da proposta não será gravada sozinha (defina na T05)."
                : $"Status \"enviado ao cliente\" nulo no legado: apontado para \"{enviado.Descricao}\".");
        }

        c.Db.Add(new EmpresaParametros
        {
            IdEmpresas = c.IdEmpresa, IdLabPropostasStatusEnvcli = envcli,
            IdAmostrasStatusResultados = c.Novo("AMOSTRAS_STATUS", legado?.Int("ID_AMOSTRAS_STATUS_RESULTADOS"), "parâmetros"),
            DiasRetroativosEntrega = 7, DiasValidadeProposta = 30, DiasPesquisaPadrao = 365,
        });
        await c.Db.SaveChangesAsync(ct);

        // o que o v2 cria por padrão e o legado não tinha fica listado (pode ser inativado na T10)
        var antes = await ContarAuxiliaresAsync(c, ct);
        var inicializar = await new Procedures(c.Db, c.Empresa).InicializarEmpresaAsync(c.IdEmpresa);
        if (!inicializar.Sucesso) throw new InvalidOperationException($"SP_INICIALIZAR_EMPRESA: {inicializar.Mensagem}");
        await c.LigarModoMigracaoAsync(ct); // por garantia: a SP não mexe na variável, mas a etapa seguinte depende dela
        var depois = await ContarAuxiliaresAsync(c, ct);
        foreach (var (tabela, n) in depois.Where(d => d.Value > antes[d.Key]))
            c.Anomalia($"{tabela}: {n - antes[tabela]} registro(s) padrão do v2 incluídos pela inicialização da empresa (sem correspondente no legado).");

        var p = await c.Db.EmpresasParametros.AsNoTracking().SingleAsync(ct);
        var statusDaEmpresa = await c.Db.LabPropostasStatus.Select(s => s.IdLabPropostasStatus).ToListAsync(ct);
        var amostras = await c.Db.AmostrasStatus.Select(s => s.IdAmostrasStatus).ToListAsync(ct);
        c.Conferir("parâmetros apontam para status da empresa", true,
            (p.IdLabPropostasStatusEnvcli is null || statusDaEmpresa.Contains(p.IdLabPropostasStatusEnvcli.Value))
            && (p.IdAmostrasStatusResultados is null || amostras.Contains(p.IdAmostrasStatusResultados.Value)));
        c.Conferir("perfil ADMINISTRADOR criado", 1, await c.Db.Perfis.CountAsync(x => x.Nome == "ADMINISTRADOR", ct));
        return 1;
    }

    private static async Task<Dictionary<string, int>> ContarAuxiliaresAsync(ContextoMigracao c, CancellationToken ct) => new()
    {
        ["Status da amostra"] = await c.Db.AmostrasStatus.CountAsync(ct),
        ["Status da análise"] = await c.Db.AnalisesStatus.CountAsync(ct),
        ["Status do boletim"] = await c.Db.BoletinsStatus.CountAsync(ct),
        ["Status da proposta"] = await c.Db.LabPropostasStatus.CountAsync(ct),
        ["Idiomas"] = await c.Db.Idiomas.CountAsync(ct),
        ["Moedas"] = await c.Db.Moedas.CountAsync(ct),
        ["Tipos de endereço"] = await c.Db.EnderecosTipos.CountAsync(ct),
        ["Tipos de telefone"] = await c.Db.FonesTipos.CountAsync(ct),
        ["Tipos de e-mail"] = await c.Db.EmailsTipos.CountAsync(ct),
        ["Registros profissionais"] = await c.Db.TiposRegProfissional.CountAsync(ct),
        ["Tipos de logradouro"] = await c.Db.TiposLogradouro.CountAsync(ct),
    };
}
