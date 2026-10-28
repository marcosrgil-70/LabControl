# 12 — Procedures de Revisão e Numeração (erros do Firebird × correção v2)

> Implementação: `Scripts/v2/02_funcoes_procedures.sql` e `03_triggers.sql`.
> Testado em 16/10/2026 (ver seção 7).

## 0. Conceito de revisão (como deve funcionar)

Laboratório acreditado (ISO/IEC 17025) **não altera documento emitido**: gera uma **revisão**, guarda a anterior intacta e registra o motivo.

```
PROPOSTA  000123/26-R0 ──revisão──► 000123/26-R1 ──► R2          (pedido de venda acompanha a proposta)
AMOSTRA   04-01382-01/26 ──revisão──► 04-01382-01/26 R01         (estoque fica na R0 = "raiz")
RESULTADO rev 0 ──────────revisão──► rev 1 ──► rev 2             (numeração contínua por amostra física)
BOLETIM   04-01382-01/26 rev 0 ─────► rev 1 ──► rev 2            (código constante; só a última é editável)
```

Encadeamento automático (igual ao legado, mas correto):

| Ação do usuário | Cria |
|---|---|
| Revisar **amostra** (só se já houver resultado) | nova amostra R+1 + cópia dos testes + **revisão do resultado** (na amostra nova) + **revisão do boletim** (se houver) |
| Revisar **resultado** | novo resultado rev+1 (mesma amostra) + **revisão do boletim** (opcional, padrão sim) |
| Revisar **boletim** | novo boletim rev+1 apontando para o **resultado vigente** |
| Revisar **proposta** | proposta R+1 com textos, produtos e análises |

Em todas: motivo obrigatório, só se revisa a **última** revisão, a anterior fica `ULTIMA_REVISAO = 0` (somente leitura), a nova guarda `ID_*_ORIGEM`.

## 1. Numeração de documentos

### Legado (`SP_GET_COD_AMOSTRA`, `SP_GET_COD_PROPOSTAS`, `SP_GET_COD_BOLETIM`, `SP_GET_ULT_PED_VENDA`)
- Contador único em `EMPRESAS_PARAMETROS.ULT_*`, "zerado" quando o ano da **última amostra digitada** é menor que o ano atual.
- Ano tirado da data digitada; sem bloqueio; chamado duas vezes pela tela.

### v2 (`SP_PROXIMO_NUMERO` + triggers)
```sql
INSERT INTO SEQUENCIAS_DOCUMENTOS (...) VALUES (emp, tipo, ano, 1)
ON DUPLICATE KEY UPDATE ULTIMO_NUMERO = ULTIMO_NUMERO + 1;   -- trava exclusiva até o COMMIT
SELECT ULTIMO_NUMERO INTO n ...;
-- (versão anterior INSERT IGNORE + SELECT … FOR UPDATE dava deadlock com gravações simultâneas — corrigido em 19/10/2026)
```
- `TG_AMOSTRAS_BI`: na revisão 0, `ANO_AMOSTRA = YEAR(CURDATE())` e `COD_AMOSTRA = SP_PROXIMO_NUMERO('AMOSTRA', ano)`. O que a aplicação mandar nesses campos é **ignorado**.
- Proteção contra erro de digitação do ano:
  1. data futura → erro;
  2. data com mais de `EMPRESAS_PARAMETROS.DIAS_RETROATIVOS_ENTREGA` dias (padrão 7) → erro "verifique o ano digitado", **a não ser** que a aplicação defina `@LC_PERMITE_RETROATIVO = 1` (só depois de confirmação explícita **e** permissão `AMOSTRAS.ENTREGA_RETROATIVA`);
  3. mesmo liberada, a data antiga **não muda o ano nem reinicia a sequência**: a amostra recebe o ano do servidor.
- Na virada do ano, a primeira amostra de 2027 cria a linha `(emp, 'AMOSTRA', 2027)` com 1 — nada é "zerado".
- `UNIQUE (ID_EMPRESAS, ANO_AMOSTRA, COD_AMOSTRA, REVISAO)` impede duplicidade.
- A tela mostra "INCLUSÃO" no número até gravar e exibe o número **devolvido pelo banco** depois do INSERT.

## 2. SP_GERAR_REVISAO_PROPOSTAS → `SP_GERAR_REVISAO_PROPOSTA`

