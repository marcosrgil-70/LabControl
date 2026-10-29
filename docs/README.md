# LabControl v2 — Documentação

Migração do sistema **Delphi 7 + Firebird 2.5** (`C:\Project\Laboratorio`) para **ASP.NET Core MVC (.NET 10) + MySQL 8**, multiempresa.

## Comece por aqui
1. [00-PLANO-EXECUCAO.md](00-PLANO-EXECUCAO.md) — fases, checklist, modelo/modo do Claude, dúvidas, riscos e diário.
2. [plano/](plano/) — uma página detalhada por fase ([F0](plano/F0-fundacao.md) · [F1](plano/F1-login-permissoes.md) · [F2](plano/F2-cadastros-basicos.md) · [F3](plano/F3-cadastros.md) · [F4](plano/F4-recebimento-amostras.md) · [F5](plano/F5-resultados-boletim.md) · [F6](plano/F6-propostas-pedidos.md) · [F7](plano/F7-estoque-arquivo.md) · [F8](plano/F8-relatorios-complementos.md) · [F9](plano/F9-homologacao-virada.md)) e a [matriz Delphi → C#](plano/MATRIZ-DELPHI-CSHARP.md).
3. [01-ARQUITETURA-V2.md](01-ARQUITETURA-V2.md) — stack, multiempresa, segurança, padrões de UI e de código.

## Situação atual (29/10/2026)
- **Feito:**
  - **Fases 0 a 4:** fundação, login/permissões/multiempresa, cadastros básicos e principais, proposta essencial, **recebimento de amostras**, pesquisa de amostras e etiquetas;
  - **Fase 5:** migração completa M-01…M-18 (resultados e boletins do legado entram emitidos e congelados), **T26 Resultados**, **T27 Boletim** (emissão em PDF com SHA-256, reimpressão conferida, 2ª via do migrado, revisão/desfazer) e o aceite da fase — falta só a **5.4** (layout com os boletins reais);
  - **Fase 6:** **T20 proposta completa** (revisão, contrato com importação de Word, PDF com a Autorização de Serviço, trocar produto) e **T21 pedidos de venda** (conclusão automática na emissão do último boletim, reabrir, cancelar, PDF);
  - **Fase 7:** **T23 movimentação** (saldo protegido no banco) e **T24 localização** (arquivo, descarte, lote);
  - **Fase 8 (29/10/2026):**
    - **T30-01…06 relatórios gerenciais** (tela, PDF e CSV), comparados com o legado em 2017 com as diferenças explicadas nos MDs;
    - **envio do boletim por e-mail**, com a configuração do servidor numa aba de *Parâmetros da Empresa* (pedido do usuário);
    - **T06 Auditoria** (eventos, diferença campo a campo, logins, CSV, histórico da amostra);
    - revisão da fase (`/simplify` + `/code-review high`);
    - a T28 (Relatório de Ensaio) foi **dispensada** pelo usuário.
  - **Fase 9, 9.1 (29/10/2026):** decisões 33, 34 e 36 aplicadas, `MySqlConnector` atualizado (37) e **migração completa em banco limpo** (74/74 conferências; relatório de anomalias para revisar com o laboratório).
  - **Testes:** suíte inteira **304/304** (~3 min); roteiro SQL do banco **76/76**.
- **Dúvidas respondidas em 29/10/2026:**
  - 1: T28 dispensada;
  - 2: e-mail configurado pelo usuário em Parâmetros da Empresa;
  - 9: "somente amostras com pedido de venda" marcado por padrão.
  - A lista completa está no §7 do plano.
- **A fazer:**
  - **5.4:** layout do PDF do boletim com os 3 boletins reais (numérico com expoente, descritivo, com revisão ou dois responsáveis) — aguarda o laboratório;
  - **Fase 9:**
    - 9.2: homologação guiada;
    - 9.3: `/security-review`, backup e virada.
- **Pendências abertas:** [00-PLANO-EXECUCAO.md › Pendências registradas](00-PLANO-EXECUCAO.md#pendências-registradas-a-executar).
  - Dependem do laboratório:
    - 21: impressão de teste na Argox;
    - 22 e 28: testes manuais guiados das Fases 4 e 5;
    - 25: os 3 boletins reais;
    - 38: teste real do e-mail;
    - 12: trocar a senha `root` do MySQL.
    - 39: revisar o relatório de anomalias da migração.
## Organização
| Pasta | Conteúdo | Quantidade |
|---|---|---|
| [banco/](banco/) | análise do Firebird, modelo MySQL, procedures/revisões, migração | 4 documentos |
| [banco/objetos/](banco/objetos/README.md) | **um MD por objeto do banco v2** (tabela, view, função/procedure, trigger) | 59 + 10 + 35 + 48 |
| [telas/](#telas) | **um MD por tela** (todas as telas do Delphi que vão para o C# + telas novas) | 54 |
| [processos/](processos/README.md) | **um MD por processo de negócio** + [etapas da migração](processos/migracao/README.md) | 26 + 18 |
| [regras/](regras/README.md) | **catálogo de regras de negócio** (`RN-xx.yy`) por domínio | 9 domínios |
| `../Scripts/v2/` | `01_schema.sql` … `06_cidades.sql` (+ `dev/` e `teste/`) — **fonte da verdade do schema** | 6 scripts |
| `../tools/gerador-docs/` | gera `banco/objetos/` a partir dos scripts e valida os links ([README](../tools/gerador-docs/README.md)) | — |

## Banco de dados
| Doc | Conteúdo |
|---|---|
| [banco/10-ANALISE-FIREBIRD.md](banco/10-ANALISE-FIREBIRD.md) | O que existe no legado: tabelas, volumes, relações, procedures, triggers, views, anomalias |
| [banco/11-MODELO-MYSQL.md](banco/11-MODELO-MYSQL.md) | Modelo v2, decisões D-01…D-27, mapeamento Firebird → MySQL |
| [banco/12-PROCEDURES-REVISOES.md](banco/12-PROCEDURES-REVISOES.md) | Erros das procedures de revisão/numeração e a correção, com roteiro de teste |
| [banco/13-MIGRACAO-DADOS.md](banco/13-MIGRACAO-DADOS.md) | Etapas M-01…M-18 e conferências da migração |
| [banco/objetos/README.md](banco/objetos/README.md) | Índice dos 152 objetos com criticidade e finalidade |

## Telas
Legenda: ⚠️ particularidades · ⚠️⚠️ crítica.

| Fase | Tela | Delphi de origem | Situação |
|---|---|---|---|
| 1 | [T00 Login e seleção de empresa](telas/T00-login-empresa.md) | lançador `MAIS_Acesso` / Ativar Empresa | ✅ |
| 1 | [T01 Layout, menu e painel](telas/T01-layout-menu.md) | `un_Menu`, `un_About` | ✅ |
| 1 | [T02 Usuários](telas/T02-usuarios.md) | `un_DireitoAcesso01` | ✅ |
| 1 | [T03 Empresa usuária](telas/T03-empresa-usuaria.md) | `un_Entidades` (empresa) | ✅ |
| 1 | [T04 Perfis e permissões](telas/T04-perfis-permissoes.md) | `SYS_*_ACESSOS` / `unClassAcessos` | ✅ |
| 1 | [T05 Parâmetros da empresa](telas/T05-parametros-empresa.md) | `un_Parametros` | ✅ |
| 8 | [T06 Auditoria](telas/T06-auditoria.md) | — (nova) | ✅ 8.4 |
| 3–9 | [T07 Migração de dados](telas/T07-migracao.md) | — (nova) | ✅ M-01…M-18 (3.5, 4.1, 5.1) |
| 2 | [T10-00 Padrão dos auxiliares](telas/T10-00-crud-auxiliares.md) + [T10-01 … T10-20](telas/T10-00-crud-auxiliares.md#lista-dos-auxiliares) | `un_CadastroBasico` + 20 forms | ✅ |
| 2 | [T11-01 Países](telas/T11-01-paises.md) · [T11-02 Estados](telas/T11-02-estados.md) · [T11-03 Cidades](telas/T11-03-cidades.md) · [T11-04 Bairros/Logradouros (descontinuados)](telas/T11-04-bairros-logradouros.md) | `un_Paises`, `un_Estados`, `un_Cidades`, `un_Bairros`, `un_Logradouros` | ✅ (T11-04 ⛔) |
| 3 | [T12 Clientes/fornecedores](telas/T12-clientes.md) | `un_Entidades` | ✅ |
| 3 | [T13 Profissionais](telas/T13-profissionais.md) | `un_Entidades` (funcionário) | ✅ |
| 3 | [T14 Produtos](telas/T14-produtos.md) | `un_Produtos` | ✅ |
| 3 | [T15 Parâmetros de análise](telas/T15-parametros-analise.md) | `un_ParametrosAnalises` | ✅ |
| 4/6 | [T20 **Propostas** ⚠️](telas/T20-propostas.md) | `un_Propostas` | ✅ completa (4.2 + 6.1) |
| 6 | [T21 **Pedidos de venda** ⚠️](telas/T21-pedidos-venda.md) | botão Ped.Venda da proposta | ✅ 6.2 |
| 4 | [T22 **Recebimento de amostras** ⚠️⚠️](telas/T22-recebimento-amostras.md) | `un_MovHistAmostras` | ✅ 4.3 |
| 7 | [T23 Movimentação de amostras](telas/T23-movimentacao-amostras.md) | `un_MovimentacaoAmostras` | ✅ 7.1 |
| 7 | [T24 Localização das amostras](telas/T24-localizacao-amostras.md) | `un_LocalAmostra` | ✅ 7.2 |
| 4 | [T25 Etiquetas de amostras](telas/T25-etiquetas-amostras.md) | `un_EtqAmostra` | 🟨 4.5; falta a impressão de teste na Argox |
| 5 | [T26 **Resultados** ⚠️](telas/T26-resultados.md) | `un_Resultados` | ✅ 5.2 |
| 5 | [T27 **Boletim** ⚠️⚠️](telas/T27-boletim.md) | `un_CadBoletim` | 🟨 5.3 pronta e envio por e-mail (8.3); layout com boletins reais na 5.4 |
| 8 | [T28 Relatório de ensaio](telas/T28-relatorio-ensaio.md) | `un_Relatorio_Ensaio` | ⛔ dispensada pelo usuário (8.2) |
| 4 | [T29 Pesquisa de amostras](telas/T29-pesquisa-amostras.md) | `un_Lab_Hist_Amostras_Pesq` | ✅ 4.4 (usada também pela T26/T27) |
| 8 | [T30-00 Padrão dos relatórios](telas/T30-00-relatorios-padrao.md) + [T30-01 Faturamento](telas/T30-01-rel-faturamento.md) · [T30-02 Amostras por período](telas/T30-02-rel-amostras-periodo.md) · [T30-03 Amostras por cliente](telas/T30-03-rel-amostras-cliente.md) · [T30-04 Parâmetros analisados](telas/T30-04-rel-parametros-analisados.md) · [T30-05 Tempo das análises](telas/T30-05-rel-tempo-analises.md) · [T30-06 Produção](telas/T30-06-rel-producao.md) | `un_RelatorioBasico` + 6 relatórios | ✅ 8.1 |

## Processos e regras
- [processos/README.md](processos/README.md) — P01 a P26 (multiempresa, numeração, número da amostra, proposta, pedido, recebimento, revisões em cascata, estoque, resultados, boletim, emissão, relatórios, auditoria, erros/transações, nova empresa, migração, cadastro de entidades e contatos, testes no navegador).
- [regras/README.md](regras/README.md) — RN-01 a RN-09 com camada (banco/serviço/tela), origem (legado/correção/nova/decisão) e criticidade.

## Histórico
`_obsoleto_v1/` — documentação da primeira tentativa (modelo de dados incorreto). Só consulta.
