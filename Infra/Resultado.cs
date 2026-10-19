namespace LabControl.Infra;

/// <summary>Resultado de uma operação de serviço: sucesso ou mensagem para o usuário.</summary>
public interface IResultado
{
    bool Sucesso { get; }
    /// <summary>Mensagem de negócio (erro) — texto pronto para a tela.</summary>
    string? Mensagem { get; }
    /// <summary>Campo do formulário ao qual o erro se refere (null = erro geral).</summary>
    string? Campo { get; }
}

/// <summary>Resultado sem valor. <c>return Resultado.Erro("...")</c> também serve para <see cref="Resultado{T}"/>.</summary>
public sealed class Resultado : IResultado
{
    private static readonly Resultado Sucesso_ = new(true, null, null);

    private Resultado(bool sucesso, string? mensagem, string? campo) => (Sucesso, Mensagem, Campo) = (sucesso, mensagem, campo);

    public bool Sucesso { get; }
    public string? Mensagem { get; }
    public string? Campo { get; }

    public static Resultado Ok() => Sucesso_;
    public static Resultado<T> Ok<T>(T valor) => new(valor);
    public static Falha Erro(string mensagem, string? campo = null) => new(mensagem, campo);

    public static implicit operator Resultado(Falha f) => new(false, f.Mensagem, f.Campo);
}

/// <summary>Resultado com valor (ex.: id gravado, números gerados pelo banco).</summary>
public sealed class Resultado<T> : IResultado
{
    private readonly T _valor = default!;

    internal Resultado(T valor) => (Sucesso, _valor) = (true, valor);
    private Resultado(string mensagem, string? campo) => (Mensagem, Campo) = (mensagem, campo);

    public bool Sucesso { get; }
    public string? Mensagem { get; }
    public string? Campo { get; }

    /// <summary>Valor (só quando <see cref="Sucesso"/>).</summary>
    public T Valor => Sucesso ? _valor : throw new InvalidOperationException($"Resultado com erro não tem valor: {Mensagem}");

    public static implicit operator Resultado<T>(Falha f) => new(f.Mensagem, f.Campo);
}

/// <summary>Erro criado por <see cref="Resultado.Erro"/>; converte para <see cref="Resultado"/> ou <see cref="Resultado{T}"/>.</summary>
public readonly record struct Falha(string Mensagem, string? Campo);
