# TG_AMOSTRAS_AI

Entrada automática no estoque (só revisão 0) + log de inclusão.

**Tabela:** [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) · **Momento:** AFTER INSERT · **Criticidade:** 🔴 Crítico · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`LAB_HIST_AMOSTRAS_AI0` (criava entrada em toda revisão) + `AI1`

Comentário do script: _Entrada automática no estoque (LAB_HIST_AMOSTRAS_AI0) somente na revisão 0 + log_

## Colunas que a trigger preenche/ajusta
- —

## Validações (mensagens SQLSTATE 45000)
- — (não recusa operações)

## Variáveis de sessão
- `@LC_MIGRACAO` — 1 = carga da migração: desliga numeração automática e validações (nunca usar fora da migração)
- `@LC_ID_USUARIOS` — usuário logado (gravado em logs); definido pelo interceptor de conexão a cada request

## O que acessa
- **Altera outras tabelas:** [`LAB_HIST_AMOSTRAS_LOG`](../tabelas/LAB_HIST_AMOSTRAS_LOG.md), [`LAB_MOV_AMOSTRAS`](../tabelas/LAB_MOV_AMOSTRAS.md)
- **Lê outras tabelas:** —
- **Chama:** —

## Impacto na aplicação
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T22 Recebimento de amostras](../../../telas/T22-recebimento-amostras.md) · [T23 Movimentação de amostras](../../../telas/T23-movimentacao-amostras.md) · [T24 Localização das amostras](../../../telas/T24-localizacao-amostras.md) · [T25 Etiquetas de amostras](../../../telas/T25-etiquetas-amostras.md) · [T26 Resultados](../../../telas/T26-resultados.md) · [T27 Boletim](../../../telas/T27-boletim.md) · [T28 Relatório de ensaio](../../../telas/T28-relatorio-ensaio.md) · [T29 Pesquisa de amostras](../../../telas/T29-pesquisa-amostras.md)
- **Processos:** [P04 Número da amostra e proteção do ano](../../../processos/P04-numero-amostra-ano.md) · [P09 Recebimento da amostra](../../../processos/P09-recebimento-amostra.md) · [P10 Datas parciais (fabricação/validade)](../../../processos/P10-datas-parciais.md) · [P11 Revisão da amostra (cascata)](../../../processos/P11-revisao-amostra.md) · [P12 Estoque e saldo das amostras](../../../processos/P12-estoque-amostras.md)

## Código
```sql
CREATE TRIGGER TG_AMOSTRAS_AI AFTER INSERT ON LAB_HIST_AMOSTRAS FOR EACH ROW
BEGIN
  IF COALESCE(@LC_MIGRACAO, 0) = 0 AND NEW.REVISAO = 0 AND NEW.QTDE_EMBALAGENS_ENTREGUE > 0 THEN
    INSERT INTO LAB_MOV_AMOSTRAS (ID_EMPRESAS, ID_LAB_HIST_AMOSTRAS, DATA_MOV, QTDE, E_S, JUSTIFICATIVA,
                                  AUTOMATICA, ID_ENTIDADES_FUNC, CRIADO_POR)
    VALUES (NEW.ID_EMPRESAS, NEW.ID_LAB_HIST_AMOSTRAS, NOW(), NEW.QTDE_EMBALAGENS_ENTREGUE, 'E',
            'INCLUSÃO AUTOMÁTICA PELO RECEBIMENTO DE AMOSTRAS', 1, NEW.ID_ENTIDADES_FUNC_DIG, @LC_ID_USUARIOS);
  END IF;
  IF COALESCE(@LC_MIGRACAO, 0) = 0 THEN
    INSERT INTO LAB_HIST_AMOSTRAS_LOG (ID_EMPRESAS, ID_LAB_HIST_AMOSTRAS, ACAO, DADOS, ID_USUARIOS)
    VALUES (NEW.ID_EMPRESAS, NEW.ID_LAB_HIST_AMOSTRAS, 'I',
            JSON_OBJECT('NR_AMOSTRA', NEW.NR_AMOSTRA, 'ID_ENTIDADES', NEW.ID_ENTIDADES,
                        'ID_LAB_PROPOSTAS', NEW.ID_LAB_PROPOSTAS, 'ID_PRODUTOS', NEW.ID_PRODUTOS,
                        'DT_ENTREGA', NEW.DT_ENTREGA, 'HR_ENTREGA', NEW.HR_ENTREGA,
                        'QTDE_EMBALAGENS_ENTREGUE', NEW.QTDE_EMBALAGENS_ENTREGUE, 'NR_LOTE', NEW.NR_LOTE,
                        'ID_AMOSTRAS_STATUS', NEW.ID_AMOSTRAS_STATUS, 'REVISAO', NEW.REVISAO,
                        'MOTIVO_REVISAO', NEW.MOTIVO_REVISAO), @LC_ID_USUARIOS);
  END IF;
END
```
