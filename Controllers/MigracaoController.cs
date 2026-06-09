using FirebirdSql.Data.FirebirdClient;
using LabControl.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;

namespace LabControl.Controllers;

public class MigResult
{
    public string Tabela { get; init; } = string.Empty;
    public int Inseridos { get; set; }
    public int Erros { get; set; }
    public string? Detalhe { get; set; }
    public bool Ok => Detalhe == null;
}

public class MigracaoController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public MigracaoController(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    private string FdbPath    => _config["Firebird:DatabasePath"] ?? @"C:\Project\Laboratorio\DBMAIS.FDB";
    private string FbUser     => _config["Firebird:User"]         ?? "SYSDBA";
    private string FbPassword => _config["Firebird:Password"]     ?? "masterkey";

    private async Task<FbConnection> AbrirFbAsync()
    {
        var modos = new[]
        {
            $"User ID={FbUser};Password={FbPassword};Database={FdbPath};Charset=ISO8859_1;ServerType=1;Pooling=false",
            $"User ID={FbUser};Password={FbPassword};DataSource=localhost;Port=3050;Database={FdbPath};Charset=ISO8859_1;ServerType=0;Pooling=false",
        };
        Exception? ultimo = null;
        foreach (var cs in modos)
        {
            try { var c = new FbConnection(cs); await c.OpenAsync(); return c; }
            catch (Exception ex) { ultimo = ex; }
        }
        throw ultimo!;
    }

    // ─── GET ──────────────────────────────────────────────────────────────────

    public IActionResult Index()
    {
        ViewBag.FdbPath = FdbPath;
        return View();
    }

    // ─── GET: Ler usuários do Firebird (para diagnóstico) ────────────────────

    public async Task<IActionResult> LerUsuariosFb()
    {
        try
        {
            using var fb = await AbrirFbAsync();
            using var cmd = new FbCommand("SELECT USUCOD, USUNOM, USUSEN, USUADM FROM USUARIO ORDER BY USUNOM", fb);
            using var reader = await cmd.ExecuteReaderAsync();
            var lista = new List<object>();
            while (await reader.ReadAsync())
            {
                lista.Add(new
                {
                    id    = reader.GetInt32(0),
                    nome  = reader.IsDBNull(1) ? "" : reader.GetString(1).Trim(),
                    senha = reader.IsDBNull(2) ? "" : reader.GetString(2).Trim(),
                    admin = !reader.IsDBNull(3) && reader.GetInt32(3) != 0
                });
            }
            return Json(lista);
        }
        catch (Exception ex)
        {
            return Json(new { erro = ex.Message });
        }
    }

    // ─── POST: Testar conexão ─────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> Testar()
    {
        try
        {
            using var fb = await AbrirFbAsync();
            var mapa = new[]
            {
                ("Amostras Status",        "AMOSTRAS_STATUS"),
                ("Amostras Tipo",          "AMOSTRAS_TIPO"),
                ("Análises Métodos",       "ANALISES_METODOS"),
                ("Análises Status",        "ANALISES_STATUS"),
                ("Análises Tipo",          "ANALISES_TIPO"),
                ("Boletins Status",        "BOLETINS_STATUS"),
                ("Embalagens Tipos",       "EMBALAGENS_TIPOS"),
                ("End. Tipos",             "ENDERECOS_TIPOS"),
                ("Fones Tipos",            "FONES_TIPOS"),
                ("Idiomas",                "IDIOMAS"),
                ("Unidades",               "UNIDADES"),
                ("Entidades",              "ENTIDADES"),
                ("Entidades PF",           "ENTIDADES_PF"),
                ("Entidades PJ",           "ENTIDADES_PJ"),
                ("Entidades Funcionários", "ENTIDADES_FUNCIONARIOS"),
                ("Fones",                  "ENTIDADES_FONES"),
                ("E-mails",                "ENTIDADES_EMAILS"),
                ("Endereços",              "ENTIDADES_ENDERECOS"),
                ("Usuários",               "USUARIO"),
            };
            var tabelas = new List<object>();
            foreach (var (nome, tabela) in mapa)
            {
                try
                {
                    using var cmd = new FbCommand($"SELECT COUNT(*) FROM {tabela}", fb);
                    var count = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
                    tabelas.Add(new { nome, count, existe = true });
                }
                catch { tabelas.Add(new { nome, count = 0, existe = false }); }
            }
            return Json(new { sucesso = true, mensagem = "Conexão OK!", tabelas });
        }
        catch (Exception ex)
        {
            return Json(new { sucesso = false, mensagem = ex.Message, tabelas = Array.Empty<object>() });
        }
    }

    // ─── POST: Executar migração ──────────────────────────────────────────────
    // Usa 1 transação MySQL + comandos preparados reutilizáveis → muito mais rápido

    [HttpPost]
    public async Task<IActionResult> Executar([FromForm] bool limpar)
    {
        var log = new List<MigResult>();
        try
        {
            using var fb = await AbrirFbAsync();

            var mysql = _db.Database.GetDbConnection();
            if (mysql.State != ConnectionState.Open)
                await mysql.OpenAsync();

            await using var tx = await mysql.BeginTransactionAsync();
            try
            {
                // ── Tabelas de lookup ──────────────────────────────────────────
                log.Add(await Mig(fb, mysql, tx, "Amostras Status", "AMOSTRAS_STATUS",
                    "SELECT ID_AMOSTRAS_STATUS,DESCRICAO,COR FROM AMOSTRAS_STATUS", limpar,
                    "INSERT IGNORE INTO AMOSTRAS_STATUS (ID_AMOSTRAS_STATUS,DESCRICAO,COR) VALUES (@p0,@p1,@p2)",
                    r => new object?[] { r.GetInt32(0), S(r,1), N(r,2) }));

                log.Add(await Mig(fb, mysql, tx, "Amostras Tipo", "AMOSTRAS_TIPO",
                    "SELECT ID_AMOSTRAS_TIPO,DESCRICAO FROM AMOSTRAS_TIPO", limpar,
                    "INSERT IGNORE INTO AMOSTRAS_TIPO (ID_AMOSTRAS_TIPO,DESCRICAO) VALUES (@p0,@p1)",
                    r => new object?[] { r.GetInt32(0), S(r,1) }));

                log.Add(await Mig(fb, mysql, tx, "Análises Métodos", "ANALISES_METODOS",
                    "SELECT ID_ANALISES_METODOS,DESCRICAO FROM ANALISES_METODOS", limpar,
                    "INSERT IGNORE INTO ANALISES_METODOS (ID_ANALISES_METODOS,DESCRICAO) VALUES (@p0,@p1)",
                    r => new object?[] { r.GetInt32(0), S(r,1) }));

                log.Add(await Mig(fb, mysql, tx, "Análises Status", "ANALISES_STATUS",
                    "SELECT ID_ANALISES_STATUS,DESCRICAO,COR FROM ANALISES_STATUS", limpar,
                    "INSERT IGNORE INTO ANALISES_STATUS (ID_ANALISES_STATUS,DESCRICAO,COR) VALUES (@p0,@p1,@p2)",
                    r => new object?[] { r.GetInt32(0), S(r,1), N(r,2) }));

                log.Add(await Mig(fb, mysql, tx, "Análises Tipo", "ANALISES_TIPO",
                    "SELECT ID_ANALISES_TIPO,DESCRICAO FROM ANALISES_TIPO", limpar,
                    "INSERT IGNORE INTO ANALISES_TIPO (ID_ANALISES_TIPO,DESCRICAO) VALUES (@p0,@p1)",
                    r => new object?[] { r.GetInt32(0), S(r,1) }));

                log.Add(await Mig(fb, mysql, tx, "Boletins Status", "BOLETINS_STATUS",
                    "SELECT ID_BOLETINS_STATUS,DESCRICAO,COR FROM BOLETINS_STATUS", limpar,
                    "INSERT IGNORE INTO BOLETINS_STATUS (ID_BOLETINS_STATUS,DESCRICAO,COR) VALUES (@p0,@p1,@p2)",
                    r => new object?[] { r.GetInt32(0), S(r,1), N(r,2) }));

                log.Add(await Mig(fb, mysql, tx, "Embalagens Tipos", "EMBALAGENS_TIPOS",
                    "SELECT ID_EMBALAGENS_TIPOS,DESCRICAO FROM EMBALAGENS_TIPOS", limpar,
                    "INSERT IGNORE INTO EMBALAGENS_TIPOS (ID_EMBALAGENS_TIPOS,DESCRICAO) VALUES (@p0,@p1)",
                    r => new object?[] { r.GetInt32(0), S(r,1) }));

                log.Add(await Mig(fb, mysql, tx, "Endereços Tipos", "ENDERECOS_TIPOS",
                    "SELECT ID_ENDERECOS_TIPOS,DESCRICAO FROM ENDERECOS_TIPOS", limpar,
                    "INSERT IGNORE INTO ENDERECOS_TIPOS (ID_ENDERECOS_TIPOS,DESCRICAO) VALUES (@p0,@p1)",
                    r => new object?[] { r.GetInt32(0), S(r,1) }));

                log.Add(await Mig(fb, mysql, tx, "Fones Tipos", "FONES_TIPOS",
                    "SELECT ID_FONES_TIPOS,DESCRICAO FROM FONES_TIPOS", limpar,
                    "INSERT IGNORE INTO FONES_TIPOS (ID_FONES_TIPOS,DESCRICAO) VALUES (@p0,@p1)",
                    r => new object?[] { r.GetInt32(0), S(r,1) }));

                log.Add(await Mig(fb, mysql, tx, "Idiomas", "IDIOMAS",
                    "SELECT ID_IDIOMAS,DESCRICAO FROM IDIOMAS", limpar,
                    "INSERT IGNORE INTO IDIOMAS (ID_IDIOMAS,DESCRICAO) VALUES (@p0,@p1)",
                    r => new object?[] { r.GetInt32(0), S(r,1) }));

                log.Add(await Mig(fb, mysql, tx, "Unidades", "UNIDADES",
                    "SELECT ID_UNIDADES,DESCRICAO,SIGLA FROM UNIDADES", limpar,
                    "INSERT IGNORE INTO UNIDADES (ID_UNIDADES,DESCRICAO,SIGLA) VALUES (@p0,@p1,@p2)",
                    r => new object?[] { r.GetInt32(0), S(r,1), S(r,2) }));

                // ── Entidades (base) ───────────────────────────────────────────
                log.Add(await Mig(fb, mysql, tx, "Entidades", "ENTIDADES",
                    "SELECT ID_ENTIDADES,CATEGORIA,DATA_CADASTRO,NOME,INATIVO FROM ENTIDADES", limpar,
                    "INSERT IGNORE INTO ENTIDADES (ID_ENTIDADES,CATEGORIA,DATA_CADASTRO,NOME,INATIVO,TIPO_CLIENTE,TIPO_FORNECEDOR,TIPO_VENDEDOR,TIPO_FUNCIONARIO,TIPO_EMPRESA_USUARIA) VALUES (@p0,@p1,@p2,@p3,@p4,0,0,0,0,0)",
                    r => new object?[] {
                        r.GetInt32(0), S(r,1),
                        r.IsDBNull(2) ? DateTime.Now : r.GetDateTime(2),
                        S(r,3),
                        (!r.IsDBNull(4) && S(r,4) == "S") ? 1 : 0
                    }));

                // ── Tipos de entidade → tenta ENTIDADES_TIPOS, depois ENTIDADES_TIPO ─
                var sqlTipos = await TabelaExisteAsync(fb, "ENTIDADES_TIPOS")
                    ? "SELECT ID_ENTIDADES,CLIENTE,FORNECEDOR,VENDEDOR,FUNCIONARIO,EMPRESA FROM ENTIDADES_TIPOS"
                    : await TabelaExisteAsync(fb, "ENTIDADES_TIPO")
                        ? "SELECT ID_ENTIDADES,CLIENTE,FORNECEDOR,VENDEDOR,FUNCIONARIO,EMPRESA FROM ENTIDADES_TIPO"
                        : null;

                if (sqlTipos != null)
                {
                    log.Add(await Mig(fb, mysql, tx, "Tipos de Entidade", "ENTIDADES_TIPOS",
                        sqlTipos, false,
                        "UPDATE ENTIDADES SET TIPO_CLIENTE=@p1,TIPO_FORNECEDOR=@p2,TIPO_VENDEDOR=@p3,TIPO_FUNCIONARIO=@p4,TIPO_EMPRESA_USUARIA=@p5 WHERE ID_ENTIDADES=@p0",
                        r => new object?[] {
                            r.GetInt32(0), B(r,1)?1:0, B(r,2)?1:0, B(r,3)?1:0, B(r,4)?1:0, B(r,5)?1:0
                        }));
                }
                else
                {
                    log.Add(new MigResult { Tabela = "Tipos de Entidade", Inseridos = 0,
                        Detalhe = "Tabela não encontrada no banco original (tipos serão configurados manualmente)" });
                }

                // ── Pessoas Físicas ────────────────────────────────────────────
                log.Add(await Mig(fb, mysql, tx, "Entidades PF", "ENTIDADES_PF",
                    "SELECT ID_ENTIDADES,CPF,NOME,SOBRENOME,SEXO,DATA_NASCIMENTO,RG,ESTADO_CIVIL FROM ENTIDADES_PF",
                    limpar,
                    "INSERT IGNORE INTO ENTIDADES_PF (ID_ENTIDADES,CPF,NOME,SOBRENOME,SEXO,DATA_NASCIMENTO,RG,ESTADO_CIVIL) VALUES (@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7)",
                    r =>
                    {
                        var ecInt = r.IsDBNull(7) ? (int?)null : r.GetInt32(7);
                        var ec = ecInt switch { 0=>"S", 1=>"C", 2=>"D", 3=>"P", 4=>"V", _=> null };
                        return new object?[] {
                            r.GetInt32(0), N(r,1), S(r,2), N(r,3), N(r,4),
                            r.IsDBNull(5) ? null : r.GetDateTime(5),
                            N(r,6), ec
                        };
                    }));

                // ── Pessoas Jurídicas ──────────────────────────────────────────
                log.Add(await Mig(fb, mysql, tx, "Entidades PJ", "ENTIDADES_PJ",
                    "SELECT ID_ENTIDADES,CNPJ,NOME_FANTASIA,INSC_ESTADUAL,INSC_MUNICIPAL FROM ENTIDADES_PJ",
                    limpar,
                    "INSERT IGNORE INTO ENTIDADES_PJ (ID_ENTIDADES,CNPJ,NOME_FANTASIA,INSC_ESTADUAL,INSC_MUNICIPAL) VALUES (@p0,@p1,@p2,@p3,@p4)",
                    r => new object?[] { r.GetInt32(0), N(r,1), N(r,2), N(r,3), N(r,4) }));

                // ── Funcionários (coluna de tipo pode variar entre versões do FDB)
                log.Add(await Mig(fb, mysql, tx, "Entidades Funcionários", "ENTIDADES_FUNCIONARIOS",
                    "SELECT ID_ENTIDADES,NR_REGISTRO_PROFISSIONAL FROM ENTIDADES_FUNCIONARIOS",
                    limpar,
                    "INSERT IGNORE INTO ENTIDADES_FUNCIONARIOS (ID_ENTIDADES,ID_TIPOS_REG_PROFISSIONAL,NR_REGISTRO_PROFISSIONAL) VALUES (@p0,NULL,@p1)",
                    r => new object?[] { r.GetInt32(0), N(r,1) }));

                // ── Fones ──────────────────────────────────────────────────────
                log.Add(await Mig(fb, mysql, tx, "Fones", "ENTIDADES_FONES",
                    "SELECT ID_ENTIDADES_FONES,ID_ENTIDADES,ID_FONES_TIPOS,DDD,FONE FROM ENTIDADES_FONES",
                    limpar,
                    "INSERT IGNORE INTO ENTIDADES_FONES (ID_ENTIDADES_FONES,ID_ENTIDADES,ID_FONES_TIPOS,DDD,FONE) VALUES (@p0,@p1,@p2,@p3,@p4)",
                    r => new object?[] {
                        r.GetInt32(0), r.GetInt32(1),
                        r.IsDBNull(2) ? null : (object)r.GetInt32(2),
                        N(r,3), N(r,4)
                    }));

                // ── E-mails ────────────────────────────────────────────────────
                log.Add(await Mig(fb, mysql, tx, "E-mails", "ENTIDADES_EMAILS",
                    "SELECT ID_ENTIDADES_EMAILS,ID_ENTIDADES,PRINCIPAL,EMAIL FROM ENTIDADES_EMAILS",
                    limpar,
                    "INSERT IGNORE INTO ENTIDADES_EMAILS (ID_ENTIDADES_EMAILS,ID_ENTIDADES,PRINCIPAL,EMAIL) VALUES (@p0,@p1,@p2,@p3)",
                    r => new object?[] { r.GetInt32(0), r.GetInt32(1), B(r,2)?1:0, S(r,3) }));

                // ── Endereços (JOIN para desnormalizar logradouro) ─────────────
                log.Add(await Mig(fb, mysql, tx, "Endereços", "ENTIDADES_ENDERECOS",
                    @"SELECT ee.ID_ENTIDADES_ENDERECOS, ee.ID_ENTIDADES, ee.ID_ENDERECOS_TIPOS,
                        TRIM(COALESCE(lt.ABREVIATURA,'') || ' ' || COALESCE(l.NOME_LOGRADOURO,'')) AS LOGRADOURO,
                        ee.NUMERO, ee.COMPLEMENTO, cb.DESCRICAO AS BAIRRO,
                        c.NOME AS CIDADE, e.SIGLA AS UF, ee.CEP
                      FROM ENTIDADES_ENDERECOS ee
                      LEFT JOIN LOGRADOUROS l       ON l.ID_LOGRADOUROS       = ee.ID_LOGRADOUROS
                      LEFT JOIN LOGRADOUROS_TIPOS lt ON lt.ID_LOGRADOUROS_TIPOS = l.ID_LOGRADOUROS_TIPOS
                      LEFT JOIN CIDADES_BAIRROS cb   ON cb.ID_CIDADES_BAIRROS   = l.ID_CIDADES_BAIRROS
                      LEFT JOIN CIDADES c             ON c.ID_CIDADES           = cb.ID_CIDADES
                      LEFT JOIN ESTADOS e             ON e.ID_ESTADOS           = c.ID_ESTADOS",
                    limpar,
                    "INSERT IGNORE INTO ENTIDADES_ENDERECOS (ID_ENTIDADES_ENDERECOS,ID_ENTIDADES,ID_ENDERECOS_TIPOS,LOGRADOURO,NUMERO,COMPLEMENTO,BAIRRO,CIDADE,UF,CEP) VALUES (@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9)",
                    r => new object?[] {
                        r.GetInt32(0), r.GetInt32(1),
                        r.IsDBNull(2) ? null : (object)r.GetInt32(2),
                        N(r,3), N(r,4), N(r,5), N(r,6), N(r,7), N(r,8), N(r,9)
                    }));

                // ── Usuários: senha padrão = nome em minúsculas ────────────────
                // (senha original do Firebird é binária/criptografada, não recuperável)
                log.Add(await Mig(fb, mysql, tx, "Usuários", "USUARIO",
                    "SELECT USUCOD,USUNOM,USUADM FROM USUARIO",
                    limpar,
                    "INSERT IGNORE INTO USUARIO (USUCOD,USUNOM,USUSEN,USUADM,INATIVO) VALUES (@p0,@p1,@p2,@p3,0)",
                    r =>
                    {
                        var nome  = S(r, 1);
                        var senha = Sha256(nome.ToLower()); // senha = nome em minúsculas
                        return new object?[] {
                            r.GetInt32(0), nome, senha,
                            (!r.IsDBNull(2) && r.GetInt32(2) != 0) ? 1 : 0
                        };
                    }));

                // ── Inferir tipos de entidade pelas tabelas PF/PJ/FUNCIONARIOS ──
                // (substitui ENTIDADES_TIPOS que não existia neste FDB)
                log.Add(await InferirTipos(mysql, tx));

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            log.Insert(0, new MigResult { Tabela = "CONEXÃO", Detalhe = ex.Message });
        }

        return Json(log);
    }

    // ─── Helper principal ─────────────────────────────────────────────────────

    private static async Task<MigResult> Mig(
        FbConnection fb,
        DbConnection mysql,
        DbTransaction tx,
        string nome,
        string tabela,
        string selectSql,
        bool limpar,
        string insertSql,
        Func<FbDataReader, object?[]> getValues)
    {
        var res = new MigResult { Tabela = nome };
        try
        {
            if (limpar)
            {
                using var del = mysql.CreateCommand();
                del.Transaction = tx;
                del.CommandText = $"DELETE FROM {tabela}";
                await del.ExecuteNonQueryAsync();
            }

            using var fbCmd    = new FbCommand(selectSql, fb);
            using var reader   = (FbDataReader)await fbCmd.ExecuteReaderAsync();
            using var insCmd   = mysql.CreateCommand();
            insCmd.Transaction = tx;
            insCmd.CommandText = insertSql;

            bool pronto = false;
            while (await reader.ReadAsync())
            {
                try
                {
                    var vals = getValues(reader);
                    if (!pronto)
                    {
                        for (int i = 0; i < vals.Length; i++)
                        {
                            var p = insCmd.CreateParameter();
                            p.ParameterName = $"@p{i}";
                            insCmd.Parameters.Add(p);
                        }
                        pronto = true;
                    }
                    for (int i = 0; i < vals.Length; i++)
                        insCmd.Parameters[i].Value = vals[i] ?? DBNull.Value;

                    await insCmd.ExecuteNonQueryAsync();
                    res.Inseridos++;
                }
                catch (Exception ex)
                {
                    res.Erros++;
                    if (res.Erros == 1) res.Detalhe = ex.Message;
                }
            }
        }
        catch (Exception ex)
        {
            res.Detalhe = $"Falha ao migrar {tabela}: {ex.Message}";
        }
        return res;
    }

    // ─── Helpers de leitura Firebird ──────────────────────────────────────────

    private static async Task<MigResult> InferirTipos(DbConnection mysql, DbTransaction tx)
    {
        var res = new MigResult { Tabela = "Tipos (inferidos)" };
        try
        {
            // Todos que têm PF ou PJ → TIPO_CLIENTE = 1
            using var c1 = mysql.CreateCommand();
            c1.Transaction = tx;
            c1.CommandText =
                "UPDATE ENTIDADES SET TIPO_CLIENTE=1 " +
                "WHERE ID_ENTIDADES IN (SELECT ID_ENTIDADES FROM ENTIDADES_PF) " +
                "   OR ID_ENTIDADES IN (SELECT ID_ENTIDADES FROM ENTIDADES_PJ)";
            res.Inseridos += await c1.ExecuteNonQueryAsync();

            // Funcionários
            using var c2 = mysql.CreateCommand();
            c2.Transaction = tx;
            c2.CommandText =
                "UPDATE ENTIDADES SET TIPO_FUNCIONARIO=1 " +
                "WHERE ID_ENTIDADES IN (SELECT ID_ENTIDADES FROM ENTIDADES_FUNCIONARIOS)";
            await c2.ExecuteNonQueryAsync();
        }
        catch (Exception ex) { res.Detalhe = ex.Message; }
        return res;
    }

    private static async Task<bool> TabelaExisteAsync(FbConnection fb, string nome)
    {
        try
        {
            using var cmd = new FbCommand($"SELECT COUNT(*) FROM {nome}", fb);
            await cmd.ExecuteScalarAsync();
            return true;
        }
        catch { return false; }
    }

    private static string  S(FbDataReader r, int i) => r.IsDBNull(i) ? string.Empty : r.GetString(i).Trim();
    private static string? N(FbDataReader r, int i) => r.IsDBNull(i) ? null : r.GetString(i).Trim();
    private static bool    B(FbDataReader r, int i) =>
        !r.IsDBNull(i) && r.GetString(i).Trim().Equals("S", StringComparison.OrdinalIgnoreCase);

    private static string Sha256(string texto)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(texto));
        return Convert.ToHexString(bytes).ToLower();
    }
}
