# P01 — Multiempresa e Empresa Ativa

**Criticidade:** 🔴 (erro aqui mistura dados de laboratórios diferentes) · **Fase:** 0/1
**Telas:** [T00](../telas/T00-login-empresa.md), [T01](../telas/T01-layout-menu.md), [T03](../telas/T03-empresa-usuaria.md) e todas as demais
**Objetos:** [`EMPRESAS`](../banco/objetos/tabelas/EMPRESAS.md), [`USUARIOS_EMPRESAS`](../banco/objetos/tabelas/USUARIOS_EMPRESAS.md), triggers de herança (`TG_ENTEND_BI`, `TG_PROPPROD_BI`, `TG_PRODAN_BI`, `TG_TESTES_BI`, `TG_RESULT_BI`, `TG_RESPAR_BI`, `TG_BOLETIM_BI`, `TG_BOLPAR_BI`, `TG_MOV_BI`…)
**Regras:** [RN-01](../regras/RN-01-gerais-multiempresa.md) · **Decisão:** D-01 (tudo por empresa)

## Objetivo
Garantir que cada request trabalhe com **uma** empresa (a ativa) e que nenhum dado de outra empresa seja lido ou gravado.

## Como era no legado
- Empresa ativa recebida por `ParamStr(2)` do lançador; troca por *Utilitários → Ativar Empresa*.
- Só os **movimentos** tinham `ID_EMPRESAS` (propostas, amostras, resultados, boletins); cadastros eram globais.
- Várias consultas esqueciam o filtro de empresa (ex.: `qry_Amostra` da tela de Resultados, pesquisa de amostras, combo do Relatório de Ensaio).

## Fluxo
1. Login → escolha da empresa entre `USUARIOS_EMPRESAS` ativas (ou todas, para ADMIN_GLOBAL) → claim `EmpresaId` (+ código, nome, `ProfissionalId`).
2. Middleware: request autenticado **sem** `EmpresaId` → redireciona para `/Conta/Empresa`.
3. `IEmpresaContexto` (scoped) lê as claims: `EmpresaId`, `UsuarioId`, `ProfissionalId`, `AdminGlobal`, `TemPermissao()`.
4. `LabDbContext`: **Global Query Filter** `e.IdEmpresas == contexto.EmpresaId` em toda entidade com `ID_EMPRESAS` (exceto globais: `EMPRESAS`, `PAISES`, `ESTADOS`, `CIDADES`, `USUARIOS`, `PERMISSOES`). Views mapeadas como keyless também recebem o filtro.
5. `SaveChanges`: entidades `Added` com `ID_EMPRESAS` recebem a empresa ativa; `Modified` não podem trocar de empresa (as triggers também bloqueiam nos documentos).
6. Banco: filhos herdam `ID_EMPRESAS` do pai pelas triggers, e um `ID_EMPRESAS` informado diferente do pai é recusado ("Registro pertence a outra empresa.", `PR_CONFERIR_EMPRESA`) — não há como gravar item de proposta, teste, resultado ou boletim em outra empresa.
7. Troca de empresa (menu do topo) → novas claims → Home (descarta formulários abertos).

## Regras
- **Proibido** `?? 1` / empresa padrão no código (erro da v1).
- **Proibido** `IgnoreQueryFilters()` fora de Migração e de telas ADMIN_GLOBAL explícitas (lista de empresas, auditoria global).
- Combos/lookups: só registros **da empresa** e **ativos**.
- SQL cru (Dapper/`FromSql`) sempre com `WHERE ID_EMPRESAS = @empresa`.
- Procedures recebem IDs; elas mesmas validam a empresa pelos registros (ex.: `PR_REVISAO_BOLETIM` "Resultado do boletim não encontrado nesta empresa").

## Implementação no C# (feita na tarefa 0.4)
- `Infra/EmpresaContexto.cs`: `IEmpresaContexto` (scoped) lê as claims `LabClaims.*` (`EmpresaId`, `UsuarioId`, `ProfissionalId`, `AdminGlobal`). `TemPermissao()` entra na Fase 1 (T04).
- O gerador marca as entidades: **`IPorEmpresa`** (tabela com `ID_EMPRESAS NOT NULL`: filtro + `SaveChanges`) ou **`IFiltradoPorEmpresa`** (views e `AUDITORIA`, cujo `ID_EMPRESAS` é anulável: só filtro).
- `LabDbContext.OnModelCreating`: `e => EmpresaAtivaId != null && (int?)e.IdEmpresas == EmpresaAtivaId` em toda `IFiltradoPorEmpresa` (o `!= null` evita que, sem empresa ativa, linhas com `ID_EMPRESAS` NULL apareçam — revisão 0.9); filhos sem `ID_EMPRESAS` filtram pelo pai (`CONDICOES_PAGTOS_PARC` → `CONDICOES_PAGTOS`, `PERFIS_PERMISSOES` → `PERFIS`).
- **Sem filtro** (lista `SEM_FILTRO_EMPRESA` do gerador): `EMPRESAS` (global) e os vínculos do usuário global `USUARIOS_EMPRESAS` / `USUARIOS_PERFIS` (o login lista as empresas do usuário; os serviços de login/usuários filtram explicitamente). A **gravação** desses vínculos fica restrita à empresa ativa, exceto para ADMIN_GLOBAL (revisão 0.9).
- `SaveChanges`: inclusão recebe a empresa ativa (ou é recusada se vier outra); alteração/exclusão só de registro da empresa ativa e sem trocar `ID_EMPRESAS`; **sem empresa ativa nada é lido nem gravado** (fail-closed).
- Alterar = carregar pela consulta filtrada e copiar os campos do formulário; **nunca** `db.Update(entidadeDoFormulario)` (o `UPDATE … WHERE ID = x` não leva a empresa).
- `Infra/ExigirEmpresaMiddleware.cs`: usuário autenticado sem empresa → `/Conta/Empresa` (actions MVC, exceto o controller `Conta`).
- Migração (F3) grava várias empresas: usará um contexto/modo próprio (a decidir na 3.5), nunca `?? 1`.
- Testes: `tests/LabControl.Tests/MultiempresaTests.cs` (2 empresas) e `ExigirEmpresaMiddlewareTests.cs`.

## Cenários de teste
- Usuário com acesso às empresas 1 e 2: dados criados na 1 não aparecem na 2 (listas, combos, views, relatórios, PDFs).
- Tentar abrir `/Amostras/Editar/{id de outra empresa}` → 404.
- Inserir item de proposta com `ID_EMPRESAS` errado via SQL → a trigger corrige para a empresa da proposta.
