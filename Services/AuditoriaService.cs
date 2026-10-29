using System.Globalization;
using System.Text;
using System.Text.Json;
using LabControl.Data;
using LabControl.Infra;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services;

/// <summary>
/// T06 — Auditoria (consulta somente leitura, tarefa 8.4; P21). Empresa ativa; os logins não têm empresa (gravados antes
/// de escolhê-la) e aparecem para os usuários com acesso à empresa ativa. ADMIN_GLOBAL pode ver todas as empresas e os
/// logins de qualquer usuário (inclusive tentativas com login inexistente).
/// </summary>
public sealed class AuditoriaService(LabDbContext db, IEmpresaContexto ctx, AmostraService amostras)
{
    public const int MaxCsv = 50_000;

    /// <summary>Assunto da tela → tabelas de <c>AUDITORIA.TABELA</c>.</summary>
    public static readonly IReadOnlyDictionary<string, (string Nome, string[] Tabelas)> Assuntos = new Dictionary<string, (string, string[])>
    {
        ["PROPOSTAS"] = ("Propostas", ["LAB_PROPOSTAS", "LAB_PROPOSTAS_PRODUTOS", "LAB_PROPOSTAS_PRODANALISES", "LAB_PROPOSTAS_TEXTOS"]),
        ["PEDIDOS"] = ("Pedidos de venda", ["PEDIDOS_VENDA", "PEDIDOS_VENDA_ITENS"]),
        ["AMOSTRAS"] = ("Amostras", ["LAB_HIST_AMOSTRAS", "LAB_HIST_AMOSTRAS_TESTES"]),
        ["ESTOQUE"] = ("Movimentação e localização", ["LAB_MOV_AMOSTRAS", "LAB_LOCAL_AMOSTRAS"]),
        ["RESULTADOS"] = ("Resultados", ["LAB_RESULTADOS_ANALISES", "LAB_RESULTADOS_PARAM"]),
        ["BOLETINS"] = ("Boletins", ["LAB_BOLETIM_ANALISES", "LAB_BOLETIM_PARAMETROS"]),
        ["USUARIOS"] = ("Usuários e login", ["USUARIOS", "USUARIOS_EMPRESAS", "USUARIOS_PERFIS"]),
        ["PERFIS"] = ("Perfis de acesso", ["PERFIS", "PERFIS_PERMISSOES"]),
        ["EMPRESA"] = ("Empresa e parâmetros", ["EMPRESAS", "EMPRESAS_PARAMETROS"]),
        ["NUMERACAO"] = ("Numeração", ["SEQUENCIAS_DOCUMENTOS"]),
        ["PROFISSIONAIS"] = ("Profissionais", ["ENTIDADES_FUNCIONARIOS"]),
    };

    public static readonly IReadOnlyDictionary<string, string> Acoes = new Dictionary<string, string>
    {
        [AcoesAuditoria.Inclusao] = "Inclusão", [AcoesAuditoria.Alteracao] = "Alteração", [AcoesAuditoria.Exclusao] = "Exclusão",
        [AcoesAuditoria.Revisao] = "Revisão", [AcoesAuditoria.Emissao] = "Emissão", [AcoesAuditoria.DesfazerRevisao] = "Desfazer revisão",
        [AcoesAuditoria.Login] = "Login", [AcoesAuditoria.EnvioEmail] = "Envio por e-mail",
    };

    // tela do registro (quando a tabela tem uma e o ID é o da própria linha)
    private static readonly Dictionary<string, string> Telas = new(StringComparer.OrdinalIgnoreCase)
    {
        ["LAB_PROPOSTAS"] = "/Propostas/Editar/", ["PEDIDOS_VENDA"] = "/PedidosVenda/Editar/", ["LAB_HIST_AMOSTRAS"] = "/Amostras/Editar/",
        ["LAB_RESULTADOS_ANALISES"] = "/Resultados/Editar/", ["LAB_BOLETIM_ANALISES"] = "/Boletins/Editar/", ["USUARIOS"] = "/Usuarios/Editar/",
        ["PERFIS"] = "/Perfis/Editar/", ["ENTIDADES_FUNCIONARIOS"] = "/Profissionais/Editar/",
    };

