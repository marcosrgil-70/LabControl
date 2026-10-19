using LabControl.Data;
using LabControl.Models.Entidades;
using LabControl.Models.Laboratorio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Controllers;

public class TabelasAuxiliaresController : Controller
{
    private readonly ApplicationDbContext _db;
    public TabelasAuxiliaresController(ApplicationDbContext db) => _db = db;

    public IActionResult Index() => View();

    // ── TIPOS DE AMOSTRA ──────────────────────────────────────────────
    public async Task<IActionResult> AmostrasTipos() =>
        View("Lista", new ListaAuxiliar("Tipos de Amostra", "AmostraTipo",
            await _db.AmostrasTipos.OrderBy(x => x.Descricao).ToListAsync<object>()));

    [HttpPost]
    public async Task<IActionResult> SalvarAmostraTipo(int? id, string descricao)
    {
        if (id.HasValue) { var r = await _db.AmostrasTipos.FindAsync(id); if (r != null) r.Descricao = descricao; }
        else _db.AmostrasTipos.Add(new AmostraTipo { Descricao = descricao });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Tipo de amostra salvo!";
        return RedirectToAction(nameof(AmostrasTipos));
    }

    // ── STATUS DE AMOSTRA ─────────────────────────────────────────────
    public async Task<IActionResult> AmostrasStatus() =>
        View("ListaStatus", new ListaAuxiliar("Status de Amostra", "AmostraStatus",
            await _db.AmostrasStatus.OrderBy(x => x.Descricao).ToListAsync<object>()));

    [HttpPost]
    public async Task<IActionResult> SalvarAmostraStatus(int? id, string descricao, string? cor)
    {
        if (id.HasValue) { var r = await _db.AmostrasStatus.FindAsync(id); if (r != null) { r.Descricao = descricao; r.Cor = cor; } }
        else _db.AmostrasStatus.Add(new AmostraStatus { Descricao = descricao, Cor = cor });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Status de amostra salvo!";
        return RedirectToAction(nameof(AmostrasStatus));
    }

    // ── TIPOS DE ANÁLISE ──────────────────────────────────────────────
    public async Task<IActionResult> AnalisesTipos() =>
        View("Lista", new ListaAuxiliar("Tipos de Análise", "AnaliseTipo",
            await _db.AnalisesTipos.OrderBy(x => x.Descricao).ToListAsync<object>()));

    [HttpPost]
    public async Task<IActionResult> SalvarAnaliseTipo(int? id, string descricao)
    {
        if (id.HasValue) { var r = await _db.AnalisesTipos.FindAsync(id); if (r != null) r.Descricao = descricao; }
        else _db.AnalisesTipos.Add(new AnaliseTipo { Descricao = descricao });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Tipo de análise salvo!";
        return RedirectToAction(nameof(AnalisesTipos));
    }

    // ── STATUS DE ANÁLISE ─────────────────────────────────────────────
    public async Task<IActionResult> AnalisesStatus() =>
        View("ListaStatus", new ListaAuxiliar("Status de Análise", "AnaliseStatus",
            await _db.AnalisesStatus.OrderBy(x => x.Descricao).ToListAsync<object>()));

    [HttpPost]
    public async Task<IActionResult> SalvarAnaliseStatus(int? id, string descricao, string? cor)
    {
        if (id.HasValue) { var r = await _db.AnalisesStatus.FindAsync(id); if (r != null) { r.Descricao = descricao; r.Cor = cor; } }
        else _db.AnalisesStatus.Add(new AnaliseStatus { Descricao = descricao, Cor = cor });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Status de análise salvo!";
        return RedirectToAction(nameof(AnalisesStatus));
    }

    // ── MÉTODOS DE ANÁLISE ────────────────────────────────────────────
    public async Task<IActionResult> AnalisesMetodos() =>
        View("Lista", new ListaAuxiliar("Métodos de Análise", "AnaliseMetodo",
            await _db.AnalisesMetodos.OrderBy(x => x.Descricao).ToListAsync<object>()));

    [HttpPost]
    public async Task<IActionResult> SalvarAnaliseMetodo(int? id, string descricao)
    {
        if (id.HasValue) { var r = await _db.AnalisesMetodos.FindAsync(id); if (r != null) r.Descricao = descricao; }
        else _db.AnalisesMetodos.Add(new AnaliseMetodo { Descricao = descricao });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Método de análise salvo!";
        return RedirectToAction(nameof(AnalisesMetodos));
    }

