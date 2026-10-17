# T14 — Produtos

**Fase:** 3 · **Rota:** `/Produtos` · **Permissão:** `PRODUTOS.*` · **Tabela:** `PRODUTOS`
**Legado:** `Standards/un_Produtos.pas` (abas Principal, Dados Fiscais, Apresentação, Fornecedores, Fabricantes). Só a aba **Principal** foi usada (as tabelas das outras estão vazias). No laboratório, "produto" = **nome da amostra** do cliente (ex.: "QUEIJO MINAS").

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
