# Fase 9 — Homologação e Virada

**Objetivo:** migração completa e conferida, homologação com o laboratório e troca do Delphi pelo sistema novo.
**Modelo/modo:** 🔵 Opus · ✋ modo padrão.
**Ler antes:** [P24](../processos/P24-migracao.md) · [doc 13 §3–§4](../banco/13-MIGRACAO-DADOS.md) · [matriz Delphi → C#](MATRIZ-DELPHI-CSHARP.md)
**Depende de:** Fases 0–8.

---

### Antes de começar (situação em 29/10/2026)
Fases 0–8 concluídas, menos a 5.4 (layout do boletim, aguarda os 3 boletins reais). Decidir/fazer antes da 9.1, porque mudam o que a migração grava ou o ambiente:
- **Pendência 36:** 9 resultados do legado com o ano digitado errado (0171, 0201, 1971, 2107, 2201) — corrigir na M-16, como as 63 datas invertidas, ou depois pela T26.
- **Pendência 34:** descarte por reprovação de amostra com boletim emitido (liberar no banco só a troca do status?).
- **Pendência 33:** período padrão das pesquisas com os dados migrados (2013–2018).
- **Pendência 37:** atualizar o `MySqlConnector` (aviso de vulnerabilidade NU1902) e rodar a suíte inteira e a migração.
- **Pendência 27:** pasta dos boletins (`Boletins:Pasta`) e **chaves do Data Protection** guardadas no servidor e no backup.
- Na 9.2, junto com o roteiro: pendências 21 (Argox), 22 e 28 (testes guiados), 38 (teste real do e-mail) e a 5.4 (boletins reais).

### 9.1 Migração completa em banco limpo
> ✅ **Concluída em 29/10/2026** — antes: decisões 33 (pesquisas pelo parâmetro da empresa), 34 (só o status em amostra com boletim emitido, D-27) e 36 (ano digitado errado corrigido na M-16), e a 37 (`MySqlConnector` 2.6.2). Cópia congelada do legado `C:\Project\backup\DBMAIS-20261029.fbk` (gbak); `labcontrol_migracao` recriado pelos scripts e migrado pela T07: 18 etapas ok, **74/74 conferências**, 695 anomalias no relatório `C:\Project\backup\migracao-9.1-20261029.csv` — revisão com o laboratório na 9.2 (pendência 39). Na virada (9.3) a migração é repetida com os dados do dia.

- **Passos:** recriar `labcontrol_v2`; congelar o Firebird (cópia do `DBMAIS.FDB` da data); executar M-01 … M-18; todas as conferências do doc 13 §3; relatório de anomalias revisado com o usuário.
- **Aceite:** 100% das conferências verdes ou justificadas por escrito.
- **Esforço:** M

### 9.2 Homologação com os usuários do laboratório
> 📋 **Roteiro pronto (29/10/2026):** [ROTEIRO-HOMOLOGACAO.md](ROTEIRO-HOMOLOGACAO.md) — preparação, 13 blocos com o resultado esperado de cada passo (inclui as pendências 21, 22, 28, 38, 39 e os modelos da 5.4) e a tabela de ocorrências. Aguarda o laboratório.

- **Roteiro guiado** (um dia de uso paralelo): login/troca de senha → proposta nova → pedido de venda → receber 3 amostras (uma com data retroativa autorizada) → etiquetas → movimentação/arquivo → lançar resultados → montar e emitir boletim → revisar amostra (cascata) → reemitir → relatórios do mês.
- Comparar 10 boletins migrados (2ª via) com os impressos do legado.
- Registrar ajustes; corrigir; repetir o roteiro.
- **Esforço:** M

### 9.3 Virada
> 📋 **Preparação feita (29/10/2026):**
> - revisão de segurança: XSS no texto rico corrigido e cabeçalhos de segurança adicionados;
> - pendência 27: chaves do Data Protection em `DataProtection:PastaChaves`;
> - [`Scripts/v2/backup.ps1`](../../Scripts/v2/README.md#backup-banco--boletins--chaves) (banco + boletins + chaves, com conferência por restauração);
> - **[CHECKLIST-VIRADA.md](CHECKLIST-VIRADA.md)** (servidor, backup agendado, passo a passo do dia, depois da virada).
>
> - **[Instalador do servidor](../../instalador/README.md)** (29/10/2026): `setup.exe` offline que instala IIS, .NET, MySQL, restaura o banco migrado, configura HTTPS e o backup agendado. Testar numa VM antes (pendência 40).
>
> Falta a virada em si, que depende da 9.2 e da 5.4.

- **Passos:** `/security-review`; backup final do Firebird e do MySQL (`mysqldump --routines --triggers`); última migração com os dados do dia; apontar `appsettings` (produção) para `labcontrol_v2`; publicar; criar senhas provisórias e entregar aos usuários; desligar o LabControl.exe (manter o Firebird somente leitura para consulta histórica por 6 meses).
- **Plano de retorno:** se houver bloqueio grave no 1º dia, voltar ao Delphi com o Firebird intacto (o v2 não escreve no Firebird).
- **Esforço:** S

## Definição de pronto
- [ ] Laboratório operando só no sistema novo; Delphi desligado; backups guardados.
