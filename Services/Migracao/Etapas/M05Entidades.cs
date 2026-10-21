using LabControl.Models;

namespace LabControl.Services.Migracao.Etapas;

/// <summary>
/// M-05 — clientes, fornecedores, comerciais, transportadores e funcionários (M-05-entidades.md): ENTIDADES + PF +
/// PJ + observações → ENTIDADES (<c>ID_LEGADO</c>). A entidade da empresa (M-01) que no legado também era cliente/
/// fornecedor ganha aqui o cadastro comercial próprio, com o mesmo CNPJ (D-22) — é ele que as propostas referenciam.
/// </summary>
public sealed class M05Entidades : IEtapaMigracao
{
    public string Codigo => "M-05";
    public string Nome => "Entidades";

    public async Task<int> ExecutarAsync(ContextoMigracao c, CancellationToken ct)
    {
        var linhas = await c.Fb.LerAsync("""
            SELECT E.ID_ENTIDADES, E.CATEGORIA, E.DATA_CADASTRO, E.NOME, E.INATIVO, E.AVALIACAO,
                   E.TIPO_CLIENTE, E.TIPO_FORNECEDOR, E.TIPO_VENDEDOR, E.TIPO_TRANSPORTADOR, E.TIPO_FUNCIONARIO,
                   PF.ID_ENTIDADES AS TEM_PF, PF.CPF, PF.SOBRENOME, PF.SEXO, PF.DATA_NASCIMENTO, PF.RG, PF.ESTADO_CIVIL,
                   PJ.ID_ENTIDADES AS TEM_PJ, PJ.CNPJ, PJ.NOME_FANTASIA, PJ.INSC_ESTADUAL, PJ.INSC_MUNICIPAL, PJ.INSC_SUFRAMA,
                   O.INCLUIR_FATURAMENTO, O.OBSERVACAO
              FROM ENTIDADES E
              LEFT JOIN ENTIDADES_PF PF ON PF.ID_ENTIDADES = E.ID_ENTIDADES
              LEFT JOIN ENTIDADES_PJ PJ ON PJ.ID_ENTIDADES = E.ID_ENTIDADES
              LEFT JOIN ENTIDADES_OBSERVACOES O ON O.ID_ENTIDADES = E.ID_ENTIDADES
             ORDER BY E.ID_ENTIDADES
            """, ct);
        var daEmpresa = c.Mapa(ContextoMigracao.EntidadeDaEmpresa);
        var documentos = new HashSet<string>();
        var novas = new List<(int Antigo, Entidade Nova)>();

        foreach (var l in linhas)
        {
            var antigo = l.IntObrigatorio("ID_ENTIDADES");
            var onde = $"entidade {antigo}";
            bool cliente = l.Flag("TIPO_CLIENTE"), fornecedor = l.Flag("TIPO_FORNECEDOR"), comercial = l.Flag("TIPO_VENDEDOR"),
                transportador = l.Flag("TIPO_TRANSPORTADOR"), funcionario = l.Flag("TIPO_FUNCIONARIO");
            if (daEmpresa.TryGetValue(antigo, out var idDaEmpresa))
            {
                if (!(cliente || fornecedor || comercial || transportador || funcionario))
                { c.Mapa("ENTIDADES")[antigo] = idDaEmpresa; continue; } // só empresa usuária: os contatos vão para ela
                c.Anomalia($"Entidade {antigo} (a própria empresa) também é cliente/fornecedor no legado: cadastro comercial à parte com o mesmo CNPJ (D-22).");
            }

            var pf = l.Texto("CATEGORIA") == "F";
            if (pf && l.Int("TEM_PF") is null) c.Anomalia($"{onde} \"{l.Texto("NOME")}\": pessoa física sem os dados de PF no legado (sem CPF).");
            if (!pf && l.Int("TEM_PJ") is null) c.Anomalia($"{onde} \"{l.Texto("NOME")}\": pessoa jurídica sem os dados de PJ no legado (sem CNPJ).");
            if (pf && l.Int("TEM_PJ") is not null) c.Anomalia($"{onde} \"{l.Texto("NOME")}\": tem PF e PJ no legado — migrada como pessoa física.");

            var documento = Conversoes.Documento(pf ? l.Texto("CPF") : l.Texto("CNPJ"));
            if (documento is not null && documento.Length is not (14 or 18)) c.Anomalia($"{onde}: documento \"{documento}\" sem 11/14 dígitos, gravado como está.");
            if (documento is not null && !documentos.Add(documento))
            {
                c.Anomalia($"{onde}: CPF/CNPJ {documento} repetido no legado — gravado sem documento.");
                documento = null;
            }

            var e = new Entidade
            {
                Categoria = pf ? Categoria.Fisica : Categoria.Juridica,
                Nome = c.Cortar<Entidade>(nameof(Entidade.Nome), l.Texto("NOME") ?? "SEM NOME", onde)!,
                NomeFantasia = c.Cortar<Entidade>(nameof(Entidade.NomeFantasia), pf ? l.Texto("SOBRENOME") : l.Texto("NOME_FANTASIA"), onde),
                CpfCnpj = documento,
                RgIe = c.Cortar<Entidade>(nameof(Entidade.RgIe), pf ? l.Texto("RG") : l.Texto("INSC_ESTADUAL"), onde),
                InscMunicipal = pf ? null : c.Cortar<Entidade>(nameof(Entidade.InscMunicipal), l.Texto("INSC_MUNICIPAL"), onde),
                InscSuframa = pf ? null : c.Cortar<Entidade>(nameof(Entidade.InscSuframa), l.Texto("INSC_SUFRAMA"), onde),
                Sexo = pf ? Conversoes.Sexo(l.Texto("SEXO")) : null,
                DtNascimento = pf ? l.Data("DATA_NASCIMENTO") : null,
                EstadoCivil = pf ? Conversoes.EstadoCivil(l.Int("ESTADO_CIVIL")) : null,
                TipoCliente = cliente, TipoFornecedor = fornecedor, TipoVendedor = comercial, TipoTransportador = transportador, TipoFuncionario = funcionario,
                Avaliacao = c.Cortar<Entidade>(nameof(Entidade.Avaliacao), l.Texto("AVALIACAO"), onde),
                Observacao = l.Texto("OBSERVACAO"), ObsIncluirFaturamento = l.Flag("INCLUIR_FATURAMENTO"),
                DataCadastro = l.Data("DATA_CADASTRO") ?? DateOnly.FromDateTime(DateTime.Today),
                Inativo = l.Flag("INATIVO"), IdLegado = antigo,
            };
            c.Db.Add(e);
            novas.Add((antigo, e));
        }
        await c.Db.SaveChangesAsync(ct);
        foreach (var (antigo, nova) in novas) c.Mapa("ENTIDADES")[antigo] = nova.IdEntidades;

        c.Conferir("entidades do legado", linhas.Count, c.Mapa("ENTIDADES").Count);
        c.Conferir("uma só entidade da empresa usuária", 1, c.Db.Entidades.Count(x => x.TipoEmpresaUsuaria));
        return novas.Count;
    }
}
