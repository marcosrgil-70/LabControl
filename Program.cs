using LabControl.Data;
using LabControl.Infra;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// MySQL v2 via EF Core (Pomelo). O schema é de Scripts/v2/*.sql: sem migrations, sem EnsureCreated.
// A connection string fica em user-secrets (ver Scripts/v2/README.md), nunca no appsettings.json.
var connectionString = builder.Configuration.GetConnectionString("LabControl");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "ConnectionStrings:LabControl não configurada. Rode: dotnet user-secrets set \"ConnectionStrings:LabControl\" " +
        "\"Server=localhost;Port=3306;Database=labcontrol_v2;User=root;Password=<senha>\"");

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IEmpresaContexto, EmpresaContexto>();
builder.Services.AddDbContext<LabDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 46))));

// Sessão: ainda lida pelo _Layout (nome da empresa/usuário). Sai na Fase 1 (cookie authentication).
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.UseMiddleware<ExigirEmpresaMiddleware>();
app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

// Permite WebApplicationFactory<Program> nos testes
public partial class Program;
