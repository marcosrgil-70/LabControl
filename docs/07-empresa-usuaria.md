# Empresa Usuária

**Controller:** `EmpresaController`
**Rota base:** `/Empresa`
**Views:** `Views/Empresa/Editar.cshtml`, `_GridFones.cshtml`, `_GridEmails.cshtml`, `_GridEnderecos.cshtml`

---

## Telas

### Editar Empresa (`GET /Empresa/Editar`)
Formulário com 5 abas — sempre edita a empresa da sessão atual (`EmpresaId`):

| Aba | Campos |
|---|---|
| **Dados da Empresa** | Razão Social (NOME), CNPJ, Nome Fantasia, Inscrição Estadual, Inscrição Municipal |
| **Telefones** | Grid AJAX: DDD, Número, Tipo |
| **E-mails** | Grid AJAX: E-mail, Principal |
| **Endereços** | Grid AJAX: Tipo, Logradouro, Número, Complemento, Bairro, Cidade, UF, CEP |
| **Observações** | Textarea livre, salva via AJAX |

---

## Regras de Negócio

### Editar Empresa
1. O `EmpresaId` é lido da sessão — o usuário só pode editar a empresa na qual está logado
2. Busca `EMPRESAS` → join com `ENTIDADES` → join com `ENTIDADES_PJ`
3. Se a empresa não tiver `ENTIDADES_PJ`, cria o registro; caso contrário, atualiza
4. Campos editáveis: Razão Social (`ENTIDADES.NOME`), CNPJ, Nome Fantasia, Inscrição Estadual, Inscrição Municipal

### Telefones, E-mails, Endereços, Observações (AJAX)
- Mesmo padrão de `ClientesController` — endpoints AJAX que retornam partials atualizados
- Endpoints dedicados: `AdicionarFone`, `ExcluirFone`, `AdicionarEmail`, `ExcluirEmail`, `MarcarEmailPrincipal`, `AdicionarEndereco`, `ExcluirEndereco`, `SalvarObservacao`

---

## Tabelas envolvidas
| Tabela | Uso |
|---|---|
| `EMPRESAS` | Dados da empresa (ID, Código) |
| `ENTIDADES` | Razão Social |
| `ENTIDADES_PJ` | CNPJ, Nome Fantasia, Inscrições |
| `ENTIDADES_FONES` | Telefones |
| `ENTIDADES_EMAILS` | E-mails |
| `ENTIDADES_ENDERECOS` | Endereços |
| `ENTIDADES_OBSERVACOES` | Observações |
