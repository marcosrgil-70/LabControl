using LabControl.Data;
using LabControl.Infra;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    // erro de negócio do MySQL (45000/1062/1451/1452) que escapar do serviço vira mensagem, não página 500
    options.Filters.Add<ErroNegocioFilter>();
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
// Serviços de negócio (Services/) registrados aqui, um AddScoped por serviço

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
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
