using System.ComponentModel.DataAnnotations;
using LabControl.Data;
using LabControl.Infra;
using LabControl.Models;
using LabControl.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabControl.Services.Entidades;

/// <summary>De qual cadastro o contato está sendo mexido (define a entidade aceita e a permissão da tela).</summary>
public enum CadastroDono { Empresa, Cliente, Profissional }

/// <summary>Contato a excluir.</summary>
public enum TipoContato { Endereco, Fone, Email }

/// <summary>
/// Endereços, telefones e e-mails das entidades (docs/processos/P25-cadastro-entidades-contatos.md): o mesmo serviço
/// para a empresa usuária (T03), clientes/fornecedores (T12) e profissionais (T13), como o legado (un_Entidades).
/// Regras de dados no banco (um endereço ativo por tipo, um telefone e um e-mail principal, empresa do pai);
/// aqui: a entidade é da empresa ativa e do cadastro certo, validações de formato e "principal" que troca sozinho.
/// </summary>
public sealed class ContatosService(LabDbContext db)
{
    public const string MensagemNaoEncontrado = "Cadastro não encontrado.";

    /// <summary>A entidade é da empresa ativa (filtro global) e pertence ao cadastro que está mexendo?</summary>
    public async Task<bool> PertenceAsync(CadastroDono dono, int idEntidade) => dono switch
    {
        CadastroDono.Empresa => await db.Entidades.AnyAsync(e => e.IdEntidades == idEntidade && e.TipoEmpresaUsuaria),
        CadastroDono.Profissional => await db.Entidades.AnyAsync(e => e.IdEntidades == idEntidade && e.TipoFuncionario),
        _ => await db.Entidades.AnyAsync(e => e.IdEntidades == idEntidade && !e.TipoEmpresaUsuaria),
    };

