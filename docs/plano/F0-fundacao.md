# Fase 0 — Fundação (banco + infraestrutura do projeto)

**Objetivo:** banco `labcontrol_v2` criado pelos scripts e um esqueleto C# que já garante multiempresa, sessão MySQL, tratamento de erros de negócio, auditoria e testes automatizados. Nenhuma tela de negócio ainda.
**Modelo/modo:** 🔵 Opus · ✋ modo padrão (mexe em banco e infraestrutura) · 📐 plan mode antes de 0.3.
**Pré-requisitos:** MySQL 8 local; .NET 10 SDK; scripts `Scripts/v2/01..05` (já validados).
**Ler antes:** [01-ARQUITETURA-V2](../01-ARQUITETURA-V2.md) · [banco/11](../banco/11-MODELO-MYSQL.md) · [banco/12](../banco/12-PROCEDURES-REVISOES.md) · [P01](../processos/P01-multiempresa.md) · [P22](../processos/P22-erros-transacoes.md) · [P21](../processos/P21-auditoria-log.md) · [objetos do banco](../banco/objetos/README.md)

---

### 0.1 Criar o banco `labcontrol_v2`
> ✅ **Concluída em 19/10/2026** — contagens conferem; roteiro `Scripts/v2/teste/roteiro_12_7.sql` 39/39 OK; v1 intacto (checksums); backup em `C:\Project\backup\labcontrol_v1_20261019.sql`.
- **Passos:** (1) `mysqldump` do banco `labcontrol` atual (v1) como backup; (2) executar `01_schema.sql` → `05_seed.sql` em ordem com `mysqlsh --sql … --file`; (3) conferir 59 tabelas + 10 views, 8 funções, 19 procedures, 42 triggers, 109 permissões; (4) executar o roteiro do [doc 12 §7](../banco/12-PROCEDURES-REVISOES.md) num banco temporário.
- **Arquivos:** nenhum (só banco).
- **Aceite:** contagens conferem; roteiro 12 §7 100% ok; banco antigo intacto.
- **Esforço:** S · **Depende de:** —

### 0.2 `Scripts/v2/README.md` + script de recriação
> ✅ **Concluída em 19/10/2026** — `.\Scripts\v2\recriar.ps1` recria o banco (contagens conferidas); `-Teste` roda o roteiro 12 §7 num banco temporário. Ver [Scripts/v2/README.md](../../Scripts/v2/README.md).
- **Passos:** documentar comandos de criação/recriação, ordem dos scripts, como rodar o roteiro de teste; criar `Scripts/v2/recriar.ps1` (parâmetro do banco, pede senha, roda os 5 scripts e para no primeiro erro).
- **Aceite:** um comando recria o banco do zero.
- **Esforço:** S · **Depende de:** 0.1

