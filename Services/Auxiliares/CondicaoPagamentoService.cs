using System.Globalization;
using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services.Auxiliares;

/// <summary>
/// Parcelas da condição de pagamento (T10-11 — grade opcional que não cabe no CRUD genérico). Código e descrição
/// ficam no <see cref="AuxiliarService"/>; aqui só a grade: dias ≥ 0, % &gt; 0 e Σ % = 100 (sem parcelas = à vista).
/// </summary>
public sealed class CondicaoPagamentoService(LabDbContext db)
{
    public const int MaximoDias = 3650;
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Grade da condição da empresa ativa (null se não existir).</summary>
    public async Task<ParcelasCondicaoVM?> CarregarAsync(int id)
    {
        var vm = new ParcelasCondicaoVM { Id = id };
        if (!await PrepararAsync(vm)) return null;
        var parcelas = await db.CondicoesPagtosParc.AsNoTracking().Where(p => p.IdCondicoesPagtos == id).OrderBy(p => p.NrParcela).ToListAsync();
        vm.Linhas = parcelas.Select(p => new ParcelaLinhaVM
            { Dias = p.DiasVencimento.ToString(CultureInfo.InvariantCulture), Porcentagem = Formatar(p.PorcVencimento) }).ToList();
        return vm;
    }

    /// <summary>Código e descrição para o título (também ao reexibir com erro); false se a condição não existir.</summary>
    public async Task<bool> PrepararAsync(ParcelasCondicaoVM vm)
    {
        var c = await db.CondicoesPagtos.AsNoTracking().SingleOrDefaultAsync(c => c.IdCondicoesPagtos == vm.Id);
        if (c is null) return false;
        (vm.Codigo, vm.Descricao) = (c.Codigo, c.Descricao);
        return true;
    }

    /// <summary>Grava a grade inteira; a parcela n é a n-ésima linha preenchida (linhas em branco são ignoradas).</summary>
    public async Task<Resultado> GravarAsync(ParcelasCondicaoVM vm)
    {
        if (!await db.CondicoesPagtos.AnyAsync(c => c.IdCondicoesPagtos == vm.Id))
            return Resultado.Erro("A condição de pagamento não foi encontrada (pode ter sido excluída).");

        var linhas = vm.Linhas.Where(l => !string.IsNullOrWhiteSpace(l.Dias) || !string.IsNullOrWhiteSpace(l.Porcentagem)).ToList();
        var parcelas = new List<(int Dias, decimal Porc)>();
        for (var i = 0; i < linhas.Count; i++)
        {
            var n = i + 1;
            if (!int.TryParse(linhas[i].Dias?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var dias) || dias < 0 || dias > MaximoDias)
                return Resultado.Erro($"Parcela {n}: informe os dias para vencimento (0 a {MaximoDias}).");
            if (!LerPorcentagem(linhas[i].Porcentagem, out var porc) || porc <= 0 || porc > 100)
                return Resultado.Erro($"Parcela {n}: informe a porcentagem (maior que 0 e até 100).");
            if (decimal.Round(porc, 4) != porc)
                return Resultado.Erro($"Parcela {n}: porcentagem com no máximo 4 casas decimais.");
            parcelas.Add((dias, porc));
        }
        if (parcelas.Count > 0 && parcelas.Sum(p => p.Porc) is var soma && soma != 100)
            return Resultado.Erro($"A soma das porcentagens deve ser 100% (está em {Formatar(soma)}%).");

        // altera no lugar (nº da parcela é UNIQUE): as primeiras mudam, as que sobram saem, as novas entram
        var atuais = await db.CondicoesPagtosParc.Where(p => p.IdCondicoesPagtos == vm.Id).OrderBy(p => p.NrParcela).ToListAsync();
        for (var i = 0; i < Math.Max(atuais.Count, parcelas.Count); i++)
        {
            if (i >= parcelas.Count) { db.Remove(atuais[i]); continue; }
            var p = i < atuais.Count ? atuais[i] : db.Add(new CondicaoPagtoParc { IdCondicoesPagtos = vm.Id }).Entity;
            (p.NrParcela, p.DiasVencimento, p.PorcVencimento) = (i + 1, parcelas[i].Dias, parcelas[i].Porc);
        }
        var r = await ErrosMySql.CapturarAsync(() => db.SaveChangesAsync());
        if (r.Sucesso) return Resultado.Ok();
        db.ChangeTracker.Clear();
        return r.Falha;
    }

    // aceita "33,3333" (tela) e "33.3333"
    private static bool LerPorcentagem(string? texto, out decimal valor) =>
        decimal.TryParse(texto?.Trim().Replace(',', '.'), NumberStyles.Number & ~NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out valor);

    private static string Formatar(decimal v) => v.ToString("0.####", PtBr);
}
