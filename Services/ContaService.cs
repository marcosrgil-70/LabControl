using System.Security.Claims;
using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services;

public sealed record EmpresaDisponivel(int IdEmpresas, string Codigo, string Nome, int? ProfissionalId);

public sealed record EmpresasDoUsuario(IReadOnlyList<EmpresaDisponivel> Empresas, int? PreSelecionada);

/// <summary>
/// Login, troca de senha e montagem da identidade (claims) por empresa — T00, docs/processos/P02-autenticacao-permissoes.md.
/// USUARIOS, USUARIOS_EMPRESAS e EMPRESAS não têm filtro de empresa; as permissões são lidas da empresa escolhida
/// depois de conferir o vínculo (única exceção ao filtro global fora de migração/admin: ainda não há empresa ativa).
/// </summary>
public sealed class ContaService(LabDbContext db, IAuditoria auditoria, TimeProvider relogio)
{
    public const string MensagemLoginInvalido = "Usuário ou senha inválidos.";
    public const int MaxTentativas = 5;
    public static readonly TimeSpan TempoBloqueio = TimeSpan.FromMinutes(15);

    private static readonly PasswordHasher<Usuario> Hasher = new();
    // usuário inexistente também passa por uma verificação PBKDF2: o tempo de resposta não revela se o login existe
    private static readonly string HashFicticio = Hasher.HashPassword(new Usuario(), Guid.NewGuid().ToString());

    private DateTime Agora => relogio.GetLocalNow().DateTime;

    public static string Normalizar(string? login) => (login ?? "").Trim().ToUpperInvariant();

    public static string GerarHash(string senha) => Hasher.HashPassword(new Usuario(), senha);

