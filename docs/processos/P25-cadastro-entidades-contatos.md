# P25 — Cadastro de Entidades e Contatos (endereços, telefones e e-mails)

**Criticidade:** 🟠 · **Fase:** antecipada em 20/10/2026 (pedido do usuário) — base das fases 3 a 8
**Telas:** [T03 Empresa usuária](../telas/T03-empresa-usuaria.md) · [T12 Clientes/Fornecedores](../telas/T12-clientes.md) · [T13 Profissionais](../telas/T13-profissionais.md) · auxiliares [T10-01](../telas/T10-01-tipos-logradouro.md) … [T10-06](../telas/T10-06-cargos-funcionarios.md) · [T11-03 Cidades](../telas/T11-03-cidades.md) (componente de endereço)
**Objetos:** [`EMPRESAS`](../banco/objetos/tabelas/EMPRESAS.md), [`ENTIDADES`](../banco/objetos/tabelas/ENTIDADES.md), [`ENTIDADES_ENDERECOS`](../banco/objetos/tabelas/ENTIDADES_ENDERECOS.md), [`ENTIDADES_FONES`](../banco/objetos/tabelas/ENTIDADES_FONES.md), [`ENTIDADES_EMAILS`](../banco/objetos/tabelas/ENTIDADES_EMAILS.md), [`ENTIDADES_FUNCIONARIOS`](../banco/objetos/tabelas/ENTIDADES_FUNCIONARIOS.md), [`CIDADES`](../banco/objetos/tabelas/CIDADES.md), [`TG_EMPRESAS_AU`](../banco/objetos/triggers/TG_EMPRESAS_AU.md), [`TG_ENT_BI`](../banco/objetos/triggers/TG_ENT_BI.md), [`TG_ENT_BU`](../banco/objetos/triggers/TG_ENT_BU.md), [`TG_ENTEND_BI`](../banco/objetos/triggers/TG_ENTEND_BI.md)/[`BU`](../banco/objetos/triggers/TG_ENTEND_BU.md), [`TG_ENTFON_BI`](../banco/objetos/triggers/TG_ENTFON_BI.md)/[`BU`](../banco/objetos/triggers/TG_ENTFON_BU.md), [`TG_ENTEML_BI`](../banco/objetos/triggers/TG_ENTEML_BI.md)/[`BU`](../banco/objetos/triggers/TG_ENTEML_BU.md), [`PR_CONFERIR_ENDERECO`](../banco/objetos/rotinas/PR_CONFERIR_ENDERECO.md), [`PR_CONFERIR_FONE`](../banco/objetos/rotinas/PR_CONFERIR_FONE.md), [`PR_CONFERIR_EMAIL`](../banco/objetos/rotinas/PR_CONFERIR_EMAIL.md), [`PR_CONFERIR_TIPOS_ENTIDADE`](../banco/objetos/rotinas/PR_CONFERIR_TIPOS_ENTIDADE.md), [`SP_INICIALIZAR_EMPRESA`](../banco/objetos/rotinas/SP_INICIALIZAR_EMPRESA.md)
**Regras:** [RN-03](../regras/RN-03-cadastros.md) (RN-03.01, .02, .05, .09, .10, .11, .12, .13, .15, .16, .17)

## 1. Por que (o que estava errado)
No legado (Delphi `Standards/un_Entidades.pas`, o mesmo formulário em modo `teCLI`, `teFUNC` e `teEMP`) **todo cadastro de pessoa** — cliente, fornecedor, funcionário e a **empresa usuária** — tem várias linhas de:
- **endereço**, cada uma com **tipo de endereço** (Nota fiscal, Correspondência, Entrega, Cobrança) e **tipo de logradouro** (Rua, Av., Rod.…);
- **telefone**, com **tipo de telefone** (Comercial, Residencial, Celular comercial/particular, Rádio) num combo;
- **e-mail**, com **tipo de e-mail**, contato, departamento, principal, envia NF-e.

A primeira versão da v2 tinha as tabelas filhas para `ENTIDADES`, mas a **empresa usuária** (`EMPRESAS`) ficou com **um** endereço, **um** telefone e **um** e-mail em colunas soltas, e as telas T12/T13 ainda não existiam. Corrigido em 20/10/2026 (decisão do usuário: "Entidade vinculada", como no legado; escopo completo).