| # | Erro no Firebird | Correção v2 |
|---|---|---|
| P1 | Verifica se existe a **revisão 0**, mas copia de `I_REVISAO`; se `I_REVISAO` não existe, `V_ID_OLD` fica nulo e a procedure devolve "OK" sem copiar nada | Valida a **origem** pelo ID e só aceita a última revisão |
| P2 | `ST_REV_ANTERIOR` vem de um `SELECT FIRST 1` **não correlacionado** ao parâmetro/produto (mesmo valor para todas as linhas e sem filtro de empresa no join) | `ST_REV_ANTERIOR = 1` para tudo que veio da origem; linhas novas ficam 0 (a tela destaca o que mudou) |
| P3 | Copia `ID_PEDIDOS_VENDAS` → o mesmo pedido em várias revisões (19 casos) | Pedido não é copiado; fica em `PEDIDOS_VENDA` (1 por proposta) e é atualizado por `SP_ATUALIZAR_PEDIDO_REVISAO` |
| P4 | Copia o status da origem (ex.: "Enviado ao cliente") | Nova revisão nasce no status inicial da empresa; `DT_ENVIO_CLIENTE` e `DT_AUTORIZACAO` zerados |
| P5 | `MAX(REV)+1` sem bloqueio | `SELECT … FOR UPDATE` em todas as revisões da proposta |
| P6 | Revisão anterior continua editável | `ULTIMA_REVISAO = 0` + trigger somente leitura (cabeçalho, produtos e análises) |
| P7 | Não registra motivo nem origem | `MOTIVO_REVISAO` obrigatório, `ID_LAB_PROPOSTAS_ORIGEM` |
| P8 | Textos copiados um por um (5 tabelas); produtos copiados com a mesma PK composta | `LAB_PROPOSTAS_TEXTOS` (1 linha); produtos com nova chave e `ID_ORIGEM`; análises remapeadas por `ID_ORIGEM` |
| P9 | Totais copiados (podiam já estar errados) | `SP_RECALCULAR_PROPOSTA` no final |

## 3. SP_GERAR_REVISAO_AMOSTRA → `SP_GERAR_REVISAO_AMOSTRA`

| # | Erro no Firebird | Correção v2 |
|---|---|---|
| A1 | Procura a amostra comparando o texto `LPAD(...)\|\|'-'\|\|...` em **toda** a tabela (não usa índice e depende de formatação) | Busca por ID; bloqueio por (empresa, ano, código) |
| A2 | Mesmo problema do P1 (valida a revisão 0, copia `I_REVISAO`) | Valida a origem e exige a última revisão |
| A3 | **Não copia `COLETOR` e `DATAHORACOLETA`** (61 revisões perderam o dado) | Cópia de todas as colunas, inclusive observação |
| A4 | A trigger `AI0` cria uma **nova entrada de estoque** para a revisão (estoque em dobro) | Entrada automática só na revisão 0; movimentos/saldo/localização sempre na raiz (`ID_AMOSTRA_RAIZ`) |
| A5 | Chama a revisão de resultado com `MAX(REVISAO)` dos resultados da amostra clicada; a procedure de resultado exige "revisão 0 existe **nesta** amostra" → se a amostra clicada já é revisão, o resultado **não é copiado** e nada avisa | Copia a última revisão do resultado da origem para a amostra nova |
| A6 | Mapeia os testes por (tipo de análise, parâmetro) com `SELECT` singleton → erro "multiple rows" quando o parâmetro aparece 2× (18 amostras) | `ID_TESTE_ORIGEM` em cada teste copiado |
| A7 | Amostra antiga continua "Liberada" e aparece nas listas de resultado/boletim | `ULTIMA_REVISAO = 0`; telas listam só a última |
| A8 | Saída `O_REV_RESULTADO` devolve a revisão da **amostra** (nome errado) | Saídas: id/revisão da amostra, id do resultado novo e id do boletim novo |
| A9 | Sem transação própria; erros no meio deixam meia revisão | Transação + `ROLLBACK`/`RESIGNAL` |

## 4. SP_GERAR_REVISAO_RESULTADOS → `SP_GERAR_REVISAO_RESULTADO`

