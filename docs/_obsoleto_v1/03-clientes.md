# Clientes

**Controller:** `ClientesController`
**Rota base:** `/Clientes`
**Views:** `Views/Clientes/Index.cshtml`, `Criar.cshtml`, `Editar.cshtml`, `_GridFones.cshtml`, `_GridEmails.cshtml`, `_GridEnderecos.cshtml`

---

## Telas

### Lista de Clientes (`GET /Clientes`)
- Campo de busca por nome (filtra `ENTIDADES.NOME`)
- Tabela com colunas: Razão Social / Nome Fantasia, CPF/CNPJ, Categoria (PF/PJ), Ações
- Segunda linha da coluna nome exibe `NOME_FANTASIA` (PJ) ou `SOBRENOME` (PF)
- Botão **Novo Cliente**

### Criar Cliente (`GET /Clientes/Criar`)
- Campos: **Nome/Razão Social** (obrigatório), **Categoria** (PF ou PJ, radio)
- Se PF: CPF, Sobrenome, RG, Data de Nascimento, Sexo
- Se PJ: CNPJ, Nome Fantasia, Inscrição Estadual
- Campo rápido: **DDD** + **Telefone** (adiciona o primeiro fone ao salvar)
- Botão **Salvar**

### Editar Cliente (`GET /Clientes/Editar/{id}`)
Formulário com 5 abas gerenciadas via AJAX:

| Aba | Campos |
|---|---|
| **Dados Gerais** | Nome/Razão Social, Inativo; PF: CPF, Sobrenome, RG, Nasc., Sexo; PJ: CNPJ, Nome Fantasia, Insc. Estadual |
| **Telefones** | Grid com DDD, Número, Tipo; Adicionar / Excluir via AJAX |
| **E-mails** | Grid com E-mail, Principal (flag); Adicionar / Marcar Principal / Excluir via AJAX |
| **Endereços** | Grid com Tipo, Logradouro, Número, Complemento, Bairro, Cidade, UF, CEP; Adicionar / Excluir |
| **Observações** | Textarea livre, salvo via AJAX |

---

## Regras de Negócio

### Criar Cliente
1. `TipoCliente = true` é setado automaticamente
2. `DataCadastro = DateTime.Now` é preenchida no momento do cadastro
3. Se Categoria = "F": cria registro em `ENTIDADES_PF`
4. Se Categoria = "J": cria registro em `ENTIDADES_PJ`
5. Se telefone informado na criação: insere diretamente em `ENTIDADES_FONES`
6. Após criação bem-sucedida, redireciona para a tela **Editar** do cliente criado

### Editar Cliente
- Apenas Nome e Inativo são editáveis na aba Dados Gerais (via POST)
- Campos PF/PJ são atualizados no mesmo POST
- **Não existe campo `FANTASIA` em `ENTIDADES`** — o apelido vem sempre de `ENTIDADES_PJ.NOME_FANTASIA` (PJ) ou `ENTIDADES_PF.SOBRENOME` (PF)

### Telefones (AJAX)
- `POST /Clientes/AdicionarFone` — valida que número não seja vazio
- `POST /Clientes/ExcluirFone` — remove pelo ID
- Retorna partial `_GridFones` atualizado

### E-mails (AJAX)
- `POST /Clientes/AdicionarEmail` — se `principal = true`, desmarca todos os outros antes
- `POST /Clientes/MarcarEmailPrincipal` — desmarca todos, marca só o selecionado
- `POST /Clientes/ExcluirEmail` — remove pelo ID
- Retorna partial `_GridEmails` atualizado

### Endereços (AJAX)
- `POST /Clientes/AdicionarEndereco` — UF convertida para maiúsculas
- `POST /Clientes/ExcluirEndereco` — remove pelo ID
- Retorna partial `_GridEnderecos` atualizado

### Observações (AJAX)
- `POST /Clientes/SalvarObservacao` — upsert: cria se não existe, atualiza se já existe
- Retorna `Json({ ok: true })`

### AJAX Busca (usado em outras telas)
- `GET /Clientes/BuscarJson?termo=` — retorna até 10 clientes como `[{ id, nome, categoria }]`

---

## Tabelas envolvidas
| Tabela | Uso |
|---|---|
| `ENTIDADES` | Registro base do cliente |
| `ENTIDADES_PF` | Dados de pessoa física (CPF, Sobrenome, etc.) |
| `ENTIDADES_PJ` | Dados de pessoa jurídica (CNPJ, Nome Fantasia, etc.) |
| `ENTIDADES_FONES` | Telefones do cliente |
| `ENTIDADES_EMAILS` | E-mails do cliente |
| `ENTIDADES_ENDERECOS` | Endereços do cliente |
| `ENTIDADES_OBSERVACOES` | Observações livres |
| `FONES_TIPOS` | Tipos de telefone (lookup) |
| `ENDERECOS_TIPOS` | Tipos de endereço (lookup) |
