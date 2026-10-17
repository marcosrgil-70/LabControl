# T22 — Recebimento de Amostras (Histórico de Amostras) ⚠️⚠️ (tela mais crítica)

**Fase:** 4 · **Rota:** `/Amostras` · **Permissões:** `AMOSTRAS.CONSULTAR/INCLUIR/ALTERAR/EXCLUIR/IMPRIMIR/REVISAR/ENTREGA_RETROATIVA`
**Legado:** `un_MovHistAmostras.pas` (menu *Movimentações → Histórico de Amostras*) + `undm_HistAmostras.pas` + `Layout_ImpHistAmostra.fr3`; pesquisa `un_Lab_Hist_Amostras_Pesq.pas`.
**Tabelas:** `LAB_HIST_AMOSTRAS`, `LAB_HIST_AMOSTRAS_TESTES`, `LAB_MOV_AMOSTRAS` (entrada automática), `LAB_HIST_AMOSTRAS_SALDO`, `LAB_HIST_AMOSTRAS_LOG`; view `VW_LAB_AMOSTRAS`; rotinas `SP_AMOSTRA_GERAR_TESTES`, `SP_GERAR_REVISAO_AMOSTRA`; triggers `TG_AMOSTRAS_*`.
**Pré-requisito:** usuário com profissional vinculado (T13); proposta do cliente (T20, versão mínima na fase 4).

## 1. Número da amostra — `TT-CCCCC-AA/YY [Rnn]` (junção de 3 códigos + ano)

| Parte | Origem | Observação |
|---|---|---|
| TT | `AMOSTRAS_TIPO.CODIGO` do tipo escolhido | 2 dígitos |
| CCCCC | `COD_AMOSTRA` — **sequencial por empresa que reinicia a cada ano** | 5 dígitos |
| AA | `ANALISES_TIPO.CODIGO` do tipo de análise | 2 dígitos |
| YY | `ANO_AMOSTRA` (4 dígitos no banco, 2 na tela) | **ano do servidor** |
| Rnn | `REVISAO` > 0 | " R01" |

### Proteção contra erro de digitação do ano (requisito do usuário)
1. O ano **não** vem da data de entrega: a trigger usa `YEAR(CURDATE())` do servidor e pede o número a `SP_PROXIMO_NUMERO('AMOSTRA', ano)` com bloqueio. A aplicação **não envia** ano nem código.
2. A sequência é guardada por **(empresa, ano)**: nunca é "zerada"; uma data errada **não reinicia** a numeração (no legado, uma data com o ano anterior digitada em janeiro fazia a próxima amostra voltar para 00001 e duplicar números).
3. Data de entrega:
   - **futura** → bloqueada ("Data de entrega maior que a data de hoje");
   - **mais antiga que N dias** (`DIAS_RETROATIVOS_ENTREGA`, padrão 7) → a tela pede confirmação explícita: *"A data de entrega 15/01/2025 é de 640 dias atrás. Confirme que o ANO está correto."*; só com a permissão `AMOSTRAS.ENTREGA_RETROATIVA` o serviço define `@LC_PERMITE_RETROATIVO = 1` e grava; sem permissão → recusa;
   - **ano diferente do ano atual** (ex.: 30/12 digitado em 02/01) → aviso amarelo antes de gravar, mesmo dentro dos N dias.
4. O número é mostrado como **"INCLUSÃO"** até gravar e o número definitivo aparece numa mensagem depois de gravar ("Código da Amostra: 04-01383-01/26"). Nenhum número é reservado antes (no legado a aba Observação também consumia número e gerava lacunas).
5. Mudar o tipo de amostra/análise recalcula o número exibido (TT/AA), mas só **enquanto não houver resultado**.

## 2. Aba "Informações Gerais" (campos e particularidades)