## 2. Modelo de dados
```
EMPRESAS (código, razão social, fantasia, CNPJ, IE, IM, site, logo, textos do laudo, acreditação, ativa, ID_ENTIDADES)
   └──► ENTIDADES  (TIPO_EMPRESA_USUARIA = 1, categoria J, mesma empresa)
CLIENTES / FORNECEDORES / COMERCIAIS / TRANSPORTADORES / PROFISSIONAIS
   └──► ENTIDADES  (TIPO_CLIENTE / _FORNECEDOR / _VENDEDOR / _TRANSPORTADOR / _FUNCIONARIO)
                    ├─ ENTIDADES_ENDERECOS  → ENDERECOS_TIPOS, TIPOS_LOGRADOURO, CIDADES (IBGE)
                    ├─ ENTIDADES_FONES      → FONES_TIPOS
                    ├─ ENTIDADES_EMAILS     → EMAILS_TIPOS
                    └─ ENTIDADES_FUNCIONARIOS (só profissional) → CARGOS_FUNCIONARIOS, TIPOS_REG_PROFISSIONAL
```
- `EMPRESAS` continua **global** (login, topo, lista de empresas, PDF) e é a dona da **identidade** da empresa; a trigger `TG_EMPRESAS_AU` espelha razão social, fantasia, CNPJ, IE, IM e situação na entidade. As colunas `LOGRADOURO, NUMERO, COMPLEMENTO, BAIRRO, CEP, ID_CIDADES, DDD, FONE, EMAIL` **saíram** de `EMPRESAS`.
- A entidade da empresa é criada por `SP_INICIALIZAR_EMPRESA` (só se `EMPRESAS.ID_ENTIDADES` for nulo — idempotente) e é da própria empresa (`ENTIDADES.ID_EMPRESAS` = a empresa), portanto passa pelo filtro global como qualquer cadastro.
- Tipos (`ENDERECOS_TIPOS`, `TIPOS_LOGRADOURO`, `FONES_TIPOS`, `EMAILS_TIPOS`, `CARGOS_FUNCIONARIOS`, `TIPOS_REG_PROFISSIONAL`) são **por empresa** e nascem com o seed do `SP_INICIALIZAR_EMPRESA` (telefones e e-mails com os valores do legado + WhatsApp/Financeiro/Técnico); mantidos nas telas de auxiliares (T10-01…06).
- Cidades: **5.571 municípios do IBGE** no seed `Scripts/v2/06_cidades.sql` (gerado por `tools/gerador-cidades`), ligados às UFs pelo código IBGE.

## 3. Regras (onde ficam)
| Regra | Onde | Mensagem |
|---|---|---|
| Filho herda `ID_EMPRESAS` da entidade; empresa diferente é recusada | `TG_ENT*_BI` + `PR_CONFERIR_EMPRESA` | "Registro pertence a outra empresa." |
| Contato não muda de entidade nem de empresa | `TG_ENTEND_BU`, `TG_ENTFON_BU`, `TG_ENTEML_BU` | "Não é permitido mover o … para outro cadastro." |
| No máximo **um endereço ativo por tipo** (o antigo fica inativo = histórico) | `PR_CONFERIR_ENDERECO` (BI/BU) | "Já existe um endereço ativo deste tipo para este cadastro. Inative o anterior ou altere-o." |
| Um **telefone principal** e um **e-mail principal** por entidade | `PR_CONFERIR_FONE` / `PR_CONFERIR_EMAIL` (BI/BU); o serviço desmarca o anterior na mesma transação | "Já existe um telefone/e-mail principal para este cadastro." |
| Contato inativo nunca é principal | triggers (zera `PRINCIPAL`) e serviço | — |
| E-mail em minúsculas, sem espaços, único na entidade | trigger + `UQ_ENTEML` | "Este e-mail já está cadastrado para esta entidade." |
| Empresa usuária não coexiste com outro tipo e não deixa de ser empresa usuária | `TG_ENT_BI/BU` + `PR_CONFERIR_TIPOS_ENTIDADE` | "A empresa usuária não pode ser também cliente…" / "Este cadastro é o da própria empresa…" |
| CPF/CNPJ com dígito verificador; único por empresa | serviço (`Documentos`) + `UQ_ENT_DOC` | "CPF inválido." / "CNPJ inválido." / "Já existe um cadastro com este CPF/CNPJ." |
| Cliente/fornecedor: ao menos um tipo (cliente, fornecedor, comercial, transportador) | `ClienteService` | "Marque ao menos um tipo de cadastro…" |
| Profissional que assina boletim tem tipo, nº e UF do registro | `ProfissionalService` (RN-03.15) | "Quem assina boletim precisa de tipo, nº e UF do registro profissional." |
| Assinatura PNG ≤ 300 KB, SHA-256 gravado | `ProfissionalService` (RN-03.17) | "A assinatura deve ser uma imagem PNG." / "…no máximo 300 KB." |
| Vínculo profissional ↔ usuário: um por empresa; profissional inativo fica sem usuário | `VinculoUsuarioProfissionalService` (mesmo da T02) + `UQ_USUEMP_FUNC` | "Este profissional já está vinculado a outro usuário nesta empresa." |
| Excluir contato/cadastro só sem uso (proposta, amostra…) — senão inativar | FKs do banco (1451) | "Registro em uso … (inative-o em vez de excluir)." |
| Validações de formato: DDD 2–3 dígitos, telefone 8–9 dígitos (gravado `0000-0000`/`00000-0000`), CEP 8 dígitos (`00000-000`), tamanhos das colunas | `ContatosService` | mensagem no campo |
| `@LC_MIGRACAO = 1` desliga as regras de principal/endereço por tipo (a carga do legado pode trazer dados fora da regra; a conferência M-xx lista) | triggers | — |

