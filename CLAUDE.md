# LabControl — instruções para o Claude

Responda sempre em português do Brasil.

## Contexto
Reescrita do sistema de laboratório **Delphi 7 + Firebird** (`C:\Project\Laboratorio`, banco `DBMAIS.FDB`) em **ASP.NET Core MVC .NET 10 + MySQL 8**, **multiempresa (tudo por empresa)**.

## Antes de qualquer tarefa
1. Leia `docs/00-PLANO-EXECUCAO.md` (fase atual, próxima tarefa, modelo/modo recomendado) e o arquivo da fase em `docs/plano/F*.md` (passos, arquivos, aceite).
2. Leia o MD da tela em `docs/telas/`, os processos citados em `docs/processos/` e as regras em `docs/regras/`.
3. Para cada tabela, view, rotina ou trigger tocada, leia o MD do objeto em `docs/banco/objetos/` (colunas preenchidas pelo banco, mensagens 45000, como chamar no C#).
4. Na dúvida sobre como o legado faz, consulte o código Delphi (`C:\Project\Laboratorio\LabControl\un_*.pas` e `.dfm`) — o MD da tela indica o arquivo.

## Regras que não podem ser quebradas
- **Schema = `Scripts/v2/*.sql`** (banco `labcontrol_v2`). Não usar EF migrations nem "patches" no `Program.cs`. Mudou o banco? Alterar o script, atualizar `docs/banco/11` e o MD do objeto em `docs/banco/objetos/`, rodar o roteiro do doc 12 §7 e regenerar as entidades (`node tools/gerador-csharp/gerar.mjs`).
- **Nunca editar `*.g.cs`** (entidades/configurações geradas): ajustes nos arquivos parciais sem `.g`.
- Código v1 fica em `Legacy/` (fora da compilação), só como referência.
- **Toda consulta filtra a empresa ativa** (Global Query Filter). Nunca `?? 1` para empresa.
- **Número da amostra/proposta/pedido é gerado pelo banco** (triggers + `SP_PROXIMO_NUMERO`, ano do servidor). A aplicação nunca calcula `MAX+1` nem o ano.
- **Revisões** (proposta, amostra, resultado, boletim) só pelas procedures `SP_GERAR_REVISAO_*`, fora de transação do EF. Revisão antiga e boletim emitido são somente leitura.
- Erros SQLSTATE `45000` são mensagens de negócio: mostrar ao usuário.
- Ao concluir: marcar o checklist do plano e do arquivo da fase, atualizar o "Status" do MD da tela, a matriz `docs/plano/MATRIZ-DELPHI-CSHARP.md` e o Diário.
- **Todo commit é seguido de `git push`** para o remoto (`origin`, branch `master`) — pedido do usuário (21/10/2026). Se o push falhar, avisar o usuário com a mensagem do erro.

## Comandos
- Recriar banco: `.\Scripts\v2\recriar.ps1` (senha lida do user-secret da connection string, sem pergunta; `-Teste` = banco temporário + roteiro do doc 12 §7; `-Dev` = empresa 001 + usuário `ADMIN` / `Admin123`, troca obrigatória no 1º acesso). Detalhes em `Scripts/v2/README.md`.
- Firebird legado: `"C:\Program Files (x86)\Firebird\Firebird_2_5\bin\isql.exe" -user SYSDBA -password masterkey localhost:C:\Project\Laboratorio\DBMAIS.FDB`.
- Executar: `dotnet run` (ou `rodar.bat`). Connection string em user-secrets: `dotnet user-secrets set "ConnectionStrings:LabControl" "Server=localhost;Port=3306;Database=labcontrol_v2;User=root;Password=<senha>"`. **Senha nunca em arquivo versionado**: onde fica e como trocar em `Scripts/v2/README.md` › "Senha do MySQL".
- Gerar entidades: `node tools/gerador-csharp/gerar.mjs`. Testes: `dotnet test tests/LabControl.Tests` (usam o banco `labcontrol_teste`, recriado sozinho quando `Scripts/v2` muda — a 1ª execução leva ~3 min). Testes no navegador (CRUD real das telas, Edge sem janela): `--filter "FullyQualifiedName~Navegador"` — ver `docs/processos/P26-testes-ponta-a-ponta.md`; não rodar com o `dotnet run` aberto (trava o build).
- Serviços: retornar `Resultado`/`Resultado<T>`; procedures só por `Infra/Procedures.cs`; erros MySQL por `ErrosMySql` (ver `docs/processos/P22-erros-transacoes.md`).
