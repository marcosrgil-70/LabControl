using LabControl.Infra;
using LabControl.Models;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Tests;

/// <summary>Achados do /code-review da Fase 1 (tarefa 1.7) que viraram correção.</summary>
public class RevisaoFase1Tests
{
    [Fact]
    public async Task Movimentacao_em_amostra_de_outra_empresa_e_recusada()
    {
        await using var a = await Cenario.CriarAsync();
        await using var b = await Cenario.CriarAsync();
        var amostraB = b.NovaAmostra(await b.HojeAsync());
        b.Db.Add(amostraB);
        await b.Db.SaveChangesAsync();
        var movimentosB = await b.Db.LabMovAmostras.CountAsync(m => m.IdLabHistAmostras == amostraB.IdLabHistAmostras);

        // antes a TG_MOV_BI trocava a empresa (A) pela da amostra (B) em silêncio e mexia no saldo de B
        a.Db.Add(new LabMovAmostra { IdLabHistAmostras = amostraB.IdLabHistAmostras, Qtde = 1, ES = EntradaSaida.Entrada, IdEntidadesFunc = a.Ativa.ProfissionalId!.Value });
        var r = await ErrosMySql.CapturarAsync(() => a.Db.SaveChangesAsync());
        Assert.Equal("Registro pertence a outra empresa.", r.Mensagem);
        Assert.Equal(movimentosB, await b.Db.LabMovAmostras.CountAsync(m => m.IdLabHistAmostras == amostraB.IdLabHistAmostras));
    }

    [Theory]
    [InlineData("192.168.0.10", "192.168.0.10")]
    [InlineData("fe80::abcd:ef01:2345:6789%12", "fe80::abcd:ef01:2345:6789")]
    [InlineData(null, null)]
    public void Ip_da_auditoria_sem_escopo_e_com_no_maximo_45_caracteres(string? ip, string? esperado) =>
        Assert.Equal(esperado, EmpresaContexto.IpParaAuditoria(ip));

    [Fact]
    public void Ip_longo_e_cortado_em_45() =>
        Assert.Equal(45, EmpresaContexto.IpParaAuditoria(new string('f', 60))!.Length);
}
