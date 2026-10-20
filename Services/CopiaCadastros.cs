using System.Text.RegularExpressions;
using LabControl.Data;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services;

/// <summary>Grupo de cadastros oferecido na tela "Nova empresa" (T03).</summary>
public sealed record GrupoCopia(string Codigo, string Nome, IReadOnlyList<string> Tabelas);

/// <param name="Chaves">Chaves únicas da tabela sem ID_EMPRESAS; a primeira casa a linha de destino (remapear FKs).</param>
/// <param name="Fks">Colunas que apontam para outra tabela copiada: o ID é trocado pelo da empresa de destino.</param>
/// <param name="Pai">Tabela filha sem ID_EMPRESAS (ex.: parcelas): coluna e tabela do pai.</param>
public sealed record TabelaCopia(string Nome, string[][] Chaves, (string Coluna, string Tabela)[] Fks, (string Coluna, string Tabela)? Pai = null);

/// <summary>
/// Cópia de cadastros entre empresas (docs/processos/P23-nova-empresa.md), em SQL (INSERT … SELECT) dentro da
/// transação de quem chama. As colunas vêm do <c>information_schema</c> (sem PK, empresa e colunas de controle);
/// o que já existe no destino (mesma chave única — ex.: o que o SP_INICIALIZAR_EMPRESA criou) não é duplicado;
/// FKs para tabelas copiadas são remapeadas pela chave. <b>Nunca</b> copia entidades, propostas, amostras,
/// resultados, boletins nem sequências (a do produto só é ajustada para não repetir códigos copiados).
/// </summary>
public sealed partial class CopiaCadastros(LabDbContext db)
{
    private static readonly string[] Descricao = ["DESCRICAO"];

    /// <summary>Ordem = ordem de inclusão (quem é referenciado vem antes).</summary>
    public static readonly IReadOnlyList<TabelaCopia> Tabelas =
    [
        new("TIPOS_LOGRADOURO", [["ABREVIATURA"]], []),
        new("ENDERECOS_TIPOS", [Descricao], []),
        new("FONES_TIPOS", [Descricao], []),
        new("EMAILS_TIPOS", [Descricao], []),
        new("TIPOS_REG_PROFISSIONAL", [Descricao], []),
        new("CARGOS_FUNCIONARIOS", [Descricao], []),
        new("IDIOMAS", [Descricao], []),
        new("UNIDADES", [["SIGLA"]], []),
        new("EMBALAGENS_TIPOS", [Descricao], []),
        new("MOEDAS", [["SIGLA"]], []),
        new("AMOSTRAS_TIPO", [["CODIGO"], Descricao], []),
        new("AMOSTRAS_STATUS", [Descricao], []),
        new("ANALISES_TIPO", [["CODIGO"], Descricao], []),
        new("ANALISES_STATUS", [Descricao], []),
        new("BOLETINS_STATUS", [Descricao], []),
        new("LAB_PROPOSTAS_STATUS", [Descricao], []),
        new("ANALISES_METODOS", [Descricao], []), // sem UNIQUE no banco: casa pela descrição
        new("LAB_TIPOS_RESULTADOS", [Descricao], []),
        new("PRAZOS", [Descricao], []),
        new("CONDICOES_PAGTOS", [["CODIGO"]], []),
        new("CONDICOES_PAGTOS_PARC", [["NR_PARCELA"]], [], Pai: ("ID_CONDICOES_PAGTOS", "CONDICOES_PAGTOS")),
        new("LAB_PARAMETROS_ANALISES", [Descricao], [("ID_ANALISES_TIPO", "ANALISES_TIPO"), ("ID_ANALISES_METODOS", "ANALISES_METODOS")]),
        new("PRODUTOS", [["CODIGO"]], [("ID_UNIDADES", "UNIDADES"), ("ID_EMBALAGENS_TIPOS", "EMBALAGENS_TIPOS")]),
    ];

