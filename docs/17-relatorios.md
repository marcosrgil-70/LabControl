# Relatórios

**Controller:** `RelatoriosController`
**Rota base:** `/Relatorios`
**Views:** `Views/Relatorios/Index.cshtml`, `Amostras.cshtml`, `Propostas.cshtml`

---

## Telas

### Página Inicial de Relatórios (`GET /Relatorios`)
- Menu de acesso rápido aos relatórios disponíveis

### Relatório de Amostras (`GET /Relatorios/Amostras`)
**Filtros:**
- **Data Início** — obrigatório para exibir resultados
- **Data Fim** — obrigatório para exibir resultados
- **Status** (select opcional)

**Colunas do relatório:**
- Código da Amostra, Cliente, Tipo de Amostra, Tipo de Análise, Produto, Nr. Lote, Data de Entrega, Status (badge colorido), Saldo

### Relatório de Propostas (`GET /Relatorios/Propostas`)
**Filtros:**
- **Data Início** — obrigatório para exibir resultados
- **Data Fim** — obrigatório para exibir resultados
- **Status** (select opcional)

**Colunas do relatório:**
- Código da Proposta, Cliente, Data de Solicitação, Data de Validade, Status (badge colorido), Valor Total, Moeda, Qtde de Itens

---

## Regras de Negócio

### Filtro por Período
- `DtFim` é expandida para incluir o dia inteiro: `dtFim.Date.AddDays(1).AddTicks(-1)`
- Sem filtro de data: nenhum dado é exibido

### Relatório de Amostras
- Filtra `LAB_HIST_AMOSTRAS` pelo campo `DT_ENTREGA` (data de recebimento)
- Filtro por status é opcional
- Ordena por `DtEntrega` desc

### Relatório de Propostas
- Filtra `LAB_PROPOSTAS` pelo campo `DT_SOLICITACAO`
- Filtro por status é opcional
- Ordena por `DtSolicitacao` desc

---

## Tabelas envolvidas
| Tabela | Uso |
|---|---|
| `LAB_HIST_AMOSTRAS` | Dados das amostras |
| `LAB_PROPOSTAS` | Dados das propostas |
| `AMOSTRAS_STATUS` | Status para filtro e colorização |
| `PROPOSTAS_STATUS` | Status para filtro e colorização |
| `ENTIDADES` | Cliente |
| `PRODUTOS` | Produto |
| `LAB_HIST_AMOSTRAS_SALDO` | Saldo da amostra |
| `LAB_PROPOSTAS_ANALISES` | Quantidade de itens da proposta |
| `MOEDAS` | Moeda da proposta |
