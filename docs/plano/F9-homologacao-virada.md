# Fase 9 — Homologação e Virada

**Objetivo:** migração completa e conferida, homologação com o laboratório e troca do Delphi pelo sistema novo.
**Modelo/modo:** 🔵 Opus · ✋ modo padrão.
**Ler antes:** [P24](../processos/P24-migracao.md) · [doc 13 §3–§4](../banco/13-MIGRACAO-DADOS.md) · [matriz Delphi → C#](MATRIZ-DELPHI-CSHARP.md)
**Depende de:** Fases 0–8.

---

### 9.1 Migração completa em banco limpo
- **Passos:** recriar `labcontrol_v2`; congelar o Firebird (cópia do `DBMAIS.FDB` da data); executar M-01 … M-18; todas as conferências do doc 13 §3; relatório de anomalias revisado com o usuário.
- **Aceite:** 100% das conferências verdes ou justificadas por escrito.
- **Esforço:** M

### 9.2 Homologação com os usuários do laboratório
- **Roteiro guiado** (um dia de uso paralelo): login/troca de senha → proposta nova → pedido de venda → receber 3 amostras (uma com data retroativa autorizada) → etiquetas → movimentação/arquivo → lançar resultados → montar e emitir boletim → revisar amostra (cascata) → reemitir → relatórios do mês.
- Comparar 10 boletins migrados (2ª via) com os impressos do legado.
- Registrar ajustes; corrigir; repetir o roteiro.
- **Esforço:** M

### 9.3 Virada
- **Passos:** `/security-review`; backup final do Firebird e do MySQL (`mysqldump --routines --triggers`); última migração com os dados do dia; apontar `appsettings` (produção) para `labcontrol_v2`; publicar; criar senhas provisórias e entregar aos usuários; desligar o LabControl.exe (manter o Firebird somente leitura para consulta histórica por 6 meses).
- **Plano de retorno:** se houver bloqueio grave no 1º dia, voltar ao Delphi com o Firebird intacto (o v2 não escreve no Firebird).
- **Esforço:** S

## Definição de pronto
- [ ] Laboratório operando só no sistema novo; Delphi desligado; backups guardados.
