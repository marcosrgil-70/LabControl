---
name: mudanca-banco
description: Roteiro obrigatório para qualquer mudança no banco MySQL do LabControl (tabela, coluna, índice, view, procedure, função, trigger, seed ou permissão em Scripts/v2/*.sql) — alterar o script, documentar (doc 11, MDs dos objetos), rodar o roteiro 12 §7, recriar o banco e regenerar as entidades C#. Use sempre que uma tarefa precisar mexer no schema ou nas rotinas do banco ("criar coluna", "nova procedure", "alterar trigger", "nova permissão", "mudar a view"), ou quando o usuário digitar /mudanca-banco.
---

# Mudança no banco (Scripts/v2)

Responda em português do Brasil. O schema é **somente** `Scripts/v2/01..06*.sql` (banco `labcontrol_v2`).
**Proibido:** EF migrations, `EnsureCreated`, "patches" de SQL no `Program.cs`, editar `*.g.cs` à mão.

## 0. Antes de mexer
1. Leia o MD de cada objeto tocado em `docs/banco/objetos/{tabelas,views,rotinas,triggers}/` (colunas preenchidas pelo banco, mensagens 45000, quem chama) e o trecho do `docs/banco/11-MODELO-MYSQL.md` (decisões D-xx).
2. Procure quem depende: `Grep` pelo nome do objeto em `Scripts/v2`, `Services/`, `Infra/Procedures.cs`, `Data/` e `docs/`.
3. Mudança que **altera regra de negócio** ou é difícil de desfazer: explique o plano ao usuário e confirme antes (modo padrão ✋ do plano).

## 1. Alterar o script
| Objeto | Arquivo |
|---|---|
| tabela, coluna, índice, FK | `01_schema.sql` (FKs de status/circulares ficam no fim do arquivo) |
| função / procedure | `02_funcoes_procedures.sql` (`DELIMITER $$`, `END$$`) |
| trigger | `03_triggers.sql` |
| view | `04_views.sql` |
| permissão, UF, dados fixos | `05_seed.sql` |
| municípios (IBGE) | `06_cidades.sql` — gerado por `tools/gerador-cidades` (não editar à mão) |

Convenções:
- Tudo por empresa: tabela de cadastro/movimento tem `ID_EMPRESAS` + FK; filho com empresa diferente do pai → `CALL PR_CONFERIR_EMPRESA(...)` na trigger.
- Erro de negócio: `SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = '<frase pronta para o usuário>'` (a aplicação mostra o texto como está).
- Procedure com transação própria grava auditoria com `PR_AUDITAR` **dentro** da transação.
- Número de documento só por `SP_PROXIMO_NUMERO` dentro da trigger; revisões só pelas `SP_GERAR_REVISAO_*` (`@LC_REVISAO`).
- Ao editar SQL por script (Node/sed), cuidado: `String.replace` do JS com `$$` no texto novo vira `$` e quebra os `END$$` — use função de substituição.

## 2. Documentar
1. `docs/banco/11-MODELO-MYSQL.md`: decisão nova (D-xx), contagens no cabeçalho se mudaram.
2. Regras de negócio do objeto: `tools/gerador-docs/meta_tabelas.mjs` / `meta_outros.mjs`; depois `node tools/gerador-docs/gerar.mjs` (regenera os MDs em `docs/banco/objetos/`) e `node tools/gerador-docs/checar_links.mjs`.
3. Rotina/trigger nova ou alterada com efeito em processo: atualize `docs/banco/12-PROCEDURES-REVISOES.md` e o `docs/processos/P*.md` correspondente.
4. Contagens mudaram (tabelas, views, funções, procedures, triggers, permissões)? Atualize `$esperado` em `Scripts/v2/recriar.ps1` e a tabela de arquivos de `Scripts/v2/README.md`.

## 3. Testar no MySQL
Rode você mesmo (PowerShell): o `recriar.ps1` lê a senha do user-secret `ConnectionStrings:LabControl` (fora do repositório), sem pergunta. Leva ~3 min.
```
.\Scripts\v2\recriar.ps1 -Teste
```
Se ele pedir a senha (user-secret ausente ou de outro usuário), peça ao usuário para rodar com o prefixo `!` na caixa de mensagem.
- Regra nova → acrescente verificação OK/FALHA em `Scripts/v2/teste/roteiro_12_7.sql` (positiva **e** negativa, com a mensagem exata) e atualize a contagem no README.
- Tudo OK → recriar o banco local: `.\Scripts\v2\recriar.ps1 -Dev -Forcar` (apaga e recria `labcontrol_v2` com a empresa 001 e `ADMIN`/`Admin123`). **Confirme com o usuário antes**: apaga os dados de desenvolvimento.
- Bancos que não podem ser apagados (`labcontrol_migracao`, bancos migrados): `.\Scripts\v2\atualizar.ps1 -Banco <nome>` acrescenta o que mudou (o instalador usa o mesmo na atualização do servidor).
- O script recusa `labcontrol` (v1). Nunca rode o roteiro no `labcontrol_v2` real.

## 4. Regenerar o C#
1. View nova/alterada: antes atualize `tools/gerador-csharp/views.json` (ver `tools/gerador-csharp/README.md`).
2. `node tools/gerador-csharp/gerar.mjs` → `Models/*.g.cs`, `Data/Configurations/*.g.cs`, DbSets. Ajustes manuais só nos parciais sem `.g`.
3. Procedure nova: método em `Infra/Procedures.cs` (único ponto de chamada; recusa dentro de transação do EF, confere a empresa); mensagens por `ErrosMySql` (ver `docs/processos/P22-erros-transacoes.md`).
4. Tabela nova com auditoria obrigatória (P21)? Inclua em `AuditoriaAlteracoes.Tabelas` (`Data/AuditoriaAlteracoes.cs`). Tabela nova sem `ID_EMPRESAS`? Confira o filtro global.
5. `dotnet build` (0 avisos) e `dotnet test tests/LabControl.Tests` — o banco `labcontrol_teste` se recria sozinho quando o hash de `Scripts/v2/0*.sql` muda (1º run ~3 min a mais). Acrescente teste C# para a regra nova (mensagem exata do 45000).

## 5. Fechar
- Registre no Diário (`docs/00-PLANO-EXECUCAO.md` §9) o que mudou no banco e por quê, com o resultado do roteiro (`n/n`) e dos testes.
- Se fizer parte de uma tarefa do plano, siga depois a skill `concluir-tarefa`.
