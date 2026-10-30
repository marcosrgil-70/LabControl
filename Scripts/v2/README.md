# Scripts/v2 — banco MySQL do LabControl v2

O schema do banco é **somente** o que está nestes scripts (sem EF migrations, sem "patches" no `Program.cs`).
Documentação: [doc 11 — modelo](../../docs/banco/11-MODELO-MYSQL.md) · [doc 12 — procedures e revisões](../../docs/banco/12-PROCEDURES-REVISOES.md) · [MDs por objeto](../../docs/banco/objetos/README.md).

## Arquivos (ordem de execução)

| # | Arquivo | Conteúdo |
|---|---|---|
| 1 | `01_schema.sql` | `CREATE DATABASE labcontrol_v2` + 59 tabelas |
| 2 | `02_funcoes_procedures.sql` | 8 funções + 27 procedures (`SP_PROXIMO_NUMERO`, `PR_AUDITAR`, `SP_GERAR_REVISAO_*`, `SP_EMITIR_BOLETIM`…) |
| 3 | `03_triggers.sql` | 48 triggers (numeração, travas de revisão/emissão, saldo, log, contatos das entidades) |
| 4 | `04_views.sql` | 10 views |
| 5 | `05_seed.sql` | Brasil + 27 UFs e catálogo de 109 permissões |
| 6 | `06_cidades.sql` | 5.571 municípios do IBGE (gerado por `tools/gerador-cidades`) |
| — | `teste/roteiro_12_7.sql` | roteiro de teste do doc 12 §7 (74 verificações) — **só em banco temporário** |
| — | `recriar.ps1` | recria um banco do zero e confere as contagens; `-Teste` roda o roteiro; `-Dev` roda `dev/*.sql` |
| — | `atualizar.ps1` | atualiza um banco que **já existe** com o que falta nos scripts, sem apagar dados (o instalador usa na atualização) — [abaixo](#atualizar-um-banco-existente-sem-apagar-dados) |
| — | `backup.ps1` | backup num `.zip`: banco (`mysqldump`) + pasta dos boletins + chaves do Data Protection; `-Conferir` restaura num banco temporário e compara ([abaixo](#backup-banco--boletins--chaves)) |
| — | `inicial.sql` | banco inicial: empresa `001` (nome em `@EMPRESA_RAZAO`) + usuário `ADMINISTRADOR` / senha `Admin123` (ADMIN_GLOBAL, sem troca de senha, vinculado ao profissional "USUÁRIO ESPECIAL DO SISTEMA") — rodado pelo `recriar.ps1 -Dev` (desenvolvimento) ou `-Empresa "<nome>"` (instalador; decisão do usuário, 29/10/2026) |

Empresas, usuários e cadastros **não** estão no seed: vêm da migração ([doc 13](../../docs/banco/13-MIGRACAO-DADOS.md)) ou do cadastro de nova empresa (`SP_INICIALIZAR_EMPRESA`).

## Recriar o banco (um comando)

```powershell
.\Scripts\v2\recriar.ps1                 # recria labcontrol_v2 (se o banco existir, pede confirmação)
.\Scripts\v2\recriar.ps1 -Dev            # idem + empresa 001 e usuário ADMINISTRADOR / Admin123 (desenvolvimento)
.\Scripts\v2\recriar.ps1 -Banco LabControlDB -Empresa "LABORATÓRIO X LTDA" -Forcar   # o que o instalador faz no banco zerado
.\Scripts\v2\recriar.ps1 -Teste          # cria labcontrol_teste_<id>, roda o roteiro 12 §7 e apaga o banco
.\Scripts\v2\recriar.ps1 -Teste -Manter  # idem, mas mantém o banco temporário para inspeção
```

Parâmetros: `-Banco` (padrão `labcontrol_v2`), `-Servidor` (`127.0.0.1`), `-Porta` (`3306`), `-Usuario` (`root`), `-MySql` (caminho do `mysql.exe`; padrão: o do PATH ou `C:\Program Files\MySQL\MySQL Server 8.0\bin`), `-Forcar` (não pede confirmação para apagar).

O script:
1. **apaga** o banco informado (recusa `labcontrol` — o v1 — e os bancos do sistema);
2. roda os 5 scripts em ordem, trocando `labcontrol_v2` pelo nome do banco, e **para no primeiro erro**;
3. confere as contagens (59 tabelas, 10 views, 8 funções, 27 procedures, 48 triggers, 109 permissões, 5.571 cidades) — se o schema mudou de propósito, atualize `$esperado` no script;
4. com `-Teste`, roda `teste/roteiro_12_7.sql` e falha se alguma verificação falhar.

Senha, nesta ordem: `$env:MYSQL_PWD` (ex.: CI) → a do user-secret `ConnectionStrings:LabControl` (quando o `-Usuario` é o mesmo da connection string) → pergunta no console.
Leva ~3 minutos por banco no MySQL local (DDL lento no Windows).

## Senha do MySQL (onde fica e como trocar)

A senha **nunca** vai para arquivo versionado (`appsettings*.json`, scripts, docs). Ela fica só em:

| Quem usa | Onde | Observação |
|---|---|---|
| App (`dotnet run`) e testes (`dotnet test`) | user-secret `ConnectionStrings:LabControl` — arquivo `%APPDATA%\Microsoft\UserSecrets\labcontrol-v2-0b6c1d2e\secrets.json` (fora do repositório) | o projeto de testes usa o mesmo `UserSecretsId` e troca só o nome do banco para `labcontrol_teste` |
| `recriar.ps1` | `$env:MYSQL_PWD` da sessão, senão lê o mesmo user-secret acima, senão pergunta no console | não grava em lugar nenhum |
| Migração (Firebird) | user-secret `Firebird:Password` | idem |
| CI / outra máquina | variável de ambiente `LABCONTROL_TESTE_CS` (testes) ou `ConnectionStrings__LabControl` (app) | definida no ambiente, não no repositório |

**Trocar a senha** (ex.: a antiga ficou no histórico do git até a tarefa 0.3):
1. No MySQL: `mysql -u root -p` → `ALTER USER 'root'@'localhost' IDENTIFIED BY '<nova senha>';` (repita para `'root'@'%'`, se existir).
2. No projeto (vale para o app **e** para os testes):
   ```powershell
   dotnet user-secrets set "ConnectionStrings:LabControl" "Server=localhost;Port=3306;Database=labcontrol_v2;User=root;Password=<nova senha>" --project LabControl.csproj
   ```
3. Conferir: `dotnet user-secrets list --project LabControl.csproj` e `dotnet test tests/LabControl.Tests` (conecta no `labcontrol_teste` com a senha nova).

Homologação/produção: um usuário próprio do app em vez do `root` (ex.: `CREATE USER 'labcontrol'@'localhost' IDENTIFIED BY '…'; GRANT SELECT, INSERT, UPDATE, DELETE, EXECUTE ON labcontrol_v2.* TO 'labcontrol'@'localhost';` — [checklist da virada](../../docs/plano/CHECKLIST-VIRADA.md)). Para rodar os testes ele precisaria de `GRANT ALL ON labcontrol_teste.*`, porque os testes recriam esse banco.

## Backup (banco + boletins + chaves)

```powershell
.\Scripts\v2\backup.ps1 -Destino C:\Project\backup                              # labcontrol_v2, pastas do appsettings.json
.\Scripts\v2\backup.ps1 -Destino C:\Project\backup -Banco labcontrol_migracao -Conferir
.\Scripts\v2\backup.ps1 -Destino D:\Backup -PastaApp C:\inetpub\LabControl -LoginPath labcontrol_backup -Usuario labcontrol_backup   # produção (tarefa agendada)
```

Gera `<banco>_<aaaaMMdd-HHmm>.zip` com:
- `banco.sql`: `mysqldump --single-transaction --routines --triggers --events`, que não trava o uso;
- `boletins/`: a pasta `Boletins:Pasta`;
- `chaves/`: a pasta `DataProtection:PastaChaves`;
- `manifesto.txt`: data, SHA-256 do dump e quantidade de arquivos.

As duas pastas vêm do `appsettings.json` e do `appsettings.Production.json` da `-PastaApp`, com as mesmas regras do `Program.cs`: relativas à pasta do app ou absolutas. Também podem ser informadas com `-PastaBoletins` e `-PastaChaves`.

O que o script confere e faz no fim:
- o dump precisa terminar com `-- Dump completed`;
- com `-Conferir`, restaura o dump num banco temporário `labcontrol_conf_<id>`, compara a quantidade de linhas de cada tabela e apaga o temporário;
- apaga os zips do mesmo banco com mais de `-ManterDias` dias (padrão 30);
- termina com código 1 em qualquer erro, para o Agendador de Tarefas avisar.

Na 9.3, o `labcontrol_migracao` (33,5 MB de dump, 2,8 MB zipado) restaurou e conferiu 59 tabelas e 148.062 linhas em 18 s.

**Senha**, nesta ordem:
1. `-LoginPath`, a credencial guardada por `mysql_config_editor set --login-path=…` no perfil do Windows que roda a tarefa (o recomendado em produção);
2. `MYSQL_PWD`;
3. `-ArquivoSenha`, a senha cifrada pelo DPAPI da máquina (é a tarefa criada pelo [instalador](../../instalador/README.md));
4. o user-secret (desenvolvimento);
5. pergunta no console.

Usuário só de backup: `GRANT SELECT, SHOW VIEW, TRIGGER, EVENT, LOCK TABLES, SHOW_ROUTINE ON *.*` (conferido em 29/10/2026: o dump sai com as 27 procedures, as 8 funções, os 48 triggers e as 10 views).

**O zip tem a senha do e-mail cifrada e as chaves que a decifram**: guarde-o em lugar com acesso restrito.

**Restaurar:**
1. `mysql -u root -p < banco.sql`, que recria o banco com o mesmo nome. Para outro nome, troque o `CREATE DATABASE`/`USE` do início.
2. Copie `boletins/` e `chaves/` para as pastas configuradas no app.

## Manualmente

```powershell
mysqlsh --sql "root@localhost:3306" --file Scripts/v2/01_schema.sql   # e 02..05, nesta ordem
```
ou com o cliente clássico: `mysql -u root -p --default-character-set=utf8mb4 < Scripts/v2/01_schema.sql`.

Os scripts têm `USE labcontrol_v2` fixo. Para outro nome, use o `recriar.ps1 -Banco <nome>` (ele faz a troca numa cópia temporária).

## Roteiro de teste (doc 12 §7)

**Reexecutar após qualquer mudança em 01..06:** `.\Scripts\v2\recriar.ps1 -Teste`.
## Atualizar um banco existente (sem apagar dados)

Pedido do usuário (30/10/2026): o instalador pergunta, quando o banco já existe, se **atualiza** ou **apaga e cria de novo**. A atualização é o `atualizar.ps1`, que também serve para os bancos de desenvolvimento e de migração:

```powershell
.\Scripts\v2\atualizar.ps1 -Banco labcontrol_migracao -SoMostrar   # só lista o que mudaria
.\Scripts\v2\atualizar.ps1 -Banco labcontrol_migracao             # aplica
.\Scripts\v2\atualizar.ps1 -Banco LabControlDB -CopiaAntes D:\backup\antes.sql   # cópia (mysqldump) antes de mudar
```

1. cria um banco de referência temporário pelo `recriar.ps1` (os mesmos scripts);
2. compara tabela por tabela (`SHOW CREATE TABLE`): cria as tabelas que faltam, acrescenta as colunas que faltam (na posição dos scripts), ajusta colunas com definição diferente (`MODIFY`, que **para no erro** se os dados não couberem — nada é truncado) e troca índices, UNIQUE, CHECK e chaves estrangeiras diferentes; tabelas e colunas que existem só no banco ficam como estão, com aviso;
3. apaga e recria todas as views, funções, procedures e triggers (02, 03, 04);
4. reaplica o `05_seed` (`INSERT IGNORE`) e roda `SP_INICIALIZAR_EMPRESA` em cada empresa (o perfil ADMINISTRADOR recebe as permissões novas);
5. confere as contagens com a referência e apaga a referência.

Testado em 30/10/2026: banco dos scripts de antes da 8.3 (13 mudanças, inclusive um `MODIFY`), tabela, índice, chave estrangeira e permissões apagados de propósito (tudo voltou), e o `labcontrol_migracao` (as 2 colunas do selo, D-28). A segunda execução não encontra mais nada.

O roteiro cria uma empresa e cadastros de teste, então **nunca** rode no `labcontrol_v2` real. Cada verificação gera uma linha OK/FALHA; o script termina com erro (`SQLSTATE 45000`) se houver falha.

## Mudou o banco?

1. Altere o script (01..06);
2. atualize o [doc 11](../../docs/banco/11-MODELO-MYSQL.md) e o MD do objeto em `docs/banco/objetos/`;
3. rode `.\Scripts\v2\recriar.ps1 -Teste` (e acrescente verificações ao roteiro se a regra for nova);
4. recrie o `labcontrol_v2` local (ou atualize os bancos que não podem ser apagados, como o `labcontrol_migracao`, com o `atualizar.ps1`);
5. regenere as entidades C#: `node tools/gerador-csharp/gerar.mjs` (view mudou? antes atualize `views.json` — ver [tools/gerador-csharp/README.md](../../tools/gerador-csharp/README.md)) e rode `dotnet test tests/LabControl.Tests`.
