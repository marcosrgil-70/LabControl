using LabControl.Models;

namespace LabControl.Services.Migracao.Etapas;

/// <summary>
/// M-06 — telefones e e-mails (M-06-contatos.md), com <c>ID_LEGADO</c> e dicionário (os e-mails são os contatos
/// das propostas na parte 2). <c>ENVIAR_LAUDO</c> = e-mail usado como contato do resultado em alguma proposta.
/// Os contatos da própria empresa (entidade 4) vão também para a entidade da empresa usuária (cabeçalho do boletim).
/// </summary>
public sealed class M06Contatos : IEtapaMigracao
{
    public string Codigo => "M-06";
    public string Nome => "Contatos (telefones e e-mails)";

    public async Task<int> ExecutarAsync(ContextoMigracao c, CancellationToken ct)
    {
        var fones = await c.Fb.LerAsync("""
            SELECT ID_ENTIDADES_FONES, ID_ENTIDADES, ID_FONES_TIPOS, DDD, FONE, PRINCIPAL, CONTATO, DEPARTAMENTO, INATIVO
              FROM ENTIDADES_FONES ORDER BY ID_ENTIDADES_FONES
            """, ct);
        var novosFones = new List<(int Antigo, EntidadeFone Novo)>();
        foreach (var l in fones)
        {
            var antigo = l.IntObrigatorio("ID_ENTIDADES_FONES");
            var onde = $"telefone {antigo}";
            if (c.Novo("ENTIDADES", l.Int("ID_ENTIDADES"), onde) is not { } entidade || c.Novo("FONES_TIPOS", l.Int("ID_FONES_TIPOS"), onde) is not { } tipo) continue;
            EntidadeFone Fone(int idEntidade, int? legado) => new()
            {
                IdEntidades = idEntidade, IdFonesTipos = tipo,
                Ddd = c.Cortar<EntidadeFone>(nameof(EntidadeFone.Ddd), l.Texto("DDD") ?? "", onde)!,
                Fone = c.Cortar<EntidadeFone>(nameof(EntidadeFone.Fone), l.Texto("FONE") ?? "", onde)!,
                Principal = l.Flag("PRINCIPAL"),
                Contato = c.Cortar<EntidadeFone>(nameof(EntidadeFone.Contato), l.Texto("CONTATO")?.ToUpperInvariant(), onde),
                Departamento = c.Cortar<EntidadeFone>(nameof(EntidadeFone.Departamento), l.Texto("DEPARTAMENTO")?.ToUpperInvariant(), onde),
                Inativo = l.Flag("INATIVO"), IdLegado = legado,
            };
            var f = Fone(entidade, antigo);
            c.Db.Add(f);
            novosFones.Add((antigo, f));
            if (c.CopiarParaEmpresa(l.Int("ID_ENTIDADES"))) c.Db.Add(Fone(c.EntidadeDaEmpresaUsuaria.Nova, null));
        }

        var contatosDoResultado = (await c.Fb.LerAsync("SELECT DISTINCT ID_ENT_CONTATO_RESULTADO AS ID FROM LAB_PROPOSTAS WHERE ID_ENT_CONTATO_RESULTADO IS NOT NULL", ct))
            .Select(x => x.IntObrigatorio("ID")).ToHashSet();
        var emails = await c.Fb.LerAsync("""
            SELECT ID_ENTIDADES_EMAILS, ID_ENTIDADES, ID_EMAILS_TIPOS, EMAIL, PRINCIPAL, ENVIAR_NFE, DEPARTAMENTO, NOME_CONTATO, INATIVO
              FROM ENTIDADES_EMAILS ORDER BY ID_ENTIDADES_EMAILS
            """, ct);
        var porEntidade = new Dictionary<(int, string), EntidadeEmail>(); // UQ_ENTEML: o mesmo e-mail duas vezes na entidade
        var novosEmails = new List<(int Antigo, EntidadeEmail Novo)>();
        var emailsDescartados = 0;
        foreach (var l in emails)
        {
            var antigo = l.IntObrigatorio("ID_ENTIDADES_EMAILS");
            var onde = $"e-mail {antigo}";
            if (c.Novo("ENTIDADES", l.Int("ID_ENTIDADES"), onde) is not { } entidade) continue;
            var original = l.Texto("EMAIL");
            var email = Conversoes.Email(original);
            if (email is null && contatosDoResultado.Contains(antigo) && !string.IsNullOrWhiteSpace(original))
            {
                // a proposta exige o contato do resultado: sem ele, a M-11 não grava a proposta
                email = original.Trim().ToLowerInvariant();
                c.Anomalia($"{onde}: \"{original}\" não é um e-mail, mas é contato do resultado de proposta — migrado como está (corrigir no cadastro).");
            }
            if (email is null) { c.Anomalia($"{onde}: \"{original}\" não é um e-mail — não migrado."); emailsDescartados++; continue; }
            if (!string.Equals(email, original, StringComparison.OrdinalIgnoreCase)) c.Anomalia($"{onde}: \"{original}\" corrigido para \"{email}\".");
            if (porEntidade.TryGetValue((entidade, email), out var mesmo))
            {
                c.Anomalia($"{onde}: {email} repetido na mesma entidade — aponta para o mesmo registro.");
                novosEmails.Add((antigo, mesmo));
                continue;
            }
            EntidadeEmail Email(int idEntidade, int? legado) => new()
            {
                IdEntidades = idEntidade, IdEmailsTipos = c.Novo("EMAILS_TIPOS", l.Int("ID_EMAILS_TIPOS"), onde),
                Email = c.Cortar<EntidadeEmail>(nameof(EntidadeEmail.Email), email, onde)!,
                NomeContato = c.Cortar<EntidadeEmail>(nameof(EntidadeEmail.NomeContato), l.Texto("NOME_CONTATO")?.ToUpperInvariant(), onde),
                Departamento = c.Cortar<EntidadeEmail>(nameof(EntidadeEmail.Departamento), l.Texto("DEPARTAMENTO")?.ToUpperInvariant(), onde),
                Principal = l.Flag("PRINCIPAL"), EnviarNfe = l.Flag("ENVIAR_NFE"), EnviarLaudo = contatosDoResultado.Contains(antigo),
                Inativo = l.Flag("INATIVO"), IdLegado = legado,
            };
            var m = Email(entidade, antigo);
            c.Db.Add(m);
            porEntidade[(entidade, email)] = m;
            novosEmails.Add((antigo, m));
            if (c.CopiarParaEmpresa(l.Int("ID_ENTIDADES"))) c.Db.Add(Email(c.EntidadeDaEmpresaUsuaria.Nova, null));
        }
        await c.Db.SaveChangesAsync(ct);
        foreach (var (antigo, f) in novosFones) c.Mapa("ENTIDADES_FONES")[antigo] = f.IdEntidadesFones;
        foreach (var (antigo, m) in novosEmails) c.Mapa("ENTIDADES_EMAILS")[antigo] = m.IdEntidadesEmails;

        c.Conferir("telefones do legado", fones.Count, c.Mapa("ENTIDADES_FONES").Count);
        c.Conferir("e-mails do legado (− os que não são e-mail)", emails.Count - emailsDescartados, c.Mapa("ENTIDADES_EMAILS").Count);
        return novosFones.Count + porEntidade.Count;
    }
}
