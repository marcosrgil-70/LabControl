using LabControl.Models;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services.Migracao.Etapas;

/// <summary>
/// M-09 — usuários (M-09-usuarios.md). A senha do legado é cifrada e não se recupera: todos recebem a senha
/// provisória <b>única</b> digitada na tela e trocam no 1º acesso; todos ficam com o perfil ADMINISTRADOR da empresa
/// (decisões do usuário, 21/10/2026 — dúvida 7); USUCOD 1 (ADMINISTRADOR do legado) vira ADMIN_GLOBAL.
/// </summary>
public sealed class M09Usuarios : IEtapaMigracao
{
    public string Codigo => "M-09";
    public string Nome => "Usuários";

    public async Task<int> ExecutarAsync(ContextoMigracao c, CancellationToken ct)
    {
        var linhas = await c.Fb.LerAsync("SELECT USUCOD, USUNOM, USUADM, ID_ENTIDADES_FUNC FROM USUARIO ORDER BY USUCOD", ct);
        var administrador = await c.Db.Perfis.SingleAsync(p => p.Nome == "ADMINISTRADOR", ct);
        var hash = ContaService.GerarHash(c.SenhaProvisoria); // mesmo hash para todos: a senha é a mesma (o sal do PBKDF2 já vem nele)
        var funcionarios = c.Mapa("ENTIDADES_FUNCIONARIOS");
        var vinculados = new HashSet<int>();
        var nomes = await c.Db.Entidades.Where(e => e.TipoFuncionario).ToDictionaryAsync(e => e.IdEntidades, e => e.Nome, ct);
        var logins = new HashSet<string>();

        foreach (var l in linhas)
        {
            var usucod = l.IntObrigatorio("USUCOD");
            var login = c.Cortar<Usuario>(nameof(Usuario.Login), ContaService.Normalizar(l.Texto("USUNOM")).Replace(" ", ""), $"usuário {usucod}")!;
            if (login.Length == 0 || !logins.Add(login))
            {
                c.Anomalia($"Usuário {usucod}: login \"{l.Texto("USUNOM")}\" vazio ou repetido — não migrado.");
                continue;
            }
            int? func = null;
            if (l.Int("ID_ENTIDADES_FUNC") is { } antigoFunc)
            {
                if (!funcionarios.TryGetValue(antigoFunc, out var f)) c.Anomalia($"Usuário {login}: profissional {antigoFunc} não migrado — usuário sem profissional.");
                else if (!vinculados.Add(f)) c.Anomalia($"Usuário {login}: o profissional {antigoFunc} já está vinculado a outro usuário — usuário sem profissional.");
                else func = f;
            }
            // desenvolvimento/testes ou banco do instalador substituído: o ADMINISTRADOR (USUCOD 1) entra com Admin123, sem troca
            var adminDev = usucod == 1 && c.AdministradorDesenvolvimento;
            var usuario = new Usuario
            {
                Login = login, Nome = c.Cortar<Usuario>(nameof(Usuario.Nome), func is { } id && nomes.TryGetValue(id, out var n) ? n : login, $"usuário {login}")!,
                SenhaHash = adminDev ? ContaService.GerarHash(ContaService.SenhaDesenvolvimento) : hash, TrocarSenha = !adminDev,
                Ativo = true, AdminGlobal = usucod == 1, UsucodLegado = usucod, IdEmpresasUltima = c.IdEmpresa,
            };
            if (adminDev) c.Log($"{login} com a senha {ContaService.SenhaDesenvolvimento}, sem troca obrigatória.");
            c.Db.Add(usuario);
            await c.Db.SaveChangesAsync(ct);
            c.Db.Add(new UsuarioEmpresa { IdUsuarios = usuario.IdUsuarios, IdEmpresas = c.IdEmpresa, IdEntidadesFunc = func, Padrao = true, Ativo = true });
            c.Db.Add(new UsuarioPerfil { IdUsuarios = usuario.IdUsuarios, IdEmpresas = c.IdEmpresa, IdPerfis = administrador.IdPerfis });
            c.Mapa("USUARIO")[usucod] = usuario.IdUsuarios;
        }
        await c.Db.SaveChangesAsync(ct);
        var semTroca = c.AdministradorDesenvolvimento && c.Mapa("USUARIO").ContainsKey(1) ? 1 : 0;
        if (semTroca == 1) await VincularProfissionalEspecialAsync(c, c.Mapa("USUARIO")[1], ct);

        c.Conferir("usuários do legado", linhas.Count, c.Mapa("USUARIO").Count);
        c.Conferir("usuários com troca de senha obrigatória", c.Mapa("USUARIO").Count - semTroca, await c.Db.Usuarios.CountAsync(u => u.TrocarSenha, ct));
        c.Conferir("usuários com o perfil ADMINISTRADOR", c.Mapa("USUARIO").Count, await c.Db.UsuariosPerfis.CountAsync(p => p.IdPerfis == administrador.IdPerfis, ct));
        c.Log($"{c.Mapa("USUARIO").Count} usuários com a senha provisória informada na tela (troca obrigatória no 1º acesso).");
        return c.Mapa("USUARIO").Count;
    }

    /// <summary>
    /// Desenvolvimento/testes (pedido do usuário, 23/10/2026): o ADMINISTRADOR fica vinculado ao profissional
    /// "USUÁRIO ESPECIAL DO SISTEMA" (criado se não existir), para gravar documentos que registram o profissional do
    /// usuário (proposta, amostra…). Mesma regra do <c>Scripts/v2/inicial.sql</c>.
    /// </summary>
    private static async Task VincularProfissionalEspecialAsync(ContextoMigracao c, int idUsuario, CancellationToken ct)
    {
        var especial = await c.Db.Entidades.FirstOrDefaultAsync(e => e.Nome == ContaService.ProfissionalEspecial, ct);
        if (especial is null)
        {
            especial = new Entidade { Categoria = Categoria.Fisica, Nome = ContaService.ProfissionalEspecial, TipoFuncionario = true };
            c.Db.Add(especial);
            await c.Db.SaveChangesAsync(ct);
        }
        if (!await c.Db.EntidadesFuncionarios.AnyAsync(f => f.IdEntidades == especial.IdEntidades, ct))
            c.Db.Add(new EntidadeFuncionario { IdEntidades = especial.IdEntidades, ExecutaAnalise = false });
        var vinculo = await c.Db.UsuariosEmpresas.SingleAsync(v => v.IdUsuarios == idUsuario && v.IdEmpresas == c.IdEmpresa, ct);
        vinculo.IdEntidadesFunc = especial.IdEntidades;
        await c.Db.SaveChangesAsync(ct);
        c.Log($"Desenvolvimento: {ContaService.LoginAdministrador} vinculado ao profissional {ContaService.ProfissionalEspecial}.");
    }
}