    // ── STATUS DE BOLETIM ─────────────────────────────────────────────
    public async Task<IActionResult> BoletinsStatus() =>
        View("ListaStatus", new ListaAuxiliar("Status de Boletim", "BoletimStatus",
            await _db.BoletinsStatus.OrderBy(x => x.Descricao).ToListAsync<object>()));

    [HttpPost]
    public async Task<IActionResult> SalvarBoletimStatus(int? id, string descricao, string? cor)
    {
        if (id.HasValue) { var r = await _db.BoletinsStatus.FindAsync(id); if (r != null) { r.Descricao = descricao; r.Cor = cor; } }
        else _db.BoletinsStatus.Add(new BoletimStatus { Descricao = descricao, Cor = cor });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Status de boletim salvo!";
        return RedirectToAction(nameof(BoletinsStatus));
    }

    // ── STATUS DE PROPOSTA ────────────────────────────────────────────
    public async Task<IActionResult> PropostasStatus() =>
        View("ListaStatus", new ListaAuxiliar("Status de Proposta", "PropostaStatus",
            await _db.PropostasStatus.OrderBy(x => x.Descricao).ToListAsync<object>()));

    [HttpPost]
    public async Task<IActionResult> SalvarPropostaStatus(int? id, string descricao, string? cor)
    {
        if (id.HasValue) { var r = await _db.PropostasStatus.FindAsync(id); if (r != null) { r.Descricao = descricao; r.Cor = cor; } }
        else _db.PropostasStatus.Add(new PropostaStatus { Descricao = descricao, Cor = cor });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Status de proposta salvo!";
        return RedirectToAction(nameof(PropostasStatus));
    }

    // ── IDIOMAS ───────────────────────────────────────────────────────
    public async Task<IActionResult> Idiomas() =>
        View("Lista", new ListaAuxiliar("Idiomas", "Idioma",
            await _db.Idiomas.OrderBy(x => x.Descricao).ToListAsync<object>()));

    [HttpPost]
    public async Task<IActionResult> SalvarIdioma(int? id, string descricao)
    {
        if (id.HasValue) { var r = await _db.Idiomas.FindAsync(id); if (r != null) r.Descricao = descricao; }
        else _db.Idiomas.Add(new Idioma { Descricao = descricao });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Idioma salvo!";
        return RedirectToAction(nameof(Idiomas));
    }

