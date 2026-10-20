using LabControl.Data;
using LabControl.Services.Geografia;
using LabControl.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Controllers;

/// <summary>
/// Apoio ao componente de endereço (T11-03 / P25): cidades da UF e busca de CEP. Leitura de dados globais (IBGE):
/// basta estar logado. A tela de cadastro de países/estados/cidades (T11-01…03) é da tarefa 2.3.
/// </summary>
public sealed class GeografiaController(LabDbContext db, ConsultaCepService cep) : Controller
{
    /// <summary>Cidades da UF para o combo (JSON <c>[{ id, nome }]</c>).</summary>
    [HttpGet]
    public async Task<IActionResult> Cidades(int idEstado) =>
        Json(await db.Cidades.AsNoTracking().Where(c => c.IdEstados == idEstado).OrderBy(c => c.Nome)
            .Select(c => new OpcaoVM(c.IdCidades, c.Nome)).ToListAsync());

    /// <summary>Endereço do CEP (ViaCEP no servidor, cache de 1 dia); erro → 422 <c>{ erro }</c>.</summary>
    [HttpGet("Geografia/Cep/{numero}")]
    public async Task<IActionResult> Cep(string numero)
    {
        var r = await cep.ConsultarAsync(numero);
        return r.Sucesso ? Json(r.Valor) : UnprocessableEntity(new { erro = r.Mensagem });
    }
}
