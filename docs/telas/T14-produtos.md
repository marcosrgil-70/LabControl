# T14 — Produtos

**Fase:** 3 · **Rota:** `/Produtos` · **Permissão:** `PRODUTOS.*` · **Tabela:** `PRODUTOS`
**Legado:** `Standards/un_Produtos.pas` (abas Principal, Dados Fiscais, Apresentação, Fornecedores, Fabricantes). Só a aba **Principal** foi usada (as tabelas das outras estão vazias). No laboratório, "produto" = **nome da amostra** do cliente (ex.: "QUEIJO MINAS").

## Referências cruzadas
- **Delphi:** `Standards/un_Produtos.pas` (1.144 linhas) + `unDM_Produtos.pas`
- **Objetos do banco:** [`PRODUTOS`](../banco/objetos/tabelas/PRODUTOS.md), [`TG_PRODUTOS_BI`](../banco/objetos/triggers/TG_PRODUTOS_BI.md), [`SP_PROXIMO_NUMERO`](../banco/objetos/rotinas/SP_PROXIMO_NUMERO.md), [`SP_TROCAR_PRODUTO_PROPOSTA`](../banco/objetos/rotinas/SP_TROCAR_PRODUTO_PROPOSTA.md), [`UNIDADES`](../banco/objetos/tabelas/UNIDADES.md), [`EMBALAGENS_TIPOS`](../banco/objetos/tabelas/EMBALAGENS_TIPOS.md)
- **Processos:** [P03 Numeração](../processos/P03-numeracao-documentos.md)
- **Regras de negócio:** [RN-03 Cadastros](../regras/RN-03-cadastros.md)
- **Plano:** [00-PLANO-EXECUCAO.md](../00-PLANO-EXECUCAO.md)

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Código | `CODIGO` | vazio → automático (`SEQUENCIAS_DOCUMENTOS 'PRODUTO'`, 6 dígitos); único por empresa |
| Descrição* | `DESCRICAO` | 150; aparece na proposta, amostra, etiqueta e boletim |
| Unidade de estoque* | `ID_UNIDADES` | T10 |
| Embalagem / Qtde por embalagem | `ID_EMBALAGENS_TIPOS`, `QTDE_EMBALAGEM` | opcional |
| Inativo | `INATIVO` | — |

Fora do escopo (tabelas vazias): seção/grupo/subgrupo, NCM/fiscal, apresentação, fornecedores, fabricantes.

## Regras
- Cadastro rápido "+" a partir da Proposta (Delphi: `Btn_IncCadProd`).
- Consulta: código/descrição (contém), inativos.
- "Trocar produto" de uma proposta (e das suas amostras) é feito na tela de Proposta via `SP_TROCAR_PRODUTO_PROPOSTA` (o `SP_TROCAPRODUTO` do legado alterava amostras de todas as propostas).

## Critérios de aceite
- [x] Código vazio → automático de 6 dígitos (sequência PRODUTO da empresa); código digitado vale como está — `ProdutosTests.Codigo_automatico_digitado_e_duplicado`.
- [x] Código duplicado recusado com a mensagem no campo — mesmo teste e `Tela_permissoes_e_cadastro_rapido`.
- [x] Em uso (proposta/pedido/amostra) não exclui; inativar some da lista e dos combos, com aviso de inativos ocultos — `Em_uso_nao_exclui_inativo_some_e_combo_mostra_o_escolhido`.
- [x] CRUD pela tela, no navegador (incluir com unidade nova pelo "+", alterar, pesquisar por código, inativar/reativar com confirmação, excluir) — `CrudNavegadorTelasTests.Produto_crud_com_codigo_automatico_e_unidade_pelo_mais`.

## Implementação (tarefa 3.3)
- `Controllers/ProdutosController.cs`, `Services/ProdutoService.cs`, `ViewModels/ProdutoVM.cs`, `Views/Produtos/{Index,Form}.cshtml` — mesmo visual da T12 (lista com busca, "mostrar inativos", 20 por página; formulário em página, só consulta sem `PRODUTOS.ALTERAR`). O item "Produtos" do menu aparece sozinho (tela existente).
- **Depois de gravar (incluir ou alterar) volta para a lista** com "Produto 000002 — DESCRIÇÃO gravado.", pronta para incluir o próximo (pedido do usuário no teste, 21/10/2026).
- Código: vazio na inclusão → o EF omite a coluna e `TG_PRODUTOS_BI` gera `000001…` (relido após gravar); digitado → maiúsculas, até 10, único (`UQ_PROD_COD` → "Já existe um produto com este código." no campo); na alteração é obrigatório (não volta a ser gerado).
- Descrição em maiúsculas (150). Unidade obrigatória e embalagem opcional: combos com ativos + o já escolhido; inativa só vale se já era a do produto. Qtde. por embalagem aceita vírgula ou ponto, > 0, até 4 casas. Unidade e embalagem com o cadastro rápido "+" (`lc-rapido`).
- Excluir: `DELETE` filtrado pela empresa; em uso o banco recusa (1451) e a mensagem sugere inativar. Inativar/reativar com confirmação (texto no `data-confirmar`).
- `POST /Produtos/Rapido` (`PRODUTOS.INCLUIR`): descrição + unidade, código automático → `{ id, texto }` (422 `{ erro, campo }`), para o "+" do combo de produto da proposta (4.2).

**Status:** ✅ concluída na tarefa 3.3 (21/10/2026).
