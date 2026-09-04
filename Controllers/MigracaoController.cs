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

                log.Add(await Mig(fb, mysql, tx, "Cargos Funcionários", "CARGO_FUNCIONARIOS",
                    "SELECT ID_CARGO_FUNCIONARIOS,DESCRICAO FROM CARGO_FUNCIONARIOS", limpar,
                    "INSERT INTO CARGO_FUNCIONARIOS (ID_CARGO_FUNCIONARIOS,DESCRICAO) VALUES (@p0,@p1) ON DUPLICATE KEY UPDATE DESCRICAO=VALUES(DESCRICAO)",
                    r => new object?[] { r.GetInt32(0), S(r,1) }));

                log.Add(await Mig(fb, mysql, tx, "Tipos Reg. Profissional", "TIPOS_REG_PROFISSIONAL",
                    "SELECT ID_TIPOS_REG_PROFISSIONAL,DESCRICAO_REG_PROFISSIONAL FROM TIPOS_REG_PROFISSIONAL", limpar,
                    "INSERT IGNORE INTO TIPOS_REG_PROFISSIONAL (ID_TIPOS_REG_PROFISSIONAL,DESCRICAO_REG_PROFISSIONAL) VALUES (@p0,@p1)",
                    r => new object?[] { r.GetInt32(0), S(r,1) }));

                log.Add(await Mig(fb, mysql, tx, "Moedas", "MOEDAS",
                    "SELECT ID_MOEDAS,DESCRICAO,SIGLA FROM MOEDAS", limpar,
                    "INSERT IGNORE INTO MOEDAS (ID_MOEDAS,DESCRICAO,SIGLA) VALUES (@p0,@p1,@p2)",
                    r => new object?[] { r.GetInt32(0), S(r,1), S(r,2) }));

                log.Add(await Mig(fb, mysql, tx, "Propostas Status", "LAB_PROPOSTAS_STATUS",
                    "SELECT ID_LAB_PROPOSTAS_STATUS,DESCRICAO,COR FROM LAB_PROPOSTAS_STATUS", limpar,
                    "INSERT IGNORE INTO LAB_PROPOSTAS_STATUS (ID_LAB_PROPOSTAS_STATUS,DESCRICAO,COR) VALUES (@p0,@p1,@p2)",
                    r => new object?[] { r.GetInt32(0), S(r,1), N(r,2) }));

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
                    "INSERT INTO ENTIDADES (ID_ENTIDADES,CATEGORIA,DATA_CADASTRO,NOME,INATIVO,TIPO_CLIENTE,TIPO_FORNECEDOR,TIPO_VENDEDOR,TIPO_FUNCIONARIO,TIPO_EMPRESA_USUARIA) VALUES (@p0,@p1,@p2,@p3,@p4,0,0,0,0,0) ON DUPLICATE KEY UPDATE NOME=VALUES(NOME),CATEGORIA=VALUES(CATEGORIA)",
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

                // ── Empresas ───────────────────────────────────────────────────
                log.Add(await Mig(fb, mysql, tx, "Empresas", "EMPRESAS",
                    "SELECT ID_EMPRESAS,COD_EMPRESAS,ID_ENTIDADES FROM EMPRESAS", limpar,
                    "INSERT INTO EMPRESAS (ID_EMPRESAS,COD_EMPRESAS,ID_ENTIDADES) VALUES (@p0,@p1,@p2) ON DUPLICATE KEY UPDATE COD_EMPRESAS=VALUES(COD_EMPRESAS),ID_ENTIDADES=VALUES(ID_ENTIDADES)",
                    r => new object?[] { r.GetInt32(0), S(r,1), r.GetInt32(2) }));

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
                    "INSERT INTO USUARIO (USUCOD,USULOG,USUNOM,USUSEN,USUADM,INATIVO) VALUES (@p0,@p1,@p2,@p3,@p4,0) ON DUPLICATE KEY UPDATE USULOG=VALUES(USULOG),USUNOM=VALUES(USUNOM),USUADM=VALUES(USUADM)",
                    r =>
                    {
                        var nome  = S(r, 1);
                        var senha = Sha256(nome.ToLower()); // senha = nome em minúsculas
                        return new object?[] {
                            r.GetInt32(0), nome, nome, senha,
                            (!r.IsDBNull(2) && r.GetInt32(2) != 0) ? 1 : 0
                        };
                    }));

                // ── Inferir tipos de entidade pelas tabelas PF/PJ/FUNCIONARIOS ──
                // (substitui ENTIDADES_TIPOS que não existia neste FDB)
                log.Add(await InferirTipos(mysql, tx));

                // ── Pré-limpar tabelas LAB (ordem inversa de FK) se solicitado ─
                if (limpar)
                {
                    using var fkOff = mysql.CreateCommand();
                    fkOff.Transaction = tx;
                    fkOff.CommandText = "SET FOREIGN_KEY_CHECKS=0";
                    await fkOff.ExecuteNonQueryAsync();

                    foreach (var tLab in new[] {
                        "LAB_LOCAL_AMOSTRAS","LAB_MOV_AMOSTRAS_PARAM","LAB_MOV_AMOSTRAS",
                        "LAB_HIST_AMOSTRAS_SALDO","LAB_HIST_AMOSTRAS_TESTES","LAB_HIST_AMOSTRAS",
                        "LAB_PROPOSTAS_ANALISES","LAB_PROPOSTAS",
                        "LAB_PARAMETROS_ANALISES","PRODUTOS","CONDICOES_PAGTOS","PRAZOS" })
                    {
                        using var del = mysql.CreateCommand();
                        del.Transaction = tx;
                        del.CommandText = $"DELETE FROM {tLab}";
                        try { await del.ExecuteNonQueryAsync(); } catch { }
                    }

                    using var fkOn = mysql.CreateCommand();
                    fkOn.Transaction = tx;
                    fkOn.CommandText = "SET FOREIGN_KEY_CHECKS=1";
                    await fkOn.ExecuteNonQueryAsync();
                }

                // ── Condições de Pagamento ─────────────────────────────────────
                log.Add(await Mig(fb, mysql, tx, "Condições Pagamento", "CONDICOES_PAGTOS",
                    "SELECT ID_CONDICOES_PAGTOS,CODIGO,DESCRICAO FROM CONDICOES_PAGTOS", false,
                    "INSERT IGNORE INTO CONDICOES_PAGTOS (ID_CONDICOES_PAGTOS,CODIGO,DESCRICAO) VALUES (@p0,@p1,@p2)",
                    r => new object?[] { r.GetInt32(0), S(r,1), S(r,2) }));

                // ── Prazos ────────────────────────────────────────────────────
                log.Add(await Mig(fb, mysql, tx, "Prazos", "PRAZOS",
                    "SELECT ID_PRAZOS,DESCRICAO,QTDE FROM PRAZOS", false,
                    "INSERT IGNORE INTO PRAZOS (ID_PRAZOS,DESCRICAO,QTDE) VALUES (@p0,@p1,@p2)",
                    r => new object?[] { r.GetInt32(0), S(r,1), r.IsDBNull(2)?0:r.GetInt32(2) }));

                // ── Produtos ──────────────────────────────────────────────────
                log.Add(await Mig(fb, mysql, tx, "Produtos", "PRODUTOS",
                    "SELECT ID_PRODUTOS,CODIGO,DESCRICAO,ID_EMBALAGENS_TIPOS,ID_UNIDADES,QTDE_EMBALAGEM,INATIVO FROM PRODUTOS",
                    false,
                    "INSERT IGNORE INTO PRODUTOS (ID_PRODUTOS,CODIGO,DESCRICAO,ID_EMBALAGENS_TIPOS,ID_UNIDADES,QTDE_EMBALAGEM,INATIVO) VALUES (@p0,@p1,@p2,@p3,@p4,@p5,@p6)",
                    r => new object?[] {
                        r.GetInt32(0), S(r,1), S(r,2),
                        r.IsDBNull(3)?null:(object)r.GetInt32(3),
                        r.IsDBNull(4)?null:(object)r.GetInt32(4),
                        r.IsDBNull(5)?null:(object)(decimal)r.GetDouble(5),
                        (!r.IsDBNull(6) && r.GetString(6).Trim()=="S")?1:0
                    }));

                // ── Parâmetros de Análise ─────────────────────────────────────
                log.Add(await Mig(fb, mysql, tx, "Parâmetros Análises", "LAB_PARAMETROS_ANALISES",
                    "SELECT ID_LAB_PARAMETROS_ANALISES,DESCRICAO,ID_ANALISES_TIPO,VR_UNITARIO,DESC_REDUZIDA,AUDITADO,ID_ANALISES_METODOS FROM LAB_PARAMETROS_ANALISES",
                    false,
                    "INSERT IGNORE INTO LAB_PARAMETROS_ANALISES (ID_LAB_PARAMETROS_ANALISES,DESCRICAO,ID_ANALISES_TIPO,VR_UNITARIO,DESC_REDUZIDA,AUDITADO,ID_ANALISES_METODOS) VALUES (@p0,@p1,@p2,@p3,@p4,@p5,@p6)",
                    r => new object?[] {
                        r.GetInt32(0), S(r,1),
                        r.IsDBNull(2)?null:(object)r.GetInt32(2),
                        r.IsDBNull(3)?(object)0m:(decimal)r.GetDouble(3),
                        N(r,4),
                        (!r.IsDBNull(5) && r.GetInt32(5)!=0)?1:0,
                        r.IsDBNull(6)?null:(object)r.GetInt32(6)
                    }));

                // ── Propostas ─────────────────────────────────────────────────
                // ID_STATUS_PROPOSTAS no Firebird → ID_LAB_PROPOSTAS_STATUS no MySQL
                log.Add(await Mig(fb, mysql, tx, "Propostas", "LAB_PROPOSTAS",
                    @"SELECT ID_LAB_PROPOSTAS,ID_ENTIDADES,ID_EMPRESAS,COD_PROPOSTA,ANO_PROPOSTA,REV_PROPOSTA,
                             DT_SOLICITACAO,DT_VALIDADE,ID_STATUS_PROPOSTAS,VR_TOTAL_PROPOSTA,PORC_DESCONTO,VR_DESCONTO,
                             ID_ENTIDADES_FUNC,ID_CONDICOES_PAGTOS,DT_AUTORIZACAO,ID_MOEDAS,ID_ENTIDADES_COMERC,
                             ID_ENT_END_LAUDO,ID_ENT_END_NF,DT_ENVIO_CLIENTE,ID_ENT_CONTATO_NF,
                             ID_ENT_CONTATO_RESULTADO,ID_ENT_COBRANCA,ID_END_ENT_COBRANCA
                      FROM LAB_PROPOSTAS", false,
                    @"INSERT IGNORE INTO LAB_PROPOSTAS
                        (ID_LAB_PROPOSTAS,ID_ENTIDADES,ID_EMPRESAS,COD_PROPOSTA,ANO_PROPOSTA,REV_PROPOSTA,
                         DT_SOLICITACAO,DT_VALIDADE,ID_LAB_PROPOSTAS_STATUS,VR_TOTAL_PROPOSTA,PORC_DESCONTO,VR_DESCONTO,
                         ID_ENTIDADES_FUNC,ID_CONDICOES_PAGTOS,DT_AUTORIZACAO,ID_MOEDAS,ID_ENTIDADES_COMERC,
                         ID_ENT_END_LAUDO,ID_ENT_END_NF,DT_ENVIO_CLIENTE,ID_ENT_CONTATO_NF,
                         ID_ENT_CONTATO_RESULTADO,ID_ENT_COBRANCA,ID_END_ENT_COBRANCA)
                      VALUES (@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11,@p12,@p13,@p14,@p15,@p16,@p17,@p18,@p19,@p20,@p21,@p22,@p23)",
                    r => new object?[] {
                        r.GetInt32(0), r.GetInt32(1), r.GetInt32(2), r.GetInt32(3), r.GetInt32(4), r.GetInt32(5),
                        r.IsDBNull(6)?null:(object)r.GetDateTime(6),
                        r.IsDBNull(7)?null:(object)r.GetDateTime(7),
                        r.IsDBNull(8)?null:(object)r.GetInt32(8),
                        r.IsDBNull(9)?(object)0m:(decimal)r.GetDouble(9),
                        r.IsDBNull(10)?null:(object)(decimal)r.GetDouble(10),
                        r.IsDBNull(11)?null:(object)(decimal)r.GetDouble(11),
                        r.IsDBNull(12)?null:(object)r.GetInt32(12),
                        r.IsDBNull(13)?null:(object)r.GetInt32(13),
                        r.IsDBNull(14)?null:(object)r.GetDateTime(14),
                        r.IsDBNull(15)?null:(object)r.GetInt32(15),
                        r.IsDBNull(16)?null:(object)r.GetInt32(16),
                        r.IsDBNull(17)?null:(object)r.GetInt32(17),
                        r.IsDBNull(18)?null:(object)r.GetInt32(18),
                        r.IsDBNull(19)?null:(object)r.GetDateTime(19),
                        r.IsDBNull(20)?null:(object)r.GetInt32(20),
                        r.IsDBNull(21)?null:(object)r.GetInt32(21),
                        r.IsDBNull(22)?null:(object)r.GetInt32(22),
                        r.IsDBNull(23)?null:(object)r.GetInt32(23)
                    }));

                // ── Propostas Análises (PRODANALISES + join PRODUTOS) ─────────
                log.Add(await Mig(fb, mysql, tx, "Propostas Análises", "LAB_PROPOSTAS_ANALISES",
                    @"SELECT pa.ID_LAB_PROPOSTAS_PRODANALISES, pa.ID_LAB_PROPOSTAS, pp.ID_PRODUTOS,
                             pa.ID_ANALISES_METODOS, pa.ID_LAB_PARAMETROS_ANALISES, pa.ID_IDIOMAS,
                             pa.QTDE_AMOSTRAS, pa.VR_UNITARIO, pa.VR_SUBTOTAL,
                             pa.PORC_DESCONTO, pa.VR_DESCONTO, pa.VR_TOTAL, pa.ID_PRAZOS, pa.TIPO_DOCUMENTO
                      FROM LAB_PROPOSTAS_PRODANALISES pa
                      LEFT JOIN LAB_PROPOSTAS_PRODUTOS pp
                             ON pp.ID_LAB_PROPOSTAS_PRODUTOS = pa.ID_LAB_PROPOSTAS_PRODUTOS
                            AND pp.ID_LAB_PROPOSTAS = pa.ID_LAB_PROPOSTAS", false,
                    @"INSERT IGNORE INTO LAB_PROPOSTAS_ANALISES
                        (ID_LAB_PROPOSTAS_ANALISES,ID_LAB_PROPOSTAS,ID_PRODUTOS,
                         ID_ANALISES_METODOS,ID_LAB_PARAMETROS_ANALISES,ID_IDIOMAS,
                         QTDE_AMOSTRAS,VR_UNITARIO,VR_SUBTOTAL,
                         PORC_DESCONTO,VR_DESCONTO,VR_TOTAL,ID_PRAZOS,TIPO_DOCUMENTO)
                      VALUES (@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11,@p12,@p13)",
                    r => new object?[] {
                        r.GetInt32(0), r.GetInt32(1),
                        r.IsDBNull(2)?null:(object)r.GetInt32(2),
                        r.IsDBNull(3)?null:(object)r.GetInt32(3),
                        r.IsDBNull(4)?null:(object)r.GetInt32(4),
                        r.IsDBNull(5)?null:(object)r.GetInt32(5),
                        r.IsDBNull(6)?1:r.GetInt32(6),
                        r.IsDBNull(7)?(object)0m:(decimal)r.GetDouble(7),
                        r.IsDBNull(8)?null:(object)(decimal)r.GetDouble(8),
                        r.IsDBNull(9)?null:(object)(decimal)r.GetDouble(9),
                        r.IsDBNull(10)?null:(object)(decimal)r.GetDouble(10),
                        r.IsDBNull(11)?(object)0m:(decimal)r.GetDouble(11),
                        r.IsDBNull(12)?null:(object)r.GetInt32(12),
                        N(r,13)
                    }));

                // ── Histórico de Amostras ─────────────────────────────────────
                // Mapeamentos: COLETOR→NOME_COLETOR, DATAHORACOLETA→DT_HR_COLETA,
                //              TEMPERATURA_VERFIFICACAO→TEMPERATURA_VERIFICACAO,
                //              ACOMPANHA_PADRA_ANALITICO→ACOMPANHA_PADRAO_ANALITICO,
                //              HR_ENTREGA TIMESTAMP→VARCHAR "HH:mm"
                log.Add(await Mig(fb, mysql, tx, "Hist. Amostras", "LAB_HIST_AMOSTRAS",
                    @"SELECT ID_LAB_HIST_AMOSTRAS,ID_AMOSTRAS_TIPO,COD_AMOSTRA,ID_ANALISES_TIPO,ANO_AMOSTRA,
                             ID_ENTIDADES,NOME_CONTATO,ID_LAB_PROPOSTAS,DT_ENTREGA,HR_ENTREGA,
                             LOCAL_RECEBIMENTO,ID_EMBALAGENS_TIPOS,QTDE_EMBALAGENS_ENTREGUE,ID_PRODUTOS,NR_LOTE,
                             FABRICACAO_DIA,FABRICACAO_MES,FABRICACAO_ANO,VALIDADE_DIA,VALIDADE_MES,VALIDADE_ANO,
                             NOTA_ROTULO,ESPECIE_AMOSTRA,ASPECTO_AMOSTRA,COR,OUTRAS_CARACTERISTICAS,
                             QTDE_AMOSTRA_VERIFICACAO,TEMPERATURA_VERFIFICACAO,
                             ACOMPANHA_FICHA_TECNICA,ACOMPANHA_PADRA_ANALITICO,ACOMPANHA_CA_CLIENTE,
                             ENVIAR_OUTRO_LABORATORIO,QTDE_ENVIO_OUTRO_LABORATORIO,
                             ID_AMOSTRAS_STATUS,ID_EMPRESAS,ID_ENTIDADES_FUNC_RESP,ID_ENTIDADES_FUNC_DIG,
                             REVISAO,COLETOR,DATAHORACOLETA,TIPO_DOCUMENTO
                      FROM LAB_HIST_AMOSTRAS", false,
                    @"INSERT IGNORE INTO LAB_HIST_AMOSTRAS
                        (ID_LAB_HIST_AMOSTRAS,ID_AMOSTRAS_TIPO,COD_AMOSTRA,ID_ANALISES_TIPO,ANO_AMOSTRA,
                         ID_ENTIDADES,NOME_CONTATO,ID_LAB_PROPOSTAS,DT_ENTREGA,HR_ENTREGA,
                         LOCAL_RECEBIMENTO,ID_EMBALAGENS_TIPOS,QTDE_EMBALAGENS_ENTREGUE,ID_PRODUTOS,NR_LOTE,
                         FABRICACAO_DIA,FABRICACAO_MES,FABRICACAO_ANO,VALIDADE_DIA,VALIDADE_MES,VALIDADE_ANO,
                         NOTA_ROTULO,ESPECIE_AMOSTRA,ASPECTO_AMOSTRA,COR,OUTRAS_CARACTERISTICAS,
                         QTDE_AMOSTRA_VERIFICACAO,TEMPERATURA_VERIFICACAO,
                         ACOMPANHA_FICHA_TECNICA,ACOMPANHA_PADRAO_ANALITICO,ACOMPANHA_CA_CLIENTE,
                         ENVIAR_OUTRO_LABORATORIO,QTDE_ENVIO_OUTRO_LABORATORIO,
                         ID_AMOSTRAS_STATUS,ID_EMPRESAS,ID_ENTIDADES_FUNC_RESP,ID_ENTIDADES_FUNC_DIG,
                         REVISAO,NOME_COLETOR,DT_HR_COLETA,TIPO_DOCUMENTO)
                      VALUES (@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10,@p11,@p12,@p13,@p14,@p15,@p16,@p17,@p18,@p19,@p20,@p21,@p22,@p23,@p24,@p25,@p26,@p27,@p28,@p29,@p30,@p31,@p32,@p33,@p34,@p35,@p36,@p37,@p38,@p39,@p40)",
                    r =>
                    {
                        var hrEntrega = r.IsDBNull(9) ? null : r.GetDateTime(9).ToString("HH:mm");
                        return new object?[] {
                            r.GetInt32(0), r.GetInt32(1), r.GetInt32(2),
                            r.IsDBNull(3)?null:(object)r.GetInt32(3),
                            r.GetInt32(4),
                            r.IsDBNull(5)?null:(object)r.GetInt32(5),
                            N(r,6),
                            r.IsDBNull(7)?null:(object)r.GetInt32(7),
                            r.IsDBNull(8)?null:(object)r.GetDateTime(8),
                            hrEntrega,
                            N(r,10),
                            r.IsDBNull(11)?null:(object)r.GetInt32(11),
                            r.IsDBNull(12)?(object)0m:(decimal)r.GetDouble(12),
                            r.IsDBNull(13)?null:(object)r.GetInt32(13),
                            N(r,14),
                            r.IsDBNull(15)?null:(object)r.GetInt32(15),
                            r.IsDBNull(16)?null:(object)r.GetInt32(16),
                            r.IsDBNull(17)?null:(object)r.GetInt32(17),
                            r.IsDBNull(18)?null:(object)r.GetInt32(18),
                            r.IsDBNull(19)?null:(object)r.GetInt32(19),
                            r.IsDBNull(20)?null:(object)r.GetInt32(20),
                            N(r,21), N(r,22), N(r,23), N(r,24), N(r,25),
                            r.IsDBNull(26)?null:(object)(decimal)r.GetDouble(26),
                            r.IsDBNull(27)?null:(object)(decimal)r.GetDouble(27),
                            B(r,28)?1:0, B(r,29)?1:0, B(r,30)?1:0,
                            B(r,31)?1:0,
                            r.IsDBNull(32)?null:(object)(decimal)r.GetDouble(32),
                            r.IsDBNull(33)?null:(object)r.GetInt32(33),
                            r.GetInt32(34),
                            r.IsDBNull(35)?null:(object)r.GetInt32(35),
                            r.IsDBNull(36)?null:(object)r.GetInt32(36),
                            r.IsDBNull(37)?null:(object)r.GetInt32(37),
                            N(r,38),
                            r.IsDBNull(39)?null:(object)r.GetDateTime(39),
                            N(r,40) ?? "BOLETIM"
                        };
                    }));

                // ── Hist. Amostras Testes ─────────────────────────────────────
                log.Add(await Mig(fb, mysql, tx, "Hist. Testes", "LAB_HIST_AMOSTRAS_TESTES",
                    @"SELECT ID_LAB_HIST_AMOSTRAS_TESTES,ID_LAB_HIST_AMOSTRAS,ID_ANALISES_TIPO,
                             ID_ANALISES_METODOS,ID_IDIOMAS,ID_PRAZOS,ID_ENTIDADES,ID_LAB_PARAMETROS_ANALISES
                      FROM LAB_HIST_AMOSTRAS_TESTES", false,
                    @"INSERT IGNORE INTO LAB_HIST_AMOSTRAS_TESTES
                        (ID_LAB_HIST_AMOSTRAS_TESTES,ID_LAB_HIST_AMOSTRAS,ID_ANALISES_TIPO,
                         ID_ANALISES_METODOS,ID_IDIOMAS,ID_PRAZOS,ID_ENTIDADES,ID_LAB_PARAMETROS_ANALISES)
                      VALUES (@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7)",
                    r => new object?[] {
                        r.GetInt32(0), r.GetInt32(1),
                        r.IsDBNull(2)?null:(object)r.GetInt32(2),
                        r.IsDBNull(3)?null:(object)r.GetInt32(3),
                        r.IsDBNull(4)?null:(object)r.GetInt32(4),
                        r.IsDBNull(5)?null:(object)r.GetInt32(5),
                        r.IsDBNull(6)?null:(object)r.GetInt32(6),
                        r.IsDBNull(7)?null:(object)r.GetInt32(7)
                    }));

                // ── Hist. Amostras Saldo (se existir no Firebird) ─────────────
                if (await TabelaExisteAsync(fb, "LAB_HIST_AMOSTRAS_SALDO"))
                {
                    log.Add(await Mig(fb, mysql, tx, "Hist. Saldo", "LAB_HIST_AMOSTRAS_SALDO",
                        "SELECT ID_LAB_HIST_AMOSTRAS,ID_EMPRESAS,SALDO_ATUAL,DATA_ATUALIZACAO FROM LAB_HIST_AMOSTRAS_SALDO",
                        false,
                        "INSERT IGNORE INTO LAB_HIST_AMOSTRAS_SALDO (ID_LAB_HIST_AMOSTRAS,ID_EMPRESAS,SALDO_ATUAL,DATA_ATUALIZACAO) VALUES (@p0,@p1,@p2,@p3)",
                        r => new object?[] {
                            r.GetInt32(0), r.GetInt32(1),
                            r.IsDBNull(2)?(object)0m:(decimal)r.GetDouble(2),
                            r.IsDBNull(3)?DateTime.Now:(object)r.GetDateTime(3)
                        }));
                }

                // ── Movimentações de Amostras ─────────────────────────────────
                log.Add(await Mig(fb, mysql, tx, "Movimentações", "LAB_MOV_AMOSTRAS",
                    @"SELECT ID_LAB_MOV_AMOSTRAS,ID_EMPRESAS,ID_LAB_HIST_AMOSTRAS,DATA_MOV,
                             QTDE,E_S,JUSTIFICATIVA,AMOSTRA_COMPLEMENTAR,ID_ENTIDADES_FUNC
                      FROM LAB_MOV_AMOSTRAS", false,
                    @"INSERT IGNORE INTO LAB_MOV_AMOSTRAS
                        (ID_LAB_MOV_AMOSTRAS,ID_EMPRESAS,ID_LAB_HIST_AMOSTRAS,DATA_MOV,
                         QTDE,E_S,JUSTIFICATIVA,AMOSTRA_COMPLEMENTAR,ID_ENTIDADES_FUNC)
                      VALUES (@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8)",
                    r => new object?[] {
                        r.GetInt32(0), r.GetInt32(1), r.GetInt32(2),
                        r.IsDBNull(3)?DateTime.Now:(object)r.GetDateTime(3),
                        r.IsDBNull(4)?(object)0m:(decimal)r.GetDouble(4),
                        N(r,5)??"E", N(r,6), N(r,7),
                        r.IsDBNull(8)?null:(object)r.GetInt32(8)
                    }));

                // ── Movimentações Parâmetros ──────────────────────────────────
                log.Add(await Mig(fb, mysql, tx, "Mov. Parâmetros", "LAB_MOV_AMOSTRAS_PARAM",
                    @"SELECT ID_LAB_MOV_AMOSTRAS_PARAM,ID_EMPRESAS,ID_LAB_MOV_AMOSTRAS,ID_LAB_HIST_AMOSTRAS_TESTES
                      FROM LAB_MOV_AMOSTRAS_PARAM", false,
                    @"INSERT IGNORE INTO LAB_MOV_AMOSTRAS_PARAM
                        (ID_LAB_MOV_AMOSTRAS_PARAM,ID_EMPRESAS,ID_LAB_MOV_AMOSTRAS,
                         ID_LAB_HIST_AMOSTRAS_TESTES,ID_LAB_PARAMETROS_ANALISES)
                      VALUES (@p0,@p1,@p2,@p3,NULL)",
                    r => new object?[] {
                        r.GetInt32(0), r.GetInt32(1), r.GetInt32(2),
                        r.IsDBNull(3)?null:(object)r.GetInt32(3)
                    }));

                // ── Localização de Amostras ───────────────────────────────────
                // DT_HR_DESCATE (typo Firebird) → DT_HR_DESCARTE (MySQL)
                log.Add(await Mig(fb, mysql, tx, "Localização Amostras", "LAB_LOCAL_AMOSTRAS",
                    @"SELECT ID_LAB_LOCAL_AMOSTRAS,ID_LAB_HIST_AMOSTRAS,ID_EMPRESAS,STATUS,DT_HR_ARQUIVO,
                             NR_ARMARIO,NR_PRATELEIRA,NR_CAIXA,OBSERVACAO,ID_FUNCIONARIO_DESCARTE,DT_HR_DESCATE
                      FROM LAB_LOCAL_AMOSTRAS", false,
                    @"INSERT IGNORE INTO LAB_LOCAL_AMOSTRAS
                        (ID_LAB_LOCAL_AMOSTRAS,ID_LAB_HIST_AMOSTRAS,ID_EMPRESAS,STATUS,DT_HR_ARQUIVO,
                         NR_ARMARIO,NR_PRATELEIRA,NR_CAIXA,OBSERVACAO,ID_FUNCIONARIO_DESCARTE,DT_HR_DESCARTE)
                      VALUES (@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9,@p10)",
                    r => new object?[] {
                        r.GetInt32(0), r.GetInt32(1), r.GetInt32(2),
                        r.IsDBNull(3)?0:r.GetInt32(3),
                        r.IsDBNull(4)?null:(object)r.GetDateTime(4),
                        N(r,5), N(r,6), N(r,7), N(r,8),
                        r.IsDBNull(9)?null:(object)r.GetInt32(9),
                        r.IsDBNull(10)?null:(object)r.GetDateTime(10)
                    }));

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
