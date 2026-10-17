# Funcionários

**Controller:** `FuncionariosController`
**Rota base:** `/Funcionarios`
**Views:** `Views/Funcionarios/Index.cshtml`, `Criar.cshtml`, `Editar.cshtml`

---

## Telas

### Lista de Funcionários (`GET /Funcionarios`)
- Campo de busca por nome
- Tabela com colunas: Nome, CPF, Cargo, Ações
- Botão **Novo Funcionário**

### Criar Funcionário (`GET /Funcionarios/Criar`)
- Campos: **Nome** (obrigatório), **CPF**, **Sobrenome**, **RG**, **Data de Nascimento**, **Sexo**
- Campos profissionais: **Cargo** (select), **Tipo de Registro Profissional** (select), **Nr. Registro Profissional**
- Campo rápido: DDD + Telefone

### Editar Funcionário (`GET /Funcionarios/Editar/{id}`)
Formulário com 6 abas:

| Aba | Campos |
|---|---|
| **Dados Gerais** | Nome, Inativo, CPF, Sobrenome, RG, Nasc., Sexo, Cargo, Tipo Reg. Profissional, Nr. Registro |
| **Telefones** | Grid AJAX (igual a Clientes) |
| **E-mails** | Grid AJAX (igual a Clientes) |
| **Endereços** | Grid AJAX (igual a Clientes) |
| **Observações** | Textarea AJAX |
| **Assinatura** | Upload de imagem da assinatura digital; exibe preview se já cadastrada |

---

## Regras de Negócio

### Criar Funcionário
1. `Categoria` é fixada em `"F"` (pessoa física)
2. `TipoFuncionario = true` é setado automaticamente
3. `DataCadastro = DateTime.Now`
4. Cria registros em: `ENTIDADES`, `ENTIDADES_PF`, `ENTIDADES_FUNCIONARIOS`
5. Se telefone informado: insere em `ENTIDADES_FONES`

### Assinatura Digital
- Upload via `POST /Funcionarios/EnviarAssinatura` (multipart/form-data)
- Bytes da imagem são armazenados como BLOB em `ENTIDADES_FUNC_ASSINATURAS`
- Hash MD5 dos bytes é calculado e armazenado junto para verificação de integridade
- Remoção via `POST /Funcionarios/RemoverAssinatura`
- Visualização via `GET /Funcionarios/Assinatura/{id}` — retorna a imagem como `image/png`

### Abas AJAX (Telefones, E-mails, Endereços, Observações)
- Reutilizam os mesmos endpoints de `ClientesController` (`AdicionarFone`, `ExcluirFone`, etc.)
- Os endpoints operam genericamente por `idEntidade`, independente de ser cliente ou funcionário

---

## Tabelas envolvidas
| Tabela | Uso |
|---|---|
| `ENTIDADES` | Registro base do funcionário |
| `ENTIDADES_PF` | Dados pessoais (CPF, Sobrenome, etc.) |
| `ENTIDADES_FUNCIONARIOS` | Dados profissionais (cargo, registro) |
| `ENTIDADES_FUNC_ASSINATURAS` | Assinatura digital (BLOB + MD5) |
| `CARGO_FUNCIONARIOS` | Cargos disponíveis (lookup) |
| `TIPOS_REG_PROFISSIONAL` | Tipos de registro profissional (lookup) |
| `ENTIDADES_FONES` | Telefones |
| `ENTIDADES_EMAILS` | E-mails |
| `ENTIDADES_ENDERECOS` | Endereços |
| `ENTIDADES_OBSERVACOES` | Observações |
