# T29 — Pesquisa de Amostras (componente de busca avançada)

**Fase:** 4 (usada a partir da fase 4 por T22, T24, T26, T27) · **Rota:** componente modal `/_Pesquisa/Amostras` (sem item de menu) · **Permissão:** a da tela que abre · **Criticidade:** ⚪
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
- [ ] Busca por cliente, produto ou número parcial.
- [ ] Nunca mostra amostras de outra empresa.
- [ ] Devolve a amostra para T22/T24/T26/T27.

**Status:** ⬜