| Campo (rótulo Delphi) | Coluna | Regra |
|---|---|---|
| Cliente* | `ID_ENTIDADES` | colunas Nome + CPF/CNPJ; ao escolher mostra CNPJ/CPF, endereço (logradouro, nº, complemento, bairro, cidade, UF, país) e telefone (só leitura) e carrega **as propostas do cliente** |
| Proposta* | `ID_LAB_PROPOSTAS` | **só a última revisão** de propostas do cliente, cujo status permite amostra; mostra nº da proposta. Ao escolher: carrega os **produtos** da proposta e o **Pedido de Venda** |
| Pedido de venda | `ID_PEDIDOS_VENDA` | só leitura (vem da proposta) |
| Produto* | `ID_LAB_PROPOSTAS_PRODUTOS` | **itens da proposta** (colunas Nr item, Código, Descrição); `ID_PRODUTOS` vem do item (trigger). Ao escolher: carrega os **tipos de análise** existentes nas análises do item |
| Tipo da amostra* | `ID_AMOSTRAS_TIPO` | ativos; recalcula TT |
| Tipo de análise* | `ID_ANALISES_TIPO` | **restrito aos tipos das análises do item** da proposta; recalcula AA |
| Nº da amostra | `NR_AMOSTRA` | só leitura ("INCLUSÃO") |
| Data da entrega* | `DT_ENTREGA` | padrão hoje; regras da seção 1 |
| Hora da entrega* | `HR_ENTREGA` | padrão agora |
| Local de recebimento* | `LOCAL_RECEBIMENTO` | 50 (sugerir o último usado) |
| Tipo de embalagem* | `ID_EMBALAGENS_TIPOS` | — |
| Qtde de embalagens entregue* | `QTDE_EMBALAGENS_ENTREGUE` | > 0; gera a **entrada automática** no estoque; após movimentação manual não pode mudar |
| Lote | `NR_LOTE` | 30 |
| **Fabricação** dia / mês / ano | `FABRICACAO_DIA/MES/ANO` | **3 combos independentes** (dia 1–31, mês por extenso Janeiro…Dezembro, ano 2100…1990). Pode informar **só o ano**, **mês+ano** ou **data completa**. Dia exige mês+ano; mês exige ano; dia válido para o mês (31/02 recusado). Impressão: `dd/mm/aaaa`, `mm/aaaa` ou `aaaa` |
| **Validade** dia / mês / ano | `VALIDADE_DIA/MES/ANO` | idem (particularidade do legado mantida) |
| Nota do rótulo | `NOTA_ROTULO` | 255 |
| Aspecto | `ASPECTO_AMOSTRA` | — |
| Cor | `COR` | — |
| Outras características | `OUTRAS_CARACTERISTICAS` | — |
| Observação da verificação | `OBSERVACAO_VERIFICACAO` | impressa no boletim |
| Temperatura | `TEMPERATURA_VERIFICACAO` | °C, decimal |
| Acompanha ficha técnica / padrão analítico / CA do cliente / Enviar a outro laboratório | `ACOMPANHA_*`, `ENVIAR_OUTRO_LABORATORIO` | checkboxes; padrão desmarcado |
| Responsável* | `ID_ENTIDADES_FUNC_RESP` | profissionais ativos |
| Status da amostra* | `ID_AMOSTRAS_STATUS` | padrão = status inicial ("Liberada para análise") |
| Coletor | `NOME_COLETOR` | 60 |
| Data e hora da coleta | `DT_HR_COLETA` | ≤ data/hora da entrega |
| Revisão | `REVISAO` | só leitura |
| (oculto) Digitador | `ID_ENTIDADES_FUNC_DIG` | profissional do usuário logado |
| (oculto) Tipo de documento | `TIPO_DOCUMENTO` | do item da proposta (BOLETIM/RELATORIO); padrão BOLETIM |

## 3. Aba "Observação"
Editor rico (`TX_OBSERVACAO`) com "Importar arquivo" (legado: RTF do WordPad). Mostra o nº da amostra. Na web a observação é gravada **junto** com a amostra (sem gravações parciais ao trocar de aba, como fazia o Delphi).

