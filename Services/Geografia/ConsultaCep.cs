using System.Net.Http.Json;
using System.Text.Json.Serialization;
using LabControl.Data;
using LabControl.Infra;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LabControl.Services.Geografia;

/// <summary>Endereço de um CEP, já com a cidade/UF do banco (IBGE) e o tipo de logradouro da empresa, quando reconhecido.</summary>
public sealed record EnderecoCepVM(string Cep, string? Logradouro, int? IdTiposLogradouro, string? Complemento, string? Bairro,
    int IdEstado, int IdCidade, string Cidade, string Uf);

/// <summary>Fonte do CEP (ViaCEP em produção; fake nos testes).</summary>
public interface IFonteCep
{
    /// <summary>Dados brutos do CEP (8 dígitos) ou null se não existir. Exceção = serviço indisponível.</summary>
    Task<CepBruto?> ConsultarAsync(string cep, CancellationToken ct);
}

public sealed record CepBruto(string? Logradouro, string? Complemento, string? Bairro, string? Ibge, string? Uf);

/// <summary>ViaCEP (https://viacep.com.br) chamado pelo servidor (sem expor o navegador a outro domínio), com timeout curto.</summary>
public sealed class ViaCep(HttpClient http) : IFonteCep
{
    public async Task<CepBruto?> ConsultarAsync(string cep, CancellationToken ct)
    {
        var r = await http.GetFromJsonAsync<Resposta>($"ws/{cep}/json/", ct);
        // ViaCEP devolve { "erro": true } (ou "true") para CEP inexistente
        var inexistente = r?.Erro is { } e && (e.ValueKind == System.Text.Json.JsonValueKind.True || e.ToString() == "true");
        return r is null || inexistente ? null : new CepBruto(r.Logradouro, r.Complemento, r.Bairro, r.Ibge, r.Uf);
    }

    private sealed record Resposta(
        [property: JsonPropertyName("logradouro")] string? Logradouro,
        [property: JsonPropertyName("complemento")] string? Complemento,
        [property: JsonPropertyName("bairro")] string? Bairro,
        [property: JsonPropertyName("ibge")] string? Ibge,
        [property: JsonPropertyName("uf")] string? Uf,
        [property: JsonPropertyName("erro")] System.Text.Json.JsonElement? Erro);
}

/// <summary>
/// Busca de CEP para o componente de endereço (T11-03 / P25): consulta a fonte, liga a cidade pelo código IBGE
/// (seed 06_cidades) e separa o tipo de logradouro ("Rua X" → RUA + "X") pelos tipos cadastrados na empresa.
/// Cache de 1 dia por CEP; falha da fonte vira mensagem (o usuário digita o endereço).
/// </summary>
public sealed class ConsultaCepService(IFonteCep fonte, IMemoryCache cache, LabDbContext db, ILogger<ConsultaCepService> log)
{
    public async Task<Resultado<EnderecoCepVM>> ConsultarAsync(string? cep)
    {
        var digitos = Documentos.SoDigitos(cep);
        if (digitos.Length != 8) return Resultado.Erro("CEP inválido (8 dígitos).");

        CepBruto? bruto;
        try
        {
            bruto = await cache.GetOrCreateAsync("cep:" + digitos, async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
                using var tempo = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                return await fonte.ConsultarAsync(digitos, tempo.Token);
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            log.LogWarning(ex, "Consulta de CEP indisponível");
            return Resultado.Erro("Serviço de CEP indisponível no momento. Preencha o endereço manualmente.");
        }
        if (bruto is null) return Resultado.Erro("CEP não encontrado.");

        var cidade = await db.Cidades.AsNoTracking().Where(c => c.CodigoIbge == bruto.Ibge)
            .Select(c => new { c.IdCidades, c.IdEstados, c.Nome, Uf = c.Estado.Sigla }).FirstOrDefaultAsync();
        if (cidade is null) return Resultado.Erro("A cidade deste CEP não está cadastrada. Escolha a cidade manualmente.");

        // "Rua das Flores" → tipo RUA + "DAS FLORES" (só se o tipo existir na empresa)
        var logradouro = bruto.Logradouro?.Trim().ToUpperInvariant();
        int? tipo = null;
        if (logradouro is { Length: > 0 } && logradouro.Split(' ', 2) is [var primeira, var resto])
        {
            var tipos = await db.TiposLogradouro.AsNoTracking().Where(t => t.Ativo).Select(t => new { t.IdTiposLogradouro, t.Descricao }).ToListAsync();
            if (tipos.FirstOrDefault(t => string.Equals(t.Descricao, primeira, StringComparison.OrdinalIgnoreCase)) is { } t)
                (tipo, logradouro) = (t.IdTiposLogradouro, resto);
        }
        return Resultado.Ok(new EnderecoCepVM(Documentos.FormatarCep(digitos)!, logradouro, tipo,
            string.IsNullOrWhiteSpace(bruto.Complemento) ? null : bruto.Complemento.Trim().ToUpperInvariant(),
            string.IsNullOrWhiteSpace(bruto.Bairro) ? null : bruto.Bairro.Trim().ToUpperInvariant(),
            cidade.IdEstados, cidade.IdCidades, cidade.Nome, cidade.Uf));
    }
}
