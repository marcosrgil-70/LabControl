using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services.Entidades;

/// <summary>
/// T12 — Clientes / Fornecedores (docs/telas/T12-clientes.md, P25): dados básicos PF/PJ e observações de
/// <c>ENTIDADES</c>; endereços/telefones/e-mails pelo <see cref="ContatosService"/>. A entidade da empresa usuária
/// nunca aparece aqui; funcionário é mantido na T13 (aqui só é mostrado). Excluir só sem movimento (senão inativar).
/// </summary>
public sealed class ClienteService(LabDbContext db)
{
    public const string MensagemNaoEncontrado = "Cadastro não encontrado (pode ter sido excluído).";

    /// <summary>Entidades da tela: não é a empresa usuária e tem algum tipo comercial.</summary>
    private IQueryable<Entidade> DaTela() => db.Entidades.Where(Entidade.DaTelaDeClientes);

    public async Task<ClienteListaVM> ListarAsync(ClienteFiltroVM f)
    {
        var q = DaTela().AsNoTracking();
        q = f.Tipo switch
        {
            "cliente" => q.Where(e => e.TipoCliente),
            "fornecedor" => q.Where(e => e.TipoFornecedor),
            "comercial" => q.Where(e => e.TipoVendedor),
            "transportador" => q.Where(e => e.TipoTransportador),
            _ => q,
        };
        if (!string.IsNullOrWhiteSpace(f.Texto))
        {
            var t = f.Texto.Trim();
            var digitos = Documentos.SoDigitos(t);
            q = q.Where(e => e.Nome.Contains(t) || (e.NomeFantasia != null && e.NomeFantasia.Contains(t))
                             || (e.CpfCnpj != null && (e.CpfCnpj.Contains(t) || (digitos.Length >= 3 &&
                                 e.CpfCnpj.Replace(".", "").Replace("/", "").Replace("-", "").Contains(digitos)))));
        }
        // inativos que a pesquisa acharia: a tela avisa (senão o cadastro "some" depois de inativado)
        var ocultos = f.Inativos ? 0 : await q.CountAsync(e => e.Inativo);
        if (!f.Inativos) q = q.Where(e => !e.Inativo);
        var total = await q.CountAsync();
        f.Pagina = Paginacao.Ajustar(f.Pagina, total, ClienteFiltroVM.TamanhoPagina);
        var itens = await q.OrderBy(e => e.Nome).Skip((f.Pagina - 1) * ClienteFiltroVM.TamanhoPagina).Take(ClienteFiltroVM.TamanhoPagina)
            .Select(e => new
            {
                e.IdEntidades, e.Categoria, e.DataCadastro, e.Nome, e.NomeFantasia, e.CpfCnpj, e.Inativo,
                e.TipoCliente, e.TipoFornecedor, e.TipoVendedor, e.TipoTransportador, e.TipoFuncionario,
                Fone = db.EntidadesFones.Where(x => x.IdEntidades == e.IdEntidades && !x.Inativo)
                    .OrderByDescending(x => x.Principal).ThenBy(x => x.IdEntidadesFones).Select(x => "(" + x.Ddd + ") " + x.Fone).FirstOrDefault(),
                Email = db.EntidadesEmails.Where(x => x.IdEntidades == e.IdEntidades && !x.Inativo)
                    .OrderByDescending(x => x.Principal).ThenBy(x => x.IdEntidadesEmails).Select(x => x.Email).FirstOrDefault(),
            }).ToListAsync();
        return new ClienteListaVM
        {
            Filtro = f, Total = total, InativosOcultos = ocultos,
            Itens = itens.Select(e => new ClienteListaItemVM(e.IdEntidades, e.Categoria, e.DataCadastro, e.Nome, e.NomeFantasia, e.CpfCnpj,
                e.Fone, e.Email, Tipos(e.TipoCliente, e.TipoFornecedor, e.TipoVendedor, e.TipoTransportador, e.TipoFuncionario), e.Inativo)).ToList(),
        };
    }

    public async Task<ClienteVM?> CarregarAsync(int id)
    {
        var e = await DaTela().AsNoTracking().SingleOrDefaultAsync(x => x.IdEntidades == id);
        return e is null ? null : new ClienteVM
        {
            Id = e.IdEntidades, DataCadastro = e.DataCadastro, Categoria = e.Categoria, Nome = e.Nome, NomeFantasia = e.NomeFantasia,
            CpfCnpj = e.CpfCnpj, RgIe = e.RgIe, InscMunicipal = e.InscMunicipal, InscSuframa = e.InscSuframa, Sexo = e.Sexo,
            DtNascimento = e.DtNascimento, EstadoCivil = e.EstadoCivil, TipoCliente = e.TipoCliente, TipoFornecedor = e.TipoFornecedor,
            TipoVendedor = e.TipoVendedor, TipoTransportador = e.TipoTransportador, TipoFuncionario = e.TipoFuncionario,
            Avaliacao = e.Avaliacao, Observacao = e.Observacao, ObsIncluirFaturamento = e.ObsIncluirFaturamento, Inativo = e.Inativo,
        };
    }

