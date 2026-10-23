using LabControl.Models;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services.Migracao.Etapas;

/// <summary>
/// M-10 — produtos e parâmetros de análise (M-10-produtos-parametros.md). Produto mantém o código (a trigger só gera
/// quando vem vazio); a sequência PRODUTO continua do último código do legado (<c>SYS_TABELAS_ID</c>). Parâmetro sem
/// descrição reduzida usa a descrição (obrigatória no v2, T15); sem tipo de análise vai para um tipo "NÃO INFORMADO".
/// </summary>
public sealed class M10ProdutosParametros : IEtapaMigracao
{
    public string Codigo => "M-10";
    public string Nome => "Produtos e parâmetros de análise";

    public async Task<int> ExecutarAsync(ContextoMigracao c, CancellationToken ct)
    {
        var produtos = await c.Fb.LerAsync("SELECT ID_PRODUTOS, CODIGO, DESCRICAO, ID_UNIDADES, ID_EMBALAGENS_TIPOS, QTDE_EMBALAGEM, INATIVO FROM PRODUTOS ORDER BY ID_PRODUTOS", ct);
        var codigos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var novosProdutos = new List<(int Antigo, Produto Novo)>();
        foreach (var l in produtos)
        {
            var antigo = l.IntObrigatorio("ID_PRODUTOS");
            var onde = $"produto {antigo}";
            if (c.Novo("UNIDADES", l.Int("ID_UNIDADES"), onde) is not { } unidade) { c.Anomalia($"{onde}: sem unidade — não migrado."); continue; }
            var codigo = c.Unico<Produto>(nameof(Produto.Codigo), c.Cortar<Produto>(nameof(Produto.Codigo), l.Texto("CODIGO") ?? antigo.ToString("000000"), onde)!,
                codigos, x => x, n => n == 1 ? $"-{antigo}" : $"-{antigo}.{n}", onde);
            var qtde = l.Decimal("QTDE_EMBALAGEM") is { } q && q > 0 ? decimal.Round(q, 4) : (decimal?)null;
            var p = new Produto
            {
                Codigo = codigo, Descricao = c.Cortar<Produto>(nameof(Produto.Descricao), l.Texto("DESCRICAO") ?? "SEM DESCRIÇÃO", onde)!,
                IdUnidades = unidade, IdEmbalagensTipos = c.Novo("EMBALAGENS_TIPOS", l.Int("ID_EMBALAGENS_TIPOS"), onde),
                QtdeEmbalagem = qtde, Inativo = l.Flag("INATIVO"), IdLegado = antigo,
            };
            c.Db.Add(p);
            novosProdutos.Add((antigo, p));
        }
        await c.Db.SaveChangesAsync(ct);
        foreach (var (antigo, p) in novosProdutos) c.Mapa("PRODUTOS")[antigo] = p.IdProdutos;

        // próximo código automático continua do legado (sem trigger nem EF: é a sequência da numeração)
        var ultimo = (await c.Fb.LerAsync("SELECT ID FROM SYS_TABELAS_ID WHERE TABELA = 'PRODUTOS'", ct)).SingleOrDefault()?.Int("ID") ?? 0;
        await c.Db.Database.ExecuteSqlRawAsync("""
            INSERT INTO SEQUENCIAS_DOCUMENTOS (ID_EMPRESAS, TIPO_DOCUMENTO, ANO, ULTIMO_NUMERO) VALUES ({0}, 'PRODUTO', 0, {1})
            ON DUPLICATE KEY UPDATE ULTIMO_NUMERO = GREATEST(ULTIMO_NUMERO, VALUES(ULTIMO_NUMERO))
            """, [c.IdEmpresa, ultimo], ct);

        var parametros = await c.Fb.LerAsync("""
            SELECT ID_LAB_PARAMETROS_ANALISES, DESCRICAO, DESC_REDUZIDA, ID_ANALISES_TIPO, ID_ANALISES_METODOS, DESCR_METODO, VR_UNITARIO, AUDITADO
              FROM LAB_PARAMETROS_ANALISES ORDER BY ID_LAB_PARAMETROS_ANALISES
            """, ct);
        AnaliseTipo? naoInformado = null;
        var descricoes = new HashSet<string>();
        var novosParametros = new List<(int Antigo, LabParametroAnalise Novo)>();
        foreach (var l in parametros)
        {
            var antigo = l.IntObrigatorio("ID_LAB_PARAMETROS_ANALISES");
            var onde = $"parâmetro {antigo}";
            var descricao = c.Unico<LabParametroAnalise>(nameof(LabParametroAnalise.Descricao),
                c.Cortar<LabParametroAnalise>(nameof(LabParametroAnalise.Descricao), l.Texto("DESCRICAO") ?? $"PARÂMETRO {antigo}", onde)!,
                descricoes, Conversoes.NomeComparavel, n => $" ({n + 1})", onde);
            var reduzida = l.Texto("DESC_REDUZIDA");
            if (reduzida is null) c.Anomalia($"{onde}: sem descrição reduzida — usa a descrição (impressa no boletim).");
            var tipo = c.Novo("ANALISES_TIPO", l.Int("ID_ANALISES_TIPO"), onde);
            if (tipo is null)
            {
                naoInformado ??= await TipoNaoInformadoAsync(c, ct);
                tipo = naoInformado.IdAnalisesTipo;
                c.Anomalia($"{onde} \"{descricao}\": sem tipo de análise no legado — tipo \"NÃO INFORMADO\" (código {naoInformado.Codigo}).");
            }
            var p = new LabParametroAnalise
            {
                Descricao = descricao, DescReduzida = c.Cortar<LabParametroAnalise>(nameof(LabParametroAnalise.DescReduzida), reduzida ?? descricao, onde)!,
                IdAnalisesTipo = tipo.Value, IdAnalisesMetodos = c.Novo("ANALISES_METODOS", l.Int("ID_ANALISES_METODOS"), onde),
                DescrMetodo = c.Cortar<LabParametroAnalise>(nameof(LabParametroAnalise.DescrMetodo), l.Texto("DESCR_METODO"), onde),
                VrUnitario = decimal.Round(l.Decimal("VR_UNITARIO") ?? 0, 2), Auditado = l.Int("AUDITADO") == 1, IdLegado = antigo,
            };
            c.Db.Add(p);
            novosParametros.Add((antigo, p));
        }
        await c.Db.SaveChangesAsync(ct);
        foreach (var (antigo, p) in novosParametros) c.Mapa("LAB_PARAMETROS_ANALISES")[antigo] = p.IdLabParametrosAnalises;

        c.Conferir("produtos do legado", produtos.Count, c.Mapa("PRODUTOS").Count);
        c.Conferir("sequência de produtos = último código do legado", ultimo,
            await c.Db.SequenciasDocumentos.Where(s => s.TipoDocumento == "PRODUTO" && s.Ano == 0).Select(s => s.UltimoNumero).SingleAsync(ct));
        c.Conferir("parâmetros de análise do legado", parametros.Count, c.Mapa("LAB_PARAMETROS_ANALISES").Count);
        c.Conferir("parâmetros acreditados", await c.Fb.ContarAsync("SELECT COUNT(*) FROM LAB_PARAMETROS_ANALISES WHERE AUDITADO = 1", ct),
            await c.Db.LabParametrosAnalises.CountAsync(p => p.Auditado, ct));
        return novosProdutos.Count + novosParametros.Count;
    }

    // o já existente com esse nome, ou um novo inativo no maior código livre (código e descrição são UNIQUE e o M-03
    // ocupa os códigos com os IDs do legado)
    private static async Task<AnaliseTipo> TipoNaoInformadoAsync(ContextoMigracao c, CancellationToken ct)
    {
        const string nome = "NÃO INFORMADO";
        if (await c.Db.AnalisesTipo.FirstOrDefaultAsync(t => t.Descricao == nome, ct) is { } existente) return existente;
        var usados = (await c.Db.AnalisesTipo.Select(t => t.Codigo).ToListAsync(ct)).ToHashSet();
        var codigo = Enumerable.Range(1, 99).Reverse().Select(n => (short)n).FirstOrDefault(n => !usados.Contains(n));
        if (codigo == 0) throw new InvalidOperationException("Parâmetro sem tipo de análise e nenhum código de 1 a 99 livre para o tipo \"NÃO INFORMADO\".");
        var tipo = new AnaliseTipo { Codigo = codigo, Descricao = nome, Ativo = false };
        c.Db.Add(tipo);
        await c.Db.SaveChangesAsync(ct);
        return tipo;
    }
}
