using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace LabControl.Tests.Navegador;

/// <summary>CRUD de cada tela já feita, pelo navegador, como o usuário faz (incluir, pesquisar, alterar, inativar, excluir).</summary>
[Collection("Navegador")]
public class CrudNavegadorTests(AppNavegador app)
{
    private static string Unico(string prefixo) => $"{prefixo} {Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    private static ILocator Linha(IPage p, string texto) => p.Locator("tbody tr", new() { HasText = texto });

    // ------------------------------------------------------------- T10 auxiliares (Idiomas)

    [Fact]
    public async Task Auxiliar_idiomas_crud_completo()
    {
        await using var c = await Cenario.CriarAsync();
        await using var s = await app.EntrarAsync(c);
        var p = s.Pagina;
        var nome = Unico("ALEMÃO");

        await s.IrAsync("/Auxiliares/IDIOMAS");
        await p.ClickAsync("button[data-form$='/Incluir']");
        await p.FillAsync("#campo_Descricao", nome);
        await p.ClickAsync("#formAuxiliar button[type=submit]");
        Assert.Equal("Idioma gravado.", await s.AlertaAsync());
        await Assertions.Expect(Linha(p, nome)).ToHaveCountAsync(1);

        // pesquisar
        await p.FillAsync("#Busca", nome[^6..]);
        await p.PressAsync("#Busca", "Enter");
        await Assertions.Expect(p.Locator("tbody tr")).ToHaveCountAsync(1);

        // alterar
        await Linha(p, nome).Locator("button[title=Alterar]").ClickAsync();
        await p.FillAsync("#campo_Descricao", nome + " DE");
        await p.ClickAsync("#formAuxiliar button[type=submit]");
        Assert.Equal("Idioma gravado.", await s.AlertaAsync());
        await Assertions.Expect(Linha(p, nome + " DE")).ToHaveCountAsync(1);

        // inativar (pede confirmação, some da lista, aparece em "mostrar inativos") e reativar
        await Linha(p, nome).Locator("button[title=Inativar]").ClickAsync();
        Assert.StartsWith("Idioma inativado.", await s.AlertaAsync());
        Assert.Contains(s.Dialogos, d => d.Contains("Inativar"));
        await Assertions.Expect(Linha(p, nome)).ToHaveCountAsync(0);
        await p.CheckAsync("#Inativos");
        await p.ClickAsync("form.card button[type=submit]");
        await Assertions.Expect(Linha(p, nome)).ToContainTextAsync("Inativo");
        await Linha(p, nome).Locator("button[title=Reativar]").ClickAsync();
        Assert.Equal("Idioma reativado.", await s.AlertaAsync());
        Assert.Contains(s.Dialogos, d => d.StartsWith("Reativar"));
        Assert.DoesNotContain(s.Dialogos, string.IsNullOrWhiteSpace); // nenhum confirm() vazio

        // excluir
        await Linha(p, nome).Locator("button[title=Excluir]").ClickAsync();
        Assert.Equal("Idioma excluído.", await s.AlertaAsync());
        await Assertions.Expect(Linha(p, nome)).ToHaveCountAsync(0);
        Assert.False(await c.Db.Idiomas.AnyAsync(i => i.Descricao.StartsWith(nome)));
        Assert.Empty(s.ErrosJs);
    }

    // ------------------------------------------------------------- T12 clientes

