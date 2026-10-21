using System.Text;
using System.Text.RegularExpressions;
using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services.Geografia;

/// <summary>
/// T11-01…03 — países, estados e cidades. Tabelas <b>globais</b> (sem empresa): todos consultam, só o ADMIN_GLOBAL
/// grava (conferido no controller por <see cref="SomenteAdminGlobalAttribute"/>). Nomes em maiúsculas, como o seed
/// IBGE e o legado. Inclui a importação do CSV de municípios do IBGE (código; nome; UF), idempotente pelo código.
/// </summary>
public sealed partial class GeografiaService(LabDbContext db, IAuditoria auditoria)
{
    public const string Paises = "Paises", Estados = "Estados", Cidades = "Cidades";
    private const string CodigoBacenBrasil = "1058";

    // ------------------------------------------------------------- listas

    public async Task<GeoListaVM> ListarAsync(string tela, GeoFiltroVM f)
    {
        var busca = f.Busca?.Trim();
        IQueryable<GeoLinhaVM> q = tela switch
        {
            Paises => db.Paises.AsNoTracking()
                .Where(p => busca == null || p.Nome.Contains(busca) || p.Sigla!.Contains(busca))
                .OrderBy(p => p.Nome)
                .Select(p => new GeoLinhaVM(p.IdPaises, null, null, p.Nome, p.Sigla, p.CodigoBacen, db.Estados.Count(e => e.IdPaises == p.IdPaises))),
            Estados => db.Estados.AsNoTracking()
                .Where(e => (f.IdPais == null || e.IdPaises == f.IdPais) && (busca == null || e.Nome.Contains(busca) || e.Sigla.Contains(busca)))
                .OrderBy(e => e.Nome)
                .Select(e => new GeoLinhaVM(e.IdEstados, e.IdPaises, e.Pais.Nome, e.Nome, e.Sigla, e.CodigoIbge, db.Cidades.Count(c => c.IdEstados == e.IdEstados))),
            Cidades => db.Cidades.AsNoTracking()
                .Where(c => (f.IdEstado == null || c.IdEstados == f.IdEstado) && (f.IdPais == null || c.Estado.IdPaises == f.IdPais)
                            && (busca == null || c.Nome.Contains(busca) || c.CodigoIbge == busca))
                .OrderBy(c => c.Nome).ThenBy(c => c.Estado.Sigla)
                // uso em endereços não aparece na lista: seria contar cadastros de outras empresas
                .Select(c => new GeoLinhaVM(c.IdCidades, c.IdEstados, c.Estado.Sigla, c.Nome, null, c.CodigoIbge, 0)),
            _ => throw new ArgumentOutOfRangeException(nameof(tela)),
        };

        var total = await q.CountAsync();
        f.Pagina = Paginacao.Ajustar(f.Pagina, total, GeoFiltroVM.TamanhoPagina);
        var itens = await q.Skip((f.Pagina - 1) * GeoFiltroVM.TamanhoPagina).Take(GeoFiltroVM.TamanhoPagina).ToListAsync();

        return new GeoListaVM
        {
            Tela = tela, Filtro = f, Itens = itens, Total = total,
            Paises = tela == Paises ? [] : await db.Paises.AsNoTracking().OrderBy(p => p.Nome).Select(p => new OpcaoVM(p.IdPaises, p.Nome)).ToListAsync(),
            Estados = tela == Cidades
                ? await db.Estados.AsNoTracking().Where(e => f.IdPais == null || e.IdPaises == f.IdPais)
                    .OrderBy(e => e.Sigla).Select(e => new OpcaoVM(e.IdEstados, e.Sigla + " — " + e.Nome)).ToListAsync()
                : [],
        };
    }

    /// <summary>Combo de cidades da UF (componente de endereço, P25).</summary>
    public Task<List<OpcaoVM>> CidadesDaUfAsync(int idEstado) => db.CidadesDaUfAsync(idEstado);

    // ------------------------------------------------------------- gravar

