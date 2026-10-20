using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.ViewModels;

namespace LabControl.Services.Auxiliares;

/// <summary>Como o campo aparece no formulário (o tipo do banco vem do modelo do EF).</summary>
public enum TipoCampo { Texto, Inteiro, Logico, Cor, Opcoes }

/// <param name="Propriedade">Nome da propriedade da entidade (ex.: <c>Descricao</c>).</param>
/// <param name="NaLista">Mostra como coluna na lista.</param>
/// <param name="Busca">Entra na busca da lista (só texto).</param>
public sealed record CampoAuxiliar(string Propriedade, string Rotulo, TipoCampo Tipo = TipoCampo.Texto,
    bool NaLista = true, bool Busca = false, string? Ajuda = null, int? Minimo = null, int? Maximo = null, bool Maiusculas = false);

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
    private static readonly CampoAuxiliar Descricao = new("Descricao", "Descrição", Busca: true);

    public static readonly IReadOnlyList<AuxiliarDefinicao> Todos =
    [
        new AuxiliarDefinicao<Idioma>
        {
            Tabela = "IDIOMAS", Titulo = "Idiomas", Singular = "idioma", Icone = "bi-translate",
            Ajuda = "Idioma de cada análise da proposta; o teste da amostra herda.",
            Campos = [Descricao],
        },
    ];

    public static AuxiliarDefinicao? Encontrar(string? tabela) =>
        Todos.FirstOrDefault(d => string.Equals(d.Tabela, tabela, StringComparison.OrdinalIgnoreCase));
}
