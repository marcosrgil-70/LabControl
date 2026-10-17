# LabControl v2 — Documentação

Migração do sistema **Delphi 7 + Firebird 2.5** (`C:\Project\Laboratorio`) para **ASP.NET Core MVC (.NET 10) + MySQL 8**, multiempresa.

## Comece por aqui
1. [00-PLANO-EXECUCAO.md](00-PLANO-EXECUCAO.md) — fases, checklist, modelo/modo do Claude para cada tarefa, diário.
2. [01-ARQUITETURA-V2.md](01-ARQUITETURA-V2.md) — stack, multiempresa, segurança, padrões de UI e código.

## Banco de dados
| Doc | Conteúdo |
|---|---|
| [banco/10-ANALISE-FIREBIRD.md](banco/10-ANALISE-FIREBIRD.md) | O que existe no legado: tabelas, volumes, relações, procedures, triggers, views, anomalias |
| [banco/11-MODELO-MYSQL.md](banco/11-MODELO-MYSQL.md) | Modelo v2, decisões D-01…D-18, mapeamento Firebird → MySQL |
| [banco/12-PROCEDURES-REVISOES.md](banco/12-PROCEDURES-REVISOES.md) | Erros das procedures de revisão/numeração e a correção, com roteiro de teste |
| [banco/13-MIGRACAO-DADOS.md](banco/13-MIGRACAO-DADOS.md) | Etapas M-01…M-18 e conferências da migração |
| `../Scripts/v2/` | `01_schema.sql`, `02_funcoes_procedures.sql`, `03_triggers.sql`, `04_views.sql`, `05_seed.sql` |

## Telas (roteiro de desenvolvimento)
| Fase | Doc | Tela |
|---|---|---|
| 1 | [T00](telas/T00-login-empresa.md) | Login e seleção de empresa |
| 1 | [T01](telas/T01-layout-menu.md) | Layout, menu e painel |
| 1 | [T02](telas/T02-usuarios-perfis.md) | Usuários, perfis e permissões |
| 1 | [T03](telas/T03-empresas-parametros.md) | Empresas e parâmetros |
| 2 | [T10](telas/T10-cadastros-auxiliares.md) | 20 cadastros auxiliares (CRUD genérico) |
| 2 | [T11](telas/T11-geografia.md) | Países, estados, cidades |
| 3 | [T12](telas/T12-clientes.md) | Clientes/fornecedores |
| 3 | [T13](telas/T13-profissionais.md) | Profissionais + vínculo com usuário |
| 3 | [T14](telas/T14-produtos.md) | Produtos |
| 3 | [T15](telas/T15-parametros-analise.md) | Parâmetros de análise |
| 4/6 | [T20](telas/T20-propostas.md) | **Propostas** ⚠️ |
| 6 | [T21](telas/T21-pedidos-venda.md) | **Pedidos de venda** ⚠️ |
| 4 | [T22](telas/T22-recebimento-amostras.md) | **Recebimento de amostras** ⚠️⚠️ |
| 7/4 | [T23–T25](telas/T23-T25-movimentacao-localizacao-etiquetas.md) | Movimentação, localização, etiquetas |
| 5 | [T26](telas/T26-resultados.md) | **Resultados** ⚠️ |
| 5 | [T27](telas/T27-boletim.md) | **Boletim** ⚠️⚠️ |
| 8 | [T28/T30](telas/T28-T30-relatorio-ensaio-relatorios.md) | Relatório de ensaio e relatórios gerenciais |

## Histórico
`_obsoleto_v1/` — documentação da primeira tentativa (modelo de dados incorreto). Só consulta.
