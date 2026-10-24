using System.Text.RegularExpressions;
using MySqlConnector;

namespace LabControl.Infra;

/// <summary>
/// Tradução de erros do MySQL para mensagens de negócio (docs/processos/P22-erros-transacoes.md).
/// SQLSTATE 45000 (SIGNAL das triggers/procedures) já vem em português, pronto para o usuário.
/// Erros não listados são técnicos: devem subir (log + página de erro).
/// </summary>
/// <summary>
/// Recusa de negócio que já veio tratada (ex.: <see cref="Resultado"/> de uma procedure chamada dentro de
/// <see cref="ErrosMySql.CapturarAsync{T}"/>): lançada para desfazer a transação e mostrada ao usuário como os 45000.
/// </summary>
public sealed class ErroDeNegocioException(string mensagem) : Exception(mensagem);

public static partial class ErrosMySql
{
    public const string SqlStateNegocio = "45000";

    /// <summary>FK recusou a exclusão (1451) — <see cref="TipoFalha.EmUso"/>.</summary>
    private const string MensagemEmUso = "Registro em uso por outros cadastros ou movimentos: não pode ser excluído (inative-o em vez de excluir).";

    /// <summary>Mensagem para o usuário, ou null se o erro for técnico.</summary>
    public static string? MensagemDeNegocio(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
            if (e is ErroDeNegocioException negocio) return negocio.Message;
        var my = Encontrar(ex);
        if (my is null) return null;
        if (my.SqlState == SqlStateNegocio) return my.Message;
        return my.ErrorCode switch
        {
            MySqlErrorCode.DuplicateKeyEntry => Duplicado(my.Message),
            MySqlErrorCode.RowIsReferenced2 or MySqlErrorCode.RowIsReferenced => MensagemEmUso,
            MySqlErrorCode.NoReferencedRow2 or MySqlErrorCode.NoReferencedRow =>
                "Registro relacionado não encontrado (pode ter sido excluído). Atualize a tela e tente de novo.",
            _ => null,
        };
    }

    public static bool EhErroDeNegocio(Exception ex) => MensagemDeNegocio(ex) is not null;

    /// <summary>Tipo do erro de negócio (para a tela decidir sem comparar a mensagem).</summary>
    public static TipoFalha TipoDe(Exception ex) => Encontrar(ex) switch
    {
        { SqlState: SqlStateNegocio } => TipoFalha.Negocio,
        { ErrorCode: MySqlErrorCode.DuplicateKeyEntry } => TipoFalha.Duplicado,
        { ErrorCode: MySqlErrorCode.RowIsReferenced2 or MySqlErrorCode.RowIsReferenced } => TipoFalha.EmUso,
        { ErrorCode: MySqlErrorCode.NoReferencedRow2 or MySqlErrorCode.NoReferencedRow } => TipoFalha.ReferenciaAusente,
        _ => TipoFalha.Negocio,
    };

    /// <summary>Executa a ação e converte erro de negócio em <see cref="Resultado"/>; erro técnico continua subindo.</summary>
    public static async Task<Resultado> CapturarAsync(Func<Task> acao)
    {
        try { await acao(); return Resultado.Ok(); }
        catch (Exception ex) when (MensagemDeNegocio(ex) is { } msg) { return Resultado.Erro(msg, tipo: TipoDe(ex)); }
    }

    /// <inheritdoc cref="CapturarAsync(Func{Task})"/>
    public static async Task<Resultado<T>> CapturarAsync<T>(Func<Task<T>> acao)
    {
        try { return Resultado.Ok(await acao()); }
        catch (Exception ex) when (MensagemDeNegocio(ex) is { } msg) { return Resultado.Erro(msg, tipo: TipoDe(ex)); }
    }

    /// <summary>MySqlException na cadeia (o EF embrulha em DbUpdateException).</summary>
    public static MySqlException? Encontrar(Exception? ex)
    {
        for (; ex is not null; ex = ex.InnerException)
            if (ex is MySqlException my) return my;
        return null;
    }

    // Constraints UNIQUE de Scripts/v2/01_schema.sql que o usuário pode violar digitando.
    // As demais (números de documento, revisões, vínculos) são protegidas pelo banco e caem na mensagem genérica.
    private static readonly Dictionary<string, string> Unicos = new(StringComparer.OrdinalIgnoreCase)
    {
        ["UQ_PAISES_NOME"] = "Já existe um país com este nome.",
        ["UQ_ESTADOS_SIGLA"] = "Já existe um estado com esta sigla neste país.",
        ["UQ_CIDADES_IBGE"] = "Já existe uma cidade com este código IBGE.",
        ["UQ_EMPRESAS_COD"] = "Já existe uma empresa com este código.",
        ["UQ_EMPRESAS_CNPJ"] = "Já existe uma empresa com este CNPJ.",
        ["UQ_USUARIOS_LOGIN"] = "Já existe um usuário com este login.",
        ["UQ_USUEMP_FUNC"] = "Este profissional já está vinculado a outro usuário nesta empresa.",
        ["UQ_PERFIS_NOME"] = "Já existe um perfil com este nome.",
        ["UQ_TIPLOG"] = "Já existe um tipo de logradouro com esta abreviatura.",
        ["UQ_UNIDADE"] = "Já existe uma unidade com esta sigla.",
        ["UQ_MOEDA"] = "Já existe uma moeda com esta sigla.",
        ["UQ_CONDPAG"] = "Já existe uma condição de pagamento com este código.",
        ["UQ_CONDPARC"] = "Parcela repetida na condição de pagamento.",
        ["UQ_AMOTIP_COD"] = "Já existe um tipo de amostra com este código.",
        ["UQ_ANATIP_COD"] = "Já existe um tipo de análise com este código.",
        ["UQ_ENT_DOC"] = "Já existe um cadastro com este CPF/CNPJ.",
        ["UQ_ENTEML"] = "Este e-mail já está cadastrado para esta entidade.",
        ["UQ_PROD_COD"] = "Já existe um produto com este código.",
        ["UQ_PARAM_DESC"] = "Já existe um parâmetro de análise com esta descrição.",
    };

    // (ID_EMPRESAS, DESCRICAO) dos cadastros auxiliares
    private static readonly HashSet<string> UnicosDescricao = new(StringComparer.OrdinalIgnoreCase)
    {
        "UQ_ENDTIP", "UQ_FONTIP", "UQ_EMLTIP", "UQ_TIPREG", "UQ_CARGO", "UQ_IDIOMA", "UQ_EMBTIP", "UQ_TIPRES", "UQ_PRAZO",
        "UQ_AMOTIP_DESC", "UQ_ANATIP_DESC", "UQ_AMOSTA", "UQ_ANASTA", "UQ_BOLSTA", "UQ_PROPSTA",
    };

    // "Duplicate entry '12-000001' for key 'produtos.UQ_PROD_COD'"
    private static string Duplicado(string mensagem)
    {
        var chave = RegexDuplicado().Match(mensagem) is { Success: true } m ? m.Groups[1].Value : null;
        if (chave is null) return "Já existe um registro com estes dados.";
        if (Unicos.TryGetValue(chave, out var texto)) return texto;
        if (UnicosDescricao.Contains(chave)) return "Já existe um registro com esta descrição.";
        return $"Já existe um registro com estes dados ({chave}).";
    }

    [GeneratedRegex(@"Duplicate entry '.*' for key '(?:\w+\.)?(\w+)'")]
    private static partial Regex RegexDuplicado();
}
