using LabControl.Data;
using LabControl.Models.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Controllers;

public class ClientesController : Controller
{
    private readonly ApplicationDbContext _db;

    public ClientesController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? busca)
    {
        ViewBag.Busca = busca;
        var query = _db.Entidades
            .Where(e => e.TipoCliente && !e.Inativo)
            .Include(e => e.PessoaFisica)
            .Include(e => e.PessoaJuridica)
            .Include(e => e.Fones)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
            query = query.Where(e => e.Nome.Contains(busca));

        var lista = await query.OrderBy(e => e.Nome).ToListAsync();
        return View(lista);
    }

    public IActionResult Criar()
    {
        return View(new Entidade { TipoCliente = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(Entidade model, string? cpf, string? cnpj,
        string? nomeFantasia, string? inscEstadual, string? rg, DateTime? dataNascimento,
        string? sexo, string? ddd, string? fone)
    {
        model.TipoCliente = true;
        model.DataCadastro = DateTime.Now;

        _db.Entidades.Add(model);
        await _db.SaveChangesAsync();

        // Criar registro PF ou PJ
        if (model.Categoria == "F")
        {
            _db.EntidadesPF.Add(new EntidadePF
            {
                Id = model.Id,
                Cpf = cpf,
                Nome = model.Nome,
                Rg = rg,
                DataNascimento = dataNascimento,
                Sexo = sexo
            });
        }
        else
        {
            _db.EntidadesPJ.Add(new EntidadePJ
            {
                Id = model.Id,
                Cnpj = cnpj,
                NomeFantasia = nomeFantasia,
                InscricaoEstadual = inscEstadual
            });
        }

        // Telefone
        if (!string.IsNullOrWhiteSpace(fone))
        {
            _db.EntidadesFones.Add(new EntidadeFone
            {
                IdEntidade = model.Id,
                Ddd = ddd,
                Fone = fone
            });
        }

        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"Cliente \"{model.Nome}\" cadastrado com sucesso!";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Editar(int id)
    {
        var entidade = await _db.Entidades
            .Include(e => e.PessoaFisica)
            .Include(e => e.PessoaJuridica)
            .Include(e => e.Fones)
            .Include(e => e.Emails)
            .Include(e => e.Enderecos)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entidade == null) return NotFound();
        return View(entidade);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, Entidade model, string? cpf, string? cnpj,
        string? nomeFantasia, string? inscEstadual)
    {
        var entidade = await _db.Entidades
            .Include(e => e.PessoaFisica)
            .Include(e => e.PessoaJuridica)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (entidade == null) return NotFound();

        entidade.Nome = model.Nome;
        entidade.Inativo = model.Inativo;

        if (entidade.Categoria == "F" && entidade.PessoaFisica != null)
            entidade.PessoaFisica.Cpf = cpf;
        else if (entidade.Categoria == "J" && entidade.PessoaJuridica != null)
        {
            entidade.PessoaJuridica.Cnpj = cnpj;
            entidade.PessoaJuridica.NomeFantasia = nomeFantasia;
            entidade.PessoaJuridica.InscricaoEstadual = inscEstadual;
        }

        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"Cliente \"{entidade.Nome}\" atualizado!";
        return RedirectToAction(nameof(Index));
    }

    // Endpoint para busca via AJAX (usado no formulário de amostras)
    [HttpGet]
    public async Task<IActionResult> BuscarJson(string termo)
    {
        var resultado = await _db.Entidades
            .Where(e => e.TipoCliente && !e.Inativo && e.Nome.Contains(termo))
            .Select(e => new { e.Id, e.Nome, e.Categoria })
            .Take(10)
            .ToListAsync();

        return Json(resultado);
    }
}
