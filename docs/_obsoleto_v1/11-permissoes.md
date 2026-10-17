# Permissões

**Controller:** `PermissoesController`
**Rota base:** `/Permissoes`
**Views:** `Views/Permissoes/Index.cshtml`

---

## Telas

### Gerenciamento de Permissões (`GET /Permissoes`)
- Lista de permissões com colunas: Código, Descrição, Ações
- Formulário inline para criar nova permissão
- Botão **Editar** por linha → atualiza via AJAX
- Botão **Excluir** por linha

---

## Regras de Negócio

### Criar / Editar Permissão (AJAX)
- `POST /Permissoes/Salvar` (form: `id, codigo, descricao`)
- Código convertido para maiúsculas
- Código e Descrição são obrigatórios
- Código deve ser único — se duplicado: `BadRequest("Já existe uma permissão com este código.")`
- Se `id == 0`: cria novo; se `id > 0`: atualiza existente
- Retorna `Ok()`

### Excluir Permissão (AJAX)
- `POST /Permissoes/Excluir` (form: `id`)
- Verifica se está vinculada a algum perfil (`PERFIS_PERMISSOES`)
- Se em uso: `BadRequest("Permissão está vinculada a um ou mais perfis.")`
- Se não em uso: remove e retorna `Ok()`

---

## Permissões pré-cadastradas (seed)
Inseridas automaticamente pelo `Program.cs` ao iniciar:
- `Movimentacao` — Movimentação de Amostras
- `LocalAmostras` — Localização de Amostras

---

## Tabelas envolvidas
| Tabela | Uso |
|---|---|
| `PERMISSOES` | Registro das permissões do sistema |
| `PERFIS_PERMISSOES` | Verificado ao excluir (integridade referencial) |
