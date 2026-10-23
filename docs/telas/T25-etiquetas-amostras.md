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

### Saída — decidida pelo usuário (dúvida 3, 23/10/2026)
- **Etiqueta de 80 mm de largura × 50 mm de altura** (era ≈ 56 × 30 mm no legado). A 203 dpi: 640 × 400 pontos (`q640`, `Q400,<gap>`).
- **Impressora térmica Argox.** Linguagem **PPLB** (compatível com o EPL2 do legado). Confirmar o modelo e o modo (PPLA/PPLB) na impressão de teste da 4.5.
- **Código de barras Code 128** (no lugar do Code 39 do legado). Conteúdo: o nº da amostra sem espaços (`04-01382-01/26` ou `04-01382-01/26R01`).
- **Impressão direta, sem PDF**, mas **pedindo para escolher a impressora de destino** a cada impressão:
  - a tela lista as impressoras instaladas no **servidor do LabControl** e já deixa marcada a última usada pelo usuário;
  - o servidor envia os comandos PPLB em **RAW** ao spooler do Windows (sem driver gráfico);
  - a Argox precisa estar instalada (ou compartilhada) na máquina do servidor (um servidor por cópia do sistema, pendência 16).
- O layout novo usa a área maior: nº grande, Code 128 com altura confortável para o leitor, produto em 2 linhas, PV e datas. As medidas finais saem da impressão de teste.

### Integração
Depois de gravar um recebimento (T22) o sistema oferece "Imprimir etiquetas" já com a amostra selecionada e a quantidade de embalagens.

## Critérios de aceite
- [ ] Etiqueta 80 × 50 mm na Argox com nº, **Code 128** legível pelo leitor do laboratório, produto (ou em branco), PV, datas.
- [ ] Impressão direta (RAW/PPLB) na impressora escolhida em lista; a última escolhida vem marcada.
- [ ] Quantidade por amostra respeitada.
- [ ] Chamada direta a partir do recebimento.

**Status:** ⬜
