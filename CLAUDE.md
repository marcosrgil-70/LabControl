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
- **Schema = `Scripts/v2/*.sql`** (banco `LabControlDB`). Não usar EF migrations nem "patches" no `Program.cs`. Mudou o banco? Alterar o script, atualizar `docs/banco/11` e o MD do objeto em `docs/banco/objetos/`, rodar o roteiro do doc 12 §7 e regenerar as entidades (`node tools/gerador-csharp/gerar.mjs`).
- **Nunca editar `*.g.cs`** (entidades/configurações geradas): ajustes nos arquivos parciais sem `.g`.
- Código v1 fica em `Legacy/` (fora da compilação), só como referência.
- **Toda consulta filtra a empresa ativa** (Global Query Filter). Nunca `?? 1` para empresa.
- **Número da amostra/proposta/pedido é gerado pelo banco** (triggers + `SP_PROXIMO_NUMERO`, ano do servidor). A aplicação nunca calcula `MAX+1` nem o ano.
- **Revisões** (proposta, amostra, resultado, boletim) só pelas procedures `SP_GERAR_REVISAO_*`, fora de transação do EF. Revisão antiga e boletim emitido são somente leitura.
- Erros SQLSTATE `45000` são mensagens de negócio: mostrar ao usuário.
- Ao concluir: marcar o checklist do plano e do arquivo da fase, atualizar o "Status" do MD da tela, a matriz `docs/plano/MATRIZ-DELPHI-CSHARP.md` e o Diário.
- **Todo commit é seguido de `git push`** para o remoto (`origin`, branch `master`) — pedido do usuário (21/10/2026). Se o push falhar, avisar o usuário com a mensagem do erro.
- **Usuário `ADMINISTRADOR` / senha `Admin123`** — pedido do usuário (23/10/2026, desenvolvimento e testes; 29/10/2026, também na instalação):
  - sempre entra e tem acesso a tudo (ADMIN_GLOBAL), **sem troca obrigatória de senha**;
  - vinculado ao profissional **"USUÁRIO ESPECIAL DO SISTEMA"** (criado se não existir, em cada empresa) para poder gravar o que registra o profissional do usuário (proposta, amostra…);
  - o `Scripts/v2/inicial.sql` cria assim: `recriar.ps1 -Dev` (desenvolvimento) e `recriar.ps1 -Empresa "<nome>"` (o **instalador**, banco zerado `LabControlDB`);
  - a migração (M-09) deixa o ADMINISTRADOR migrado assim quando `Desenvolvimento:AdministradorComSenhaPadrao = true` (`appsettings.Development.json`) **ou** quando a T07 migra para o próprio banco recém-instalado;
  - constantes em `ContaService.LoginAdministrador`/`SenhaDesenvolvimento`;
  - **produção (decisão do usuário, 29/10/2026):** na instalação nova o ADMINISTRADOR fica **fixo com `Admin123`, sem troca obrigatória**, para entrar e migrar o legado pela T07 no mesmo banco; banco restaurado de backup mantém os usuários e senhas do backup; migração para um banco separado continua com a senha provisória da T07 para todos (inclusive o ADMINISTRADOR, fora do desenvolvimento);
  - num banco de desenvolvimento já existente: senha com o mesmo hash do `inicial.sql` e o profissional pelo bloco final do mesmo arquivo (idempotente).
- **Um só banco permanente: `LabControlDB`, com os dados do legado** — decisão do usuário (30/10/2026). É nele que se programa, roda o sistema e testa à mão (o mesmo nome do instalador). Os testes automáticos usam bancos **temporários** `lc_teste_*`, criados e apagados a cada execução. Não criar outros bancos permanentes (`labcontrol_v2`, `labcontrol_teste`, `labcontrol_migracao`… foram apagados). Mudou um script? `atualizar.ps1 -Banco LabControlDB` (mantém os dados); começar de novo com o legado do dia: `carregar-legado.ps1`.
- **Prazo em dias úteis** — decisão do usuário (30/10/2026): prazo com "Dias úteis" marcado conta só dias úteis (pula sábado e domingo); desmarcado, dias corridos. Calculado pelo banco em `FN_DATA_PREVISTA` (D-29).
- **2ª via do boletim é idêntica à 1ª** — decisão do usuário (30/10/2026): sem faixa de aviso, com as assinaturas e o mesmo nome de arquivo (o papel pode ter se danificado e a reemissão vai para os órgãos competentes e para o cliente).
- **Antes de começar uma nova tarefa, commit + push** do que estiver pendente (`git status`; nada pendente = seguir) — pedido do usuário (23/10/2026). Assim cada tarefa começa sobre um ponto salvo no remoto.

