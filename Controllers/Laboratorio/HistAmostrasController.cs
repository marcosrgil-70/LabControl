using LabControl.Data;
using LabControl.Models.Laboratorio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Controllers.Laboratorio;

public class HistAmostrasController : Controller
{
    private readonly ApplicationDbContext _db;

    public HistAmostrasController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var amostras = await _db.HistAmostras
            .Include(a => a.AmostraTipo)
            .Include(a => a.AnaliseTipo)
            .Include(a => a.Entidade).ThenInclude(e => e!.PessoaJuridica)
            .Include(a => a.Entidade).ThenInclude(e => e!.PessoaFisica)
            .Include(a => a.AmostraStatus)
            .OrderByDescending(a => a.AnoAmostra)
            .ThenByDescending(a => a.CodAmostra)
            .ToListAsync();

        return View(amostras);
    }

    public async Task<IActionResult> Detalhes(int id)
    {
        var amostra = await _db.HistAmostras
            .Include(a => a.AmostraTipo)
            .Include(a => a.AnaliseTipo)
            .Include(a => a.Entidade).ThenInclude(e => e!.PessoaFisica)
            .Include(a => a.Entidade).ThenInclude(e => e!.PessoaJuridica)
            .Include(a => a.Produto)
            .Include(a => a.EmbalagemTipo)
            .Include(a => a.AmostraStatus)
            .Include(a => a.Saldo)
            .Include(a => a.LocalizacaoAtual)
            .Include(a => a.Testes).ThenInclude(t => t.AnaliseTipo)
            .Include(a => a.Testes).ThenInclude(t => t.AnaliseMetodo)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (amostra == null) return NotFound();

        return View(amostra);
    }

    public async Task<IActionResult> Criar()
    {
        await CarregarDadosFormulario();
        var model = new HistAmostra
        {
            DtEntrega = DateTime.Today,
            HrEntrega = DateTime.Now.ToString("HH:mm"),
            TipoDocumento = "BOLETIM",
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(HistAmostra model)
    {
        if (!model.IdEntidade.HasValue || model.IdEntidade == 0)
            ModelState.AddModelError("IdEntidade", "Cliente é obrigatório.");
        if (!model.IdProposta.HasValue || model.IdProposta == 0)
            ModelState.AddModelError("IdProposta", "Proposta é obrigatória.");
        if (!model.DtEntrega.HasValue)
            ModelState.AddModelError("DtEntrega", "Data de entrega é obrigatória.");
        else if (model.DtEntrega.Value.Date > DateTime.Today)
            ModelState.AddModelError("DtEntrega", "Data de entrega não pode ser futura.");
        if (string.IsNullOrWhiteSpace(model.HrEntrega))
            ModelState.AddModelError("HrEntrega", "Hora de entrega é obrigatória.");
        if (!model.IdEmbalagemTipo.HasValue || model.IdEmbalagemTipo == 0)
            ModelState.AddModelError("IdEmbalagemTipo", "Tipo de embalagem é obrigatório.");
        if (model.IdAmostraTipo == 0)
            ModelState.AddModelError("IdAmostraTipo", "Tipo de amostra é obrigatório.");
        if (!model.IdAnaliseTipo.HasValue || model.IdAnaliseTipo == 0)
            ModelState.AddModelError("IdAnaliseTipo", "Tipo de análise é obrigatório.");
        if (!model.IdFuncionarioResp.HasValue || model.IdFuncionarioResp == 0)
            ModelState.AddModelError("IdFuncionarioResp", "Responsável é obrigatório.");
        if (!model.IdAmostraStatus.HasValue || model.IdAmostraStatus == 0)
            ModelState.AddModelError("IdAmostraStatus", "Status da amostra é obrigatório.");

        if (!ModelState.IsValid)
        {
            await CarregarDadosFormulario(model.IdEntidade, model.IdProposta);
            return View(model);
        }

        var empresaId = HttpContext.Session.GetInt32("EmpresaId") ?? 1;
        var funcionarioId = HttpContext.Session.GetInt32("UsuarioId");

        // ANO_AMOSTRA = 2 últimos dígitos do ano da data de entrega (igual ao Delphi)
        model.AnoAmostra = model.DtEntrega!.Value.Year % 100;

        // Próximo COD_AMOSTRA sequencial por empresa (equivalente ao SP_GET_COD_AMOSTRA)
        var ultimoCod = await _db.HistAmostras
            .Where(a => a.IdEmpresa == empresaId)
            .MaxAsync(a => (int?)a.CodAmostra) ?? 0;

        model.CodAmostra = ultimoCod + 1;
        model.IdEmpresa = empresaId;
        model.IdFuncionarioDigitador = funcionarioId;

        if (string.IsNullOrWhiteSpace(model.TipoDocumento))
            model.TipoDocumento = "BOLETIM";

        _db.HistAmostras.Add(model);
        await _db.SaveChangesAsync();

        // Movimentação de entrada e saldo inicial
        var mov = new MovAmostra
        {
            IdHistAmostra = model.Id,
            IdEmpresa = empresaId,
            Qtde = model.QtdeEmbalagensEntregue ?? 1,
            EntradaSaida = "E",
            DataMov = DateTime.Now,
            Justificativa = "Entrada inicial de amostra"
        };
        _db.MovAmostras.Add(mov);

        var saldo = new HistAmostraSaldo
        {
            IdHistAmostra = model.Id,
            IdEmpresa = empresaId,
            SaldoAtual = mov.Qtde,
            DataAtualizacao = DateTime.Now
        };
        _db.HistAmostrasaldos.Add(saldo);

        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"Amostra {model.CodigoFormatado} registrada com sucesso!";
        return RedirectToAction(nameof(Detalhes), new { id = model.Id });
    }

    public async Task<IActionResult> Editar(int id)
    {
        var amostra = await _db.HistAmostras.FindAsync(id);
        if (amostra == null) return NotFound();

        await CarregarDadosFormulario(amostra.IdEntidade, amostra.IdProposta);
        return View(amostra);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, HistAmostra model)
    {
        if (id != model.Id) return BadRequest();

        if (!model.IdEntidade.HasValue || model.IdEntidade == 0)
            ModelState.AddModelError("IdEntidade", "Cliente é obrigatório.");
        if (!model.IdProposta.HasValue || model.IdProposta == 0)
            ModelState.AddModelError("IdProposta", "Proposta é obrigatória.");
        if (!model.DtEntrega.HasValue)
            ModelState.AddModelError("DtEntrega", "Data de entrega é obrigatória.");
        else if (model.DtEntrega.Value.Date > DateTime.Today)
            ModelState.AddModelError("DtEntrega", "Data de entrega não pode ser futura.");
        if (string.IsNullOrWhiteSpace(model.HrEntrega))
            ModelState.AddModelError("HrEntrega", "Hora de entrega é obrigatória.");
        if (!model.IdEmbalagemTipo.HasValue || model.IdEmbalagemTipo == 0)
            ModelState.AddModelError("IdEmbalagemTipo", "Tipo de embalagem é obrigatório.");
        if (model.IdAmostraTipo == 0)
            ModelState.AddModelError("IdAmostraTipo", "Tipo de amostra é obrigatório.");
        if (!model.IdAnaliseTipo.HasValue || model.IdAnaliseTipo == 0)
            ModelState.AddModelError("IdAnaliseTipo", "Tipo de análise é obrigatório.");
        if (!model.IdFuncionarioResp.HasValue || model.IdFuncionarioResp == 0)
            ModelState.AddModelError("IdFuncionarioResp", "Responsável é obrigatório.");
        if (!model.IdAmostraStatus.HasValue || model.IdAmostraStatus == 0)
            ModelState.AddModelError("IdAmostraStatus", "Status da amostra é obrigatório.");

        if (!ModelState.IsValid)
        {
            await CarregarDadosFormulario(model.IdEntidade, model.IdProposta);
            return View(model);
        }

        model.AnoAmostra = model.DtEntrega!.Value.Year % 100;

        if (string.IsNullOrWhiteSpace(model.TipoDocumento))
            model.TipoDocumento = "BOLETIM";

        _db.HistAmostras.Update(model);
        await _db.SaveChangesAsync();

        TempData["Sucesso"] = $"Amostra {model.CodigoFormatado} atualizada com sucesso!";
        return RedirectToAction(nameof(Detalhes), new { id = model.Id });
    }

    // AJAX: propostas filtradas por cliente
    public async Task<IActionResult> PropostasPorCliente(int idCliente)
    {
        var propostas = await _db.Propostas
            .Where(p => p.IdEntidade == idCliente)
            .OrderByDescending(p => p.AnoProposta)
            .ThenByDescending(p => p.CodProposta)
            .Select(p => new { id = p.Id, texto = $"{p.CodProposta}/{p.AnoProposta}" })
            .ToListAsync();

        return Json(propostas);
    }

    // AJAX: produtos distintos da proposta
    public async Task<IActionResult> ProdutosPorProposta(int idProposta)
    {
        var produtos = await _db.PropostasAnalises
            .Where(pa => pa.IdProposta == idProposta && pa.IdProduto != null)
            .Include(pa => pa.Produto)
            .Select(pa => new { id = pa.IdProduto, texto = pa.Produto!.Descricao })
            .Distinct()
            .OrderBy(p => p.texto)
            .ToListAsync();

        return Json(produtos);
    }

    // AJAX: tipos de análise da proposta + produto
    public async Task<IActionResult> AnaliseTiposPorProposta(int idProposta, int idProduto)
    {
        var tipos = await _db.PropostasAnalises
            .Where(pa => pa.IdProposta == idProposta && pa.IdProduto == idProduto && pa.IdAnaliseTipo != null)
            .Include(pa => pa.AnaliseTipo)
            .Select(pa => new { id = pa.IdAnaliseTipo, texto = pa.AnaliseTipo!.Descricao })
            .Distinct()
            .OrderBy(p => p.texto)
            .ToListAsync();

        return Json(tipos);
    }

    private async Task CarregarDadosFormulario(int? idClienteSelecionado = null, int? idPropostaSelecionada = null)
    {
        ViewBag.Clientes = await _db.Entidades
            .Where(e => _db.Propostas.Any(p => p.IdEntidade == e.Id))
            .Include(e => e.PessoaJuridica)
            .Include(e => e.PessoaFisica)
            .OrderBy(e => e.Nome)
            .ToListAsync();

        ViewBag.Propostas = idClienteSelecionado.HasValue && idClienteSelecionado > 0
            ? await _db.Propostas
                .Where(p => p.IdEntidade == idClienteSelecionado)
                .OrderByDescending(p => p.AnoProposta)
                .ThenByDescending(p => p.CodProposta)
                .ToListAsync()
            : new List<Proposta>();

        ViewBag.Produtos = idPropostaSelecionada.HasValue && idPropostaSelecionada > 0
            ? await _db.PropostasAnalises
                .Where(pa => pa.IdProposta == idPropostaSelecionada && pa.IdProduto != null)
                .Include(pa => pa.Produto)
                .Select(pa => pa.Produto!)
                .Distinct()
                .OrderBy(p => p.Descricao)
                .ToListAsync()
            : new List<Produto>();

        ViewBag.AmostrasTipos = await _db.AmostrasTipos.OrderBy(a => a.Descricao).ToListAsync();
        ViewBag.AnalisesTipos = await _db.AnalisesTipos.OrderBy(a => a.Descricao).ToListAsync();
        ViewBag.EmbalagensTopos = await _db.EmbalagensTopos.OrderBy(e => e.Descricao).ToListAsync();
        ViewBag.AmostrasStatus = await _db.AmostrasStatus.OrderBy(s => s.Descricao).ToListAsync();

        ViewBag.Funcionarios = await _db.EntidadesFuncionarios
            .Include(f => f.Entidade)
            .Where(f => f.Entidade != null)
            .OrderBy(f => f.Entidade!.Nome)
            .ToListAsync();
    }
}