    /// <summary>Inclui (Id = 0) ou altera; o erro traz no Campo a propriedade do formulário (Nome, Sigla, Codigo, IdPai).</summary>
    public Task<Resultado<int>> GravarAsync(string tela, GeoFormVM vm) => tela switch
    {
        Paises => GravarPaisAsync(vm),
        Estados => GravarEstadoAsync(vm),
        Cidades => GravarCidadeAsync(vm),
        _ => throw new ArgumentOutOfRangeException(nameof(tela)),
    };

    private async Task<Resultado<int>> GravarPaisAsync(GeoFormVM vm)
    {
        if (Texto(vm.Nome, "nome", 80, out var nome) is { } e1) return e1;
        var sigla = Textos.Maiusculas(vm.Sigla);
        if (sigla?.Length > 5) return Resultado.Erro("Sigla: no máximo 5 caracteres.", "Sigla");
        var codigo = Textos.Limpo(vm.Codigo);
        if (codigo is not null && !RegexDigitos().IsMatch(codigo) || codigo?.Length > 10)
            return Resultado.Erro("Código BACEN: até 10 dígitos.", "Codigo");

        var p = vm.Id == 0 ? db.Paises.Add(new Pais()).Entity : await db.Paises.FindAsync(vm.Id);
        if (p is null) return Resultado.Erro("O país não foi encontrado (pode ter sido excluído).");
        (p.Nome, p.Sigla, p.CodigoBacen) = (nome!, sigla, codigo);
        return await SalvarAsync(() => p.IdPaises, "Nome");
    }

    private async Task<Resultado<int>> GravarEstadoAsync(GeoFormVM vm)
    {
        if (vm.IdPai is not { } idPais || !await db.Paises.AnyAsync(p => p.IdPaises == idPais)) return Resultado.Erro("Escolha o país.", "IdPai");
        if (Texto(vm.Nome, "nome", 100, out var nome) is { } e1) return e1;
        var sigla = Textos.Maiusculas(vm.Sigla);
        if (sigla is null || !RegexUf().IsMatch(sigla)) return Resultado.Erro("Sigla: 2 letras (ex.: SP).", "Sigla");
        var codigo = Textos.Limpo(vm.Codigo);
        if (codigo is not null && !RegexIbgeUf().IsMatch(codigo)) return Resultado.Erro("Código IBGE: 2 dígitos (ex.: 35).", "Codigo");

        var e = vm.Id == 0 ? db.Estados.Add(new Estado()).Entity : await db.Estados.FindAsync(vm.Id);
        if (e is null) return Resultado.Erro("O estado não foi encontrado (pode ter sido excluído).");
        (e.IdPaises, e.Nome, e.Sigla, e.CodigoIbge) = (idPais, nome!, sigla, codigo);
        return await SalvarAsync(() => e.IdEstados, "Sigla");
    }

    private async Task<Resultado<int>> GravarCidadeAsync(GeoFormVM vm)
    {
        var uf = vm.IdPai is { } idEstado ? await db.Estados.AsNoTracking().SingleOrDefaultAsync(e => e.IdEstados == idEstado) : null;
        if (uf is null) return Resultado.Erro("Escolha o estado.", "IdPai");
        if (Texto(vm.Nome, "nome", 150, out var nome) is { } e1) return e1;
        var codigo = Textos.Limpo(vm.Codigo);
        if (codigo is not null && !RegexIbgeCidade().IsMatch(codigo)) return Resultado.Erro("Código IBGE: 7 dígitos (ex.: 3550308).", "Codigo");
        // o código do município começa pelo da UF: evita cidade de SP com código de MG (a busca de CEP casa pelo código)
        if (codigo is not null && uf.CodigoIbge is { } cuf && !codigo.StartsWith(cuf, StringComparison.Ordinal))
            return Resultado.Erro($"Código IBGE: as cidades de {uf.Sigla} começam por {cuf}.", "Codigo");

        var c = vm.Id == 0 ? db.Cidades.Add(new Cidade()).Entity : await db.Cidades.FindAsync(vm.Id);
        if (c is null) return Resultado.Erro("A cidade não foi encontrada (pode ter sido excluída).");
        (c.IdEstados, c.Nome, c.CodigoIbge) = (uf.IdEstados, nome!, codigo);
        return await SalvarAsync(() => c.IdCidades, "Codigo");
    }

