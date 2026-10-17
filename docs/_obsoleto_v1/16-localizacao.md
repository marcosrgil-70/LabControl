# Localização de Amostras

**Controller:** `LocalAmostrasController`
**Rota base:** `/LocalAmostras`
**Views:** `Views/LocalAmostras/Index.cshtml`, `Editar.cshtml`

---

## Telas

### Busca de Amostras (`GET /LocalAmostras`)
Filtros de pesquisa:
- **Cliente** (select)
- **Produto** (select)
- **Código / Nr. Lote** (campo texto — busca por código numérico ou por lote)

Sem filtros → lista vazia por padrão.

Resultado (quando filtros aplicados):
- Tabela com colunas: Código, Cliente, Produto, Nr. Lote, Status Arquivo (badge), Local (Armário / Prat. / Caixa), Descarte (data), Ação "Localização"

**Badges de status:**
- `Não arquivada` (cinza) — sem registro em `LAB_LOCAL_AMOSTRAS`
- `Arquivada` (azul) — Status = 0
- `Descartada` (vermelho) — Status = 1

### Tela de Localização / Arquivo (`GET /LocalAmostras/Editar/{id}`)
Dividida em 2 colunas:

**Coluna Esquerda — Info da Amostra (read-only):**
- Código, Cliente, Produto, Nr. Lote, Tipo de Amostra, Status atual (badge)

**Coluna Direita — Arquivo Físico (condicional):**

**Modo Normal (amostra não descartada):**
| Campo | Tipo | Regra |
|---|---|---|
| **Data de Entrada no Arquivo** | DateTime | Padrão: agora se ainda não arquivada |
| **Nr. Armário** | Texto | Máx. 30 chars |
| **Prateleira** | Texto | Máx. 30 chars |
| **Nr. Caixa** | Texto | Máx. 30 chars |
| **Observação** | Texto | Máx. 200 chars |

Botões:
- **"Marcar como Descartada"** (vermelho, esquerda) — visível apenas se já tem localização salva
- **"Arquivar Amostra"** (azul, direita) — texto muda para "Salvar Localização" após 1º arquivamento

**Modo Descartada:**
- Alert vermelho com data e nome do funcionário que fez o descarte
- Botão **"Desfazer Descarte"** (amarelo)
- Sem formulário de edição

---

## Regras de Negócio

### Salvar Localização (`POST /LocalAmostras/SalvarLocalizacao`)
1. Busca registro existente em `LAB_LOCAL_AMOSTRAS` pelo `IdHistAmostra`
2. Se não existe: cria novo com `Status = 0` (Arquivada), `DtHrArquivo = agora` se não informado
3. Se já existe: atualiza todos os campos (Status, DtHrArquivo, NrArmario, NrPrateleira, NrCaixa, Observacao)
4. Exibe `TempData["Sucesso"]` e redireciona para a mesma tela

### Marcar como Descartada (`POST /LocalAmostras/Descartar`)
1. Busca o registro em `LAB_LOCAL_AMOSTRAS` — se não existe retorna `NotFound()`
2. Define: `Status = 1`, `DtHrDescarte = DateTime.Now`, `IdFuncionarioDescarte = UsuarioId` da sessão
3. O funcionário registrado é o usuário logado no momento do descarte

### Desfazer Descarte (`POST /LocalAmostras/DesfazerDescarte`)
1. Busca o registro em `LAB_LOCAL_AMOSTRAS`
2. Define: `Status = 0`, `DtHrDescarte = null`, `IdFuncionarioDescarte = null`
3. Exibe mensagem "Descarte desfeito. Amostra retornou ao status Arquivada."

### Status de Localização (campo `STATUS`)
| Valor | Descrição |
|---|---|
| `0` | Arquivada (localização física registrada) |
| `1` | Descartada (amostra foi descartada) |

### Visualização do Funcionário do Descarte
- Na tela de edição: busca `ENTIDADES_FUNCIONARIOS` → `ENTIDADES.NOME` pelo `IdFuncionarioDescarte`
- Exibido no alert vermelho da tela de descarte

---

## Acesso à Tela
- Via menu lateral: **Laboratório → Localização**
- Via botão **"Localização"** na tela de Detalhes da Amostra
- Via botão **"Localização"** na lista de resultados da busca de localização

---

## Tabelas envolvidas
| Tabela | Uso |
|---|---|
| `LAB_LOCAL_AMOSTRAS` | Registro de localização física / descarte |
| `LAB_HIST_AMOSTRAS` | Amostra |
| `ENTIDADES_FUNCIONARIOS` | Funcionário responsável pelo descarte |
| `ENTIDADES` | Nome do funcionário |
| `ENTIDADES` (via Clientes) | Filtro por cliente na busca |
| `PRODUTOS` | Filtro por produto na busca |

### Campos da tabela `LAB_LOCAL_AMOSTRAS`
| Coluna | Tipo | Descrição |
|---|---|---|
| `ID_LAB_LOCAL_AMOSTRAS` | INT PK | Chave primária |
| `ID_LAB_HIST_AMOSTRAS` | INT FK | Amostra vinculada (1:1) |
| `ID_EMPRESAS` | INT | Empresa |
| `STATUS` | INT | 0=Arquivada, 1=Descartada |
| `DT_HR_ARQUIVO` | DATETIME | Data/hora de arquivamento |
| `NR_ARMARIO` | VARCHAR(30) | Número do armário |
| `NR_PRATELEIRA` | VARCHAR(30) | Prateleira |
| `NR_CAIXA` | VARCHAR(30) | Caixa |
| `OBSERVACAO` | VARCHAR(200) | Observações |
| `DT_HR_DESCARTE` | DATETIME | Data/hora do descarte |
| `ID_FUNCIONARIO_DESCARTE` | INT FK | Funcionário que descartou |
