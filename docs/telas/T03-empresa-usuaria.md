# T03 — Empresas Usuárias do Sistema (dados da empresa e nova empresa)

**Fase:** 1 · **Menu:** *Sistema → Empresas Usuárias do Sistema* (um item só) · **Rotas:** `/Empresas` (lista; ADMIN_GLOBAL — os demais são levados a `/Empresa/Dados`), `/Empresa/Dados` (empresa ativa, com contatos) · **Permissões:** `EMPRESAS.CONSULTAR/INCLUIR/ALTERAR` · **Criticidade:** 🟠
**Legado:** menu *Cadastros → Empresa Usuária* = `Standards/un_Entidades.pas` em modo `teEMP` (entidade com `TIPO_EMPRESA_USUARIA`, que *"não pode coexistir com nenhum outro tipo"*) + tabela `EMPRESAS` (`COD_EMPRESAS`, `ID_ENTIDADES`). Troca de empresa: *Utilitários → Ativar Empresa*.
**Objetos do banco:** [`EMPRESAS`](../banco/objetos/tabelas/EMPRESAS.md), [`EMPRESAS_PARAMETROS`](../banco/objetos/tabelas/EMPRESAS_PARAMETROS.md), [`USUARIOS_EMPRESAS`](../banco/objetos/tabelas/USUARIOS_EMPRESAS.md), [`SP_INICIALIZAR_EMPRESA`](../banco/objetos/rotinas/SP_INICIALIZAR_EMPRESA.md), [`CIDADES`](../banco/objetos/tabelas/CIDADES.md)
**Processos:** [P23 Nova empresa e cópia de cadastros](../processos/P23-nova-empresa.md) · [P01 Multiempresa](../processos/P01-multiempresa.md) · **Regras:** [RN-01](../regras/RN-01-gerais-multiempresa.md)

## Campos (`EMPRESAS`)
| Campo | Coluna | Regra |
|---|---|---|
| Código* | `COD_EMPRESA` | 3 caracteres, único, **imutável** depois de criado (legado `'001'`) |
| Razão social* | `RAZAO_SOCIAL` | até 150 |
| Nome fantasia | `NOME_FANTASIA` | aparece no topo do sistema |
| CNPJ | `CNPJ` | máscara + dígito verificador, único |
| Inscrições estadual / municipal | `INSC_ESTADUAL`, `INSC_MUNICIPAL` | IE aceita "ISENTO" |
| **Endereços** (vários) | `ENTIDADES_ENDERECOS` da entidade da empresa (`EMPRESAS.ID_ENTIDADES`) | aba própria: tipo de endereço, tipo de logradouro, CEP com busca (ViaCEP), cidade IBGE; um ativo por tipo ([P25](../processos/P25-cadastro-entidades-contatos.md)) |
| **Telefones** (vários) | `ENTIDADES_FONES` | aba própria: tipo de telefone, DDD, número, contato, departamento, principal |
| **E-mails** (vários) | `ENTIDADES_EMAILS` | aba própria: tipo de e-mail, contato, departamento, principal, envia NF-e, envia laudo |
| Site | `SITE` | validado (http/https) |

> **Correção de 20/10/2026 (pedido do usuário):** a primeira versão tinha um endereço, um telefone e um e-mail em colunas de `EMPRESAS`. Como no legado (empresa usuária = entidade `teEMP`), agora a empresa tem a **sua entidade** (`TIPO_EMPRESA_USUARIA`, criada por `SP_INICIALIZAR_EMPRESA`) com vários endereços, telefones e e-mails tipados — o mesmo componente de Clientes (T12) e Profissionais (T13). A identidade (razão social, fantasia, CNPJ, IE, IM, ativa) continua em `EMPRESAS` e é espelhada na entidade (`TG_EMPRESAS_AU`).
| Logo | `LOGO`, `LOGO_MIME` | PNG/JPG até 1 MB; prévia; impressa no boletim, proposta, pedido e comprovante |
| Cabeçalho / rodapé do laudo | `CABECALHO_LAUDO`, `RODAPE_LAUDO` | texto (HTML simples) |
| Nº de acreditação | `NR_ACREDITACAO` | ex.: "CRL 0000" — impresso no boletim quando `FN_ACREDITADO` ≥ 1 |
| Ativa | `ATIVA` | empresa inativa some da seleção de empresa no login |

## Nova empresa (ADMIN_GLOBAL)
1. Formulário acima → INSERT em `EMPRESAS`.
2. `CALL SP_INICIALIZAR_EMPRESA(id)` (status, idiomas, moedas, tipos de endereço/fone/e-mail/registro/logradouro, parâmetros e perfil ADMINISTRADOR).
3. Opção **"Copiar cadastros de"** outra empresa: checkboxes Auxiliares · Métodos · Parâmetros de análise · Produtos · Condições de pagamento · Prazos · Tipos de resultado. **Clientes, propostas e movimentos nunca são copiados.** Detalhes em [P23](../processos/P23-nova-empresa.md).
4. Vincula o usuário criador à empresa com perfil ADMINISTRADOR.
5. Tudo registrado em `AUDITORIA`.

