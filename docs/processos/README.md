# Processos de Negócio

Cada processo descreve um fluxo de ponta a ponta (tela → serviço C# → banco), com regras, mensagens, erros do legado corrigidos e cenários de teste.
Legenda de criticidade: 🔴🔴 núcleo do laboratório · 🔴 crítico · 🟠 importante · ⚪ normal.

## Fundamentos (fase 0/1)
| Processo | Crit. | Resumo |
|---|---|---|
| [P01 Multiempresa e empresa ativa](P01-multiempresa.md) | 🔴 | filtro global, herança de empresa, troca de empresa |
| [P02 Autenticação, sessão e permissões](P02-autenticacao-permissoes.md) | 🔴 | login PBKDF2, bloqueio, perfis, profissional do usuário |
| [P03 Numeração de documentos](P03-numeracao-documentos.md) | 🔴 | `SP_PROXIMO_NUMERO` com bloqueio, sequências por ano |
| [P21 Auditoria e log](P21-auditoria-log.md) | 🟠 | `AUDITORIA` (aplicação) + log da amostra (banco) |
| [P22 Erros de negócio e transações](P22-erros-transacoes.md) | 🔴 | SQLSTATE 45000, procedures com/sem transação, variáveis de sessão |
| [P23 Nova empresa e cópia de cadastros](P23-nova-empresa.md) | 🟠 | `SP_INICIALIZAR_EMPRESA` + cópia |

## Propostas e pedidos (fases 4/6)
| Processo | Crit. | Resumo |
|---|---|---|
| [P05 Ciclo de vida da proposta](P05-ciclo-proposta.md) | 🔴 | status, contatos/endereços, alterar × revisar, excluir |
| [P06 Cálculo dos valores da proposta](P06-valores-proposta.md) | 🔴 | fórmulas da linha e do cabeçalho |
| [P07 Revisão da proposta](P07-revisao-proposta.md) | 🔴 | `SP_GERAR_REVISAO_PROPOSTA` |
| [P08 Pedido de venda](P08-pedido-venda.md) | 🔴 | geração, situação, revisão vigente, cancelamento |

## Amostras (fases 4/7)
| Processo | Crit. | Resumo |
|---|---|---|
| [P04 Número da amostra e proteção do ano](P04-numero-amostra-ano.md) | 🔴🔴 | ano do servidor, data retroativa com permissão |
| [P09 Recebimento da amostra](P09-recebimento-amostra.md) | 🔴🔴 | fluxo completo de inclusão/alteração/exclusão |
| [P10 Datas parciais](P10-datas-parciais.md) | 🟠 | fabricação/validade só ano, mês/ano ou completa |
| [P11 Revisão da amostra (cascata)](P11-revisao-amostra.md) | 🔴🔴 | amostra → resultado → boletim |
| [P12 Estoque e saldo](P12-estoque-amostras.md) | 🟠 | entrada automática, saídas, saldo na raiz |
| [P13 Arquivo e descarte](P13-arquivo-descarte.md) | ⚪ | localização física e descarte |
| [P19 Etiquetas](P19-etiquetas.md) | 🟠 | código de barras do nº da amostra |

## Resultados e boletim (fase 5)
| Processo | Crit. | Resumo |
|---|---|---|
| [P14 Lançamento de resultados](P14-lancamento-resultados.md) | 🔴🔴 | numérico × descritivo, formatação, fila de análises |
| [P15 Revisão e desfazer de resultado](P15-revisao-resultado.md) | 🔴 | `SP_GERAR_REVISAO_RESULTADO`, `SP_EXCLUIR_REVISAO_RESULTADO` |
| [P16 Montagem do boletim](P16-montagem-boletim.md) | 🔴🔴 | seleção de parâmetros, metodologias, layout do PDF |
| [P17 Emissão do boletim](P17-emissao-boletim.md) | 🔴🔴 | PDF + hash + congelamento |
| [P18 Revisão e desfazer de boletim](P18-revisao-boletim.md) | 🔴 | `SP_GERAR_REVISAO_BOLETIM`, `SP_EXCLUIR_REVISAO_BOLETIM` |

## Relatórios e migração (fases 3–9)
| Processo | Crit. | Resumo |
|---|---|---|
| [P20 Relatórios gerenciais](P20-relatorios-gerenciais.md) | 🟠 | regras de contagem |
| [P24 Migração Firebird → MySQL](P24-migracao.md) | 🔴🔴 | princípios, partes, encerramento |
| [Etapas M-01 … M-18](migracao/README.md) | 🔴 | um MD por etapa |
