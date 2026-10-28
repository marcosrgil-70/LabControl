# LabControl v2

Sistema de gestão do laboratório (propostas, recebimento de amostras, resultados, boletins), reescrito do legado
**Delphi 7 + Firebird 2.5** (`C:\Project\Laboratorio`, banco `DBMAIS.FDB`) em **ASP.NET Core MVC (.NET 10) + MySQL 8**,
**multiempresa** (tudo por empresa).

> **Onde estamos e o que vem a seguir:** [docs/00-PLANO-EXECUCAO.md](docs/00-PLANO-EXECUCAO.md) (bloco "Ponto de retorno",
> checklist por fase, pendências e diário). Regras de trabalho: [CLAUDE.md](CLAUDE.md).

## Situação (27/10/2026)
| Fase | Entrega | Situação |
|---|---|---|
| 0 | Fundação: banco v2 pelos scripts, multiempresa, erros, auditoria, testes | ✅ |
| 1 | Login, empresa ativa, usuários, perfis, empresa usuária, parâmetros | ✅ |
| 2 | 20 cadastros auxiliares, geografia (IBGE + CEP), cadastro rápido "+" | ✅ |
| 3 | Clientes, profissionais, produtos, parâmetros de análise, migração parte 1 | ✅ |
| 4 | Migração parte 2, proposta essencial, **recebimento de amostras**, pesquisa, etiquetas | ✅ (faltam com o laboratório: impressão de teste na Argox e teste manual guiado) |
| 5 | Migração parte 3, **resultados**, **boletim** (PDF com hash) | 🟨 5.1, 5.2, 5.3 e 5.5 prontas (migração completa, resultados, boletim, aceite e revisão de código); falta a 5.4 (layout do PDF com 3 boletins reais do laboratório) |
| 6 | Proposta completa (revisão, PDF, contrato) e pedido de venda | 🟨 6.1 proposta completa e 6.2 pedidos de venda prontas; falta a 6.3 (revisão) |
| 7 | Movimentação e localização de amostras | ⬜ |
| 8 | Relatórios gerenciais, auditoria, e-mail | ⬜ |
| 9 | Migração completa, homologação e virada | ⬜ |

Telas por situação: [matriz Delphi → C#](docs/plano/MATRIZ-DELPHI-CSHARP.md).

## Tecnologias
| Camada | Tecnologia |
|---|---|
| Web | ASP.NET Core MVC (.NET 10), Razor, Bootstrap 5, Bootstrap Icons |
| Dados | EF Core 9 + Pomelo MySQL, **sem migrations**: o schema é `Scripts/v2/*.sql` (entidades geradas por `tools/gerador-csharp`) |
| Banco | MySQL 8.0 — regras críticas no banco (triggers e procedures: numeração, revisões, estoque, valores) |
| Autenticação | Cookie + senha PBKDF2, bloqueio após 5 tentativas, troca obrigatória no 1º acesso |
| Impressão | comprovante como página de impressão; etiquetas PPLB em RAW na térmica (Argox); **boletim em PDF (QuestPDF)**, emitido gravado numa pasta do servidor com SHA-256 |
| Testes | xUnit (serviços + banco `labcontrol_teste`) e Playwright (Edge sem janela) para as telas |

## Como rodar (desenvolvimento)
1. **Senha do MySQL** em user-secrets (nunca em arquivo versionado):
   `dotnet user-secrets set "ConnectionStrings:LabControl" "Server=localhost;Port=3306;Database=labcontrol_v2;User=root;Password=<senha>"`
2. **Banco** pelos scripts: `.\Scripts\v2\recriar.ps1 -Dev` (cria o `labcontrol_v2` com a empresa 001 e o usuário de desenvolvimento). Detalhes em [Scripts/v2/README.md](Scripts/v2/README.md).
3. **Executar:** `dotnet run` (ou `rodar.bat`) → http://localhost:5180.
4. **Login de desenvolvimento/testes:** `ADMINISTRADOR` / `Admin123` (acesso a tudo, sem troca de senha, vinculado ao profissional "USUÁRIO ESPECIAL DO SISTEMA"). Nunca em produção.
5. **PDFs dos boletins emitidos:** pasta `Boletins:Pasta` do `appsettings.json` (padrão `storage/boletins`, fora do git). Em produção, uma pasta do servidor que **entra no backup** junto com o banco (o banco guarda só o caminho e o SHA-256).

### Com os dados reais do legado
1. `.\Scripts\v2\recriar.ps1 -Banco labcontrol_migracao -Forcar`
2. No app (banco `labcontrol_v2`), tela **Migração** (T07): executa M-01 → M-18 (cadastros, propostas, pedidos, amostras, resultados e boletins; ~5 min) com conferências e anomalias. Os boletins do legado entram **emitidos** (congelados) e reimprimem como "2ª via".
3. Aponte o user-secret para `Database=labcontrol_migracao` e entre com `ADMINISTRADOR` / `Admin123` (ou um usuário migrado com a senha provisória digitada na T07).

## Testes
`dotnet test tests/LabControl.Tests` — suíte inteira (~25–30 min; 253 testes (suíte inteira a reexecutar — pendência 29) em 27/10/2026), inclusive a migração real contra o `DBMAIS.FDB` quando ele existe na máquina. Só as telas no navegador: `--filter "FullyQualifiedName~Navegador"`. Não rodar com o `dotnet run` aberto (trava o build). Ver [P26](docs/processos/P26-testes-ponta-a-ponta.md).

## Documentação
- [docs/README.md](docs/README.md) — índice: arquitetura, banco (um MD por objeto), telas, processos, regras, migração.
- [docs/01-ARQUITETURA-V2.md](docs/01-ARQUITETURA-V2.md) — stack, multiempresa, segurança, padrões de UI e de código.
- `Legacy/` — código da primeira versão (v1, modelo de dados errado), fora da compilação, só como referência.

## Licença
Uso privado.
