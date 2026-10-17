# Regras de Negócio — Catálogo

Cada regra tem um ID estável (`RN-xx.yy`) para ser citada em código (comentário/teste), PRs e MDs de tela.

**Colunas dos catálogos**
- **Camada** — onde a regra é garantida: **B** = banco (trigger/procedure/constraint, vale para qualquer cliente), **S** = serviço C# (backend), **T** = tela (ajuda/UX; nunca a única garantia).
- **Origem** — **L** = legado Delphi (mantida), **C** = correção de erro do legado, **N** = nova no v2, **D** = decisão do usuário.
- **Crit.** — 🔴 crítico · 🟠 importante · ⚪ normal.

| Arquivo | Domínio |
|---|---|
| [RN-01-gerais-multiempresa.md](RN-01-gerais-multiempresa.md) | Multiempresa, numeração, erros e transações, convenções gerais |
| [RN-02-seguranca.md](RN-02-seguranca.md) | Login, senhas, permissões, profissional do usuário, auditoria |
| [RN-03-cadastros.md](RN-03-cadastros.md) | Auxiliares, geografia, clientes, profissionais, produtos, parâmetros |
| [RN-04-propostas-pedidos.md](RN-04-propostas-pedidos.md) | Propostas, valores, revisões, pedidos de venda |
| [RN-05-amostras.md](RN-05-amostras.md) | Recebimento, número/ano, datas parciais, testes, revisão, etiquetas |
| [RN-06-estoque-arquivo.md](RN-06-estoque-arquivo.md) | Movimentação, saldo, arquivo e descarte |
| [RN-07-resultados.md](RN-07-resultados.md) | Lançamento, formatação, revisão e desfazer de resultados |
| [RN-08-boletim.md](RN-08-boletim.md) | Montagem, emissão/congelamento, revisão de boletim, relatório de ensaio |
| [RN-09-relatorios.md](RN-09-relatorios.md) | Regras de contagem dos relatórios gerenciais |

Processos que aplicam as regras: [../processos/](../processos/README.md). Objetos do banco que as garantem: [../banco/objetos/](../banco/objetos/README.md).