| # | Erro no Firebird | Correção v2 |
|---|---|---|
| R1 | Valida "revisão 0 existe" e copia `I_REVISAO` (P1) | Valida a origem (última revisão) |
| R2 | Mapeamento de teste singleton (A6) | Mesma amostra: mesmo teste; outra amostra: `ID_TESTE_ORIGEM`. Se algum resultado ficar sem teste, a revisão é cancelada com mensagem |
| R3 | Chama `SP_GERAR_REVISAO_BOLETIM(empresa, V_ID_OLD, V_ID)`, mas a procedure de boletim **ignora** o 3º parâmetro → o boletim novo aponta para o resultado **antigo** e lista os valores **antigos** | O boletim novo recebe o ID do resultado novo e os parâmetros mapeados por `ID_PARAM_ORIGEM` |
| R4 | Numeração `MAX(REVISAO)+1` só da amostra atual (ao revisar a amostra, recomeça a contagem dela) | Numeração contínua por amostra física (todas as revisões da raiz), com bloqueio só da linha da amostra raiz (D-23) |
| R5 | Revisões antigas editáveis/excluíveis fora de ordem | `ULTIMA_REVISAO` + `TG_RESULT_BU/BD`; desfazer só via `SP_EXCLUIR_REVISAO_RESULTADO` |
| R6 | Resultado com boletim emitido podia ser alterado (a trava nunca existiu) | `TG_RESPAR_BU/BD` bloqueiam e pedem revisão |

## 5. SP_GERAR_REVISAO_BOLETIM → `SP_GERAR_REVISAO_BOLETIM`

| # | Erro no Firebird | Correção v2 |
|---|---|---|
| B1 | Busca `COD_BOLETIM` com `SELECT … WHERE ID_LAB_RESULTADO_ANALISES = :id` singleton. A tela de boletim chama a revisão passando o **mesmo** resultado nos dois parâmetros → o resultado passa a ter 2 boletins e a próxima revisão dá **"multiple rows in singleton select"** (120 resultados já estão assim) | Origem identificada pelo **ID do boletim** |
| B2 | Ignora `I_ID_LAB_RESULT_ANALISES_NEW` (ver R3) | Usa o resultado novo (ou o vigente, se o resultado foi revisado depois) |
| B3 | Copia `DH_EMISSAO`, `DH_GERACAO`, `DT_BOLETIM` e o emissor da origem → a revisão nasce "emitida" (e a trigger impediria editá-la) | Nasce **não emitida**, com data de hoje, gerador = profissional do usuário, status inicial |
| B4 | Copia **todos** os parâmetros do resultado, inclusive os que o usuário **não** tinha escolhido para o boletim | Copia só os parâmetros do boletim de origem, mantendo a ordem |
| B5 | Usa `DESC_REDUZIDA` que pode ser nula numa coluna `NOT NULL` | `DESC_REDUZIDA` passou a ser obrigatória no cadastro de parâmetros |
| B6 | Texto do resultado montado com `SP_FORMATDOUBLE` e truncado em `VARCHAR(15)` | `RESULTADO_FORMATADO` (até 80 caracteres) calculado pela trigger do resultado |
| B7 | `COD_BOLETIM` herda o " R01" da amostra revisada e a revisão do boletim recomeça | `COD_BOLETIM` = número base constante; `REVISAO` contínua |
| B8 | Metodologias copiadas da origem (podem estar desatualizadas após revisar a amostra) | Recalculadas por `FN_OBS_METODOS` a partir dos testes da amostra (acreditado / não acreditado) |
| B9 | `IF (OLD.DH_EMISSAO > 0)` compara timestamp com inteiro; e `DH_EMISSAO` nunca era gravado | `SP_EMITIR_BOLETIM` grava `DH_EMISSAO`; `TG_BOLETIM_BU/BD` e `TG_BOLPAR_*` congelam |

## 6. Outras correções

