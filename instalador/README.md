# Instalador do LabControl v2 (servidor Windows)

`LabControl-Setup-<versão>.exe` instala no servidor do laboratório o sistema e tudo de que ele precisa, **sem internet**. Decisões do usuário em 29/10/2026: `setup.exe` com assistente, pacote offline, IIS e certificado autoassinado.

## Gerar o instalador (máquina de desenvolvimento)

```powershell
.\instalador\construir.ps1              # versão 2.0.<nº de commits>
.\instalador\construir.ps1 -Versao 2.1.0
```

O script faz quatro coisas, nesta ordem:
1. Confere os pré-requisitos de [`prerequisitos.json`](prerequisitos.json) pelo SHA-256. Se faltar algum, baixa da URL oficial.
2. Publica o app (`dotnet publish -c Release`), sem `appsettings.Development.json` e sem `storage/`.
3. Confere a sintaxe e o BOM UTF-8 dos scripts PowerShell. O PowerShell 5.1 lê um arquivo sem BOM como ANSI e estraga os acentos.
4. Compila o [`LabControl.iss`](LabControl.iss) com o **Inno Setup 6** (`winget install JRSoftware.InnoSetup`).

Saída: `instalador\saida\LabControl-Setup-<versão>.exe` (~415 MB). Os pré-requisitos e a saída **não vão para o git**.

| Pré-requisito | Versão | Por quê |
|---|---|---|
| .NET Hosting Bundle | 10.0.14 (a do desenvolvimento) | runtime do ASP.NET Core + módulo do IIS |
| MySQL Community Server (ZIP) | 8.0.46 (a do desenvolvimento) | banco; instalado como serviço `MySQL80`, só em `127.0.0.1` |
| Visual C++ 2015-2022 x64 | a atual | exigido pelo MySQL |

Para trocar uma versão, atualize `prerequisitos.json` (arquivo, URL e SHA-256), apague o arquivo antigo de `prerequisitos\` e gere de novo.

## Instalar (no servidor)

**Requisitos:** Windows Server 2016 ou mais novo (ou Windows 10/11), 64 bits, e um usuário administrador.

### Primeira instalação

1. Gere o backup do banco migrado na máquina da migração:
   ```powershell
   .\Scripts\v2\backup.ps1 -Destino <pasta> -Banco <banco migrado> -Conferir
   ```
2. Leve o `.zip` do backup e o `setup.exe` ao servidor.
3. Execute o `setup.exe`. O assistente pede:
   - **pasta do sistema** (padrão `C:\LabControl`);
   - **pasta dos dados** (padrão `C:\LabControlDados`): boletins, chaves de criptografia, backups, logs e certificado;
   - **nome do servidor e porta HTTPS** (padrão: o nome do computador e a porta 443);
   - **senha do root do MySQL**: nova, se o MySQL for instalado agora; a atual, se já existir. **Guarde essa senha.**
   - **backup `.zip` do banco migrado.** A opção "restaurar também as chaves" serve só para recuperar este mesmo servidor;
   - **senha provisória do ADMINISTRADOR**, com troca obrigatória no 1º acesso. Assim o `Admin123` do desenvolvimento nunca chega à produção (regra do CLAUDE.md).
4. Uma janela do PowerShell mostra as 9 etapas do [`scripts/Instalar.ps1`](scripts/Instalar.ps1):
   1. IIS;
   2. VC++ e .NET;
   3. MySQL;
   4. restauração do banco e conferência dos objetos com as contagens do `recriar.ps1`;
   5. usuários `labcontrol` (app) e `labcontrol_backup`, com permissões mínimas e senhas internas aleatórias;
   6. pool do IIS e pastas com acesso restrito;
   7. arquivos do sistema;
   8. certificado, site HTTPS e firewall;
   9. tarefa diária de backup às 22h e teste da tela de login.
5. Ao final:
   - **`C:\LabControlDados\LEIA-ME.txt`** tem o endereço, as pastas e o que fazer nas estações;
   - um atalho "LabControl" fica na área de trabalho do servidor.
6. **Estações:** copie a pasta `C:\LabControlDados\certificado` e execute `instalar-certificado-na-estacao.cmd` como administrador, uma vez por estação. Sem isso, o navegador avisa "não seguro".

### Atualizar o sistema

Execute o `setup.exe` da versão nova e deixe o backup **em branco**:
- o banco `labcontrol_v2` é mantido;
- os arquivos do sistema são trocados com o pool parado;
- as senhas internas são renovadas.

Se a versão nova mudar o banco (`Scripts/v2`), o instalador ainda **não** aplica a mudança: siga o que estiver no Diário da versão.

### O que fica onde

| Item | Onde |
|---|---|
| Sistema publicado | `C:\LabControl\site` (Administradores, SYSTEM e o pool com leitura) |
| Scripts (instalar, backup, desinstalar) | `C:\LabControl\scripts` |
| Boletins emitidos / chaves | `C:\LabControlDados\boletins`, `\chaves` (Administradores, SYSTEM e o pool do IIS) |
| Backups diários (30 dias) e log | `C:\LabControlDados\backup`, `\logs\backup-ultimo.log` |
| Senha do usuário de backup | `C:\LabControlDados\config\backup.senha`, cifrada pelo DPAPI da máquina |
| Connection string do app | `web.config` do site (variável de ambiente do ANCM), legível só pelo pool e pelos administradores |
| Log da instalação | `C:\LabControlDados\logs\instalacao-<data>.log` (sem senhas) |
| MySQL | serviço `MySQL80`, `C:\Program Files\MySQL\MySQL Server 8.0`, dados em `C:\ProgramData\MySQL\MySQL Server 8.0\Data` |

**Pool do IIS:** sem reciclagem periódica e sem desligar por ociosidade. A sessão vale só para a execução do app (pendência 16). Sem esse ajuste, os usuários voltariam para o login no meio do dia.

### Desinstalar
"Adicionar ou remover programas" › LabControl.
- **Remove:** o site, o pool, a tarefa de backup e as regras do firewall.
- **Mantém:** o MySQL, o banco, a pasta de dados e o certificado. Os dados do laboratório nunca são apagados pelo desinstalador.

### Se algo falhar
- O assistente mostra onde está o log.
- Depois de corrigir a causa, **execute o setup de novo**: cada etapa confere o que já está pronto.
- Se a restauração falhar no meio, o banco criado é apagado, para a próxima tentativa começar limpa.

## O que foi testado (29/10/2026)

O instalador inteiro **ainda não rodou num servidor limpo**: a máquina de desenvolvimento é a do usuário, e instalar o IIS e o serviço do MySQL nela mudaria o ambiente. Faça o primeiro teste numa máquina virtual com Windows Server antes do servidor do cliente.

Testado por partes:
- **Hash da senha provisória:** gerado em PowerShell, é aceito pelo `PasswordHasher` do ASP.NET (`Success`, sem recálculo).
- **Restauração (`RestaurarBanco`):**
  - o backup do `labcontrol_migracao` foi restaurado em outro nome e conferido: 59 tabelas, 10 views, 8 funções, 27 procedures, 48 triggers e o boletim;
  - um zip inválido é recusado e o banco criado pela metade é apagado.
- **App publicado em produção com o usuário MySQL de permissões mínimas:**
  - login com a senha provisória, troca obrigatória da senha e escolha da empresa;
  - 20 telas, 3 relatórios de 2017, comprovante e PDF da 2ª via;
  - revisão da proposta (procedure) e revisão da amostra em cascata (amostra, resultado e boletim → R01), tudo auditado.
- **Backup com o usuário de backup mínimo:** já testado na 9.3.