## Auditoria SABOTADOR (pedido do usuário, 09/11/2026)
Toda tarefa com impacto funcional passa pelo subagente `sabotador` antes de ser concluída (fluxo na skill global `auditoria-sabotador`; passo do `concluir-tarefa`). O que ele precisa atacar aqui, além do roteiro geral:
- **Multiempresa:** ler, alterar ou excluir registro de outra empresa trocando o id na URL/formulário; consulta que escapa do Global Query Filter (`IgnoreQueryFilters`, SQL cru, procedure, view).
- **Permissões:** action de escrita sem `[Permissao]`/`[SomenteAdminGlobal]`, item de menu e action com permissões diferentes, chamada direta ao endpoint por usuário sem o perfil.
- **Banco:** numeração (`SP_PROXIMO_NUMERO`) em paralelo, revisões fora das `SP_GERAR_REVISAO_*`, edição de revisão antiga ou de boletim emitido, mensagens `45000` engolidas ou trocadas por erro genérico.
- **Ambiente:** testes só em bancos temporários `lc_teste_*` (criados pelos testes); **nunca gravar no `LabControlDB`** (consulta de leitura pode). Não rodar testes com o `dotnet run` aberto.
- **Testes de reprodução/regressão** em `tests/LabControl.Tests/` no padrão da classe do módulo; relatório completo pode ir em `docs/auditorias/aaaa-mm-dd-<assunto>.md`.

## Comandos
- Carregar o legado no banco de trabalho: `.\Scripts\v2\carregar-legado.ps1` (**apaga** o `LabControlDB`, recria como o instalador e migra o `DBMAIS.FDB` pela T07, ~11 min; login `ADMINISTRADOR` / `Admin123`).
- Recriar banco: `.\Scripts\v2\recriar.ps1` (senha lida do user-secret da connection string, sem pergunta; `-Teste` = banco temporário + roteiro do doc 12 §7 — **o uso do dia a dia**; `-Dev` = **apaga** o `LabControlDB` e cria vazio com a empresa 001 + usuário `ADMINISTRADOR` / `Admin123`, sem troca de senha, vinculado ao profissional "USUÁRIO ESPECIAL DO SISTEMA"; `-Empresa "<nome>"` = o mesmo com o nome da empresa, usado pelo instalador). Detalhes em `Scripts/v2/README.md`.
- Atualizar um banco que já existe (sem apagar dados; o instalador usa o mesmo): `.\Scripts\v2\atualizar.ps1 -Banco <nome>` (`-SoMostrar` só lista). Ver `Scripts/v2/README.md`.
- Firebird legado: `"C:\Program Files (x86)\Firebird\Firebird_2_5\bin\isql.exe" -user SYSDBA -password masterkey localhost:C:\Project\Laboratorio\DBMAIS.FDB`.
  - O `DBMAIS.FDB` recebe cópias novas do laboratório: desde 29/10/2026 ele tem ~10× os dados da 9.1.
  - O `MigracaoTests` lê a cópia fixa da 9.1 (`C:\Project\backup\DBMAIS-9.1.FDB`), porque confere as contagens do aceite.
  - Para testar a migração com a cópia do dia, troque a constante `Fdb` só localmente (cerca de 10 min; as contagens fixas vão falhar).
- Executar: `dotnet run` (ou `rodar.bat`). Connection string em user-secrets: `dotnet user-secrets set "ConnectionStrings:LabControl" "Server=localhost;Port=3306;Database=LabControlDB;User=root;Password=<senha>"`. **Senha nunca em arquivo versionado**: onde fica e como trocar em `Scripts/v2/README.md` › "Senha do MySQL".
- Gerar entidades: `node tools/gerador-csharp/gerar.mjs`. Testes: `dotnet test tests/LabControl.Tests` (cada execução cria um banco temporário `lc_teste_*` pelos scripts, ~3 min, e o apaga no fim; nunca usam o `LabControlDB`). Suíte inteira de uma vez, ~17 min (`MigracaoTests` roda sozinho no fim, coleção sem paralelismo — P26 §4). Testes no navegador (CRUD real das telas, Edge sem janela): `--filter "FullyQualifiedName~Navegador"` — ver `docs/processos/P26-testes-ponta-a-ponta.md`; não rodar com o `dotnet run` aberto (trava o build).
- Manual do usuário (`Manual.pdf` na raiz, para quem opera o sistema): texto em `docs/manual/manual.html`; `.\docs\manual\gerar-manual.ps1` tira as imagens das telas com um laboratório fictício (banco temporário, nenhum dado real) e imprime o PDF pelo Edge (~2 min; `-SoPdf` só refaz o PDF). Tela, botão ou regra visível mudou? Atualize o manual junto (skill `concluir-tarefa`).
- Serviços: retornar `Resultado`/`Resultado<T>`; procedures só por `Infra/Procedures.cs`; erros MySQL por `ErrosMySql` (ver `docs/processos/P22-erros-transacoes.md`).
