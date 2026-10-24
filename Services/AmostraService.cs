using System.Globalization;
using System.Text.Json;
using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services;

/// <summary>Resultado da gravação: o ID e um aviso (ex.: amostras além do contratado no pedido), sem bloquear.</summary>
public sealed record AmostraGravada(int Id, string? Aviso);

/// <summary>
/// T22 — Recebimento de amostras (tarefa 4.3; P04/P09/P11). O banco numera (ano do <b>servidor</b> +
/// <c>SP_PROXIMO_NUMERO</c>), confere proposta/item/cliente, faz a entrada automática no estoque e o log; o serviço
/// valida na ordem do Delphi, controla a data retroativa (confirmação + permissão → <c>@LC_PERMITE_RETROATIVO</c>),
/// gera os testes pela proposta (<c>SP_AMOSTRA_GERAR_TESTES</c>, nunca com resultado) e a revisão
/// (<c>SP_GERAR_REVISAO_AMOSTRA</c>, fora da transação do EF).
/// </summary>
public sealed class AmostraService(LabDbContext db, IEmpresaContexto ctx, Procedures procedures)
{
    public const string MensagemNaoEncontrada = "A amostra não foi encontrada (pode ter sido excluída).";
    public const string MensagemComResultados =
        "Por existir Resultados cadastrados para este Histórico de Amostra, os parâmetros NÃO serão alterados conforme a Proposta. " +
        "Para alterá-los conforme a Proposta, é necessário excluir os Resultados da Amostra.";
    public const string PermissaoRetroativa = "AMOSTRAS.ENTREGA_RETROATIVA";
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Data do servidor do banco: é dela o ano do número e o limite da data de entrega.</summary>
    private Task<DateOnly> HojeAsync() => db.Database.SqlQueryRaw<DateOnly>("SELECT CURDATE() AS Value").SingleAsync();

    // ------------------------------------------------------------------ consulta

    public async Task<AmostraListaVM> ListarAsync(AmostraFiltroVM f)
    {
        var q = db.VwLabAmostras.AsNoTracking();
        if (f.SoUltima) q = q.Where(a => a.UltimaRevisao);
        if (Textos.Limpo(f.Cliente) is { } c)
            q = q.Where(a => a.ClienteNome.Contains(c) || (a.ClienteFantasia != null && a.ClienteFantasia.Contains(c))
                             || (a.ClienteCpfCnpj != null && a.ClienteCpfCnpj.Contains(c)));
        if (f.APartirDe is { } de) q = q.Where(a => a.DtEntrega >= de);
        if (Textos.Limpo(f.Numero) is { } n) q = q.Where(a => a.NrAmostra.Contains(n));
        if (Textos.Limpo(f.Proposta) is { } pr) q = q.Where(a => a.NrProposta.Contains(pr));
        if (Textos.Limpo(f.Produto) is { } pd) q = q.Where(a => a.ProdutoCodigo.Contains(pd) || a.ProdutoDescricao.Contains(pd));
        if (f.IdStatus is { } st) q = q.Where(a => a.IdAmostrasStatus == st);
        if (Textos.Limpo(f.Lote) is { } lote) q = q.Where(a => a.NrLote != null && a.NrLote.Contains(lote));
        if (f.Ate is { } ate) q = q.Where(a => a.DtEntrega <= ate);
        if (f.ApenasPermiteResultado) q = q.Where(a => a.PermiteResultado);
        if (f.ApenasComResultado) q = q.Where(a => a.TemResultado != 0);
        if (f.ApenasSemLocalizacao) q = q.Where(a => a.SituacaoArquivo == null);

        var total = await q.CountAsync();
        f.Pagina = Paginacao.Ajustar(f.Pagina, total, AmostraFiltroVM.TamanhoPagina);
        var ordenada = f.OrdenarPorNumero
            ? q.OrderByDescending(a => a.AnoAmostra).ThenByDescending(a => a.CodAmostra).ThenByDescending(a => a.Revisao)
            : q.OrderByDescending(a => a.DtEntrega).ThenByDescending(a => a.HrEntrega).ThenByDescending(a => a.IdLabHistAmostras);
        var itens = await ordenada
            .Skip((f.Pagina - 1) * AmostraFiltroVM.TamanhoPagina).Take(AmostraFiltroVM.TamanhoPagina)
            .Select(a => new AmostraListaItemVM(a.IdLabHistAmostras, a.NrAmostra, a.UltimaRevisao, a.DtEntrega, a.HrEntrega,
                a.QtdeEmbalagensEntregue, a.Embalagem, a.NrLote, a.Fabricacao, a.Validade, a.TemperaturaVerificacao, a.AspectoAmostra, a.Cor,
                a.StatusAmostra, a.CorStatus, a.ClienteNome, a.ProdutoDescricao, a.TipoAmostra, a.TipoAnalise, a.NrProposta, a.NrPedidoFmt))
            .ToListAsync();
        var status = await db.AmostrasStatus.AsNoTracking().OrderBy(s => s.Descricao).Select(s => new OpcaoVM(s.IdAmostrasStatus, s.Descricao)).ToListAsync();
        return new AmostraListaVM { Filtro = f, Itens = itens, Status = status, Total = total };
    }

