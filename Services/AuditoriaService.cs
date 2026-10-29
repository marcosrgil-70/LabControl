using System.Globalization;
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

    private static readonly Dictionary<string, string> AssuntoDaTabela =
        Assuntos.Values.SelectMany(a => a.Tabelas.Select(t => (t, a.Nome))).ToDictionary(x => x.t, x => x.Nome, StringComparer.OrdinalIgnoreCase);

    // tela do registro (quando a tabela tem uma e o ID é o da própria linha)
    private static readonly Dictionary<string, string> Telas = new(StringComparer.OrdinalIgnoreCase)
    {
        ["LAB_PROPOSTAS"] = "/Propostas/Editar/", ["PEDIDOS_VENDA"] = "/PedidosVenda/Editar/", ["LAB_HIST_AMOSTRAS"] = "/Amostras/Editar/",
        ["LAB_RESULTADOS_ANALISES"] = "/Resultados/Editar/", ["LAB_BOLETIM_ANALISES"] = "/Boletins/Editar/", ["USUARIOS"] = "/Usuarios/Editar/",
        ["PERFIS"] = "/Perfis/Editar/", ["ENTIDADES_FUNCIONARIOS"] = "/Profissionais/Editar/",
    };

    public async Task<AuditoriaListaVM> ListarAsync(AuditoriaFiltroVM f)
    {
        await NormalizarAsync(f);
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
        var (antes, depois) = (DadosAuditoria.Campos(a.DadosAntes), DadosAuditoria.Campos(a.DadosDepois));
        var nomes = antes.Keys.Concat(depois.Keys).Distinct().ToList();   // a ordem da gravação
        var campos = nomes.Select(n =>
        {
            var (temAntes, temDepois) = (antes.TryGetValue(n, out var va), depois.TryGetValue(n, out var vd));
            // inclusão/exclusão e eventos: só um dos lados; alteração: só as colunas que mudaram (P21)
            // coluna sigilosa ou binária grava "***"/"[binário]" dos dois lados: se está no evento A, mudou
            var mudou = a.Acao == AcoesAuditoria.Alteracao ? va != vd || va is "***" or "[binário]" : temAntes && temDepois && va != vd;
            return new AuditoriaCampoVM(n, va, vd, mudou);
        }).ToList();
        return new AuditoriaDetalheVM { Evento = Item(a, antes, depois), Campos = campos };
    }

    /// <summary>CSV do filtro (UTF-8 com BOM e <c>;</c>), até <see cref="MaxCsv"/> linhas, com os JSON antes/depois.</summary>
    public async Task<byte[]> CsvAsync(AuditoriaFiltroVM f)
    {
        await NormalizarAsync(f);
        var linhas = await Projetar(Consulta(f).OrderByDescending(a => a.DtEvento).ThenByDescending(a => a.IdAuditoria).Take(MaxCsv)).ToListAsync();
        var csv = new Csv().Linha("Data/hora", "Usuário", "Ação", "Assunto", "Tabela", "Registro", "IP", "Empresa", "Resumo", "Antes", "Depois");
        foreach (var a in linhas)
        {
            var i = Item(a);
            csv.Linha(i.Quando.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture), i.Usuario, i.Acao, i.Assunto,
                i.Tabela, i.IdRegistro, i.Ip, i.Empresa, i.Resumo, a.DadosAntes, a.DadosDepois);
        }
        return csv.Bytes();
    }

    /// <summary>Aba "Histórico da amostra": todas as revisões da amostra física pelo número (com ou sem " R01").</summary>
    public async Task<HistoricoAmostraVM> HistoricoAmostraAsync(string? numero)
    {
        if (Textos.Limpo(numero) is not { } n) return new HistoricoAmostraVM();
        var achadas = await db.LabHistAmostras.AsNoTracking().Where(a => a.NrAmostra == n || a.NrAmostra.StartsWith(n + " "))
            .Select(a => a.IdAmostraRaiz ?? a.IdLabHistAmostras).Distinct().ToListAsync();
        if (achadas.Count == 0) return new HistoricoAmostraVM { Numero = n, Mensagem = $"Amostra {n} não encontrada nesta empresa (digite o número completo, ex.: 04-00001-01/26)." };
        // pela chave e pelo índice da raiz (COALESCE(...) IN (...) não usa índice)
        var revisoes = await db.LabHistAmostras.AsNoTracking()
            .Where(a => achadas.Contains(a.IdLabHistAmostras) || (a.IdAmostraRaiz != null && achadas.Contains(a.IdAmostraRaiz.Value)))
            .OrderBy(a => a.IdAmostraRaiz ?? a.IdLabHistAmostras).ThenBy(a => a.Revisao).Select(a => new { a.IdLabHistAmostras, a.NrAmostra }).ToListAsync();
        var logs = await amostras.HistoricoAsync(revisoes.Select(r => r.IdLabHistAmostras).ToList());
        return new HistoricoAmostraVM { Numero = n, Revisoes = revisoes.Select(r => (r.NrAmostra, logs[r.IdLabHistAmostras])).ToList() };
    }

    // ------------------------------------------------------------------ consulta

    private sealed record Linha(long IdAuditoria, DateTime DtEvento, string? Login, string Acao, string Tabela, string IdRegistro,
        string? Ip, string? DadosAntes, string? DadosDepois, string? Empresa);

    /// <summary>Período padrão (últimos 7 dias) e "todas as empresas" só para ADMIN_GLOBAL.</summary>
    private async Task NormalizarAsync(AuditoriaFiltroVM f)
    {
        f.Ate ??= await db.HojeAsync();
        f.De ??= f.Ate.Value.AddDays(-6);
        if (!ctx.AdminGlobal) f.TodasEmpresas = false;
    }

    /// <summary>
    /// O que o usuário pode ver: eventos da empresa ativa + logins (gravados sem empresa) dos usuários com acesso a ela;
    /// ADMIN_GLOBAL com "todas", tudo. A regra é explícita porque o IgnoreQueryFilters vale para a consulta inteira (num
    /// UNION com o filtro global, o filtro sumia também da outra parte e a empresa vazava — achado no /simplify da 8.5).
    /// </summary>
    private IQueryable<Models.Auditoria> Visiveis(bool todas)
    {
        var q = db.Auditoria.IgnoreQueryFilters().AsNoTracking();
        if (todas) return q;
        var ativa = ctx.EmpresaId;
        var usuariosDaEmpresa = db.UsuariosEmpresas.IgnoreQueryFilters().Where(u => u.IdEmpresas == ativa).Select(u => (int?)u.IdUsuarios);
        return q.Where(a => (ativa != null && a.IdEmpresas == ativa)
                            || (a.IdEmpresas == null && a.Acao == AcoesAuditoria.Login && usuariosDaEmpresa.Contains(a.IdUsuarios)));
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
            // tentativa com login inexistente: sem usuário, o login digitado fica no ID do registro ("LOGIN:...")
            q = q.Where(a => ids.Contains(a.IdUsuarios) || (a.IdUsuarios == null && a.Acao == AcoesAuditoria.Login && a.IdRegistro.Contains(u)));
        }
        if (f.Assunto is { } s && Assuntos.TryGetValue(s, out var assunto))
        {
            var tabelas = assunto.Tabelas.ToList();   // List: array.Contains vira ReadOnlySpan no C# 14 e o EF não traduz
            q = q.Where(a => tabelas.Contains(a.Tabela));
        }
        if (f.Acao is { } ac && Acoes.ContainsKey(ac)) q = q.Where(a => a.Acao == ac);
        if (Textos.Limpo(f.Registro) is { } r)
            q = q.Where(a => a.IdRegistro == r || (a.DadosDepois != null && a.DadosDepois.Contains(r)) || (a.DadosAntes != null && a.DadosAntes.Contains(r)));
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

    private static AuditoriaItemVM Item(Linha a) => Item(a, DadosAuditoria.Campos(a.DadosAntes), DadosAuditoria.Campos(a.DadosDepois));

    /// <param name="antes">JSON já lido (uma vez por linha).</param>
    private static AuditoriaItemVM Item(Linha a, Dictionary<string, string?> antes, Dictionary<string, string?> d)
    {
        var assunto = AssuntoDaTabela.GetValueOrDefault(a.Tabela, a.Tabela);
        var link = Telas.TryGetValue(a.Tabela, out var tela) && int.TryParse(a.IdRegistro, out _) && a.Acao != AcoesAuditoria.Exclusao ? tela + a.IdRegistro : null;
        // tentativa com login inexistente: sem usuário, o login digitado está nos dados
        var usuario = a.Login ?? (a.Acao == AcoesAuditoria.Login ? d.GetValueOrDefault("LOGIN") : null);
        return new AuditoriaItemVM(a.IdAuditoria, a.DtEvento, usuario, Acoes.GetValueOrDefault(a.Acao, a.Acao), assunto, a.Tabela, a.IdRegistro, link, a.Ip,
            Resumo(a.Acao, antes, d), a.Empresa);
    }

    /// <summary>Uma linha para o grid: motivo das revisões, resultado do login, destinatários do e-mail, número do documento.</summary>
    private static string? Resumo(string acao, Dictionary<string, string?> antes, Dictionary<string, string?> d)
    {
        string? V(string campo) => d.GetValueOrDefault(campo) ?? antes.GetValueOrDefault(campo);
        if (acao == AcoesAuditoria.Login)
            return V("SUCESSO") == "true" ? $"entrou ({V("LOGIN")})" : $"falhou ({V("LOGIN")}{(V("MOTIVO") is { } m ? ": " + m.ToLowerInvariant() : "")})";
        if (acao == AcoesAuditoria.EnvioEmail) return $"para {V("PARA")}";
        var partes = new List<string>();
        foreach (var c in new[] { "NR_PROPOSTA", "NR_AMOSTRA", "COD_BOLETIM", "NR_PEDIDO", "LOGIN", "DESCRICAO" })
            if (V(c) is { } v) { partes.Add(v); break; }
        if ((V("MOTIVO") ?? V("MOTIVO_REVISAO")) is { } motivo) partes.Add("motivo: " + motivo);
        if (acao == AcoesAuditoria.Alteracao && partes.Count == 0 && d.Count > 0) partes.Add(string.Join(", ", d.Keys.Take(4)) + (d.Count > 4 ? "…" : ""));
        return partes.Count == 0 ? null : string.Join(" · ", partes);
    }
}
