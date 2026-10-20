using System.Collections.Frozen;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LabControl.Models;

// Colunas ENUM do MySQL. O valor gravado no banco vem de [ValorBanco]; o gerador
// (tools/gerador-csharp) escolhe o enum pela lista de valores da coluna.

/// <summary>ENTIDADES.CATEGORIA</summary>
public enum Categoria
{
    [ValorBanco("F")] Fisica,
    [ValorBanco("J")] Juridica,
}

/// <summary>ENTIDADES.SEXO</summary>
public enum Sexo
{
    [ValorBanco("M")] Masculino,
    [ValorBanco("F")] Feminino,
}

/// <summary>ENTIDADES.ESTADO_CIVIL</summary>
public enum EstadoCivil
{
    [ValorBanco("S")] Solteiro,
    [ValorBanco("C")] Casado,
    [ValorBanco("D")] Divorciado,
    [ValorBanco("P")] Separado,
    [ValorBanco("V"), System.ComponentModel.DataAnnotations.Display(Name = "Viúvo")] Viuvo,
}

/// <summary>TIPO_DOCUMENTO da análise/amostra: boletim ou relatório de ensaio</summary>
public enum TipoDocumento
{
    [ValorBanco("BOLETIM")] Boletim,
    [ValorBanco("RELATORIO")] Relatorio,
}

/// <summary>PEDIDOS_VENDA.SITUACAO</summary>
public enum SituacaoPedido
{
    [ValorBanco("ABERTO")] Aberto,
    [ValorBanco("EM_EXECUCAO")] EmExecucao,
    [ValorBanco("CONCLUIDO")] Concluido,
    [ValorBanco("CANCELADO")] Cancelado,
}

/// <summary>LAB_MOV_AMOSTRAS.E_S</summary>
public enum EntradaSaida
{
    [ValorBanco("E")] Entrada,
    [ValorBanco("S")] Saida,
}

/// <summary>LAB_LOCAL_AMOSTRAS.STATUS</summary>
public enum SituacaoArquivo
{
    [ValorBanco("ARQUIVADA")] Arquivada,
    [ValorBanco("DESCARTADA")] Descartada,
}

/// <summary>ENDERECOS_TIPOS.FINALIDADE</summary>
public enum FinalidadeEndereco
{
    [ValorBanco("GERAL")] Geral,
    [ValorBanco("NOTA_FISCAL"), System.ComponentModel.DataAnnotations.Display(Name = "Nota fiscal")] NotaFiscal,
    [ValorBanco("CORRESPONDENCIA"), System.ComponentModel.DataAnnotations.Display(Name = "Correspondência")] Correspondencia,
    [ValorBanco("ENTREGA")] Entrega,
    [ValorBanco("COBRANCA"), System.ComponentModel.DataAnnotations.Display(Name = "Cobrança")] Cobranca,
}

/// <summary>Valor do membro do enum na coluna ENUM do MySQL.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class ValorBancoAttribute(string valor) : Attribute
{
    public string Valor { get; } = valor;
}

/// <summary>Conversão enum C# ⇄ texto da coluna ENUM, a partir de <see cref="ValorBancoAttribute"/>.</summary>
public static class EnumBanco<T> where T : struct, Enum
{
    private static readonly FrozenDictionary<T, string> ParaTexto = typeof(T)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .ToFrozenDictionary(f => (T)f.GetValue(null)!,
                            f => f.GetCustomAttribute<ValorBancoAttribute>()?.Valor
                                 ?? throw new InvalidOperationException($"{typeof(T).Name}.{f.Name} sem [ValorBanco]."));

    private static readonly FrozenDictionary<string, T> DoTexto = ParaTexto.ToFrozenDictionary(p => p.Value, p => p.Key);

    public static string ParaBanco(T valor) => ParaTexto[valor];

    public static T DoBanco(string valor) =>
        DoTexto.TryGetValue(valor, out var v) ? v : throw new InvalidOperationException($"Valor '{valor}' inválido para {typeof(T).Name}.");

    public static readonly ValueConverter<T, string> Conversor = new(v => ParaBanco(v), s => DoBanco(s));
}
