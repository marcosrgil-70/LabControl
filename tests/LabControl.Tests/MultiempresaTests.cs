using LabControl.Models;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>
/// Aceite da 0.4 (docs/processos/P01-multiempresa.md): com 2 empresas as consultas não vazam,
/// a inclusão grava a empresa ativa e trocar/gravar em outra empresa é recusado. Transação revertida.
/// </summary>
public class MultiempresaTests
{
    [Fact]
    public async Task Duas_empresas_nao_se_enxergam_e_nao_gravam_uma_na_outra()
    {
        var ativa = new EmpresaFixa();
        await using var db = await BancoTeste.NovoContextoAsync(ativa);
        await using var tx = await db.Database.BeginTransactionAsync();

        // empresas são globais: criadas sem empresa ativa
        var a = new Empresa { CodEmpresa = "TA1", RazaoSocial = "EMPRESA A" };
        var b = new Empresa { CodEmpresa = "TB1", RazaoSocial = "EMPRESA B" };
        db.AddRange(a, b);
        await db.SaveChangesAsync();
        foreach (var id in new[] { a.IdEmpresas, b.IdEmpresas })
            await db.Database.ExecuteSqlInterpolatedAsync($"CALL SP_INICIALIZAR_EMPRESA({id})");

        // cada empresa grava seus cadastros SEM informar ID_EMPRESAS
        var dados = new Dictionary<int, (Produto Produto, Entidade Cliente)>();
        foreach (var emp in new[] { a, b })
        {
            ativa.EmpresaId = emp.IdEmpresas;
            var uni = new Unidade { Descricao = "UNIDADE " + emp.CodEmpresa, Sigla = "U" };
            db.Add(uni);
            await db.SaveChangesAsync();
            Assert.Equal(emp.IdEmpresas, uni.IdEmpresas);

            var prod = new Produto { Descricao = "PRODUTO " + emp.CodEmpresa, IdUnidades = uni.IdUnidades };
            var cli = new Entidade { Categoria = Categoria.Juridica, Nome = "CLIENTE " + emp.CodEmpresa, TipoCliente = true };
            var cpg = new CondicaoPagto { Codigo = "01", Descricao = "30 DIAS" };
            db.AddRange(prod, cli, cpg);
            await db.SaveChangesAsync();
            db.Add(new CondicaoPagtoParc { IdCondicoesPagtos = cpg.IdCondicoesPagtos, NrParcela = 1 });
            await db.SaveChangesAsync();
            Assert.Equal(emp.IdEmpresas, prod.IdEmpresas);
            Assert.Equal("000001", prod.Codigo); // sequência de produto por empresa
            dados[emp.IdEmpresas] = (prod, cli);
        }
        db.ChangeTracker.Clear();

        // consultas da empresa A: tabelas, filhos sem ID_EMPRESAS, views e busca por id da outra empresa
        ativa.EmpresaId = a.IdEmpresas;
        Assert.Equal(["PRODUTO TA1"], await db.Produtos.Select(p => p.Descricao).ToListAsync());
        Assert.All(await db.Unidades.ToListAsync(), u => Assert.Equal(a.IdEmpresas, u.IdEmpresas));
        Assert.Equal(3, await db.Idiomas.CountAsync());
        Assert.Equal(1, await db.CondicoesPagtosParc.CountAsync());
        Assert.Equal(["CLIENTE TA1"], await db.VwClientes.Select(c => c.Nome).ToListAsync());
        Assert.Null(await db.Produtos.FirstOrDefaultAsync(p => p.IdProdutos == dados[b.IdEmpresas].Produto.IdProdutos));
        Assert.Null(await db.Produtos.FindAsync(dados[b.IdEmpresas].Produto.IdProdutos));
        Assert.Equal(1, await db.EmpresasParametros.CountAsync());

        // inclusão informando outra empresa
        db.Add(new Unidade { IdEmpresas = b.IdEmpresas, Descricao = "INTRUSA", Sigla = "X" });
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        Assert.Contains("outra empresa", ex.Message);
        db.ChangeTracker.Clear();

        // trocar a empresa de um registro
        var prodA = await db.Produtos.SingleAsync();
        prodA.IdEmpresas = b.IdEmpresas;
        ex = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        Assert.Contains("trocar a empresa", ex.Message);
        db.ChangeTracker.Clear();

        // alterar/excluir registro de outra empresa (carregado sem filtro)
        var prodB = await db.Produtos.IgnoreQueryFilters().SingleAsync(p => p.IdProdutos == dados[b.IdEmpresas].Produto.IdProdutos);
        prodB.Descricao = "ALTERADO PELA A";
        ex = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        Assert.Contains("outra empresa", ex.Message);
        db.ChangeTracker.Clear();
        db.Remove(await db.Produtos.IgnoreQueryFilters().SingleAsync(p => p.IdProdutos == dados[b.IdEmpresas].Produto.IdProdutos));
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        // sem empresa ativa: nada é lido, nada é gravado
        ativa.EmpresaId = null;
        Assert.Equal(0, await db.Produtos.CountAsync());
        Assert.Equal(0, await db.VwClientes.CountAsync());
        db.Add(new Unidade { Descricao = "SEM EMPRESA", Sigla = "X" });
        ex = await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        Assert.Contains("sem empresa ativa", ex.Message);
        db.ChangeTracker.Clear();

        // empresa B continua vendo só o que é dela
        ativa.EmpresaId = b.IdEmpresas;
        Assert.Equal(["PRODUTO TB1"], await db.Produtos.Select(p => p.Descricao).ToListAsync());

        await tx.RollbackAsync();
    }
}
