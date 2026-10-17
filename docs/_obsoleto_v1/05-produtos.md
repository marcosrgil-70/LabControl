# Produtos

**Controller:** `ProdutosController`
**Rota base:** `/Produtos`
**Views:** `Views/Produtos/Index.cshtml`, `Criar.cshtml`, `Editar.cshtml`, `_Form.cshtml`

---

## Telas

### Lista de Produtos (`GET /Produtos`)
- Campo de busca por descrição ou código
- Checkbox **Mostrar Inativos** (padrão: não exibe inativos)
- Tabela com colunas: Código, Descrição, Unidade, Embalagem, Qtde Embalagem, Inativo, Ações

### Criar / Editar Produto
Campos do formulário (compartilhados via `_Form.cshtml`):

| Campo | Tipo | Regra |
|---|---|---|
| **Código** | Texto | Obrigatório, único por empresa |
| **Descrição** | Texto | Obrigatório |
| **Unidade** | Select | Lookup de `UNIDADES` |
| **Tipo de Embalagem** | Select | Lookup de `EMBALAGENS_TIPOS` |
| **Qtde por Embalagem** | Número decimal | Opcional |
| **Inativo** | Checkbox | Padrão: não |

---

## Regras de Negócio

### Criar Produto
1. Valida unicidade do **Código** — se já existe outro produto com o mesmo código, exibe erro
2. Salva em `PRODUTOS` e redireciona para a lista

### Editar Produto
1. Verifica unicidade do **Código** excluindo o próprio registro (`p.Id != id`)
2. Atualiza: Codigo, Descricao, IdUnidade, IdEmbalagemTipo, QtdeEmbalagem, Inativo

### Excluir Produto
- `POST /Produtos/Excluir/{id}` — remove diretamente sem verificação de uso
- Exibe mensagem de sucesso e retorna à lista

### Filtro de Inativos
- Por padrão (`mostrarInativos = false`), apenas produtos ativos são exibidos
- O checkbox na lista altera o comportamento via query string `?mostrarInativos=true`

---

## Tabelas envolvidas
| Tabela | Uso |
|---|---|
| `PRODUTOS` | Registro do produto |
| `UNIDADES` | Lookup de unidades de medida |
| `EMBALAGENS_TIPOS` | Lookup de tipos de embalagem |
