# T23 — Movimentação de Amostras (estoque físico)

**Fase:** 7 · **Rota:** `/Movimentacao` · **Permissões:** `MOVIMENTACAO.CONSULTAR/INCLUIR/ALTERAR/EXCLUIR` · **Criticidade:** 🟠
**Legado:** `LabControl/un_MovimentacaoAmostras.pas` (1.260 linhas), menu *Movimentações → Movimentação de Amostras*. Tabelas `LAB_MOV_AMOSTRAS` (5.770, todas entradas automáticas), `LAB_MOV_AMOSTRAS_PARAM` (0), `LAB_HIST_AMOSTRAS_SALDO`.
**Objetos do banco:** [`LAB_MOV_AMOSTRAS`](../banco/objetos/tabelas/LAB_MOV_AMOSTRAS.md), [`LAB_MOV_AMOSTRAS_PARAM`](../banco/objetos/tabelas/LAB_MOV_AMOSTRAS_PARAM.md), [`LAB_HIST_AMOSTRAS_SALDO`](../banco/objetos/tabelas/LAB_HIST_AMOSTRAS_SALDO.md), [`LAB_HIST_AMOSTRAS_TESTES`](../banco/objetos/tabelas/LAB_HIST_AMOSTRAS_TESTES.md), [`VW_LAB_AMOSTRAS`](../banco/objetos/views/VW_LAB_AMOSTRAS.md), triggers [`TG_MOV_BI`](../banco/objetos/triggers/TG_MOV_BI.md), [`TG_MOV_BD`](../banco/objetos/triggers/TG_MOV_BD.md), [`TG_MOV_AI`](../banco/objetos/triggers/TG_MOV_AI.md)/[`AU`](../banco/objetos/triggers/TG_MOV_AU.md)/[`AD`](../banco/objetos/triggers/TG_MOV_AD.md), [`PR_ATUALIZA_SALDO`](../banco/objetos/rotinas/PR_ATUALIZA_SALDO.md)
**Processos:** [P12 Estoque e saldo](../processos/P12-estoque-amostras.md) · **Regras:** [RN-06](../regras/RN-06-estoque-arquivo.md)

## Como era no Delphi
1. **Busca de amostras**: escolher o **cliente** (obrigatório: *"É necessário informar um cliente para localizar as Amostras disponíveis."*) e clicar "Filtrar" → grid "Amostras disponíveis" do cliente (nº proposta, nº amostra, produto, lote, fabricação, validade, tipo de amostra, tipo de análise, status).
2. Filtros locais por proposta (nº com zeros à esquerda até 6, ano, revisão) e amostra (tipo com 2 dígitos, nº com 5, tipo de análise, ano com "20"+YY): o campo fica **verde** se encontrou e **vermelho** se não.
3. Ao posicionar numa amostra: mostra lote, fabricação, validade e **saldo atual**; carrega o histórico de movimentos e a lista de parâmetros (testes).
4. **Nova movimentação** (`cds_PrincipalNewRecord`): data = agora, tipo padrão **Saída**, funcionário = funcionário ativo, amostra = selecionada. Ao gravar: entrada manual vira `AMOSTRA_COMPLEMENTAR = 'C'` (complementar) e saída vira `'M'`; a entrada automática do recebimento é `'A'` e não permite trocar o tipo.
5. Saída maior que o saldo → *"Não existe saldo de amostras suficiente para esta saída."* (só aviso de tela; o banco aceitava).
6. **Seleção de parâmetros atendidos** pela saída (`Btn_SelParam` → `Btn_ConcluirSel`): lista os testes com cor indicando "já selecionado nesta movimentação" e "atendido em movimentações anteriores"; *"Já existe(m) Parâmetro(s) selecionado(s). Refazer a seleção atual?"*.
7. Excluir movimentação: *"Todas as informações referentes a esta Movimentação de Amostra serão apagados."* (duplo clique na coluna "Excluir" do histórico).

## Tela web
### Painel de busca
Cliente* (Tom Select) → "Filtrar"; filtros por nº da proposta / nº da amostra (aceita digitar `04-01382-01/26` completo ou partes); grid de amostras **última revisão** da empresa (`VW_LAB_AMOSTRAS`), com saldo e status.

### Painel da amostra selecionada
Cabeçalho só leitura: nº da amostra, produto, lote, fabricação, validade (datas parciais), **saldo atual**.

### Nova movimentação (modal)
| Campo | Coluna | Regra |
|---|---|---|
| Data/hora* | `DATA_MOV` | padrão agora; não futura |
| Tipo* | `E_S` | Entrada (complementar) / Saída; padrão Saída |
| Quantidade* | `QTDE` | > 0; saída ≤ saldo (validado na tela e pelo banco) |
| Justificativa | `JUSTIFICATIVA` | até 250; obrigatória na saída (novo) |
| Parâmetros atendidos | `LAB_MOV_AMOSTRAS_PARAM` | checklist dos testes da amostra, com selo "já atendido em movimento anterior" |
| (oculto) Profissional | `ID_ENTIDADES_FUNC` | profissional do usuário logado (exige vínculo) |

### Histórico
Data, tipo (badge E/S), quantidade, justificativa, profissional, parâmetros, automático (sim/não); excluir só movimentos manuais.

## Regras
- Movimento é sempre gravado na **amostra raiz** (revisão 0): `TG_MOV_BI` redireciona se vier o ID de uma revisão.
- Saída sem saldo → recusada pelo banco: *"Saldo insuficiente da amostra para esta saída."*
- A entrada automática do recebimento não pode ser excluída (*"A entrada automática do recebimento não pode ser excluída."*) e sua quantidade só muda pelo recebimento (T22) enquanto não houver movimento manual.
- Saldo recalculado pelo banco após cada inclusão/alteração/exclusão (`PR_ATUALIZA_SALDO`).
- Toda gravação em transação (movimento + parâmetros).

## Erros do legado a não repetir
- Estoque duplicado: cada **revisão** da amostra criava nova entrada automática (corrigido: só a raiz).
- Saldo negativo permitido no banco.
- Filtro de amostras dependia de digitar códigos com zeros manualmente.

## Critérios de aceite
- [ ] Saída maior que o saldo recusada com mensagem.
- [ ] Movimento numa revisão (`… R01`) aparece na raiz e o saldo é único.
- [ ] Entrada automática não pode ser excluída.
- [ ] Parâmetros atendidos gravados e exibidos no histórico.

**Status:** ⬜ (v1 tem Movimentação parcial — refazer sobre v2)