## 4. Telas e componente
- **Componente de contatos** (um só para as três telas): `Views/Shared/_ContatosAbas.cshtml` (três `.tab-pane`: Endereços, Telefones, E-mails — recarregadas por AJAX), `Views/Shared/_ContatosModais.cshtml` (formulários em modal), `wwwroot/js/contatos.js`, `Services/Entidades/ContatosService.cs`, `Controllers/ContatosAcoes.cs`.
- **Rotas** (cada tela com a própria permissão de ALTERAR; GET com a de CONSULTAR):

| Tela | Base | Permissão de escrita |
|---|---|---|
| Empresa (empresa ativa) | `/Empresa` | `EMPRESAS.ALTERAR` |
| Cliente/fornecedor | `/Clientes/{idEntidade}` | `CLIENTES.ALTERAR` |
| Profissional | `/Profissionais/{idEntidade}` | `PROFISSIONAIS.ALTERAR` |

`GET {base}/Contatos` (HTML das abas) · `POST {base}/ContatoEndereco | ContatoFone | ContatoEmail` → `{ ok, id }` ou **422** `{ erro, campo }` · `POST {base}/ContatoExcluir` (`tipo` = Endereco/Fone/Email, `idContato`).
- O serviço confere que a entidade é **da empresa ativa** e **do cadastro certo** (`CadastroDono`): pela rota de clientes não se mexe na entidade da empresa, nem pela de profissionais num cliente que não é profissional.
- **Endereço**: tipo*, CEP com botão **Buscar** (e busca ao sair do campo se o logradouro estiver vazio), tipo de logradouro, logradouro*, número, complemento, bairro, UF → cidade* (combo `GET /Geografia/CidadesDaUf?idEstado=`), inativo. Textos gravados em maiúsculas.
- **CEP**: `GET /Geografia/Cep/{cep}` → ViaCEP chamado **pelo servidor** (timeout 5 s, cache de 1 dia), cidade casada pelo **código IBGE**, "Rua X"/"Avenida X" separado em tipo de logradouro + nome quando o tipo existe na empresa. Fora do ar → mensagem e preenchimento manual.
- **Inclusão de cadastro**: grava os dados básicos e volta já na aba Endereços (`/Clientes/Editar/{id}#aba-enderecos`); as abas de contatos ficam desabilitadas antes de existir o cadastro (os contatos são gravados na hora, sem o botão "Gravar" do cadastro).
- **Sem permissão de alterar**: a tela abre só para consulta (campos desabilitados, sem botões de incluir/alterar/excluir), e o POST devolve 403.
- **Auxiliares dos tipos**: T10-01…06 no CRUD genérico (`/Auxiliares/TIPOS_LOGRADOURO`, `ENDERECOS_TIPOS` com Finalidade, `FONES_TIPOS`, `EMAILS_TIPOS`, `TIPOS_REG_PROFISSIONAL`, `CARGOS_FUNCIONARIOS`); inativo some dos combos de inclusão e continua nas linhas que já o usam.

