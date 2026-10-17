# Módulos Implementados

## Status geral

| Módulo | Status | Observações |
|--------|--------|-------------|
| Login / Sessão | ✅ Completo | SHA256, sessão 8h, filtro global |
| Clientes | ✅ Completo | PF e PJ, abas AJAX |
| Funcionários | ✅ Completo | Mesmo padrão de Clientes + assinatura digital |
| Usuários | ✅ Completo | Permissões por módulo |
| Produtos | ✅ Completo | CRUD simples |
| Parâmetros | ✅ Completo | Filtro por tipo de análise |
| Tabelas Auxiliares | ✅ Completo | Tipos, status, prazos, unidades |
| Migração de Dados | ✅ Completo | Firebird → MySQL |
| Relatórios | ✅ Completo | Amostras e Propostas por período |
| Amostras (HistAmostras) | ✅ Completo | Registro, movimentações, saldo |
| Propostas | ✅ Completo | Itens, desconto %, revisões |
| Resultados | ✅ Completo | Por parâmetro, revisões |
| Laudos | ❌ Não iniciado | Link desabilitado no menu |

---

## Login

**Controller:** `LoginController`

| Action | Método | Descrição |
|--------|--------|-----------|
| `Index` | GET | Exibe formulário |
| `Index` | POST | Autentica e inicia sessão |
| `Sair` | GET | Encerra sessão e redireciona |

**Campos do formulário:** `name="login"` e `name="senha"`

**Regras:**
- Senha comparada como `SHA256(input).ToUpperHex()`
- Sessão armazena: `ID`, `Nome`, `Login`, `Admin`
- Usuários inativos (`USUATIVO = false`) são rejeitados

---

## Clientes

**Controller:** `ClientesController`

### Tela de listagem (Index)
- Grid com busca textual (Nome/Razão Social, CPF/CNPJ)
- Coluna 1: Razão Social / Nome (27%)
- Coluna 2: CPF/CNPJ (18%)
- Segunda linha da coluna 1: `NOME_FANTASIA` (PJ) ou `SOBRENOME` (PF)
- Botões: Novo, Editar

### Formulário (Criar / Editar)
- Campo **Categoria**: Pessoa Física (`F`) ou Jurídica (`J`) — define qual conjunto de campos aparece
- **Abas via AJAX:**
  - **Dados Gerais** — nome, documento, datas, observações gerais
  - **Telefones** — grid com DDD + número + tipo; adicionar/remover via AJAX
  - **E-mails** — grid com e-mail + flag principal; adicionar/remover/marcar principal via AJAX
  - **Endereços** — grid com tipo + logradouro completo; adicionar/remover via AJAX
  - **Observações** — campo texto livre salvo via AJAX

### Regra de nomenclatura
| Campo | PJ | PF |
|-------|----|----|
| Nome principal | `ENTIDADES_PJ.RAZAO_SOCIAL` | `ENTIDADES_PF.NOME` |
| Apelido/Fantasia | `ENTIDADES_PJ.NOME_FANTASIA` | `ENTIDADES_PF.SOBRENOME` |

> **Importante:** não existe mais o campo `FANTASIA` na tabela `ENTIDADES` — foi removido (commit `9a18990`).

### Autocomplete
`GET /Clientes/BuscarJson?termo=` — retorna JSON com id + nome para uso em outros módulos (ex: Propostas).

---

## Funcionários

**Controller:** `FuncionariosController`

Segue o mesmo padrão de Clientes (AJAX para fones, e-mails, endereços, observações), com as abas adicionais:

- **Assinatura** — upload de imagem PNG que fica salva como BLOB em `ENTIDADES_FUNC_ASSINATURAS`

### Actions específicos

| Action | Método | Descrição |
|--------|--------|-----------|
| `EnviarAssinatura` | POST | Upload PNG, salva BLOB + MD5 |
| `RemoverAssinatura` | POST | Remove registro da assinatura |
| `Assinatura` | GET | Retorna imagem para visualização (`image/png`) |

### Tabelas usadas
- `ENTIDADES` + `ENTIDADES_PF` (funcionários são sempre PF)
- `ENTIDADES_FUNCIONARIOS` (cargo, CREA/CRF, data admissão)
- `CARGO_FUNCIONARIOS` (9 cargos pré-cadastrados)
- `ENTIDADES_FUNC_ASSINATURAS` (BLOB da assinatura + MD5)
- Compartilhado: `ENTIDADES_FONES`, `ENTIDADES_EMAILS`, `ENTIDADES_ENDERECOS`, `ENTIDADES_OBSERVACOES`

---

## Usuários

**Controller:** `UsuariosController`

| Action | Método | Descrição |
|--------|--------|-----------|
| `Index` | GET | Lista todos os usuários |
| `Criar` | GET/POST | Novo usuário com permissões |
| `Editar` | GET/POST | Alterar senha e permissões |
| `AlternarStatus` | POST | Ativar / desativar usuário |

