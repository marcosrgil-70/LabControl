# Roteiro de homologação — LabControl v2 (tarefa 9.2)

**Para quem:** usuários do laboratório (recepção, analista, responsável técnico, comercial) com o apoio de quem conduz a implantação.
**Duração:** um dia de uso em paralelo com o sistema antigo (Delphi), que continua valendo até a virada (9.3).
**Objetivo:** fazer o trabalho de um dia normal no sistema novo, conferir que dá o mesmo resultado do antigo e anotar tudo que estranhar.
**Base:** banco `labcontrol_migracao`, com **todos os dados do sistema antigo migrados** (9.1, 29/10/2026). O que for gravado no dia da homologação **não** vai para produção: na virada a migração é feita de novo, com os dados do dia.

> Cada passo tem o **resultado esperado**. Marque ✅ se deu certo; se não, marque ❌ e anote na [tabela de ocorrências](#registro-de-ocorrências) (o que fez, o que esperava, o que aconteceu, se possível com um print).

---

## 0. Preparação (quem conduz, antes do dia)
- [ ] Sistema publicado no servidor do laboratório apontando para o `labcontrol_migracao` (ou rodando numa máquina com acesso ao banco); endereço anotado: ______________________
- [ ] De preferência já como na produção ([checklist da virada](CHECKLIST-VIRADA.md), parte A): HTTPS, pastas dos boletins e das chaves fora da pasta do app, usuário MySQL próprio com as permissões mínimas.
- [ ] Impressora **Argox** instalada no servidor e visível na lista de impressoras do Windows (pendência 21).
- [ ] Em *Sistema › Parâmetros da Empresa › E-mail*: servidor, porta, segurança, usuário, senha e remetente do e-mail do laboratório preenchidos e **"Enviar e-mail de teste"** recebido (pendência 38).
- [ ] Um usuário para cada pessoa que vai testar (os usuários migrados entram com a senha provisória `Mudar@2026` e trocam no 1º acesso), cada um **vinculado ao seu profissional** em *Sistema › Usuários* (sem vínculo não dá para receber amostra, lançar resultado nem assinar boletim).
- [ ] Perfis conferidos em *Sistema › Perfis de Acesso* (RECEPÇÃO, ANALISTA, RESPONSÁVEL TÉCNICO, COMERCIAL).
- [ ] **Período das pesquisas:** os dados migrados vão de 2013 a 2018 e as listas mostram por padrão os últimos 365 dias. Para ver o histórico, ponha "Entrega de" 01/01/2013 nos filtros, ou aumente o "Período padrão de pesquisa" em *Parâmetros da Empresa*.
- [ ] Separar **10 boletins impressos do sistema antigo** (de anos e tipos diferentes) para o passo 12 e os **3 boletins-modelo** da 5.4 (numérico com expoente, descritivo, com revisão ou dois responsáveis).
- [ ] Relatório de anomalias da migração impresso ou aberto: `C:\Project\backup\migracao-9.1-20261029.csv` (pendência 39, passo 13).

## 1. Entrar no sistema
| # | Faça | Resultado esperado | ✅/❌ |
|---|---|---|---|
| 1.1 | Entre com seu usuário e a senha provisória | pede a troca da senha; depois da troca, abre o painel | |
| 1.2 | Erre a senha 5 vezes seguidas (com um usuário de teste) | na 5ª o usuário fica bloqueado por 15 minutos | |
| 1.3 | Confira o topo da tela | nome da empresa e do seu usuário; sem aviso "sem profissional" | |
| 1.4 | Abra o menu | só aparecem as telas que o seu perfil permite | |

## 2. Proposta (comercial)
| # | Faça | Resultado esperado | ✅/❌ |
|---|---|---|---|
| 2.1 | *Laboratório › Propostas › Nova*: escolha um cliente real | endereço e contato do resultado e da cobrança vêm sugeridos | |
| 2.2 | Inclua 2 produtos e, em cada um, parâmetros pela seleção múltipla | valor unitário e método padrão vêm do cadastro do parâmetro | |
| 2.3 | Dê um desconto e grave | número `000NNN/AA-R0` gerado pelo sistema; total = soma − desconto | |
| 2.4 | Contrato: "Usar contrato padrão" (ou importar um Word) | texto aparece na aba Contrato | |
| 2.5 | "PDF" | proposta com a página "Autorização de Serviço"; compare com a do sistema antigo | |
| 2.6 | "Gerar revisão" com um motivo | abre a R1; a R0 fica só para consulta | |

## 3. Pedido de venda
| # | Faça | Resultado esperado | ✅/❌ |
|---|---|---|---|
| 3.1 | Na proposta, "Gerar pedido" com a data de autorização | pedido de 9 dígitos, situação "Aberto"; link do pedido na proposta | |
| 3.2 | *Pedidos de Venda*: abra o pedido | contratado × recebido por item (ainda 0 recebido) | |
| 3.3 | "PDF" do pedido | compare com o pedido impresso do sistema antigo | |

## 4. Recebimento de 3 amostras (recepção)
| # | Faça | Resultado esperado | ✅/❌ |
|---|---|---|---|
| 4.1 | *Recebimento de Amostras › Nova*: cliente → proposta → item → tipo | número `TT-NNNNN-01/AA` gerado pelo sistema ao gravar; testes da proposta criados | |
| 4.2 | Receba a 2ª amostra com datas parciais de fabricação/validade (só mês/ano) | aceita; mostra como no sistema antigo | |
| 4.3 | Receba a 3ª com **data de entrega retroativa** além do limite (ex.: 10 dias atrás) | pede confirmação; só grava com a permissão de entrega retroativa | |
| 4.4 | Abra o pedido de venda | situação "Em execução"; recebido conta as 3 amostras | |
| 4.5 | "Comprovante" de uma amostra | página de impressão com os dados da amostra | |
| 4.6 | "Pesquisa avançada": ache uma amostra **migrada** de 2017 pelo número | abre a amostra antiga com histórico "Registro do legado, sem data" | |

## 5. Etiquetas (pendência 21)
| # | Faça | Resultado esperado | ✅/❌ |
|---|---|---|---|
| 5.1 | No recebimento, "Etiquetas" → escolha a **Argox** → imprimir 1 etiqueta | sai a etiqueta 80 × 50 mm, na posição certa (girada como a antiga) | |
| 5.2 | Leia o código de barras com o leitor do laboratório | o leitor devolve o número da amostra (Code 128) | |
| 5.3 | Produto sigiloso: imprima | as linhas do produto saem em branco | |

## 6. Movimentação e arquivo
| # | Faça | Resultado esperado | ✅/❌ |
|---|---|---|---|
| 6.1 | *Movimentação*: ache uma amostra nova → "Nova movimentação" de saída com justificativa | saldo diminui; não deixa sair mais que o saldo | |
| 6.2 | *Localização › Arquivar amostra*: armário/prateleira/caixa | situação "Arquivada" | |

## 7. Resultados (analista)
| # | Faça | Resultado esperado | ✅/❌ |
|---|---|---|---|
| 7.1 | *Resultados*: aba "Fila de análises" | as análises das 3 amostras aparecem, com o prazo | |
| 7.2 | "Novo resultado" → amostra 1 → "Lançar todos" | datas e analista preenchidos | |
| 7.3 | Um resultado **numérico com expoente** (ex.: < 1,0 × 10²) e um **descritivo** (ex.: Ausência em 25 g) | a prévia mostra o texto como sai no boletim (expoente sobrescrito) | |
| 7.4 | Grave | "Resultado gravado"; a fila não mostra mais essas análises | |

## 8. Boletim (responsável técnico)
| # | Faça | Resultado esperado | ✅/❌ |
|---|---|---|---|
| 8.1 | *Boletins › Novo* → amostra 1 → marque os parâmetros → "Concluir seleção" | metodologias preenchidas sozinhas | |
| 8.2 | Ordene com ▲/▼, escolha o(s) responsável(is) e grave | | |
| 8.3 | "Visualizar" | prévia com a marca d'água; compare com o boletim do sistema antigo (modelos da 5.4) | |
| 8.4 | "Emitir" | PDF definitivo; boletim, parâmetros e resultado ficam travados | |
| 8.5 | "Imprimir" | o mesmo PDF da emissão (conferido pelo sistema) | |
| 8.6 | "Enviar por e-mail" para um e-mail do laboratório (pendência 38) | chega com o PDF anexado e fora do spam; o envio aparece no histórico | |
| 8.7 | Emita os boletins de todas as amostras do pedido | o pedido passa sozinho para "Concluído" | |

## 9. Revisão em cascata
| # | Faça | Resultado esperado | ✅/❌ |
|---|---|---|---|
| 9.1 | No recebimento, gere uma **revisão da amostra 1** com motivo | nasce a "… R01" com resultado e boletim novos (não emitidos); o pedido volta para "Em execução" | |
| 9.2 | Corrija o resultado, emita o boletim R01 | pedido conclui de novo; o boletim R00 continua o mesmo PDF | |
| 9.3 | *Localização*: descarte por reprovação de uma amostra com boletim emitido | status "REPROVADA - DESCARTADA"; o boletim não muda | |

## 10. Relatórios do mês
| # | Faça | Resultado esperado | ✅/❌ |
|---|---|---|---|
| 10.1 | *Relatórios* (os seis), período = mês atual | os números do dia aparecem; PDF e CSV abrem (CSV direto no Excel) | |
| 10.2 | Os seis com um mês fechado do histórico (ex.: 10/2017) e compare com o sistema antigo | diferenças só as explicadas nos MDs (revisões e linhas repetidas que o antigo contava) | |

## 11. Auditoria
| # | Faça | Resultado esperado | ✅/❌ |
|---|---|---|---|
| 11.1 | *Sistema › Auditoria*: ação "Revisão" | a revisão do passo 9 com usuário e motivo | |
| 11.2 | "Ver diferença" de uma alteração | campos alterados destacados | |
| 11.3 | Aba "Histórico da amostra" com o número da amostra 1 | as duas revisões | |

## 12. Boletins migrados (2ª via)
| # | Faça | Resultado esperado | ✅/❌ |
|---|---|---|---|
| 12.1 | Para cada um dos **10 boletins impressos** do sistema antigo: *Boletins*, ache pelo número (filtro "a partir de" antigo) → "Imprimir 2ª via" | mesmos parâmetros, resultados, unidades, especificações e responsáveis do impresso | |

## 13. Relatório de anomalias da migração (pendência 39)
Revisar com quem conhece os dados o arquivo `C:\Project\backup\migracao-9.1-20261029.csv` (filtrar "Anomalia"). Em especial os 4 resultados que ficaram como estavam no sistema antigo:
- amostra **04-00551-01/18**: as duas datas em 16/09/0320 — qual a data certa?
- amostra **02-01183-01/17**: início 02/01/2017, antes da entrega (31/08/2017) — mês digitado errado?
- amostras **02-01174-01/17** (65 dias) e **04-01040-01/18** (99 dias): estão certas?
Decidir: corrigir pela tela de Resultados depois da virada (revisão) ou criar regra na migração antes da virada.

---

## Registro de ocorrências
| # | Passo | Quem | O que fez | O que esperava | O que aconteceu | Gravidade (impede / atrapalha / detalhe) |
|---|---|---|---|---|---|---|
| 1 | | | | | | |
| 2 | | | | | | |
| 3 | | | | | | |

## Encerramento
- [ ] Todas as ocorrências "impede" corrigidas e o roteiro repetido nesses passos.
- [ ] Layout do boletim aprovado com os 3 modelos (5.4).
- [ ] Data da virada (9.3) combinada: ____/____/______ — no dia: backup do sistema antigo e do novo, última migração com os dados do dia, senhas provisórias entregues, sistema antigo só para consulta.