    public async Task<ContatosVM?> CarregarAsync(CadastroDono dono, int idEntidade, string urlBase, bool podeAlterar)
    {
        if (!await PertenceAsync(dono, idEntidade)) return null;

        var enderecos = await db.EntidadesEnderecos.AsNoTracking().Where(e => e.IdEntidades == idEntidade)
            .OrderBy(e => e.Inativo).ThenBy(e => e.EnderecoTipo.Descricao)
            .Select(e => new
            {
                e.IdEntidadesEnderecos, e.IdEnderecosTipos, Tipo = e.EnderecoTipo.Descricao, e.IdTiposLogradouro,
                Abrev = e.TipoLogradouro != null ? e.TipoLogradouro.Abreviatura : null, e.Logradouro, e.Numero, e.Complemento,
                e.Bairro, e.Cep, e.Cidade.IdEstados, e.IdCidades, Cidade = e.Cidade.Nome, Uf = e.Cidade.Estado.Sigla, e.Inativo,
            }).ToListAsync();
        var fones = await db.EntidadesFones.AsNoTracking().Where(f => f.IdEntidades == idEntidade)
            .OrderBy(f => f.Inativo).ThenByDescending(f => f.Principal).ThenBy(f => f.IdEntidadesFones)
            .Select(f => new FoneLinhaVM(f.IdEntidadesFones, f.IdFonesTipos, f.FoneTipo.Descricao, f.Ddd, f.Fone, f.Contato,
                f.Departamento, f.Principal, f.Inativo)).ToListAsync();
        var emails = await db.EntidadesEmails.AsNoTracking().Where(m => m.IdEntidades == idEntidade)
            .OrderBy(m => m.Inativo).ThenByDescending(m => m.Principal).ThenBy(m => m.Email)
            .Select(m => new EmailLinhaVM(m.IdEntidadesEmails, m.IdEmailsTipos, m.EmailTipo != null ? m.EmailTipo.Descricao : null,
                m.Email, m.NomeContato, m.Departamento, m.Principal, m.EnviarNfe, m.EnviarLaudo, m.Inativo)).ToListAsync();

        // combos: ativos + os já usados nesta entidade (inativos continuam aparecendo nos registros antigos)
        var usadosEnd = enderecos.Select(e => e.IdEnderecosTipos).ToList();
        var usadosLog = enderecos.Where(e => e.IdTiposLogradouro != null).Select(e => e.IdTiposLogradouro!.Value).ToList();
        var usadosFone = fones.Select(f => f.IdTipo).ToList();
        var usadosEmail = emails.Where(m => m.IdTipo != null).Select(m => m.IdTipo!.Value).ToList();

        return new ContatosVM
        {
            Dono = dono, IdEntidades = idEntidade, UrlBase = urlBase, PodeAlterar = podeAlterar,
            Enderecos = enderecos.Select(e => new EnderecoLinhaVM(e.IdEntidadesEnderecos, e.IdEnderecosTipos, e.Tipo, e.IdTiposLogradouro,
                e.Logradouro, e.Numero, e.Complemento, e.Bairro, e.Cep, e.IdEstados, e.IdCidades, e.Cidade, e.Uf, e.Inativo,
                string.Join(", ", new[] { string.Join(" ", new[] { e.Abrev, e.Logradouro }.Where(s => !string.IsNullOrEmpty(s))), e.Numero, e.Complemento }
                    .Where(s => !string.IsNullOrEmpty(s))),
                string.Join(" - ", new[] { e.Bairro, $"{e.Cidade}/{e.Uf}", e.Cep }.Where(s => !string.IsNullOrEmpty(s))))).ToList(),
            Fones = fones,
            Emails = emails,
            TiposEndereco = await db.EnderecosTipos.AsNoTracking().Where(t => t.Ativo || usadosEnd.Contains(t.IdEnderecosTipos))
                .OrderBy(t => t.Descricao).Select(t => new OpcaoVM(t.IdEnderecosTipos, t.Descricao)).ToListAsync(),
            TiposLogradouro = await db.TiposLogradouro.AsNoTracking().Where(t => t.Ativo || usadosLog.Contains(t.IdTiposLogradouro))
                .OrderBy(t => t.Descricao).Select(t => new OpcaoVM(t.IdTiposLogradouro, t.Descricao)).ToListAsync(),
            TiposFone = await db.FonesTipos.AsNoTracking().Where(t => t.Ativo || usadosFone.Contains(t.IdFonesTipos))
                .OrderBy(t => t.Descricao).Select(t => new OpcaoVM(t.IdFonesTipos, t.Descricao)).ToListAsync(),
            TiposEmail = await db.EmailsTipos.AsNoTracking().Where(t => t.Ativo || usadosEmail.Contains(t.IdEmailsTipos))
                .OrderBy(t => t.Descricao).Select(t => new OpcaoVM(t.IdEmailsTipos, t.Descricao)).ToListAsync(),
            Estados = await db.Estados.AsNoTracking().OrderBy(e => e.Sigla).Select(e => new OpcaoVM(e.IdEstados, e.Sigla)).ToListAsync(),
        };
    }

    // ---------------------------------------------------------------- endereços

    public async Task<Resultado<int>> GravarEnderecoAsync(CadastroDono dono, int idEntidade, EnderecoVM vm)
    {
        if (!await PertenceAsync(dono, idEntidade)) return Resultado.Erro(MensagemNaoEncontrado);
        if (vm.IdEnderecosTipos is not { } tipo || !await db.EnderecosTipos.AnyAsync(t => t.IdEnderecosTipos == tipo))
            return Resultado.Erro("Informe o tipo de endereço.", nameof(vm.IdEnderecosTipos));
        if (vm.IdTiposLogradouro is { } tl && !await db.TiposLogradouro.AnyAsync(t => t.IdTiposLogradouro == tl))
            return Resultado.Erro("Tipo de logradouro não encontrado.", nameof(vm.IdTiposLogradouro));
        if (string.IsNullOrWhiteSpace(vm.Logradouro)) return Resultado.Erro("Informe o logradouro.", nameof(vm.Logradouro));
        if (vm.IdCidades is not { } cidade || !await db.Cidades.AnyAsync(c => c.IdCidades == cidade))
            return Resultado.Erro("Informe a cidade.", nameof(vm.IdCidades));
        string? cep = null;
        if (!string.IsNullOrWhiteSpace(vm.Cep) && (cep = Documentos.FormatarCep(vm.Cep)) is null)
            return Resultado.Erro("CEP inválido (8 dígitos).", nameof(vm.Cep));
        if (Longo((vm.Logradouro, 150, "Logradouro", nameof(vm.Logradouro)), (vm.Numero, 20, "Número", nameof(vm.Numero)),
                  (vm.Complemento, 50, "Complemento", nameof(vm.Complemento)), (vm.Bairro, 80, "Bairro", nameof(vm.Bairro))) is { } longo1)
            return longo1;

        EntidadeEndereco? e;
        if (vm.Id == 0) db.Add(e = new EntidadeEndereco { IdEntidades = idEntidade });
        else if ((e = await db.EntidadesEnderecos.SingleOrDefaultAsync(x => x.IdEntidadesEnderecos == vm.Id && x.IdEntidades == idEntidade)) is null)
            return Resultado.Erro("Endereço não encontrado (pode ter sido excluído).");
        e.IdEnderecosTipos = tipo;
        e.IdTiposLogradouro = vm.IdTiposLogradouro;
        e.Logradouro = Texto(vm.Logradouro)!.ToUpperInvariant();
        e.Numero = Texto(vm.Numero)?.ToUpperInvariant();
        e.Complemento = Texto(vm.Complemento)?.ToUpperInvariant();
        e.Bairro = Texto(vm.Bairro)?.ToUpperInvariant();
        e.Cep = cep;
        e.IdCidades = cidade;
        e.Inativo = vm.Inativo;
        return await GravarAsync(() => e.IdEntidadesEnderecos);
    }

