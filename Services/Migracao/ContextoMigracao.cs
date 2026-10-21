using LabControl.Data;
using LabControl.Infra;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services.Migracao;

/// <summary>
/// Empresa "ativa" da migração: nenhuma até a M-01 criar a empresa 1, depois ela. Sem usuário: o usuário logado
/// é do banco do app, não do destino (CRIADO_POR/AUDITORIA apontariam para um ID que não existe lá).
/// </summary>
public sealed class EmpresaDaMigracao : IEmpresaContexto
{
    public int? EmpresaId { get; set; }
    public int? UsuarioId => null;
    public int? ProfissionalId => null;
    public bool AdminGlobal => true;
    public string? Ip { get; init; }
    public bool TemPermissao(string codigo) => true;
}

/// <summary>
/// Tudo que as etapas compartilham: leitor do Firebird, contexto do destino (conexão aberta com
/// <c>@LC_MIGRACAO = 1</c>), dicionários <c>idAntigo → idNovo</c> por tabela do legado e o relatório.
/// </summary>
public sealed class ContextoMigracao(LeitorFirebird fb, LabDbContext db, EmpresaDaMigracao empresa, RelatorioMigracao relatorio, string senhaProvisoria)
{
    public const string EntidadeDaEmpresa = "ENTIDADE_DA_EMPRESA";

    private readonly Dictionary<string, Dictionary<int, int>> _mapas = new(StringComparer.OrdinalIgnoreCase);

    public LeitorFirebird Fb { get; } = fb;
    public LabDbContext Db { get; } = db;
    public EmpresaDaMigracao Empresa { get; } = empresa;
    public RelatorioMigracao Relatorio { get; } = relatorio;
    /// <summary>Senha provisória única dos usuários migrados (decisão do usuário); nunca vai para log ou relatório.</summary>
    public string SenhaProvisoria { get; } = senhaProvisoria;
    public string Etapa { get; set; } = "";
    public int IdEmpresa => Empresa.EmpresaId ?? throw new InvalidOperationException("A empresa ainda não foi migrada (M-01).");

    /// <summary>ID da entidade da empresa no legado (a 4) e o da entidade da empresa usuária no v2 (M-01).</summary>
    public (int Legado, int Nova) EntidadeDaEmpresaUsuaria
    {
        get { var p = Mapa(EntidadeDaEmpresa).Single(); return (p.Key, p.Value); }
    }

    /// <summary>
    /// Contato da entidade da empresa no legado também vai para a entidade da empresa usuária quando ela ganhou
    /// cadastro comercial à parte na M-05 (senão o próprio mapa já aponta para a entidade da empresa).
    /// </summary>
    public bool CopiarParaEmpresa(int? entidadeLegado) =>
        EntidadeDaEmpresaUsuaria is var (legado, nova) && entidadeLegado == legado && Mapa("ENTIDADES").GetValueOrDefault(legado) != nova;

    /// <summary>Dicionário <c>ID do legado → ID novo</c> da tabela do legado (criado vazio na 1ª vez).</summary>
    public Dictionary<int, int> Mapa(string tabelaLegado)
    {
        if (!_mapas.TryGetValue(tabelaLegado, out var m)) _mapas[tabelaLegado] = m = [];
        return m;
    }

    /// <summary>ID novo do registro do legado, ou null (com anomalia) se não foi migrado.</summary>
    public int? Novo(string tabelaLegado, int? idAntigo, string onde)
    {
        if (idAntigo is not { } id) return null;
        if (Mapa(tabelaLegado).TryGetValue(id, out var novo)) return novo;
        Anomalia($"{onde}: {tabelaLegado} {id} não existe no legado (referência ignorada).");
        return null;
    }

    public void Log(string texto) => Relatorio.Log($"[{Etapa}] {texto}");
    public void Anomalia(string texto) => Relatorio.Anomalia(Etapa, texto);
    public void Conferir(string item, object esperado, object encontrado) => Relatorio.Conferir(Etapa, item, esperado, encontrado);

    /// <summary>Texto cortado no tamanho da coluna do destino (pelo modelo do EF); corte vira anomalia.</summary>
    public string? Cortar<T>(string propriedade, string? valor, string onde)
    {
        if (valor is null) return null;
        var max = Db.Model.FindEntityType(typeof(T))!.FindProperty(propriedade)!.GetMaxLength();
        if (max is not { } m || valor.Length <= m) return valor;
        Anomalia($"{onde}: {propriedade} com {valor.Length} caracteres cortado em {m} (\"{valor}\").");
        return valor[..m];
    }

    /// <summary>
    /// Liga <c>@LC_MIGRACAO = 1</c> na conexão aberta do contexto (o interceptor zera ao abrir; numeração e
    /// validações das triggers ficam desligadas e os números do legado são preservados).
    /// </summary>
    public Task LigarModoMigracaoAsync(CancellationToken ct) => Db.Database.ExecuteSqlRawAsync("SET @LC_MIGRACAO = 1", ct);
}
