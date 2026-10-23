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
  - destino = **banco separado e vazio** (padrão `labcontrol_migracao`, criado por `.\Scripts\v2\recriar.ps1 -Banco labcontrol_migracao -Forcar`), executado a partir do app logado no `labcontrol_v2`;
  - **senha provisória única** para todos (digitada na tela, nunca gravada em claro nem exibida; troca obrigatória no 1º acesso);
  - **todos os usuários com o perfil ADMINISTRADOR** (USUCOD 1 → ADMIN_GLOBAL).
- **Tela:**
  - conexão Firebird (caminho/usuário de `appsettings` › `Firebird`, senha digitada), banco destino, senha provisória + confirmação;
  - recusa destino com dados (empresas/usuários), inexistente ou proibido (`labcontrol`, `mysql`…) mostrando o comando de criação;
  - execução **em segundo plano**, com a página se atualizando a cada 2 s;
  - etapas (situação, gravados, tempo, erro), conferências esperado (lido no Firebird na hora) × encontrado, anomalias, log e **relatório CSV**.
- **Diferença do desenho original:** executa a parte inteira em sequência (M-01 → M-10), parando na 1ª falha. Cada etapa tem a sua transação, então a falha desfaz só ela. Não há escolha de etapas soltas: os dicionários `idAntigo → idNovo` vivem em memória e o destino é recriado a cada execução. A parte 2 (4.1) reconstrói os mapas por `ID_LEGADO` e chaves únicas.
- **Conexão destino:** própria, aberta à mão com `SET @LC_MIGRACAO = 1`; contexto sem usuário (o logado é do banco do app), empresa = a criada na M-01.
- **Resultado contra o `DBMAIS.FDB` real** (`MigracaoParte1Tests`, banco `labcontrol_migracao_teste`):
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

## Critérios de aceite
- [x] Parte 1: todas as conferências verdes num banco limpo — `MigracaoParte1Tests`.
- [x] Login de usuário migrado com troca obrigatória — mesmo teste.
- [ ] Todas as conferências do doc 13 §3 (partes 2 e 3: propostas, amostras, resultados, boletins) — 4.1, 5.1.
- [ ] Nº das amostras idêntico ao legado (amostragem de 20) — 4.1.
- [ ] Boletins migrados congelados (emitidos) e somente leitura — 5.1.
- [x] Relatório de anomalias gerado (tela + CSV).
- [x] Teste no navegador da tela (`CrudNavegadorTelasTests.Migracao_tela_valida_o_pedido_sem_executar`) — passou em 22/10/2026 (pendência 17).

**Status:** 🟨 parte 1 (M-01…M-10) pronta na 3.5; partes 2 (4.1) e 3 (5.1) pendentes.
