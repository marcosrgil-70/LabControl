# P24 — Migração Firebird → MySQL v2 (visão geral)

**Criticidade:** 🔴🔴 · **Fases:** 3.5, 4.1, 5.1 e 9.1
**Telas:** [T07 Migração](../telas/T07-migracao.md)
**Documento base:** [banco/13-MIGRACAO-DADOS.md](../banco/13-MIGRACAO-DADOS.md) · **Anomalias:** [banco/10 §8](../banco/10-ANALISE-FIREBIRD.md#8-anomalias-de-dados-encontradas-tratar-na-migração)
**Etapas detalhadas (um MD por etapa):** [processos/migracao/](migracao/README.md)

## Princípios
1. Banco destino **recriado do zero** pelos scripts `Scripts/v2/01..05` a cada execução completa (sem migração incremental).
2. Conexão MySQL com `SET @LC_MIGRACAO = 1` (triggers não numeram nem validam; números do legado preservados).
3. Tudo vai para a **empresa 1** (`COD_EMPRESA = '001'`).
4. `ID_LEGADO` em cada tabela + dicionário `idAntigo → idNovo` por tabela.
5. Uma transação por etapa, log por etapa, relatório final com conferências e anomalias.
6. Strings ISO8859_1 → UTF-8 (`Charset=ISO8859_1` no FirebirdClient) com `Trim()`; booleanos 'S'/'N' → 1/0; cores `TColor` → `#RRGGBB`.

## Etapas
| Parte (fase) | Etapas |
|---|---|
| Parte 1 (fase 3.5) — cadastros | [M-01](migracao/M-01-empresa.md) a [M-10](migracao/M-10-produtos-parametros.md) |
| Parte 2 (fase 4.1) — propostas, pedidos, amostras | [M-11](migracao/M-11-propostas.md) a [M-15](migracao/M-15-testes-estoque-arquivo.md) |
| Parte 3 (fase 5.1) — resultados, boletins, sequências | [M-16](migracao/M-16-resultados.md) a [M-18](migracao/M-18-sequencias.md) |

## Encerramento
1. `SET @LC_MIGRACAO = 0`.
2. Conferências obrigatórias (doc 13 §3) todas verdes.
3. Login com um usuário migrado (troca de senha obrigatória).
4. Roteiro do doc 12 §7 numa **cópia** do banco migrado.
5. `mysqldump --routines --triggers labcontrol_v2`.

## Riscos e mitigação
| Risco | Mitigação |
|---|---|
| Número de amostra diferente do impresso | `CODIGO` dos tipos = ID antigo; `NR_AMOSTRA` recalculado e comparado com o legado (20 sorteados + todos no relatório) |
| Revisões com raiz/origem erradas | cálculo por (empresa, ano, código) ordenado pela revisão; conferência "1 `ULTIMA_REVISAO` por documento" |
| Estoque duplicado | descartar movimentos de revisões > 0 e recalcular saldos |
| Boletins editáveis depois da migração | `DH_EMISSAO = DH_GERACAO` em todos |
| Encoding | conferir acentos em 20 nomes sorteados |

## Decisões do usuário e implementação (parte 1, tarefa 3.5 — 21/10/2026)
- **Destino:** banco **separado e vazio** (ex. `labcontrol_migracao`, `.\Scripts\v2\recriar.ps1 -Banco labcontrol_migracao -Forcar`), executado pela T07 a partir do app no `labcontrol_v2`. Para usar o banco migrado, aponte o user-secret `ConnectionStrings:LabControl` para ele.
- **Ou o próprio banco em uso, recém-instalado (29/10/2026):** o instalador cria o `LabControlDB` com a empresa e o ADMINISTRADOR / Admin123; a T07 aceita esse banco como destino, apaga o conteúdo inicial e migra **numa transação só** (falha desfaz tudo). Regra completa em [T07](../telas/T07-migracao.md).
- **Usuários:**
  - **senha provisória única** para todos (dúvida 7 do plano), digitada na tela e nunca gravada em claro;
  - troca obrigatória no 1º acesso;
  - todos com o perfil **ADMINISTRADOR**; USUCOD 1 → ADMIN_GLOBAL.
- **Entidade da empresa:**
  - a entidade 4 do legado era empresa usuária **e** cliente: virou a entidade da empresa (M-01) + um cadastro de cliente com o mesmo CNPJ (M-05, D-22);
  - os contatos dela vão para os dois;
  - as propostas (parte 2) apontam para o cliente.
- **Geografia:** cidades casadas por nome + UF com o seed IBGE; as que não casam são incluídas sem código e listadas.
- **Código:** `Services/Migracao/` (um arquivo por etapa em `Etapas/`); integração real em `MigracaoTests`.

## Decisões do usuário e implementação (parte 2, tarefa 4.1 — 23/10/2026)
- **Uma execução só:** a T07 roda M-01→M-18 em sequência num banco recém-criado. Os dicionários `idAntigo → idNovo` ficam em memória do começo ao fim, e não existe "parte 2" sobre um banco que já tem a parte 1.
- **Log das amostras do legado:** migrado e marcado como `"ORIGEM":"LEGADO"` / sem data. O legado não guardava data, usuário nem chave.
- **INSERT direto em lote** nas tabelas cujas colunas o banco preenche fora da migração (o EF não as envia):
  - os IDs voltam na ordem;
  - o lote é recusado se os IDs não vierem em sequência.
- **Correções de dados do legado** (todas listadas no relatório), detalhadas em [M-11](migracao/M-11-propostas.md)…[M-17](migracao/M-17-boletins.md):
  - validações das triggers que valem mesmo com `@LC_MIGRACAO = 1` (validade < solicitação, datas parciais);
  - referências inexistentes;
  - itens ausentes;
  - pedidos duplicados;
  - movimentos de revisões;
  - na parte 3 (5.1): datas do resultado invertidas, testes de outra amostra ou repetidos no resultado, boletins na amostra vizinha e linhas de boletim de parâmetros apagados e relançados.
