using System.Linq.Expressions;

namespace LabControl.Models;

public partial class Entidade
{
    /// <summary>
    /// Cadastro da T12 (clientes/fornecedores): não é a empresa usuária e tem algum tipo comercial. A lista da T12 e
    /// os contatos abertos por ela usam o mesmo critério — um profissional só (T13) não aparece nem é alterado por lá.
    /// </summary>
    public static readonly Expression<Func<Entidade, bool>> DaTelaDeClientes = e => !e.TipoEmpresaUsuaria
        && (e.TipoCliente || e.TipoFornecedor || e.TipoVendedor || e.TipoTransportador);
}
