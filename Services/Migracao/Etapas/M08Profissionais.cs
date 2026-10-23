using LabControl.Models;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services.Migracao.Etapas;

/// <summary>
/// M-08 — profissionais (M-08-profissionais.md): ENTIDADES_FUNCIONARIOS → ENTIDADES_FUNCIONARIOS (a chave é a
/// entidade). Entidade marcada como funcionário sem linha no legado ganha uma linha mínima. <c>ASSINA_BOLETIM</c> =
/// já foi Responsável 1/2 em algum boletim do legado; <c>EXECUTA_ANALISE</c> = todos.
/// </summary>
public sealed class M08Profissionais : IEtapaMigracao
{
    public string Codigo => "M-08";
    public string Nome => "Profissionais";

    public async Task<int> ExecutarAsync(ContextoMigracao c, CancellationToken ct)
    {
        var linhas = await c.Fb.LerAsync("SELECT ID_ENTIDADES, NR_REGISTRO_PROFISSIONAL, ID_TIPOS_REG_PROFISSIONAL, ID_CARGO_FUNCIONARIO FROM ENTIDADES_FUNCIONARIOS", ct);
        var assinam = (await c.Fb.LerAsync("""
            SELECT ID_ENTIDADES_FUNC_RESP AS ID FROM LAB_BOLETIM_ANALISES WHERE ID_ENTIDADES_FUNC_RESP IS NOT NULL
            UNION SELECT ID_ENTIDADES_FUNC_RESP2 FROM LAB_BOLETIM_ANALISES WHERE ID_ENTIDADES_FUNC_RESP2 IS NOT NULL
            """, ct)).Select(l => l.IntObrigatorio("ID")).ToHashSet();
        var marcados = (await c.Fb.LerAsync("SELECT ID_ENTIDADES FROM ENTIDADES WHERE TIPO_FUNCIONARIO = 'S'", ct)).Select(l => l.IntObrigatorio("ID_ENTIDADES")).ToHashSet();

        // a entidade do legado que só é a empresa foi juntada à entidade da empresa usuária (M-05), que não pode ser
        // funcionário (PR_CONFERIR_TIPOS_ENTIDADE, também na migração): fica de fora, com anomalia
        var daEmpresa = new HashSet<int>();
        bool DaEmpresa(int antigo, int entidade)
        {
            if (entidade != c.EntidadeDaEmpresaUsuaria.Nova) return false;
            if (daEmpresa.Add(antigo)) c.Anomalia($"Profissional {antigo}: é a própria empresa usuária no legado — não migrado como profissional.");
            return true;
        }

        var gravados = new HashSet<int>();
        foreach (var l in linhas)
        {
            var antigo = l.IntObrigatorio("ID_ENTIDADES");
            if (c.Novo("ENTIDADES", antigo, $"profissional {antigo}") is not { } entidade || DaEmpresa(antigo, entidade)) continue;
            if (!marcados.Contains(antigo)) c.Anomalia($"Profissional {antigo}: tem dados de funcionário mas não estava marcado como funcionário — marcado.");
            c.Db.Add(new EntidadeFuncionario
            {
                IdEntidades = entidade,
                IdCargosFuncionarios = c.Novo("CARGO_FUNCIONARIOS", l.Int("ID_CARGO_FUNCIONARIO"), $"profissional {antigo}"),
                IdTiposRegProfissional = c.Novo("TIPOS_REG_PROFISSIONAL", l.Int("ID_TIPOS_REG_PROFISSIONAL"), $"profissional {antigo}"),
                NrRegistroProfissional = c.Cortar<EntidadeFuncionario>(nameof(EntidadeFuncionario.NrRegistroProfissional), l.Texto("NR_REGISTRO_PROFISSIONAL")?.ToUpperInvariant(), $"profissional {antigo}"),
                AssinaBoletim = assinam.Contains(antigo), ExecutaAnalise = true,
            });
            gravados.Add(antigo);
        }
        foreach (var antigo in marcados.Except(gravados).Order())
        {
            if (c.Novo("ENTIDADES", antigo, $"profissional {antigo}") is not { } entidade || DaEmpresa(antigo, entidade)) continue;
            c.Db.Add(new EntidadeFuncionario { IdEntidades = entidade, AssinaBoletim = assinam.Contains(antigo), ExecutaAnalise = true });
            gravados.Add(antigo);
            c.Anomalia($"Profissional {antigo}: marcado como funcionário sem dados de funcionário no legado — criado sem cargo e sem registro (completar na T13).");
        }
        // entidades com linha de funcionário viram funcionário no v2 (flag usada nos combos da T02/T13)
        var ids = gravados.Select(a => c.Mapa("ENTIDADES")[a]).ToList();
        await c.Db.Entidades.Where(e => ids.Contains(e.IdEntidades)).ExecuteUpdateAsync(s => s.SetProperty(e => e.TipoFuncionario, true), ct);
        await c.Db.SaveChangesAsync(ct);
        foreach (var a in gravados) c.Mapa("ENTIDADES_FUNCIONARIOS")[a] = c.Mapa("ENTIDADES")[a];

        c.Conferir("profissionais (linhas do legado + marcados sem linha − a própria empresa)",
            linhas.Select(l => l.IntObrigatorio("ID_ENTIDADES")).Union(marcados).Except(daEmpresa).Count(), gravados.Count);
        c.Conferir("profissionais que assinam boletim", assinam.Count(a => gravados.Contains(a)), await c.Db.EntidadesFuncionarios.CountAsync(f => f.AssinaBoletim, ct));
        return gravados.Count;
    }
}
