# TG_AMOSTRAS_AD

Log de exclusão da amostra.

**Tabela:** [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md) · **Momento:** AFTER DELETE · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
`LAB_HIST_AMOSTRAS_AD2`

## Colunas que a trigger preenche/ajusta
- —

## Validações (mensagens SQLSTATE 45000)
- — (não recusa operações)

## Variáveis de sessão
- `@LC_ID_USUARIOS` — usuário logado (gravado em logs); definido pelo interceptor de conexão a cada request

## O que acessa
- **Altera outras tabelas:** [`LAB_HIST_AMOSTRAS_LOG`](../tabelas/LAB_HIST_AMOSTRAS_LOG.md)
- **Lê outras tabelas:** —
- **Chama:** —

## Impacto na aplicação
- —

## Telas e processos relacionados
- **Telas:** [T22 Recebimento de amostras](../../../telas/T22-recebimento-amostras.md) · [T23 Movimentação de amostras](../../../telas/T23-movimentacao-amostras.md) · [T24 Localização das amostras](../../../telas/T24-localizacao-amostras.md) · [T25 Etiquetas de amostras](../../../telas/T25-etiquetas-amostras.md) · [T26 Resultados](../../../telas/T26-resultados.md) · [T27 Boletim](../../../telas/T27-boletim.md) · [T28 Relatório de ensaio](../../../telas/T28-relatorio-ensaio.md) · [T29 Pesquisa de amostras](../../../telas/T29-pesquisa-amostras.md)
- **Processos:** [P04 Número da amostra e proteção do ano](../../../processos/P04-numero-amostra-ano.md) · [P09 Recebimento da amostra](../../../processos/P09-recebimento-amostra.md) · [P10 Datas parciais (fabricação/validade)](../../../processos/P10-datas-parciais.md) · [P11 Revisão da amostra (cascata)](../../../processos/P11-revisao-amostra.md) · [P12 Estoque e saldo das amostras](../../../processos/P12-estoque-amostras.md)

## Código
```sql
CREATE TRIGGER TG_AMOSTRAS_AD AFTER DELETE ON LAB_HIST_AMOSTRAS FOR EACH ROW
BEGIN
  INSERT INTO LAB_HIST_AMOSTRAS_LOG (ID_EMPRESAS, ID_LAB_HIST_AMOSTRAS, ACAO, DADOS, ID_USUARIOS)
  VALUES (OLD.ID_EMPRESAS, OLD.ID_LAB_HIST_AMOSTRAS, 'E',
          JSON_OBJECT('NR_AMOSTRA', OLD.NR_AMOSTRA, 'ID_ENTIDADES', OLD.ID_ENTIDADES,
                      'ID_LAB_PROPOSTAS', OLD.ID_LAB_PROPOSTAS, 'DT_ENTREGA', OLD.DT_ENTREGA), @LC_ID_USUARIOS);
END
```