    public static readonly IReadOnlyList<GrupoCopia> Grupos =
    [
        new("AUXILIARES", "Cadastros auxiliares (tipos, status, unidades, embalagens, idiomas, moedas…)",
            ["TIPOS_LOGRADOURO", "ENDERECOS_TIPOS", "FONES_TIPOS", "EMAILS_TIPOS", "TIPOS_REG_PROFISSIONAL", "CARGOS_FUNCIONARIOS",
             "IDIOMAS", "UNIDADES", "EMBALAGENS_TIPOS", "MOEDAS", "AMOSTRAS_TIPO", "AMOSTRAS_STATUS", "ANALISES_TIPO",
             "ANALISES_STATUS", "BOLETINS_STATUS", "LAB_PROPOSTAS_STATUS"]),
        new("METODOS", "Métodos de análise", ["ANALISES_METODOS"]),
        new("PARAMETROS", "Parâmetros de análise (traz junto os tipos de análise e métodos usados)", ["LAB_PARAMETROS_ANALISES"]),
        new("PRODUTOS", "Produtos (traz junto as unidades e embalagens usadas)", ["PRODUTOS"]),
        new("CONDICOES", "Condições de pagamento", ["CONDICOES_PAGTOS", "CONDICOES_PAGTOS_PARC"]),
        new("PRAZOS", "Prazos", ["PRAZOS"]),
        new("TIPOS_RESULTADO", "Tipos de resultado", ["LAB_TIPOS_RESULTADOS"]),
    ];

    private static readonly HashSet<string> Controle = new(StringComparer.OrdinalIgnoreCase)
        { "ID_EMPRESAS", "CRIADO_EM", "ALTERADO_EM", "CRIADO_POR", "ALTERADO_POR" };

    /// <summary>Tabelas dos grupos + as que elas referenciam (parâmetros precisam dos tipos/métodos; produtos, das unidades…).</summary>
    public static IReadOnlyList<TabelaCopia> Resolver(IEnumerable<string> grupos)
    {
        var nomes = Grupos.Where(g => grupos.Contains(g.Codigo)).SelectMany(g => g.Tabelas).ToHashSet();
        bool mudou;
        do
        {
            mudou = false;
            foreach (var t in Tabelas.Where(t => nomes.Contains(t.Nome)).ToList())
                foreach (var dep in t.Fks.Select(f => f.Tabela).Append(t.Pai?.Tabela).OfType<string>())
                    mudou |= nomes.Add(dep);
        } while (mudou);
        return Tabelas.Where(t => nomes.Contains(t.Nome)).ToList();
    }

    /// <summary>Copia os grupos de <paramref name="origem"/> para <paramref name="destino"/>; devolve as linhas incluídas por tabela.</summary>
    public async Task<IReadOnlyDictionary<string, int>> CopiarAsync(int origem, int destino, IEnumerable<string> grupos, int? usuario)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("A cópia de cadastros roda dentro da transação da criação da empresa.");
        var resumo = new Dictionary<string, int>();
        foreach (var t in Resolver(grupos))
            resumo[t.Nome] = await db.Database.ExecuteSqlRawAsync(await MontarSqlAsync(t), origem, destino, usuario ?? (object)DBNull.Value);

