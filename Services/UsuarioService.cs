using System.Security.Cryptography;
using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services;

/// <summary>
/// T02 — Usuários (docs/telas/T02-usuarios.md). <c>USUARIOS</c> é global; o acesso é por empresa:
/// <list type="bullet">
/// <item>ADMIN_GLOBAL vê todos os usuários e edita o acesso a todas as empresas;</item>
/// <item>quem administra uma empresa vê os usuários com acesso a ela e edita só o vínculo dessa empresa;
/// os dados globais (nome, ativo, senha, exclusão) só se o usuário for exclusivo da empresa ativa — senão
/// um administrador local mudaria (ou descobriria a senha de) um usuário de outra empresa.</item>
/// </list>
/// Toda gravação passa pelo SaveChanges auditado (USUARIOS, USUARIOS_EMPRESAS e USUARIOS_PERFIS estão no P21).
/// </summary>
public sealed partial class UsuarioService(LabDbContext db, IEmpresaContexto ctx, VinculoUsuarioProfissionalService vinculos)
{
    private const string AlfabetoSenha = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

    // ------------------------------------------------------------------ consulta

    private IQueryable<Usuario> Visiveis() => ctx.AdminGlobal
        ? db.Usuarios
        : db.Usuarios.Where(u => db.UsuariosEmpresas.Any(v => v.IdUsuarios == u.IdUsuarios && v.IdEmpresas == ctx.EmpresaId));

    public async Task<IReadOnlyList<UsuarioListaItemVM>> ListarAsync(UsuarioFiltroVM filtro)
    {
        var q = Visiveis().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var t = filtro.Texto.Trim();
            q = q.Where(u => u.Login.Contains(t) || u.Nome.Contains(t) || (u.Email != null && u.Email.Contains(t)));
        }
        if (filtro.Ativos is { } ativos) q = q.Where(u => u.Ativo == ativos);
        var agora = DateTime.Now;
        var lista = await q.OrderBy(u => u.Login)
            .Select(u => new { u.IdUsuarios, u.Login, u.Nome, u.Email, u.Ativo, u.AdminGlobal, u.BloqueadoAte, u.UltimoAcesso })
            .Take(500).ToListAsync();

