# T12 — Clientes / Fornecedores (Entidades)

**Fase:** 3 · **Rota:** `/Clientes` · **Permissão:** `CLIENTES.*`
**Legado:** `Standards/un_Entidades.pas` + `undm_Entidades` (mesmo form para Clientes, Funcionários e Empresa, conforme `TipoCadEnt`).
**Tabelas:** `ENTIDADES`, `ENTIDADES_ENDERECOS`, `ENTIDADES_FONES`, `ENTIDADES_EMAILS`. Views: `VW_CLIENTES`, `VW_ENTIDADES_ENDERECOS`.

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

**Status:** 🟨 v1 existe (sem empresa e com endereço incompleto) — **refazer sobre o schema v2**