## 5. Desenvolvimento (como foi feito / como estender)
1. **Banco** (skill `/mudanca-banco`): `01_schema.sql` (EMPRESAS sem as colunas soltas + `ID_ENTIDADES` com `UQ_EMPRESAS_ENT` e FK adiada; `ENTIDADES.TIPO_EMPRESA_USUARIA`), `02_funcoes_procedures.sql` (`PR_CONFERIR_*`, `SP_INICIALIZAR_EMPRESA` cria a entidade e os tipos), `03_triggers.sql` (`TG_EMPRESAS_AU`, `TG_ENT_BI/BU`, `TG_ENTEND/ENTFON/ENTEML_BI/BU`), `06_cidades.sql` (novo); `recriar.ps1` passou a rodar o 06 e conferir 26 procedures, 48 triggers e 5.571 cidades.
2. **Entidades C#**: `node tools/gerador-csharp/gerar.mjs` (`PRINCIPAL`/`EMAIL` marcadas como colunas que a trigger só normaliza).
3. **Serviços**: `ContatosService`, `ClienteService`, `ProfissionalService` (`Services/Entidades/`), `ConsultaCepService` + `ViaCep` (`Services/Geografia/`), `EmpresaService` sem os campos soltos.
4. **Controllers**: `ClientesController`, `ProfissionaisController`, `GeografiaController`, `EmpresaController` (+ contatos); actions de contatos são uma linha cada, delegando a `ContatosAcoes`.
5. **Novo cadastro com contatos** (ex.: transportadora com tela própria): incluir um valor em `CadastroDono`, a regra em `ContatosService.PertenceAsync`, cinco actions com a permissão da tela e as partials `_ContatosAbas`/`_ContatosModais` + `contatos.js` na view.

## 6. Testes
| Onde | O que cobre |
|---|---|
| Roteiro SQL `Scripts/v2/teste/roteiro_12_7.sql` passo **14** (13 verificações, total 62) | entidade da empresa criada e não duplicada; espelho da identidade; empresa usuária exclusiva e imutável; endereço herda empresa; um ativo por tipo (e histórico inativo aceito); um telefone/e-mail principal; inativo não é principal; e-mail em minúsculas; contato não muda de entidade |
| `CadastrosEntidadesTests` (serviços, 9) | empresa com vários contatos tipados e linhas formatadas; espelho da razão social; validações de endereço e regra por tipo; principal que troca sozinho; e-mail inválido/duplicado; contato em uso não exclui; isolamento entre empresas; cliente PF/PJ (CPF/CNPJ, duplicado, campos da outra categoria limpos, lista por tipo/documento sem máscara, empresa usuária fora da lista); exclusão com/sem movimento (contatos em cascata); profissional (registro obrigatório para assinar, assinatura PNG/tamanho/hash, vínculo com usuário, inativo desvincula, exclusão bloqueada com usuário); CEP (IBGE, tipo de logradouro, cache, inexistente, fora do ar) com fonte falsa |
| `CadastrosTelasTests` (app inteiro, 4) | incluir cliente pela tela → aba Endereços; gravar telefone por AJAX (200 / 422 com campo) e recarregar as abas; tela só de consulta sem ALTERAR (403 no POST); Empresa/Dados com abas e e-mail gravado; `/Geografia/CidadesDaUf` (645 de SP) e CEP inválido; profissional pela tela; cartões e rótulos dos auxiliares |
| `AutorizacaoActionsTests` | toda action nova de escrita com `[Permissao]` de escrita |
| **Navegador** (`tests/LabControl.Tests/Navegador/`, [P26](P26-testes-ponta-a-ponta.md)) | o CRUD clicando, com o JavaScript real: abas, modais, UF → cidade, botão **Buscar CEP**, principal, confirmações, mensagens; T03, T12, T13 |

Teste manual sugerido: Empresa → Dados → Endereços → Incluir → CEP `01310-100` → Buscar (preenche Av. Paulista, Bela Vista, São Paulo/SP) → número → Gravar; incluir um segundo endereço do mesmo tipo (recusado), inativar o primeiro e incluir de novo (aceito); telefones com dois "principal" (o segundo tira a marca do primeiro).

## 7. Migração (impacto)
- **M-01/M-05**: a entidade do legado apontada por `EMPRESAS.ID_ENTIDADES` (marcada `TIPO_EMPRESA_USUARIA`) migra como a entidade da empresa (com `@LC_MIGRACAO = 1`), e `EMPRESAS.ID_ENTIDADES` aponta para ela — o `SP_INICIALIZAR_EMPRESA` não cria outra.
- **M-02**: as 190 cidades do legado não têm código IBGE: casar por **nome + UF** com as do seed `06_cidades.sql`; as não encontradas vão para a conferência.
- **M-06/M-07**: telefones, e-mails e endereços migram com o tipo; duplicidades de "principal"/endereço ativo por tipo são listadas na conferência (as triggers não barram com `@LC_MIGRACAO = 1`).
