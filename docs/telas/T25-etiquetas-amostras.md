# T25 — Etiquetas de Amostras (código de barras)

**Fase:** 4 (junto do recebimento) · **Rota:** `/Etiquetas` · **Permissão:** `ETIQUETAS.IMPRIMIR` · **Criticidade:** 🟠 (identificação física da amostra)
**Legado:** `LabControl/un_EtqAmostra.pas` + `un_EtqAmostraQtde.pas`, menu *Relatórios → Etiqueta de Amostras*. Impressão **direta na porta da impressora** (`edtPorta`, ex.: `LPT1:`) em linguagem **EPL2** (Zebra/Eltron).
**Objetos do banco:** [`VW_LAB_AMOSTRAS`](../banco/objetos/views/VW_LAB_AMOSTRAS.md), [`AMOSTRAS_STATUS`](../banco/objetos/tabelas/AMOSTRAS_STATUS.md), [`FN_DATA_PARCIAL`](../banco/objetos/rotinas/FN_DATA_PARCIAL.md)
**Processos:** [P19 Etiquetas](../processos/P19-etiquetas.md) · [P09 Recebimento](../processos/P09-recebimento-amostra.md) · **Regras:** [RN-05](../regras/RN-05-amostras.md)

## Como era no Delphi
- Filtros: **cliente** (CPF/CNPJ ou nome) **ou** nº da amostra, e "amostras a partir de" (data de entrega ≥).
- Só amostras com o status de `EMPRESAS_PARAMETROS.ID_AMOSTRAS_STATUS_RESULTADOS` (Liberada) da empresa ativa.
- Grid com seleção ("…" marca/desmarca), "Marcar todos"/"Desmarcar todos", quantidade padrão de etiquetas (campo geral, padrão 1) e quantidade por linha (`un_EtqAmostraQtde`).
- Opção **"Não imprimir o nome do produto"** (sigilo).
- Layout EPL2 de cada etiqueta (`q450`, `Q240,25` ≈ 56 × 30 mm a 203 dpi):

| Linha | Conteúdo |
|---|---|
| Código de barras | **Code 39** (`B375,223,2,3,…`) com o nº da amostra |
| Texto | nº da amostra (fonte 3) |
| Produto | descrição em **2 linhas de 32 caracteres** (vazio se "não imprimir produto") |
| PV | `PV: <nº do pedido de venda>` |
| Recebimento | `Recebimento: dd/mm/aaaa` (data de entrega) |
| Fabricação | `Fabricação: <data parcial>` |
| Validade | `Validade: <data parcial>` ou `---` |
| Cópias | `P<quantidade>` |

## Tela web
### Filtros
Cliente (nome/CPF-CNPJ) **ou** nº da amostra; "amostras a partir de" (padrão hoje − 7 dias); só última revisão; status que permite resultado (`PERMITE_RESULTADO = 1`).

### Grid
Seleção · nº da amostra · produto · data de entrega · pedido · fabricação · validade · **quantidade de etiquetas** (padrão = `QTDE_EMBALAGENS_ENTREGUE`, editável).

### Saída (duas opções — decidir com o usuário, dúvida 3 do plano)
1. **PDF no tamanho da etiqueta** (QuestPDF, largura × altura × colunas configuráveis na empresa), impresso pelo navegador — recomendado, funciona com qualquer impressora.
2. **Arquivo EPL2/ZPL** (download ou envio a um serviço local de impressão) — reproduz exatamente o legado para impressoras térmicas Zebra/Argox.
- Código de barras: manter **Code 39** (compatível com leitores atuais) ou Code 128 (mais compacto) — **confirmar**; o conteúdo é o nº da amostra sem espaços (`04-01382-01/26` ou `04-01382-01/26R01`).

### Integração
Depois de gravar um recebimento (T22) o sistema oferece "Imprimir etiquetas" já com a amostra selecionada e a quantidade de embalagens.

## Critérios de aceite
- [ ] Etiqueta com nº, código de barras legível pelo leitor do laboratório, produto (ou em branco), PV, datas.
- [ ] Quantidade por amostra respeitada.
- [ ] Chamada direta a partir do recebimento.

**Status:** ⬜