    /// <summary>"Exibir a partir de" padrão: hoje − DIAS_PESQUISA_PADRAO.</summary>
    public async Task<DateOnly> APartirDePadraoAsync()
    {
        var dias = await db.EmpresasParametros.AsNoTracking().Select(p => (int?)p.DiasPesquisaPadrao).SingleOrDefaultAsync() ?? 365;
        return (await HojeAsync()).AddDays(-dias);
    }

    // ------------------------------------------------------------------ formulário

    /// <summary>Nova amostra: entrega agora (servidor), status inicial, local = último usado, responsável = o próprio profissional.</summary>
    public async Task<AmostraVM> NovaAsync()
    {
        var par = await db.EmpresasParametros.AsNoTracking().SingleOrDefaultAsync();
        var prof = ctx.ProfissionalId;
        var local = await db.LabHistAmostras.AsNoTracking().Where(a => prof != null && a.IdEntidadesFuncDig == prof)
                        .OrderByDescending(a => a.IdLabHistAmostras).Select(a => a.LocalRecebimento).FirstOrDefaultAsync()
                    ?? await db.LabHistAmostras.AsNoTracking().OrderByDescending(a => a.IdLabHistAmostras).Select(a => a.LocalRecebimento).FirstOrDefaultAsync();
        var vm = new AmostraVM
        {
            DtEntrega = await HojeAsync(), HrEntrega = TimeOnly.FromDateTime(DateTime.Now).Truncar(),
            IdAmostrasStatus = par?.IdAmostrasStatusInicial, LocalRecebimento = local, IdEntidadesFuncResp = prof, QtdeEmbalagens = "1",
        };
        await PrepararAsync(vm);
        return vm;
    }

    public async Task<AmostraVM?> CarregarAsync(int id)
    {
        var a = await db.LabHistAmostras.AsNoTracking().SingleOrDefaultAsync(x => x.IdLabHistAmostras == id);
        if (a is null) return null;
        var v = await db.VwLabAmostras.AsNoTracking().Where(x => x.IdLabHistAmostras == id)
            .Select(x => new { x.NrPedidoFmt, x.TemResultado, x.TemBoletimEmitido }).SingleAsync();
        var vm = new AmostraVM
        {
            Id = a.IdLabHistAmostras, NrAmostra = a.NrAmostra, Revisao = a.Revisao, MotivoRevisao = a.MotivoRevisao, NrPedido = v.NrPedidoFmt,
            UltimaRevisao = a.UltimaRevisao, TemResultado = v.TemResultado != 0,
            Bloqueio = !a.UltimaRevisao ? "Revisão antiga da amostra é somente leitura."
                : v.TemBoletimEmitido != 0 ? "Amostra com boletim emitido: para alterar, gere uma revisão da amostra." : null,
            IdEntidades = a.IdEntidades, IdLabPropostas = a.IdLabPropostas, IdLabPropostasProdutos = a.IdLabPropostasProdutos,
            IdAmostrasTipo = a.IdAmostrasTipo, IdAnalisesTipo = a.IdAnalisesTipo, DtEntrega = a.DtEntrega, HrEntrega = a.HrEntrega,
            LocalRecebimento = a.LocalRecebimento, IdEmbalagensTipos = a.IdEmbalagensTipos, QtdeEmbalagens = a.QtdeEmbalagensEntregue.ToString("0.####", PtBr),
            NrLote = a.NrLote, FabricacaoDia = a.FabricacaoDia, FabricacaoMes = a.FabricacaoMes, FabricacaoAno = a.FabricacaoAno,
            ValidadeDia = a.ValidadeDia, ValidadeMes = a.ValidadeMes, ValidadeAno = a.ValidadeAno,
            NotaRotulo = a.NotaRotulo, AspectoAmostra = a.AspectoAmostra, Cor = a.Cor, OutrasCaracteristicas = a.OutrasCaracteristicas,
            ObservacaoVerificacao = a.ObservacaoVerificacao, Temperatura = a.TemperaturaVerificacao?.ToString("0.##", PtBr),
            AcompanhaFichaTecnica = a.AcompanhaFichaTecnica, AcompanhaPadraoAnalitico = a.AcompanhaPadraoAnalitico,
            AcompanhaCaCliente = a.AcompanhaCaCliente, EnviarOutroLaboratorio = a.EnviarOutroLaboratorio,
            IdEntidadesFuncResp = a.IdEntidadesFuncResp, IdAmostrasStatus = a.IdAmostrasStatus, NomeColetor = a.NomeColetor, DtHrColeta = a.DtHrColeta,
            TxObservacao = HtmlSimples.Limpar(a.TxObservacao),
            Testes = await TestesAsync(id), Historico = await HistoricoAsync(id),
        };
        await PrepararAsync(vm);
        return vm;
    }

