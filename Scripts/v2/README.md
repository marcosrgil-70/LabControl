# Scripts/v2 — banco MySQL do LabControl v2

O schema do banco é **somente** o que está nestes scripts (sem EF migrations, sem "patches" no `Program.cs`).
Documentação: [doc 11 — modelo](../../docs/banco/11-MODELO-MYSQL.md) · [doc 12 — procedures e revisões](../../docs/banco/12-PROCEDURES-REVISOES.md) · [MDs por objeto](../../docs/banco/objetos/README.md).

## Arquivos (ordem de execução)

| # | Arquivo | Conteúdo |
|---|---|---|
| 1 | `01_schema.sql` | `CREATE DATABASE labcontrol_v2` + 59 tabelas |
| 2 | `02_funcoes_procedures.sql` | 8 funções + 19 procedures (`SP_PROXIMO_NUMERO`, `SP_GERAR_REVISAO_*`, `SP_EMITIR_BOLETIM`…) |
| 3 | `03_triggers.sql` | 42 triggers (numeração, travas de revisão/emissão, saldo, log) |
| 4 | `04_views.sql` | 10 views |
| 5 | `05_seed.sql` | Brasil + 27 UFs e catálogo de 109 permissões |
| — | `teste/roteiro_12_7.sql` | roteiro de teste do doc 12 §7 (39 verificações) — **só em banco temporário** |
| — | `recriar.ps1` | recria um banco do zero e confere as contagens; `-Teste` roda o roteiro |

Empresas, usuários e cadastros **não** estão no seed: vêm da migração ([doc 13](../../docs/banco/13-MIGRACAO-DADOS.md)) ou do cadastro de nova empresa (`SP_INICIALIZAR_EMPRESA`).

## Recriar o banco (um comando)

```powershell
.\Scripts\v2\recriar.ps1                 # recria labcontrol_v2 (pede a senha e, se o banco existir, confirmação)
.\Scripts\v2\recriar.ps1 -Teste          # cria labcontrol_teste_<id>, roda o roteiro 12 §7 e apaga o banco
.\Scripts\v2\recriar.ps1 -Teste -Manter  # idem, mas mantém o banco temporário para inspeção
```

Parâmetros: `-Banco` (padrão `labcontrol_v2`), `-Servidor` (`127.0.0.1`), `-Porta` (`3306`), `-Usuario` (`root`), `-MySql` (caminho do `mysql.exe`; padrão: o do PATH ou `C:\Program Files\MySQL\MySQL Server 8.0\bin`), `-Forcar` (não pede confirmação para apagar).

O script:
1. **apaga** o banco informado (recusa `labcontrol` — o v1 — e os bancos do sistema);
2. roda os 5 scripts em ordem, trocando `labcontrol_v2` pelo nome do banco, e **para no primeiro erro**;
3. confere as contagens (59 tabelas, 10 views, 8 funções, 19 procedures, 42 triggers, 109 permissões) — se o schema mudou de propósito, atualize `$esperado` no script;
4. com `-Teste`, roda `teste/roteiro_12_7.sql` e falha se alguma verificação falhar.

A senha é pedida no console. Para rodar sem pergunta (ex.: CI), defina antes `$env:MYSQL_PWD`.
Leva ~3 minutos por banco no MySQL local (DDL lento no Windows).

## Manualmente

```powershell
mysqlsh --sql "root@localhost:3306" --file Scripts/v2/01_schema.sql   # e 02..05, nesta ordem
```
ou com o cliente clássico: `mysql -u root -p --default-character-set=utf8mb4 < Scripts/v2/01_schema.sql`.

Os scripts têm `USE labcontrol_v2` fixo. Para outro nome, use o `recriar.ps1 -Banco <nome>` (ele faz a troca numa cópia temporária).

## Roteiro de teste (doc 12 §7)

**Reexecutar após qualquer mudança em 01..05:** `.\Scripts\v2\recriar.ps1 -Teste`.
O roteiro cria uma empresa e cadastros de teste, então **nunca** rode no `labcontrol_v2` real. Cada verificação gera uma linha OK/FALHA; o script termina com erro (`SQLSTATE 45000`) se houver falha.

## Mudou o banco?

1. Altere o script (01..05);
2. atualize o [doc 11](../../docs/banco/11-MODELO-MYSQL.md) e o MD do objeto em `docs/banco/objetos/`;
3. rode `.\Scripts\v2\recriar.ps1 -Teste` (e acrescente verificações ao roteiro se a regra for nova);
4. recrie o `labcontrol_v2` local;
5. regenere as entidades C#: `node tools/gerador-csharp/gerar.mjs` (view mudou? antes atualize `views.json` — ver [tools/gerador-csharp/README.md](../../tools/gerador-csharp/README.md)) e rode `dotnet test tests/LabControl.Tests`.
