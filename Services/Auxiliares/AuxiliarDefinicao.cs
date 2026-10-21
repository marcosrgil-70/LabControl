using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services.Auxiliares;

/// <summary>Como o campo aparece no formulário (o tipo do banco vem do modelo do EF).</summary>
public enum TipoCampo { Texto, Inteiro, Logico, Cor, Opcoes }

/// <param name="Propriedade">Nome da propriedade da entidade (ex.: <c>Descricao</c>).</param>
/// <param name="NaLista">Mostra como coluna na lista.</param>
/// <param name="Busca">Entra na busca da lista (só texto).</param>
public sealed record CampoAuxiliar(string Propriedade, string Rotulo, TipoCampo Tipo = TipoCampo.Texto,
    bool NaLista = true, bool Busca = false, string? Ajuda = null, int? Minimo = null, int? Maximo = null, bool Maiusculas = false);

/// <summary>Botão da linha que abre <c>Controller/Action/{id}</c> (consulta; a gravação lá exige AUXILIARES.ALTERAR).</summary>
public sealed record AcaoLinhaAuxiliar(string Texto, string Icone, string Controller, string Action);

/// <summary>
/// Um cadastro auxiliar (docs/telas/T10-00-crud-auxiliares.md): tabela, textos e campos além de ATIVO. Tamanho e
/// obrigatoriedade vêm do modelo do EF (gerado de Scripts/v2), a unicidade vem das UNIQUE do banco (erro 1062).
/// </summary>
public abstract class AuxiliarDefinicao
{
    /// <summary>Nome da tabela, usado na rota (<c>/Auxiliares/IDIOMAS</c>).</summary>
    public required string Tabela { get; init; }
    public required string Titulo { get; init; }
    /// <summary>Nome no singular, minúsculo ("idioma"), para mensagens e botões.</summary>
    public required string Singular { get; init; }
    /// <summary>Gênero do nome, para as mensagens ("Condição de pagamento gravada").</summary>
    public bool Feminino { get; init; }
    public string Icone { get; init; } = "bi-tags";
    public string? Ajuda { get; init; }
    public required IReadOnlyList<CampoAuxiliar> Campos { get; init; }
    /// <summary>Propriedade mostrada nos combos e na ordenação.</summary>
    public string CampoTexto { get; init; } = "Descricao";
    /// <summary>Botão extra em cada linha da lista, para uma tela própria (ex.: parcelas da condição de pagamento).</summary>
    public AcaoLinhaAuxiliar? Acao { get; init; }

    public abstract Type Entidade { get; }

    /// <summary>"Idioma gravado" / "Condição de pagamento gravada" (particípio informado no masculino).</summary>
    public string Frase(string participio) =>
        $"{char.ToUpperInvariant(Singular[0])}{Singular[1..]} {(Feminino ? participio[..^1] + "a" : participio)}.";

    public string NaoEncontrado => $"{(Feminino ? "A" : "O")} {Singular} não foi {(Feminino ? "encontrada" : "encontrado")} (pode ter sido excluíd{(Feminino ? "a" : "o")}).";

    // despacho para os métodos genéricos do serviço (o controller só conhece a definição)
    public abstract Task<AuxiliarListaVM> ListarAsync(AuxiliarService s, AuxiliarFiltroVM filtro);
    public abstract Task<AuxiliarFormVM?> CarregarAsync(AuxiliarService s, int id);
    public abstract Task PrepararFormAsync(AuxiliarService s, AuxiliarFormVM vm);
    public abstract Task<Resultado<int>> GravarAsync(AuxiliarService s, AuxiliarFormVM vm);
    public abstract Task<Resultado> ExcluirAsync(AuxiliarService s, int id);
    public abstract Task<Resultado> DefinirAtivoAsync(AuxiliarService s, int id, bool ativo);
    public abstract Task<IReadOnlyList<OpcaoVM>> OpcoesAsync(AuxiliarService s, int? incluirId);
}

/// <summary>Definição tipada. Ganchos para as regras específicas de cada auxiliar (2.2: T10-14, T10-15…).</summary>
public sealed class AuxiliarDefinicao<T> : AuxiliarDefinicao where T : class, IPorEmpresa, new()
{
    public override Type Entidade => typeof(T);

