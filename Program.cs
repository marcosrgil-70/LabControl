using LabControl.Data;
using LabControl.Filters;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<SessaoFilter>();
});

// MySQL via EF Core (Pomelo)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Sessão para autenticação simples
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Cria tabelas novas que podem não existir ainda no banco
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LabControl.Data.ApplicationDbContext>();
    // Remove coluna FANTASIA da tabela ENTIDADES, caso exista (campo substituído por
    // ENTIDADES_PJ.NOME_FANTASIA e ENTIDADES_PF.SOBRENOME)
    try
    {
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE ENTIDADES DROP COLUMN FANTASIA");
    }
    catch (Exception ex) when (ex.Message.Contains("check that") || ex.Message.Contains("1091"))
    {
        // Coluna já não existe — ok
    }

    await db.Database.ExecuteSqlRawAsync(@"
        CREATE TABLE IF NOT EXISTS ENTIDADES_OBSERVACOES (
            ID_ENTIDADES INT NOT NULL PRIMARY KEY,
            OBSERVACAO   TEXT NULL,
            FOREIGN KEY (ID_ENTIDADES) REFERENCES ENTIDADES(ID_ENTIDADES) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci");

    // Tabelas de apoio ao cadastro de Funcionários
    await db.Database.ExecuteSqlRawAsync(@"
        CREATE TABLE IF NOT EXISTS CARGO_FUNCIONARIOS (
            ID_CARGO_FUNCIONARIOS INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
            DESCRICAO VARCHAR(50) NOT NULL
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci");

    await db.Database.ExecuteSqlRawAsync(@"
        INSERT IGNORE INTO CARGO_FUNCIONARIOS (ID_CARGO_FUNCIONARIOS, DESCRICAO) VALUES
        (1, 'Farmacêutica'), (2, 'Diretora Técnica'), (3, 'Diretor Geral'),
        (4, 'Diretora Científica'), (5, 'Estagiário'), (6, 'Comercial'),
        (7, 'Financeiro(a)'), (8, 'Técnico(a)'), (9, 'Analista de Laboratório')");

    // Adiciona coluna ID_CARGO_FUNCIONARIOS em ENTIDADES_FUNCIONARIOS, caso não exista
    try
    {
        await db.Database.ExecuteSqlRawAsync(
            "ALTER TABLE ENTIDADES_FUNCIONARIOS ADD COLUMN ID_CARGO_FUNCIONARIOS INT NULL, " +
            "ADD FOREIGN KEY (ID_CARGO_FUNCIONARIOS) REFERENCES CARGO_FUNCIONARIOS(ID_CARGO_FUNCIONARIOS)");
    }
    catch (Exception ex) when (ex.Message.Contains("Duplicate column") || ex.Message.Contains("1060"))
    {
        // Coluna já existe — ok
    }

    await db.Database.ExecuteSqlRawAsync(@"
        CREATE TABLE IF NOT EXISTS ENTIDADES_FUNC_ASSINATURAS (
            ID_ENTIDADES_FUNC INT NOT NULL PRIMARY KEY,
            ASSINATURA_DIGITAL LONGBLOB NULL,
            MD5_ASSINATURA VARCHAR(32) NULL,
            FOREIGN KEY (ID_ENTIDADES_FUNC) REFERENCES ENTIDADES_FUNCIONARIOS(ID_ENTIDADES) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci");

    // ── Sistema de Perfis e Permissões ────────────────────────────────────────

    // Adiciona coluna USULOG (código/login do usuário) em USUARIO
    try
    {
        await db.Database.ExecuteSqlRawAsync(
            "ALTER TABLE USUARIO ADD COLUMN USULOG VARCHAR(30) NULL AFTER USUCOD");
        // Popula a partir do nome existente (compatibilidade com usuários migrados)
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE USUARIO SET USULOG = USUNOM WHERE USULOG IS NULL OR USULOG = ''");
    }
    catch (Exception ex) when (ex.Message.Contains("Duplicate column") || ex.Message.Contains("1060"))
    {
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE USUARIO SET USULOG = USUNOM WHERE USULOG IS NULL OR USULOG = ''");
    }

    await db.Database.ExecuteSqlRawAsync(@"
        CREATE TABLE IF NOT EXISTS PERMISSOES (
            ID_PERMISSOES  INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
            COD_PERMISSAO  VARCHAR(30) NOT NULL,
            DSC_PERMISSAO  VARCHAR(100) NOT NULL,
            UNIQUE KEY UK_COD_PERMISSAO (COD_PERMISSAO)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci");

    // Seed das permissões (telas do sistema)
    await db.Database.ExecuteSqlRawAsync(@"
        INSERT IGNORE INTO PERMISSOES (COD_PERMISSAO, DSC_PERMISSAO) VALUES
        ('Clientes',          'Clientes'),
        ('Funcionarios',      'Funcionários'),
        ('Produtos',          'Produtos'),
        ('Parametros',        'Parâmetros de Análise'),
        ('TabelasAuxiliares', 'Tabelas Auxiliares'),
        ('HistAmostras',      'Amostras'),
        ('Propostas',         'Propostas'),
        ('Resultados',        'Resultados'),
        ('Boletins',          'Boletins'),
        ('Relatorios',        'Relatórios'),
        ('Usuarios',          'Usuários'),
        ('Perfis',            'Perfis'),
        ('Permissoes',        'Permissões')");

    await db.Database.ExecuteSqlRawAsync(@"
        CREATE TABLE IF NOT EXISTS PERFIS (
            ID_PERFIS  INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
            COD_PERFIL VARCHAR(20) NOT NULL,
            DSC_PERFIL VARCHAR(80) NOT NULL,
            UNIQUE KEY UK_COD_PERFIL (COD_PERFIL)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci");

    await db.Database.ExecuteSqlRawAsync(@"
        CREATE TABLE IF NOT EXISTS PERFIS_PERMISSOES (
            ID_PERFIS     INT NOT NULL,
            ID_PERMISSOES INT NOT NULL,
            INCLUIR       TINYINT(1) NOT NULL DEFAULT 0,
            ALTERAR       TINYINT(1) NOT NULL DEFAULT 0,
            CONSULTAR     TINYINT(1) NOT NULL DEFAULT 0,
            EXCLUIR       TINYINT(1) NOT NULL DEFAULT 0,
            IMPRIMIR      TINYINT(1) NOT NULL DEFAULT 0,
            PRIMARY KEY (ID_PERFIS, ID_PERMISSOES),
            FOREIGN KEY (ID_PERFIS)     REFERENCES PERFIS(ID_PERFIS)         ON DELETE CASCADE,
            FOREIGN KEY (ID_PERMISSOES) REFERENCES PERMISSOES(ID_PERMISSOES) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci");

    await db.Database.ExecuteSqlRawAsync(@"
        CREATE TABLE IF NOT EXISTS USUARIOS_PERFIS (
            USUCOD    INT NOT NULL,
            ID_PERFIS INT NOT NULL,
            PRIMARY KEY (USUCOD, ID_PERFIS),
            FOREIGN KEY (USUCOD)    REFERENCES USUARIO(USUCOD)    ON DELETE CASCADE,
            FOREIGN KEY (ID_PERFIS) REFERENCES PERFIS(ID_PERFIS)  ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci");
}

app.Run();
