# T07 — Migração de Dados (Firebird → MySQL v2)

**Fases:** 3 (parte 1), 4 (parte 2), 5 (parte 3), 9 (completa) · **Rota:** `/Migracao` · **Permissão:** somente `ADMIN_GLOBAL` · **Criticidade:** 🔴
**Legado:** não existe no Delphi. A v1 tinha `Controllers/MigracaoController.cs`, que está **errado** (achata produtos da proposta, não migra resultados/boletins, endereços incompletos) e será substituído.
**Objetos do banco:** todos (ver [objetos](../banco/objetos/README.md)); especialmente [`SEQUENCIAS_DOCUMENTOS`](../banco/objetos/tabelas/SEQUENCIAS_DOCUMENTOS.md) e as variáveis `@LC_MIGRACAO`.
**Processos:** [P24 Migração](../processos/P24-migracao.md) e as etapas [M-01 … M-18](../processos/migracao/README.md) · **Documento base:** [banco/13-MIGRACAO-DADOS.md](../banco/13-MIGRACAO-DADOS.md)

## Tela
1. **Conexões**: string do Firebird (`localhost:3050`, `C:\Project\Laboratorio\DBMAIS.FDB`, SYSDBA, charset ISO8859_1) e confirmação do banco destino (`labcontrol_v2`, deve estar **vazio** — recém-criado pelos scripts).
2. **Etapas**: lista M-01 … M-18 com checkbox, estado (pendente / executando / ok / erro), registros lidos × gravados e tempo. Botões "Executar selecionadas" e "Executar todas". Etapas dependentes ficam desabilitadas até as anteriores terminarem.
3. **Log em tempo real** (SignalR ou polling): mensagens por etapa, avisos de anomalia (doc 10 §8) e erros.
4. **Conferências** (doc 13 §3): tabela esperado × encontrado com ✅/❌ — contagens, somas de valores por ano, amostras por ano, 20 números de amostra sorteados, uma `ULTIMA_REVISAO = 1` por documento, saldos.
5. **Relatório final** (PDF/CSV): conferências, anomalias, propostas com `COD_PROPOSTA = ID`, documentos duplicados, **senhas provisórias dos usuários** (exibidas uma única vez).

## Regras
- Conexão MySQL própria com `SET @LC_MIGRACAO = 1` (desliga numeração e validações das triggers); ao final `SET @LC_MIGRACAO = 0`.
- Uma transação por etapa; falha numa etapa não deixa dados parciais dela.
- Repetição = recriar o banco do zero (`Scripts/v2/01..05`) — a tela avisa que não faz "migração incremental".
- Dicionários `idAntigo → idNovo` em memória por tabela + coluna `ID_LEGADO` para rastrear.
- Só ADMIN_GLOBAL; tudo registrado em `AUDITORIA`.

## Critérios de aceite
- [ ] Todas as conferências do doc 13 §3 verdes num banco limpo.
- [ ] Nº das amostras idêntico ao legado (amostragem de 20).
- [ ] Boletins migrados congelados (emitidos) e somente leitura.
- [ ] Relatório de anomalias gerado.

## Implementação — parte 1 (tarefa 3.5, 21/10/2026)
- **Arquivos:**
  - `Services/Migracao/`: `MigracaoService`, `MigracaoExecutor` (singleton, segundo plano), `ContextoMigracao`, `LeitorFirebird`, `Conversoes`, `RelatorioMigracao`;
  - `Etapas/M01…M10`;
  - `Controllers/MigracaoController.cs`, `ViewModels/MigracaoVM.cs`, `Views/Migracao/Index.cshtml`.
- **Decisões do usuário:**
  - destino = **banco separado e vazio** (ex. `labcontrol_migracao`, criado por `.\Scripts\v2\recriar.ps1 -Banco labcontrol_migracao -Forcar`), executado a partir do app logado no `labcontrol_v2`;
  - **ou o próprio banco em uso, recém-instalado** (29/10/2026, decisão do usuário: instalação com um banco só, `LabControlDB`). É o destino padrão da tela. Aceito só se tiver apenas o que o `inicial.sql` cria: no máximo 1 empresa, só o ADMINISTRADOR sem `USUCOD_LEGADO`, até 2 entidades, nenhuma proposta ou amostra. A migração apaga esse conteúdo (menos `PAISES`, `ESTADOS`, `CIDADES` e `PERMISSOES`, dos scripts) e roda M-01…M-18 **numa transação só**: falha ou interrupção desfaz tudo e o banco volta como o instalador deixou. O ADMINISTRADOR migrado fica com `Admin123`, sem troca. Ao final, entra-se de novo (o usuário logado foi substituído). Teste: `MigracaoTests.Instalacao_nova_migra_no_proprio_banco_e_falha_desfaz_tudo`;
  - **senha provisória única** para todos (digitada na tela, nunca gravada em claro nem exibida; troca obrigatória no 1º acesso);
  - **todos os usuários com o perfil ADMINISTRADOR** (USUCOD 1 → ADMIN_GLOBAL).
