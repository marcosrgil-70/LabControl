using LabControl.Data;
using LabControl.Infra;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services;

/// <summary>
/// T05 — Parâmetros da empresa ativa e numeração dos documentos (docs/telas/T05-parametros-empresa.md, P03).
/// <c>EMPRESAS_PARAMETROS</c> é auditada pelo SaveChanges; o ajuste da numeração (só ADMIN_GLOBAL) grava um evento
/// próprio com o motivo. As triggers leem os parâmetros a cada gravação: nada fica em cache aqui.
/// </summary>
public sealed class ParametrosEmpresaService(LabDbContext db, IEmpresaContexto ctx, IAuditoria auditoria)
{
    /// <summary>Tipos de <c>SEQUENCIAS_DOCUMENTOS.TIPO_DOCUMENTO</c> e o nome mostrado na tela.</summary>
    public static readonly IReadOnlyDictionary<string, string> Documentos = new Dictionary<string, string>
    {
        ["AMOSTRA"] = "Amostra", ["PROPOSTA"] = "Proposta", ["PEDIDO_VENDA"] = "Pedido de venda",
        ["PRODUTO"] = "Produto", ["RELATORIO_ENSAIO"] = "Relatório de ensaio",
    };

    /// <param name="email">Aba E-mail recusada: reexibe o digitado em vez do gravado.</param>
    public async Task<ParametrosEmpresaVM?> CarregarAsync(EmailEmpresaVM? email = null)
    {
        var p = await db.EmpresasParametros.AsNoTracking().SingleOrDefaultAsync();
        if (p is null) return null;
        var vm = new ParametrosEmpresaVM
        {
            IdLabPropostasStatusInicial = p.IdLabPropostasStatusInicial, IdLabPropostasStatusEnvcli = p.IdLabPropostasStatusEnvcli,
            IdAmostrasStatusInicial = p.IdAmostrasStatusInicial, IdAmostrasStatusResultados = p.IdAmostrasStatusResultados,
            IdBoletinsStatusInicial = p.IdBoletinsStatusInicial, IdBoletinsStatusEmitido = p.IdBoletinsStatusEmitido,
            DiasRetroativosEntrega = p.DiasRetroativosEntrega, DiasValidadeProposta = p.DiasValidadeProposta,
            DiasPesquisaPadrao = p.DiasPesquisaPadrao, ExigeResp2Boletim = p.ExigeResp2Boletim,
            TextoPadraoLegenda = p.TextoPadraoLegenda, TextoPadraoContrato = p.TextoPadraoContrato,
        };
        await PrepararAsync(vm, email ?? Email.ConfiguracaoEmailService.Tela(p));
        return vm;
    }

    /// <summary>Combos (só status da empresa ativa: ativos + o já escolhido), numeração, data da última alteração e aba E-mail.</summary>
    public async Task PrepararAsync(ParametrosEmpresaVM vm, EmailEmpresaVM? email = null)
    {
        vm.AdminGlobal = ctx.AdminGlobal;
        if (email is null)
        {
            var p = await db.EmpresasParametros.AsNoTracking().SingleAsync();
            (vm.AlteradoEm, vm.Email) = (p.AlteradoEm, Email.ConfiguracaoEmailService.Tela(p));
        }
        else (vm.AlteradoEm, vm.Email) = (await db.EmpresasParametros.Select(p => p.AlteradoEm).SingleOrDefaultAsync(), email);
        // List (não array): string[]/int[].Contains vira ReadOnlySpan no C# 14 e o EF não traduz
        static List<int> Escolhidos(params int?[] ids) => ids.OfType<int>().ToList();
        var prop = Escolhidos(vm.IdLabPropostasStatusInicial, vm.IdLabPropostasStatusEnvcli);
        var amo = Escolhidos(vm.IdAmostrasStatusInicial, vm.IdAmostrasStatusResultados);
        var bol = Escolhidos(vm.IdBoletinsStatusInicial, vm.IdBoletinsStatusEmitido);
        vm.StatusPropostas = await db.LabPropostasStatus.AsNoTracking().Where(s => s.Ativo || prop.Contains(s.IdLabPropostasStatus))
            .OrderBy(s => s.Descricao).Select(s => new OpcaoVM(s.IdLabPropostasStatus, s.Ativo ? s.Descricao : s.Descricao + " (inativo)")).ToListAsync();
        vm.StatusAmostras = await db.AmostrasStatus.AsNoTracking().Where(s => s.Ativo || amo.Contains(s.IdAmostrasStatus))
            .OrderBy(s => s.Descricao).Select(s => new OpcaoVM(s.IdAmostrasStatus, s.Ativo ? s.Descricao : s.Descricao + " (inativo)")).ToListAsync();
        vm.StatusBoletins = await db.BoletinsStatus.AsNoTracking().Where(s => s.Ativo || bol.Contains(s.IdBoletinsStatus))
            .OrderBy(s => s.Descricao).Select(s => new OpcaoVM(s.IdBoletinsStatus, s.Ativo ? s.Descricao : s.Descricao + " (inativo)")).ToListAsync();
        vm.Numeracao = (await db.SequenciasDocumentos.AsNoTracking().OrderBy(s => s.TipoDocumento).ThenByDescending(s => s.Ano).ToListAsync())
            .Select(s => new SequenciaVM(s.TipoDocumento, Documentos.GetValueOrDefault(s.TipoDocumento, s.TipoDocumento), s.Ano, s.UltimoNumero, s.AlteradoEm))
            .ToList();
    }

