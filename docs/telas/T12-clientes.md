# T12 — Clientes / Fornecedores (Entidades)

**Fase:** 3 · **Rota:** `/Clientes` · **Permissão:** `CLIENTES.*`
**Legado:** `Standards/un_Entidades.pas` + `undm_Entidades` (mesmo form para Clientes, Funcionários e Empresa, conforme `TipoCadEnt`).
**Tabelas:** `ENTIDADES`, `ENTIDADES_ENDERECOS`, `ENTIDADES_FONES`, `ENTIDADES_EMAILS`. Views: `VW_CLIENTES`, `VW_ENTIDADES_ENDERECOS`.

## Referências cruzadas
- **Delphi:** `Standards/un_Entidades.pas` (1.475 linhas) + `undm_Entidades.pas` + `unfr_Entidades_Observacoes.pas`
- **Objetos do banco:** [`ENTIDADES`](../banco/objetos/tabelas/ENTIDADES.md), [`ENTIDADES_ENDERECOS`](../banco/objetos/tabelas/ENTIDADES_ENDERECOS.md), [`ENTIDADES_FONES`](../banco/objetos/tabelas/ENTIDADES_FONES.md), [`ENTIDADES_EMAILS`](../banco/objetos/tabelas/ENTIDADES_EMAILS.md), [`VW_CLIENTES`](../banco/objetos/views/VW_CLIENTES.md), [`VW_ENTIDADES_ENDERECOS`](../banco/objetos/views/VW_ENTIDADES_ENDERECOS.md), [`TG_ENTEND_BI`](../banco/objetos/triggers/TG_ENTEND_BI.md), [`TG_ENTFON_BI`](../banco/objetos/triggers/TG_ENTFON_BI.md), [`TG_ENTEML_BI`](../banco/objetos/triggers/TG_ENTEML_BI.md)
- **Processos:** [P01 Multiempresa](../processos/P01-multiempresa.md)
- **Regras de negócio:** [RN-03 Cadastros](../regras/RN-03-cadastros.md)
- **Plano:** [00-PLANO-EXECUCAO.md](../00-PLANO-EXECUCAO.md)

## Abas e campos (mesma estrutura do Delphi)

### Dados básicos
| Campo | Coluna | Regra |
|---|---|---|
| Código | `ID_ENTIDADES` | automático |
| Data do cadastro | `DATA_CADASTRO` | padrão hoje |
| Categoria | `CATEGORIA` | Física / Jurídica (radio) — define os campos abaixo |
| Nome / Razão social* | `NOME` | 150 |
| CPF ou CNPJ | `CPF_CNPJ` | máscara + dígito verificador; **único por empresa**; opcional só para estrangeiro |
| Inativo | `INATIVO` | inativos somem dos combos |
| Tipo de cadastro | `TIPO_CLIENTE`, `TIPO_FORNECEDOR`, `TIPO_FUNCIONARIO`, `TIPO_VENDEDOR` (Comercial), `TIPO_TRANSPORTADOR` | ao menos um |
| Avaliação | `AVALIACAO` | texto livre (20) |
| **PF:** Sobrenome, RG, Data de nascimento, Sexo, Estado civil | `NOME_FANTASIA`, `RG_IE`, `DT_NASCIMENTO`, `SEXO`, `ESTADO_CIVIL` | — |
| **PJ:** Nome fantasia, Inscrição estadual, municipal, SUFRAMA | `NOME_FANTASIA`, `RG_IE`, `INSC_MUNICIPAL`, `INSC_SUFRAMA` | IE aceita "ISENTO" |

### Endereços (grid + modal)
Tipo de endereço*, CEP, tipo de logradouro, logradouro*, número, complemento, bairro, cidade*, inativo. Regra: no máximo **um endereço ativo por tipo** (no Firebird era `UNIQUE (entidade, tipo)`). Componente da T11.

### Telefones
Tipo*, DDD* (2–3), telefone*, contato, departamento, **principal** (um só por entidade — ao marcar, desmarca os outros na aplicação), inativo.