        // produtos copiados mantêm o código: a sequência do destino não pode gerar um código que já existe
        if (resumo.GetValueOrDefault("PRODUTOS") > 0)
        {
            var maior = await db.Database.SqlQueryRaw<long>(
                "SELECT COALESCE(MAX(CAST(CODIGO AS UNSIGNED)), 0) AS Value FROM PRODUTOS WHERE ID_EMPRESAS = {0} AND CODIGO REGEXP '^[0-9]+$'",
                destino).SingleAsync();
            await db.Database.ExecuteSqlRawAsync("""
                INSERT INTO SEQUENCIAS_DOCUMENTOS (ID_EMPRESAS, TIPO_DOCUMENTO, ANO, ULTIMO_NUMERO) VALUES ({0}, 'PRODUTO', 0, {1})
                ON DUPLICATE KEY UPDATE ULTIMO_NUMERO = GREATEST(ULTIMO_NUMERO, {1})
                """, destino, maior);
        }
        return resumo;
    }

    /// <summary>INSERT … SELECT da tabela. Parâmetros: {0} = origem, {1} = destino, {2} = usuário (CRIADO_POR).</summary>
    public async Task<string> MontarSqlAsync(TabelaCopia t)
    {
        var colunas = await ColunasAsync(t.Nome);
        var pk = colunas.Single(c => c.Pk).Nome;
        var fks = t.Fks.ToDictionary(f => f.Coluna, f => f.Tabela, StringComparer.OrdinalIgnoreCase);
        var copiadas = colunas.Where(c => !c.Pk && !Controle.Contains(c.Nome) && !c.Gerada && c.Nome != t.Pai?.Coluna).Select(c => c.Nome).ToList();
        var temCriadoPor = colunas.Any(c => c.Nome.Equals("CRIADO_POR", StringComparison.OrdinalIgnoreCase));

        var destinoCols = new List<string>();
        var valores = new List<string>();
        var joins = new List<string>();
        string Igual(string a, string b, string[] chave) => string.Join(" AND ", chave.Select(k => $"{a}.`{k}` <=> {b}.`{k}`"));

        if (t.Pai is { } pai)
        {
            var tp = Tabelas.Single(x => x.Nome == pai.Tabela);
            var pkPai = (await ColunasAsync(pai.Tabela)).Single(c => c.Pk).Nome;
            joins.Add($"JOIN `{pai.Tabela}` op ON op.`{pkPai}` = o.`{pai.Coluna}` AND op.ID_EMPRESAS = {{0}}");
            joins.Add($"JOIN `{pai.Tabela}` dp ON dp.ID_EMPRESAS = {{1}} AND {Igual("dp", "op", tp.Chaves[0])}");
            destinoCols.Add(pai.Coluna);
            valores.Add($"dp.`{pkPai}`");
        }
        else
        {
            destinoCols.Add("ID_EMPRESAS");
            valores.Add("{1}");
        }

        var i = 0;
        foreach (var c in copiadas)
        {
            destinoCols.Add(c);
            if (fks.TryGetValue(c, out var ref_))
            {
                var tr = Tabelas.Single(x => x.Nome == ref_);
                var pkRef = (await ColunasAsync(ref_)).Single(x => x.Pk).Nome;
                i++;
                joins.Add($"LEFT JOIN `{ref_}` o{i} ON o{i}.`{pkRef}` = o.`{c}`");
                joins.Add($"LEFT JOIN `{ref_}` d{i} ON d{i}.ID_EMPRESAS = {{1}} AND {Igual($"d{i}", $"o{i}", tr.Chaves[0])}");
                valores.Add($"d{i}.`{pkRef}`");
            }
            else valores.Add($"o.`{c}`");
        }
        if (temCriadoPor)
        {
            destinoCols.Add("CRIADO_POR");
            valores.Add("{2}");
        }

        var onde = t.Pai is { } p2
            ? $"NOT EXISTS (SELECT 1 FROM `{t.Nome}` x WHERE x.`{p2.Coluna}` = dp.`{(await ColunasAsync(p2.Tabela)).Single(c => c.Pk).Nome}` AND {Igual("x", "o", t.Chaves[0])})"
            : $"o.ID_EMPRESAS = {{0}} AND NOT EXISTS (SELECT 1 FROM `{t.Nome}` x WHERE x.ID_EMPRESAS = {{1}} AND (" +
              string.Join(" OR ", t.Chaves.Select(k => "(" + Igual("x", "o", k) + ")")) + "))";

        return $"INSERT INTO `{t.Nome}` ({string.Join(", ", destinoCols.Select(c => $"`{c}`"))})\n" +
               $"SELECT {string.Join(", ", valores)}\n  FROM `{t.Nome}` o\n  {string.Join("\n  ", joins)}\n WHERE {onde}";
    }

    internal sealed record Coluna(string Nome, bool Pk, bool Gerada);

    private readonly Dictionary<string, IReadOnlyList<Coluna>> _cache = new(StringComparer.OrdinalIgnoreCase);

    internal async Task<IReadOnlyList<Coluna>> ColunasAsync(string tabela)
    {
        if (_cache.TryGetValue(tabela, out var c)) return c;
        var lidas = await db.Database.SqlQueryRaw<ColunaLida>("""
            SELECT COLUMN_NAME AS Nome, COLUMN_KEY AS Chave, EXTRA AS Extra FROM information_schema.COLUMNS
             WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = {0} ORDER BY ORDINAL_POSITION
            """, tabela).ToListAsync();
        if (lidas.Count == 0 || lidas.Any(x => !NomeSeguro().IsMatch(x.Nome)))
            throw new InvalidOperationException($"Tabela {tabela} sem colunas válidas no information_schema.");
        return _cache[tabela] = lidas.Select(x => new Coluna(x.Nome, x.Chave == "PRI",
            x.Extra.Contains("GENERATED", StringComparison.OrdinalIgnoreCase))).ToList();
    }

    private sealed record ColunaLida(string Nome, string Chave, string Extra);

    [GeneratedRegex("^[A-Za-z0-9_]+$")]
    private static partial Regex NomeSeguro();
}
