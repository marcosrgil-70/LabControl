# Checklist da virada — LabControl v2 (tarefa 9.3)

**Quando:** depois da homologação (9.2) sem ocorrência "impede" aberta e com o layout do boletim aprovado (5.4).
**Quem:** quem conduz a implantação, com o responsável do laboratório para o "pode abrir".
**Retorno:** se houver bloqueio grave no 1º dia, o laboratório volta ao Delphi — o v2 **não escreve** no Firebird, que fica intacto ([F9 § 9.3](F9-homologacao-virada.md#93-virada)).

> Marque cada item. O que for anotado aqui (endereços, pastas, nomes de usuário) **não** inclui senhas: senha só no cofre/papel do responsável, nunca em arquivo versionado nem neste MD.

> 🧰 **Instalador (29/10/2026):** `instalador\saida\LabControl-Setup-<versão>.exe` faz sozinho quase tudo das partes A e B. Gerado por `instalador\construir.ps1`; detalhes em [instalador/README.md](../../instalador/README.md). O que ele faz:
> - instala o IIS, o .NET 10 Hosting Bundle, o VC++ e o MySQL 8.0, este só em `127.0.0.1`;
> - restaura o banco migrado e dá ao ADMINISTRADOR a senha provisória com troca obrigatória;
> - cria os usuários MySQL `labcontrol` e `labcontrol_backup`, com permissões mínimas;
> - cria as pastas com acesso restrito;
> - configura o site HTTPS com certificado autoassinado e o firewall;
> - agenda o backup diário.

---

## A. Servidor (dias antes)
- [ ] **Testar o instalador numa máquina virtual com Windows Server** antes do servidor do cliente (pendência 40). Ele foi testado por partes, mas nunca de ponta a ponta.
- [ ] Servidor: Windows Server 2016 ou mais novo, 64 bits. Nome na rede: ______________________. Firebird 2.5 (32 bits) só se a consulta histórica ficar nele.
- [ ] Rodar o `setup.exe` com o **backup do banco migrado** (`Scripts\v2\backup.ps1 … -Conferir` na máquina da migração). Anotar:
  - pasta do sistema: ______________________ (padrão `C:\LabControl`)
  - pasta dos dados: ______________________ (padrão `C:\LabControlDados`; de preferência em outro disco)
  - endereço: https://______________________/
  - senha do **root** do MySQL definida no assistente: guardar no cofre (pendência 12 — a do servidor nasce nova, diferente da do desenvolvimento).
- [ ] Conferir o `LEIA-ME.txt` da pasta dos dados e o atalho "LabControl" na área de trabalho do servidor.
- [ ] **Estações:** executar `instalar-certificado-na-estacao.cmd` (pasta `certificado` dos dados) como administrador, uma vez por estação, e abrir o endereço no navegador **sem aviso de "não seguro"**.
- [ ] Se o laboratório tiver domínio e certificado próprio: trocar o autoassinado pelo `.pfx` no IIS (o instalador mantém o certificado que já estiver no site).
- [ ] Impressora **Argox** instalada no servidor (pendência 21).

Configuração manual, só sem o instalador: [Scripts/v2/README.md](../../Scripts/v2/README.md#senha-do-mysql-onde-fica-e-como-trocar) e o histórico deste arquivo no git.

## B. Backup (antes de abrir para os usuários)
- [ ] Tarefa **"LabControl - Backup"** criada pelo instalador (todo dia às 22h, como SYSTEM; senha do `labcontrol_backup` cifrada pelo DPAPI da máquina em `<dados>\config\backup.senha`). **Rodar à mão uma vez** (Agendador de Tarefas › Executar) e conferir:
  - o zip em `<dados>\backup`, com o banco, os boletins e as chaves;
  - o log em `<dados>\logs\backup-ultimo.log`.
- [ ] Cópia do zip **para fora do servidor** (outro disco, nuvem ou mídia) — o zip tem dados de clientes e as chaves: destino com acesso restrito.
- [ ] **Teste de restauração** uma vez: `C:\LabControl\scripts\backup.ps1 … -Conferir` (restaura num banco temporário e compara as linhas de todas as tabelas).

## C. Dia da virada
1. [ ] Avisar o laboratório: a partir de ____:____ ninguém lança nada no Delphi.
2. [ ] **Backup final do Firebird** com o Delphi fechado: `gbak -b -user SYSDBA -password <senha> DBMAIS.FDB DBMAIS-<data>.fbk` (guardar junto com os backups da 9.1).
3. [ ] **Última migração** (T07) a partir da cópia congelada, na máquina da migração:
   - banco destino novo, criado sem `-Dev` (`.\Scripts\v2\recriar.ps1 -Banco labcontrol_virada`);
   - o app num banco de operação, criado com `-Dev` e apagado no fim;
   - *Sistema › Migração* com **senha provisória nova** para os usuários (não a da homologação).

   Esperado: 18 etapas ok e **todas as conferências verdes** (na 9.1: 74/74).
4. [ ] Relatório de anomalias salvo fora do repositório (`C:\Project\backup\migracao-<data>.csv`); conferir que as decisões da pendência 39 foram aplicadas.
5. [ ] Backup do banco migrado: `.\Scripts\v2\backup.ps1 -Destino <pasta> -Banco labcontrol_virada -Conferir` → levar o zip ao servidor.
6. [ ] No servidor, se o banco da homologação estiver lá:
   - rodar a tarefa de backup (guardar o zip da homologação);
   - apagar o banco: `DROP DATABASE labcontrol_v2;` como root. O instalador nunca sobrescreve um banco existente.
7. [ ] Rodar o `setup.exe` **com o zip da virada** e uma **senha provisória nova do ADMINISTRADOR**. Esperado: "Banco restaurado: 59 tabelas…" e a tela de login respondendo.
8. [ ] *Sistema › Parâmetros da Empresa*: conferir numeração, período de pesquisa e **e-mail**. A senha do e-mail precisa ser digitada de novo, porque as chaves de produção são novas. Depois, "Enviar e-mail de teste".
9. [ ] **Primeiro backup** pela tarefa agendada (rodar à mão) e conferir o zip (boletins e chaves dentro).
10. [ ] Fumaça com o responsável: login → abrir uma amostra migrada → 2ª via de um boletim migrado → receber uma amostra nova → etiqueta na Argox.
11. [ ] Entregar a senha provisória a cada usuário (pessoalmente); cada um troca no 1º acesso.
12. [ ] Delphi: tirar o atalho das estações; Firebird **somente leitura** para consulta histórica por 6 meses (`gfix -mode read_only`).

## D. Depois da virada
- [ ] 1º dia acompanhado no laboratório; ocorrências na tabela do [roteiro de homologação](ROTEIRO-HOMOLOGACAO.md#registro-de-ocorrências).
- [ ] 1ª semana: conferir diariamente que o backup rodou (zip novo no destino e na cópia externa).
- [ ] **Atualizações do sistema:** gerar o `setup.exe` novo e executá-lo com o backup **em branco**. O banco é mantido e só os arquivos são trocados. Mudança de banco ainda é manual.
- [ ] Depois de 6 meses: desligar o Firebird (o `.fbk` final continua guardado).
- [ ] Marcar a 9.3 e a definição de pronto da [F9](F9-homologacao-virada.md#definição-de-pronto) no plano.
