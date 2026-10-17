# Usuários

**Controller:** `UsuariosController`
**Rota base:** `/Usuarios`
**Views:** `Views/Usuarios/Index.cshtml`, `Criar.cshtml`, `Editar.cshtml`

---

## Telas

### Lista de Usuários (`GET /Usuarios`)
- Tabela com colunas: Código, Nome, Admin (badge), Perfis vinculados, Status (Ativo/Inativo), Ações
- Botão **Novo Usuário**
- Botão **Ativar/Desativar** por linha

### Criar Usuário (`GET /Usuarios/Criar`)
- **Código** (login) — obrigatório, convertido para maiúsculas
- **Nome** — obrigatório
- **Senha** — obrigatório para novo usuário
- **Confirmar Senha** — deve coincidir com Senha
- **É Administrador** — checkbox
- **Perfis** — lista de checkboxes com todos os perfis disponíveis

### Editar Usuário (`GET /Usuarios/Editar/{id}`)
- Mesmos campos do Criar
- Senha é opcional na edição (se não preenchida, mantém a atual)
- Perfis mostrados com marcação dos já vinculados

---

## Regras de Negócio

### Criar Usuário
1. **Código** é convertido para maiúsculas (`.ToUpper()`)
2. Validações obrigatórias: Código, Nome, Senha, Confirmação de Senha
3. Código deve ser único — verifica duplicata antes de salvar
4. Senha e confirmação devem coincidir
5. Senha é armazenada como hash SHA-256 (hex minúsculo) — nunca em texto puro
6. Perfis selecionados são inseridos em `USUARIOS_PERFIS`

### Editar Usuário
1. Código duplicado verificado excluindo o próprio usuário (`u.Id != id`)
2. Se `NovaSenha` não informada: mantém a senha atual (não atualiza `SenhaHash`)
3. Se `NovaSenha` informada: valida confirmação e recalcula o hash
4. Perfis: remove todos os vínculos existentes e reinsere os selecionados

### Ativar / Desativar (`POST /Usuarios/AlternarStatus/{id}`)
- Inverte o flag `Inativo` do usuário
- Exibe mensagem indicando se foi ativado ou desativado

---

## Tabelas envolvidas
| Tabela | Uso |
|---|---|
| `USUARIOS` | Dados do usuário (código, nome, hash senha, admin, inativo) |
| `USUARIOS_PERFIS` | Vínculo N:N entre usuários e perfis |
| `PERFIS` | Perfis disponíveis para seleção |