    /// <summary>Confere login e senha (bloqueio após 5 falhas por 15 min). Devolve o ID do usuário.</summary>
    public async Task<Resultado<int>> EntrarAsync(string? login, string? senha)
    {
        var codigo = Normalizar(login);
        var usu = codigo.Length == 0 ? null : await db.Usuarios.AsNoTracking().SingleOrDefaultAsync(u => u.Login == codigo);

        var verificacao = Hasher.VerifyHashedPassword(usu ?? new Usuario(), usu?.SenhaHash ?? HashFicticio, senha ?? "");
        var motivo = usu switch
        {
            null => "INEXISTENTE",
            { Ativo: false } => "INATIVO",
            { BloqueadoAte: { } ate } when ate > Agora => "BLOQUEADO",
            _ when verificacao == PasswordVerificationResult.Failed => "SENHA",
            _ => null,
        };

        if (motivo is not null)
        {
            if (motivo == "SENHA") await RegistrarFalhaAsync(usu!.IdUsuarios);
            await auditoria.RegistrarAsync(AcoesAuditoria.Login, "USUARIOS", usu?.IdUsuarios.ToString() ?? Truncar("LOGIN:" + codigo),
                depois: new { SUCESSO = false, MOTIVO = motivo, LOGIN = codigo });
            return Resultado.Erro(MensagemLoginInvalido);
        }

        // sucesso: zera tentativas e grava o acesso (SQL direto: o evento L abaixo já registra o login)
        var novoHash = verificacao == PasswordVerificationResult.SuccessRehashNeeded ? GerarHash(senha!) : usu!.SenhaHash;
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE USUARIOS SET TENTATIVAS_FALHAS = 0, BLOQUEADO_ATE = NULL, ULTIMO_ACESSO = {Agora}, SENHA_HASH = {novoHash}
             WHERE ID_USUARIOS = {usu!.IdUsuarios}
            """);
        await auditoria.RegistrarAsync(AcoesAuditoria.Login, "USUARIOS", usu.IdUsuarios.ToString(),
            depois: new { SUCESSO = true, LOGIN = codigo });
        return Resultado.Ok(usu.IdUsuarios);
    }

    // Um UPDATE só (sem ler-e-gravar): logins errados simultâneos não perdem incremento.
    // O MySQL avalia o SET da esquerda para a direita: BLOQUEADO_ATE usa o contador ANTES do incremento.
    private Task RegistrarFalhaAsync(int idUsuario) => db.Database.ExecuteSqlInterpolatedAsync($"""
        UPDATE USUARIOS
           SET BLOQUEADO_ATE = IF(TENTATIVAS_FALHAS + 1 >= {MaxTentativas}, {Agora + TempoBloqueio}, BLOQUEADO_ATE),
               TENTATIVAS_FALHAS = IF(TENTATIVAS_FALHAS + 1 >= {MaxTentativas}, 0, TENTATIVAS_FALHAS + 1)
         WHERE ID_USUARIOS = {idUsuario}
        """);

    /// <summary>Troca a senha (mín. 8, letra + número, diferente da atual) e desliga a troca obrigatória.</summary>
    public async Task<Resultado> TrocarSenhaAsync(int idUsuario, string? atual, string? nova, string? confirmacao)
    {
        var usu = await db.Usuarios.SingleAsync(u => u.IdUsuarios == idUsuario);
        if (Hasher.VerifyHashedPassword(usu, usu.SenhaHash, atual ?? "") == PasswordVerificationResult.Failed)
            return Resultado.Erro("Senha atual incorreta.", "SenhaAtual");
        if (ValidarNovaSenha(nova) is { } erro) return Resultado.Erro(erro, "NovaSenha");
        if (nova != confirmacao) return Resultado.Erro("A confirmação não confere com a nova senha.", "Confirmacao");
        if (nova == atual) return Resultado.Erro("A nova senha deve ser diferente da atual.", "NovaSenha");

        usu.SenhaHash = GerarHash(nova!);
        usu.TrocarSenha = false;
        await db.SaveChangesAsync(); // USUARIOS é auditada: A com SENHA_HASH = "***"
        return Resultado.Ok();
    }

    public static string? ValidarNovaSenha(string? senha) =>
        senha is null || senha.Length < 8 || !senha.Any(char.IsLetter) || !senha.Any(char.IsDigit)
            ? "A senha deve ter pelo menos 8 caracteres, com letras e números."
            : null;

    /// <summary>Empresas que o usuário pode usar (vínculo e empresa ativos; ADMIN_GLOBAL: todas as ativas).</summary>
    public async Task<EmpresasDoUsuario> EmpresasDoUsuarioAsync(int idUsuario)
    {
        var usu = await db.Usuarios.AsNoTracking().SingleAsync(u => u.IdUsuarios == idUsuario);
        var vinculos = await db.UsuariosEmpresas.AsNoTracking()
            .Where(v => v.IdUsuarios == idUsuario && v.Ativo && v.Empresa.Ativa)
            .Select(v => new { v.IdEmpresas, v.IdEntidadesFunc, v.Padrao })
            .ToListAsync();

        var empresas = await db.Empresas.AsNoTracking()
            .Where(e => e.Ativa)
            .OrderBy(e => e.CodEmpresa)
            .Select(e => new { e.IdEmpresas, e.CodEmpresa, Nome = e.NomeFantasia ?? e.RazaoSocial })
            .ToListAsync();

        var lista = empresas
            .Where(e => usu.AdminGlobal || vinculos.Any(v => v.IdEmpresas == e.IdEmpresas))
            .Select(e => new EmpresaDisponivel(e.IdEmpresas, e.CodEmpresa, e.Nome,
                vinculos.FirstOrDefault(v => v.IdEmpresas == e.IdEmpresas)?.IdEntidadesFunc))
            .ToList();

        int? pre = lista.Any(e => e.IdEmpresas == usu.IdEmpresasUltima) ? usu.IdEmpresasUltima
                 : vinculos.FirstOrDefault(v => v.Padrao && lista.Any(e => e.IdEmpresas == v.IdEmpresas))?.IdEmpresas;
        return new EmpresasDoUsuario(lista, pre ?? lista.FirstOrDefault()?.IdEmpresas);
    }

    /// <summary>
    /// Identidade do cookie. Sem <paramref name="idEmpresa"/>: só o usuário (troca de senha / escolha de empresa).
    /// Com empresa: confere o acesso e grava empresa, profissional e permissões (união dos perfis ativos na empresa).
    /// </summary>
    public async Task<Resultado<ClaimsPrincipal>> MontarIdentidadeAsync(int idUsuario, int? idEmpresa = null)
    {
        var usu = await db.Usuarios.AsNoTracking().SingleAsync(u => u.IdUsuarios == idUsuario);
        var claims = new List<Claim>
        {
            new(LabClaims.UsuarioId, usu.IdUsuarios.ToString()),
            new(ClaimTypes.Name, usu.Nome),
            new(LabClaims.Login, usu.Login),
            new(LabClaims.Instancia, ValidarSessao.Instancia),
        };
        if (usu.AdminGlobal) claims.Add(new(LabClaims.AdminGlobal, "1"));
        if (usu.TrocarSenha) claims.Add(new(LabClaims.TrocarSenha, "1"));

        if (idEmpresa is { } emp)
        {
            if ((await EmpresasDoUsuarioAsync(idUsuario)).Empresas.FirstOrDefault(e => e.IdEmpresas == emp) is not { } escolhida)
                return Resultado.Erro("Você não tem acesso a esta empresa.");

            claims.Add(new(LabClaims.EmpresaId, emp.ToString()));
            claims.Add(new(LabClaims.EmpresaCodigo, escolhida.Codigo));
            claims.Add(new(LabClaims.EmpresaNome, escolhida.Nome));
            if (escolhida.ProfissionalId is { } func) claims.Add(new(LabClaims.ProfissionalId, func.ToString()));

            // exceção ao filtro global (ainda não há empresa ativa): empresa conferida acima e fixada no Where
            var permissoes = await db.PerfisPermissoes.IgnoreQueryFilters().AsNoTracking()
                .Where(pp => pp.Perfil.IdEmpresas == emp && pp.Perfil.Ativo
                          && db.UsuariosPerfis.Any(up => up.IdUsuarios == idUsuario && up.IdEmpresas == emp && up.IdPerfis == pp.IdPerfis))
                .Select(pp => pp.Permissao.Codigo)
                .Distinct()
                .ToListAsync();
            claims.AddRange(permissoes.Order().Select(p => new Claim(LabClaims.Permissao, p)));

            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE USUARIOS SET ID_EMPRESAS_ULTIMA = {emp} WHERE ID_USUARIOS = {idUsuario}");
        }

        var identidade = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
        return Resultado.Ok(new ClaimsPrincipal(identidade));
    }

    /// <summary>
    /// Sessão ainda válida? Usuário ativo, ADMIN_GLOBAL igual ao do cookie e, com empresa, acesso ainda ativo
    /// (revalidação a cada request). ADMIN_GLOBAL retirado (ou dado) derruba a sessão: a claim vale para tudo
    /// (inclusive para se promover de novo), então não pode sobreviver à mudança até a próxima entrada.
    /// </summary>
    public async Task<bool> SessaoValidaAsync(int idUsuario, int? idEmpresa, bool adminGlobalNoCookie)
    {
        var usu = await db.Usuarios.AsNoTracking().Where(u => u.IdUsuarios == idUsuario)
            .Select(u => new { u.Ativo, u.AdminGlobal }).SingleOrDefaultAsync();
        if (usu is not { Ativo: true } || usu.AdminGlobal != adminGlobalNoCookie) return false;
        if (idEmpresa is not { } emp) return true;
        return usu.AdminGlobal
            ? await db.Empresas.AnyAsync(e => e.IdEmpresas == emp && e.Ativa)
            : await db.UsuariosEmpresas.AnyAsync(v => v.IdUsuarios == idUsuario && v.IdEmpresas == emp && v.Ativo && v.Empresa.Ativa);
    }

    private static string Truncar(string s) => s.Length <= 60 ? s : s[..60];
}
