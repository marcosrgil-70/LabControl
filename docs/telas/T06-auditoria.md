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
- [ ] Revisão de amostra/resultado/boletim e emissão de boletim aparecem com usuário e motivo.
- [ ] Login com falha aparece (ação L).
- [ ] Diferença antes/depois legível.

**Status:** ⬜