    private async Task<Resultado<int>> SalvarAsync(Func<int> id, string campoDuplicado)
    {
        var r = await ErrosMySql.CapturarAsync(() => db.SaveChangesAsync());
        if (r.Sucesso) return Resultado.Ok(id());
        db.ChangeTracker.Clear();
        return Resultado.Erro(r.Mensagem!, r.Tipo == TipoFalha.Duplicado ? campoDuplicado : null, r.Tipo);
    }

    // ------------------------------------------------------------- excluir

    /// <summary>Só sem filhos: país sem estados, estado sem cidades, cidade sem endereço (de qualquer empresa).</summary>
    public async Task<Resultado> ExcluirAsync(string tela, int id)
    {
        var emUso = tela switch
        {
            Paises => await db.Estados.AnyAsync(e => e.IdPaises == id) ? "O país tem estados cadastrados: não pode ser excluído." : null,
            Estados => await db.Cidades.AnyAsync(c => c.IdEstados == id) ? "O estado tem cidades cadastradas: não pode ser excluído." : null,
            // tabela global: o uso conta em todas as empresas (o filtro da empresa ativa esconderia endereços das outras)
            Cidades => await db.EntidadesEnderecos.IgnoreQueryFilters().AnyAsync(e => e.IdCidades == id)
                ? "A cidade é usada em endereços de cadastros: não pode ser excluída." : null,
            _ => throw new ArgumentOutOfRangeException(nameof(tela)),
        };
        if (emUso is not null) return Resultado.Erro(emUso);

        var r = await ErrosMySql.CapturarAsync(() => tela switch
        {
            Paises => db.Paises.Where(p => p.IdPaises == id).ExecuteDeleteAsync(),
            Estados => db.Estados.Where(e => e.IdEstados == id).ExecuteDeleteAsync(),
            _ => db.Cidades.Where(c => c.IdCidades == id).ExecuteDeleteAsync(),
        });
        if (!r.Sucesso) return r.Falha;
        return r.Valor == 0 ? Resultado.Erro("Registro não encontrado (pode ter sido excluído).") : Resultado.Ok();
    }

    // ------------------------------------------------------------- importação IBGE