## Regras
- Usuário comum só vê/edita a **empresa ativa** (`/Empresa/Dados`), com `EMPRESAS.ALTERAR`.
- Não existe exclusão de empresa (só inativar).
- A empresa ativa (código + nome) aparece sempre no topo (Delphi `lbDBEmpresaAtiva`).

## Critérios de aceite
- [x] Nova empresa já nasce com status, perfis e parâmetros; aparece na seleção de empresa do criador.
- [x] Copiar auxiliares/parâmetros/produtos de outra empresa sem copiar clientes.
- [ ] Logo aparece no PDF do boletim — **na tarefa 5.4** (PDF do boletim). O logo já é gravado (PNG/JPG validados pelo conteúdo, até 1 MB) e servido em `/Empresa/Logo/{id}`.

Testes: `tests/LabControl.Tests/EmpresaServiceTests.cs`.

## Implementação (tarefa 1.5)
- `Controllers/EmpresaController.cs` (`/Empresa/Dados` da empresa ativa: `EMPRESAS.CONSULTAR` para ver, `EMPRESAS.ALTERAR` para gravar; `/Empresa/Logo`; `/Empresa/Cidades` para o combo) · `Controllers/EmpresasController.cs` (`/Empresas`, só ADMIN_GLOBAL — `[SomenteAdminGlobal]`) · `Services/EmpresaService.cs` · `Services/CopiaCadastros.cs` · `Views/Shared/_EmpresaCampos.cshtml`.
- Código imutável; CNPJ com dígito verificador, gravado como `00.000.000/0000-00`; IE em maiúsculas (aceita "ISENTO"); endereço com UF → cidade (combo simples; o componente com ViaCEP vem na T11-03); usuário comum não altera "Ativa"; ADMIN_GLOBAL não inativa a empresa ativa. Gravar os dados da empresa ativa atualiza o nome no topo (claims).
- **Nova empresa** (ADMIN_GLOBAL), numa transação: `EMPRESAS` → `SP_INICIALIZAR_EMPRESA` (status, parâmetros, ADMINISTRADOR + perfis sugeridos) → cópia opcional → acesso do criador com o perfil ADMINISTRADOR → evento de auditoria com o resumo da cópia (linhas por tabela).
- **Cópia** (`CopiaCadastros`): INSERT … SELECT por tabela, colunas lidas do `information_schema`; não duplica o que já existe no destino (mesma chave única); FKs remapeadas pela chave natural (parâmetro → tipo/método, produto → unidade/embalagem, parcela → condição); grupos puxam as tabelas de que dependem; a sequência `PRODUTO` do destino sobe até o maior código copiado (o próximo produto não repete). Nunca copia entidades, propostas, amostras, resultados, boletins nem sequências de documentos. Um teste confere que as chaves e FKs declaradas batem com o banco.

## Contatos da empresa (20/10/2026 — P25)
- `/Empresa/Dados` com abas **Dados · Endereços · Telefones · E-mails** (`Views/Empresa/Dados.cshtml` + `_ContatosAbas`/`_ContatosModais` + `wwwroot/js/contatos.js`); contatos gravados na hora por AJAX em `/Empresa/ContatoEndereco|ContatoFone|ContatoEmail|ContatoExcluir` (`EMPRESAS.ALTERAR`), lidos em `/Empresa/Contatos` (`EMPRESAS.CONSULTAR`).
- Nova empresa: só os dados de identificação; os contatos ficam disponíveis em Dados depois de criar (a entidade nasce no `SP_INICIALIZAR_EMPRESA`). Lista de empresas mostra a cidade do 1º endereço ativo.
- Testes: `CadastrosEntidadesTests` (contatos tipados, espelho da razão social) e `CadastrosTelasTests` (abas e e-mail pela tela).

## Nomenclatura (20/10/2026 — pedido do usuário)
- Havia dois itens no menu, *Empresa* (`/Empresa/Dados`) e *Empresas* (`/Empresas`), que eram **o mesmo cadastro** (tabela `EMPRESAS`, os laboratórios que usam o sistema): o primeiro editava só a empresa ativa (administrador do laboratório), o segundo listava/incluía todas (ADMIN_GLOBAL). Nenhum foi excluído, mas viraram **um item só: "Empresas Usuárias do Sistema"** (`/Empresas`, `EMPRESAS.CONSULTAR`): ADMIN_GLOBAL vê a lista; os demais caem direto nos dados da empresa ativa. Na lista, *Alterar* da empresa ativa abre `/Empresa/Dados` (com contatos); as outras abrem só a identificação. Títulos e mensagens dizem "empresa usuária".

**Status:** ✅ concluída na tarefa 1.5 (19/10/2026); contatos múltiplos tipados em 20/10/2026 — falta só o logo no PDF (5.4).

> **Teste no navegador (20/10/2026, [P26](../processos/P26-testes-ponta-a-ponta.md)):** `Empresa_dados_contatos_e_parametros` (dados + telefone persistido após recarregar) e `Empresas_admin_global_cria_e_altera` (escolha de empresa, nova empresa já com a entidade, alterar).
