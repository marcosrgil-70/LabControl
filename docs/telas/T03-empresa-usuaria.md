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
- [x] Nova empresa já nasce com status, perfis e parâmetros; aparece na seleção de empresa do criador.
- [x] Copiar auxiliares/parâmetros/produtos de outra empresa sem copiar clientes.
- [ ] Logo aparece no PDF do boletim — **na tarefa 5.4** (PDF do boletim). O logo já é gravado (PNG/JPG validados pelo conteúdo, até 1 MB) e servido em `/Empresa/Logo/{id}`.

Testes: `tests/LabControl.Tests/EmpresaServiceTests.cs`.

## Implementação (tarefa 1.5)
- `Controllers/EmpresaController.cs` (`/Empresa/Dados` da empresa ativa: `EMPRESAS.CONSULTAR` para ver, `EMPRESAS.ALTERAR` para gravar; `/Empresa/Logo`; `/Empresa/Cidades` para o combo) · `Controllers/EmpresasController.cs` (`/Empresas`, só ADMIN_GLOBAL — `[SomenteAdminGlobal]`) · `Services/EmpresaService.cs` · `Services/CopiaCadastros.cs` · `Views/Shared/_EmpresaCampos.cshtml`.
- Código imutável; CNPJ com dígito verificador, gravado como `00.000.000/0000-00`; IE em maiúsculas (aceita "ISENTO"); endereço com UF → cidade (combo simples; o componente com ViaCEP vem na T11-03); usuário comum não altera "Ativa"; ADMIN_GLOBAL não inativa a empresa ativa. Gravar os dados da empresa ativa atualiza o nome no topo (claims).
- **Nova empresa** (ADMIN_GLOBAL), numa transação: `EMPRESAS` → `SP_INICIALIZAR_EMPRESA` (status, parâmetros, ADMINISTRADOR + perfis sugeridos) → cópia opcional → acesso do criador com o perfil ADMINISTRADOR → evento de auditoria com o resumo da cópia (linhas por tabela).
- **Cópia** (`CopiaCadastros`): INSERT … SELECT por tabela, colunas lidas do `information_schema`; não duplica o que já existe no destino (mesma chave única); FKs remapeadas pela chave natural (parâmetro → tipo/método, produto → unidade/embalagem, parcela → condição); grupos puxam as tabelas de que dependem; a sequência `PRODUTO` do destino sobe até o maior código copiado (o próximo produto não repete). Nunca copia entidades, propostas, amostras, resultados, boletins nem sequências de documentos. Um teste confere que as chaves e FKs declaradas batem com o banco.

**Status:** ✅ concluída na tarefa 1.5 (19/10/2026) — falta só o logo no PDF (5.4).