    public async Task PrepararAsync(AmostraVM vm)
    {
        var o = vm.Opcoes;
        int? cliente = vm.IdEntidades, tipoAmostra = vm.IdAmostrasTipo, tipoAnalise = vm.IdAnalisesTipo, emb = vm.IdEmbalagensTipos,
            resp = vm.IdEntidadesFuncResp, status = vm.IdAmostrasStatus;
        o.Clientes = (await db.Entidades.AsNoTracking().Where(e => (e.TipoCliente && !e.Inativo) || e.IdEntidades == cliente)
            .OrderBy(e => e.Nome).Select(e => new { e.IdEntidades, e.Nome, e.CpfCnpj }).ToListAsync())
            .Select(e => new OpcaoVM(e.IdEntidades, e.CpfCnpj is null ? e.Nome : $"{e.Nome} — {e.CpfCnpj}")).ToList();
        o.TiposAmostra = await db.AmostrasTipo.AsNoTracking().Where(t => t.Ativo || t.IdAmostrasTipo == tipoAmostra).OrderBy(t => t.Codigo)
            .Select(t => new OpcaoCodigoVM(t.IdAmostrasTipo, t.Descricao, t.Codigo)).ToListAsync();
        o.TiposAnalise = await db.AnalisesTipo.AsNoTracking().Where(t => t.Ativo || t.IdAnalisesTipo == tipoAnalise).OrderBy(t => t.Codigo)
            .Select(t => new OpcaoCodigoVM(t.IdAnalisesTipo, t.Descricao, t.Codigo)).ToListAsync();
        o.Embalagens = await db.EmbalagensTipos.AsNoTracking().Where(x => x.Ativo || x.IdEmbalagensTipos == emb).OrderBy(x => x.Descricao)
            .Select(x => new OpcaoVM(x.IdEmbalagensTipos, x.Descricao)).ToListAsync();
        o.Responsaveis = await db.VwProfissionais.AsNoTracking().Where(x => !x.Inativo || x.IdEntidades == resp).OrderBy(x => x.Nome)
            .Select(x => new OpcaoVM(x.IdEntidades, x.NomeCompleto ?? x.Nome)).ToListAsync();
        o.Status = await db.AmostrasStatus.AsNoTracking().Where(x => x.Ativo || x.IdAmostrasStatus == status).OrderBy(x => x.Descricao)
            .Select(x => new OpcaoVM(x.IdAmostrasStatus, x.Descricao)).ToListAsync();
        o.Cliente = cliente is { } c ? await ClienteAsync(c, vm.IdLabPropostas) : null;
        o.Proposta = vm.IdLabPropostas is { } p ? await PropostaAsync(p) : null;
        o.DiasRetroativos = await db.EmpresasParametros.AsNoTracking().Select(x => (int?)x.DiasRetroativosEntrega).SingleOrDefaultAsync() ?? 7;
        o.Hoje = (await HojeAsync()).ToString("yyyy-MM-dd");
        o.PodeRetroativa = ctx.TemPermissao(PermissaoRetroativa);
    }

