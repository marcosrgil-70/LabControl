# Instalador do LabControl v2 (servidor Windows)

`LabControl-Setup-<versão>.exe` (~43 MB) instala o sistema no servidor do laboratório:
- confere o que o servidor já tem e **baixa e instala o que faltar**: IIS, Visual C++, .NET 10 e MySQL 8.0;
- cria o banco **`LabControlDB`** zerado, ou restaura um backup;
- configura o site HTTPS e o backup diário.

Decisões do usuário (29/10/2026):
- `setup.exe` com assistente, IIS e certificado autoassinado;
- o assistente pede a empresa e as pastas do sistema e dos boletins;
- componentes baixados dentro do setup;
- **ADMINISTRADOR / Admin123 fixo, sem troca obrigatória** (registrado no CLAUDE.md);
- banco zerado `LabControlDB`, sem bancos auxiliares. O legado entra pela **migração do próprio sistema** (T07), no mesmo banco.

## Gerar o instalador (máquina de desenvolvimento)

```powershell
.\instalador\construir.ps1              # versão 2.0.<nº de commits>
.\instalador\construir.ps1 -Offline     # + instalador\saida\prerequisitos (kit para servidor sem internet)
.\instalador\construir.ps1 -Versao 2.1.0
```

O script faz quatro coisas, nesta ordem:
1. Publica o app (`dotnet publish -c Release`), sem `appsettings.Development.json` e sem `storage/`.
2. Confere a sintaxe e o BOM UTF-8 dos scripts PowerShell e do `.iss`. O PowerShell 5.1 e o compilador do Inno leem arquivo sem BOM como ANSI, e os acentos estragariam.
3. Com `-Offline`, baixa os pré-requisitos e confere o SHA-256 de cada um. Depois copia todos para `saida\prerequisitos`.
4. Compila o [`LabControl.iss`](LabControl.iss) com o **Inno Setup 6.1+** (`winget install JRSoftware.InnoSetup`), passando os pré-requisitos de [`prerequisitos.json`](prerequisitos.json) por `/D`: nome, versão, URL, URL alternativa e SHA-256.

A saída e o cache dos pré-requisitos **não vão para o git**.

**Ícone:** `LabControl.ico` é o frasco da barra do sistema, no azul da barra. Aparece:
- no `setup.exe`;
- no atalho da área de trabalho;
- em "Adicionar ou remover programas";
- na aba do navegador (`wwwroot/favicon.ico`).

Ele é gerado por [`gerar-icone.ps1`](gerar-icone.ps1). Só precisa rodar de novo se o desenho mudar.

| Pré-requisito | Versão | Como o setup confere |
|---|---|---|
| IIS | do Windows | registro `InetStp` (ativado como recurso do Windows, sem download) |
| Visual C++ 2015-2022 x64 | a atual | registro `VC\Runtimes\x64`. Vem de um link da Microsoft que sempre aponta para a versão atual, por isso o setup não fixa o hash. Em troca, o `Instalar.ps1` exige **assinatura digital válida da Microsoft** em todo `.exe` baixado |
| .NET 10 Hosting Bundle | 10.0.14 ou mais novo | pasta do runtime `Microsoft.AspNetCore.App\10.*` + módulo do IIS; download conferido pelo SHA-256 |
| MySQL Community Server (ZIP) | 8.0.46 | algum serviço `MySQL*` com o `mysqld`. Download conferido pelo SHA-256. Se a URL principal sumir (versão antiga sai do CDN), o setup tenta a URL do arquivo de versões antigas |

