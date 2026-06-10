using LabControl.Data;
using LabControl.Models.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace LabControl.Controllers;

public class FuncionariosController : Controller
{
    private readonly ApplicationDbContext _db;
    public FuncionariosController(ApplicationDbContext db) => _db = db;

    // ─── Index ───────────────────────────────────────────────────────────────

    public async Task<IActionResult> Index(string? busca)
    {
        ViewBag.Busca = busca;
        var query = _db.Entidades
            .Where(e => e.TipoFuncionario && !e.Inativo)
            .Include(e => e.PessoaFisica)
            .Include(e => e.Funcionario).ThenInclude(f => f!.CargoFuncionario)
            .Include(e => e.Fones)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
            query = query.Where(e => e.Nome.Contains(busca));

        return View(await query.OrderBy(e => e.Nome).ToListAsync());
    }

    // ─── Criar ───────────────────────────────────────────────────────────────

    public async Task<IActionResult> Criar()
    {
        await CarregarViewBag();
        return View(new Entidade { TipoFuncionario = true, Categoria = "F" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(Entidade model, string? cpf, string? sobrenome,
        string? rg, DateTime? dataNascimento, string? sexo, string? ddd, string? fone,
        string? nrRegistroProfissional, int? idTipoRegProfissional, int? idCargoFuncionario)
    {
        model.Categoria       = "F";
        model.TipoFuncionario = true;
        model.DataCadastro    = DateTime.Now;
        _db.Entidades.Add(model);
        await _db.SaveChangesAsync();

        _db.EntidadesPF.Add(new EntidadePF
        {
            Id = model.Id, Cpf = cpf, Nome = model.Nome, Sobrenome = sobrenome,
            Rg = rg, DataNascimento = dataNascimento, Sexo = sexo
        });

        _db.EntidadesFuncionarios.Add(new EntidadeFuncionario
        {
            Id = model.Id,
            NrRegistroProfissional = nrRegistroProfissional,
            IdTipoRegProfissional  = idTipoRegProfissional,
            IdCargoFuncionario     = idCargoFuncionario
        });

        if (!string.IsNullOrWhiteSpace(fone))
            _db.EntidadesFones.Add(new EntidadeFone { IdEntidade = model.Id, Ddd = ddd, Fone = fone });

        await _db.SaveChangesAsync();
        TempData["Sucesso"] = $"Funcionário \"{model.Nome}\" cadastrado com sucesso!";
        return RedirectToAction(nameof(Editar), new { id = model.Id });
    }

    // ─── Editar ───────────────────────────────────────────────────────────────

    public async Task<IActionResult> Editar(int id)
    {
        var e = await CarregarCompleto(id);
        if (e == null) return NotFound();
        await CarregarViewBag();
        return View(e);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, Entidade model, string? cpf, string? sobrenome,
        string? rg, DateTime? dataNascimento, string? sexo,
        string? nrRegistroProfissional, int? idTipoRegProfissional, int? idCargoFuncionario)
    {
        var entidade = await _db.Entidades
            .Include(e => e.PessoaFisica)
            .Include(e => e.Funcionario)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (entidade == null) return NotFound();

        entidade.Nome    = model.Nome;
        entidade.Inativo = model.Inativo;

        if (entidade.PessoaFisica != null)
        {
            entidade.PessoaFisica.Cpf            = cpf;
            entidade.PessoaFisica.Sobrenome      = sobrenome;
            entidade.PessoaFisica.Rg             = rg;
            entidade.PessoaFisica.DataNascimento = dataNascimento;
            entidade.PessoaFisica.Sexo           = sexo;
        }

        if (entidade.Funcionario != null)
        {
            entidade.Funcionario.NrRegistroProfissional = nrRegistroProfissional;
            entidade.Funcionario.IdTipoRegProfissional  = idTipoRegProfissional;
            entidade.Funcionario.IdCargoFuncionario     = idCargoFuncionario;
        }

        await _db.SaveChangesAsync();
        TempData["Sucesso"] = $"Dados de \"{entidade.Nome}\" atualizados!";
        return RedirectToAction(nameof(Editar), new { id });
    }

    // ─── Assinatura digital ───────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnviarAssinatura(int idEntidade, IFormFile arquivo)
    {
        if (arquivo == null || arquivo.Length == 0)
            return BadRequest("Selecione um arquivo de imagem.");

        using var ms = new MemoryStream();
        await arquivo.CopyToAsync(ms);
        var bytes = ms.ToArray();
        var md5   = Convert.ToHexString(MD5.HashData(bytes)).ToLowerInvariant();

        var assinatura = await _db.EntidadesFuncAssinaturas.FindAsync(idEntidade);
        if (assinatura == null)
        {
            assinatura = new EntidadeFuncAssinatura { IdEntidadeFunc = idEntidade };
            _db.EntidadesFuncAssinaturas.Add(assinatura);
        }
        assinatura.AssinaturaDigital = bytes;
        assinatura.Md5Assinatura     = md5;
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Editar), new { id = idEntidade });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoverAssinatura(int idEntidade)
    {
        var assinatura = await _db.EntidadesFuncAssinaturas.FindAsync(idEntidade);
        if (assinatura != null)
        {
            _db.EntidadesFuncAssinaturas.Remove(assinatura);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Editar), new { id = idEntidade });
    }

    [HttpGet]
    public async Task<IActionResult> Assinatura(int id)
    {
        var assinatura = await _db.EntidadesFuncAssinaturas.FindAsync(id);
        if (assinatura?.AssinaturaDigital == null) return NotFound();
        return File(assinatura.AssinaturaDigital, "image/png");
    }

    // ─── Helpers privados ─────────────────────────────────────────────────────

    private async Task<Entidade?> CarregarCompleto(int id) =>
        await _db.Entidades
            .Include(e => e.PessoaFisica)
            .Include(e => e.Funcionario).ThenInclude(f => f!.TipoRegProfissional)
            .Include(e => e.Funcionario).ThenInclude(f => f!.CargoFuncionario)
            .Include(e => e.Funcionario).ThenInclude(f => f!.Assinatura)
            .Include(e => e.Fones).ThenInclude(f => f.FoneTipo)
            .Include(e => e.Emails)
            .Include(e => e.Enderecos).ThenInclude(en => en.EnderecoTipo)
            .Include(e => e.Observacao)
            .FirstOrDefaultAsync(e => e.Id == id);

    private async Task CarregarViewBag()
    {
        ViewBag.FonesTipos          = await _db.FonesTipos.OrderBy(f => f.Descricao).ToListAsync();
        ViewBag.EnderecosTipos      = await _db.EnderecosTipos.OrderBy(e => e.Descricao).ToListAsync();
        ViewBag.TiposRegProfissional = await _db.TiposRegProfissional.OrderBy(t => t.Descricao).ToListAsync();
        ViewBag.CargosFuncionarios   = await _db.CargosFuncionarios.OrderBy(c => c.Descricao).ToListAsync();
    }
}
