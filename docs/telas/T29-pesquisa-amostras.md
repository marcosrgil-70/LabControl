# T29 — Pesquisa de Amostras (componente de busca avançada)

**Fase:** 4 (usada a partir da fase 4 por T22, T24, T26, T27) · **Rota:** componente modal, conteúdo em `/Amostras/Pesquisa` (sem item de menu) · **Permissão:** a da tela que abre · **Criticidade:** ⚪
**Legado:** `LabControl/un_Lab_Hist_Amostras_Pesq.pas` — modal aberto pelo botão "Pesquisar" da Localização (e reutilizado em outras telas): filtros **cliente** e **produto**, grid com nº da amostra, cliente, produto, status, tipo de amostra, tipo de análise, nº da proposta, lote, fabricação, validade e embalagem; duplo clique devolve o nº da amostra. A consulta **não filtrava a empresa** (erro).
**Objetos do banco:** [`VW_LAB_AMOSTRAS`](../banco/objetos/views/VW_LAB_AMOSTRAS.md), [`VW_CLIENTES`](../banco/objetos/views/VW_CLIENTES.md), [`PRODUTOS`](../banco/objetos/tabelas/PRODUTOS.md)
**Processos:** [P01 Multiempresa](../processos/P01-multiempresa.md)

## Filtros
Cliente (nome/CPF-CNPJ) · produto (código/descrição) · nº da amostra (contém, aceita só o sequencial "1382") · nº da proposta · lote · período de entrega (padrão `DIAS_PESQUISA_PADRAO`) · status · "só última revisão" (padrão marcado).

## Grid
Nº da amostra (com revisão) · data de entrega · cliente · produto · status (badge) · tipo de amostra · tipo de análise · proposta · pedido · lote · fabricação · validade · embalagem. Paginação no servidor; ordenação por nº da amostra.

## Comportamento
- Duplo clique ou "Selecionar" devolve `{ id, nrAmostra }` para a tela chamadora (que decide se precisa da raiz ou da última revisão).
- Parâmetros de abertura: `apenasPermiteResultado`, `apenasComResultado`, `apenasSemLocalizacao` (cada tela passa o seu filtro).
- **Sempre** filtra a empresa ativa.

## Critérios de aceite
- [x] Busca por cliente, produto ou número parcial — `AmostrasTests.Pesquisa_filtra_e_nunca_mostra_outra_empresa` e navegador `Pesquisa_de_amostras_seleciona_e_abre`.
- [x] Nunca mostra amostras de outra empresa — mesmo teste (as duas empresas têm a amostra `04-00001-01/AA`; só a da empresa ativa aparece).
- [ ] Devolve a amostra para T22/T24/T26/T27 — **T22 ✅** (4.4, "Pesquisa avançada" abre a amostra escolhida); T24/T26/T27 quando essas telas existirem (7.2, 5.2, 5.3).

## Implementação (tarefa 4.4, 23/10/2026)
- **Servidor:** `AmostrasController.Pesquisa` (`GET /Amostras/Pesquisa`, devolve só a partial `Views/Amostras/_Pesquisa.cshtml`) sobre o mesmo `AmostraService.ListarAsync` da T22.
  - filtros novos no `AmostraFiltroVM`: lote, entrega até, e os da tela que abre: `ApenasPermiteResultado`, `ApenasComResultado`, `ApenasSemLocalizacao`;
  - grid ordenada pelo número (`OrdenarPorNumero`);
  - `inicial=true` aplica o período padrão (`DIAS_PESQUISA_PADRAO`);
  - empresa sempre a ativa (filtro global do EF).
- **Cliente:** `wwwroot/js/pesquisa-amostras.js`. Qualquer `<button data-pesquisa-amostras>` abre o modal. Opções:
  - `data-filtros="ApenasComResultado=true"`: filtros fixos da tela que abre;
  - `data-alvo-id` / `data-alvo-nr`: campos a preencher;
  - `data-abrir="/Amostras/Editar/{id}"`: navegar depois de escolher;
  - `data-url="/Resultados/Pesquisa"`: action da tela que abre (5.2), com a permissão de consulta dela; padrão `/Amostras/Pesquisa`.
- **Escolha** (duplo clique ou "Selecionar"): dispara `amostra-selecionada` `{ id, nrAmostra }` no botão. A tela decide se usa a raiz ou a última revisão.
- **Permissão:** `AMOSTRAS.CONSULTAR`, a da T22. Cada tela que adota o componente tem a própria action de pesquisa com a permissão dela, devolvendo a mesma partial: T26 → `ResultadosController.Pesquisa` (`RESULTADOS.CONSULTAR`; sempre última revisão + status que permite resultado; período padrão da empresa na 1ª abertura, pendência 33).
- **T22:** botão "Pesquisa avançada" na consulta.

**Status:** ✅ concluída na tarefa 4.4 (23/10/2026); usada pela T26 desde a 5.2; entra nas T24/T27 quando elas forem feitas.

