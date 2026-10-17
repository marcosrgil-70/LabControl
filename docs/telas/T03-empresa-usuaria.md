# T03 — Empresa Usuária (dados da empresa e nova empresa)

**Fase:** 1 · **Rotas:** `/Empresas` (lista, só ADMIN_GLOBAL), `/Empresa/Dados` (empresa ativa) · **Permissões:** `EMPRESAS.CONSULTAR/INCLUIR/ALTERAR` · **Criticidade:** 🟠
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
| Endereço | `CEP`, `LOGRADOURO`, `NUMERO`, `COMPLEMENTO`, `BAIRRO`, `ID_CIDADES` | componente de endereço com ViaCEP ([P01/T11-03](T11-03-cidades.md)) |
| DDD / Fone / E-mail / Site | `DDD`, `FONE`, `EMAIL`, `SITE` | e-mail e site validados |
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
- [ ] Nova empresa já nasce com status, perfis e parâmetros; aparece na seleção de empresa do criador.
- [ ] Copiar auxiliares/parâmetros/produtos de outra empresa sem copiar clientes.
- [ ] Logo aparece no PDF do boletim.

**Status:** ⬜ (v1 tem "Empresa" parcial — refazer sobre `EMPRESAS` v2)
