using LabControl.Infra;
using LabControl.Models;

namespace LabControl.Services.Migracao.Etapas;

/// <summary>
/// M-01 — empresa usuária (docs/processos/migracao/M-01-empresa.md). A identidade (razão social, fantasia, CNPJ,
/// IE, IM) vem da entidade apontada por <c>EMPRESAS.ID_ENTIDADES</c> (a 4). Essa entidade vira a entidade da empresa
/// (<c>TIPO_EMPRESA_USUARIA</c>, D-19) já aqui, para o <c>SP_INICIALIZAR_EMPRESA</c> (M-04) não criar outra; se no
/// legado ela também era cliente/fornecedor, a M-05 cria o cadastro comercial à parte com o mesmo CNPJ (D-22).
/// </summary>
public sealed class M01Empresa : IEtapaMigracao
{
    public string Codigo => "M-01";
    public string Nome => "Empresa";

    public async Task<int> ExecutarAsync(ContextoMigracao c, CancellationToken ct)
    {
        var empresas = await c.Fb.LerAsync("SELECT ID_EMPRESAS, ID_ENTIDADES, COD_EMPRESAS FROM EMPRESAS", ct);
        if (empresas.Count != 1) throw new InvalidOperationException($"O legado tem {empresas.Count} empresas; a migração espera 1.");
        var legado = empresas[0];
        var idEntidade = legado.IntObrigatorio("ID_ENTIDADES");
        var ent = (await c.Fb.LerAsync($"""
            SELECT E.NOME, E.DATA_CADASTRO, PJ.CNPJ, PJ.NOME_FANTASIA, PJ.INSC_ESTADUAL, PJ.INSC_MUNICIPAL, PJ.INSC_SUFRAMA
              FROM ENTIDADES E LEFT JOIN ENTIDADES_PJ PJ ON PJ.ID_ENTIDADES = E.ID_ENTIDADES
             WHERE E.ID_ENTIDADES = {idEntidade}
            """, ct)).Single();

        var cnpj = Conversoes.Documento(ent.Texto("CNPJ"));
        var empresa = new Empresa
        {
            CodEmpresa = legado.Texto("COD_EMPRESAS") ?? "001",
            RazaoSocial = c.Cortar<Empresa>(nameof(Empresa.RazaoSocial), ent.Texto("NOME"), "empresa")!,
            NomeFantasia = c.Cortar<Empresa>(nameof(Empresa.NomeFantasia), ent.Texto("NOME_FANTASIA"), "empresa"),
            Cnpj = cnpj,
            InscEstadual = c.Cortar<Empresa>(nameof(Empresa.InscEstadual), ent.Texto("INSC_ESTADUAL"), "empresa"),
            InscMunicipal = c.Cortar<Empresa>(nameof(Empresa.InscMunicipal), ent.Texto("INSC_MUNICIPAL"), "empresa"),
            Ativa = true,
        };
        c.Db.Add(empresa);
        await c.Db.SaveChangesAsync(ct);
        c.Empresa.EmpresaId = empresa.IdEmpresas; // daqui em diante tudo é gravado nesta empresa
        c.Mapa("EMPRESAS")[legado.IntObrigatorio("ID_EMPRESAS")] = empresa.IdEmpresas;

        var entidade = new Entidade
        {
            Categoria = Categoria.Juridica, Nome = empresa.RazaoSocial, NomeFantasia = empresa.NomeFantasia, CpfCnpj = cnpj,
            RgIe = empresa.InscEstadual, InscMunicipal = empresa.InscMunicipal,
            InscSuframa = c.Cortar<Entidade>(nameof(Entidade.InscSuframa), ent.Texto("INSC_SUFRAMA"), "empresa"),
            TipoEmpresaUsuaria = true, DataCadastro = ent.Data("DATA_CADASTRO") ?? DateOnly.FromDateTime(DateTime.Today),
        };
        c.Db.Add(entidade);
        await c.Db.SaveChangesAsync(ct);
        empresa.IdEntidades = entidade.IdEntidades; // TG_EMPRESAS_AU espelha a identidade
        await c.Db.SaveChangesAsync(ct);
        c.Mapa(ContextoMigracao.EntidadeDaEmpresa)[idEntidade] = entidade.IdEntidades;

        c.Conferir("1 empresa", 1, c.Db.Empresas.Count());
        c.Conferir("CNPJ da empresa válido", true, Documentos.CnpjValido(cnpj));
        c.Log($"Empresa {empresa.CodEmpresa} — {empresa.RazaoSocial} (entidade {idEntidade} do legado).");
        return 2;
    }
}