    public async Task<AuditoriaListaVM> ListarAsync(AuditoriaFiltroVM f)
    {
        var hoje = await db.HojeAsync();
        f.Ate ??= hoje;
        f.De ??= f.Ate.Value.AddDays(-6);
        if (!ctx.AdminGlobal) f.TodasEmpresas = false;
        var q = Consulta(f);
        var total = await q.CountAsync();
        f.Pagina = Paginacao.Ajustar(f.Pagina, total, AuditoriaFiltroVM.TamanhoPagina);
        var linhas = await Projetar(q.OrderByDescending(a => a.DtEvento).ThenByDescending(a => a.IdAuditoria)
            .Skip((f.Pagina - 1) * AuditoriaFiltroVM.TamanhoPagina).Take(AuditoriaFiltroVM.TamanhoPagina)).ToListAsync();
        return new AuditoriaListaVM { Filtro = f, Itens = linhas.Select(Item).ToList(), Total = total, AdminGlobal = ctx.AdminGlobal };
    }

    /// <summary>"Ver diferença": campo a campo, antes × depois (só os eventos que a consulta deixaria ver).</summary>
    public async Task<AuditoriaDetalheVM?> DetalheAsync(long id)
    {
        var a = await Projetar(Visiveis(ctx.AdminGlobal).Where(x => x.IdAuditoria == id)).SingleOrDefaultAsync();
        if (a is null) return null;
        var antes = Campos(a.DadosAntes);
        var depois = Campos(a.DadosDepois);
        var nomes = antes.Keys.Concat(depois.Keys).Distinct().ToList();   // a ordem da gravação
        var campos = nomes.Select(n =>
        {
            var (temAntes, temDepois) = (antes.TryGetValue(n, out var va), depois.TryGetValue(n, out var vd));
            // inclusão/exclusão e eventos: só um dos lados; alteração: só as colunas que mudaram (P21)
            var mudou = a.Acao == AcoesAuditoria.Alteracao ? va != vd : temAntes && temDepois && va != vd;
            return new AuditoriaCampoVM(n, va, vd, mudou);
        }).ToList();
        return new AuditoriaDetalheVM { Evento = Item(a), Campos = campos };
    }