    /// <summary>Grava os parâmetros da empresa ativa. Status só da própria empresa (o filtro global esconde as outras).</summary>
    public async Task<Resultado> AlterarAsync(ParametrosEmpresaVM vm)
    {
        var p = await db.EmpresasParametros.SingleOrDefaultAsync();
        if (p is null) return Resultado.Erro("Parâmetros da empresa não encontrados.");

        // tabelas de status são pequenas; o filtro global deixa só as da empresa ativa
        var prop = await db.LabPropostasStatus.Select(s => s.IdLabPropostasStatus).ToListAsync();
        var amo = await db.AmostrasStatus.Select(s => s.IdAmostrasStatus).ToListAsync();
        var bol = await db.BoletinsStatus.Select(s => s.IdBoletinsStatus).ToListAsync();
        var invalido = new (int? Id, List<int> DaEmpresa, string Campo)[]
        {
            (vm.IdLabPropostasStatusInicial, prop, nameof(vm.IdLabPropostasStatusInicial)),
            (vm.IdLabPropostasStatusEnvcli, prop, nameof(vm.IdLabPropostasStatusEnvcli)),
            (vm.IdAmostrasStatusInicial, amo, nameof(vm.IdAmostrasStatusInicial)),
            (vm.IdAmostrasStatusResultados, amo, nameof(vm.IdAmostrasStatusResultados)),
            (vm.IdBoletinsStatusInicial, bol, nameof(vm.IdBoletinsStatusInicial)),
            (vm.IdBoletinsStatusEmitido, bol, nameof(vm.IdBoletinsStatusEmitido)),
        }.FirstOrDefault(s => s.Id is { } id && !s.DaEmpresa.Contains(id));
        if (invalido.Campo is not null) return Resultado.Erro("Status não encontrado.", invalido.Campo);
        if (vm.IdLabPropostasStatusInicial == vm.IdLabPropostasStatusEnvcli)
            return Resultado.Erro("O status \"Enviado ao cliente\" deve ser diferente do status inicial da proposta.", nameof(vm.IdLabPropostasStatusEnvcli));
        if (vm.IdBoletinsStatusInicial == vm.IdBoletinsStatusEmitido)
            return Resultado.Erro("O status do boletim emitido deve ser diferente do status inicial.", nameof(vm.IdBoletinsStatusEmitido));
        if (vm.DiasRetroativosEntrega is < 0 or > 365) return Resultado.Erro("Informe de 0 a 365 dias.", nameof(vm.DiasRetroativosEntrega));
        if (vm.DiasValidadeProposta < 0) return Resultado.Erro("Informe um número de dias válido.", nameof(vm.DiasValidadeProposta));
        if (vm.DiasPesquisaPadrao < 0) return Resultado.Erro("Informe um número de dias válido.", nameof(vm.DiasPesquisaPadrao));

        p.IdLabPropostasStatusInicial = vm.IdLabPropostasStatusInicial;
        p.IdLabPropostasStatusEnvcli = vm.IdLabPropostasStatusEnvcli;
        p.IdAmostrasStatusInicial = vm.IdAmostrasStatusInicial;
        p.IdAmostrasStatusResultados = vm.IdAmostrasStatusResultados;
        p.IdBoletinsStatusInicial = vm.IdBoletinsStatusInicial;
        p.IdBoletinsStatusEmitido = vm.IdBoletinsStatusEmitido;
        p.DiasRetroativosEntrega = vm.DiasRetroativosEntrega;
        p.DiasValidadeProposta = vm.DiasValidadeProposta;
        p.DiasPesquisaPadrao = vm.DiasPesquisaPadrao;
        p.ExigeResp2Boletim = vm.ExigeResp2Boletim;
        p.TextoPadraoLegenda = Textos.Limpo(vm.TextoPadraoLegenda);
        p.TextoPadraoContrato = Textos.Limpo(vm.TextoPadraoContrato);
        var gravou = await ErrosMySql.CapturarAsync(() => db.SaveChangesAsync());
        return gravou.Sucesso ? Resultado.Ok() : gravou.Falha;
    }