- **Tela:**
  - conexão Firebird (caminho/usuário de `appsettings` › `Firebird`, senha digitada), banco destino, senha provisória + confirmação;
  - recusa destino com dados (empresas/usuários), inexistente ou proibido (`labcontrol`, `mysql`…) mostrando o comando de criação;
  - execução **em segundo plano**, com a página se atualizando a cada 2 s;
  - etapas (situação, gravados, tempo, erro), conferências esperado (lido no Firebird na hora) × encontrado, anomalias, log e **relatório CSV**.
- **Diferença do desenho original:** executa a parte inteira em sequência (M-01 → M-10), parando na 1ª falha. Cada etapa tem a sua transação, então a falha desfaz só ela. Não há escolha de etapas soltas: os dicionários `idAntigo → idNovo` vivem em memória e o destino é recriado a cada execução. A parte 2 (4.1) reconstrói os mapas por `ID_LEGADO` e chaves únicas.
- **Conexão destino:** própria, aberta à mão com `SET @LC_MIGRACAO = 1`; contexto sem usuário (o logado é do banco do app), empresa = a criada na M-01.
- **Resultado contra o `DBMAIS.FDB` real** (`MigracaoTests`, banco `labcontrol_migracao_teste`):
  - todas as conferências verdes;
  - 527 entidades / 664 e-mails / 695 fones / 538 endereços / 1.915 produtos / 462 parâmetros (52 acreditados) / 176 métodos / 15 usuários;
  - login de usuário migrado exige troca de senha;
  - migração em ~1 min 23 s.
- **Anomalias da 1ª execução (28, todas no relatório):**
  - 11 cidades sem IBGE (grafia, bairro, UF trocada);
  - 1 status repetido;
  - tipos padrão do v2 incluídos;
  - a entidade 4 (empresa e cliente) virou entidade da empresa + cliente com o mesmo CNPJ (D-22);
  - 1 PF sem dados, 1 PF+PJ;
  - 4 e-mails corrigidos, 1 CEP inválido;
  - 2 profissionais sem dados;
  - 1 parâmetro sem tipo → "NÃO INFORMADO" (99, inativo).
- **Revisão (3.6, 23/10/2026)** — robustez para dados que o legado real não tem, achados do `/code-review`:
  - repetidos sempre únicos e dentro do tamanho da coluna (`ContextoMigracao.Unico`): código de produto (`-{ID antigo}`), descrição de parâmetro (" (2)", " (3)"…), descrição de tipo de amostra/análise (" ({ID})");
  - tipo "NÃO INFORMADO" reaproveita o existente ou usa o maior código livre (não mais 99 fixo);
  - CPF/CNPJ fora de 11/14 dígitos cortado no tamanho da coluna (M-01, M-05);
  - entidade do legado que é só a empresa e tem linha de funcionário não vira profissional (a empresa usuária não pode ser funcionário);
  - M-04 chama o `SP_INICIALIZAR_EMPRESA` pelo `Procedures`; CSV com a situação/erro da etapa num campo só.

## Implementação — parte 2 (tarefa 4.1, 23/10/2026)
- **Decisões do usuário:**
  - **uma execução só**, M-01→M-15 em sequência, num banco recém-criado (`MigracaoService.Etapas`); a tela não tem mais "parte 1";
  - **log das amostras do legado migrado marcado** (sem data/usuário no legado).
- **Etapas:** `Etapas/M11Propostas`, `M12ProdutosAnalisesProposta`, `M13PedidosVenda`, `M14Amostras`, `M15TestesEstoqueArquivo`.
- **Apoio:**
  - `ContextoMigracao.InserirEmLoteAsync`: INSERT multi-linha parametrizado; devolve os IDs na ordem e recusa o lote se não vierem em sequência;
  - `ContextoMigracao.Compartilhado<T>`: dados que uma etapa monta para as seguintes;
  - `Conversoes.RtfParaHtml` e `Conversoes.DataParcial`;
  - `LinhaFb.DataHora/Hora/Colunas`.
