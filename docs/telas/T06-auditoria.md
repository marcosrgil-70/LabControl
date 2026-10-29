# T06 — Auditoria (consulta)

**Fase:** 8 · **Rota:** `/Auditoria` · **Permissões:** `AUDITORIA.CONSULTAR/IMPRIMIR` · **Criticidade:** 🟠 (rastreabilidade ISO/IEC 17025)
**Legado:** não existia tela; só o log da amostra `LAB_HIST_AMOSTRAS_LOG` (10.509 linhas, gravado por trigger com as 45 colunas).
**Objetos do banco:** [`AUDITORIA`](../banco/objetos/tabelas/AUDITORIA.md), [`LAB_HIST_AMOSTRAS_LOG`](../banco/objetos/tabelas/LAB_HIST_AMOSTRAS_LOG.md), [`USUARIOS`](../banco/objetos/tabelas/USUARIOS.md)
**Processos:** [P21 Auditoria e log](../processos/P21-auditoria-log.md) · **Regras:** [RN-02](../regras/RN-02-seguranca.md)

## Filtros
Período (data/hora do evento, padrão últimos 7 dias) · usuário · tabela/assunto (Propostas, Amostras, Resultados, Boletins, Usuários, Perfis, Numeração, Login…) · ação (I incluir, A alterar, E excluir, R revisão, M emissão, L login) · ID do registro / nº do documento.

## Grid
Data/hora · usuário · ação · tabela · registro (com link para a tela do registro quando existir) · IP · botão "Ver diferença".

## Detalhe ("Ver diferença")
Tabela campo a campo: valor antes × valor depois, destacando os campos alterados (JSON `DADOS_ANTES`/`DADOS_DEPOIS`).

## Aba "Histórico da amostra"
Consulta de `LAB_HIST_AMOSTRAS_LOG` por nº da amostra (inclui o log migrado do legado), mostrando ação, data, usuário e os campos do JSON (status anterior → novo, quantidade, datas, responsável).

## Regras
- Somente leitura; nada pode ser alterado ou excluído pela aplicação.
- Filtra a empresa ativa (`ID_EMPRESAS`); ADMIN_GLOBAL pode ver todas e os logins (sem empresa).
- Paginação no servidor (a tabela cresce sempre); exportar CSV do filtro.

## Critérios de aceite
- [x] Revisão de amostra/resultado/boletim e emissão de boletim aparecem com usuário e motivo.
- [x] Login com falha aparece (ação L).
- [x] Diferença antes/depois legível.

## Implementação (tarefa 8.4, 29/10/2026)
- **Arquivos:** `Controllers/AuditoriaController.cs` (`Index`, `Detalhe/{id}`, `Csv` com `AUDITORIA.IMPRIMIR`, `Amostra?numero=`), `Services/AuditoriaService.cs`, `ViewModels/AuditoriaVM.cs`, `Views/Auditoria/{Index,Detalhe,Amostra,_Abas}`.
- **Filtros:** período (padrão: últimos 7 dias, relógio do banco), usuário (parte do login), assunto (grupos de tabelas: propostas, pedidos, amostras, movimentação/localização, resultados, boletins, usuários e login, perfis, empresa e parâmetros, numeração, profissionais), ação (I, A, E, R, M, D, L e **N** = envio por e-mail, 8.3) e "registro ou nº do documento" (ID do registro ou texto dentro dos dados — `LIKE` sobre o texto do JSON, porque o `Contains` do EF numa coluna JSON vira `CAST AS JSON` e quebra). 50 por página.
- **Grid:** data/hora, usuário, ação, assunto (a tabela no título), registro com link para a tela (proposta, pedido, amostra, resultado, boletim, usuário, perfil, profissional; não em exclusão), **resumo** (motivo da revisão, "entrou"/"falhou: senha" no login, destinatários do e-mail, nº do documento ou colunas alteradas), IP e "Ver diferença".
- **Ver diferença:** campo a campo antes × depois; na alteração os campos que mudaram ficam destacados; inclusão/exclusão/eventos mostram um lado.
- **Empresa:** a consulta não usa o filtro global — os logins são gravados **sem empresa** (antes de escolhê-la). Regra: eventos da empresa ativa + logins (inclusive com falha) dos usuários com acesso a ela; **ADMIN_GLOBAL** com "Todas as empresas" vê tudo, inclusive tentativas com login inexistente, e a coluna Empresa. O detalhe de um evento que a consulta não mostraria devolve 404.
- **Usuário do login:** no login ainda não há sessão (`ID_USUARIOS` vazio); o usuário vem do ID do registro (filtro) e do login gravado nos dados (coluna).
- **A revisão de amostra** gera um evento R só (o ID do resultado e do boletim novos vão dentro dele, `ID_RESULTADO_NOVO`/`ID_BOLETIM_NOVO`).
- **Aba "Histórico da amostra":** pelo nº (com ou sem " R01"), todas as revisões da amostra física, com o mesmo resumo da aba Histórico do recebimento (`AmostraService.HistoricoAsync`, agora público), inclusive o log do legado "sem data".
- **CSV** do filtro: UTF-8 com BOM e `;`, até 50.000 linhas, com os JSON antes/depois.
- **Testes:** `AuditoriaConsultaTests` (2) e o de navegador `Auditoria_filtra_mostra_a_diferenca_o_historico_da_amostra_e_exporta`.

**Status:** ✅ concluída na tarefa 8.4 (29/10/2026).