    /// <summary>Inclui (Id = 0) ou altera os dados básicos. Devolve o ID.</summary>
    public async Task<Resultado<int>> GravarAsync(ClienteVM vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Nome)) return Resultado.Erro("Informe o nome / razão social.", nameof(vm.Nome));
        if (!(vm.TipoCliente || vm.TipoFornecedor || vm.TipoVendedor || vm.TipoTransportador))
            return Resultado.Erro("Marque ao menos um tipo de cadastro (cliente, fornecedor, comercial ou transportador).", nameof(vm.TipoCliente));

        string? doc = null;
        if (!string.IsNullOrWhiteSpace(vm.CpfCnpj))
        {
            var fisica = vm.Categoria == Categoria.Fisica;
            if (fisica ? !Documentos.CpfValido(vm.CpfCnpj) : !Documentos.CnpjValido(vm.CpfCnpj))
                return Resultado.Erro(fisica ? "CPF inválido." : "CNPJ inválido.", nameof(vm.CpfCnpj));
            doc = fisica ? Documentos.FormatarCpf(vm.CpfCnpj) : Documentos.FormatarCnpj(vm.CpfCnpj);
        }
        if (vm.DtNascimento is { } nasc && nasc > DateOnly.FromDateTime(DateTime.Today))
            return Resultado.Erro("Data de nascimento no futuro.", nameof(vm.DtNascimento));

        Entidade? e;
        if (vm.Id == 0) db.Add(e = new Entidade());
        else if ((e = await DaTela().SingleOrDefaultAsync(x => x.IdEntidades == vm.Id)) is null)
            return Resultado.Erro(MensagemNaoEncontrado);

        var pf = vm.Categoria == Categoria.Fisica;
        e.Categoria = vm.Categoria;
        e.Nome = vm.Nome.Trim().ToUpperInvariant();
        e.NomeFantasia = Textos.Maiusculas(vm.NomeFantasia);
        e.CpfCnpj = doc;
        e.RgIe = Textos.Maiusculas(vm.RgIe);
        // campos só de PF / só de PJ (trocar a categoria limpa os da outra)
        e.InscMunicipal = pf ? null : Textos.Limpo(vm.InscMunicipal);
        e.InscSuframa = pf ? null : Textos.Limpo(vm.InscSuframa);
        e.Sexo = pf ? vm.Sexo : null;
        e.DtNascimento = pf ? vm.DtNascimento : null;
        e.EstadoCivil = pf ? vm.EstadoCivil : null;
        e.TipoCliente = vm.TipoCliente;
        e.TipoFornecedor = vm.TipoFornecedor;
        e.TipoVendedor = vm.TipoVendedor;
        e.TipoTransportador = vm.TipoTransportador;
        e.Avaliacao = Textos.Maiusculas(vm.Avaliacao);
        e.Observacao = Textos.Limpo(vm.Observacao);
        e.ObsIncluirFaturamento = vm.ObsIncluirFaturamento;
        e.Inativo = vm.Inativo;

        var r = await ErrosMySql.CapturarAsync(() => (Task)db.SaveChangesAsync());
        if (!r.Sucesso)
        {
            db.ChangeTracker.Clear();
            return Resultado.Erro(r.Mensagem!, r.Tipo == TipoFalha.Duplicado ? nameof(vm.CpfCnpj) : null, r.Tipo);
        }
        return Resultado.Ok(e.IdEntidades);
    }

    /// <summary>Exclui só sem movimento: proposta/amostra apontando para a entidade → o banco recusa e a tela oferece inativar.</summary>
    public async Task<Resultado> ExcluirAsync(int id)
    {
        if (!await DaTela().AnyAsync(e => e.IdEntidades == id)) return Resultado.Erro(MensagemNaoEncontrado);
        if (await db.Entidades.AnyAsync(e => e.IdEntidades == id && e.TipoFuncionario))
            return Resultado.Erro("Este cadastro também é profissional: inative-o em vez de excluir.");
        var r = await ErrosMySql.CapturarAsync(() => DaTela().Where(e => e.IdEntidades == id).ExecuteDeleteAsync());
        return r.Sucesso ? Resultado.Ok() : r.Falha;
    }

    public async Task<Resultado> DefinirInativoAsync(int id, bool inativo)
    {
        var n = await DaTela().Where(e => e.IdEntidades == id).ExecuteUpdateAsync(s => s.SetProperty(e => e.Inativo, inativo));
        return n == 0 ? Resultado.Erro(MensagemNaoEncontrado) : Resultado.Ok();
    }

    private static string Tipos(bool cliente, bool fornecedor, bool comercial, bool transportador, bool funcionario) =>
        string.Join(", ", new[] { (cliente, "Cliente"), (fornecedor, "Fornecedor"), (comercial, "Comercial"),
            (transportador, "Transportador"), (funcionario, "Profissional") }.Where(t => t.Item1).Select(t => t.Item2));

}