        // códigos das empresas: só para ADMIN_GLOBAL (o administrador local não vê as outras empresas do usuário)
        var empresas = ctx.AdminGlobal
            ? (await db.UsuariosEmpresas.AsNoTracking().Where(v => v.Ativo)
                  .Select(v => new { v.IdUsuarios, v.Empresa.CodEmpresa }).ToListAsync())
              .ToLookup(v => v.IdUsuarios, v => v.CodEmpresa)
            : null;
        return lista.Select(u => new UsuarioListaItemVM(u.IdUsuarios, u.Login, u.Nome, u.Email, u.Ativo, u.AdminGlobal,
                u.BloqueadoAte > agora, u.UltimoAcesso, empresas is null ? "" : string.Join(", ", empresas[u.IdUsuarios].Order())))
            .ToList();
    }

    /// <summary>
    /// Formulário de inclusão: linhas das empresas editáveis (administrador local: só a ativa). A empresa ativa já vem
    /// marcada e como padrão, também para o ADMIN_GLOBAL: sem o acesso, o usuário não aparece no combo do profissional (T13).
    /// </summary>
    public async Task<UsuarioVM> NovoAsync()
    {
        var vm = new UsuarioVM { PodeMarcarAdminGlobal = ctx.AdminGlobal, TrocarSenha = true, EmpresaPadrao = ctx.EmpresaId };
        await PrepararFormularioAsync(vm, null);
        vm.Empresas.Where(e => e.IdEmpresa == ctx.EmpresaId).ToList().ForEach(e => e.Acesso = true);
        return vm;
    }

    public async Task<UsuarioVM?> CarregarAsync(int id)
    {
        var u = await Visiveis().AsNoTracking().SingleOrDefaultAsync(x => x.IdUsuarios == id);
        if (u is null) return null;
        var vm = new UsuarioVM
        {
            Id = u.IdUsuarios, Login = u.Login, Nome = u.Nome, Email = u.Email, Ativo = u.Ativo, AdminGlobal = u.AdminGlobal,
            TrocarSenha = u.TrocarSenha, BloqueadoAte = u.BloqueadoAte, UltimoAcesso = u.UltimoAcesso,
            EmpresaPadrao = await db.UsuariosEmpresas.Where(v => v.IdUsuarios == id && v.Padrao).Select(v => (int?)v.IdEmpresas).FirstOrDefaultAsync(),
        };
        await PrepararFormularioAsync(vm, u);
        return vm;
    }

    /// <summary>Recarrega as opções (combos) e as permissões de edição; usado também ao reexibir o POST com erro.</summary>
    public async Task PrepararFormularioAsync(UsuarioVM vm, Usuario? alvo = null)
    {
        alvo ??= vm.Id == 0 ? null : await db.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.IdUsuarios == vm.Id);
        vm.PodeEditarDados = alvo is null || await PodeEditarDadosAsync(alvo);
        vm.PodeMarcarAdminGlobal = ctx.AdminGlobal;

        var postadas = vm.Empresas.ToDictionary(e => e.IdEmpresa);
        var atuais = alvo is null ? [] : await db.UsuariosEmpresas.AsNoTracking().Where(v => v.IdUsuarios == alvo.IdUsuarios).ToListAsync();
        var perfisAtuais = alvo is null ? [] : await db.UsuariosPerfis.AsNoTracking().Where(p => p.IdUsuarios == alvo.IdUsuarios).ToListAsync();

        vm.Empresas = [];
        foreach (var emp in await EmpresasEditaveisAsync())
        {
            var linha = postadas.GetValueOrDefault(emp.IdEmpresas);
            if (linha is null)
            {
                var v = atuais.FirstOrDefault(x => x.IdEmpresas == emp.IdEmpresas);
                linha = new UsuarioEmpresaVM
                {
                    IdEmpresa = emp.IdEmpresas, Acesso = v is not null, Ativo = v?.Ativo ?? true, IdProfissional = v?.IdEntidadesFunc,
                    Perfis = perfisAtuais.Where(p => p.IdEmpresas == emp.IdEmpresas).Select(p => p.IdPerfis).ToList(),
                };
            }
            linha.Codigo = emp.CodEmpresa;
            linha.Nome = emp.NomeFantasia ?? emp.RazaoSocial;
            linha.Profissionais = await vinculos.ProfissionaisAsync(emp.IdEmpresas);
            linha.PerfisDisponiveis = (await PerfisAtivos(emp.IdEmpresas).OrderBy(p => p.Nome).Select(p => new { p.IdPerfis, p.Nome }).ToListAsync())
                .Select(p => (p.IdPerfis, p.Nome)).ToList();
            vm.Empresas.Add(linha);
        }
    }

    // ------------------------------------------------------------------ gravação

    /// <summary>Inclui com senha provisória forte (devolvida para ser exibida uma vez) e troca obrigatória.</summary>
    public async Task<Resultado<SenhaProvisoriaVM>> IncluirAsync(UsuarioVM vm)
    {
        if (!ctx.AdminGlobal && vm.AdminGlobal) return Resultado.Erro("Somente o administrador global pode criar outro administrador global.", nameof(vm.AdminGlobal));
        // usuário comum precisa de acesso a alguma empresa (senão não consegue entrar); o administrador local, à empresa ativa
        if (!vm.AdminGlobal && !vm.Empresas.Any(e => e.Acesso && (ctx.AdminGlobal || e.IdEmpresa == ctx.EmpresaId)))
            return Resultado.Erro(ctx.AdminGlobal ? "Marque o acesso a pelo menos uma empresa." : "Marque o acesso do usuário à empresa ativa.");
        var senha = GerarSenhaProvisoria();
        var usuario = new Usuario
        {
            Login = ContaService.Normalizar(vm.Login), Nome = vm.Nome.Trim(), Email = Textos.Limpo(vm.Email), Ativo = vm.Ativo,
            AdminGlobal = vm.AdminGlobal, TrocarSenha = true, SenhaHash = ContaService.GerarHash(senha),
        };
        await using var tx = await db.Database.BeginTransactionAsync();
        db.Add(usuario);
        var r = await ErrosMySql.CapturarAsync(() => db.SaveChangesAsync());
        if (!r.Sucesso) return Resultado.Erro(r.Mensagem!, CampoDoErro(r.Mensagem));
        var emp = await AplicarEmpresasAsync(usuario, vm, podeEditarDados: true);
        if (!emp.Sucesso) return Resultado.Erro(emp.Mensagem!, emp.Campo);
        var gravou = await ErrosMySql.CapturarAsync(() => db.SaveChangesAsync());
        if (!gravou.Sucesso) return Resultado.Erro(gravou.Mensagem!, CampoDoErro(gravou.Mensagem));
        await tx.CommitAsync();
        return Resultado.Ok(new SenhaProvisoriaVM(usuario.IdUsuarios, usuario.Login, senha, Inclusao: true));
    }

    public async Task<Resultado> AlterarAsync(UsuarioVM vm)
    {
        var alvo = await Visiveis().SingleOrDefaultAsync(u => u.IdUsuarios == vm.Id);
        if (alvo is null) return Resultado.Erro("Usuário não encontrado.");
        var podeDados = await PodeEditarDadosAsync(alvo);
        var proprio = alvo.IdUsuarios == ctx.UsuarioId;

        if (podeDados)
        {
            if (vm.AdminGlobal != alvo.AdminGlobal && !ctx.AdminGlobal)
                return Resultado.Erro("Somente o administrador global pode marcar ou desmarcar o administrador global.", nameof(vm.AdminGlobal));
            if (proprio && alvo.AdminGlobal && !vm.AdminGlobal)
                return Resultado.Erro("Você não pode remover o seu próprio acesso de administrador global.", nameof(vm.AdminGlobal));
            if (proprio && !vm.Ativo) return Resultado.Erro("Você não pode inativar o seu próprio usuário.", nameof(vm.Ativo));
            alvo.Login = ContaService.Normalizar(vm.Login);
            alvo.Nome = vm.Nome.Trim();
            alvo.Email = Textos.Limpo(vm.Email);
            alvo.Ativo = vm.Ativo;
            alvo.AdminGlobal = vm.AdminGlobal;
        }

        await using var tx = await db.Database.BeginTransactionAsync();
        var emp = await AplicarEmpresasAsync(alvo, vm, podeDados);
        if (!emp.Sucesso) return emp;
        if (proprio && !alvo.AdminGlobal && ctx.EmpresaId is { } ativa
            && !db.ChangeTracker.Entries<UsuarioEmpresa>().Any(e => e.Entity.IdUsuarios == alvo.IdUsuarios && e.Entity.IdEmpresas == ativa
                                                                  && e.Entity.Ativo && e.State != EntityState.Deleted))
            return Resultado.Erro("Você não pode remover o seu próprio acesso à empresa ativa.");
        var r = await ErrosMySql.CapturarAsync(() => db.SaveChangesAsync());
        if (!r.Sucesso) return Resultado.Erro(r.Mensagem!, CampoDoErro(r.Mensagem));
        await tx.CommitAsync();
        return Resultado.Ok();
    }

    /// <summary>Nova senha provisória (exibida uma vez), troca obrigatória, zera tentativas e bloqueio.</summary>
    public async Task<Resultado<SenhaProvisoriaVM>> ResetarSenhaAsync(int id)
    {
        var alvo = await Visiveis().SingleOrDefaultAsync(u => u.IdUsuarios == id);
        if (alvo is null) return Resultado.Erro("Usuário não encontrado.");
        if (!await PodeEditarDadosAsync(alvo)) return Resultado.Erro(MensagemSoAdminGlobal);
        var senha = GerarSenhaProvisoria();
        alvo.SenhaHash = ContaService.GerarHash(senha);
        alvo.TrocarSenha = true;
        alvo.TentativasFalhas = 0;
        alvo.BloqueadoAte = null;
        await db.SaveChangesAsync(); // auditado: A em USUARIOS com SENHA_HASH = "***"
        return Resultado.Ok(new SenhaProvisoriaVM(alvo.IdUsuarios, alvo.Login, senha, Inclusao: false));
    }

    public async Task<Resultado> DesbloquearAsync(int id)
    {
        var alvo = await Visiveis().SingleOrDefaultAsync(u => u.IdUsuarios == id);
        if (alvo is null) return Resultado.Erro("Usuário não encontrado.");
        alvo.TentativasFalhas = 0;
        alvo.BloqueadoAte = null;
        await db.SaveChangesAsync();
        return Resultado.Ok();
    }

    /// <summary>Só usuário que nunca gravou nada (CRIADO_POR/ALTERADO_POR, AUDITORIA, log da amostra); senão inativar.</summary>
    public async Task<Resultado> ExcluirAsync(int id)
    {
        var alvo = await Visiveis().SingleOrDefaultAsync(u => u.IdUsuarios == id);
        if (alvo is null) return Resultado.Erro("Usuário não encontrado.");
        if (alvo.IdUsuarios == ctx.UsuarioId) return Resultado.Erro("Você não pode excluir o seu próprio usuário.");
        if (!await PodeEditarDadosAsync(alvo)) return Resultado.Erro(MensagemSoAdminGlobal);
        if (await JaGravouAsync(id)) return Resultado.Erro("Este usuário já gravou registros no sistema: inative-o em vez de excluir.");

        await using var tx = await db.Database.BeginTransactionAsync();
        db.RemoveRange(await db.UsuariosPerfis.Where(p => p.IdUsuarios == id).ToListAsync());
        db.RemoveRange(await db.UsuariosEmpresas.Where(v => v.IdUsuarios == id).ToListAsync());
        db.Remove(alvo);
        var r = await ErrosMySql.CapturarAsync(() => db.SaveChangesAsync());
        if (!r.Sucesso) return r.Falha;
        await tx.CommitAsync();
        return Resultado.Ok();
    }

    // ------------------------------------------------------------------ regras internas

    public const string MensagemSoAdminGlobal =
        "Este usuário também tem acesso a outras empresas (ou é administrador global): somente o administrador global altera os dados, a senha ou exclui.";

    /// <summary>Dados globais: ADMIN_GLOBAL, ou usuário comum com acesso só à empresa ativa.</summary>
    private async Task<bool> PodeEditarDadosAsync(Usuario alvo) =>
        ctx.AdminGlobal
        || (!alvo.AdminGlobal && !await db.UsuariosEmpresas.AnyAsync(v => v.IdUsuarios == alvo.IdUsuarios && v.IdEmpresas != ctx.EmpresaId));

    private async Task<List<Empresa>> EmpresasEditaveisAsync() => ctx.AdminGlobal
        ? await db.Empresas.AsNoTracking().Where(e => e.Ativa).OrderBy(e => e.CodEmpresa).ToListAsync()
        : await db.Empresas.AsNoTracking().Where(e => e.IdEmpresas == ctx.EmpresaId).ToListAsync();

    // ADMIN_GLOBAL edita perfis de outra empresa: sem o filtro global, com a empresa no Where
    private IQueryable<Perfil> PerfisAtivos(int idEmpresa) =>
        db.Perfis.IgnoreQueryFilters().Where(p => p.IdEmpresas == idEmpresa && p.Ativo);

    /// <summary>Aplica as linhas do grid só das empresas editáveis (linhas de outras empresas no POST são ignoradas).</summary>
    private async Task<Resultado> AplicarEmpresasAsync(Usuario alvo, UsuarioVM vm, bool podeEditarDados)
    {
        var editaveis = (await EmpresasEditaveisAsync()).ToDictionary(e => e.IdEmpresas, e => e.CodEmpresa);

        for (var i = 0; i < vm.Empresas.Count; i++)
        {
            var linha = vm.Empresas[i];
            if (!editaveis.TryGetValue(linha.IdEmpresa, out var codigo)) continue;
            var vinculo = await db.UsuariosEmpresas.SingleOrDefaultAsync(v => v.IdUsuarios == alvo.IdUsuarios && v.IdEmpresas == linha.IdEmpresa);
            var perfisAtuais = await db.UsuariosPerfis.Where(p => p.IdUsuarios == alvo.IdUsuarios && p.IdEmpresas == linha.IdEmpresa).ToListAsync();

            if (!linha.Acesso)
            {
                db.RemoveRange(perfisAtuais);
                if (vinculo is not null) db.Remove(vinculo);
                continue;
            }

            var campo = $"Empresas[{i}]";
            var prof = await vinculos.ValidarAsync(alvo.IdUsuarios, linha.IdEmpresa, linha.IdProfissional, $"{campo}.IdProfissional");
            if (!prof.Sucesso) return prof;
            var validos = await PerfisAtivos(linha.IdEmpresa).Where(p => linha.Perfis.Contains(p.IdPerfis)).Select(p => p.IdPerfis).ToListAsync();
            if (validos.Count == 0 && !alvo.AdminGlobal)
                return Resultado.Erro($"Selecione ao menos um perfil na empresa {codigo}.", $"{campo}.Perfis");

            if (vinculo is null)
            {
                vinculo = new UsuarioEmpresa { IdUsuarios = alvo.IdUsuarios, IdEmpresas = linha.IdEmpresa };
                db.Add(vinculo);
            }
            vinculo.IdEntidadesFunc = linha.IdProfissional;
            vinculo.Ativo = linha.Ativo;
            if (podeEditarDados) vinculo.Padrao = vm.EmpresaPadrao == linha.IdEmpresa;

            db.RemoveRange(perfisAtuais.Where(p => !validos.Contains(p.IdPerfis)));
            foreach (var p in validos.Where(p => perfisAtuais.All(a => a.IdPerfis != p)))
                db.Add(new UsuarioPerfil { IdUsuarios = alvo.IdUsuarios, IdEmpresas = linha.IdEmpresa, IdPerfis = p });
        }

        // padrão é um só: desmarca nas empresas que o editor não vê (só quem pode editar os dados)
        if (podeEditarDados && vm.EmpresaPadrao is { } padrao)
            foreach (var outro in await db.UsuariosEmpresas.Where(v => v.IdUsuarios == alvo.IdUsuarios && v.IdEmpresas != padrao && v.Padrao).ToListAsync())
                outro.Padrao = false;
        return Resultado.Ok();
    }

    private async Task<bool> JaGravouAsync(int id)
    {
        if (await db.Auditoria.IgnoreQueryFilters().AnyAsync(a => a.IdUsuarios == id)) return true;
        if (await db.LabHistAmostrasLog.IgnoreQueryFilters().AnyAsync(l => l.IdUsuarios == id)) return true;
        // CRIADO_POR / ALTERADO_POR de qualquer tabela (sem FK para USUARIOS, por isso a busca no catálogo)
        var colunas = await db.Database.SqlQueryRaw<ColunaAutoria>("""
            SELECT TABLE_NAME AS Tabela, COLUMN_NAME AS Coluna FROM information_schema.COLUMNS
             WHERE TABLE_SCHEMA = DATABASE() AND COLUMN_NAME IN ('CRIADO_POR', 'ALTERADO_POR')
               AND TABLE_NAME IN (SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE')
            """).ToListAsync();
        foreach (var c in colunas)
        {
            // nomes vindos do information_schema (não do usuário) e conferidos; o valor vai como parâmetro
            if (!NomeSeguro().IsMatch(c.Tabela) || !NomeSeguro().IsMatch(c.Coluna)) continue;
            var sql = "SELECT EXISTS(SELECT 1 FROM `" + c.Tabela + "` WHERE `" + c.Coluna + "` = {0}) AS Value";
            if (await db.Database.SqlQueryRaw<int>(sql, id).SingleAsync() == 1) return true;
        }
        return false;
    }

    private sealed record ColunaAutoria(string Tabela, string Coluna);

    [System.Text.RegularExpressions.GeneratedRegex("^[A-Za-z0-9_]+$")]
    private static partial System.Text.RegularExpressions.Regex NomeSeguro();

    private static string? CampoDoErro(string? mensagem) => mensagem == "Já existe um usuário com este login." ? nameof(UsuarioVM.Login) : null;

    public static string GerarSenhaProvisoria()
    {
        while (true)
        {
            var s = new string(Enumerable.Range(0, 12).Select(_ => AlfabetoSenha[RandomNumberGenerator.GetInt32(AlfabetoSenha.Length)]).ToArray());
            if (ContaService.ValidarNovaSenha(s) is null) return s; // letras e números garantidos
        }
    }

}