Para trocar uma versão:
1. Atualize `prerequisitos.json`: arquivo, URL, SHA-256 e versão.
2. Apague o arquivo antigo de `instalador\prerequisitos\`.
3. Gere de novo.

## Instalar (no servidor)

**Requisitos:** Windows Server 2016 ou mais novo (ou Windows 10/11), 64 bits, e um usuário administrador. Para baixar os componentes, o servidor precisa de internet. Sem internet, copie a pasta `prerequisitos` (gerada com `-Offline`) para **ao lado do setup**: o setup usa os arquivos de lá.

### O assistente

1. **Empresa:** o nome (razão social) do laboratório. Num banco novo, ele vira a empresa 001.
2. **Pasta do sistema** (padrão `C:\LabControl`).
3. **Pastas de dados:**
   - **boletins**: padrão `C:\LabControlDados\Boletins`;
   - **dados**: backups, chaves de criptografia, logs e certificado; padrão `C:\LabControlDados`.
4. **Componentes:** mostra o que já existe (✔) e o que falta (✘), com a opção "baixar e instalar" marcada. Desmarcar um componente que falta impede de continuar, porque todos são necessários.
5. **Senha do root do MySQL:**
   - se o MySQL for instalado agora, é a senha nova (mínimo de 8 caracteres) — **guarde-a**;
   - se ele já existir, é a senha atual.
6. **Banco**, sempre chamado `LabControlDB` (o MySQL no Windows guarda `labcontroldb`):
   - **zerado:** criado pelos scripts, com a empresa digitada e o usuário **ADMINISTRADOR / Admin123**;
   - **restaurar backup (.zip):** gerado pelo `backup.ps1` ou pela tarefa diária. Ficam os usuários e senhas do backup.
   - **Se o `LabControlDB` já existir** (pedido do usuário, 30/10/2026), o assistente confere a senha do root e pergunta antes:
     - **Atualizar o banco existente** (recomendado): mantém todos os dados e acrescenta só o que falta nesta versão, pelo [`atualizar.ps1`](../Scripts/v2/README.md#atualizar-um-banco-existente-sem-apagar-dados);
     - **Apagar o banco e criar um novo:** pede confirmação; depois escolhe zerado ou restaurar backup.
     - Nos dois casos, antes de mexer, uma cópia do banco (`mysqldump`) vai para a pasta de backup: `labcontroldb-antes-da-instalacao-<data>.sql`.
   - Backup de uma versão anterior é atualizado depois de restaurado (mesmo `atualizar.ps1`).
7. **Endereço:** nome do servidor na rede e porta HTTPS (443).
8. **Download:** baixa o que faltar e abre a janela do PowerShell com as 9 etapas do [`scripts/Instalar.ps1`](scripts/Instalar.ps1):
   1. IIS;
   2. VC++ e .NET (só os que faltam; assinatura conferida);
   3. MySQL como serviço `MySQL80`, só em 127.0.0.1;
   4. banco;
   5. usuários `labcontrol` (app) e `labcontrol_backup`, com permissões mínimas e senhas internas aleatórias;
   6. pool do IIS e pastas com acesso restrito;
   7. arquivos do sistema;
   8. certificado, site HTTPS e firewall;
   9. backup diário às 22h e teste da tela de login.

Ao final:
- o **`LEIA-ME.txt`** na pasta dos dados tem o endereço, as pastas e o que fazer nas estações;
- um atalho "LabControl" fica na área de trabalho do servidor.

### Primeiro acesso e migração do sistema antigo

1. Abra o endereço e entre com **ADMINISTRADOR / Admin123**.
2. *Sistema › Migração* (T07):
   - **banco destino:** já vem com o próprio banco (`labcontroldb`);
   - caminho do `.FDB` do sistema antigo;
   - senha do Firebird;
   - senha provisória dos outros usuários.
3. A migração **substitui** a empresa e o ADMINISTRADOR da instalação pelos dados do legado. Tudo roda numa transação só: se falhar, nada fica gravado, e o banco volta como o instalador deixou. Corrija e execute de novo.
4. Ao final, entre de novo com **ADMINISTRADOR / Admin123**. O ADMINISTRADOR do legado fica com essa senha, sem troca. Os outros usuários entram com a senha provisória e trocam no 1º acesso.

Requisitos e limites:
- O **Firebird do sistema antigo precisa estar rodando neste servidor** (`localhost:3050`), como no Delphi.
- A T07 só aceita o banco em uso se ele estiver recém-instalado: nenhum cadastro além da empresa e do ADMINISTRADOR, e nenhuma proposta ou amostra. Depois de usar o sistema, a migração no mesmo banco é recusada.

**Estações:** copie a pasta `certificado` (na pasta dos dados) e execute `instalar-certificado-na-estacao.cmd` como administrador, uma vez por estação. Sem isso, o navegador avisa "não seguro".

### Atualizar o sistema

Execute o `setup.exe` da versão nova:
- o `LabControlDB` existente é **atualizado** (opção recomendada no assistente) — dados mantidos, cópia antes na pasta de backup;
- os arquivos do sistema são trocados com o pool parado;
- as senhas internas são renovadas;
- os componentes que já estão instalados não são baixados de novo.

Se a versão nova mudar o banco (`Scripts/v2`), a atualização aplica o que falta (tabelas, colunas, índices, rotinas, triggers, views e permissões). Coluna com definição diferente que não caiba nos dados para a instalação com o erro e a cópia de antes na pasta de backup.

### O que fica onde

| Item | Onde |
|---|---|
| Sistema publicado | `C:\LabControl\site` (Administradores, SYSTEM e o pool com leitura) |
| Scripts (instalar, backup, desinstalar, banco) | `C:\LabControl\scripts` |
| Boletins emitidos | pasta escolhida (padrão `C:\LabControlDados\Boletins`). Se a pasta já existia fora dos dados, o instalador só acrescenta a gravação do pool, sem tirar acessos |
| Chaves / backups (30 dias) / logs / certificado | `C:\LabControlDados\chaves`, `\backup`, `\logs`, `\certificado` (Administradores, SYSTEM e o pool nas chaves) |
| Senha do usuário de backup | `C:\LabControlDados\config\backup.senha`, cifrada pelo DPAPI da máquina |
| Connection string do app | `web.config` do site (variável de ambiente do ANCM), legível só pelo pool e pelos administradores |
| Log da instalação | `C:\LabControlDados\logs\instalacao-<data>.log` (sem senhas) |
| MySQL | serviço `MySQL80`, `C:\Program Files\MySQL\MySQL Server 8.0`, dados em `C:\ProgramData\MySQL\MySQL Server 8.0\Data` |

**Pool do IIS:** sem reciclagem periódica e sem desligar por ociosidade. A sessão vale só para a execução do app (pendência 16): sem esse ajuste, os usuários voltariam para o login no meio do dia.

### Desinstalar
"Adicionar ou remover programas" › LabControl.
- **Remove:** o site, o pool, a tarefa de backup e as regras do firewall.
- **Mantém:** o MySQL, o banco, as pastas dos boletins e dos dados e o certificado.

### Se algo falhar
- O assistente mostra onde está o log.
- Corrija a causa e **execute o setup de novo**: cada etapa confere o que já está pronto.
- Uma restauração que falha apaga o banco criado pela metade.
- **O IIS não ativa** ("Não foi possível ler a chave do Registro de configuração"): o setup tenta de novo pelo `dism.exe`. Se ainda falhar, quase sempre o Windows tem um reinício pendente ou uma atualização em andamento:
  1. Reinicie o computador e execute o setup de novo.
  2. Se persistir, ative à mão em *Ativar ou desativar recursos do Windows*: "Serviços de Informações da Internet" e, dentro de "Ferramentas de Gerenciamento da Web", também "Scripts e Ferramentas de Gerenciamento do IIS". Depois execute o setup de novo: com o IIS já ativado, ele pula essa parte.

  Os detalhes do Windows ficam em `C:\Windows\Logs\DISM\dism.log`.

## O que foi testado (30/10/2026)

**Teste real do usuário, num Windows 10 (29–30/10/2026):**

| Versão | O que aconteceu | Correção |
|---|---|---|
| 2.0.111 | etapa 1: `Enable-WindowsOptionalFeature` falhou com "Não foi possível ler a chave do Registro de configuração" | 2.0.112: tenta pelo `dism.exe` |
| 2.0.112 | o `dism.exe` também falhou (código 50), com reinício pendente do Windows | 2.0.115: se o IIS já estiver ativo (ativado à mão), não tenta de novo; a mensagem ensina a ativação à mão |
| 2.0.115 | etapas 1 a 3 ok (IIS, VC++ e .NET já presentes; MySQL 8.0.46 baixado e instalado como serviço); etapa 4 parou com "Não há suporte para o formato do caminho dado" | 2.0.116: `@($pastas.Values)`, porque o PowerShell 5.1 juntava as pastas num texto só |
| 2.0.116 | instalação concluída; a Migração (T07) deu erro com a cópia nova do legado | 2.0.117: correções na migração (ver [P24](../docs/processos/P24-migracao.md)) e ícone |

Com o 2.0.116, a instalação foi até o fim e o usuário entrou no sistema como ADMINISTRADOR. **Falta:** executar o 2.0.117 nesse computador (atualização, mantém o banco), repetir a T07 e conferir os dados migrados.

O instalador inteiro **ainda não rodou num servidor limpo**: a máquina de desenvolvimento é a do usuário, e instalar o IIS e o serviço do MySQL nela mudaria o ambiente. Teste numa máquina virtual com Windows Server antes do servidor do cliente (pendência 40).

Testado por partes:
- **Banco novo + migração no próprio banco** (`MigracaoTests.Instalacao_nova_migra_no_proprio_banco_e_falha_desfaz_tudo`):
  - o banco foi criado pelo `recriar.ps1 -Empresa`, igual ao do instalador;
  - uma migração interrompida no meio não deixa nada gravado;
  - a migração completa teve todas as conferências verdes, os mesmos números do banco separado e o ADMINISTRADOR entrando com Admin123, sem troca.
- **Restauração** (`RestaurarBanco`): o backup do `labcontrol_migracao` foi restaurado e conferido (59 tabelas, 10 views, 8 funções, 27 procedures, 48 triggers e o boletim). Um zip inválido é recusado e o banco parcial é apagado.
- **App publicado em produção com o usuário MySQL mínimo:**
  - login, telas, relatórios e PDF;
  - revisão de proposta e revisão de amostra em cascata.
- **Assinatura digital:** a regra aceita os instaladores reais do .NET e do VC++.
- **Compilação:** o setup compila sem erro no Inno Setup 6.7.3 e o kit `-Offline` é gerado com os hashes conferidos.
