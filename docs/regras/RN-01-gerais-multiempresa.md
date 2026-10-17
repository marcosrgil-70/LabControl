# RN-01 — Regras Gerais e Multiempresa

Legenda: Camada **B** banco · **S** serviço · **T** tela | Origem **L** legado · **C** correção · **N** nova · **D** decisão do usuário. [Catálogo](README.md)

| ID | Regra | Camada | Origem | Crit. | Onde |
|---|---|---|---|---|---|
| RN-01.01 | Todo cadastro e movimento pertence a uma empresa (`ID_EMPRESAS`); globais apenas `EMPRESAS`, `PAISES`, `ESTADOS`, `CIDADES`, `USUARIOS`, `PERMISSOES` | B S | D | 🔴 | [P01](../processos/P01-multiempresa.md) |
| RN-01.02 | Toda consulta da aplicação filtra a empresa ativa (Global Query Filter, inclusive views e SQL cru); proibido `?? 1` | S | C | 🔴 | [P01](../processos/P01-multiempresa.md) |
| RN-01.03 | Registros filhos herdam a empresa do pai pelas triggers (item, análise, teste, resultado, boletim, movimento, contatos) | B | N | 🔴 | [objetos](../banco/objetos/README.md#triggers-por-tabela) |
| RN-01.04 | Documento só referencia registros da mesma empresa (proposta da amostra, resultado do boletim, produto da troca) | B | C | 🔴 | [`TG_AMOSTRAS_BI`](../banco/objetos/triggers/TG_AMOSTRAS_BI.md), [`PR_REVISAO_BOLETIM`](../banco/objetos/rotinas/PR_REVISAO_BOLETIM.md) |
| RN-01.05 | Combos/lookups mostram só registros da empresa ativa e ativos; inativos continuam visíveis nos registros antigos | S T | L | 🟠 | [T10-00](../telas/T10-00-crud-auxiliares.md) |
| RN-01.06 | Trocar de empresa recarrega permissões e profissional e volta para a Home | S | L | 🟠 | [T00](../telas/T00-login-empresa.md) |
| RN-01.07 | Números de amostra, proposta, pedido, produto e RE são gerados **só** pelo banco (`SP_PROXIMO_NUMERO`, com bloqueio); a aplicação nunca calcula `MAX+1` | B | C | 🔴 | [P03](../processos/P03-numeracao-documentos.md) |
| RN-01.08 | Números anuais usam o **ano do servidor** e uma sequência por (empresa, tipo, ano) que nunca é "zerada" | B | C/D | 🔴 | [P03](../processos/P03-numeracao-documentos.md), [P04](../processos/P04-numero-amostra-ano.md) |
| RN-01.09 | `UNIQUE` impede número duplicado (amostra, proposta, pedido, produto, RE, boletim) | B | C | 🔴 | [`SEQUENCIAS_DOCUMENTOS`](../banco/objetos/tabelas/SEQUENCIAS_DOCUMENTOS.md) |
| RN-01.10 | Ajuste manual de numeração: só ADMIN_GLOBAL, nunca menor que o máximo usado, com motivo em auditoria | S T | N | 🟠 | [T05](../telas/T05-parametros-empresa.md) |
| RN-01.11 | Erro SQLSTATE 45000 é mensagem de negócio: mostrar o texto ao usuário (não é erro técnico) | S T | N | 🔴 | [P22](../processos/P22-erros-transacoes.md) |
| RN-01.12 | Procedures `SP_*` com transação própria não podem ser chamadas dentro de transação do EF | S | N | 🔴 | [P22](../processos/P22-erros-transacoes.md) |
| RN-01.13 | Variáveis de sessão (`@LC_ID_USUARIOS`, `@LC_MIGRACAO = 0`, `@LC_PERMITE_RETROATIVO = 0`) definidas em toda abertura de conexão | S | N | 🔴 | [P22](../processos/P22-erros-transacoes.md) |
| RN-01.14 | Revisões de documentos (proposta, amostra, resultado, boletim) só pelas procedures `SP_GERAR_REVISAO_*`, com motivo; revisão antiga é somente leitura | B S | C | 🔴 | [P07](../processos/P07-revisao-proposta.md), [P11](../processos/P11-revisao-amostra.md), [P15](../processos/P15-revisao-resultado.md), [P18](../processos/P18-revisao-boletim.md) |
| RN-01.15 | Schema só pelos scripts `Scripts/v2/*.sql` (sem EF migrations nem patches no `Program.cs`) | — | D | 🔴 | [CLAUDE.md](../../CLAUDE.md) |
| RN-01.16 | Consultas abrem com o período padrão `DIAS_PESQUISA_PADRAO` (365 dias) | T | L | ⚪ | [T05](../telas/T05-parametros-empresa.md) |
| RN-01.17 | Valores monetários `DECIMAL(15,2)`, quantidades `DECIMAL(15,4)`, datas `DATE`/`TIME`, booleanos 0/1, cores `#RRGGBB` | B | N | ⚪ | [banco/11 §1](../banco/11-MODELO-MYSQL.md) |
| RN-01.18 | Nova empresa: `SP_INICIALIZAR_EMPRESA` + cópia opcional de cadastros (nunca clientes ou movimentos) | B S | N | 🟠 | [P23](../processos/P23-nova-empresa.md) |
