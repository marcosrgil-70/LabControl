using LabControl.Data;
using LabControl.Infra;
using LabControl.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    // erro de negócio do MySQL (45000/1062/1451/1452) que escapar do serviço vira mensagem, não página 500
    options.Filters.Add<ErroNegocioFilter>();
    // antiforgery em todo POST (RN-02.16)
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// MySQL v2 via EF Core (Pomelo). O schema é de Scripts/v2/*.sql: sem migrations, sem EnsureCreated.
// A connection string fica em user-secrets (ver Scripts/v2/README.md), nunca no appsettings.json.
var connectionString = builder.Configuration.GetConnectionString("LabControl");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "ConnectionStrings:LabControl não configurada. Rode: dotnet user-secrets set \"ConnectionStrings:LabControl\" " +
        "\"Server=localhost;Port=3306;Database=labcontrol_v2;User=root;Password=<senha>\"");

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IEmpresaContexto, EmpresaContexto>();
// interceptor de sessão MySQL (@LC_*) por request: recebe o IEmpresaContexto do escopo
builder.Services.AddDbContext<LabDbContext>((sp, options) =>
    options.Configurar(connectionString, sp.GetRequiredService<IEmpresaContexto>()));
builder.Services.AddScoped<IAuditoria, ServicoAuditoria>();
builder.Services.AddScoped<Procedures>();
builder.Services.AddSingleton(TimeProvider.System);
// Serviços de negócio (Services/) registrados aqui, um AddScoped por serviço
builder.Services.AddScoped<ContaService>();
builder.Services.AddScoped<PainelService>();
builder.Services.AddScoped<VinculoUsuarioProfissionalService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<PerfilService>();
builder.Services.AddScoped<CopiaCadastros>();
builder.Services.AddScoped<EmpresaService>();
builder.Services.AddScoped<ParametrosEmpresaService>();
builder.Services.AddScoped<LabControl.Services.Auxiliares.AuxiliarService>();
builder.Services.AddSingleton<TelasExistentes>(); // menu e painel só mostram telas já reescritas

// Login próprio (T00 / P02): cookie HttpOnly + Secure + SameSite=Lax, expiração deslizante, revalidado a cada request
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Conta/Login";
        options.LogoutPath = "/Conta/Sair";
        options.AccessDeniedPath = "/Conta/AcessoNegado";
        options.Cookie.Name = "LabControl.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(builder.Configuration.GetValue("Autenticacao:ExpiracaoHoras", 8));
        options.SlidingExpiration = true;
        options.EventsType = typeof(ValidarSessao);
    });
builder.Services.AddScoped<ValidarSessao>();
// tudo exige login, exceto o que tiver [AllowAnonymous] (tela de login)
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ExigirEmpresaMiddleware>();
app.MapStaticAssets().AllowAnonymous(); // CSS/JS da tela de login

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

// Permite WebApplicationFactory<Program> nos testes
public partial class Program;