    /// <summary>Cliente: documento, endereço de correspondência, fone e as propostas que podem receber amostra (última revisão, status que permite).</summary>
    public async Task<AmostraClienteVM?> ClienteAsync(int id, int? manterProposta = null)
    {
        var e = await db.Entidades.AsNoTracking().Where(x => x.IdEntidades == id).Select(x => new { x.IdEntidades, x.CpfCnpj }).SingleOrDefaultAsync();
        if (e is null) return null;
        var end = await db.VwEntidadesEnderecos.AsNoTracking().Where(x => x.IdEntidades == id && !x.Inativo)
            .OrderBy(x => x.Finalidade == FinalidadeEndereco.Correspondencia ? 0 : 1).Select(x => x.EnderecoLinha1 + " — " + x.EnderecoLinha2).FirstOrDefaultAsync();
        var fone = await db.EntidadesFones.AsNoTracking().Where(x => x.IdEntidades == id && !x.Inativo)
            .OrderByDescending(x => x.Principal).Select(x => "(" + x.Ddd + ") " + x.Fone).FirstOrDefaultAsync();
        var propostas = await db.LabPropostas.AsNoTracking()
            .Where(p => p.IdEntidades == id && ((p.UltimaRevisao && p.LabPropostaStatus.PermiteAmostra) || p.IdLabPropostas == manterProposta))
            .OrderByDescending(p => p.AnoProposta).ThenByDescending(p => p.CodProposta)
            .Select(p => new { p.IdLabPropostas, p.NrProposta, p.DtSolicitacao }).ToListAsync();
        return new AmostraClienteVM(e.IdEntidades, e.CpfCnpj, end, fone,
            propostas.Select(p => new OpcaoVM(p.IdLabPropostas, $"{p.NrProposta} — {p.DtSolicitacao:dd/MM/yyyy}")).ToList());
    }

    /// <summary>Proposta: pedido de venda e itens com os tipos de análise das suas análises (o tipo da amostra fica restrito a eles).</summary>
    public async Task<AmostraPropostaVM?> PropostaAsync(int id)
    {
        var p = await db.VwLabPropostas.AsNoTracking().Where(x => x.IdLabPropostas == id).Select(x => new { x.IdLabPropostas, x.NrProposta, x.NrPedidoFmt }).SingleOrDefaultAsync();
        if (p is null) return null;
        var itens = await db.LabPropostasProdutos.AsNoTracking().Where(x => x.IdLabPropostas == id).OrderBy(x => x.Item)
            .Select(x => new { x.IdLabPropostasProdutos, x.Item, x.Produto.Codigo, x.Produto.Descricao }).ToListAsync();
        var analises = await db.LabPropostasProdanalises.AsNoTracking().Where(x => x.IdLabPropostas == id)
            .Select(x => new { x.IdLabPropostasProdutos, x.LabParametroAnalise.IdAnalisesTipo, x.TipoDocumento }).ToListAsync();
        return new AmostraPropostaVM(p.IdLabPropostas, p.NrProposta, p.NrPedidoFmt, itens.Select(i =>
        {
            var doItem = analises.Where(a => a.IdLabPropostasProdutos == i.IdLabPropostasProdutos).ToList();
            var doc = doItem.Any(a => a.TipoDocumento == TipoDocumento.Relatorio) && doItem.All(a => a.TipoDocumento == TipoDocumento.Relatorio) ? "RELATORIO" : "BOLETIM";
            return new AmostraItemVM(i.IdLabPropostasProdutos, i.Item, $"{i.Descricao} ({i.Codigo})", doc,
                doItem.Select(a => a.IdAnalisesTipo).Distinct().ToList());
        }).ToList());
    }

    private async Task<IReadOnlyList<AmostraTesteVM>> TestesAsync(int id) =>
        await db.LabHistAmostrasTestes.AsNoTracking().Where(t => t.IdLabHistAmostras == id).OrderBy(t => t.IdLabHistAmostrasTestes)
            .Select(t => new AmostraTesteVM(t.AnaliseTipo.Descricao, t.LabParametroAnalise.Descricao, t.AnaliseMetodo.Descricao,
                t.Idioma.Descricao, t.Prazo.Descricao, t.DtPrevista)).ToListAsync();