### E-mails / Contatos (**usados na Proposta**)
E-mail* (único por entidade), nome do contato, departamento, tipo, **principal**, **enviar NF-e/NFS-e**, **enviar laudo**, inativo.
> Na proposta, "Contato", "Contato do resultado" e "Contato da NF" são **linhas desta aba**. Um cliente sem e-mail não consegue ter proposta gravada.

### Observações
Texto + "Incluir observação no faturamento" (`OBS_INCLUIR_FATURAMENTO`).

## Consulta
Filtros: nome/fantasia/documento (contém), tipo (cliente/fornecedor/...), inativos s/n. Colunas: código, F/J, data, nome, fantasia, documento, fone principal, inativo.

## Regras
- Excluir só sem movimento (proposta/amostra); senão inativar.
- A v1 usa endpoints AJAX por aba (`/Clientes/AdicionarFone`…): **manter o padrão**, agora com `ID_EMPRESAS` e validações acima.

## Critérios de aceite
- [x] Incluir PF/PJ com CPF/CNPJ validado (dígito verificador) e único por empresa; campos da outra categoria limpos.
- [x] Ao menos um tipo (cliente, fornecedor, comercial, transportador); funcionário é mantido na T13 (só exibido aqui).
- [x] Vários endereços (tipo de endereço + tipo de logradouro + CEP com ViaCEP + cidade IBGE), um ativo por tipo; vários telefones e e-mails tipados com um principal.
- [x] Consulta por nome/fantasia/documento (com ou sem máscara), tipo e inativos, com telefone e e-mail principais; empresa usuária nunca aparece.
- [x] Excluir só sem movimento (senão a mensagem sugere inativar; botão inativar/reativar na lista); contatos excluídos junto.
- [x] Sem `CLIENTES.ALTERAR` a tela abre só para consulta.

## Implementação (antecipada em 20/10/2026 — pedido do usuário)
- `Controllers/ClientesController.cs` (`/Clientes`, `Criar`, `Editar/{id}`, `Excluir`, `Inativo`; contatos em `/Clientes/{idEntidade}/Contatos|ContatoEndereco|ContatoFone|ContatoEmail|ContatoExcluir`) · `Services/Entidades/ClienteService.cs` · `Services/Entidades/ContatosService.cs` · `Views/Clientes/{Index,Form}.cshtml` · componente `_ContatosAbas`/`_ContatosModais` + `wwwroot/js/contatos.js` · `/Geografia/Cidades` e `/Geografia/Cep/{cep}`.
- Abas **Dados básicos · Endereços · Telefones · E-mails · Observações**; ao incluir, grava os dados e abre a aba Endereços (as de contatos ficam desabilitadas antes de existir o cadastro); contatos gravados na hora, por AJAX. Textos em maiúsculas (e-mail em minúsculas). Detalhes, regras e testes: [P25](../processos/P25-cadastro-entidades-contatos.md).
- Testes: `CadastrosEntidadesTests`, `CadastrosTelasTests`.

## Ajustes depois do teste do usuário (20/10/2026)
- **Relato:** "ao clicar no botão com formato de um olho cortado … apagou". Não apagou: era **Inativar** (só ícone, sem confirmação); o cadastro ficou `INATIVO = 1` com os contatos intactos e a lista, que esconde inativos, deixou de mostrá-lo — inclusive na pesquisa.
- **Correções:** botões com texto (Abrir, Inativar/Reativar, Excluir); confirmação antes de inativar explicando que o cadastro não é apagado; mensagem pós-inativação com "marque Mostrar inativos"; aviso no rodapé quando a pesquisa encontrou inativos ocultos (com link para mostrá-los).
- **Prova:** `Cliente_crud_completo_com_enderecos_telefones_e_emails` no navegador ([P26](../processos/P26-testes-ponta-a-ponta.md)) — incluir, endereço por UF/cidade e por **CEP (Buscar)**, recusa do 2º endereço ativo do mesmo tipo, telefones/e-mails com principal, alterar, pesquisar por CNPJ, inativar/reativar, excluir.

**Status:** ✅ concluída em 20/10/2026 (antecipada da tarefa 3.1); ajustes de usabilidade e teste no navegador no mesmo dia.