### 0.3 Novo `LabDbContext` (schema v2, sem migrations)
> ✅ **Concluída em 19/10/2026** — entidades e configurações **geradas** por `node tools/gerador-csharp/gerar.mjs` ([README](../../tools/gerador-csharp/README.md)); classes no singular, DbSet = tabela em PascalCase; v1 movida para `Legacy/` (antecipa o isolamento da 0.8); `tests/LabControl.Tests` 3/3 (modelo × `information_schema`, leitura de cada DbSet, gravação com triggers); build sem avisos.
- **Passos:**
  1. Criar `Data/LabDbContext.cs` e `Data/Configurations/*.cs` (uma `IEntityTypeConfiguration` por tabela, nomes das colunas do Firebird preservados — ver "Implementação no C#" de cada MD em [objetos/tabelas](../banco/objetos/README.md#tabelas)).
  2. Entidades em `Models/` (uma classe por tabela), enums para `ENUM` (`CATEGORIA`, `TIPO_DOCUMENTO`, `SITUACAO`, `STATUS`, `E_S`, `FINALIDADE`), `DateOnly`/`TimeOnly`, `bool` para `TINYINT(1)`.
  3. Colunas preenchidas por trigger marcadas como geradas pelo banco (`ValueGeneratedOnAdd`/`OnAddOrUpdate`) — lista em cada MD de tabela.
  4. Views como entidades keyless (`ToView`).
  5. **Remover** os "patches de schema" do `Program.cs` e o `ApplicationDbContext` antigo (manter no git); **não** usar `Database.Migrate()`/`EnsureCreated()`.
  6. Connection string `labcontrol_v2` via **user-secrets** (tirar senha do `appsettings.json`).
- **Aceite:** `dotnet build` sem avisos novos; teste de leitura de cada `DbSet` num banco vazio; nenhuma migration EF no projeto.
- **Esforço:** G · **Depende de:** 0.1

### 0.4 Contexto da empresa + Global Query Filter
- **Passos:** `Infra/EmpresaContexto.cs` (`IEmpresaContexto` scoped lendo as claims); interface `IPorEmpresa`; `HasQueryFilter` por convenção para toda entidade com `ID_EMPRESAS` (exceto globais); preenchimento no `SaveChanges`; middleware que exige empresa nas rotas autenticadas.
- **Aceite:** teste com 2 empresas: consultas não vazam; inclusão grava a empresa ativa; trocar `ID_EMPRESAS` num update é recusado.
- **Esforço:** M · **Depende de:** 0.3

### 0.5 Sessão MySQL, erros de negócio e chamadas de procedure
- **Passos:** `Data/Interceptors/SessaoMySqlInterceptor.cs` (`SET @LC_ID_USUARIOS, @LC_MIGRACAO = 0, @LC_PERMITE_RETROATIVO = 0` em toda conexão aberta); `Infra/Resultado.cs` (`Resultado`/`Resultado<T>`); `Infra/ErrosMySql.cs` (45000 → mensagem; 1062/1451/1452 → mensagens amigáveis); `Infra/Procedures.cs` com helpers para `SP_*` com OUT params (padrão do [P22](../processos/P22-erros-transacoes.md) e dos MDs de [rotinas](../banco/objetos/README.md#funções-e-procedures)); filtro MVC que converte `Resultado.Erro` em `ModelState`/`TempData`.
- **Aceite:** teste: trigger que recusa (ex.: data de entrega futura) devolve a mensagem exata; helper de `SP_GERAR_REVISAO_*` lê os OUT params; conexão do pool não herda variáveis de outro request.
- **Esforço:** M · **Depende de:** 0.3

### 0.6 Auditoria (infraestrutura)
- **Passos:** `Data/Interceptors/AuditoriaInterceptor.cs` (antes/depois em JSON para as tabelas da lista do [P21](../processos/P21-auditoria-log.md)); `IAuditoria.Registrar(acao, tabela, id, antes, depois)` para eventos (login, revisão, emissão).
- **Aceite:** alteração numa tabela auditada gera linha em `AUDITORIA` na mesma transação; blobs e hash de senha nunca vão para o JSON.
- **Esforço:** S · **Depende de:** 0.4

### 0.7 Projeto de testes
- **Passos:** `LabControl.Tests` (xUnit); fixture que cria um banco `labcontrol_teste_{guid}` pelos scripts e apaga ao final; testes do roteiro do [doc 12 §7](../banco/12-PROCEDURES-REVISOES.md) chamando o banco **pelos helpers C#**; testes de 0.4–0.6.
- **Aceite:** `dotnet test` verde, sem depender de dados manuais.
- **Esforço:** M · **Depende de:** 0.5

### 0.8 Estrutura de pastas e limpeza da v1
- **Passos:** criar `Services/`, `ViewModels/`, `Infra/`, `Data/Configurations/`; mover o layout/CSS reaproveitável; marcar controllers v1 como obsoletos (pasta `Legacy/` excluída da compilação ou removida quando a tela for refeita) — **não apagar antes de a tela nova existir**.
- **Aceite:** projeto compila só com o que usa o schema v2; telas v1 ainda acessíveis só se compilarem contra o novo contexto (senão removidas da navegação).
- **Esforço:** S · **Depende de:** 0.3

### 0.9 Revisão da fase
`/code-review` (nível high) sobre a fase; corrigir achados; atualizar o diário do plano.

---

## Definição de pronto da fase
- [ ] Banco v2 recriável por um comando; roteiro 12 §7 automatizado e verde.
- [ ] Filtro de empresa, sessão MySQL, erros 45000 e auditoria funcionando e testados.
- [ ] Sem migrations EF e sem patches de schema no `Program.cs`; segredos fora do git.

## Riscos
| Risco | Mitigação |
|---|---|
| Pomelo não mapear bem colunas geradas por trigger | reler entidade após `SaveChanges` (`Entry.ReloadAsync`) nos serviços críticos |
| Procedures com transação chamadas dentro de transação EF | helper único que verifica `Database.CurrentTransaction == null` e lança exceção em DEBUG |
| Pool de conexões levando variáveis de sessão | interceptor sempre zera; teste específico |
