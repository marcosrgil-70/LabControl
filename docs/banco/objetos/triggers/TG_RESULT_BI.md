# TG_RESULT_BI

Resultado herda a empresa da amostra.

**Tabela:** [`LAB_RESULTADOS_ANALISES`](../tabelas/LAB_RESULTADOS_ANALISES.md) · **Momento:** BEFORE INSERT · **Criticidade:** ⚪ Normal · **Script:** `Scripts/v2/03_triggers.sql`

> Seções técnicas geradas a partir de `Scripts/v2/*.sql` (fonte da verdade). Mudou o script? Rode `node tools/gerador-docs/gerar.mjs` (regras de negócio ficam em `tools/gerador-docs/meta_*.mjs`).

## Substitui no legado
—

Comentário do script: _Resultados_

## Colunas que a trigger preenche/ajusta
- `NEW.ID_EMPRESAS`

## Validações (mensagens SQLSTATE 45000)
- — (não recusa operações)

## Variáveis de sessão
- —

## O que acessa
- **Altera outras tabelas:** —
- **Lê outras tabelas:** [`LAB_HIST_AMOSTRAS`](../tabelas/LAB_HIST_AMOSTRAS.md)
- **Chama:** —

## Impacto na aplicação
- A aplicação não precisa (e não deve depender de) enviar: `ID_EMPRESAS`. Reler a linha depois de gravar.

## Telas e processos relacionados
- **Telas:** [T26 Resultados](../../../telas/T26-resultados.md) · [T27 Boletim](../../../telas/T27-boletim.md)
- **Processos:** [P14 Lançamento de resultados](../../../processos/P14-lancamento-resultados.md) · [P15 Revisão e desfazer de resultado](../../../processos/P15-revisao-resultado.md) · [P11 Revisão da amostra (cascata)](../../../processos/P11-revisao-amostra.md)

## Código
```sql
CREATE TRIGGER TG_RESULT_BI BEFORE INSERT ON LAB_RESULTADOS_ANALISES FOR EACH ROW
BEGIN
  SET NEW.ID_EMPRESAS = (SELECT ID_EMPRESAS FROM LAB_HIST_AMOSTRAS WHERE ID_LAB_HIST_AMOSTRAS = NEW.ID_LAB_HIST_AMOSTRAS);
END
```
