# RN-02 — Segurança, Permissões e Auditoria

Legenda: Camada **B** banco · **S** serviço · **T** tela | Origem **L** legado · **C** correção · **N** nova · **D** decisão. [Catálogo](README.md)

| ID | Regra | Camada | Origem | Crit. | Onde |
|---|---|---|---|---|---|
| RN-02.01 | Login próprio (o legado dependia do lançador `MAIS_Acesso`); login único global, em maiúsculas | S | N | 🔴 | [T00](../telas/T00-login-empresa.md) |
| RN-02.02 | Senha com PBKDF2 (`PasswordHasher`); nunca SHA-256 sem salt | S | C | 🔴 | [P02](../processos/P02-autenticacao-permissoes.md) |
| RN-02.03 | Mensagem única "Usuário ou senha inválidos"; 5 falhas bloqueiam por 15 minutos | S | N | 🔴 | [P02](../processos/P02-autenticacao-permissoes.md) |
| RN-02.04 | Troca de senha obrigatória no 1º acesso e após reset (mín. 8, letra + número, diferente da atual) | S T | N | 🟠 | [T00](../telas/T00-login-empresa.md) |
| RN-02.05 | Usuários migrados recebem senha provisória e troca obrigatória (hash do legado não recuperável) | S | N | 🟠 | [M-09](../processos/migracao/M-09-usuarios.md) |
| RN-02.06 | Permissão = `TELA.ACAO`; efetivas = união dos perfis ativos do usuário **na empresa**; ADMIN_GLOBAL ignora | S | C | 🔴 | [T04](../telas/T04-perfis-permissoes.md) |
| RN-02.07 | Toda action protegida por `[Permissao]` (403 sem permissão); menu e botões ocultos sem permissão | S T | L | 🔴 | [T01](../telas/T01-layout-menu.md) |
| RN-02.08 | Perfil ADMINISTRADOR da empresa não pode ser excluído | S | N | ⚪ | [T04](../telas/T04-perfis-permissoes.md) |
| RN-02.09 | Um profissional ↔ no máximo um usuário por empresa | B | N | 🟠 | [`USUARIOS_EMPRESAS`](../banco/objetos/tabelas/USUARIOS_EMPRESAS.md) |
| RN-02.10 | O profissional do usuário é o digitador/gerador gravado nos registros; sem vínculo não recebe amostra, não lança resultado, não gera/emite boletim, não movimenta nem descarta | S T | L | 🔴 | [P02](../processos/P02-autenticacao-permissoes.md) |
| RN-02.11 | Só ADMIN_GLOBAL altera `ADMIN_GLOBAL`, cria empresas, migra dados e ajusta numeração | S | N | 🔴 | [T02](../telas/T02-usuarios.md), [T07](../telas/T07-migracao.md) |
| RN-02.12 | Usuário com registros não é excluído: é inativado | S | N | ⚪ | [T02](../telas/T02-usuarios.md) |
| RN-02.13 | Auditoria (antes/depois) obrigatória em tabelas críticas e eventos de login, revisão, emissão, numeração e senha | S | N | 🟠 | [P21](../processos/P21-auditoria-log.md) |
| RN-02.14 | Log da amostra gravado pelo banco com o usuário da sessão | B | L | 🟠 | [`LAB_HIST_AMOSTRAS_LOG`](../banco/objetos/tabelas/LAB_HIST_AMOSTRAS_LOG.md) |
| RN-02.15 | Data de entrega retroativa além do limite exige permissão `AMOSTRAS.ENTREGA_RETROATIVA` + confirmação | B S T | N/D | 🔴 | [P04](../processos/P04-numero-amostra-ano.md) |
| RN-02.16 | Cookie HttpOnly/Secure/SameSite, expiração de 8 h, antiforgery em todo POST | S | N | 🟠 | [P02](../processos/P02-autenticacao-permissoes.md) |