**Permissões por módulo:** `Incluir`, `Alterar`, `Consultar`, `Excluir`, `Imprimir`  
**Admins** (`ADMIN = true`) ignoram a tabela de permissões — têm acesso total.

---

## Produtos

**Controller:** `ProdutosController`

CRUD simples. Cada produto possui:
- Descrição
- Unidade de medida (`UNIDADES`)
- Tipo de embalagem (`EMBALAGEM_TIPOS`)

---

## Parâmetros de Análise

**Controller:** `ParametrosController`

- Filtro por **Tipo de Análise** na listagem
- `GET /Parametros/PorTipo?idAnaliseTipo=` — retorna JSON com parâmetros para um tipo (usado em Propostas/Resultados)

Cada parâmetro possui:
- Descrição + código
- Tipo de análise (`ANALISES_TIPOS`)
- Método de análise (`ANALISES_METODOS`)
- Unidade de medida
- Valor unitário (decimal 15,4)

---

## Tabelas Auxiliares

**Controller:** `TabelasAuxiliaresController`

Gerencia as tabelas de lookup do sistema via edição inline (sem página separada por registro):

| Tabela | Campos extras |
|--------|---------------|
| Tipos de Amostra | — |
| Tipos de Análise | — |
| Métodos de Análise | — |
| Status de Amostra | Cor (hex) para badge colorido |
| Prazos | Quantidade de dias |
| Unidades de Medida | Sigla |

---

## Amostras (HistAmostras)

**Controller:** `HistAmostrasController`

### Código de amostra
Formato: `TIPO(2) + SEQ(3) + ANÁLISE(2) + ANO(4)`  
Exemplo: `QU001FQ2026`

### Tela de listagem (Index)
- Grid com código, cliente, data entrada, status

### Criar
- Seleciona cliente (autocomplete)
- Define tipo de amostra, tipo de análise, prazo
- Ao salvar: gera código, registra movimentação de **entrada** e cria registro de **saldo**

### Detalhes
- Dados da amostra
- Testes/análises vinculados
- Histórico de movimentações
- Saldo atual

### Tabelas envolvidas
| Tabela | Descrição |
|--------|-----------|
| `HIST_AMOSTRAS` | Registro principal da amostra |
| `HIST_AMOSTRAS_TESTES` | Análises/testes da amostra |
| `HIST_AMOSTRAS_SALDOS` | Saldo atual (atualizado por trigger) |
| `MOV_AMOSTRAS` | Movimentações (entrada/saída/descarte) |
| `LOCAL_AMOSTRAS` | Localização física e data de descarte |

---

## Propostas

**Controller:** `PropostasController` (em `Controllers/Laboratorio/`)

### Código de proposta
Formato: `COD/ANO-RREV`  
Exemplo: `001/2026-R0`

### Tela de listagem (Index)
- Grid com código, cliente, data, valor total, status

### Criar
- Seleciona cliente
- Define condição de pagamento, moeda, idioma

### Detalhes (edição de itens)
- Adicionar itens: produto + tipo de análise + método + parâmetro + idioma + prazo + quantidade + valor unitário + desconto
- Remover itens
- `POST /Propostas/AplicarDesconto` — aplica desconto percentual geral, recalcula todos os itens

### Revisão
- Ao alterar proposta já enviada, cria nova revisão (R0 → R1 → R2...)

---

## Resultados

**Controller:** `ResultadosController` (em `Controllers/Laboratorio/`)

### Criar resultado
- Vinculado a uma amostra
- Lança valores por parâmetro
- Flag `satisfeito` / `insatisfeito` por parâmetro
- Suporta revisões (novo lançamento cria revisão)

### Detalhes
- Visualiza resultado com todos os parâmetros
- Histórico de revisões

### Tabelas envolvidas
| Tabela | Descrição |
|--------|-----------|
| `RESULTADOS_ANALISES` | Cabeçalho do resultado (+ nº revisão) |
| `RESULTADOS_PARAM` | Valores por parâmetro |

---

## Relatórios

**Controller:** `RelatoriosController`

| Relatório | Filtros | Saída |
|-----------|---------|-------|
| Amostras | Data início, data fim, status | Tabela + impressão |
| Propostas | Data início, data fim, status | Tabela + impressão |

---

## Migração de Dados (Firebird → MySQL)

**Controller:** `MigracaoController`

### Fluxo
1. **Testar conexão** — conecta ao Firebird e lista tabelas disponíveis
2. **Executar migração** — importa dados na ordem correta:
   - Tabelas auxiliares (tipos, status, idiomas, moedas, etc.)
   - Entidades (base)
   - PF / PJ
   - Funcionários
   - Fones, e-mails, endereços
   - Usuários (senha = SHA256 do login em minúsculas)

### Resultado da última migração
- 3.253+ linhas migradas
- 522 clientes
- 15 usuários
- Executada com transações e prepared statements para performance
