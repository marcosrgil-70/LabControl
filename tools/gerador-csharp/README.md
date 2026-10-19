# Gerador das entidades EF Core (schema v2)

Gera, a partir de `Scripts/v2/*.sql` (fonte da verdade do schema):

| Saída | Conteúdo |
|---|---|
| `Models/<Classe>.g.cs` | uma classe `partial` por tabela/view, propriedade por coluna, navegação por FK |
| `Data/Configurations/<Classe>Configuration.g.cs` | Fluent API: tabela/view, chave, colunas, tamanhos, precisão, enums, FKs, colunas preenchidas pelo banco |
| `Data/LabDbContext.DbSets.g.cs` | os `DbSet<>` do `LabDbContext` |

Reaproveita o parser de `tools/gerador-docs/parse.mjs`. Requer Node 18+ (sem dependências).

## Uso
```bash
node tools/gerador-csharp/gerar.mjs
```
Se uma **view** mudou, atualize antes as colunas (o parser não deduz tipos de um SELECT), com o banco já recriado:
```bash
mysql -u root -p -N --raw -D labcontrol_v2 < tools/gerador-csharp/colunas_views.sql > tools/gerador-csharp/views.json
```
Depois: `dotnet build` e `dotnet test tests/LabControl.Tests` (o teste `ModeloTests` compara o modelo com o `information_schema`).

## Convenções
- Classe no **singular** (mapa `CLASSES` em `gerar.mjs`); DbSet = tabela em PascalCase (`LAB_HIST_AMOSTRAS` → `LabHistAmostra` / `db.LabHistAmostras`); propriedade = coluna em PascalCase (`ID_LAB_HIST_AMOSTRAS` → `IdLabHistAmostras`).
- Navegação: FK com o mesmo nome da PK do alvo → nome da classe (`IdEmpresas` → `Empresa`); senão a coluna sem `Id` (`IdEntidadesFuncResp` → `EntidadesFuncResp`). Sem coleções inversas: adicione à mão quando uma tela precisar.
- `ENUM` → enum de `Models/Enums.cs` (mapa `ENUMS`), valor do banco em `[ValorBanco]`.
- `DEFAULT` constante → inicializador na propriedade (ex.: `UltimaRevisao = true`), para o EF não gravar o padrão do tipo.
- Colunas preenchidas por trigger (mapa `GERADAS`):
  - `calc` → `.CalculadaPeloBanco()`: nunca enviada, relida após INSERT/UPDATE (`NR_AMOSTRA`, `RESULTADO_FORMATADO`, totais da proposta, `ALTERADO_EM`…);
  - `add` → `.PreenchidaPeloBancoNaInclusao()`: omitida no INSERT quando vazia e relida (`CODIGO` do produto, `ITEM`, `DT_SOLICITACAO`…);
  - `fixo` → idem, mas nunca enviada em UPDATE (`COD_AMOSTRA`, `ANO_AMOSTRA`, `COD_PROPOSTA`, `NR_PEDIDO`…).
  O gerador avisa quando uma trigger passa a atribuir uma coluna que não está em `GERADAS` nem em `NORMAIS`.
- Valores alterados por trigger **AFTER** de outra tabela (ex.: totais da proposta ao incluir análise) não voltam sozinhos: `await db.Entry(x).ReloadAsync()`.
- Migração (`@LC_MIGRACAO = 1`): colunas `calc` não são enviadas pelo EF — a carga do legado grava essas colunas por SQL.

## Ajustes manuais
Nunca edite `*.g.cs`. Use os arquivos parciais sem `.g`:
- `Models/LabProposta.cs` — `public partial class LabProposta { public List<LabPropostaProduto> Produtos { get; set; } = []; }`
- `Data/Configurations/LabPropostaConfiguration.cs` — `partial void Ajustar(EntityTypeBuilder<LabProposta> b) { ... }`