    // ---------------------------------------------------------------- telefones

    public async Task<Resultado<int>> GravarFoneAsync(CadastroDono dono, int idEntidade, FoneVM vm)
    {
        if (!await PertenceAsync(dono, idEntidade)) return Resultado.Erro(MensagemNaoEncontrado);
        if (vm.IdFonesTipos is not { } tipo || !await db.FonesTipos.AnyAsync(t => t.IdFonesTipos == tipo))
            return Resultado.Erro("Informe o tipo de telefone.", nameof(vm.IdFonesTipos));
        var ddd = Documentos.SoDigitos(vm.Ddd);
        if (ddd.Length is < 2 or > 3) return Resultado.Erro("DDD com 2 ou 3 dígitos.", nameof(vm.Ddd));
        var fone = Documentos.SoDigitos(vm.Fone);
        if (fone.Length is < 8 or > 9) return Resultado.Erro("Telefone com 8 ou 9 dígitos (sem o DDD).", nameof(vm.Fone));
        if (Longo((vm.Contato, 100, "Contato", nameof(vm.Contato)), (vm.Departamento, 100, "Departamento", nameof(vm.Departamento))) is { } longo2)
            return longo2;

        await using var tx = await db.Database.BeginTransactionAsync();
        EntidadeFone? f;
        if (vm.Id == 0) db.Add(f = new EntidadeFone { IdEntidades = idEntidade });
        else if ((f = await db.EntidadesFones.SingleOrDefaultAsync(x => x.IdEntidadesFones == vm.Id && x.IdEntidades == idEntidade)) is null)
            return Resultado.Erro("Telefone não encontrado (pode ter sido excluído).");
        f.IdFonesTipos = tipo;
        f.Ddd = ddd;
        f.Fone = fone.Length == 9 ? $"{fone[..5]}-{fone[5..]}" : $"{fone[..4]}-{fone[4..]}";
        f.Contato = Texto(vm.Contato)?.ToUpperInvariant();
        f.Departamento = Texto(vm.Departamento)?.ToUpperInvariant();
        f.Inativo = vm.Inativo;
        f.Principal = vm.Principal && !vm.Inativo;
        // o novo principal tira a marca do anterior (o banco recusa dois)
        if (f.Principal)
            await db.EntidadesFones.Where(x => x.IdEntidades == idEntidade && x.Principal && x.IdEntidadesFones != vm.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Principal, false));
        var r = await GravarAsync(() => f.IdEntidadesFones);
        if (r.Sucesso) await tx.CommitAsync();
        return r;
    }

    // ---------------------------------------------------------------- e-mails

    public async Task<Resultado<int>> GravarEmailAsync(CadastroDono dono, int idEntidade, EmailVM vm)
    {
        if (!await PertenceAsync(dono, idEntidade)) return Resultado.Erro(MensagemNaoEncontrado);
        var email = Texto(vm.Email)?.ToLowerInvariant();
        if (email is null || email.Length > 150 || !new EmailAddressAttribute().IsValid(email) || !email.Contains('.'))
            return Resultado.Erro("E-mail inválido.", nameof(vm.Email));
        if (vm.IdEmailsTipos is { } tipo && !await db.EmailsTipos.AnyAsync(t => t.IdEmailsTipos == tipo))
            return Resultado.Erro("Tipo de e-mail não encontrado.", nameof(vm.IdEmailsTipos));
        if (Longo((vm.NomeContato, 100, "Nome do contato", nameof(vm.NomeContato)), (vm.Departamento, 100, "Departamento", nameof(vm.Departamento))) is { } longo3)
            return longo3;

        await using var tx = await db.Database.BeginTransactionAsync();
        EntidadeEmail? m;
        if (vm.Id == 0) db.Add(m = new EntidadeEmail { IdEntidades = idEntidade });
        else if ((m = await db.EntidadesEmails.SingleOrDefaultAsync(x => x.IdEntidadesEmails == vm.Id && x.IdEntidades == idEntidade)) is null)
            return Resultado.Erro("E-mail não encontrado (pode ter sido excluído).");
        m.IdEmailsTipos = vm.IdEmailsTipos;
        m.Email = email;
        m.NomeContato = Texto(vm.NomeContato)?.ToUpperInvariant();
        m.Departamento = Texto(vm.Departamento)?.ToUpperInvariant();
        m.EnviarNfe = vm.EnviarNfe;
        m.EnviarLaudo = vm.EnviarLaudo;
        m.Inativo = vm.Inativo;
        m.Principal = vm.Principal && !vm.Inativo;
        if (m.Principal)
            await db.EntidadesEmails.Where(x => x.IdEntidades == idEntidade && x.Principal && x.IdEntidadesEmails != vm.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Principal, false));
        var r = await GravarAsync(() => m.IdEntidadesEmails);
        if (r.Sucesso) await tx.CommitAsync();
        return r;
    }

    // ---------------------------------------------------------------- exclusão

    /// <summary>Exclui o contato; se estiver em uso (proposta aponta para o endereço/e-mail), o banco recusa e a mensagem sugere inativar.</summary>
    public async Task<Resultado> ExcluirAsync(CadastroDono dono, int idEntidade, TipoContato tipo, int id)
    {
        if (!await PertenceAsync(dono, idEntidade)) return Resultado.Erro(MensagemNaoEncontrado);
        var r = await ErrosMySql.CapturarAsync(() => tipo switch
        {
            TipoContato.Endereco => db.EntidadesEnderecos.Where(x => x.IdEntidadesEnderecos == id && x.IdEntidades == idEntidade).ExecuteDeleteAsync(),
            TipoContato.Fone => db.EntidadesFones.Where(x => x.IdEntidadesFones == id && x.IdEntidades == idEntidade).ExecuteDeleteAsync(),
            _ => db.EntidadesEmails.Where(x => x.IdEntidadesEmails == id && x.IdEntidades == idEntidade).ExecuteDeleteAsync(),
        });
        if (!r.Sucesso) return Resultado.Erro(r.Mensagem!);
        return r.Valor == 0 ? Resultado.Erro("Contato não encontrado (pode ter sido excluído).") : Resultado.Ok();
    }

    // ----------------------------------------------------------------

    private async Task<Resultado<int>> GravarAsync(Func<int> id)
    {
        var r = await ErrosMySql.CapturarAsync(() => (Task)db.SaveChangesAsync());
        if (!r.Sucesso)
        {
            db.ChangeTracker.Clear();
            return Resultado.Erro(r.Mensagem!);
        }
        return Resultado.Ok(id());
    }

    private static string? Texto(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>Primeiro texto acima do tamanho da coluna, como erro no campo (null = todos cabem).</summary>
    private static Falha? Longo(params (string? Valor, int Maximo, string Rotulo, string Campo)[] campos) =>
        campos.Where(c => c.Valor is not null && c.Valor.Trim().Length > c.Maximo)
            .Select(c => (Falha?)new Falha($"{c.Rotulo}: no máximo {c.Maximo} caracteres.", c.Campo)).FirstOrDefault();
}
