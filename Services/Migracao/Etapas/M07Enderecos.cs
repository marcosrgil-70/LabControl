using LabControl.Infra;
using LabControl.Models;

namespace LabControl.Services.Migracao.Etapas;

/// <summary>
/// M-07 — endereços (M-07-enderecos.md). No legado o endereço apontava para LOGRADOUROS → CIDADES_BAIRROS → CIDADES;
/// no v2 logradouro e bairro são texto (T11-04 descontinuada) e a cidade vem do dicionário da M-02. Dicionário de IDs
/// mantido (endereço do laudo e da cobrança nas propostas).
/// </summary>
public sealed class M07Enderecos : IEtapaMigracao
{
    public string Codigo => "M-07";
    public string Nome => "Endereços";

    public async Task<int> ExecutarAsync(ContextoMigracao c, CancellationToken ct)
    {
        var linhas = await c.Fb.LerAsync("""
            SELECT EE.ID_ENTIDADES_ENDERECOS, EE.ID_ENTIDADES, EE.ID_ENDERECOS_TIPOS, EE.NUMERO, EE.COMPLEMENTO, EE.CEP, EE.INATIVO,
                   L.NOME_LOGRADOURO, L.ID_LOGRADOUROS_TIPOS, B.DESCRICAO AS BAIRRO, B.ID_CIDADES
              FROM ENTIDADES_ENDERECOS EE
              LEFT JOIN LOGRADOUROS L ON L.ID_LOGRADOUROS = EE.ID_LOGRADOUROS
              LEFT JOIN CIDADES_BAIRROS B ON B.ID_CIDADES_BAIRROS = L.ID_CIDADES_BAIRROS
             ORDER BY EE.ID_ENTIDADES_ENDERECOS
            """, ct);
        var novos = new List<(int Antigo, EntidadeEndereco Novo)>();
        foreach (var l in linhas)
        {
            var antigo = l.IntObrigatorio("ID_ENTIDADES_ENDERECOS");
            var onde = $"endereço {antigo}";
            if (c.Novo("ENTIDADES", l.Int("ID_ENTIDADES"), onde) is not { } entidade
                || c.Novo("ENDERECOS_TIPOS", l.Int("ID_ENDERECOS_TIPOS"), onde) is not { } tipo) continue;
            if (c.Novo("CIDADES", l.Int("ID_CIDADES"), onde) is not { } cidade) { c.Anomalia($"{onde}: sem cidade — não migrado."); continue; }
            var cep = l.Texto("CEP");
            if (cep is not null && Documentos.FormatarCep(cep) is null) c.Anomalia($"{onde}: CEP \"{cep}\" inválido — gravado sem CEP.");
            EntidadeEndereco Endereco(int idEntidade, int? legado) => new()
            {
                IdEntidades = idEntidade, IdEnderecosTipos = tipo, IdCidades = cidade,
                IdTiposLogradouro = c.Novo("LOGRADOUROS_TIPOS", l.Int("ID_LOGRADOUROS_TIPOS"), onde),
                Logradouro = c.Cortar<EntidadeEndereco>(nameof(EntidadeEndereco.Logradouro), l.Texto("NOME_LOGRADOURO")?.ToUpperInvariant() ?? "", onde)!,
                Numero = c.Cortar<EntidadeEndereco>(nameof(EntidadeEndereco.Numero), l.Texto("NUMERO")?.ToUpperInvariant(), onde),
                Complemento = c.Cortar<EntidadeEndereco>(nameof(EntidadeEndereco.Complemento), l.Texto("COMPLEMENTO")?.ToUpperInvariant(), onde),
                Bairro = c.Cortar<EntidadeEndereco>(nameof(EntidadeEndereco.Bairro), l.Texto("BAIRRO")?.ToUpperInvariant(), onde),
                Cep = Documentos.FormatarCep(cep), Inativo = l.Flag("INATIVO"), IdLegado = legado,
            };
            var e = Endereco(entidade, antigo);
            c.Db.Add(e);
            novos.Add((antigo, e));
            if (c.CopiarParaEmpresa(l.Int("ID_ENTIDADES"))) c.Db.Add(Endereco(c.EntidadeDaEmpresaUsuaria.Nova, null));
        }
        await c.Db.SaveChangesAsync(ct);
        foreach (var (antigo, e) in novos) c.Mapa("ENTIDADES_ENDERECOS")[antigo] = e.IdEntidadesEnderecos;

        c.Conferir("endereços do legado", linhas.Count, c.Mapa("ENTIDADES_ENDERECOS").Count);
        return novos.Count;
    }
}
