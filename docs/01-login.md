# Login / Autenticação

**Controller:** `LoginController`
**Rota base:** `/Login`
**Views:** `Views/Login/Index.cshtml`

---

## Telas

### Tela de Login (`GET /Login`)
- Campo **Usuário** (`name="login"`)
- Campo **Senha** (`name="senha"`)
- Select **Empresa** (`name="idEmpresa"`) — exibido apenas quando há mais de uma empresa cadastrada
- Botão **Entrar**

---

## Regras de Negócio

### Autenticação (`POST /Login`)
1. A senha digitada é convertida para hash SHA-256 (hex minúsculo) antes da consulta
2. Busca o usuário pelo código (convertido para maiúsculas) + hash da senha + `Inativo = false`
3. Se usuário não encontrado: exibe mensagem `"Login ou senha incorretos."`
4. **Lógica multi-empresa:**
   - Se há apenas 1 empresa cadastrada → seleciona automaticamente
   - Se há mais de 1 empresa e nenhuma foi selecionada → exibe `"Selecione uma empresa para continuar."`
   - Se a empresa informada não existe na lista → exibe `"Empresa inválida."`
5. Ao autenticar com sucesso, grava na sessão:
   - `UsuarioId` — ID do usuário
   - `UsuarioNome` — Nome do usuário
   - `UsuarioCodigo` — Código (login) do usuário
   - `UsuarioAdmin` — flag booleana
   - `EmpresaId` — ID da empresa selecionada
   - `EmpresaNome` — Nome da empresa
6. Redireciona para `Home/Index`

### Logout (`GET /Login/Sair`)
- Limpa toda a sessão (`Session.Clear()`)
- Redireciona para `Login/Index`

### Proteção de rotas
- O filtro global `SessaoFilter` intercepta todas as requisições
- Se `Session["UsuarioNome"]` for nulo, redireciona para `/Login`
- Exceção: o próprio controller `/Login` não é interceptado

---

## Tabelas envolvidas
| Tabela | Uso |
|---|---|
| `USUARIOS` | Autenticação (código + hash senha) |
| `EMPRESAS` | Lista de empresas para seleção |
| `ENTIDADES` | Join para obter o nome da empresa |
