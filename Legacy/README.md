# Legacy — código da v1 (fora da compilação)

Controllers, models, views, `ApplicationDbContext` e `SessaoFilter` da primeira versão C#, feita sobre o modelo de dados antigo (banco `labcontrol`). Foram movidos para cá na tarefa 0.3 (19/10/2026) porque não compilam contra o schema v2.

- **Não compila** (`<Compile Remove="Legacy/**">` no `LabControl.csproj`) e não é publicado.
- Serve de **referência de layout e comportamento** ao reescrever cada tela (ver `docs/telas/`).
- Quando a tela nova existir, apague o arquivo correspondente daqui (o histórico fica no git).
- O `MigracaoController` (Firebird → MySQL) é a base da migração das fases 3–5 (`docs/processos/migracao/`).
