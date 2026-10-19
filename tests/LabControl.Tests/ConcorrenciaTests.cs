using LabControl.Infra;
using LabControl.Models;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>Numeração pelo banco sob gravações simultâneas (vários usuários ao mesmo tempo).</summary>
public class ConcorrenciaTests
{
    private const int Paralelos = 8, PorTarefa = 10;

    [Fact]
    public async Task Numeracao_simultanea_na_mesma_empresa_nao_trava_nem_repete()
    {
        await using var c = await Cenario.CriarAsync();
        var erros = new List<string>();

        await Task.WhenAll(Enumerable.Range(0, Paralelos).Select(async t =>
        {
            var ativa = new EmpresaFixa { EmpresaId = c.Ativa.EmpresaId, UsuarioId = c.Ativa.UsuarioId, ProfissionalId = c.Ativa.ProfissionalId };
            await using var db = await BancoTeste.NovoContextoAsync(ativa);
            for (var i = 0; i < PorTarefa; i++)
            {
                db.Add(new Produto { Descricao = $"P{t}-{i}", IdUnidades = c.Unidade.IdUnidades });   // SP_PROXIMO_NUMERO('PRODUTO')
                try { await db.SaveChangesAsync(); }
                catch (Exception ex) { lock (erros) erros.Add(ErrosMySql.Encontrar(ex)?.Message ?? ex.Message); db.ChangeTracker.Clear(); }
            }
        }));

        var codigos = await c.Db.Produtos.Select(p => p.Codigo).ToListAsync();
        Assert.True(erros.Count == 0, $"{erros.Count} falhas:\n" + string.Join("\n", erros.Distinct()));
        Assert.Equal(codigos.Count, codigos.Distinct().Count());
        Assert.Equal(1 + Paralelos * PorTarefa, codigos.Count);
    }

    [Fact]
    public async Task Primeiro_item_de_propostas_diferentes_ao_mesmo_tempo_nao_trava()
    {
        await using var c = await Cenario.CriarAsync();
        var erros = new List<string>();
        var modelo = await c.Db.LabPropostas.AsNoTracking().SingleAsync();

        await Task.WhenAll(Enumerable.Range(0, Paralelos).Select(async t =>
        {
            var ativa = new EmpresaFixa { EmpresaId = c.Ativa.EmpresaId, UsuarioId = c.Ativa.UsuarioId, ProfissionalId = c.Ativa.ProfissionalId };
            await using var db = await BancoTeste.NovoContextoAsync(ativa);
            for (var i = 0; i < PorTarefa; i++)
            {
                try
                {
                    var p = new LabProposta
                    {
                        IdEntidades = modelo.IdEntidades, IdEntidadesFunc = modelo.IdEntidadesFunc, IdEntEndLaudo = modelo.IdEntEndLaudo,
                        IdEntContatoResultado = modelo.IdEntContatoResultado, IdEntCobranca = modelo.IdEntCobranca,
                        IdLabPropostasStatus = modelo.IdLabPropostasStatus, IdCondicoesPagtos = modelo.IdCondicoesPagtos,
                    };
                    db.Add(p);
                    await db.SaveChangesAsync();                                   // SP_PROXIMO_NUMERO('PROPOSTA')
                    db.Add(new LabPropostaProduto { IdLabPropostas = p.IdLabPropostas, IdProdutos = c.Produto.IdProdutos, QtdeAmostras = 1 });
                    await db.SaveChangesAsync();                                   // ITEM = MAX + 1 (TG_PROPPROD_BI)
                }
                catch (Exception ex) { lock (erros) erros.Add(ErrosMySql.Encontrar(ex)?.Message ?? ex.Message); db.ChangeTracker.Clear(); }
            }
        }));

        Assert.True(erros.Count == 0, $"{erros.Count} falhas:\n" + string.Join("\n", erros.Distinct()));
        var numeros = await c.Db.LabPropostas.Select(p => p.CodProposta).ToListAsync();
        Assert.Equal(numeros.Count, numeros.Distinct().Count());
    }
}
