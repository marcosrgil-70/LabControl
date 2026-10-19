namespace LabControl.Models;

/// <summary>
/// Entidade/view com <c>ID_EMPRESAS</c>: o <c>LabDbContext</c> aplica o filtro da empresa ativa em toda consulta.
/// Marcada pelo gerador (tools/gerador-csharp); exceções em <c>SEM_FILTRO_EMPRESA</c>.
/// </summary>
public interface IFiltradoPorEmpresa;

/// <summary>
/// Tabela com <c>ID_EMPRESAS NOT NULL</c>: além do filtro, o <c>SaveChanges</c> grava a empresa ativa
/// na inclusão e recusa alterar/excluir registro de outra empresa ou trocar a empresa.
/// </summary>
public interface IPorEmpresa : IFiltradoPorEmpresa
{
    int IdEmpresas { get; set; }
}
