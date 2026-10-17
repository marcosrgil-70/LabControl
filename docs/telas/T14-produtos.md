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

**Status:** 🟨 v1 existe — ajustar ao schema v2
