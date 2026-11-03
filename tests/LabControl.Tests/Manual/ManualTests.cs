using LabControl.Tests.Navegador;
using Microsoft.Playwright;
using Xunit.Abstractions;

namespace LabControl.Tests.Manual;

/// <summary>
/// Manual do usuário (pedido do usuário, 03/11/2026): tira as imagens das telas com o app real e um laboratório fictício
/// (<see cref="CenarioManual"/>) e imprime <c>docs/manual/manual.html</c> no <c>Manual.pdf</c> da raiz pelo Edge.
/// Ferramenta: não roda na suíte — só com <c>LABCONTROL_MANUAL</c> = pasta <c>docs/manual</c> (ver
/// <c>docs/manual/gerar-manual.ps1</c>). <c>LABCONTROL_MANUAL_SO_PDF=1</c> pula as imagens e só refaz o PDF.
/// </summary>
[Collection("Navegador")]
public class ManualTests(AppNavegador app, ITestOutputHelper saida)
{
    private static string? Pasta => Environment.GetEnvironmentVariable("LABCONTROL_MANUAL") is { Length: > 0 } p ? p : null;
    private static bool SoPdf => Environment.GetEnvironmentVariable("LABCONTROL_MANUAL_SO_PDF") == "1";

    [Fact]
    public async Task Gera_o_manual_do_usuario()
    {
        if (Pasta is not { } pasta) { saida.WriteLine("LABCONTROL_MANUAL ausente: nada a gerar."); return; }
        if (!SoPdf) await CapturarAsync(Path.Combine(pasta, "img"));
        await ImprimirPdfAsync(Path.Combine(pasta, "manual.html"), Path.Combine(BancoTeste.RaizDoRepositorio(), "Manual.pdf"));
    }