    // ── PRAZOS ────────────────────────────────────────────────────────
    public async Task<IActionResult> Prazos() =>
        View("ListaPrazos", await _db.Prazos.OrderBy(x => x.Descricao).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> SalvarPrazo(int? id, string descricao, int qtde)
    {
        if (id.HasValue) { var r = await _db.Prazos.FindAsync(id); if (r != null) { r.Descricao = descricao; r.QtdeDias = qtde; } }
        else _db.Prazos.Add(new Prazo { Descricao = descricao, QtdeDias = qtde });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Prazo salvo!";
        return RedirectToAction(nameof(Prazos));
    }

    // ── UNIDADES ──────────────────────────────────────────────────────
    public async Task<IActionResult> Unidades() =>
        View("ListaUnidades", await _db.Unidades.OrderBy(x => x.Descricao).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> SalvarUnidade(int? id, string descricao, string sigla)
    {
        if (id.HasValue) { var r = await _db.Unidades.FindAsync(id); if (r != null) { r.Descricao = descricao; r.Sigla = sigla; } }
        else _db.Unidades.Add(new Unidade { Descricao = descricao, Sigla = sigla });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Unidade salva!";
        return RedirectToAction(nameof(Unidades));
    }

    // ── MOEDAS ────────────────────────────────────────────────────────
    public async Task<IActionResult> Moedas() =>
        View("ListaMoedas", await _db.Moedas.OrderBy(x => x.Descricao).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> SalvarMoeda(int? id, string descricao, string sigla)
    {
        if (id.HasValue) { var r = await _db.Moedas.FindAsync(id); if (r != null) { r.Descricao = descricao; r.Sigla = sigla; } }
        else _db.Moedas.Add(new Moeda { Descricao = descricao, Sigla = sigla });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Moeda salva!";
        return RedirectToAction(nameof(Moedas));
    }

    // ── TIPOS DE EMBALAGEM ────────────────────────────────────────────
    public async Task<IActionResult> EmbalagensTopos() =>
        View("Lista", new ListaAuxiliar("Tipos de Embalagem", "EmbalagemTipo",
            await _db.EmbalagensTopos.OrderBy(x => x.Descricao).ToListAsync<object>()));

    [HttpPost]
    public async Task<IActionResult> SalvarEmbalagemTipo(int? id, string descricao)
    {
        if (id.HasValue) { var r = await _db.EmbalagensTopos.FindAsync(id); if (r != null) r.Descricao = descricao; }
        else _db.EmbalagensTopos.Add(new EmbalagemTipo { Descricao = descricao });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Tipo de embalagem salvo!";
        return RedirectToAction(nameof(EmbalagensTopos));
    }

    // ── TIPOS DE RESULTADO ────────────────────────────────────────────
    public async Task<IActionResult> TiposResultados() =>
        View("Lista", new ListaAuxiliar("Tipos de Resultado", "TipoResultado",
            await _db.TiposResultados.OrderBy(x => x.Descricao).ToListAsync<object>()));

    [HttpPost]
    public async Task<IActionResult> SalvarTipoResultado(int? id, string descricao)
    {
        if (id.HasValue) { var r = await _db.TiposResultados.FindAsync(id); if (r != null) r.Descricao = descricao; }
        else _db.TiposResultados.Add(new TipoResultado { Descricao = descricao });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Tipo de resultado salvo!";
        return RedirectToAction(nameof(TiposResultados));
    }

    // ── CONDIÇÕES DE PAGAMENTO ────────────────────────────────────────
    public async Task<IActionResult> CondicoesPagamentos() =>
        View("ListaCondicaoPagamentos", await _db.CondicoesPagamentos.OrderBy(x => x.Descricao).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> SalvarCondicaoPagamento(int? id, string codigo, string descricao)
    {
        if (id.HasValue) { var r = await _db.CondicoesPagamentos.FindAsync(id); if (r != null) { r.Codigo = codigo; r.Descricao = descricao; } }
        else _db.CondicoesPagamentos.Add(new CondicaoPagamento { Codigo = codigo, Descricao = descricao });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Condição de pagamento salva!";
        return RedirectToAction(nameof(CondicoesPagamentos));
    }

    // ── TIPOS DE ENDEREÇO ─────────────────────────────────────────────
    public async Task<IActionResult> EnderecosTipos() =>
        View("Lista", new ListaAuxiliar("Tipos de Endereço", "EnderecoTipo",
            await _db.EnderecosTipos.OrderBy(x => x.Descricao).ToListAsync<object>()));

    [HttpPost]
    public async Task<IActionResult> SalvarEnderecoTipo(int? id, string descricao)
    {
        if (id.HasValue) { var r = await _db.EnderecosTipos.FindAsync(id); if (r != null) r.Descricao = descricao; }
        else _db.EnderecosTipos.Add(new EnderecoTipo { Descricao = descricao });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Tipo de endereço salvo!";
        return RedirectToAction(nameof(EnderecosTipos));
    }

    // ── REGISTROS PROFISSIONAIS ────────────────────────────────────────
    public async Task<IActionResult> RegistrosProfissionais() =>
        View("ListaRegProfissional", await _db.TiposRegProfissional.OrderBy(x => x.Descricao).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> SalvarTipoRegProfissional(int? id, string descricao, string? nomenclatura)
    {
        if (id.HasValue)
        {
            var r = await _db.TiposRegProfissional.FindAsync(id);
            if (r != null) { r.Descricao = descricao; r.Nomenclatura = nomenclatura; }
        }
        else _db.TiposRegProfissional.Add(new TipoRegProfissional { Descricao = descricao, Nomenclatura = nomenclatura });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Registro profissional salvo!";
        return RedirectToAction(nameof(RegistrosProfissionais));
    }

    // ── CARGOS DE FUNCIONÁRIOS ────────────────────────────────────────
    public async Task<IActionResult> CargosFuncionarios() =>
        View("Lista", new ListaAuxiliar("Cargos de Funcionários", "CargoFuncionario",
            await _db.CargosFuncionarios.OrderBy(x => x.Descricao).ToListAsync<object>()));

    [HttpPost]
    public async Task<IActionResult> SalvarCargoFuncionario(int? id, string descricao)
    {
        if (id.HasValue) { var r = await _db.CargosFuncionarios.FindAsync(id); if (r != null) r.Descricao = descricao; }
        else _db.CargosFuncionarios.Add(new CargoFuncionario { Descricao = descricao });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Cargo salvo!";
        return RedirectToAction(nameof(CargosFuncionarios));
    }

    // ── PAÍSES ────────────────────────────────────────────────────────
    public async Task<IActionResult> Paises() =>
        View("ListaPaises", await _db.Paises.OrderBy(x => x.Descricao).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> SalvarPais(int? id, string descricao, string? sigla)
    {
        if (id.HasValue) { var r = await _db.Paises.FindAsync(id); if (r != null) { r.Descricao = descricao; r.Sigla = sigla; } }
        else _db.Paises.Add(new Pais { Descricao = descricao, Sigla = sigla });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "País salvo!";
        return RedirectToAction(nameof(Paises));
    }

    // ── ESTADOS ───────────────────────────────────────────────────────
    public async Task<IActionResult> Estados() =>
        View("ListaEstados", await _db.Estados.OrderBy(x => x.Descricao).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> SalvarEstado(int? id, string descricao, string? sigla)
    {
        if (id.HasValue) { var r = await _db.Estados.FindAsync(id); if (r != null) { r.Descricao = descricao; r.Sigla = sigla; } }
        else _db.Estados.Add(new Estado { Descricao = descricao, Sigla = sigla });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Estado salvo!";
        return RedirectToAction(nameof(Estados));
    }

    // ── CIDADES ───────────────────────────────────────────────────────
    public async Task<IActionResult> Cidades() =>
        View("Lista", new ListaAuxiliar("Cidades", "Cidade",
            await _db.Cidades.OrderBy(x => x.Descricao).ToListAsync<object>()));

    [HttpPost]
    public async Task<IActionResult> SalvarCidade(int? id, string descricao)
    {
        if (id.HasValue) { var r = await _db.Cidades.FindAsync(id); if (r != null) r.Descricao = descricao; }
        else _db.Cidades.Add(new Cidade { Descricao = descricao });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Cidade salva!";
        return RedirectToAction(nameof(Cidades));
    }

    // ── BAIRROS ───────────────────────────────────────────────────────
    public async Task<IActionResult> Bairros() =>
        View("Lista", new ListaAuxiliar("Bairros", "Bairro",
            await _db.Bairros.OrderBy(x => x.Descricao).ToListAsync<object>()));

    [HttpPost]
    public async Task<IActionResult> SalvarBairro(int? id, string descricao)
    {
        if (id.HasValue) { var r = await _db.Bairros.FindAsync(id); if (r != null) r.Descricao = descricao; }
        else _db.Bairros.Add(new Bairro { Descricao = descricao });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Bairro salvo!";
        return RedirectToAction(nameof(Bairros));
    }

    // ── TIPOS DE LOGRADOURO ───────────────────────────────────────────
    public async Task<IActionResult> TiposLogradouros() =>
        View("Lista", new ListaAuxiliar("Tipos de Logradouro", "TipoLogradouro",
            await _db.TiposLogradouros.OrderBy(x => x.Descricao).ToListAsync<object>()));

    [HttpPost]
    public async Task<IActionResult> SalvarTipoLogradouro(int? id, string descricao)
    {
        if (id.HasValue) { var r = await _db.TiposLogradouros.FindAsync(id); if (r != null) r.Descricao = descricao; }
        else _db.TiposLogradouros.Add(new TipoLogradouro { Descricao = descricao });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Tipo de logradouro salvo!";
        return RedirectToAction(nameof(TiposLogradouros));
    }

    // ── LOGRADOUROS ───────────────────────────────────────────────────
    public async Task<IActionResult> Logradouros() =>
        View("Lista", new ListaAuxiliar("Logradouros", "Logradouro",
            await _db.Logradouros.OrderBy(x => x.Descricao).ToListAsync<object>()));

    [HttpPost]
    public async Task<IActionResult> SalvarLogradouro(int? id, string descricao)
    {
        if (id.HasValue) { var r = await _db.Logradouros.FindAsync(id); if (r != null) r.Descricao = descricao; }
        else _db.Logradouros.Add(new Logradouro { Descricao = descricao });
        await _db.SaveChangesAsync();
        TempData["Sucesso"] = "Logradouro salvo!";
        return RedirectToAction(nameof(Logradouros));
    }
}

public record ListaAuxiliar(string Titulo, string Entidade, List<object> Itens);