    [Fact]
    public async Task Cliente_crud_completo_com_enderecos_telefones_e_emails()
    {
        await using var c = await Cenario.CriarAsync();
        await using var s = await app.EntrarAsync(c);
        var p = s.Pagina;
        var nome = Unico("CLIENTE NAVEGADOR");

        // incluir
        await s.IrAsync("/Clientes");
        await p.ClickAsync("a:has-text('Incluir')");
        await p.FillAsync("#Nome", nome);
        await p.FillAsync("#NomeFantasia", "fantasia nav");
        await p.FillAsync("#CpfCnpj", "11.444.777/0001-61");
        await p.ClickAsync("button:has-text('Gravar dados')");
        Assert.Contains("Cadastro gravado", await s.AlertaAsync());
        await p.WaitForURLAsync(u => u.Contains("/Clientes/Editar/"));
        var id = int.Parse(p.Url.Split('/').Last().Split('#')[0]);
        await Assertions.Expect(p.Locator("#aba-enderecos")).ToBeVisibleAsync(); // abriu na aba Endereços

        // endereço: tipo, tipo de logradouro, UF → cidade
        await p.ClickAsync("[data-contato-novo=endereco]");
        await p.SelectOptionAsync("#end-tipo", new SelectOptionValue { Label = "NOTA FISCAL" });
        await p.SelectOptionAsync("#end-tiplog", new SelectOptionValue { Label = "RUA" });
        await p.FillAsync("#end-logradouro", "das flores");
        await p.FillAsync("#end-numero", "100");
        await p.FillAsync("#end-bairro", "centro");
        await p.SelectOptionAsync("#end-uf", new SelectOptionValue { Label = "SP" });
        await Assertions.Expect(p.Locator("#end-cidade option", new() { HasText = "CAMPINAS" })).ToHaveCountAsync(1);
        await p.SelectOptionAsync("#end-cidade", new SelectOptionValue { Label = "CAMPINAS" });
        await p.ClickAsync("#modal-endereco button[type=submit]");
        await Assertions.Expect(p.Locator("#aba-enderecos tbody")).ToContainTextAsync("R. DAS FLORES, 100");
        await Assertions.Expect(p.Locator("#aba-enderecos tbody")).ToContainTextAsync("CAMPINAS/SP");

        // segundo endereço ativo do mesmo tipo: recusado no modal
        await p.ClickAsync("[data-contato-novo=endereco]");
        await p.SelectOptionAsync("#end-tipo", new SelectOptionValue { Label = "NOTA FISCAL" });
        await p.FillAsync("#end-logradouro", "outra");
        await p.SelectOptionAsync("#end-uf", new SelectOptionValue { Label = "SP" });
        await Assertions.Expect(p.Locator("#end-cidade option", new() { HasText = "CAMPINAS" })).ToHaveCountAsync(1);
        await p.SelectOptionAsync("#end-cidade", new SelectOptionValue { Label = "CAMPINAS" });
        await p.ClickAsync("#modal-endereco button[type=submit]");
        await Assertions.Expect(p.Locator("#modal-endereco [data-erro]")).ToContainTextAsync("Já existe um endereço ativo deste tipo");
        await p.ClickAsync("#modal-endereco button:has-text('Cancelar')");

        // alterar o endereço (o modal vem preenchido, inclusive a cidade)
        await p.Locator("#aba-enderecos button[title=Alterar]").First.ClickAsync();
        await Assertions.Expect(p.Locator("#end-cidade")).ToHaveValueAsync(
            (await c.Db.Cidades.Where(x => x.CodigoIbge == "3509502").Select(x => x.IdCidades).SingleAsync()).ToString());
        await p.FillAsync("#end-numero", "200");
        await p.ClickAsync("#modal-endereco button[type=submit]");
        await Assertions.Expect(p.Locator("#aba-enderecos tbody")).ToContainTextAsync("R. DAS FLORES, 200");

        // endereço pelo CEP (botão Buscar): preenche logradouro, tipo, bairro, UF e cidade
        await p.ClickAsync("[data-contato-novo=endereco]");
        await p.SelectOptionAsync("#end-tipo", new SelectOptionValue { Label = "ENTREGA" });
        await p.FillAsync("#end-cep", "01310-100");
        await p.ClickAsync("[data-buscar-cep]");
        await Assertions.Expect(p.Locator("#end-logradouro")).ToHaveValueAsync("PAULISTA");
        await Assertions.Expect(p.Locator("#end-tiplog option:checked")).ToHaveTextAsync("AVENIDA");
        await Assertions.Expect(p.Locator("#end-cidade option:checked")).ToHaveTextAsync("SÃO PAULO");
        await p.FillAsync("#end-numero", "1000");
        await p.ClickAsync("#modal-endereco button[type=submit]");
        await Assertions.Expect(p.Locator("#aba-enderecos tbody")).ToContainTextAsync("AV. PAULISTA, 1000");
        await Assertions.Expect(p.Locator("#aba-enderecos tbody")).ToContainTextAsync("BELA VISTA - SÃO PAULO/SP - 01310-100");
        // CEP inexistente: mensagem e digitação manual
        await p.ClickAsync("[data-contato-novo=endereco]");
        await p.FillAsync("#end-cep", "99999-999");
        await p.ClickAsync("[data-buscar-cep]");
        await Assertions.Expect(p.Locator("#modal-endereco [data-cep-msg]")).ToHaveTextAsync("CEP não encontrado.");
        await p.ClickAsync("#modal-endereco button:has-text('Cancelar')");

        // telefones: dois "principal" → só o último fica principal
        await p.ClickAsync("button[data-bs-target='#aba-telefones']");
        foreach (var (fone, tipo) in new[] { ("33334444", "COMERCIAL"), ("991234567", "CELULAR COMERCIAL") })
        {
            await p.ClickAsync("[data-contato-novo=fone]");
            await p.SelectOptionAsync("#fone-tipo", new SelectOptionValue { Label = tipo });
            await p.FillAsync("#fone-ddd", "19");
            await p.FillAsync("#fone-numero", fone);
            await p.CheckAsync("#fone-principal");
            await p.ClickAsync("#modal-fone button[type=submit]");
            await Assertions.Expect(p.Locator("#aba-telefones tbody")).ToContainTextAsync(fone.Length == 9 ? "99123-4567" : "3333-4444");
        }
        await Assertions.Expect(p.Locator("#aba-telefones .badge", new() { HasText = "principal" })).ToHaveCountAsync(1);
        await Assertions.Expect(Linha(p, "99123-4567")).ToContainTextAsync("principal");
        await Assertions.Expect(p.Locator("button[data-bs-target='#aba-telefones'] [data-contagem]")).ToHaveTextAsync("2");

        // excluir um telefone
        await Linha(p, "3333-4444").Locator("button[title=Excluir]").ClickAsync();
        await Assertions.Expect(Linha(p, "3333-4444")).ToHaveCountAsync(0);

        // e-mail: incluir, alterar
        await p.ClickAsync("button[data-bs-target='#aba-emails']");
        await p.ClickAsync("[data-contato-novo=email]");
        await p.FillAsync("#email-email", "Contato@Cliente.com");
        await p.FillAsync("#email-contato", "maria");
        await p.CheckAsync("#email-laudo");
        await p.ClickAsync("#modal-email button[type=submit]");
        await Assertions.Expect(p.Locator("#aba-emails tbody")).ToContainTextAsync("contato@cliente.com");
        await Linha(p, "contato@cliente.com").Locator("button[title=Alterar]").ClickAsync();
        await Assertions.Expect(p.Locator("#email-laudo")).ToBeCheckedAsync();
        await p.FillAsync("#email-contato", "maria silva");
        await p.ClickAsync("#modal-email button[type=submit]");
        await Assertions.Expect(p.Locator("#aba-emails tbody")).ToContainTextAsync("MARIA SILVA");

        // alterar dados básicos (aba Observações também)
        await p.ClickAsync("button[data-bs-target='#aba-dados']");
        await p.FillAsync("#Nome", nome + " ALTERADO");
        await p.ClickAsync("button[data-bs-target='#aba-observacoes']");
        await p.FillAsync("#Observacao", "observação do cliente");
        await p.ClickAsync("button:has-text('Gravar dados')");
        Assert.Equal("Cadastro gravado.", await s.AlertaAsync());
        var gravado = await c.Db.Entidades.AsNoTracking().SingleAsync(e => e.IdEntidades == id);
        Assert.Equal((nome + " ALTERADO", "observação do cliente", "FANTASIA NAV"), (gravado.Nome, gravado.Observacao, gravado.NomeFantasia));

        // pesquisar: por nome e por parte do CNPJ sem máscara
        await s.IrAsync("/Clientes");
        await p.FillAsync("#Texto", "444777");
        await p.PressAsync("#Texto", "Enter");
        await Assertions.Expect(p.Locator("tbody tr")).ToHaveCountAsync(1);
        await Assertions.Expect(Linha(p, nome)).ToContainTextAsync("(19) 99123-4567");

        // inativar (com confirmação) → some; "mostrar inativos" → volta; reativar
        await Linha(p, nome).Locator("button[title=Inativar]").ClickAsync();
        Assert.Contains("inativado", await s.AlertaAsync());
        await p.FillAsync("#Texto", nome);
        await p.PressAsync("#Texto", "Enter");
        await Assertions.Expect(Linha(p, nome)).ToHaveCountAsync(0);
        await Assertions.Expect(p.Locator(".card-footer")).ToContainTextAsync("1 inativo(s) também correspondem à pesquisa");
        await p.CheckAsync("#Inativos");
        await p.ClickAsync("form.card button[type=submit]");
        await Linha(p, nome).Locator("button[title=Reativar]").ClickAsync();
        Assert.Contains("reativado", await s.AlertaAsync());
        Assert.Contains(s.Dialogos, d => d.StartsWith("Reativar"));
        Assert.DoesNotContain(s.Dialogos, string.IsNullOrWhiteSpace); // nenhum confirm() vazio

        // excluir (sem movimento) → contatos vão junto
        await p.FillAsync("#Texto", nome);
        await p.PressAsync("#Texto", "Enter");
        await Linha(p, nome).Locator("button[title=Excluir]").ClickAsync();
        Assert.Equal("Cadastro excluído.", await s.AlertaAsync());
        Assert.False(await c.Db.Entidades.AnyAsync(e => e.IdEntidades == id));
        Assert.Empty(s.ErrosJs);
    }
}
