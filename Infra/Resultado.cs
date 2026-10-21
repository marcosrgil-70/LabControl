namespace LabControl.Infra;

/// <summary>Que tipo de erro é: as telas decidem por ele, nunca pelo texto da mensagem.</summary>
public enum TipoFalha
{
    /// <summary>Regra de negócio (validação do serviço ou 45000 do banco).</summary>
    Negocio,
    /// <summary>UNIQUE violada (1062).</summary>
    Duplicado,
    /// <summary>Exclusão recusada pela FK (1451): a tela pode oferecer "Inativar".</summary>
    EmUso,
    /// <summary>Registro relacionado não existe mais (1452).</summary>
    ReferenciaAusente,
}

/// <summary>Resultado de uma operação de serviço: sucesso ou mensagem para o usuário.</summary>
public interface IResultado
{
    bool Sucesso { get; }
    /// <summary>Mensagem de negócio (erro) — texto pronto para a tela.</summary>
    string? Mensagem { get; }
    /// <summary>Campo do formulário ao qual o erro se refere (null = erro geral).</summary>
    string? Campo { get; }
    TipoFalha Tipo { get; }
}

/// <summary>Resultado sem valor. <c>return Resultado.Erro("...")</c> também serve para <see cref="Resultado{T}"/>.</summary>
public sealed class Resultado : IResultado
{
    private static readonly Resultado Sucesso_ = new(true, null, null, TipoFalha.Negocio);

    private Resultado(bool sucesso, string? mensagem, string? campo, TipoFalha tipo) => (Sucesso, Mensagem, Campo, Tipo) = (sucesso, mensagem, campo, tipo);

    public bool Sucesso { get; }
    public string? Mensagem { get; }
    public string? Campo { get; }
    public TipoFalha Tipo { get; }

    /// <summary>O erro, para devolver adiante (inclusive como <see cref="Resultado{T}"/>) sem perder campo e tipo.</summary>
    public Falha Falha => new(Mensagem!, Campo, Tipo);

    public static Resultado Ok() => Sucesso_;
    public static Resultado<T> Ok<T>(T valor) => new(valor);
    public static Falha Erro(string mensagem, string? campo = null, TipoFalha tipo = TipoFalha.Negocio) => new(mensagem, campo, tipo);

    public static implicit operator Resultado(Falha f) => new(false, f.Mensagem, f.Campo, f.Tipo);
}

/// <summary>Resultado com valor (ex.: id gravado, números gerados pelo banco).</summary>
public sealed class Resultado<T> : IResultado
{
    private readonly T _valor = default!;

    internal Resultado(T valor) => (Sucesso, _valor) = (true, valor);
    private Resultado(Falha f) => (Mensagem, Campo, Tipo) = (f.Mensagem, f.Campo, f.Tipo);

    public bool Sucesso { get; }
    public string? Mensagem { get; }
    public string? Campo { get; }
    public TipoFalha Tipo { get; }

    /// <summary>Valor (só quando <see cref="Sucesso"/>).</summary>
    public T Valor => Sucesso ? _valor : throw new InvalidOperationException($"Resultado com erro não tem valor: {Mensagem}");

    /// <inheritdoc cref="Resultado.Falha"/>
    public Falha Falha => new(Mensagem!, Campo, Tipo);

    public static implicit operator Resultado<T>(Falha f) => new(f);
}

/// <summary>Erro criado por <see cref="Resultado.Erro"/>; converte para <see cref="Resultado"/> ou <see cref="Resultado{T}"/>.</summary>
public readonly record struct Falha(string Mensagem, string? Campo, TipoFalha Tipo = TipoFalha.Negocio);