| Legado | Erro | v2 |
|---|---|---|
| `SP_TROCAPRODUTO` | Atualiza o produto de **todas** as amostras do banco que tenham o produto antigo (`FOR SELECT` sem filtro) | `SP_TROCAR_PRODUTO_PROPOSTA` filtra pela proposta e valida a empresa |
| `SP_OBSMETODOS` | Busca o método na proposta pelo parâmetro (pode pegar o método de outro produto) | `FN_OBS_METODOS` usa o método gravado no teste da amostra |
| `LAB_RELATORIO_ENSAIO_BI` | `ULT_COD_RE = NEW.ID_EMPRESAS` | Número anual via `SP_PROXIMO_NUMERO('RELATORIO_ENSAIO')` |
| `LAB_HIST_AMOSTRAS_AU0` | Bloqueia qualquer alteração da amostra com movimento manual (até mudar status) | Bloqueia só a mudança de quantidade |
| `TG_LAB_MOV_AMOSTRAS_AIUD` | Permite saldo negativo | `TG_MOV_BI` bloqueia saída sem saldo |
| `qry_Amostra` (tela de Resultados) | Lista amostras **sem filtrar a empresa** | Todas as views/consultas filtram `ID_EMPRESAS` |

## 7. Roteiro de teste (executado em 16/10/2026 — reexecutar após qualquer mudança)

> Automatizado em [`Scripts/v2/teste/roteiro_12_7.sql`](../../Scripts/v2/teste/roteiro_12_7.sql) (49 verificações — o passo 1 inclui os perfis sugeridos e o passo 13 cobre a revisão da Fase 0; termina com erro se alguma falhar). Rodar **só num banco temporário** criado pelos scripts 01..05. O mesmo roteiro **pela aplicação** (EF + `Infra/Procedures.cs`) está em [`tests/LabControl.Tests/RoteiroTests.cs`](../../tests/LabControl.Tests/RoteiroTests.cs) (`dotnet test`). Última execução: **19/10/2026 — 49/49 OK**.
> No passo 12 o script confere "último número + 1" (as recusas do passo 11 **não** consomem número): na execução de 19/10 a amostra retroativa recebeu a sequência **2**.

1. `SP_INICIALIZAR_EMPRESA(1)` → status/idiomas/moedas/perfil ADMINISTRADOR + 4 perfis sugeridos (cada ação com o CONSULTAR da tela); reinicializar não devolve permissão retirada.
2. Proposta com 2 análises (100×2 e 50×2, 10% de desconto geral) → `NR_PROPOSTA = 000001/26-R0`, total das análises 300,00, desconto 30,00, total 270,00. ✅
3. Produto com código vazio → `000001`. ✅
4. `SP_GERAR_PEDIDO_VENDA` → pedido nº 1; amostra recebida depois herda o pedido. ✅
5. Amostra enviada com `ANO=1999, COD=999` → gravada como `04-00001-01/26` (ano/número do servidor); validade só mês/ano → `05/2027`; saldo 3; acreditação mista = 2; prazo +10 dias. ✅
6. Resultado numérico `<`, `1,0`, exp 2, UFC/g → `< 1,0e2 UFC/g`; descritivo → `Ausência em 25 g`. ✅
7. Boletim com **1** dos 2 parâmetros, metodologia acreditada `CONT. TOTAL: ISO 4833`; `SP_EMITIR_BOLETIM` → emitido. ✅
8. `SP_GERAR_REVISAO_AMOSTRA` → amostra `… R01` (raiz = 1), resultado rev 1 na amostra nova, boletim rev 1 **não emitido** com **1** parâmetro apontando para o resultado novo; versões antigas com `ULTIMA_REVISAO = 0`. ✅
9. `SP_GERAR_REVISAO_RESULTADO` → resultado rev 2 + boletim rev 2. ✅
10. `SP_GERAR_REVISAO_PROPOSTA` → `000001/26-R1`, 2 análises com `ST_REV_ANTERIOR = 1`, total recalculado. ✅
11. Travas (todas recusadas com mensagem): alterar boletim emitido; alterar revisão antiga da amostra; alterar análise de revisão antiga da proposta; saída maior que o saldo; segundo pedido para a mesma proposta; validade 31/02; entrega 1 ano atrás sem liberação. ✅
12. Entrega 1 ano atrás **com** `@LC_PERMITE_RETROATIVO = 1` → gravada no ano **2026**, sequência 3 (sem reinício). ✅
13. Revisão da Fase 0 (19/10/2026): AUDITORIA gravada pelas procedures (R, M, I); proposta enviada com `COD=900/ANO=1999` e pedido com `NR_PEDIDO=57` recebem o número do banco; revisão de proposta/resultado incluída fora da `SP_GERAR_REVISAO_*` recusada; filho com `ID_EMPRESAS` diferente do pai recusado. ✅
