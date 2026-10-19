using LabControl.Data;
using LabControl.Models.Laboratorio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Controllers.Laboratorio;

public class PropostasController : Controller
{
    private readonly ApplicationDbContext _db;
    public PropostasController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var propostas = await _db.Propostas
            .Include(p => p.Entidade)
            .Include(p => p.Status)
            .Include(p => p.Moeda)
            .Include(p => p.Analises)
            .OrderByDescending(p => p.AnoProposta)
            .ThenByDescending(p => p.CodProposta)
            .ThenByDescending(p => p.RevProposta)
            .ToListAsync();
        return View(propostas);
    }

    public async Task<IActionResult> Detalhes(int id)
    {
        var proposta = await _db.Propostas
            .Include(p => p.Entidade).ThenInclude(e => e.PessoaFisica)
            .Include(p => p.Entidade).ThenInclude(e => e.PessoaJuridica)
            .Include(p => p.Status)
            .Include(p => p.CondicaoPagamento)
            .Include(p => p.Moeda)
            .Include(p => p.Analises).ThenInclude(a => a.Produto)
            .Include(p => p.Analises).ThenInclude(a => a.AnaliseTipo)
            .Include(p => p.Analises).ThenInclude(a => a.AnaliseMetodo)
            .Include(p => p.Analises).ThenInclude(a => a.ParametroAnalise)
            .Include(p => p.Analises).ThenInclude(a => a.Prazo)
            .Include(p => p.Analises).ThenInclude(a => a.Idioma)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (proposta == null) return NotFound();

        await CarregarFormulario();
        return View(proposta);
    }

    public async Task<IActionResult> Criar()
    {
        await CarregarFormulario();
        return View(new Proposta
        {
            AnoProposta = DateTime.Now.Year % 100,
            DtSolicitacao = DateTime.Now,
            IdStatus = 1
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(Proposta model)
    {
        if (model.IdEntidade == 0)
            ModelState.AddModelError("IdEntidade", "Cliente é obrigatório.");
        if (!model.IdStatus.HasValue || model.IdStatus == 0)
            ModelState.AddModelError("IdStatus", "Status é obrigatório.");

        if (!ModelState.IsValid)
        {
            await CarregarFormulario();
            return View(model);
        }

        var empresaId = HttpContext.Session.GetInt32("EmpresaId") ?? 1;
        var funcionarioId = HttpContext.Session.GetInt32("UsuarioId");

        // ANO_PROPOSTA = 2 últimos dígitos do ano (igual ao Delphi)
        model.AnoProposta = model.DtSolicitacao.Year % 100;

        // COD_PROPOSTA sequencial por empresa (equivalente ao SP_GET_COD_PROPOSTAS)
        var ultimoCod = await _db.Propostas
            .Where(p => p.IdEmpresa == empresaId)
            .MaxAsync(p => (int?)p.CodProposta) ?? 0;

        model.CodProposta = ultimoCod + 1;
        model.RevProposta = 0;
        model.IdEmpresa = empresaId;
        model.IdFuncionario = funcionarioId;

        _db.Propostas.Add(model);
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"Proposta {model.CodigoFormatado} criada com sucesso!";
        return RedirectToAction(nameof(Detalhes), new { id = model.Id });
    }

    public async Task<IActionResult> Editar(int id)
    {
        var proposta = await _db.Propostas.FindAsync(id);
        if (proposta == null) return NotFound();

        await CarregarFormulario();
        return View(proposta);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, Proposta model)
    {
        if (id != model.Id) return BadRequest();

        if (model.IdEntidade == 0)
            ModelState.AddModelError("IdEntidade", "Cliente é obrigatório.");
        if (!model.IdStatus.HasValue || model.IdStatus == 0)
            ModelState.AddModelError("IdStatus", "Status é obrigatório.");

        if (!ModelState.IsValid)
        {
            await CarregarFormulario();
            return View(model);
        }

        // Mantém AnoProposta derivado da data de solicitação
        model.AnoProposta = model.DtSolicitacao.Year % 100;

        _db.Propostas.Update(model);
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"Proposta {model.CodigoFormatado} atualizada!";
        return RedirectToAction(nameof(Detalhes), new { id = model.Id });
    }

    // POST: adicionar item de análise à proposta
    [HttpPost]
    public async Task<IActionResult> AdicionarItem(int idProposta,
        int? idProduto, int? idAnaliseTipo, int? idAnaliseMetodo,
        int? idParametro, int? idIdioma, int? idPrazo,
        string? tipoDocumento, int qtde, decimal vrUnitario, decimal porcDesconto)
    {
        // Cálculo igual ao Delphi:
        // VR_SUBTOTAL = QTDE * VR_UNITARIO
        // VR_DESCONTO = round(VR_SUBTOTAL * PORC_DESCONTO/100, 2)
        // VR_TOTAL    = round(VR_SUBTOTAL - VR_DESCONTO, 2)
        var vrSubtotal = vrUnitario * qtde;
        var vrDesconto = Math.Round(vrSubtotal * (porcDesconto / 100m), 2);
        var vrTotal    = Math.Round(vrSubtotal - vrDesconto, 2);

        var item = new PropostaAnalise
        {
            IdProposta        = idProposta,
            IdProduto         = idProduto,
            IdAnaliseTipo     = idAnaliseTipo,
            IdAnaliseMetodo   = idAnaliseMetodo,
            IdParametroAnalise = idParametro,
            IdIdioma          = idIdioma,
            IdPrazo           = idPrazo,
            TipoDocumento     = tipoDocumento,
            QtdeAmostras      = qtde,
            VrUnitario        = vrUnitario,
            VrSubtotal        = vrSubtotal,
            PorcDesconto      = porcDesconto,
            VrDesconto        = vrDesconto,
            VrTotal           = vrTotal
        };
        _db.PropostasAnalises.Add(item);
        await _db.SaveChangesAsync();

        await RecalcularTotal(idProposta);

        TempData["Sucesso"] = "Item adicionado!";
        return RedirectToAction(nameof(Detalhes), new { id = idProposta });
    }

    [HttpPost]
    public async Task<IActionResult> RemoverItem(int idItem, int idProposta)
    {
        var item = await _db.PropostasAnalises.FindAsync(idItem);
        if (item != null)
        {
            _db.PropostasAnalises.Remove(item);
            await _db.SaveChangesAsync();
            await RecalcularTotal(idProposta);
        }
        return RedirectToAction(nameof(Detalhes), new { id = idProposta });
    }

    [HttpPost]
    public async Task<IActionResult> AplicarDesconto(int id, decimal porcDesconto)
    {
        var proposta = await _db.Propostas
            .Include(p => p.Analises)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (proposta != null)
        {
            // Subtotal = soma dos VR_TOTAL dos itens (VR_TOTAL já descontado por item)
            var subtotal = proposta.Analises.Sum(a => a.VrTotal);
            proposta.PorcDesconto = porcDesconto;
            proposta.VrDesconto   = Math.Round(subtotal * (porcDesconto / 100m), 2);
            proposta.VrTotal      = subtotal - (proposta.VrDesconto ?? 0);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Detalhes), new { id });
    }

    // Recalcula o total da proposta somando os itens
    private async Task RecalcularTotal(int idProposta)
    {
        var proposta = await _db.Propostas
            .Include(p => p.Analises)
            .FirstOrDefaultAsync(p => p.Id == idProposta);

        if (proposta == null) return;

        var subtotal = proposta.Analises.Sum(a => a.VrTotal);
        proposta.VrDesconto = Math.Round(subtotal * ((proposta.PorcDesconto ?? 0) / 100m), 2);
        proposta.VrTotal    = subtotal - (proposta.VrDesconto ?? 0);
        await _db.SaveChangesAsync();
    }

    private async Task CarregarFormulario()
    {
        ViewBag.PropostasStatus = await _db.PropostasStatus.OrderBy(s => s.Descricao).ToListAsync();
        ViewBag.CondicoesPagamentos = await _db.CondicoesPagamentos.OrderBy(c => c.Descricao).ToListAsync();
        ViewBag.Moedas = await _db.Moedas.OrderBy(m => m.Descricao).ToListAsync();

        // Clientes: entidades com flag TipoCliente ou que tenham propostas
        ViewBag.Clientes = await _db.Entidades
            .Where(e => !e.Inativo)
            .OrderBy(e => e.Nome)
            .ToListAsync();

        ViewBag.Funcionarios = await _db.EntidadesFuncionarios
            .Include(f => f.Entidade)
            .Where(f => f.Entidade != null)
            .OrderBy(f => f.Entidade!.Nome)
            .ToListAsync();

        ViewBag.Produtos = await _db.Produtos.OrderBy(p => p.Descricao).ToListAsync();
        ViewBag.AnalisesTipos = await _db.AnalisesTipos.OrderBy(a => a.Descricao).ToListAsync();
        ViewBag.AnalisesMetodos = await _db.AnalisesMetodos.OrderBy(a => a.Descricao).ToListAsync();
        ViewBag.Prazos = await _db.Prazos.OrderBy(p => p.Descricao).ToListAsync();
        ViewBag.Idiomas = await _db.Idiomas.OrderBy(i => i.Descricao).ToListAsync();
        ViewBag.Parametros = await _db.ParametrosAnalises.OrderBy(p => p.Descricao).ToListAsync();
    }
}
