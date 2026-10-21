using System.Globalization;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LabControl.Services.Auxiliares;

/// <summary>
/// CRUD genérico dos cadastros auxiliares (T10-00). Lê e grava pelas propriedades declaradas na
/// <see cref="AuxiliarDefinicao{T}"/>; o filtro global limita tudo à empresa ativa e o SaveChanges grava
/// ID_EMPRESAS. Duplicidade (1062) e uso (1451) viram mensagem pelo <see cref="ErrosMySql"/>.
/// </summary>
public sealed partial class AuxiliarService(LabDbContext db)
{
    private const string Ativo = "Ativo";

    public async Task<AuxiliarListaVM> ListarAsync<T>(AuxiliarDefinicao<T> def, AuxiliarFiltroVM filtro) where T : class, IPorEmpresa, new()
    {
        var q = db.Set<T>().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filtro.Busca)) q = q.Where(Busca<T>(def, filtro.Busca.Trim()));
        var ocultos = filtro.Inativos ? 0 : await q.CountAsync(e => !EF.Property<bool>(e, Ativo));
        if (!filtro.Inativos) q = q.Where(e => EF.Property<bool>(e, Ativo));

        var total = await q.CountAsync();
        filtro.Pagina = Math.Clamp(filtro.Pagina, 1, Math.Max(1, (total + AuxiliarFiltroVM.TamanhoPagina - 1) / AuxiliarFiltroVM.TamanhoPagina));
        var entidades = await q.OrderBy(e => EF.Property<string>(e, def.CampoTexto))
            .Skip((filtro.Pagina - 1) * AuxiliarFiltroVM.TamanhoPagina).Take(AuxiliarFiltroVM.TamanhoPagina).ToListAsync();

        var tipo = Tipo<T>();
        var colunas = def.Campos.Where(c => c.NaLista).ToList();
        return new AuxiliarListaVM
        {
            Definicao = def, Filtro = filtro, Colunas = colunas, Total = total, InativosOcultos = ocultos,
            Itens = entidades.Select(e => new AuxiliarLinhaVM(Id(tipo, e), (bool)Valor(tipo, e, Ativo)!,
                colunas.Select(c => Exibir(c, tipo.FindProperty(c.Propriedade)!, Valor(tipo, e, c.Propriedade))).ToList())).ToList(),
        };
    }

    /// <summary>Formulário vazio (inclusão) ou do registro da empresa ativa (null se não existir).</summary>
    public async Task<AuxiliarFormVM?> CarregarAsync<T>(AuxiliarDefinicao<T> def, int id) where T : class, IPorEmpresa, new()
    {
        var vm = new AuxiliarFormVM();
        if (id != 0)
        {
            var e = await PorIdAsync(def, id, rastrear: false);
            if (e is null) return null;
            var tipo = Tipo<T>();
            vm.Id = id;
            vm.Ativo = (bool)Valor(tipo, e, Ativo)!;
            foreach (var c in def.Campos) vm.Valores[c.Propriedade] = ParaFormulario(c, Valor(tipo, e, c.Propriedade));
        }
        await PrepararAsync(def, vm);
        return vm;
    }

    /// <summary>Campos resolvidos para a view (usado também ao reexibir o formulário com erro).</summary>
    public async Task PrepararAsync<T>(AuxiliarDefinicao<T> def, AuxiliarFormVM vm) where T : class, IPorEmpresa, new()
    {
        var tipo = Tipo<T>();
        T? atual = vm.Id != 0 && def.PodeAlterarCampo is not null ? await PorIdAsync(def, vm.Id, rastrear: false) : null;
        var campos = new List<CampoFormVM>();
        foreach (var c in def.Campos)
        {
            var p = tipo.FindProperty(c.Propriedade)!;
            var bloqueado = atual is not null && !await def.PodeAlterarCampo!(db, atual, c.Propriedade);
            var opcoes = c.Tipo == TipoCampo.Opcoes ? OpcoesDoEnum(p.ClrType) : [];
            campos.Add(new CampoFormVM(c.Propriedade, c.Rotulo, c.Tipo, p.GetMaxLength(), Obrigatorio(c, p),
                c.Ajuda, c.Minimo, c.Maximo, c.Maiusculas, opcoes, bloqueado));
        }
        (vm.Tabela, vm.Singular, vm.Campos) = (def.Tabela, def.Singular, campos);
    }

    /// <summary>Inclui (Id = 0) ou altera. Devolve o ID; o erro traz no Campo a propriedade, quando for de um campo.</summary>
    public async Task<Resultado<int>> GravarAsync<T>(AuxiliarDefinicao<T> def, AuxiliarFormVM vm) where T : class, IPorEmpresa, new()
    {
        var tipo = Tipo<T>();
        var entidade = vm.Id == 0 ? new T() : await PorIdAsync(def, vm.Id, rastrear: true);
        if (entidade is null) return Resultado.Erro(def.NaoEncontrado);
        var entrada = db.Entry(entidade);

        foreach (var c in def.Campos)
        {
            var p = tipo.FindProperty(c.Propriedade)!;
            var convertido = Converter(c, p, vm.Valores.GetValueOrDefault(c.Propriedade));
            if (!convertido.Sucesso) return Resultado.Erro(convertido.Mensagem!, c.Propriedade);
            if (vm.Id != 0 && def.PodeAlterarCampo is not null && !Equals(entrada.Property(p.Name).CurrentValue, convertido.Valor)
                && !await def.PodeAlterarCampo(db, entidade, c.Propriedade))
                return Resultado.Erro($"{c.Rotulo}: não pode ser alterado (já está em uso).", c.Propriedade);
            entrada.Property(p.Name).CurrentValue = convertido.Valor;
        }
        entrada.Property(Ativo).CurrentValue = vm.Ativo;

        if (def.ValidarAntesDeGravar is not null && await def.ValidarAntesDeGravar(db, entidade) is { Sucesso: false } invalido)
            return Resultado.Erro(invalido.Mensagem!, invalido.Campo);

        if (vm.Id == 0) db.Add(entidade);
        var gravou = await ErrosMySql.CapturarAsync(() => db.SaveChangesAsync());
        if (!gravou.Sucesso)
        {
            db.ChangeTracker.Clear();
            return Resultado.Erro(gravou.Mensagem!, gravou.Mensagem!.StartsWith("Já existe") ? CampoDuplicado(def, gravou.Mensagem) : null);
        }
        return Resultado.Ok(Id(tipo, entidade));
    }

    // "Já existe uma unidade com esta sigla." → Sigla; sem campo citado na mensagem, o 1º do formulário
    private static string CampoDuplicado(AuxiliarDefinicao def, string mensagem) =>
        (def.Campos.FirstOrDefault(c => mensagem.Contains(c.Rotulo, StringComparison.OrdinalIgnoreCase)) ?? def.Campos[0]).Propriedade;

    /// <summary>
    /// Exclui só sem uso: com uso o banco recusa (1451) e a mensagem sugere inativar. DELETE direto (o filtro global
    /// limita à empresa ativa): quem decide o uso é a FK do banco, não o que o contexto tiver carregado.
    /// </summary>
    public async Task<Resultado> ExcluirAsync<T>(AuxiliarDefinicao<T> def, int id) where T : class, IPorEmpresa, new()
    {
        var chave = Tipo<T>().FindPrimaryKey()!.Properties[0].Name;
        if (def.ValidarAntesDeExcluir is not null && await PorIdAsync(def, id, rastrear: false) is { } atual
            && await def.ValidarAntesDeExcluir(db, atual) is { Sucesso: false } invalido)
            return invalido;
        var r = await ErrosMySql.CapturarAsync(() => db.Set<T>().Where(e => EF.Property<int>(e, chave) == id).ExecuteDeleteAsync());
        if (!r.Sucesso) return Resultado.Erro(r.Mensagem!);
        return r.Valor == 0 ? Resultado.Erro(def.NaoEncontrado) : Resultado.Ok();
    }

    public async Task<Resultado> DefinirAtivoAsync<T>(AuxiliarDefinicao<T> def, int id, bool ativo) where T : class, IPorEmpresa, new()
    {
        var entidade = await PorIdAsync(def, id, rastrear: true);
        if (entidade is null) return Resultado.Erro(def.NaoEncontrado);
        db.Entry(entidade).Property(Ativo).CurrentValue = ativo;
        if (def.ValidarAntesDeGravar is not null && await def.ValidarAntesDeGravar(db, entidade) is { Sucesso: false } invalido)
            return invalido;
        return await ErrosMySql.CapturarAsync(() => (Task)db.SaveChangesAsync());
    }

    /// <summary>
    /// Combo das telas de inclusão: só ativos, mais o registro já escolhido (mesmo inativo) para os registros antigos
    /// continuarem mostrando o valor.
    /// </summary>
    public async Task<IReadOnlyList<OpcaoVM>> OpcoesAsync<T>(AuxiliarDefinicao<T> def, int? incluirId) where T : class, IPorEmpresa, new()
    {
        var chave = Tipo<T>().FindPrimaryKey()!.Properties[0].Name;
        var atual = incluirId ?? 0;
        return await db.Set<T>().AsNoTracking()
            .Where(e => EF.Property<bool>(e, Ativo) || EF.Property<int>(e, chave) == atual)
            .OrderBy(e => EF.Property<string>(e, def.CampoTexto))
            .Select(e => new OpcaoVM(EF.Property<int>(e, chave), EF.Property<string>(e, def.CampoTexto)))
            .ToListAsync();
    }

    // ---------------------------------------------------------------------

    private IEntityType Tipo<T>() => db.Model.FindEntityType(typeof(T))!;

    private static object? Valor(IEntityType tipo, object entidade, string propriedade) =>
        tipo.FindProperty(propriedade)!.GetGetter().GetClrValue(entidade);

    private static int Id(IEntityType tipo, object entidade) =>
        Convert.ToInt32(tipo.FindPrimaryKey()!.Properties[0].GetGetter().GetClrValue(entidade), CultureInfo.InvariantCulture);

    /// <summary>Registro da empresa ativa pelo ID (o filtro global esconde as outras empresas).</summary>
    private Task<T?> PorIdAsync<T>(AuxiliarDefinicao<T> def, int id, bool rastrear) where T : class, IPorEmpresa, new()
    {
        var chave = Tipo<T>().FindPrimaryKey()!.Properties[0].Name;
        var q = rastrear ? db.Set<T>() : db.Set<T>().AsNoTracking();
        return q.SingleOrDefaultAsync(e => EF.Property<int>(e, chave) == id);
    }

    /// <summary><c>e =&gt; e.A.Contains(busca) || e.B.Contains(busca)</c> nos campos de texto marcados para busca.</summary>
    private static Expression<Func<T, bool>> Busca<T>(AuxiliarDefinicao def, string busca)
    {
        var e = Expression.Parameter(typeof(T), "e");
        var contains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
        var propriedade = typeof(EF).GetMethod(nameof(EF.Property))!.MakeGenericMethod(typeof(string));
        var corpo = def.Campos.Where(c => c.Busca)
            .Select(c => (Expression)Expression.Call(Expression.Call(propriedade, e, Expression.Constant(c.Propriedade)), contains, Expression.Constant(busca)))
            .Aggregate(Expression.OrElse);
        return Expression.Lambda<Func<T, bool>>(corpo, e);
    }

    private static bool Obrigatorio(CampoAuxiliar c, IProperty p) =>
        c.Tipo != TipoCampo.Logico && !p.IsNullable;

    private static IReadOnlyList<OpcaoVM> OpcoesDoEnum(Type tipo)
    {
        var e = Nullable.GetUnderlyingType(tipo) ?? tipo;
        return Enum.GetValues(e).Cast<object>().Select(v => new OpcaoVM(Convert.ToInt32(v, CultureInfo.InvariantCulture), Rotulo(v))).ToList();
    }

    // [Display(Name)] do enum, ou o nome em frase ("NotaFiscal" → "Nota fiscal")
    private static string Rotulo(object valor)
    {
        var membro = valor.GetType().GetField(valor.ToString()!);
        if (membro?.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), false)
                is [System.ComponentModel.DataAnnotations.DisplayAttribute { Name: { } nome }, ..])
            return nome;
        var s = RegexMaiuscula().Replace(valor.ToString()!, " $1").Trim();
        return s.Length == 0 ? s : s[0] + s[1..].ToLowerInvariant();
    }

    private static string? ParaFormulario(CampoAuxiliar c, object? valor) => valor switch
    {
        null => null,
        bool b => b ? "true" : "false",
        Enum en => Convert.ToInt32(en, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => valor.ToString(),
    };

    private static string? Exibir(CampoAuxiliar c, IProperty p, object? valor) => valor switch
    {
        null => null,
        bool b => b ? "Sim" : "Não",
        Enum en => Rotulo(en),
        _ => ParaFormulario(c, valor),
    };

    /// <summary>Texto do formulário → valor da propriedade, com as validações de tamanho, faixa e formato.</summary>
    private static Resultado<object?> Converter(CampoAuxiliar c, IProperty p, string? texto)
    {
        var tipo = Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType;
        texto = string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

        if (tipo == typeof(bool)) return Resultado.Ok<object?>(texto is "true" or "on");
        if (texto is null)
            return Obrigatorio(c, p) ? Resultado.Erro($"Informe {c.Rotulo.ToLowerInvariant()}.", c.Propriedade) : Resultado.Ok<object?>(null);

        if (tipo == typeof(string))
        {
            if (c.Maiusculas) texto = texto.ToUpperInvariant();
            if (c.Tipo == TipoCampo.Cor)
            {
                if (!RegexCor().IsMatch(texto)) return Resultado.Erro($"{c.Rotulo}: use o formato #RRGGBB.", c.Propriedade);
                texto = texto.ToUpperInvariant();
            }
            if (p.GetMaxLength() is { } max && texto.Length > max)
                return Resultado.Erro($"{c.Rotulo}: no máximo {max} caracteres.", c.Propriedade);
            return Resultado.Ok<object?>(texto);
        }
        if (tipo.IsEnum)
        {
            if (!int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) || !Enum.IsDefined(tipo, n))
                return Resultado.Erro($"{c.Rotulo}: opção inválida.", c.Propriedade);
            return Resultado.Ok<object?>(Enum.ToObject(tipo, n));
        }
        if (tipo == typeof(int) || tipo == typeof(short))
        {
            if (!long.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                return Resultado.Erro($"{c.Rotulo}: informe um número inteiro.", c.Propriedade);
            var min = c.Minimo ?? (tipo == typeof(short) ? short.MinValue : int.MinValue);
            var maxV = c.Maximo ?? (tipo == typeof(short) ? short.MaxValue : int.MaxValue);
            if (n < min || n > maxV) return Resultado.Erro($"{c.Rotulo}: informe de {min} a {maxV}.", c.Propriedade);
            return Resultado.Ok<object?>(tipo == typeof(short) ? (object)(short)n : (int)n); // sem o cast o ternário vira int
        }
        throw new NotSupportedException($"{p.DeclaringType.ClrType.Name}.{p.Name}: tipo {tipo.Name} não suportado pelo cadastro auxiliar.");
    }

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex RegexCor();

    [GeneratedRegex("(?<!^)([A-Z])")]
    private static partial Regex RegexMaiuscula();
}
