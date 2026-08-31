# Convenções e Padrões de Desenvolvimento

## Padrão de Controller

Todos os módulos seguem a mesma estrutura:

```
Index(busca)        → lista com busca textual
Criar() GET         → formulário em branco
Criar() POST        → valida e salva, redireciona para Index
Editar(id) GET      → carrega entidade existente
Editar(id) POST     → valida e salva, redireciona para Index
```

Ações destrutivas (excluir) usam `POST` — nunca `DELETE` via link.

---

## Padrão de View (Razor)

### Listagem (Index)
- Formulário GET com campo `busca` para filtro
- Tabela Bootstrap com cabeçalho fixo
- Botão "Novo" no cabeçalho
- Botão "Editar" em cada linha

### Formulário (Criar / Editar)
- `asp-for` em todos os campos
- `asp-validation-for` abaixo de cada campo
- `div.mb-3` como wrapper de cada campo
- Botão "Salvar" + link "Cancelar" (volta para Index)

### Mensagens de feedback
Via `TempData["Sucesso"]` ou `TempData["Erro"]` — exibidas automaticamente pelo `_Layout.cshtml`.

---

## Padrão de Módulo com Abas (Clientes / Funcionários)

As abas são divs com `id` específicos. O conteúdo de cada aba de dados relacionados (fones, e-mails, endereços) é um partial renderizado via AJAX:

```js
// Exemplo: após adicionar telefone
fetch('/Clientes/AdicionarFone', { method: 'POST', body: formData })
  .then(r => r.text())
  .then(html => document.getElementById('grid-fones').innerHTML = html);
```

Cada endpoint de ação AJAX retorna `PartialView("_GridFones", model)`.

---

## Nomenclatura de Banco de Dados

- Tabelas: `PLURAL_MAIUSCULO` (ex: `ENTIDADES_FONES`)
- Colunas: `MAIUSCULO` (ex: `ID_ENTIDADE`, `DT_CADASTRO`)
- PKs: sempre `ID int AUTO_INCREMENT`
- FKs: `ID_<TABELA>` (ex: `ID_ENTIDADE`, `ID_AMOSTRA_TIPO`)
- Datas: prefixo `DT_` (ex: `DT_NASC`, `DT_ENTRADA`)
- Booleans: `ATIVO`, `ADMIN`, `PRINCIPAL`, `SATISFEITO`

---

## Regras de Negócio por Módulo

### Clientes / Entidades
- `CATEGORIA = 'F'` → dados em `ENTIDADES_PF`; apelido = `SOBRENOME`
- `CATEGORIA = 'J'` → dados em `ENTIDADES_PJ`; apelido = `NOME_FANTASIA`
- Campo `FANTASIA` na tabela `ENTIDADES` **não existe** — foi removido

### Amostras
- Código gerado no servidor no momento do cadastro
- Formato: `[TIPO(2)][SEQ(3)][ANÁLISE(2)][ANO(4)]` → ex: `QU001FQ2026`
- `SEQ` é o próximo número sequencial para a combinação tipo+análise+ano
- Ao criar: insere em `MOV_AMOSTRAS` (tipo `E`) e em `HIST_AMOSTRAS_SALDOS`

### Propostas
- Código: `[COD_ENTIDADE padded]/[ANO]-R[REVISAO]`
- Revisão começa em `0` (R0); cada edição relevante incrementa
- Desconto aplicado em cascata: altera `PORC_DESCONTO` e recalcula `VR_DESCONTO` de todos os itens

### Usuários
- Senha sempre armazenada como `SHA256(senha_digitada).ToUpperInvariant()`
- Usuários migrados do Firebird: senha inicial = `SHA256(login.ToLower())`

---

## Segurança

- Nunca confiar em dados do cliente sem validação server-side
- Datas e IDs vindos de forms são validados via `ModelState`
- Upload de assinatura: valida tipo MIME antes de salvar no BLOB
- Sessão tem timeout de 8 horas — SessaoFilter rejeita requests sem sessão válida

---

## Convenções de Commit

Mensagens em português, no infinitivo:

```
Adicionar cadastro de funcionários com abas AJAX
Corrigir exibição do NOME_FANTASIA no grid de clientes
Remover coluna FANTASIA da tabela ENTIDADES
```

---

## Ambiente de Desenvolvimento

| Item | Valor |
|------|-------|
| URL local | `http://localhost:5050` |
| Banco MySQL | `localhost:3306` / `labcontrol` / root:root |
| Banco Firebird (legado) | `C:\Project\Laboratorio\DBMAIS.FDB` / SYSDBA:masterkey |
| Login padrão | ADMINISTRADOR / administrador |
| GitHub | `marcosrgil-70/LabControl` (branch `master`) |