    /// <summary>Log da amostra; as linhas migradas do legado (sem data nem usuário) vêm marcadas.</summary>
    private async Task<IReadOnlyList<AmostraLogVM>> HistoricoAsync(int id)
    {
        var linhas = await db.LabHistAmostrasLog.AsNoTracking().Where(l => l.IdLabHistAmostras == id).OrderBy(l => l.DtLog).ThenBy(l => l.IdLabHistAmostrasLog)
            .Select(l => new { l.Acao, l.DtLog, l.Dados, l.IdUsuarios }).ToListAsync();
        var ids = linhas.Where(l => l.IdUsuarios != null).Select(l => l.IdUsuarios!.Value).Distinct().ToList();
        var usuarios = await db.Usuarios.AsNoTracking().Where(u => ids.Contains(u.IdUsuarios)).ToDictionaryAsync(u => u.IdUsuarios, u => u.Login);
        var status = await db.AmostrasStatus.AsNoTracking().ToDictionaryAsync(s => s.IdAmostrasStatus, s => s.Descricao);
        return linhas.Select(l =>
        {
            using var json = JsonDocument.Parse(l.Dados);
            var raiz = json.RootElement;
            var legado = raiz.TryGetProperty("ORIGEM", out var origem) && origem.GetString() == "LEGADO";
            var campos = legado && raiz.TryGetProperty("CAMPOS", out var c) ? c : raiz;
            string? Valor(string nome) => campos.TryGetProperty(nome, out var v) && v.ValueKind is not JsonValueKind.Null ? v.ToString() : null;
            var partes = new List<string>();
            if (legado) partes.Add("Registro do legado, sem data");
            else if (l.IdUsuarios is { } u) partes.Add($"Usuário {usuarios.GetValueOrDefault(u, u.ToString())}");
            if (Valor("ID_AMOSTRAS_STATUS") is { } st && int.TryParse(st, out var ids) && status.TryGetValue(ids, out var nomeStatus)) partes.Add($"status {nomeStatus}");
            if (Valor("QTDE_EMBALAGENS_ENTREGUE") is { } q) partes.Add($"qtde {q}");
            if (Valor("NR_LOTE") is { } lote) partes.Add($"lote {lote}");
            if (Valor("MOTIVO_REVISAO") is { } motivo) partes.Add($"motivo: {motivo}");
            return new AmostraLogVM(l.Acao switch { "I" => "Inclusão", "E" => "Exclusão", _ => "Alteração" }, legado ? null : l.DtLog, legado, string.Join(" · ", partes));
        }).ToList();
    }

    // ------------------------------------------------------------------ gravação

