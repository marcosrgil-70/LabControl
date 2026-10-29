# T05 — Parâmetros da Empresa (antigo "Parâmetros do Sistema")

**Fase:** 1 · **Rota:** `/Empresa/Parametros` · **Permissões:** `PARAMETROS_EMPRESA.CONSULTAR/ALTERAR` · **Criticidade:** 🔴 (controla a proteção do número da amostra)
**Legado:** menu *Cadastros → Parâmetros do Sistema* = `Standards/un_Parametros.pas` + `unDM_cadParametros`, que mostrava/editava `ULT_COD_PROPOSTA`, `ULT_COD_AMOSTRA`, `ULT_PED_VENDA`, `ULT_COD_BOLETIM`, `ULT_COD_RE`. Os IDs de status ficavam na tabela sem tela (valores do legado: `ULT_COD_PROPOSTA = 1799`, `ULT_COD_AMOSTRA = 1402`, `ULT_PED_VENDA = 1471`, `ID_AMOSTRAS_STATUS_RESULTADOS = 1`, `ID_LAB_PROPOSTAS_STATUS_ENVCLI = NULL`).
**Objetos do banco:** [`EMPRESAS_PARAMETROS`](../banco/objetos/tabelas/EMPRESAS_PARAMETROS.md), [`SEQUENCIAS_DOCUMENTOS`](../banco/objetos/tabelas/SEQUENCIAS_DOCUMENTOS.md), [`AUDITORIA`](../banco/objetos/tabelas/AUDITORIA.md)
**Processos:** [P03 Numeração](../processos/P03-numeracao-documentos.md) · [P04 Número da amostra](../processos/P04-numero-amostra-ano.md) · [P05 Ciclo da proposta](../processos/P05-ciclo-proposta.md) · [P17 Emissão do boletim](../processos/P17-emissao-boletim.md) · **Regras:** [RN-01](../regras/RN-01-gerais-multiempresa.md)

## Aba "Parâmetros" (`EMPRESAS_PARAMETROS`)
| Campo | Coluna | Padrão | Uso |
|---|---|---|---|
| Status inicial da proposta | `ID_LAB_PROPOSTAS_STATUS_INICIAL` | Em elaboração | nova proposta e nova revisão (`PR_REVISAO_PROPOSTA`) |
| Status "Enviado ao cliente" | `ID_LAB_PROPOSTAS_STATUS_ENVCLI` | Enviado ao cliente | `TG_PROPOSTAS_BU` grava `DT_ENVIO_CLIENTE` ao entrar nesse status |
| Status inicial da amostra | `ID_AMOSTRAS_STATUS_INICIAL` | Liberada para análise | recebimento (T22) |
| Status que permite resultados (legado) | `ID_AMOSTRAS_STATUS_RESULTADOS` | Liberada | informativo; no v2 vale a flag `AMOSTRAS_STATUS.PERMITE_RESULTADO` |
| Status inicial do boletim | `ID_BOLETINS_STATUS_INICIAL` | Em elaboração | boletim novo e revisão |
| Status do boletim emitido | `ID_BOLETINS_STATUS_EMITIDO` | Enviado ao cliente | `SP_EMITIR_BOLETIM` |
| **Dias retroativos para a data de entrega** | `DIAS_RETROATIVOS_ENTREGA` | **7** | `TG_AMOSTRAS_BI`: data mais antiga exige confirmação + permissão |
| Validade padrão da proposta (dias) | `DIAS_VALIDADE_PROPOSTA` | 30 | sugere `DT_VALIDADE` |
| Período padrão de pesquisa (dias) | `DIAS_PESQUISA_PADRAO` | 365 | "exibir a partir de" das consultas |
| Exige 2º responsável no boletim | `EXIGE_RESP2_BOLETIM` | não | `SP_EMITIR_BOLETIM` |
| Legenda padrão do boletim | `TEXTO_PADRAO_LEGENDA` | — | sugerida no boletim novo |
| Contrato padrão da proposta | `TEXTO_PADRAO_CONTRATO` | — | botão "Usar contrato padrão" (T20) |

Validações: dias ≥ 0 (`DIAS_RETROATIVOS_ENTREGA` entre 0 e 365, com aviso se > 30); combos só com status **da empresa**; alteração grava `AUDITORIA`.

## Aba "Numeração" (`SEQUENCIAS_DOCUMENTOS`) — substitui os `ULT_*` do legado
- Grid somente leitura: tipo (Amostra, Proposta, Pedido de venda, Produto, Relatório de ensaio), ano (0 = não anual), último número, data da última alteração.
- **Ajustar** (só ADMIN_GLOBAL): novo valor + motivo obrigatório. Recusar valor **menor** que o maior número já usado (`MAX(COD_AMOSTRA)` do ano, `MAX(COD_PROPOSTA)` do ano, `MAX(NR_PEDIDO)`, maior código numérico de produto). Grava `AUDITORIA` com antes/depois.
- Explicação na tela: "A numeração reinicia sozinha a cada ano (linha nova por ano). Não é preciso zerar nada na virada do ano."