## 4. Aba "Testes" (testes solicitados)
Grid só leitura: tipo de análise, parâmetro, método, idioma, prazo, **data prevista** (entrega + dias do prazo).
- Na inclusão, e na alteração **sem resultados**, os testes são **regerados da proposta** (`SP_AMOSTRA_GERAR_TESTES`) com todas as análises do item escolhido.
- Com resultados lançados, os testes **não** são alterados e a tela avisa (mensagem do legado): *"Por existir Resultados cadastrados para este Histórico de Amostra, os parâmetros NÃO serão alterados conforme a Proposta. Para alterá-los conforme a Proposta, é necessário excluir os Resultados da Amostra."*

## 5. Aba "Consultar"
Filtros: cliente (tecla Delete limpa), "exibir a partir de" (padrão hoje − `DIAS_PESQUISA_PADRAO`), nº da amostra, proposta, produto, status, "só última revisão" (padrão marcado).
Grid: amostra, revisão, data/hora de entrega, qtde por embalagem, lote, fabricação, validade, temperatura, aspecto, cor, status (badge), cliente, produto.
Pesquisa avançada (legado `un_Lab_Hist_Amostras_Pesq`): por cliente/produto mostrando nº, status, proposta, lote, datas, embalagem, tipos.

## 6. Ações

| Ação | Regra |
|---|---|
| **Incluir** | foco em Cliente; exige profissional vinculado ao usuário |
| **Gravar** | valida obrigatórios na ordem do Delphi (cliente, proposta, data, hora, embalagem, tipo de amostra, produto, tipo de análise, responsável, status) → INSERT (o banco gera o número) → `SP_AMOSTRA_GERAR_TESTES` → mesma transação → mensagem com o número → oferece **imprimir etiquetas** (T25) |
| **Alterar** | bloqueado se a amostra é revisão antiga ou tem **boletim emitido** (o banco também bloqueia) → oferecer "Gerar revisão". Com resultados: tipo/proposta/produto ficam bloqueados |
| **Gerar revisão** (menu "Criar Revisão") | só com resultados (*"Não existe Resultados para esta Amostra. A Amostra pode ser alterada."*); motivo obrigatório → `SP_GERAR_REVISAO_AMOSTRA` → abre a nova revisão (`… R01`) em edição; resultado e boletim também são revisados (doc 12 §3) |
| **Excluir** | só revisão 0 sem resultados e sem movimentação manual (confirmação) |
| **Imprimir** | "Comprovante de recebimento" (layout `Layout_ImpHistAmostra.fr3`): cliente com endereço de correspondência (*"Cliente sem endereço de correspondência"* se faltar), dados da amostra, testes solicitados, observação |
| **Etiquetas** | atalho para a T25 com a amostra |

## 7. Erros do legado a não repetir
- Número com ano da data digitada / reinício da sequência / lacunas / sem bloqueio (doc 10 §4).
- Revisão perdia coletor/data da coleta e duplicava o estoque.
- Lista de propostas mostrava revisões antigas.
- `VW_LAB_HIST_AMOSTRA` duplicava amostras e escondia clientes sem endereço do tipo 2.

## Aceite
- [ ] Duas inclusões simultâneas recebem números diferentes.
- [ ] Data de entrega futura recusada; 1 ano atrás recusada sem permissão; com permissão + confirmação grava **com o ano atual**.
- [ ] Virada de ano: primeira amostra do ano = 00001; números do ano anterior intactos.
- [ ] Validade só "mês/ano" grava e imprime `05/2027`; 31/02 recusado.
- [ ] Testes iguais às análises do item da proposta, com data prevista.
- [ ] Tipo de análise limitado aos tipos do item.
- [ ] Entrada automática de estoque criada (saldo = qtde).
- [ ] Revisão gera `R01` com todos os campos (inclusive coletor) e mantém o estoque na raiz.

**Status:** 🟨 v1 existe com regras erradas (número = MAX+1 sem ano) — **refazer**
