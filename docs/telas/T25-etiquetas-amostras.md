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
  - Comandos conferidos por `EtiquetasTests.Comandos_pplb_80x50_code128_com_copias_e_sigilo`.
  - **Falta a impressão de teste na Argox real e a leitura pelo leitor** (não há Argox no computador de desenvolvimento) — ver "Impressão de teste" abaixo.
- [x] Impressão direta (RAW/PPLB) na impressora escolhida em lista; a última escolhida vem marcada — `EtiquetasTests.Imprime_na_escolhida_…` + navegador.
- [x] Quantidade por amostra respeitada — mesmos testes (`P{n}` por amostra).
- [x] Chamada direta a partir do recebimento — botão "Etiquetas" na T22, navegador `Etiquetas_a_partir_do_recebimento_imprimem_na_escolhida`.

## Implementação (tarefa 4.5, 23/10/2026)
- **Arquivos:**
  - `Controllers/EtiquetasController.cs` (`Index`, `Imprimir`, permissão `ETIQUETAS.IMPRIMIR`);
  - `Services/EtiquetaService.cs`, `ViewModels/EtiquetaVM.cs`;
  - `Views/Etiquetas/Index.cshtml`, `wwwroot/js/etiquetas.js`;
  - `Infra/ImpressoraRaw.cs` (`IImpressoraRaw` + `ImpressoraRawWindows`: `EnumPrinters`/`OpenPrinter`/`WritePrinter` do `winspool.drv`, trabalho RAW).
- **Comandos** (`EtiquetaService.Pplb`):
  - mesma inicialização do EPL2 do legado (`I8,A` = 8 bits Windows-1252, `ZN`, `S3`, `OD`, `JF`, `ZT`);
  - área `q640` × `Q400,24` (80 × 50 mm a 203 dpi, gap de 3 mm);
  - **rotação de 180°**, como o legado imprimia nessa impressora;
  - **Code 128** (`B…,1,…`, 100 pontos de altura) com o nº sem espaços;
  - nº em fonte 4 dobrada; produto em 2 linhas de 48 caracteres (vazias com "Não imprimir o nome do produto");
  - `PV`, `Recebimento`, `Fabricação`, `Validade` (`---` quando não há);
  - `P{n}` cópias por amostra.
- **Tela:**
  - filtros cliente, nº e "entrega a partir de" (padrão hoje − 7);
  - só amostras com status que permite resultado (como o legado) e só a última revisão;
  - marcar/desmarcar todas; quantidade por amostra (padrão = embalagens entregues, 1–99);
  - impressora escolhida na lista das instaladas no **servidor**; a última usada fica marcada pelo navegador (`localStorage`).
- **Do recebimento (T22):** o botão "Etiquetas" abre a T25 com a amostra já marcada e a quantidade de embalagens.
- **Testes:** `EtiquetasTests` (3, um deles chama o spooler real do Windows) e o navegador, com a impressora falsa `ImpressoraDoTeste`, que guarda os comandos.

### Impressão de teste (a fazer com a Argox)
1. Instale a Argox no computador **servidor do LabControl** (driver da Argox ou "Generic / Text Only"; a porta USB/rede da impressora). Confirme no painel da impressora que ela está em **PPLB**.
2. Na T25, escolha uma amostra, a impressora Argox e quantidade 1, e clique em "Imprimir etiquetas".
3. Confira:
   - a etiqueta sai **inteira na 80 × 50 mm, sem pular** (senão ajustar o gap `Q400,24`);
   - os textos ficam **na posição de leitura** (senão tirar a rotação de 180°);
   - o **leitor do laboratório lê o Code 128** com o número exato.
4. Registre aqui o modelo da Argox e os ajustes feitos.

**Status:** 🟨 pronta na 4.5 (tela, comandos, impressão direta, integração com a T22); falta a impressão de teste na Argox real e a leitura pelo leitor.
