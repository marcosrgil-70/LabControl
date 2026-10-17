# Parâmetros de Análise

**Controller:** `ParametrosController`
**Rota base:** `/Parametros`
**Views:** `Views/Parametros/Index.cshtml`, `Criar.cshtml`, `Editar.cshtml`, `_Form.cshtml`

---

## Telas

### Lista de Parâmetros (`GET /Parametros`)
- Campo de busca por descrição ou descrição reduzida
- Filtro por **Tipo de Análise** (select)
- Checkbox **Somente Ativos** (padrão: sim)
- Tabela com colunas: Descrição, Desc. Reduzida, Tipo de Análise, Método, Valor Unitário, Auditado, Ações

### Criar / Editar Parâmetro
Campos do formulário (compartilhados via `_Form.cshtml`):

| Campo | Tipo | Regra |
|---|---|---|
| **Descrição** | Texto | Obrigatório |
| **Descrição Reduzida** | Texto | Opcional (DESC_REDUZIDA) |
| **Tipo de Análise** | Select | Lookup de `ANALISES_TIPO` |
| **Método de Análise** | Select | Lookup de `ANALISES_METODOS` |
| **Valor Unitário** | Decimal | Usado no cálculo de proposta |
| **Auditado** | Checkbox | Flag AUDITADO (campo do Delphi) |

---

## Regras de Negócio

### Criar / Editar Parâmetro
- Não há validação de duplicidade (múltiplos parâmetros com o mesmo nome são permitidos)
- Ao editar, atualiza: Descricao, DescReduzida, IdAnaliseTipo, IdAnaliseMetodo, VrUnitario, Auditado

### Excluir Parâmetro
- `POST /Parametros/Excluir/{id}` — remove diretamente sem verificação de uso

### AJAX — Parâmetros por Tipo
- `GET /Parametros/PorTipo?idAnaliseTipo={id}` — retorna `[{ id, descricao, vrUnitario }]`
- Utilizado no formulário de **Proposta** para carregar parâmetros ao selecionar o tipo de análise

---

## Tabelas envolvidas
| Tabela | Uso |
|---|---|
| `LAB_PARAMETROS_ANALISES` | Registro do parâmetro |
| `ANALISES_TIPO` | Lookup de tipos de análise |
| `ANALISES_METODOS` | Lookup de métodos de análise |
