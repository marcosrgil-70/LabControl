# LabControl — instruções para o Claude

Responda sempre em português do Brasil.

## Idioma das mensagens (pedido do usuário, 27/10/2026)
**Todas as mensagens para o usuário têm que ser em português do Brasil** — sem exceção:
- respostas, resumos de fim de tarefa ou de fase, planos, perguntas e avisos de andamento ("estou fazendo X");
- textos curtos entre uma ferramenta e outra, inclusive enquanto espera testes ou tarefas em segundo plano;
- mensagens de commit, comentários no código, documentação e textos das telas.

Só ficam no original os nomes técnicos (classes, métodos, comandos, mensagens de erro copiadas de ferramentas). Antes de enviar um resumo, releia: se alguma frase saiu em inglês, reescreva em português.

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
- **Usuário `ADMINISTRADOR` / senha `Admin123` em desenvolvimento e testes** — pedido do usuário (23/10/2026):
  - sempre entra e tem acesso a tudo (ADMIN_GLOBAL), **sem troca obrigatória de senha**;
  - vinculado ao profissional **"USUÁRIO ESPECIAL DO SISTEMA"** (criado se não existir, em cada empresa) para poder gravar o que registra o profissional do usuário (proposta, amostra…);
  - vale em todo banco de desenvolvimento/teste: o `recriar.ps1 -Dev` cria assim (`Scripts/v2/dev/admin_dev.sql`), e a migração (M-09) deixa o ADMINISTRADOR migrado assim quando `Desenvolvimento:AdministradorComSenhaPadrao = true` (`appsettings.Development.json`);
  - constantes em `ContaService.LoginAdministrador`/`SenhaDesenvolvimento`;
  - **nunca em produção**: lá o ADMINISTRADOR migrado recebe a senha provisória da T07 e troca no 1º acesso;
  - num banco de desenvolvimento já existente: senha com o mesmo hash do `admin_dev.sql` e o profissional pelo bloco final do mesmo arquivo (idempotente).
- **Antes de começar uma nova tarefa, commit + push** do que estiver pendente (`git status`; nada pendente = seguir) — pedido do usuário (23/10/2026). Assim cada tarefa começa sobre um ponto salvo no remoto.

## Comandos
- Recriar banco: `.\Scripts\v2\recriar.ps1` (senha lida do user-secret da connection string, sem pergunta; `-Teste` = banco temporário + roteiro do doc 12 §7; `-Dev` = empresa 001 + usuário `ADMINISTRADOR` / `Admin123`, sem troca de senha, vinculado ao profissional "USUÁRIO ESPECIAL DO SISTEMA"). Detalhes em `Scripts/v2/README.md`.
- Firebird legado: `"C:\Program Files\Firebird\Firebird_2_5\bin\isql.exe" -user SYSDBA -password masterkey localhost:C:\Project\Laboratorio\DBMAIS.FDB`.
- Executar: `dotnet run` (ou `rodar.bat`). Connection string em user-secrets: `dotnet user-secrets set "ConnectionStrings:LabControl" "Server=localhost;Port=3306;Database=labcontrol_v2;User=root;Password=<senha>"`. **Senha nunca em arquivo versionado**: onde fica e como trocar em `Scripts/v2/README.md` › "Senha do MySQL".
- Gerar entidades: `node tools/gerador-csharp/gerar.mjs`. Testes: `dotnet test tests/LabControl.Tests` (usam o banco `labcontrol_teste`, recriado sozinho quando `Scripts/v2` muda — a 1ª execução leva ~3 min). Suíte inteira de uma vez, ~17 min (`MigracaoTests` roda sozinho no fim, coleção sem paralelismo — P26 §4). Testes no navegador (CRUD real das telas, Edge sem janela): `--filter "FullyQualifiedName~Navegador"` — ver `docs/processos/P26-testes-ponta-a-ponta.md`; não rodar com o `dotnet run` aberto (trava o build).
- Serviços: retornar `Resultado`/`Resultado<T>`; procedures só por `Infra/Procedures.cs`; erros MySQL por `ErrosMySql` (ver `docs/processos/P22-erros-transacoes.md`).