    /// <summary>
    /// Inclui (Id = 0) ou altera. Data mais antiga que DIAS_RETROATIVOS_ENTREGA: sem <see cref="AmostraVM.ConfirmouRetroativa"/>
    /// volta um erro no campo <c>ConfirmouRetroativa</c> (a tela pergunta); confirmada, exige a permissão (P04).
    /// </summary>
    public async Task<Resultado<AmostraGravada>> GravarAsync(AmostraVM vm)
    {
        var atual = vm.Id == 0 ? null : await db.LabHistAmostras.SingleOrDefaultAsync(a => a.IdLabHistAmostras == vm.Id);
        if (vm.Id != 0 && atual is null) return Resultado.Erro(MensagemNaoEncontrada);
        var flags = atual is null ? null : await db.VwLabAmostras.AsNoTracking().Where(x => x.IdLabHistAmostras == vm.Id)
            .Select(x => new { x.TemResultado, x.TemBoletimEmitido }).SingleAsync();
        if (atual is { UltimaRevisao: false }) return Resultado.Erro("Revisão antiga da amostra é somente leitura.");
        if (flags?.TemBoletimEmitido is > 0) return Resultado.Erro("Amostra com boletim emitido: para alterar, gere uma revisão da amostra.");
        var temResultado = flags?.TemResultado is > 0;

        // obrigatórios na ordem do Delphi (P09 › 7)
        if (vm.IdEntidades is not { } cliente) return Resultado.Erro("Cliente é obrigatório.", nameof(vm.IdEntidades));
        if (vm.IdLabPropostas is not { } proposta) return Resultado.Erro("Proposta é obrigatória.", nameof(vm.IdLabPropostas));
        if (vm.DtEntrega is not { } entrega) return Resultado.Erro("Data da entrega é obrigatória.", nameof(vm.DtEntrega));
        if (vm.HrEntrega is not { } hora) return Resultado.Erro("Hora da entrega é obrigatória.", nameof(vm.HrEntrega));
        if (vm.IdEmbalagensTipos is not { } embalagem) return Resultado.Erro("Tipo de embalagem é obrigatório.", nameof(vm.IdEmbalagensTipos));
        if (vm.IdAmostrasTipo is not { } tipoAmostra) return Resultado.Erro("Tipo da amostra é obrigatório.", nameof(vm.IdAmostrasTipo));
        if (vm.IdLabPropostasProdutos is not { } item) return Resultado.Erro("Produto é obrigatório.", nameof(vm.IdLabPropostasProdutos));
        if (vm.IdAnalisesTipo is not { } tipoAnalise) return Resultado.Erro("Tipo de Análise obrigatório.", nameof(vm.IdAnalisesTipo));
        if (vm.IdEntidadesFuncResp is not { } responsavel) return Resultado.Erro("Responsável é obrigatório.", nameof(vm.IdEntidadesFuncResp));
        if (vm.IdAmostrasStatus is not { } status) return Resultado.Erro("Status é obrigatório.", nameof(vm.IdAmostrasStatus));
        if (Textos.Limpo(vm.LocalRecebimento) is not { } local) return Resultado.Erro("Local de recebimento é obrigatório.", nameof(vm.LocalRecebimento));
        if (!Textos.Decimal(Textos.Limpo(vm.QtdeEmbalagens) ?? "", out var qtde) || qtde <= 0 || decimal.Round(qtde, 4) != qtde)
            return Resultado.Erro("Quantidade de embalagens: número maior que zero.", nameof(vm.QtdeEmbalagens));
        if (!LerTemperatura(vm.Temperatura, out var temperatura)) return Resultado.Erro("Temperatura: número em °C, até 2 casas decimais.", nameof(vm.Temperatura));

        // data de entrega (P04): futura recusada; antiga demais só confirmada e com permissão
        var hoje = await HojeAsync();
        if (entrega > hoje) return Resultado.Erro("Data de entrega maior que a data de hoje.", nameof(vm.DtEntrega));
        var dias = hoje.DayNumber - entrega.DayNumber;
        var limite = await db.EmpresasParametros.AsNoTracking().Select(x => (int?)x.DiasRetroativosEntrega).SingleOrDefaultAsync() ?? 7;
        var retroativa = dias > limite && (atual is null || atual.DtEntrega != entrega);
        if (retroativa && !vm.ConfirmouRetroativa)
            return Resultado.Erro($"A data de entrega {entrega:dd/MM/yyyy} é de {dias} dias atrás. Confirme que o ANO está correto.", nameof(vm.ConfirmouRetroativa));
        if (retroativa && !ctx.TemPermissao(PermissaoRetroativa))
            return Resultado.Erro($"Data de entrega com mais de {limite} dias exige a permissão “Amostras — Data de entrega além do limite”.", nameof(vm.DtEntrega));
        if (vm.DtHrColeta is { } coleta && coleta > entrega.ToDateTime(hora))
            return Resultado.Erro("A data/hora da coleta não pode ser depois da entrega.", nameof(vm.DtHrColeta));

        // tipo de análise: só os das análises do item (T22)
        var tiposDoItem = await db.LabPropostasProdanalises.Where(x => x.IdLabPropostasProdutos == item)
            .Select(x => x.LabParametroAnalise.IdAnalisesTipo).Distinct().ToListAsync();
        if (!tiposDoItem.Contains(tipoAnalise))
            return Resultado.Erro("O tipo de análise precisa ser um dos tipos das análises do produto na proposta.", nameof(vm.IdAnalisesTipo));
        if (temResultado && (atual!.IdLabPropostas != proposta || atual.IdLabPropostasProdutos != item || atual.IdAmostrasTipo != tipoAmostra || atual.IdAnalisesTipo != tipoAnalise))
            return Resultado.Erro("Existem resultados: tipo, proposta e produto da amostra não podem ser alterados (gere revisão).");
        var digitador = atual?.IdEntidadesFuncDig ?? ctx.ProfissionalId;
        if (digitador is null)
            return Resultado.Erro("Seu usuário não está vinculado a um profissional (Usuários › profissional): o recebimento registra quem digitou.");
        var documento = await db.LabPropostasProdanalises.Where(x => x.IdLabPropostasProdutos == item).AllAsync(x => x.TipoDocumento == TipoDocumento.Relatorio)
                        && await db.LabPropostasProdanalises.AnyAsync(x => x.IdLabPropostasProdutos == item) ? TipoDocumento.Relatorio : TipoDocumento.Boletim;

        var r = await ErrosMySql.CapturarAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            if (retroativa) await procedures.DefinirPermiteRetroativoAsync(true);
            var a = atual ?? db.LabHistAmostras.Add(new LabHistAmostra { IdEntidadesFuncDig = digitador.Value }).Entity;
            (a.IdEntidades, a.IdLabPropostas, a.IdLabPropostasProdutos, a.IdAmostrasTipo, a.IdAnalisesTipo, a.TipoDocumento) =
                (cliente, proposta, item, tipoAmostra, tipoAnalise, documento);
            (a.DtEntrega, a.HrEntrega, a.LocalRecebimento, a.IdEmbalagensTipos, a.QtdeEmbalagensEntregue) = (entrega, hora, Corte(local, 50)!, embalagem, qtde);
            a.NrLote = Corte(vm.NrLote, 30);
            (a.FabricacaoDia, a.FabricacaoMes, a.FabricacaoAno) = (Parte(vm.FabricacaoDia), Parte(vm.FabricacaoMes), (short?)vm.FabricacaoAno);
            (a.ValidadeDia, a.ValidadeMes, a.ValidadeAno) = (Parte(vm.ValidadeDia), Parte(vm.ValidadeMes), (short?)vm.ValidadeAno);
            a.NotaRotulo = Corte(vm.NotaRotulo, 255);
            (a.AspectoAmostra, a.Cor, a.OutrasCaracteristicas, a.ObservacaoVerificacao) =
                (Corte(vm.AspectoAmostra, 50), Corte(vm.Cor, 20), Corte(vm.OutrasCaracteristicas, 100), Corte(vm.ObservacaoVerificacao, 255));
            a.TemperaturaVerificacao = temperatura;
            (a.AcompanhaFichaTecnica, a.AcompanhaPadraoAnalitico, a.AcompanhaCaCliente, a.EnviarOutroLaboratorio) =
                (vm.AcompanhaFichaTecnica, vm.AcompanhaPadraoAnalitico, vm.AcompanhaCaCliente, vm.EnviarOutroLaboratorio);
            (a.IdEntidadesFuncResp, a.IdAmostrasStatus, a.NomeColetor, a.DtHrColeta) = (responsavel, status, Corte(vm.NomeColetor, 60), vm.DtHrColeta);
            a.TxObservacao = HtmlSimples.Limpar(vm.TxObservacao);
            await db.SaveChangesAsync(); // inclusão: o banco numera (ano do servidor), entra no estoque e loga

            // testes = análises do item da proposta; com resultado nunca são regerados (P09)
            if (!temResultado)
            {
                var t = await procedures.AmostraGerarTestesAsync(a.IdLabHistAmostras);
                if (!t.Sucesso) throw new InvalidOperationException(t.Mensagem);
            }
            await db.Entry(a).ReloadAsync();
            if (a.IdPedidosVenda is { } pedido)
                await db.PedidosVenda.Where(p => p.IdPedidosVenda == pedido && p.Situacao == SituacaoPedido.Aberto)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Situacao, SituacaoPedido.EmExecucao));
            if (retroativa) await procedures.DefinirPermiteRetroativoAsync(false);
            await tx.CommitAsync();
            return new AmostraGravada(a.IdLabHistAmostras, await AvisoContratadoAsync(a));
        });
        if (!r.Sucesso) db.ChangeTracker.Clear();
        return r;
    }

    // recebidas além do contratado no pedido: só aviso (P09 › 12)
    private async Task<string?> AvisoContratadoAsync(LabHistAmostra a)
    {
        if (a.IdPedidosVenda is not { } pedido) return null;
        var contratada = await db.PedidosVendaItens.Where(i => i.IdPedidosVenda == pedido && i.IdLabPropostasProdutos == a.IdLabPropostasProdutos)
            .Select(i => (int?)i.QtdeContratada).SingleOrDefaultAsync();
        if (contratada is null) return null;
        var recebidas = await db.LabHistAmostras.CountAsync(x => x.IdPedidosVenda == pedido && x.IdLabPropostasProdutos == a.IdLabPropostasProdutos && x.Revisao == 0);
        return recebidas > contratada ? $"Atenção: {recebidas} amostras recebidas para este produto e o pedido contratou {contratada}." : null;
    }

    // ------------------------------------------------------------------ exclusão e revisão

    /// <summary>Só a revisão 0 sem resultados e sem movimentação manual — o banco recusa o resto (TG_AMOSTRAS_BD).</summary>
    public async Task<Resultado> ExcluirAsync(int id)
    {
        var r = await ErrosMySql.CapturarAsync(() => db.LabHistAmostras.Where(a => a.IdLabHistAmostras == id).ExecuteDeleteAsync());
        if (!r.Sucesso) return r.Falha;
        return r.Valor == 0 ? Resultado.Erro(MensagemNaoEncontrada) : Resultado.Ok();
    }

    /// <summary>Revisão (P11): só da última revisão e só com resultados; motivo obrigatório. Devolve o ID da revisão nova.</summary>
    public async Task<Resultado<int>> GerarRevisaoAsync(int id, string? motivo)
    {
        var a = await db.LabHistAmostras.AsNoTracking().SingleOrDefaultAsync(x => x.IdLabHistAmostras == id);
        if (a is null) return Resultado.Erro(MensagemNaoEncontrada);
        if (!a.UltimaRevisao) return Resultado.Erro("Só é possível revisar a última revisão da amostra.");
        if (!await db.LabResultadosAnalises.AnyAsync(r => r.IdLabHistAmostras == id))
            return Resultado.Erro("Não existe Resultados para esta Amostra. A Amostra pode ser alterada.");
        if (Textos.Limpo(motivo) is not { } m) return Resultado.Erro("Informe o motivo da revisão.", "Motivo");
        var r = await procedures.GerarRevisaoAmostraAsync(id, Corte(m, 500)!); // transação própria: fora do EF (P22)
        return r.Sucesso ? Resultado.Ok(r.Valor.IdNova) : r.Falha;
    }

    // ------------------------------------------------------------------ comprovante

    public async Task<AmostraComprovanteVM?> ComprovanteAsync(int id)
    {
        var v = await db.VwLabAmostras.AsNoTracking().SingleOrDefaultAsync(x => x.IdLabHistAmostras == id);
        if (v is null) return null;
        var empresa = await db.Empresas.AsNoTracking().Where(e => e.IdEmpresas == v.IdEmpresas).Select(e => e.RazaoSocial).SingleAsync();
        var obs = await db.LabHistAmostras.AsNoTracking().Where(a => a.IdLabHistAmostras == id).Select(a => a.TxObservacao).SingleAsync();
        var endereco = v.EnderecoLinha1 is null ? null : $"{v.EnderecoLinha1} — {v.EnderecoLinha2}";
        return new AmostraComprovanteVM(empresa, v.NrAmostra, v.ClienteNome, v.ClienteCpfCnpj, endereco, v.NrProposta, v.NrPedidoFmt,
            $"{v.ProdutoDescricao} ({v.ProdutoCodigo})", v.NrLote, v.Fabricacao, v.Validade, v.Embalagem, v.QtdeEmbalagensEntregue,
            v.DtEntrega, v.HrEntrega, v.LocalRecebimento, v.DtHrColeta, v.NomeColetor, v.ResponsavelNome, v.StatusAmostra,
            await TestesAsync(id), HtmlSimples.Limpar(obs));
    }

    // ------------------------------------------------------------------ apoio

    private static string? Corte(string? s, int max) => Textos.Limpo(s) is { } t ? (t.Length > max ? t[..max] : t) : null;

    private static byte? Parte(int? v) => v is > 0 and <= 255 ? (byte)v : null;

    // temperatura pode ser negativa (congelados)
    private static bool LerTemperatura(string? texto, out decimal? valor)
    {
        valor = null;
        if (Textos.Limpo(texto) is not { } t) return true;
        var negativo = t.StartsWith('-');
        if (!Textos.Decimal(negativo ? t[1..].Trim() : t, out var v) || decimal.Round(v, 2) != v || v >= 100_000) return false;
        valor = negativo ? -v : v;
        return true;
    }
}

internal static class HoraExtensions
{
    /// <summary>Hora sem segundos (o legado guardava hh:mm).</summary>
    public static TimeOnly Truncar(this TimeOnly t) => new(t.Hour, t.Minute);
}
