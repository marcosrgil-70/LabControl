using System.Text;
using LabControl.Data;
using LabControl.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MySqlConnector;

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
    private readonly Dictionary<string, object> _compartilhados = [];

    public LeitorFirebird Fb { get; } = fb;
    public LabDbContext Db { get; } = db;
    public EmpresaDaMigracao Empresa { get; } = empresa;
    public RelatorioMigracao Relatorio { get; } = relatorio;
    /// <summary>Senha provisória única dos usuários migrados (decisão do usuário); nunca vai para log ou relatório.</summary>
    public string SenhaProvisoria { get; } = senhaProvisoria;
    /// <summary>Desenvolvimento/testes ou banco do instalador substituído: o ADMINISTRADOR migrado fica com <see cref="ContaService.SenhaDesenvolvimento"/>, sem troca (M-09).</summary>
    public bool AdministradorDesenvolvimento { get; init; }
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

    /// <summary>Dado que uma etapa monta e as seguintes usam (ex.: itens da proposta por (proposta, item) do legado).</summary>
    public T Compartilhado<T>(string chave) where T : new()
    {
        if (!_compartilhados.TryGetValue(chave, out var v)) _compartilhados[chave] = v = new T();
        return (T)v;
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
    /// Valor único entre os já <paramref name="usados"/> (comparados pela <paramref name="chave"/>): repetido ganha
    /// <c>sufixo(1)</c>, <c>sufixo(2)</c>… até não repetir, com a base cortada para caber na coluna; vira anomalia.
    /// </summary>
    public string Unico<T>(string propriedade, string valor, HashSet<string> usados, Func<string, string> chave, Func<int, string> sufixo, string onde)
    {
        if (usados.Add(chave(valor))) return valor;
        var max = Db.Model.FindEntityType(typeof(T))!.FindProperty(propriedade)!.GetMaxLength() ?? int.MaxValue;
        for (var n = 1; ; n++)
        {
            var s = sufixo(n);
            var novo = (valor.Length + s.Length <= max ? valor : valor[..Math.Max(0, max - s.Length)]) + s;
            if (!usados.Add(chave(novo))) continue;
            Anomalia($"{onde}: {propriedade} \"{valor}\" repetido no legado — gravado como \"{novo}\".");
            return novo;
        }
    }

    /// <summary>
    /// INSERT direto em lote (parte 2): o EF não envia as colunas que o banco preenche fora da migração (ano/código/nº
    /// de proposta, amostra e pedido; valores das análises; datas de movimento e de log), e na migração elas vêm do
    /// legado. Devolve os IDs gerados na ordem das linhas: dentro de um INSERT de várias linhas o InnoDB numera em
    /// sequência; o destino é exclusivo da migração, e o lote só é aceito se o maior ID da tabela fechar a conta.
    /// </summary>
    public async Task<List<int>> InserirEmLoteAsync(string tabela, string chave, IReadOnlyList<string> colunas,
        IReadOnlyList<object?[]> linhas, CancellationToken ct)
    {
        var ids = new List<int>(linhas.Count);
        var con = (MySqlConnection)Db.Database.GetDbConnection();
        var tx = (MySqlTransaction?)Db.Database.CurrentTransaction?.GetDbTransaction();
        var porLote = Math.Max(1, 30_000 / colunas.Count);
        for (var inicio = 0; inicio < linhas.Count; inicio += porLote)
        {
            var lote = linhas.Skip(inicio).Take(porLote).ToList();
            await using var cmd = con.CreateCommand();
            cmd.Transaction = tx;
            var sql = new StringBuilder($"INSERT INTO {tabela} ({string.Join(", ", colunas)}) VALUES ");
            for (var r = 0; r < lote.Count; r++)
            {
                if (lote[r].Length != colunas.Count) throw new InvalidOperationException($"{tabela}: linha com {lote[r].Length} valores para {colunas.Count} colunas.");
                sql.Append(r == 0 ? "(" : ",(");
                for (var k = 0; k < colunas.Count; k++)
                {
                    var nome = $"@p{r}_{k}";
                    sql.Append(k == 0 ? nome : "," + nome);
                    cmd.Parameters.AddWithValue(nome, lote[r][k] ?? DBNull.Value);
                }
                sql.Append(')');
            }
            cmd.CommandText = sql.ToString();
            await cmd.ExecuteNonQueryAsync(ct);
            var primeiro = (int)cmd.LastInsertedId;
            cmd.Parameters.Clear();
            cmd.CommandText = $"SELECT MAX({chave}) FROM {tabela}";
            var maior = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
            if (maior != primeiro + lote.Count - 1)
                throw new InvalidOperationException($"{tabela}: IDs gerados fora de sequência ({primeiro}…{maior} para {lote.Count} linhas).");
            ids.AddRange(Enumerable.Range(primeiro, lote.Count));
        }
        return ids;
    }

    /// <summary>
    /// Liga <c>@LC_MIGRACAO = 1</c> na conexão aberta do contexto (o interceptor zera ao abrir; numeração e
    /// validações das triggers ficam desligadas e os números do legado são preservados).
    /// </summary>
    public Task LigarModoMigracaoAsync(CancellationToken ct) => Db.Database.ExecuteSqlRawAsync("SET @LC_MIGRACAO = 1", ct);
}
