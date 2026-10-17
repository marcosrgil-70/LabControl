# Amostras (HistAmostras)

**Controller:** `HistAmostrasController`
**Rota base:** `/HistAmostras`
**Views:** `Views/HistAmostras/Index.cshtml`, `Criar.cshtml`, `Editar.cshtml`, `Detalhes.cshtml`

---

## Telas

### Lista de Amostras (`GET /HistAmostras`)
- Tabela com colunas: Código, Cliente, Produto, Tipo Amostra, Tipo Análise, Status (badge colorido), Ações
- Código formatado: `TT-SSSSS-AA/YY` — ex: `01-00001-02/26`
  - `TT` = ID do tipo de amostra (2 dígitos)
  - `SSSSS` = código sequencial (5 dígitos)
  - `AA` = ID do tipo de análise (2 dígitos)
  - `YY` = ano (2 últimos dígitos)

### Criar Amostra (`GET /HistAmostras/Criar`)
Campos do formulário:

| Campo | Tipo | Regra |
|---|---|---|
| **Cliente** | Select | Obrigatório (apenas clientes com propostas) |
| **Proposta** | Select dinâmico | Obrigatório, carregado via AJAX após selecionar cliente |
| **Produto** | Select dinâmico | Obrigatório, carregado via AJAX após selecionar proposta |
| **Tipo de Análise** | Select dinâmico | Obrigatório, carregado via AJAX após selecionar proposta+produto |
| **Tipo de Amostra** | Select | Obrigatório |
| **Status da Amostra** | Select | Obrigatório |
| **Data de Entrega** | Date | Obrigatório, não pode ser futura |
| **Hora de Entrega** | Hora | Obrigatório |
| **Funcionário Responsável** | Select | Obrigatório |
| **Tipo de Embalagem** | Select | Obrigatório |
| **Qtde de Embalagens** | Número | Obrigatório |
| **Nr. Lote** | Texto | Opcional |
| **Fabricação** | Dia/Mês/Ano separados | Opcional |
| **Validade** | Dia/Mês/Ano separados | Opcional |
| **Espécie** | Texto | Opcional |
| **Aspecto** | Texto | Opcional |
| **Temperatura** | Decimal | Opcional |
| **Local de Recebimento** | Texto | Opcional |
| **Pedido de Venda** | Texto | Opcional |
| **Nome do Contato** | Texto | Opcional |
| **Nr. Lote** | Texto | Opcional |
| **Nota / Rótulo** | Texto | Opcional |
| Docs. Acompanhantes | Checkboxes | Ficha Técnica, Padrão Analítico, CA Cliente |
| **Enviar a outro lab.** | Checkbox + Qtde | Opcional |
| **Observação** | Textarea | Opcional |
| **Tipo de Documento** | Select | Padrão: "BOLETIM" |

### Detalhes da Amostra (`GET /HistAmostras/Detalhes/{id}`)
Tela read-only com:
- Identificação: código, tipo amostra, tipo análise, ano, cliente, contato, proposta
- Recebimento: data/hora, local, embalagem, qtde
- Produto/Amostra: produto, lote, fabricação, validade, espécie/aspecto, temperatura
- Documentos acompanhantes: lista com checkmarks
- **Grid de Testes/Ensaios** (da proposta vinculada)
- **Grid de Movimentações** (entradas e saídas com saldo)
- Botões de ação: **Lançar Resultado**, **Movimentar**, **Localização**

---

## Regras de Negócio

### Numeração Automática
1. `AnoAmostra = DtEntrega.Year % 100` (equivalente ao Delphi)
2. `CodAmostra = MAX(CodAmostra WHERE IdEmpresa) + 1` (equivalente ao `SP_GET_COD_AMOSTRA` do Delphi)

### Entrada Inicial de Amostra
Ao criar uma amostra, são gerados automaticamente:
1. **MovAmostra** de entrada: `EntradaSaida = "E"`, `Qtde = QtdeEmbalagensEntregue ?? 1`, `Justificativa = "Entrada inicial de amostra"`
2. **HistAmostraSaldo**: `SaldoAtual = Qtde da entrada inicial`

### Cascata de Selects (AJAX)
- Ao selecionar **Cliente** → carrega Propostas daquele cliente (`/HistAmostras/PropostasPorCliente`)
- Ao selecionar **Proposta** → carrega Produtos dos itens da proposta (`/HistAmostras/ProdutosPorProposta`)
- Ao selecionar **Proposta + Produto** → carrega Tipos de Análise correspondentes (`/HistAmostras/AnaliseTiposPorProposta`)

### Validações
- Data de Entrega não pode ser futura
- Cliente, Proposta, Produto, Tipo Amostra, Tipo Análise, Status, Embalagem, Funcionário: todos obrigatórios

### Fabricação e Validade
- Armazenados em 6 campos separados: `FABRICACAO_DIA`, `FABRICACAO_MES`, `FABRICACAO_ANO`, `VALIDADE_DIA`, `VALIDADE_MES`, `VALIDADE_ANO`
- Exibição: `dia?/mês?/ano?` com "?" para campos não preenchidos

---

## Tabelas envolvidas
| Tabela | Uso |
|---|---|
| `LAB_HIST_AMOSTRAS` | Registro da amostra |
| `LAB_HIST_AMOSTRAS_TESTES` | Testes vinculados (da proposta) |
| `LAB_HIST_AMOSTRAS_SALDO` | Saldo atual da amostra |
| `LAB_MOV_AMOSTRAS` | Movimentações (entrada inicial + posteriores) |
| `AMOSTRAS_TIPO` | Tipo de amostra |
| `ANALISES_TIPO` | Tipo de análise |
| `AMOSTRAS_STATUS` | Status da amostra |
| `ENTIDADES` | Cliente |
| `PRODUTOS` | Produto |
| `EMBALAGENS_TIPOS` | Tipo de embalagem |
| `LAB_PROPOSTAS` | Proposta vinculada |
| `ENTIDADES_FUNCIONARIOS` | Funcionário responsável e digitador |
