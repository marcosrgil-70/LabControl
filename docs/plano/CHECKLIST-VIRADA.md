# Checklist da virada — LabControl v2 (tarefa 9.3)

**Quando:** depois da homologação (9.2) sem ocorrência "impede" aberta e com o layout do boletim aprovado (5.4).
**Quem:** quem conduz a implantação, com o responsável do laboratório para o "pode abrir".
**Retorno:** se houver bloqueio grave no 1º dia, o laboratório volta ao Delphi — o v2 **não escreve** no Firebird, que fica intacto ([F9 § 9.3](F9-homologacao-virada.md#93-virada)).

> Marque cada item. O que for anotado aqui (endereços, pastas, nomes de usuário) **não** inclui senhas: senha só no cofre/papel do responsável, nunca em arquivo versionado nem neste MD.

---

## A. Servidor (dias antes)
- [ ] Windows com **IIS** + *ASP.NET Core Hosting Bundle* do **.NET 10**; **MySQL 8** com `--default-character-set=utf8mb4`; Firebird 2.5 (32 bits) só para a última migração e a consulta histórica.
- [ ] **HTTPS** com certificado válido no site do IIS (o cookie de login é `Secure`: sem HTTPS ninguém entra). Endereço: ______________________
- [ ] Publicação: `dotnet publish LabControl.csproj -c Release -o <pasta>` → copiar para a pasta do site. Pasta do app: ______________________
- [ ] Pool de aplicativos: "Sem código gerenciado", identidade própria (ex.: `IIS AppPool\LabControl`).
- [ ] Pastas **fora** da pasta do app (uma nova publicação não pode apagá-las), com permissão de **gravação** só para a identidade do pool:
  - boletins emitidos: ______________________ (ex.: `D:\LabControl\boletins`)
  - chaves do Data Protection: ______________________ (ex.: `D:\LabControl\chaves`) — leitura só para o pool e os administradores (as chaves decifram a senha do e-mail)
- [ ] `appsettings.Production.json` na pasta do app (**nunca no git** — já está no `.gitignore`), sem senha:
  ```json
  {
    "AllowedHosts": "<nome do servidor>",
    "Boletins": { "Pasta": "D:\\LabControl\\boletins" },
    "DataProtection": { "PastaChaves": "D:\\LabControl\\chaves" }
  }
  ```
- [ ] Connection string como **variável de ambiente** do pool (`ConnectionStrings__LabControl`, editor de configuração do IIS › `environmentVariables`) — não no `appsettings`.
- [ ] Usuário MySQL **próprio do app** (não o `root` — pendência 12):
  ```sql
  CREATE USER 'labcontrol'@'localhost' IDENTIFIED BY '<senha>';
  GRANT SELECT, INSERT, UPDATE, DELETE, EXECUTE ON labcontrol_v2.* TO 'labcontrol'@'localhost';
  ```
  Use um usuário com essas mesmas permissões (no `labcontrol_migracao`) já na homologação (9.2), para ver que bastam. A migração da T07 continua com um usuário que pode recriar o banco.
- [ ] Senha do `root` do MySQL trocada (pendência 12; [Scripts/v2/README.md › Senha do MySQL](../../Scripts/v2/README.md#senha-do-mysql-onde-fica-e-como-trocar)).
- [ ] Impressora **Argox** instalada no servidor (pendência 21).

## B. Backup (antes de abrir para os usuários)
- [ ] Usuário MySQL só de backup e credencial guardada no perfil do Windows que roda a tarefa (sem senha em arquivo):
  ```sql
  CREATE USER 'labcontrol_backup'@'localhost' IDENTIFIED BY '<senha>';
  GRANT SELECT, SHOW VIEW, TRIGGER, EVENT, LOCK TABLES, SHOW_ROUTINE ON *.* TO 'labcontrol_backup'@'localhost';
  ```
  ```powershell
  mysql_config_editor set --login-path=labcontrol_backup --host=127.0.0.1 --user=labcontrol_backup --password
  ```
- [ ] Tarefa diária no Agendador de Tarefas (com o mesmo usuário do Windows do passo anterior):
  `powershell -NoProfile -ExecutionPolicy Bypass -File <repo>\Scripts\v2\backup.ps1 -Destino <pasta de backup> -PastaApp <pasta do app> -LoginPath labcontrol_backup -Usuario labcontrol_backup`
  — gera `labcontrol_v2_<data>.zip` com banco + boletins + chaves; termina com erro (código 1) se algo falhar; guarda 30 dias. Detalhes em [Scripts/v2/README.md › Backup](../../Scripts/v2/README.md#backup-banco--boletins--chaves).
- [ ] Cópia do zip **para fora do servidor** (outro disco, nuvem ou mídia) — o zip tem dados de clientes e as chaves: destino com acesso restrito.
- [ ] **Teste de restauração** uma vez: `backup.ps1 … -Conferir` (restaura num banco temporário e compara as linhas de todas as tabelas).

## C. Dia da virada
1. [ ] Avisar o laboratório: a partir de ____:____ ninguém lança nada no Delphi.
2. [ ] **Backup final do Firebird** com o Delphi fechado: `gbak -b -user SYSDBA -password <senha> DBMAIS.FDB DBMAIS-<data>.fbk` (guardar junto com os backups da 9.1).
3. [ ] **Banco de produção do zero, sem `-Dev`** (sem o ADMINISTRADOR/Admin123 — regra do CLAUDE.md): `.\Scripts\v2\recriar.ps1 -Banco labcontrol_v2`.
4. [ ] **Última migração** (T07) a partir da cópia congelada: na máquina da migração, o app num banco de operação (criado com `-Dev`, apagado no fim) → *Sistema › Migração* → banco destino `labcontrol_v2`, **senha provisória nova** (não a da homologação). Esperado: 18 etapas ok e **todas as conferências verdes** (na 9.1: 74/74).
5. [ ] Relatório de anomalias salvo fora do repositório (`C:\Project\backup\migracao-<data>.csv`); conferir que as decisões da pendência 39 foram aplicadas.
6. [ ] Conferir no banco de produção: `SELECT LOGIN FROM USUARIOS WHERE LOGIN = 'ADMINISTRADOR' AND TROCAR_SENHA = 0;` → **nenhuma linha** (o ADMINISTRADOR migrado troca a senha no 1º acesso).
7. [ ] App de produção apontando para `labcontrol_v2` (variável do pool) e reiniciado; *Sistema › Parâmetros da Empresa*: conferir numeração, período de pesquisa e **e-mail** (digitar a senha do e-mail de novo — as chaves de produção são novas) → "Enviar e-mail de teste".
8. [ ] **Primeiro backup** pela tarefa agendada (rodar à mão) e conferir o zip (boletins e chaves dentro).
9. [ ] Fumaça com o responsável: login → abrir uma amostra migrada → 2ª via de um boletim migrado → receber uma amostra nova → etiqueta na Argox.
10. [ ] Entregar a senha provisória a cada usuário (pessoalmente); cada um troca no 1º acesso.
11. [ ] Delphi: tirar o atalho das estações; Firebird **somente leitura** para consulta histórica por 6 meses (`gfix -mode read_only`).

## D. Depois da virada
- [ ] 1º dia acompanhado no laboratório; ocorrências na tabela do [roteiro de homologação](ROTEIRO-HOMOLOGACAO.md#registro-de-ocorrências).
- [ ] 1ª semana: conferir diariamente que o backup rodou (zip novo no destino e na cópia externa).
- [ ] Depois de 6 meses: desligar o Firebird (o `.fbk` final continua guardado).
- [ ] Marcar a 9.3 e a definição de pronto da [F9](F9-homologacao-virada.md#definição-de-pronto) no plano.