    /// <summary>
    /// CSV do IBGE: <c>código;nome;UF</c> (ou vírgula/tab; cabeçalho opcional; UTF-8 ou Latin-1). Inclui os códigos que
    /// faltam e atualiza nome/UF pelo código; a UF vem do prefixo do código (conferida com a coluna UF, se houver).
    /// Linhas com erro são listadas e não impedem as demais. Tudo numa transação, com um evento de auditoria.
    /// </summary>
    public async Task<Resultado<ImportacaoIbgeVM>> ImportarIbgeAsync(Stream arquivo)
    {
        using var ms = new MemoryStream();
        await arquivo.CopyToAsync(ms);
        var texto = Decodificar(ms.ToArray());
        var linhas = texto.Split('\n').Select(l => l.TrimEnd('\r')).ToList();

        // IBGE é do Brasil; o código da UF não é único no banco (outro país pode repetir "35")
        var ufs = (await db.Estados.AsNoTracking().Where(e => e.CodigoIbge != null && e.Pais.CodigoBacen == CodigoBacenBrasil).ToListAsync())
            .GroupBy(e => e.CodigoIbge!).ToDictionary(g => g.Key, g => g.OrderBy(e => e.IdEstados).First());
        var existentes = await db.Cidades.Where(c => c.CodigoIbge != null).ToDictionaryAsync(c => c.CodigoIbge!);
        var vistos = new HashSet<string>();
        var erros = new List<string>();
        int lidas = 0, incluidas = 0, atualizadas = 0, iguais = 0;

        for (var i = 0; i < linhas.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(linhas[i])) continue;
            var sep = linhas[i].Contains(';') ? ';' : linhas[i].Contains('\t') ? '\t' : ',';
            var campos = linhas[i].Split(sep).Select(c => c.Trim().Trim('"').Trim()).ToArray();
            var codigo = campos[0];
            if (!RegexIbgeCidade().IsMatch(codigo))
            {
                if (lidas == 0 && erros.Count == 0) continue; // cabeçalho
                Erro($"Linha {i + 1}: código IBGE \"{codigo}\" inválido (7 dígitos).");
                continue;
            }
            lidas++;
            var nome = campos.Length > 1 ? campos[1].ToUpperInvariant() : "";
            if (nome.Length == 0 || nome.Length > 150) { Erro($"Linha {i + 1}: nome da cidade vazio ou com mais de 150 caracteres."); continue; }
            if (!ufs.TryGetValue(codigo[..2], out var uf)) { Erro($"Linha {i + 1}: nenhum estado com o código IBGE {codigo[..2]}."); continue; }
            if (campos.Length > 2 && campos[2].Length > 0 && !campos[2].Equals(uf.Sigla, StringComparison.OrdinalIgnoreCase))
            { Erro($"Linha {i + 1}: UF {campos[2]} não confere com o código {codigo} ({uf.Sigla})."); continue; }
            if (!vistos.Add(codigo)) { Erro($"Linha {i + 1}: código {codigo} repetido no arquivo."); continue; }

            if (!existentes.TryGetValue(codigo, out var cidade))
            {
                db.Cidades.Add(new Cidade { CodigoIbge = codigo, Nome = nome, IdEstados = uf.IdEstados });
                incluidas++;
            }
            else if (cidade.Nome != nome || cidade.IdEstados != uf.IdEstados)
            {
                (cidade.Nome, cidade.IdEstados) = (nome, uf.IdEstados);
                atualizadas++;
            }
            else iguais++;
        }
        if (lidas == 0) return Resultado.Erro("Nenhum município encontrado no arquivo (esperado: código IBGE; nome; UF por linha).");

        var resumo = new ImportacaoIbgeVM(lidas, incluidas, atualizadas, iguais, erros);
        var r = await ErrosMySql.CapturarAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            await db.SaveChangesAsync();
            await auditoria.RegistrarAsync(AcoesAuditoria.Alteracao, "CIDADES", "IMPORTACAO_IBGE", null,
                new { resumo.Lidas, resumo.Incluidas, resumo.Atualizadas, resumo.Iguais, Erros = erros.Count });
            await tx.CommitAsync();
        });
        if (!r.Sucesso)
        {
            db.ChangeTracker.Clear();
            return r.Falha;
        }
        return Resultado.Ok(resumo);

        void Erro(string m)
        {
            if (erros.Count < ImportacaoIbgeVM.MaximoErros) erros.Add(m);
            else if (erros.Count == ImportacaoIbgeVM.MaximoErros) erros.Add("… (demais erros omitidos)");
        }
    }

    // UTF-8 válido (com ou sem BOM); senão Latin-1, a codificação dos CSV do Excel em português
    private static string Decodificar(byte[] bytes)
    {
        try { return new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes).TrimStart('﻿'); }
        catch (DecoderFallbackException) { return Encoding.Latin1.GetString(bytes); }
    }

    // ------------------------------------------------------------- apoio


    /// <summary>Texto obrigatório em maiúsculas; devolve o erro (ou null) e o valor.</summary>
    private static Falha? Texto(string? valor, string rotulo, int maximo, out string? texto)
    {
        texto = Textos.Maiusculas(valor);
        if (texto is null) return Resultado.Erro($"Informe o {rotulo}.", "Nome");
        if (texto.Length > maximo) return Resultado.Erro($"{char.ToUpperInvariant(rotulo[0])}{rotulo[1..]}: no máximo {maximo} caracteres.", "Nome");
        return null;
    }

    [GeneratedRegex("^[0-9]+$")] private static partial Regex RegexDigitos();
    [GeneratedRegex("^[A-Z]{2}$")] private static partial Regex RegexUf();
    [GeneratedRegex("^[0-9]{2}$")] private static partial Regex RegexIbgeUf();
    [GeneratedRegex("^[0-9]{7}$")] private static partial Regex RegexIbgeCidade();
}
