# Perfis de Acesso

**Controller:** `PerfisController`
**Rota base:** `/Perfis`
**Views:** `Views/Perfis/Index.cshtml`

---

## Telas

### Gerenciamento de Perfis (`GET /Perfis`)
Tela única com dois painéis gerenciados via AJAX:

**Painel Esquerdo — Lista de Perfis:**
- Tabela com colunas: Código, Descrição, Ações
- Botão **Novo Perfil** → abre modal inline para criação
- Botão **Editar** por linha → abre modal com dados preenchidos
- Botão **Excluir** por linha
- Clique em um perfil → carrega as permissões à direita

**Painel Direito — Permissões do Perfil:**
- Tabela com todas as permissões cadastradas
- Para cada permissão: checkboxes **Incluir**, **Alterar**, **Consultar**, **Excluir**, **Imprimir**
- Botão **Salvar Permissões** — envia via AJAX (JSON body)

---

## Regras de Negócio

### Criar / Editar Perfil (AJAX)
- `POST /Perfis/SalvarPerfil` (body: `id, codigo, descricao`)
- Código convertido para maiúsculas
- Código e Descrição são obrigatórios
- Código deve ser único (exceto o próprio na edição)
- Se `id == 0`: cria novo; se `id > 0`: atualiza existente
- Retorna `Json({ id, codigo, descricao })` para atualizar a grid sem reload

### Excluir Perfil (AJAX)
- `POST /Perfis/ExcluirPerfil` (body: `id`)
- Verifica se o perfil está vinculado a algum usuário (`USUARIOS_PERFIS`)
- Se em uso: retorna `BadRequest("Perfil está vinculado a um ou mais usuários.")`
- Se não em uso: remove e retorna `Ok()`

### Carregar Permissões do Perfil (AJAX)
- `GET /Perfis/PermissoesDoPerfil?idPerfil={id}`
- Retorna JSON com todas as permissões e as flags de cada uma para o perfil selecionado
- Permissões não vinculadas retornam com todos os booleans `false`

### Salvar Permissões (AJAX)
- `POST /Perfis/SalvarPermissoesPerfil` (JSON body: `[{ idPermissao, incluir, alterar, consultar, excluir, imprimir }]`)
- Remove todos os vínculos existentes do perfil
- Reinsere apenas as permissões que têm pelo menos um flag `true`
- Retorna `Ok()`

### 5 Flags de Permissão
Cada vínculo perfil-permissão tem 5 flags booleanos independentes:
- **Incluir** — pode criar novos registros
- **Alterar** — pode editar registros existentes
- **Consultar** — pode visualizar
- **Excluir** — pode remover
- **Imprimir** — pode imprimir/exportar

---

## Tabelas envolvidas
| Tabela | Uso |
|---|---|
| `PERFIS` | Definição dos perfis |
| `PERMISSOES` | Módulos/funcionalidades do sistema |
| `PERFIS_PERMISSOES` | Vínculo perfil ↔ permissão com 5 flags |
| `USUARIOS_PERFIS` | Usado apenas para verificar se perfil está em uso |
