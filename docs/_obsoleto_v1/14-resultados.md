# Resultados de Análise

**Controller:** `ResultadosController`
**Rota base:** `/Resultados`
**Views:** `Views/Resultados/Index.cshtml`, `Criar.cshtml`, `Detalhes.cshtml`

---

## Telas

### Lista de Resultados (`GET /Resultados`)
- Tabela com colunas: Data, Amostra (código formatado), Cliente, Revisão, Parâmetros, Ações
- Ordenação: Data desc

### Lançar Resultado (`GET /Resultados/Criar?idAmostra={id}`)
- Exibe dados da amostra (read-only): Código, Cliente, Tipo de Análise
- **Grid de Parâmetros** para preenchimento — um linha por teste da amostra:
  - Tipo de Análise
  - Parâmetro
  - Tipo de Resultado (select: Numérico, Texto, etc.)
  - Valor do Resultado (texto livre)
  - Satisfeito (select: Sim/Não/N.A.)
  - Unidade de Medida (select)
  - Símbolo de Grandeza
- Botão **Salvar Resultado**

### Detalhes do Resultado (`GET /Resultados/Detalhes/{id}`)
- Cabeçalho: Amostra, Cliente, Data, Revisão
- Grid de parâmetros com valores lançados

---

## Regras de Negócio

### Lançar Resultado
1. A tela é acessada a partir dos **Detalhes da Amostra** (botão "Lançar Resultado")
2. A lista de parâmetros é carregada dos **Testes da Amostra** (`LAB_HIST_AMOSTRAS_TESTES`)
3. `DtResultado = DateTime.Now` (preenchida automaticamente)
4. **Revisão automática**: `MAX(Revisao WHERE IdHistAmostra) + 1` (começa em 0 se não há resultados anteriores)
5. Salva um `ResultadoAnalise` (cabeçalho) e múltiplos `ResultadoParam` (um por parâmetro preenchido)
6. Após salvar, redireciona para **Detalhes da Amostra**

### Campos por Parâmetro
| Campo | Descrição |
|---|---|
| `TipoResultado` | Tipo do valor ("N" = Numérico, padrão) |
| `VrResultado` | Valor obtido (texto) |
| `VrSatisfeito` | Resultado satisfatório (Sim/Não/N.A.) |
| `IdUnidade` | Unidade de medida |
| `SimboloGrandeza` | Símbolo (ex: %, mg/L) |
| `DtResultado` | Data/hora do resultado |

---

## Tabelas envolvidas
| Tabela | Uso |
|---|---|
| `LAB_RESULTADOS_ANALISES` | Cabeçalho do resultado (revisão, data) |
| `LAB_RESULTADOS_PARAMS` | Valores por parâmetro |
| `LAB_HIST_AMOSTRAS` | Amostra vinculada |
| `LAB_HIST_AMOSTRAS_TESTES` | Parâmetros disponíveis para lançamento |
| `UNIDADES` | Lookup de unidades de medida |