    /// <summary>CSV do filtro (UTF-8 com BOM e <c>;</c>), até <see cref="MaxCsv"/> linhas, com os JSON antes/depois.</summary>
    public async Task<byte[]> CsvAsync(AuditoriaFiltroVM f)
    {
        await ListarAsync(f);   // mesmas datas padrão e a mesma restrição de empresas
        var linhas = await Projetar(Consulta(f).OrderByDescending(a => a.DtEvento).ThenByDescending(a => a.IdAuditoria).Take(MaxCsv)).ToListAsync();
        static string C(string? s) => s is null ? "" : "\"" + s.Replace("\"", "\"\"") + "\"";
        var csv = new StringBuilder("Data/hora;Usuário;Ação;Assunto;Tabela;Registro;IP;Empresa;Resumo;Antes;Depois\r\n");
        foreach (var a in linhas)
        {
            var i = Item(a);
            csv.AppendJoin(';', [C(i.Quando.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture)), C(i.Usuario), C(i.Acao), C(i.Assunto),
                C(i.Tabela), C(i.IdRegistro), C(i.Ip), C(i.Empresa), C(i.Resumo), C(a.DadosAntes), C(a.DadosDepois)]).Append("\r\n");
        }
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }

    /// <summary>Aba "Histórico da amostra": todas as revisões da amostra física pelo número (com ou sem " R01").</summary>
    public async Task<HistoricoAmostraVM> HistoricoAmostraAsync(string? numero)
    {
        if (Textos.Limpo(numero) is not { } n) return new HistoricoAmostraVM();
        var achadas = await db.LabHistAmostras.AsNoTracking().Where(a => a.NrAmostra == n || a.NrAmostra.StartsWith(n + " "))
            .Select(a => a.IdAmostraRaiz ?? a.IdLabHistAmostras).Distinct().ToListAsync();
        if (achadas.Count == 0) return new HistoricoAmostraVM { Numero = n, Mensagem = $"Amostra {n} não encontrada nesta empresa (digite o número completo, ex.: 04-00001-01/26)." };
        var revisoes = await db.LabHistAmostras.AsNoTracking().Where(a => achadas.Contains(a.IdAmostraRaiz ?? a.IdLabHistAmostras))
            .OrderBy(a => a.IdAmostraRaiz ?? a.IdLabHistAmostras).ThenBy(a => a.Revisao).Select(a => new { a.IdLabHistAmostras, a.NrAmostra }).ToListAsync();
        var lista = new List<(string, IReadOnlyList<AmostraLogVM>)>();
        foreach (var r in revisoes) lista.Add((r.NrAmostra, await amostras.HistoricoAsync(r.IdLabHistAmostras)));
        return new HistoricoAmostraVM { Numero = n, Revisoes = lista };
    }

    // ------------------------------------------------------------------ consulta

    private sealed record Linha(long IdAuditoria, DateTime DtEvento, string? Login, string Acao, string Tabela, string IdRegistro,
        string? Ip, string? DadosAntes, string? DadosDepois, string? Empresa);

    /// <summary>
    /// O que o usuário pode ver. Sem o filtro global (os logins não têm empresa): empresa ativa + logins de quem tem acesso
    /// a ela; ADMIN_GLOBAL com "todas", tudo.
    /// </summary>
    private IQueryable<Models.Auditoria> Visiveis(bool todas)
    {
        var q = db.Auditoria.IgnoreQueryFilters().AsNoTracking();
        if (todas) return q;
        var ativa = ctx.EmpresaId;
        var usuariosDaEmpresa = db.UsuariosEmpresas.IgnoreQueryFilters().Where(u => u.IdEmpresas == ativa).Select(u => u.IdUsuarios.ToString());
        return q.Where(a => (ativa != null && a.IdEmpresas == ativa)
                            || (a.IdEmpresas == null && a.Acao == AcoesAuditoria.Login && a.Tabela == "USUARIOS" && usuariosDaEmpresa.Contains(a.IdRegistro)));
    }

    private IQueryable<Models.Auditoria> Consulta(AuditoriaFiltroVM f)
    {
        var q = Visiveis(f.TodasEmpresas && ctx.AdminGlobal);
        var de = f.De!.Value.ToDateTime(TimeOnly.MinValue);
        var ate = f.Ate!.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
        q = q.Where(a => a.DtEvento >= de && a.DtEvento < ate);
        if (Textos.Limpo(f.Usuario) is { } u)
        {
            var ids = db.Usuarios.IgnoreQueryFilters().Where(x => x.Login.Contains(u)).Select(x => (int?)x.IdUsuarios);
            var idsTexto = db.Usuarios.IgnoreQueryFilters().Where(x => x.Login.Contains(u)).Select(x => x.IdUsuarios.ToString());
            // login: ainda sem sessão, o usuário fica no ID do registro (ou o login digitado, se não existe)
            q = q.Where(a => ids.Contains(a.IdUsuarios)
                             || (a.Acao == AcoesAuditoria.Login && (idsTexto.Contains(a.IdRegistro) || a.IdRegistro.Contains(u))));
        }
        if (f.Assunto is { } s && Assuntos.TryGetValue(s, out var assunto))
        {
            var tabelas = assunto.Tabelas.ToList();   // List: array.Contains vira ReadOnlySpan no C# 14 e o EF não traduz
            q = q.Where(a => tabelas.Contains(a.Tabela));
        }
        if (f.Acao is { } ac && Acoes.ContainsKey(ac)) q = q.Where(a => a.Acao == ac);
        if (Textos.Limpo(f.Registro) is { } r)
        {
            // DADOS_* são JSON: o Contains do EF vira CAST AS JSON e quebra com texto comum; LIKE sobre o texto da coluna
            var padrao = "%" + r.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";
            var nosDados = db.Auditoria.FromSqlInterpolated(
                    $"SELECT * FROM AUDITORIA WHERE CAST(DADOS_DEPOIS AS CHAR) LIKE {padrao} OR CAST(DADOS_ANTES AS CHAR) LIKE {padrao}")
                .IgnoreQueryFilters().Select(x => x.IdAuditoria);
            q = q.Where(a => a.IdRegistro == r || nosDados.Contains(a.IdAuditoria));
        }
        return q;
    }

    private IQueryable<Linha> Projetar(IQueryable<Models.Auditoria> q) =>
        from a in q
        join u in db.Usuarios.IgnoreQueryFilters() on a.IdUsuarios equals (int?)u.IdUsuarios into us
        from u in us.DefaultIfEmpty()
        join e in db.Empresas.IgnoreQueryFilters() on a.IdEmpresas equals (int?)e.IdEmpresas into es
        from e in es.DefaultIfEmpty()
        select new Linha(a.IdAuditoria, a.DtEvento, u == null ? null : u.Login, a.Acao, a.Tabela, a.IdRegistro, a.Ip, a.DadosAntes, a.DadosDepois,
            e == null ? null : e.CodEmpresa + " — " + (e.NomeFantasia ?? e.RazaoSocial));

    private static AuditoriaItemVM Item(Linha a)
    {
        var assunto = Assuntos.Values.FirstOrDefault(x => x.Tabelas.Contains(a.Tabela, StringComparer.OrdinalIgnoreCase)).Nome ?? a.Tabela;
        var link = Telas.TryGetValue(a.Tabela, out var tela) && int.TryParse(a.IdRegistro, out _) && a.Acao != AcoesAuditoria.Exclusao ? tela + a.IdRegistro : null;
        // login: ainda sem sessão (ID_USUARIOS vazio); o login digitado está nos dados
        var usuario = a.Login ?? (a.Acao == AcoesAuditoria.Login ? Campos(a.DadosDepois).GetValueOrDefault("LOGIN") : null);
        return new AuditoriaItemVM(a.IdAuditoria, a.DtEvento, usuario, Acoes.GetValueOrDefault(a.Acao, a.Acao), assunto, a.Tabela, a.IdRegistro, link, a.Ip,
            Resumo(a), a.Empresa);
    }

    /// <summary>Uma linha para o grid: motivo das revisões, resultado do login, destinatários do e-mail, número do documento.</summary>
    private static string? Resumo(Linha a)
    {
        var d = Campos(a.DadosDepois);
        var antes = Campos(a.DadosAntes);
        string? V(string campo) => d.GetValueOrDefault(campo) ?? antes.GetValueOrDefault(campo);
        if (a.Acao == AcoesAuditoria.Login)
            return V("SUCESSO") == "true" ? $"entrou ({V("LOGIN")})" : $"falhou ({V("LOGIN")}{(V("MOTIVO") is { } m ? ": " + m.ToLowerInvariant() : "")})";
        if (a.Acao == AcoesAuditoria.EnvioEmail) return $"para {V("PARA")}";
        var partes = new List<string>();
        foreach (var c in new[] { "NR_PROPOSTA", "NR_AMOSTRA", "COD_BOLETIM", "NR_PEDIDO", "LOGIN", "DESCRICAO" })
            if (V(c) is { } v) { partes.Add(v); break; }
        if ((V("MOTIVO") ?? V("MOTIVO_REVISAO")) is { } motivo) partes.Add("motivo: " + motivo);
        if (a.Acao == AcoesAuditoria.Alteracao && partes.Count == 0 && d.Count > 0) partes.Add(string.Join(", ", d.Keys.Take(4)) + (d.Count > 4 ? "…" : ""));
        return partes.Count == 0 ? null : string.Join(" · ", partes);
    }

    /// <summary>JSON da auditoria → campo/valor em texto (objetos e listas aninhados ficam como JSON).</summary>
    private static Dictionary<string, string?> Campos(string? json)
    {
        var r = new Dictionary<string, string?>();
        if (string.IsNullOrWhiteSpace(json)) return r;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return new() { ["(dados)"] = json };
            foreach (var p in doc.RootElement.EnumerateObject())
                r[p.Name] = p.Value.ValueKind switch
                {
                    JsonValueKind.Null => null,
                    JsonValueKind.String => p.Value.GetString(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    _ => p.Value.GetRawText(),
                };
        }
        catch (JsonException) { r["(dados)"] = json; }
        return r;
    }
}
