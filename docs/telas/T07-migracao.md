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

**Status:** ⬜ (v1 errada — reescrever como `Services/Migracao/MigracaoService.cs`)