- **Por que INSERT direto:** o EF nunca envia o que o banco preenche fora da migração: ano/código/nº de proposta, amostra e pedido; subtotal/total das análises e totais da proposta; `DATA_MOV`; `DT_LOG`. Na migração esses valores vêm do legado, e as triggers com `@LC_MIGRACAO = 1` os preservam (o `NR_PROPOSTA`/`NR_AMOSTRA` continua calculado pelo banco).
- **Resultado contra o `DBMAIS.FDB` real** (`MigracaoTests`):
  - todas as conferências verdes;
  - M-01→M-15 em **~3 min** (M-15 é a maior: 71 s);
  - 2.358 propostas, 5.769 itens (5.728 + 41 criados), 19.168 análises;
  - 1.433 pedidos, 5.770 amostras (5.767 números idênticos ao impresso), 20.551 testes;
  - 5.699 movimentos, 2.494 localizações, saldos iguais, 10.390 linhas de log.
- **Anomalias da parte 2:** ver M-11…M-15 › Implementação. Principais:
  - 33 propostas provisórias;
  - itens de 21 propostas excluídas;
  - itens criados para 47 amostras;
  - 1 proposta com 2 pedidos;
  - amostra do ano 0;
  - 7 datas parciais;
  - 71 movimentos de revisão;
  - 178 localizações duplicadas;
  - 119 linhas de log órfãs.

## Implementação — parte 3 (tarefa 5.1, 27/10/2026)
- **Etapas:** `Etapas/M16Resultados`, `M17Boletins`, `M18Sequencias`; a tela e a confirmação passam a dizer M-01 a M-18.
- **Decisões do usuário:** resultado com data antes do início → início = data do resultado (63); boletim com amostra diferente da do resultado → fica na amostra do resultado, código recalculado (7).
- **Resultado contra o `DBMAIS.FDB` real** (`MigracaoTests`):
  - todas as conferências verdes; M-16 ~60 s, M-17 ~70 s, M-18 < 1 s;
  - 5.345 resultados, 19.497 parâmetros (31 repetidos fundidos);
  - 5.312 boletins, **todos emitidos** com status "ENVIADO AO CLIENTE", 19.469 linhas (146 não migradas, todas listadas);
  - próximo número de amostra/proposta/pedido/produto conferido num savepoint desfeito.
- **Anomalias da parte 3:** ver M-16…M-18 › Implementação.

## Critérios de aceite
- [x] Parte 1: todas as conferências verdes num banco limpo — `MigracaoTests`.
- [x] Login de usuário migrado com troca obrigatória — mesmo teste.
- [x] Todas as conferências do doc 13 §3 — parte 2 (propostas, pedidos, amostras, testes, saldo) na 4.1; resultados, boletins e sequências na 5.1.
- [x] Nº das amostras idêntico ao legado — **todas** conferidas na 4.1 (não só 20).
- [x] Boletins migrados congelados (emitidos) e somente leitura — 5.1 (`MigracaoTests`: alteração recusada com a migração desligada).
- [x] Relatório de anomalias gerado (tela + CSV).
- [x] Teste no navegador da tela (`CrudNavegadorTelasTests.Migracao_tela_valida_o_pedido_sem_executar`) — passou em 22/10/2026 (pendência 17).

## Cópia nova do legado (29/10/2026)
O usuário trocou o `DBMAIS.FDB` pela cópia do dia, com **11.517 propostas e 55.710 amostras**, cerca de 10× a da 9.1. A migração parava, e foi corrigida:
- **M-11:** propostas sem endereço do laudo recebem o do cliente.
- **M-06:** contato do resultado que não é e-mail é migrado como está, para corrigir no cadastro.
- **Tempo dos comandos:** limite de 30 min por comando, porque a M-17 leva 158 s.
- **Mensagem de erro:** a tela mostra o erro original, e não mais "Connection must be Open".

Com a cópia nova: 18 etapas e todas as conferências verdes, em ~10 min. A tela mostra o progresso; a migração roda no servidor.

O `MigracaoTests` usa a cópia fixa da 9.1 (`C:\Project\backup\DBMAIS-9.1.FDB`), porque confere as contagens do aceite.

**Status:** ✅ M-01…M-18 prontas (3.5, 4.1 e 5.1), numa execução só; ajustadas para a cópia nova do legado (30/10/2026).
