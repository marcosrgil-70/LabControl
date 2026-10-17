# Tabelas Auxiliares

**Controller:** `TabelasAuxiliaresController`
**Rota base:** `/TabelasAuxiliares`
**Views:** `Views/TabelasAuxiliares/Index.cshtml` + views especializadas por tipo

---

## Visão Geral

Centraliza 21 cadastros básicos (equivalente ao submenu "Cadastros Básicos" do Delphi). Cada cadastro tem uma view com lista + formulário inline (sem tela separada de criar/editar).

---

## Cadastros Disponíveis

### Seção: Laboratório

| Cadastro | Rota | View | Campos |
|---|---|---|---|
| Tipos de Amostra | `/TabelasAuxiliares/AmostrasTipos` | `Lista` | Descrição |
| Status de Amostra | `/TabelasAuxiliares/AmostrasStatus` | `ListaStatus` | Descrição, Cor (hex) |
| Tipos de Análise | `/TabelasAuxiliares/AnalisesTipos` | `Lista` | Descrição |
| Status de Análise | `/TabelasAuxiliares/AnalisesStatus` | `ListaStatus` | Descrição, Cor (hex) |
| Métodos de Análise | `/TabelasAuxiliares/AnalisesMetodos` | `Lista` | Descrição |
| Status de Boletim | `/TabelasAuxiliares/BoletinsStatus` | `ListaStatus` | Descrição, Cor (hex) |
| Status de Proposta | `/TabelasAuxiliares/PropostasStatus` | `ListaStatus` | Descrição, Cor (hex) |
| Idiomas | `/TabelasAuxiliares/Idiomas` | `Lista` | Descrição |
| Prazos | `/TabelasAuxiliares/Prazos` | `ListaPrazos` | Descrição, Qtde de Dias |
| Unidades de Medida | `/TabelasAuxiliares/Unidades` | `ListaUnidades` | Descrição, Sigla |
| Moedas | `/TabelasAuxiliares/Moedas` | `ListaMoedas` | Descrição, Sigla |
| Tipos de Embalagem | `/TabelasAuxiliares/EmbalagensTopos` | `Lista` | Descrição |
| Tipos de Resultado | `/TabelasAuxiliares/TiposResultados` | `Lista` | Descrição |
| Condições de Pagamento | `/TabelasAuxiliares/CondicoesPagamentos` | `ListaCondicaoPagamentos` | Código, Descrição |

### Seção: Pessoas / Endereços

| Cadastro | Rota | View | Campos |
|---|---|---|---|
| Tipos de Endereço | `/TabelasAuxiliares/EnderecosTipos` | `Lista` | Descrição |
| Registros Profissionais | `/TabelasAuxiliares/RegistrosProfissionais` | `ListaRegProfissional` | Descrição, Nomenclatura |
| Cargos de Funcionário | `/TabelasAuxiliares/CargosFuncionarios` | `Lista` | Descrição |
| Países | `/TabelasAuxiliares/Paises` | `ListaPaises` | Descrição, Sigla |
| Estados | `/TabelasAuxiliares/Estados` | `ListaEstados` | Descrição, Sigla |
| Cidades | `/TabelasAuxiliares/Cidades` | `Lista` | Descrição |
| Bairros | `/TabelasAuxiliares/Bairros` | `Lista` | Descrição |
| Tipos de Logradouro | `/TabelasAuxiliares/TiposLogradouros` | `Lista` | Descrição |
| Logradouros | `/TabelasAuxiliares/Logradouros` | `Lista` | Descrição |

---

## Regras de Negócio

### Padrão de Upsert
Todos os POSTs seguem o mesmo padrão:
1. Se `id == 0` → cria novo registro (`Add`)
2. Se `id > 0` → busca pelo ID e atualiza (`FindAsync` → update)
3. Exibe `TempData["Sucesso"]` e redireciona para o GET do mesmo cadastro

### Views por tipo

- **`Lista`** — colunas: ID + Descrição. Usado para cadastros simples.
- **`ListaStatus`** — colunas: Descrição + cor (exibe swatch colorido). Usado para cadastros com status colorido.
- **`ListaPrazos`** — colunas: Descrição + Qtde de Dias.
- **`ListaUnidades`** — colunas: Descrição + Sigla.
- **`ListaMoedas`** — colunas: Descrição + Sigla.
- **`ListaCondicaoPagamentos`** — colunas: Código + Descrição.
- **`ListaRegProfissional`** — colunas: Descrição + Nomenclatura.
- **`ListaPaises`** / **`ListaEstados`** — colunas: Descrição + Sigla.

### Views genéricas
As views simples (`Lista`) recebem um record `ListaAuxiliar(string Titulo, string Entidade, List<object> Itens)` como model, permitindo reutilização do mesmo template para vários cadastros.

---

## Tabelas envolvidas
`AMOSTRAS_STATUS`, `AMOSTRAS_TIPO`, `ANALISES_STATUS`, `ANALISES_TIPO`, `ANALISES_METODOS`, `BOLETINS_STATUS`, `PROPOSTAS_STATUS`, `IDIOMAS`, `PRAZOS`, `UNIDADES`, `MOEDAS`, `EMBALAGENS_TIPOS`, `TIPOS_RESULTADOS`, `CONDICOES_PAGAMENTOS`, `ENDERECOS_TIPOS`, `TIPOS_REG_PROFISSIONAL`, `CARGO_FUNCIONARIOS`, `PAISES`, `ESTADOS`, `CIDADES`, `BAIRROS`, `TIPOS_LOGRADOUROS`, `LOGRADOUROS`