## Erros do legado a não repetir
- Editar `ULT_COD_AMOSTRA` livremente na tela podia **duplicar** números (não havia `UNIQUE`).
- `ULT_COD_BOLETIM` e `ULT_COD_RE` eram mostrados, mas nunca usados (o boletim usa o número da amostra; o RE gravava o ID da empresa).
- `ID_LAB_PROPOSTAS_STATUS_ENVCLI` nulo: a data de envio ao cliente nunca era gravada automaticamente.

## Critérios de aceite
- [x] Mudar `DIAS_RETROATIVOS_ENTREGA` altera imediatamente a regra do recebimento.
- [x] Ajuste de numeração para valor menor é recusado; ajuste válido aparece na Auditoria.
- [x] Combos mostram só status da empresa ativa.

## Implementação (tarefa 1.6)
- `Controllers/EmpresaController.cs` (`/Empresa/Parametros`: `PARAMETROS_EMPRESA.CONSULTAR` para ver, `PARAMETROS_EMPRESA.ALTERAR` para gravar; `/Empresa/AjustarNumeracao` só ADMIN_GLOBAL — `[SomenteAdminGlobal]`) · `Services/ParametrosEmpresaService.cs` · `ViewModels/ParametrosEmpresaVM.cs` · `Views/Empresa/Parametros.cshtml` (abas Parâmetros/Numeração, modal de ajuste).
- **Parâmetros:** combos só com status da empresa ativa (ativos + o já escolhido, marcado "(inativo)"); obrigatórios todos os status, exceto "permite resultados (legado)"; "Enviado ao cliente" ≠ inicial da proposta e boletim emitido ≠ inicial; dias retroativos 0–365 (aviso na tela acima de 30), validade/pesquisa 0–3650. Gravação pelo EF → auditoria automática (`EMPRESAS_PARAMETROS`, só colunas alteradas). Sem cache: as triggers leem a tabela a cada gravação, então a mudança vale na hora.
- **Numeração:** grid de `SEQUENCIAS_DOCUMENTOS` da empresa ativa (ano 0 = não anual). Ajuste numa transação: `SELECT … FOR UPDATE` na linha (mesma trava do `SP_PROXIMO_NUMERO`); recusa valor igual ao atual ou menor que o maior já usado (`MAX(COD_AMOSTRA)`/`MAX(COD_PROPOSTA)`/`MAX(NR_RELATORIO)` do ano, `MAX(NR_PEDIDO)`, maior `PRODUTOS.CODIGO` numérico); motivo de 10 a 500 caracteres; `UPDATE` direto + evento `A` em `AUDITORIA` (`ID_REGISTRO` = `empresa-tipo-ano`; antes: `ULTIMO_NUMERO`; depois: `ULTIMO_NUMERO`, `MAIOR_USADO`, `MOTIVO`). Só linhas existentes são ajustáveis (o ano novo nasce sozinho; os máximos do legado entram pela M-18).
- Aceite em `ParametrosEmpresaServiceTests` (4 testes).

## Aba "E-mail" (tarefa 8.3, 29/10/2026 — pedido do usuário)
Configuração do envio de boletins por e-mail **da empresa ativa**, editável pelo usuário (`PARAMETROS_EMPRESA.ALTERAR`), em `EMPRESAS_PARAMETROS` (D-26):
- **Servidor de saída:** servidor SMTP (vazio = envio desligado), porta (padrão 587), segurança (Nenhuma, SSL/TLS direto — 465, STARTTLS — 587), usuário e senha. A senha é **cifrada** (ASP.NET Data Protection), nunca volta para a tela ("em branco mantém a gravada"; "Apagar a senha gravada") e aparece como `***` na auditoria.
- **Remetente:** e-mail e nome do campo "De:", e cópia oculta opcional de todo boletim enviado.
- **Modelo do e-mail do boletim:** assunto e texto com os marcadores `{boletim}` `{amostra}` `{cliente}` `{empresa}` (vazio = o padrão).
- **"Enviar e-mail de teste"** com a configuração gravada; falha de conexão/autenticação aparece com a mensagem do servidor.
- Se as chaves do Data Protection do servidor mudarem (outra máquina), o envio pede a senha de novo em vez de dar erro. As chaves ficam em `DataProtection:PastaChaves` (padrão `storage/chaves`) e entram no backup (`Scripts/v2/backup.ps1`).
- Arquivos: `Services/Email/ConfiguracaoEmailService.cs`, `Views/Empresa/_Email.cshtml`, actions `Email` e `TestarEmail` do `EmpresaController`. Testes: `EmailTests.Configuracao_valida_cifra_a_senha_e_a_auditoria_nao_mostra` e o de navegador `Email_configurado_na_empresa_envia_o_boletim_emitido`.

**Status:** ✅ concluída na tarefa 1.6 (20/10/2026); aba E-mail na 8.3 (29/10/2026).

> **Teste no navegador (20/10/2026, [P26](../processos/P26-testes-ponta-a-ponta.md)):** `Empresa_dados_contatos_e_parametros` — alterar os dias retroativos e conferir o valor gravado.