    private async Task CapturarAsync(string img)
    {
        Directory.CreateDirectory(img);
        foreach (var f in Directory.GetFiles(img, "*.png")) File.Delete(f);
        var c = await CenarioManual.CriarAsync(app.PastaBoletins);

        await using var ctx = await app.Browser.NewContextAsync(new() { Locale = "pt-BR", ViewportSize = new() { Width = 1366, Height = 820 }, DeviceScaleFactor = 1.5f });
        var p = await ctx.NewPageAsync();
        p.Dialog += async (_, d) => await d.AcceptAsync();
        // inteira = página toda (login, painel com o menu); senão só a área de conteúdo, até o fim do que está nela
        // (o menu lateral é comprido e deixaria as telas curtas com muito espaço vazio)
        async Task Foto(string nome, bool inteira = false)
        {
            await p.WaitForLoadStateAsync(LoadState.NetworkIdle);
            var arquivo = Path.Combine(img, nome + ".png");
            var area = inteira ? null : await p.EvaluateAsync<double[]?>("""
                () => {
                  const m = document.querySelector('main.content-wrapper'); if (!m) return null;
                  const r = m.getBoundingClientRect();
                  let fim = r.top;
                  for (const f of m.querySelectorAll(':scope > *')) { const b = f.getBoundingClientRect(); if (b.height > 0) fim = Math.max(fim, b.bottom); }
                  return [r.left + scrollX, r.top + scrollY, r.width, Math.max(200, fim - r.top + 24)];
                }
                """);
            if (area is { Length: 4 })
                await p.ScreenshotAsync(new() { Path = arquivo, FullPage = true, Clip = new() { X = (float)area[0], Y = (float)area[1], Width = (float)area[2], Height = (float)area[3] } });
            else
                await p.ScreenshotAsync(new() { Path = arquivo, FullPage = inteira });
            saida.WriteLine($"{nome}: {p.Url}");
        }
        async Task Ir(string caminho, string nome) { await p.GotoAsync(app.Url + caminho); await Foto(nome); }
        // janela (modal) aberta: a parte visível da tela
        async Task FotoTela(string nome)
        {
            await p.WaitForTimeoutAsync(600); // animação do modal
            await p.ScreenshotAsync(new() { Path = Path.Combine(img, nome + ".png") });
            saida.WriteLine($"{nome}: {p.Url} (tela)");
        }
        // telas de acesso: só o quadro do formulário, com uma margem do fundo
        async Task FotoCartao(string nome)
        {
            await p.WaitForLoadStateAsync(LoadState.NetworkIdle);
            var b = (await p.Locator("main.conta-cartao").BoundingBoxAsync())!;
            await p.ScreenshotAsync(new() { Path = Path.Combine(img, nome + ".png"), Clip = new() { X = b.X - 30, Y = b.Y - 30, Width = b.Width + 60, Height = b.Height + 60 } });
            saida.WriteLine($"{nome}: {p.Url} (cartão)");
        }
        async Task Aba(string texto) { await p.Locator(".nav-tabs .nav-link", new() { HasTextString = texto }).First.ClickAsync(); await p.WaitForTimeoutAsync(300); }
        async Task Botao(string texto) => await p.Locator("button:visible, a.btn:visible", new() { HasTextString = texto }).First.ClickAsync();

        // acesso: login → escolha da empresa (o operador tem matriz e filial) → painel
        await p.GotoAsync(app.Url + "/Conta/Login");
        await p.FillAsync("[name=Login]", CenarioManual.LoginOperador);
        await FotoCartao("01-login");
        await p.FillAsync("[name=Senha]", CenarioManual.Senha);
        await p.ClickAsync("button[type=submit]");
        await p.WaitForURLAsync(u => u.Contains("/Conta/Empresa"));
        await FotoCartao("02-escolher-empresa");
        await p.CheckAsync($"input[name=IdEmpresa][value='{c.Empresa}']");
        await p.ClickAsync("button[type=submit]");
        await p.WaitForURLAsync(u => !u.Contains("/Conta/"));
        await Foto("03-painel", true);

        // laboratório
        await Ir("/Propostas", "10-propostas-lista");
        await Ir($"/Propostas/Editar/{c.Proposta}", "11-proposta-com-pedido");
        await Ir($"/Propostas/Editar/{c.PropostaAberta}", "12-proposta-editar");
        await Aba("Análises");
        await Foto("13-proposta-analises");
        await Ir("/PedidosVenda", "15-pedidos-lista");
        await Ir($"/PedidosVenda/Editar/{c.Pedido}", "16-pedido-editar");
        await Ir("/Amostras", "20-amostras-lista");
        await Botao("Pesquisa avançada");
        await FotoTela("24-pesquisa-avancada");
        await Ir("/Amostras/Criar", "21-amostra-nova");
        await Ir($"/Amostras/Editar/{c.AmostraPendente}", "22-amostra-editar");
        await Aba("Testes");
        await Foto("22b-amostra-testes");
        await Ir($"/Amostras/Comprovante/{c.AmostraPendente}", "23-amostra-comprovante");
        await Ir($"/Movimentacao/Amostra/{c.AmostraEmitida}", "26-movimentacao-amostra");
        await Botao("Nova movimentação");
        await FotoTela("26b-movimentacao-nova");
        await Ir("/Localizacao", "27-localizacao-lista");
        await Ir($"/Localizacao/Amostra/{c.AmostraEmitida}", "28-localizacao-amostra");
        await Ir("/Etiquetas", "29-etiquetas");
        await Ir("/Resultados", "30-resultados-lista");
        await Aba("Fila de análises");
        await Foto("30b-fila-analises");
        await Ir($"/Resultados/Editar/{c.Resultado}", "31-resultado-editar");
        await Ir("/Boletins", "35-boletins-lista");
        await Ir($"/Boletins/Editar/{c.BoletimAberto}", "36-boletim-editar");
        await Ir($"/Boletins/Editar/{c.BoletimEmitido}", "37-boletim-emitido");

        // cadastros
        await Ir("/Clientes", "40-clientes-lista");
        await Ir($"/Clientes/Editar/{c.Cliente}", "41-cliente-editar");
        await Aba("Endereços");
        await Foto("41b-cliente-enderecos");
        await Ir($"/Profissionais/Editar/{c.Profissional}", "43-profissional-editar");
        await Ir($"/Produtos/Editar/{c.Produto}", "45-produto-editar");
        await Ir($"/ParametrosAnalise/Editar/{c.Parametro}", "47-parametro-editar");
        await Ir("/Auxiliares", "48-auxiliares");
        await Ir("/Auxiliares/PRAZOS", "49-auxiliar-prazos");
        await p.Locator("tr", new() { HasTextString = "5 DIAS ÚTEIS" }).Locator("button", new() { HasTextString = "Alterar" }).ClickAsync();
        await FotoTela("50-auxiliar-prazo-editar");
        await Ir("/Geografia/Cidades?Busca=CAMPINAS", "51-cidades");

        // relatórios
        await p.GotoAsync(app.Url + "/Relatorios/Faturamento"); await Botao("Visualizar"); await Foto("55-rel-faturamento");
        await p.GotoAsync(app.Url + "/Relatorios/AmostrasPeriodo"); await Botao("Visualizar"); await Foto("56-rel-amostras-periodo");

        // sistema
        await Ir("/Empresas", "65-empresa");
        await Ir("/Empresa/Parametros", "66-parametros-empresa");
        await Aba("Numeração");
        await Foto("66b-parametros-numeracao");
        await Aba("E-mail");
        await Foto("66c-parametros-email");
        await Ir("/Usuarios", "67-usuarios-lista");
        await Ir($"/Usuarios/Editar/{c.UsuarioRecepcao}", "68-usuario-editar");
        await Ir("/Usuarios/Criar", "68b-usuario-novo");
        await Ir("/Perfis", "69-perfis-lista");
        await Ir($"/Perfis/Editar/{c.PerfilRecepcao}", "70-perfil-editar");
        await Ir("/Auditoria", "71-auditoria");
        await Ir($"/Auditoria/Detalhe/{c.EventoAuditoria}", "72-auditoria-detalhe");

        // administrador do sistema: lista de empresas e migração
        await using var ctxAdm = await app.Browser.NewContextAsync(new() { Locale = "pt-BR", ViewportSize = new() { Width = 1366, Height = 820 }, DeviceScaleFactor = 1.5f });
        p = await ctxAdm.NewPageAsync();
        await p.GotoAsync(app.Url + "/Conta/Login");
        await p.FillAsync("[name=Login]", CenarioManual.LoginAdministrador);
        await p.FillAsync("[name=Senha]", CenarioManual.Senha);
        await p.ClickAsync("button[type=submit]");
        if (p.Url.Contains("/Conta/Empresa"))
        {
            await p.CheckAsync($"input[name=IdEmpresa][value='{c.Empresa}']");
            await p.ClickAsync("button[type=submit]");
        }
        await p.WaitForURLAsync(u => !u.Contains("/Conta/"));
        await Ir("/Empresas", "75-empresas-admin");
        await Ir("/Empresas/Criar", "76-empresa-nova");
        await Ir("/Migracao", "77-migracao");

        // 1º acesso: usuário com senha provisória troca a senha antes de entrar
        await using var ctxNovo = await app.Browser.NewContextAsync(new() { Locale = "pt-BR", ViewportSize = new() { Width = 1366, Height = 820 }, DeviceScaleFactor = 1.5f });
        p = await ctxNovo.NewPageAsync();
        await p.GotoAsync(app.Url + "/Conta/Login");
        await p.FillAsync("[name=Login]", "JOAO.PEREIRA");
        await p.FillAsync("[name=Senha]", CenarioManual.Senha);
        await p.ClickAsync("button[type=submit]");
        await p.WaitForURLAsync(u => u.Contains("/Conta/TrocarSenha"));
        await FotoCartao("04-trocar-senha");
    }

    /// <summary>Imprime o HTML do manual em A4 pelo Edge, com o número da página no rodapé.</summary>
    private async Task ImprimirPdfAsync(string html, string pdf)
    {
        await using var ctx = await app.Browser.NewContextAsync();
        var p = await ctx.NewPageAsync();
        await p.GotoAsync(new Uri(html).AbsoluteUri);
        await p.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await p.PdfAsync(new()
        {
            Path = pdf, Format = "A4", PrintBackground = true, DisplayHeaderFooter = true,
            Margin = new() { Top = "16mm", Bottom = "16mm", Left = "15mm", Right = "15mm" },
            HeaderTemplate = "<span></span>",
            FooterTemplate = "<div style='width:100%;font-size:8px;color:#777;padding:0 15mm;display:flex;justify-content:space-between'>" +
                             "<span>LabControl — Manual do Usuário</span><span>Página <span class='pageNumber'></span> de <span class='totalPages'></span></span></div>",
        });
        saida.WriteLine($"PDF: {pdf} ({new FileInfo(pdf).Length / 1024} KB)");
    }
}
