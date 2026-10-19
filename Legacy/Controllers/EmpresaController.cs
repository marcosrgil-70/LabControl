using LabControl.Data;
using LabControl.Models.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Controllers;

public class EmpresaController : Controller
{
    private readonly ApplicationDbContext _db;
    public EmpresaController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Editar()
    {
        var empresaId = HttpContext.Session.GetInt32("EmpresaId") ?? 1;
        var empresa = await _db.Empresas
            .Include(e => e.Entidade)
            .FirstOrDefaultAsync(e => e.Id == empresaId);

        if (empresa?.Entidade == null) return NotFound();

        var entidade = await CarregarCompleto(empresa.Entidade.Id);
        if (entidade == null) return NotFound();

        await CarregarViewBag();
        return View(entidade);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, Entidade model, string? cnpj,
        string? nomeFantasia, string? inscEstadual, string? inscMunicipal)
    {
        var entidade = await _db.Entidades
            .Include(e => e.PessoaJuridica)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (entidade == null) return NotFound();

        entidade.Nome = model.Nome;

        if (entidade.PessoaJuridica == null)
        {
            _db.EntidadesPJ.Add(new EntidadePJ
            {
                Id = entidade.Id,
                Cnpj = cnpj?.Trim(),
                NomeFantasia = nomeFantasia?.Trim(),
                InscricaoEstadual = inscEstadual?.Trim(),
                InscricaoMunicipal = inscMunicipal?.Trim()
            });
        }
        else
        {
            entidade.PessoaJuridica.Cnpj = cnpj?.Trim();
            entidade.PessoaJuridica.NomeFantasia = nomeFantasia?.Trim();
            entidade.PessoaJuridica.InscricaoEstadual = inscEstadual?.Trim();
            entidade.PessoaJuridica.InscricaoMunicipal = inscMunicipal?.Trim();
        }

        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Dados da empresa atualizados!";
        return RedirectToAction(nameof(Editar));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdicionarFone(int idEntidade, string? ddd, string? fone, int? idFoneTipo)
    {
        if (string.IsNullOrWhiteSpace(fone)) return BadRequest("Número obrigatório.");
        _db.EntidadesFones.Add(new EntidadeFone { IdEntidade = idEntidade, Ddd = ddd?.Trim(), Fone = fone.Trim(), IdFoneTipo = idFoneTipo });
        await _db.SaveChangesAsync();
        return PartialView("_GridFones", await FonesDeEntidade(idEntidade));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirFone(int id, int idEntidade)
    {
        var f = await _db.EntidadesFones.FindAsync(id);
        if (f != null) { _db.EntidadesFones.Remove(f); await _db.SaveChangesAsync(); }
        return PartialView("_GridFones", await FonesDeEntidade(idEntidade));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdicionarEmail(int idEntidade, string? email, bool principal)
    {
        if (string.IsNullOrWhiteSpace(email)) return BadRequest("E-mail obrigatório.");
        if (principal)
        {
            var existentes = await _db.EntidadesEmails.Where(e => e.IdEntidade == idEntidade).ToListAsync();
            existentes.ForEach(e => e.Principal = false);
        }
        _db.EntidadesEmails.Add(new EntidadeEmail { IdEntidade = idEntidade, Email = email.Trim(), Principal = principal });
        await _db.SaveChangesAsync();
        return PartialView("_GridEmails", await EmailsDeEntidade(idEntidade));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirEmail(int id, int idEntidade)
    {
        var e = await _db.EntidadesEmails.FindAsync(id);
        if (e != null) { _db.EntidadesEmails.Remove(e); await _db.SaveChangesAsync(); }
        return PartialView("_GridEmails", await EmailsDeEntidade(idEntidade));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarEmailPrincipal(int id, int idEntidade)
    {
        var todos = await _db.EntidadesEmails.Where(e => e.IdEntidade == idEntidade).ToListAsync();
        todos.ForEach(e => e.Principal = e.Id == id);
        await _db.SaveChangesAsync();
        return PartialView("_GridEmails", todos);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdicionarEndereco(int idEntidade, int? idEnderecoTipo,
        string? logradouro, string? numero, string? complemento,
        string? bairro, string? cidade, string? uf, string? cep)
    {
        _db.EntidadesEnderecos.Add(new EntidadeEndereco
        {
            IdEntidade     = idEntidade,
            IdEnderecoTipo = idEnderecoTipo,
            Logradouro     = logradouro?.Trim(),
            Numero         = numero?.Trim(),
            Complemento    = complemento?.Trim(),
            Bairro         = bairro?.Trim(),
            Cidade         = cidade?.Trim(),
            Uf             = uf?.Trim().ToUpper(),
            Cep            = cep?.Trim()
        });
        await _db.SaveChangesAsync();
        return PartialView("_GridEnderecos", await EnderecosDeEntidade(idEntidade));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirEndereco(int id, int idEntidade)
    {
        var en = await _db.EntidadesEnderecos.FindAsync(id);
        if (en != null) { _db.EntidadesEnderecos.Remove(en); await _db.SaveChangesAsync(); }
        return PartialView("_GridEnderecos", await EnderecosDeEntidade(idEntidade));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SalvarObservacao(int idEntidade, string? observacao)
    {
        var obs = await _db.EntidadesObservacoes.FindAsync(idEntidade);
        if (obs == null)
            _db.EntidadesObservacoes.Add(new EntidadeObservacao { IdEntidade = idEntidade, Observacao = observacao });
        else
            obs.Observacao = observacao;
        await _db.SaveChangesAsync();
        return Json(new { ok = true });
    }

    private async Task<Entidade?> CarregarCompleto(int id) =>
        await _db.Entidades
            .Include(e => e.PessoaJuridica)
            .Include(e => e.Fones).ThenInclude(f => f.FoneTipo)
            .Include(e => e.Emails)
            .Include(e => e.Enderecos).ThenInclude(en => en.EnderecoTipo)
            .Include(e => e.Observacao)
            .FirstOrDefaultAsync(e => e.Id == id);

    private async Task CarregarViewBag()
    {
        ViewBag.FonesTipos     = await _db.FonesTipos.OrderBy(f => f.Descricao).ToListAsync();
        ViewBag.EnderecosTipos = await _db.EnderecosTipos.OrderBy(e => e.Descricao).ToListAsync();
    }

    private async Task<List<EntidadeFone>>     FonesDeEntidade(int id)     =>
        await _db.EntidadesFones.Include(f => f.FoneTipo).Where(f => f.IdEntidade == id).ToListAsync();

    private async Task<List<EntidadeEmail>>    EmailsDeEntidade(int id)    =>
        await _db.EntidadesEmails.Where(e => e.IdEntidade == id).ToListAsync();

    private async Task<List<EntidadeEndereco>> EnderecosDeEntidade(int id) =>
        await _db.EntidadesEnderecos.Include(e => e.EnderecoTipo).Where(e => e.IdEntidade == id).ToListAsync();
}