    /// <summary>Regra antes de gravar (inclusão e alteração); a entidade já tem os valores do formulário.</summary>
    public Func<LabDbContext, T, Task<Resultado>>? ValidarAntesDeGravar { get; init; }

    /// <summary>Na alteração: o campo pode mudar? (ex.: código do tipo de amostra imutável depois de usado).</summary>
    public Func<LabDbContext, T, string, Task<bool>>? PodeAlterarCampo { get; init; }

    /// <summary>Regra antes de excluir um registro sem uso (o uso já é recusado pela FK do banco).</summary>
    public Func<LabDbContext, T, Task<Resultado>>? ValidarAntesDeExcluir { get; init; }

    public override Task<AuxiliarListaVM> ListarAsync(AuxiliarService s, AuxiliarFiltroVM filtro) => s.ListarAsync(this, filtro);
    public override Task<AuxiliarFormVM?> CarregarAsync(AuxiliarService s, int id) => s.CarregarAsync(this, id);
    public override Task PrepararFormAsync(AuxiliarService s, AuxiliarFormVM vm) => s.PrepararAsync(this, vm);
    public override Task<Resultado<int>> GravarAsync(AuxiliarService s, AuxiliarFormVM vm) => s.GravarAsync(this, vm);
    public override Task<Resultado> ExcluirAsync(AuxiliarService s, int id) => s.ExcluirAsync(this, id);
    public override Task<Resultado> DefinirAtivoAsync(AuxiliarService s, int id, bool ativo) => s.DefinirAtivoAsync(this, id, ativo);
    public override Task<IReadOnlyList<OpcaoVM>> OpcoesAsync(AuxiliarService s, int? incluirId) => s.OpcoesAsync(this, incluirId);
}

/// <summary>
/// Catálogo dos cadastros auxiliares (T10-01…T10-20). Para incluir um auxiliar: uma entrada aqui, conforme o MD
/// da tela (campos, regras específicas nos ganchos) — controller, serviço e views são os mesmos para todos.
/// </summary>
public static class CatalogoAuxiliares
{
    // antes de Todos: campos estáticos são inicializados na ordem em que aparecem
    private static readonly CampoAuxiliar Descricao = new("Descricao", "Descrição", Busca: true);
    private static readonly CampoAuxiliar Cor = new("Cor", "Cor", TipoCampo.Cor);
    private static readonly CampoAuxiliar CodigoNumero = new("Codigo", "Código", TipoCampo.Inteiro, Minimo: 1, Maximo: 99,
        Ajuda: "Compõe o número da amostra; não pode mudar depois de usado.");