    /// <summary>
    /// Ajuste manual de uma linha da numeração (só ADMIN_GLOBAL). Trava a linha (a mesma trava do SP_PROXIMO_NUMERO),
    /// recusa valor menor que o maior número já usado e grava em AUDITORIA o antes/depois com o motivo.
    /// </summary>
    public async Task<Resultado> AjustarNumeracaoAsync(AjusteNumeracaoVM vm)
    {
        if (!ctx.AdminGlobal) return Resultado.Erro("Somente o administrador global ajusta a numeração.");
        if (ctx.EmpresaId is not { } empresa) return Resultado.Erro("Sem empresa ativa.");
        if (!Documentos.ContainsKey(vm.Tipo)) return Resultado.Erro("Tipo de documento inválido.");
        if (vm.NovoValor is not { } novo || novo < 0) return Resultado.Erro("Informe o novo valor.", nameof(vm.NovoValor));
        var motivo = vm.Motivo?.Trim() ?? "";
        if (motivo.Length < 10) return Resultado.Erro("Descreva o motivo (de 10 a 500 caracteres).", nameof(vm.Motivo));

        await using var tx = await db.Database.BeginTransactionAsync();
        var atual = await db.Database.SqlQuery<int?>($"""
            SELECT ULTIMO_NUMERO AS Value FROM SEQUENCIAS_DOCUMENTOS
            WHERE ID_EMPRESAS = {empresa} AND TIPO_DOCUMENTO = {vm.Tipo} AND ANO = {vm.Ano} FOR UPDATE
            """).SingleOrDefaultAsync();
        if (atual is not { } antes) return Resultado.Erro("Numeração não encontrada.");
        if (novo == antes) return Resultado.Erro("O valor informado é o mesmo do último número.", nameof(vm.NovoValor));

        var maior = await MaiorUsadoAsync(empresa, vm.Tipo, vm.Ano);
        if (novo < maior)
            return Resultado.Erro($"O último número não pode ser menor que o maior número já usado ({maior}).", nameof(vm.NovoValor));

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE SEQUENCIAS_DOCUMENTOS SET ULTIMO_NUMERO = {novo}
            WHERE ID_EMPRESAS = {empresa} AND TIPO_DOCUMENTO = {vm.Tipo} AND ANO = {vm.Ano}
            """);
        await auditoria.RegistrarAsync(AcoesAuditoria.Alteracao, "SEQUENCIAS_DOCUMENTOS", $"{empresa}-{vm.Tipo}-{vm.Ano}",
            new { ULTIMO_NUMERO = antes }, new { ULTIMO_NUMERO = novo, MAIOR_USADO = maior, MOTIVO = motivo });
        await tx.CommitAsync();
        return Resultado.Ok();
    }

    /// <summary>Maior número já gravado do documento na empresa ativa (0 se nenhum).</summary>
    private async Task<int> MaiorUsadoAsync(int empresa, string tipo, short ano) => tipo switch
    {
        "AMOSTRA" => await db.LabHistAmostras.Where(a => a.AnoAmostra == ano).MaxAsync(a => (int?)a.CodAmostra) ?? 0,
        "PROPOSTA" => await db.LabPropostas.Where(p => p.AnoProposta == ano).MaxAsync(p => (int?)p.CodProposta) ?? 0,
        "PEDIDO_VENDA" => await db.PedidosVenda.MaxAsync(p => (int?)p.NrPedido) ?? 0,
        "RELATORIO_ENSAIO" => await db.LabRelatorioEnsaio.Where(r => r.AnoRelatorio == ano).MaxAsync(r => (int?)r.NrRelatorio) ?? 0,
        // código de produto é texto: só os numéricos contam (os mesmos que a trigger gera)
        "PRODUTO" => (int)await db.Database.SqlQuery<long>(
            $"SELECT COALESCE(MAX(CAST(CODIGO AS UNSIGNED)), 0) AS Value FROM PRODUTOS WHERE ID_EMPRESAS = {empresa} AND CODIGO REGEXP '^[0-9]+$'")
            .SingleAsync(),
        _ => throw new ArgumentOutOfRangeException(nameof(tipo)),
    };

}
