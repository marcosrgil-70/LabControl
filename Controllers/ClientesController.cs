using LabControl.Data;
using LabControl.Models.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Controllers;

public class ClientesController : Controller
{
    private readonly ApplicationDbContext _db;
    public ClientesController(ApplicationDbContext db) => _db = db;

    // ─── Index ───────────────────────────────────────────────────────────────

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

        return View(await query.OrderBy(e => e.Nome).ToListAsync());
    }

    // ─── Criar ───────────────────────────────────────────────────────────────

    public IActionResult Criar() => View(new Entidade { TipoCliente = true });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(Entidade model, string? cpf, string? cnpj,
        string? nomeFantasia, string? inscEstadual, string? rg, DateTime? dataNascimento,
        string? sexo, string? ddd, string? fone)
    {
        model.TipoCliente  = true;
        model.DataCadastro = DateTime.Now;
        _db.Entidades.Add(model);
        await _db.SaveChangesAsync();

        if (model.Categoria == "F")
            _db.EntidadesPF.Add(new EntidadePF { Id = model.Id, Cpf = cpf, Nome = model.Nome, Rg = rg, DataNascimento = dataNascimento, Sexo = sexo });
        else
            _db.EntidadesPJ.Add(new EntidadePJ { Id = model.Id, Cnpj = cnpj, NomeFantasia = nomeFantasia, InscricaoEstadual = inscEstadual });

        if (!string.IsNullOrWhiteSpace(fone))
            _db.EntidadesFones.Add(new EntidadeFone { IdEntidade = model.Id, Ddd = ddd, Fone = fone });

        await _db.SaveChangesAsync();
        TempData["Sucesso"] = $"Cliente \"{model.Nome}\" cadastrado com sucesso!";
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
    public async Task<IActionResult> Editar(int id, Entidade model, string? cpf, string? cnpj,
        string? nomeFantasia, string? inscEstadual)
    {
        var entidade = await _db.Entidades
            .Include(e => e.PessoaFisica)
            .Include(e => e.PessoaJuridica)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (entidade == null) return NotFound();

        entidade.Nome    = model.Nome;
        entidade.Inativo = model.Inativo;

        if (entidade.Categoria == "F" && entidade.PessoaFisica != null)
            entidade.PessoaFisica.Cpf = cpf;
        else if (entidade.Categoria == "J" && entidade.PessoaJuridica != null)
        {
            entidade.PessoaJuridica.Cnpj              = cnpj;
            entidade.PessoaJuridica.NomeFantasia       = nomeFantasia;
            entidade.PessoaJuridica.InscricaoEstadual  = inscEstadual;
        }

        await _db.SaveChangesAsync();
        TempData["Sucesso"] = $"Dados de \"{entidade.Nome}\" atualizados!";
        return RedirectToAction(nameof(Editar), new { id });
    }

    // ─── AJAX: Telefones ──────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdicionarFone(int idEntidade, string? ddd, string? fone, int? idFoneTipo)
    {
        if (string.IsNullOrWhiteSpace(fone))
            return BadRequest("Número obrigatório.");

        _db.EntidadesFones.Add(new EntidadeFone
        {
            IdEntidade = idEntidade, Ddd = ddd?.Trim(), Fone = fone.Trim(), IdFoneTipo = idFoneTipo
        });
        await _db.SaveChangesAsync();
        return PartialView("_GridFones", await FonesDeEntidade(idEntidade));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirFone(int id, int idEntidade)
    {
        var fone = await _db.EntidadesFones.FindAsync(id);
        if (fone != null) { _db.EntidadesFones.Remove(fone); await _db.SaveChangesAsync(); }
        return PartialView("_GridFones", await FonesDeEntidade(idEntidade));
    }

    // ─── AJAX: E-mails ────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdicionarEmail(int idEntidade, string? email, bool principal)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("E-mail obrigatório.");

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
        var email = await _db.EntidadesEmails.FindAsync(id);
        if (email != null) { _db.EntidadesEmails.Remove(email); await _db.SaveChangesAsync(); }
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

    // ─── AJAX: Endereços ──────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdicionarEndereco(int idEntidade, int? idEnderecoTipo,
        string? logradouro, string? numero, string? complemento,
        string? bairro, string? cidade, string? uf, string? cep)
    {
        _db.EntidadesEnderecos.Add(new EntidadeEndereco
        {
            IdEntidade    = idEntidade,
            IdEnderecoTipo = idEnderecoTipo,
            Logradouro    = logradouro?.Trim(),
            Numero        = numero?.Trim(),
            Complemento   = complemento?.Trim(),
            Bairro        = bairro?.Trim(),
            Cidade        = cidade?.Trim(),
            Uf            = uf?.Trim().ToUpper(),
            Cep           = cep?.Trim()
        });
        await _db.SaveChangesAsync();
        return PartialView("_GridEnderecos", await EnderecosDeEntidade(idEntidade));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExcluirEndereco(int id, int idEntidade)
    {
        var end = await _db.EntidadesEnderecos.FindAsync(id);
        if (end != null) { _db.EntidadesEnderecos.Remove(end); await _db.SaveChangesAsync(); }
        return PartialView("_GridEnderecos", await EnderecosDeEntidade(idEntidade));
    }

    // ─── AJAX: Observações ────────────────────────────────────────────────────

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

    // ─── Busca AJAX (amostras) ────────────────────────────────────────────────

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

    // ─── Helpers privados ─────────────────────────────────────────────────────

    private async Task<Entidade?> CarregarCompleto(int id) =>
        await _db.Entidades
            .Include(e => e.PessoaFisica)
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

    private async Task<List<EntidadeFone>>    FonesDeEntidade(int id)     =>
        await _db.EntidadesFones.Include(f => f.FoneTipo).Where(f => f.IdEntidade == id).ToListAsync();

    private async Task<List<EntidadeEmail>>   EmailsDeEntidade(int id)    =>
        await _db.EntidadesEmails.Where(e => e.IdEntidade == id).ToListAsync();

    private async Task<List<EntidadeEndereco>> EnderecosDeEntidade(int id) =>
        await _db.EntidadesEnderecos.Include(e => e.EnderecoTipo).Where(e => e.IdEntidade == id).ToListAsync();
}