    public static readonly IReadOnlyList<AuxiliarDefinicao> Todos =
    [
        new AuxiliarDefinicao<Idioma>
        {
            Tabela = "IDIOMAS", Titulo = "Idiomas", Singular = "idioma", Icone = "bi-translate",
            Ajuda = "Idioma de cada análise da proposta; o teste da amostra herda.",
            Campos = [Descricao],
        },
        // T10-01…T10-06: tipos usados nos cadastros de empresa, clientes/fornecedores e profissionais (P25)
        new AuxiliarDefinicao<TipoLogradouro>
        {
            Tabela = "TIPOS_LOGRADOURO", Titulo = "Tipos de logradouro", Singular = "tipo de logradouro", Icone = "bi-signpost",
            Ajuda = "Rua, Avenida, Rodovia… escolhido em cada endereço (a busca de CEP reconhece pela descrição).",
            Campos = [Descricao, new("Abreviatura", "Abreviatura", Busca: true, Maiusculas: true)],
        },
        new AuxiliarDefinicao<EnderecoTipo>
        {
            Tabela = "ENDERECOS_TIPOS", Titulo = "Tipos de endereço", Singular = "tipo de endereço", Icone = "bi-mailbox",
            Ajuda = "Nota fiscal, correspondência, entrega, cobrança… (um endereço ativo por tipo em cada cadastro).",
            Campos = [Descricao, new("Finalidade", "Finalidade", TipoCampo.Opcoes,
                Ajuda: "Orienta as sugestões da proposta (endereço do laudo, da cobrança).")],
        },
        new AuxiliarDefinicao<FoneTipo>
        {
            Tabela = "FONES_TIPOS", Titulo = "Tipos de telefone", Singular = "tipo de telefone", Icone = "bi-telephone",
            Ajuda = "Comercial, residencial, celular… escolhido em cada telefone.",
            Campos = [Descricao],
        },
        new AuxiliarDefinicao<EmailTipo>
        {
            Tabela = "EMAILS_TIPOS", Titulo = "Tipos de e-mail", Singular = "tipo de e-mail", Icone = "bi-envelope",
            Ajuda = "Geral, comercial, financeiro… escolhido em cada e-mail.",
            Campos = [Descricao],
        },
        new AuxiliarDefinicao<TipoRegProfissional>
        {
            Tabela = "TIPOS_REG_PROFISSIONAL", Titulo = "Registros profissionais", Singular = "tipo de registro", Icone = "bi-patch-check",
            Ajuda = "CRQ, CRF, CRBio… impresso no boletim junto com o nº e a UF do profissional.",
            Campos = [Descricao],
        },
        new AuxiliarDefinicao<CargoFuncionario>
        {
            Tabela = "CARGOS_FUNCIONARIOS", Titulo = "Cargos de funcionários", Singular = "cargo", Icone = "bi-person-vcard",
            Ajuda = "Cargo do profissional, impresso no boletim.",
            Campos = [Descricao],
        },
        // T10-08…T10-20 (2.2)
        new AuxiliarDefinicao<Unidade>
        {
            Tabela = "UNIDADES", Titulo = "Unidades de medida", Singular = "unidade", Feminino = true, Icone = "bi-rulers",
            Ajuda = "UFC/g, mg/L… a sigla compõe o resultado impresso no boletim.",
            Campos = [Descricao, new("Sigla", "Sigla", Busca: true)],
        },
        new AuxiliarDefinicao<EmbalagemTipo>
        {
            Tabela = "EMBALAGENS_TIPOS", Titulo = "Tipos de embalagem", Singular = "tipo de embalagem", Icone = "bi-box-seam",
            Ajuda = "Embalagem da amostra, obrigatória no recebimento.",
            Campos = [Descricao],
        },
        new AuxiliarDefinicao<LabTipoResultado>
        {
            Tabela = "LAB_TIPOS_RESULTADOS", Titulo = "Tipos de resultado", Singular = "tipo de resultado", Icone = "bi-card-text",
            Ajuda = "Textos dos resultados descritivos (\"Ausência em 25 g\", \"Satisfatório\") impressos no boletim.",
            Campos = [Descricao],
        },
        new AuxiliarDefinicao<CondicaoPagto>
        {
            Tabela = "CONDICOES_PAGTOS", Titulo = "Condições de pagamento", Singular = "condição de pagamento", Feminino = true, Icone = "bi-credit-card",
            Ajuda = "Condição da proposta e do pedido de venda, com parcelas opcionais.",
            Campos = [new("Codigo", "Código", Busca: true, Maiusculas: true), Descricao],
            Acao = new("Parcelas", "bi-list-ol", "CondicoesPagamento", "Parcelas"),
        },
        new AuxiliarDefinicao<Prazo>
        {
            Tabela = "PRAZOS", Titulo = "Prazos", Singular = "prazo", Icone = "bi-hourglass-split",
            Ajuda = "Prazo da análise na proposta: a data prevista do teste é a entrega + os dias.",
            Campos = [Descricao, new("QtdeDias", "Qtde. de dias", TipoCampo.Inteiro, Minimo: 0, Maximo: 3650),
                new("DiasUteis", "Dias úteis", TipoCampo.Logico, Ajuda: "Ainda não usado no cálculo: a data prevista conta dias corridos, como o legado.")],
        },
        new AuxiliarDefinicao<Moeda>
        {
            Tabela = "MOEDAS", Titulo = "Moedas", Singular = "moeda", Feminino = true, Icone = "bi-currency-exchange",
            Ajuda = "Moeda opcional da proposta e do pedido (R$, US$).",
            Campos = [Descricao, new("Sigla", "Sigla", Busca: true)],
        },
        new AuxiliarDefinicao<AmostraTipo>
        {
            Tabela = "AMOSTRAS_TIPO", Titulo = "Tipos de amostra", Singular = "tipo de amostra", Icone = "bi-droplet",
            Ajuda = "O código (1–99) é o TT do número da amostra: não muda depois de usado.",
            Campos = [CodigoNumero, Descricao],
            PodeAlterarCampo = async (db, e, campo) => campo != "Codigo"
                || !await db.LabHistAmostras.AnyAsync(a => a.IdAmostrasTipo == e.IdAmostrasTipo),
        },
        new AuxiliarDefinicao<AmostraStatus>
        {
            Tabela = "AMOSTRAS_STATUS", Titulo = "Status da amostra", Singular = "status da amostra", Icone = "bi-flag",
            Ajuda = "Só amostras num status que permite resultado vão para Resultados, Boletim, Etiquetas e a fila.",
            Campos = [Descricao, Cor, new("PermiteResultado", "Permite resultado", TipoCampo.Logico,
                Ajuda: "Libera a amostra para Resultados, Boletim, Etiquetas e a fila de análises.")],
            ValidarAntesDeGravar = (db, e) => RestaStatusComResultadoAsync(db, e, excluindo: false),
            ValidarAntesDeExcluir = (db, e) => RestaStatusComResultadoAsync(db, e, excluindo: true),
        },
        new AuxiliarDefinicao<AnaliseTipo>
        {
            Tabela = "ANALISES_TIPO", Titulo = "Tipos de análise", Singular = "tipo de análise", Icone = "bi-clipboard2-pulse",
            Ajuda = "O código (1–99) é o AA do número da amostra: não muda depois de usado.",
            Campos = [CodigoNumero, Descricao],
            PodeAlterarCampo = async (db, e, campo) => campo != "Codigo"
                || !(await db.LabHistAmostras.AnyAsync(a => a.IdAnalisesTipo == e.IdAnalisesTipo)
                     || await db.LabParametrosAnalises.AnyAsync(p => p.IdAnalisesTipo == e.IdAnalisesTipo)),
        },
        new AuxiliarDefinicao<AnaliseStatus>
        {
            Tabela = "ANALISES_STATUS", Titulo = "Status da análise", Singular = "status da análise", Icone = "bi-activity",
            Ajuda = "Situação de cada teste da amostra (aguardando, em análise, concluída).",
            Campos = [Descricao, Cor],
        },
        new AuxiliarDefinicao<AnaliseMetodo>
        {
            Tabela = "ANALISES_METODOS", Titulo = "Métodos de análise", Singular = "método de análise", Icone = "bi-journal-bookmark",
            Ajuda = "Norma ou método (\"ISO 4833-1:2013\") das metodologias utilizadas no boletim.",
            Campos = [Descricao],
        },
        new AuxiliarDefinicao<BoletimStatus>
        {
            Tabela = "BOLETINS_STATUS", Titulo = "Status do boletim", Singular = "status do boletim", Icone = "bi-file-earmark-check",
            Ajuda = "Situação do boletim; o inicial e o de emitido ficam nos parâmetros da empresa.",
            Campos = [Descricao, Cor],
        },
        new AuxiliarDefinicao<LabPropostaStatus>
        {
            Tabela = "LAB_PROPOSTAS_STATUS", Titulo = "Status da proposta", Singular = "status da proposta", Icone = "bi-file-earmark-text",
            Ajuda = "Situação da proposta; só as que permitem recebem amostras.",
            Campos = [Descricao, Cor, new("PermiteAmostra", "Permite receber amostra", TipoCampo.Logico,
                Ajuda: "Desmarcado, o recebimento recusa amostras de propostas neste status.")],
        },
    ];

    /// <summary>T10-15: sem um status ativo que permita resultado, nenhuma amostra chegaria a Resultados/Boletim.</summary>
    private static async Task<Resultado> RestaStatusComResultadoAsync(LabDbContext db, AmostraStatus e, bool excluindo)
    {
        // gravar um que permite, ou excluir um que não permite, não tira o último
        if ((e.Ativo && e.PermiteResultado) != excluindo) return Resultado.Ok();
        var outro = await db.AmostrasStatus.AnyAsync(s => s.IdAmostrasStatus != e.IdAmostrasStatus && s.Ativo && s.PermiteResultado);
        return outro ? Resultado.Ok()
            : Resultado.Erro("Deve existir ao menos um status ativo que permite resultado (sem ele nenhuma amostra chega a Resultados e Boletim).", "PermiteResultado");
    }

    public static AuxiliarDefinicao? Encontrar(string? tabela) =>
        Todos.FirstOrDefault(d => string.Equals(d.Tabela, tabela, StringComparison.OrdinalIgnoreCase));
}
