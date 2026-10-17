# Propostas

**Controller:** `PropostasController`
**Rota base:** `/Propostas`
**Views:** `Views/Propostas/Index.cshtml`, `Criar.cshtml`, `Editar.cshtml`, `Detalhes.cshtml`

---

## Telas

### Lista de Propostas (`GET /Propostas`)
- Tabela com colunas: Código, Cliente, Data Solicitação, Status (badge colorido), Valor Total, Moeda, Qtde Itens, Ações
- Ordenação: Ano desc, Código desc
- Código formatado: `000001/26-R0` (CodProposta/AnoProposta-R{revisão})

### Criar Proposta (`GET /Propostas/Criar`)
Campos do formulário:

| Campo | Tipo | Regra |
|---|---|---|
| **Cliente** | Select | Obrigatório |
| **Status** | Select | Obrigatório, default: primeiro status (ID 1) |
| **Data de Solicitação** | Date | Padrão: hoje |
| **Data de Validade** | Date | Opcional |
| **Condição de Pagamento** | Select | Opcional |
| **Moeda** | Select | Opcional |
| **Funcionário Comercial** | Select | Opcional |
| **Tipo de Documento** | Texto | Padrão: "BOLETIM" |
| **Observação** | Textarea | Opcional |

### Detalhes / Editar Proposta (`GET /Propostas/Detalhes/{id}`)
Exibe todos os dados da proposta + grid de itens de análise com:
- Produto, Tipo de Análise, Método, Parâmetro, Prazo, Idioma, Tipo Documento
- Qtde, Valor Unitário, % Desconto, Subtotal, Desconto, Valor Total (calculados)
- Formulário para adicionar novo item
- Campo global de **% Desconto** para aplicar sobre o total

---

## Regras de Negócio

### Numeração Automática
1. `AnoProposta = DtSolicitacao.Year % 100` (2 últimos dígitos do ano)
2. `CodProposta = MAX(CodProposta WHERE IdEmpresa) + 1` (sequencial por empresa — equivalente ao `SP_GET_COD_PROPOSTAS` do Delphi)
3. `RevProposta = 0` na criação

### Cálculo de Itens
Para cada item adicionado (`POST /Propostas/AdicionarItem`):
```
VrSubtotal = VrUnitario × Qtde
VrDesconto = Round(VrSubtotal × PorcDesconto / 100, 2)
VrTotal = VrSubtotal − VrDesconto
```

### Recalcular Total da Proposta
Chamado automaticamente após adicionar ou remover item:
```
Subtotal = SUM(VrTotal dos itens)
VrDesconto = Round(Subtotal × PorcDesconto_da_proposta / 100, 2)
VrTotal = Subtotal − VrDesconto
```

### Aplicar Desconto Global (`POST /Propostas/AplicarDesconto`)
- Recebe `porcDesconto` para a proposta inteira
- Recalcula `VrDesconto` e `VrTotal` da proposta com base na soma dos itens
- Não altera os descontos individuais dos itens

### Remover Item (`POST /Propostas/RemoverItem`)
- Remove o item pelo ID
- Recalcula o total da proposta

---

## Tabelas envolvidas
| Tabela | Uso |
|---|---|
| `LAB_PROPOSTAS` | Cabeçalho da proposta |
| `LAB_PROPOSTAS_ANALISES` | Itens da proposta (análises) |
| `ENTIDADES` | Cliente |
| `PROPOSTAS_STATUS` | Status da proposta |
| `MOEDAS` | Moeda |
| `CONDICOES_PAGAMENTOS` | Condição de pagamento |
| `ENTIDADES_FUNCIONARIOS` + `ENTIDADES` | Funcionário comercial |
| `PRODUTOS` | Produto de cada item |
| `ANALISES_TIPO` | Tipo de análise |
| `ANALISES_METODOS` | Método de análise |
| `LAB_PARAMETROS_ANALISES` | Parâmetro de análise |
| `PRAZOS` | Prazo de entrega |
| `IDIOMAS` | Idioma do laudo |
