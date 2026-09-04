# Movimentação de Amostras

**Controller:** `MovimentacaoController`
**Rota base:** `/Movimentacao`
**Views:** `Views/Movimentacao/Index.cshtml`, `Movimentar.cshtml`

---

## Telas

### Busca de Amostras (`GET /Movimentacao`)
Filtros de pesquisa:
- **Cliente** (select)
- **Tipo de Amostra** (select)
- **Tipo de Análise** (select)
- **Nr. Amostra** (campo texto)
- **Nr. Proposta** (campo texto)
- **Ano** (número)

Sem filtros → lista vazia (nenhuma amostra exibida por padrão).

Resultado (quando filtros aplicados):
- Tabela com colunas: Código, Cliente, Produto, Nr. Lote, Tipo Amostra, Tipo Análise, Status (badge), Saldo (verde/vermelho), Ação "Movimentar"

### Tela de Movimentação (`GET /Movimentacao/Movimentar/{id}`)
Dividida em 3 seções:

**Seção Esquerda — Info da Amostra (read-only):**
- Código formatado, Cliente, Produto, Nr. Lote, Tipo de Amostra, Status, Saldo atual

**Seção Direita — Formulário de Nova Movimentação:**
| Campo | Tipo | Regra |
|---|---|---|
| **Data/Hora** | DateTime | Padrão: agora |
| **Entrada / Saída** | Radio (E / S) | Obrigatório |
| **Quantidade** | Decimal | Obrigatório; para Saída: deve ser ≤ saldo atual (validado no JS e no servidor) |
| **Justificativa** | Texto | Opcional |
| **Parâmetros** | Checkboxes | Opcional; lista os parâmetros da amostra; badge "já movimentado" (verde) em parâmetros que já tiveram movimentação |

**Seção Inferior — Histórico de Movimentações:**
- Grid com: Data, Tipo (Entrada/Saída com badge), Quantidade, Justificativa, Parâmetros, Botão **Excluir**

---

## Regras de Negócio

### Salvar Movimentação (`POST /Movimentacao/SalvarMovimentacao`)
1. Lê `EmpresaId` e `UsuarioId` da sessão
2. **Validação de Saldo (Saída):** se `EntradaSaida == "S"` e `saldoAtual < Qtde` → aborta com `TempData["Erro"]`
3. Cria `MovAmostra` com:
   - `AmostraComplementar = "C"` para Entrada, `"M"` para Saída (igual ao Delphi)
4. Se parâmetros selecionados: cria um `MovAmostraParam` por parâmetro, vinculando ao `HistAmostraTeste` correspondente da amostra
5. Atualiza `HistAmostraSaldo`:
   - Entrada: `SaldoAtual += Qtde`
   - Saída: `SaldoAtual -= Qtde`
   - Se não existe saldo ainda: cria com o valor da movimentação

### Excluir Movimentação (`POST /Movimentacao/ExcluirMovimentacao`)
1. Busca a movimentação pelo ID
2. Calcula delta inverso: se era Entrada → `-Qtde`; se era Saída → `+Qtde`
3. Remove a movimentação e seus parâmetros vinculados
4. Atualiza `HistAmostraSaldo` com o delta inverso

### Validação de Saldo no Frontend
- JavaScript calcula em tempo real: se E/S = "S" e Qtde > saldo → exibe aviso e desabilita envio via `setCustomValidity`

### Campo `AMOSTRA_COMPLEMENTAR`
- Tipo: `VARCHAR(1)` no banco (não boolean)
- Valores: `"C"` = entrada/complementar, `"M"` = saída/movimentação, `"A"` = anulação
- Equivalente exato ao campo do sistema Delphi

---

## Acesso à Tela
- Via menu lateral: **Laboratório → Movimentação**
- Via botão **"Movimentar"** na tela de Detalhes da Amostra
- Via botão **"Movimentar"** na lista de resultados da busca

---

## Tabelas envolvidas
| Tabela | Uso |
|---|---|
| `LAB_HIST_AMOSTRAS` | Amostra que está sendo movimentada |
| `LAB_MOV_AMOSTRAS` | Registro de cada movimentação |
| `LAB_MOV_AMOSTRAS_PARAM` | Parâmetros vinculados à movimentação |
| `LAB_HIST_AMOSTRAS_SALDO` | Saldo atual da amostra (atualizado em cada mov.) |
| `LAB_HIST_AMOSTRAS_TESTES` | Parâmetros disponíveis da amostra |
| `LAB_PARAMETROS_ANALISES` | Descrição dos parâmetros |
