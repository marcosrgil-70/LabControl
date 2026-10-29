using System.Linq.Expressions;

namespace LabControl.Models;

public partial class Entidade
{
    /// <summary>Texto livre: nome, fantasia ou CPF/CNPJ (com ou sem pontuação). A busca da T12, dos pedidos e dos relatórios.</summary>
    public static Expression<Func<Entidade, bool>> Busca(string texto)
    {
        var t = texto.Trim();
        var digitos = LabControl.Infra.Documentos.SoDigitos(t);
        return e => e.Nome.Contains(t) || (e.NomeFantasia != null && e.NomeFantasia.Contains(t))
                    || (e.CpfCnpj != null && (e.CpfCnpj.Contains(t) || (digitos.Length >= 3 &&
                        e.CpfCnpj.Replace(".", "").Replace("/", "").Replace("-", "").Contains(digitos))));
    }

    /// <summary>
    /// Cadastro da T12 (clientes/fornecedores): não é a empresa usuária e tem algum tipo comercial. A lista da T12 e
    /// os contatos abertos por ela usam o mesmo critério — um profissional só (T13) não aparece nem é alterado por lá.
    /// </summary>
    public static readonly Expression<Func<Entidade, bool>> DaTelaDeClientes = e => !e.TipoEmpresaUsuaria
        && (e.TipoCliente || e.TipoFornecedor || e.TipoVendedor || e.TipoTransportador);
}
