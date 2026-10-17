# LabControl — instruções para o Claude

Responda sempre em português do Brasil.

## Contexto
Reescrita do sistema de laboratório **Delphi 7 + Firebird** (`C:\Project\Laboratorio`, banco `DBMAIS.FDB`) em **ASP.NET Core MVC .NET 10 + MySQL 8**, **multiempresa (tudo por empresa)**.

## Antes de qualquer tarefa
1. Leia `docs/00-PLANO-EXECUCAO.md` (fase atual, próxima tarefa, modelo/modo recomendado).
2. Leia o MD da tela em `docs/telas/` e as tabelas em `docs/banco/11-MODELO-MYSQL.md`.
3. Na dúvida sobre como o legado faz, consulte o código Delphi (`C:\Project\Laboratorio\LabControl\un_*.pas` e `.dfm`) — o MD da tela indica o arquivo.

## Regras que não podem ser quebradas
- **Schema = `Scripts/v2/*.sql`** (banco `labcontrol_v2`). Não usar EF migrations nem "patches" no `Program.cs`. Mudou o banco? Alterar o script, atualizar `docs/banco/11` e rodar o roteiro do doc 12 §7.
- **Toda consulta filtra a empresa ativa** (Global Query Filter). Nunca `?? 1` para empresa.
- **Número da amostra/proposta/pedido é gerado pelo banco** (triggers + `SP_PROXIMO_NUMERO`, ano do servidor). A aplicação nunca calcula `MAX+1` nem o ano.
- **Revisões** (proposta, amostra, resultado, boletim) só pelas procedures `SP_GERAR_REVISAO_*`, fora de transação do EF. Revisão antiga e boletim emitido são somente leitura.
- Erros SQLSTATE `45000` são mensagens de negócio: mostrar ao usuário.
- Ao concluir: marcar o checklist do plano, atualizar o "Status" do MD da tela e o Diário.

## Comandos
- Recriar banco: `mysqlsh --sql "root:<senha>@localhost:3306" --file Scripts/v2/01_schema.sql` (e 02..05, em ordem).
- Firebird legado: `"C:\Program Files (x86)\Firebird\Firebird_2_5\bin\isql.exe" -user SYSDBA -password masterkey localhost:C:\Project\Laboratorio\DBMAIS.FDB`.
- Executar: `dotnet run` (ou `rodar.bat`).
