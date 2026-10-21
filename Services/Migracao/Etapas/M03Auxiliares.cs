using LabControl.Models;

namespace LabControl.Services.Migracao.Etapas;

/// <summary>
/// M-03 — os 20 cadastros auxiliares (M-03-auxiliares.md). <b>Código dos tipos de amostra/análise = ID antigo</b>
/// (é o TT/AA do número da amostra: os números impressos continuam iguais). Cores TColor → #RRGGBB. Roda antes do
/// <c>SP_INICIALIZAR_EMPRESA</c> (M-04), que depois só completa o que faltar (INSERT IGNORE pela descrição).
/// </summary>
public sealed class M03Auxiliares : IEtapaMigracao
{
    public string Codigo => "M-03";
    public string Nome => "Cadastros auxiliares";

    public async Task<int> ExecutarAsync(ContextoMigracao c, CancellationToken ct)
    {
        var permiteResultado = (await c.Fb.LerAsync("SELECT ID_AMOSTRAS_STATUS_RESULTADOS FROM EMPRESAS_PARAMETROS", ct)).SingleOrDefault()?.Int("ID_AMOSTRAS_STATUS_RESULTADOS");
        string Desc<T>(LinhaFb l, string onde, string coluna = "DESCRICAO") => c.Cortar<T>("Descricao", l.Texto(coluna) ?? "SEM DESCRIÇÃO", onde)!;
        var n = 0;
        // tipos de amostra/análise nunca se fundem (cada código é um TT/AA de número impresso): descrição repetida ganha o ID
        string Unica(HashSet<string> vistas, string desc, string tipo, int id)
        {
            if (vistas.Add(Conversoes.NomeComparavel(desc))) return desc;
            c.Anomalia($"{tipo} {id}: descrição \"{desc}\" repetida no legado — gravada como \"{desc} ({id})\".");
            return $"{desc} ({id})";
        }
        HashSet<string> descAmostra = [], descAnalise = [];

        n += await Migrar.AuxiliarAsync(c, "LOGRADOUROS_TIPOS", "ID_LOGRADOUROS_TIPOS", "DESCRICAO, ABREVIATURA",
            (l, id) => new TipoLogradouro { Descricao = Desc<TipoLogradouro>(l, $"tipo de logradouro {id}").ToUpperInvariant(),
                Abreviatura = c.Cortar<TipoLogradouro>(nameof(TipoLogradouro.Abreviatura), (l.Texto("ABREVIATURA") ?? l.Texto("DESCRICAO") ?? "-").ToUpperInvariant(), $"tipo de logradouro {id}")! },
            e => e.Abreviatura, e => e.IdTiposLogradouro, ct);
        n += await Migrar.AuxiliarAsync(c, "ENDERECOS_TIPOS", "ID_ENDERECOS_TIPOS", "DESCRICAO",
            (l, id) => new EnderecoTipo { Descricao = Desc<EnderecoTipo>(l, $"tipo de endereço {id}"), Finalidade = id switch
            {
                1 => FinalidadeEndereco.NotaFiscal, 2 => FinalidadeEndereco.Correspondencia, 3 => FinalidadeEndereco.Entrega,
                4 => FinalidadeEndereco.Cobranca, _ => FinalidadeEndereco.Geral,
            } },
            e => e.Descricao, e => e.IdEnderecosTipos, ct);
        n += await Migrar.AuxiliarAsync(c, "FONES_TIPOS", "ID_FONES_TIPOS", "DESCRICAO",
            (l, id) => new FoneTipo { Descricao = Desc<FoneTipo>(l, $"tipo de telefone {id}") }, e => e.Descricao, e => e.IdFonesTipos, ct);
        n += await Migrar.AuxiliarAsync(c, "EMAILS_TIPOS", "ID_EMAILS_TIPOS", "DESCRICAO",
            (l, id) => new EmailTipo { Descricao = Desc<EmailTipo>(l, $"tipo de e-mail {id}") }, e => e.Descricao, e => e.IdEmailsTipos, ct);
        n += await Migrar.AuxiliarAsync(c, "TIPOS_REG_PROFISSIONAL", "ID_TIPOS_REG_PROFISSIONAL", "DESCRICAO_REG_PROFISSIONAL",
            (l, id) => new TipoRegProfissional { Descricao = Desc<TipoRegProfissional>(l, $"registro profissional {id}", "DESCRICAO_REG_PROFISSIONAL") },
            e => e.Descricao, e => e.IdTiposRegProfissional, ct);
        n += await Migrar.AuxiliarAsync(c, "CARGO_FUNCIONARIOS", "ID_CARGO_FUNCIONARIOS", "DESCRICAO",
            (l, id) => new CargoFuncionario { Descricao = Desc<CargoFuncionario>(l, $"cargo {id}") }, e => e.Descricao, e => e.IdCargosFuncionarios, ct);
        n += await Migrar.AuxiliarAsync(c, "IDIOMAS", "ID_IDIOMAS", "DESCRICAO",
            (l, id) => new Idioma { Descricao = Desc<Idioma>(l, $"idioma {id}") }, e => e.Descricao, e => e.IdIdiomas, ct);
        n += await Migrar.AuxiliarAsync(c, "UNIDADES", "ID_UNIDADES", "DESCRICAO, SIGLA",
            (l, id) => new Unidade { Descricao = Desc<Unidade>(l, $"unidade {id}"),
                Sigla = c.Cortar<Unidade>(nameof(Unidade.Sigla), l.Texto("SIGLA") ?? $"U{id}", $"unidade {id}")! },
            e => e.Sigla, e => e.IdUnidades, ct);
        n += await Migrar.AuxiliarAsync(c, "EMBALAGENS_TIPOS", "ID_EMBALAGENS_TIPOS", "DESCRICAO",
            (l, id) => new EmbalagemTipo { Descricao = Desc<EmbalagemTipo>(l, $"embalagem {id}") }, e => e.Descricao, e => e.IdEmbalagensTipos, ct);
        n += await Migrar.AuxiliarAsync(c, "LAB_TIPOS_RESULTADOS", "ID_LAB_TIPOS_RESULTADOS", "DESCRICAO",
            (l, id) => new LabTipoResultado { Descricao = Desc<LabTipoResultado>(l, $"tipo de resultado {id}") }, e => e.Descricao, e => e.IdLabTiposResultados, ct);
        n += await Migrar.AuxiliarAsync(c, "CONDICOES_PAGTOS", "ID_CONDICOES_PAGTOS", "CODIGO, DESCRICAO, INATIVO",
            (l, id) => new CondicaoPagto { Codigo = c.Cortar<CondicaoPagto>(nameof(CondicaoPagto.Codigo), (l.Texto("CODIGO") ?? id.ToString()).ToUpperInvariant(), $"condição {id}")!,
                Descricao = Desc<CondicaoPagto>(l, $"condição {id}"), Ativo = l.Int("INATIVO") != 1 },
            e => e.Codigo, e => e.IdCondicoesPagtos, ct);
        n += await Migrar.AuxiliarAsync(c, "PRAZOS", "ID_PRAZOS", "DESCRICAO, QTDE",
            (l, id) => new Prazo { Descricao = Desc<Prazo>(l, $"prazo {id}"), QtdeDias = Math.Max(0, l.Int("QTDE") ?? 0) }, e => e.Descricao, e => e.IdPrazos, ct);
        n += await Migrar.AuxiliarAsync(c, "MOEDAS", "ID_MOEDAS", "DESCRICAO, SIGLA",
            (l, id) => new Moeda { Descricao = Desc<Moeda>(l, $"moeda {id}"), Sigla = c.Cortar<Moeda>(nameof(Moeda.Sigla), l.Texto("SIGLA") ?? $"M{id}", $"moeda {id}")! },
            e => e.Sigla, e => e.IdMoedas, ct);
        n += await Migrar.AuxiliarAsync(c, "AMOSTRAS_TIPO", "ID_AMOSTRAS_TIPO", "DESCRICAO",
            (l, id) => new AmostraTipo { Codigo = CodigoDoNumero(c, "tipo de amostra", id), Descricao = Unica(descAmostra, Desc<AmostraTipo>(l, $"tipo de amostra {id}"), "Tipo de amostra", id) },
            chave: null, e => e.IdAmostrasTipo, ct);
        n += await Migrar.AuxiliarAsync(c, "AMOSTRAS_STATUS", "ID_AMOSTRAS_STATUS", "DESCRICAO, COR",
            (l, id) => new AmostraStatus { Descricao = Desc<AmostraStatus>(l, $"status da amostra {id}"), Cor = Conversoes.Cor(l.Texto("COR")), PermiteResultado = id == permiteResultado },
            e => e.Descricao, e => e.IdAmostrasStatus, ct);
        n += await Migrar.AuxiliarAsync(c, "ANALISES_TIPO", "ID_ANALISES_TIPO", "DESCRICAO",
            (l, id) => new AnaliseTipo { Codigo = CodigoDoNumero(c, "tipo de análise", id), Descricao = Unica(descAnalise, Desc<AnaliseTipo>(l, $"tipo de análise {id}"), "Tipo de análise", id) },
            chave: null, e => e.IdAnalisesTipo, ct);
        n += await Migrar.AuxiliarAsync(c, "ANALISES_STATUS", "ID_ANALISES_STATUS", "DESCRICAO, COR",
            (l, id) => new AnaliseStatus { Descricao = Desc<AnaliseStatus>(l, $"status da análise {id}"), Cor = Conversoes.Cor(l.Texto("COR")) },
            e => e.Descricao, e => e.IdAnalisesStatus, ct);
        n += await Migrar.AuxiliarAsync(c, "BOLETINS_STATUS", "ID_BOLETINS_STATUS", "DESCRICAO, COR",
            (l, id) => new BoletimStatus { Descricao = Desc<BoletimStatus>(l, $"status do boletim {id}"), Cor = Conversoes.Cor(l.Texto("COR")) },
            e => e.Descricao, e => e.IdBoletinsStatus, ct);
        n += await Migrar.AuxiliarAsync(c, "LAB_PROPOSTAS_STATUS", "ID_LAB_PROPOSTAS_STATUS", "DESCRICAO, COR",
            (l, id) => new LabPropostaStatus { Descricao = Desc<LabPropostaStatus>(l, $"status da proposta {id}"), Cor = Conversoes.Cor(l.Texto("COR")) },
            e => e.Descricao, e => e.IdLabPropostasStatus, ct);
        // métodos não têm UNIQUE no destino: os 7 repetidos do legado entram como estão (o ID é a chave)
        var metodos = 0;
        n += metodos = await Migrar.AuxiliarAsync(c, "ANALISES_METODOS", "ID_ANALISES_METODOS", "DESCRICAO",
            (l, id) => new AnaliseMetodo { Descricao = Desc<AnaliseMetodo>(l, $"método {id}") }, chave: null, e => e.IdAnalisesMetodos, ct);

        c.Conferir("códigos dos tipos de amostra = IDs do legado", true, c.Mapa("AMOSTRAS_TIPO").All(m => c.Db.AmostrasTipo.Single(t => t.IdAmostrasTipo == m.Value).Codigo == m.Key));
        c.Conferir("códigos dos tipos de análise = IDs do legado", true, c.Mapa("ANALISES_TIPO").All(m => c.Db.AnalisesTipo.Single(t => t.IdAnalisesTipo == m.Value).Codigo == m.Key));
        c.Conferir("status da amostra que permite resultado", 1, c.Db.AmostrasStatus.Count(s => s.PermiteResultado));
        c.Log($"{n} registros em 20 cadastros auxiliares ({metodos} métodos).");
        return n;
    }

    // o código (1–99) é o TT/AA do número da amostra: tem de ser o ID antigo
    private static short CodigoDoNumero(ContextoMigracao c, string tipo, int id)
    {
        if (id is >= 1 and <= 99) return (short)id;
        throw new InvalidOperationException($"O {tipo} {id} do legado não cabe no código de 1 a 99 do número da amostra.");
    }
}
