# TG_AMOSTRAS_AU

Atualiza a quantidade da entrada automática + log de alteração.

**Tabela:** [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) · **Momento:** AFTER UPDATE · **Criticidade:** 🟠 Importante · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`LAB_HIST_AMOSTRAS_AU0/AU1`

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
- **Chama:** [`FN_DATA_PARCIAL`](../rotinas/FN_DATA_PARCIAL.md)

## Impacto na aplicação
- Com `@LC_MIGRACAO = 1` (só na migração) as validações/numeração ficam desligadas.

## Telas e processos relacionados
- **Telas:** [T22 Recebimento de amostras](../../../telas/T22-recebimento-amostras.md) · [T23 Movimentação de amostras](../../../telas/T23-movimentacao-amostras.md) · [T24 Localização das amostras](../../../telas/T24-localizacao-amostras.md) · [T25 Etiquetas de amostras](../../../telas/T25-etiquetas-amostras.md) · [T26 Resultados](../../../telas/T26-resultados.md) · [T27 Boletim](../../../telas/T27-boletim.md) · [T28 Relatório de ensaio](../../../telas/T28-relatorio-ensaio.md) · [T29 Pesquisa de amostras](../../../telas/T29-pesquisa-amostras.md)
- **Processos:** [P04 Número da amostra e proteção do ano](../../../processos/P04-numero-amostra-ano.md) · [P09 Recebimento da amostra](../../../processos/P09-recebimento-amostra.md) · [P10 Datas parciais (fabricação/validade)](../../../processos/P10-datas-parciais.md) · [P11 Revisão da amostra (cascata)](../../../processos/P11-revisao-amostra.md) · [P12 Estoque e saldo das amostras](../../../processos/P12-estoque-amostras.md)

## Código
```sql
CREATE TRIGGER TG_AMOSTRAS_AU AFTER UPDATE ON LAB_HIST_AMOSTRAS FOR EACH ROW
BEGIN
  IF COALESCE(@LC_MIGRACAO, 0) = 0 AND NEW.REVISAO = 0
     AND NEW.QTDE_EMBALAGENS_ENTREGUE <> OLD.QTDE_EMBALAGENS_ENTREGUE AND NEW.QTDE_EMBALAGENS_ENTREGUE > 0 THEN
    UPDATE LAB_MOV_AMOSTRAS
       SET QTDE = NEW.QTDE_EMBALAGENS_ENTREGUE, DATA_MOV = NOW(),
           JUSTIFICATIVA = 'ALTERAÇÃO NO RECEBIMENTO DE AMOSTRAS'
     WHERE ID_LAB_HIST_AMOSTRAS = NEW.ID_LAB_HIST_AMOSTRAS AND AUTOMATICA = 1;
  END IF;
  IF COALESCE(@LC_MIGRACAO, 0) = 0 AND NOT (NEW.ULTIMA_REVISAO <> OLD.ULTIMA_REVISAO) THEN
    INSERT INTO LAB_HIST_AMOSTRAS_LOG (ID_EMPRESAS, ID_LAB_HIST_AMOSTRAS, ACAO, DADOS, ID_USUARIOS)
    VALUES (NEW.ID_EMPRESAS, NEW.ID_LAB_HIST_AMOSTRAS, 'A',
            JSON_OBJECT('NR_AMOSTRA', NEW.NR_AMOSTRA, 'ID_ENTIDADES', NEW.ID_ENTIDADES,
                        'ID_LAB_PROPOSTAS', NEW.ID_LAB_PROPOSTAS, 'ID_PRODUTOS', NEW.ID_PRODUTOS,
                        'DT_ENTREGA', NEW.DT_ENTREGA, 'HR_ENTREGA', NEW.HR_ENTREGA,
                        'QTDE_EMBALAGENS_ENTREGUE', NEW.QTDE_EMBALAGENS_ENTREGUE, 'NR_LOTE', NEW.NR_LOTE,
                        'VALIDADE', FN_DATA_PARCIAL(NEW.VALIDADE_DIA, NEW.VALIDADE_MES, NEW.VALIDADE_ANO),
                        'FABRICACAO', FN_DATA_PARCIAL(NEW.FABRICACAO_DIA, NEW.FABRICACAO_MES, NEW.FABRICACAO_ANO),
                        'ID_AMOSTRAS_STATUS', NEW.ID_AMOSTRAS_STATUS, 'ID_AMOSTRAS_STATUS_ANTERIOR', OLD.ID_AMOSTRAS_STATUS,
                        'ID_ENTIDADES_FUNC_RESP', NEW.ID_ENTIDADES_FUNC_RESP), @LC_ID_USUARIOS);
  END IF;
END
```
