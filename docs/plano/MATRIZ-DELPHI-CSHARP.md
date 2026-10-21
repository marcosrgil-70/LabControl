# Matriz de Rastreabilidade — Delphi 7 → C#

Todas as telas do LabControl.exe (menu `un_Menu.dfm`) e as telas auxiliares, com o destino no sistema novo.
**Situação:** ⬜ não iniciado · 🟨 existe na v1 (refazer) · ✅ pronto · ⛔ descontinuado.

## Cadastros → Cadastros Básicos
| Menu Delphi | Unit Delphi | MD v2 | Rota v2 | Fase | Situação |
|---|---|---|---|---|---|
| País | `Standards/un_Paises` | [T11-01](../telas/T11-01-paises.md) | `/Geografia/Paises` | 2 | ✅ |
| Estados | `Standards/un_Estados` | [T11-02](../telas/T11-02-estados.md) | `/Geografia/Estados` | 2 | ✅ |
| Cidades | `Standards/un_Cidades` | [T11-03](../telas/T11-03-cidades.md) | `/Geografia/Cidades` | 2 | ✅ |
| Bairros | `Standards/un_Bairros` | [T11-04](../telas/T11-04-bairros-logradouros.md) | — | — | ⛔ (endereço em texto) |
| Tipos de Logradouros | `Standards/un_Logradouros_Tipos` | [T10-01](../telas/T10-01-tipos-logradouro.md) | `/Auxiliares/TIPOS_LOGRADOURO` | 2 | ✅ |
| Logradouros | `Standards/un_Logradouros` | [T11-04](../telas/T11-04-bairros-logradouros.md) | — | — | ⛔ |
| Tipos de Endereço | `Standards/un_Enderecos_Tipos` | [T10-02](../telas/T10-02-tipos-endereco.md) | `/Auxiliares/ENDERECOS_TIPOS` | 2 | ✅ |
| Registros Profissionais | `un_CadRegProfissional` | [T10-05](../telas/T10-05-registros-profissionais.md) | `/Auxiliares/TIPOS_REG_PROFISSIONAL` | 2 | ✅ |
| Cargos de Funcionários | `un_Cargo_Funcionarios` | [T10-06](../telas/T10-06-cargos-funcionarios.md) | `/Auxiliares/CARGOS_FUNCIONARIOS` | 2 | ✅ |
| Idiomas | `un_CadIdiomas` | [T10-07](../telas/T10-07-idiomas.md) | `/Auxiliares/IDIOMAS` | 2 | ✅ |
| Unidades | `un_CadUnidades` | [T10-08](../telas/T10-08-unidades.md) | `/Auxiliares/UNIDADES` | 2 | ✅ |
| Tipo de Embalagens | `un_CadTipoEmbalagem` | [T10-09](../telas/T10-09-tipos-embalagem.md) | `/Auxiliares/EMBALAGENS_TIPOS` | 2 | ✅ |
| Tipo de Resultados | `un_TipoResultado` | [T10-10](../telas/T10-10-tipos-resultado.md) | `/Auxiliares/LAB_TIPOS_RESULTADOS` | 2 | ✅ |
| Condições de Pagamento | `Standards/un_Condicoes_Pagtos` | [T10-11](../telas/T10-11-condicoes-pagamento.md) | `/Auxiliares/CONDICOES_PAGTOS` | 2 | ✅ |
| Prazos | `un_CadPrazos` | [T10-12](../telas/T10-12-prazos.md) | `/Auxiliares/PRAZOS` | 2 | ✅ |
| (fora do menu) Tipos de Telefone | `Standards/un_CadTipoFone` | [T10-03](../telas/T10-03-tipos-telefone.md) | `/Auxiliares/FONES_TIPOS` | 2 | ✅ |
| (fora do menu) Tipos de E-mail | `Standards/un_cadEmails_Tipos` | [T10-04](../telas/T10-04-tipos-email.md) | `/Auxiliares/EMAILS_TIPOS` | 2 | ✅ |
| (sem tela) Moedas | — | [T10-13](../telas/T10-13-moedas.md) | `/Auxiliares/MOEDAS` | 2 | ✅ |

## Cadastros
| Menu Delphi | Unit Delphi | MD v2 | Rota v2 | Fase | Situação |
|---|---|---|---|---|---|
| Clientes/Fornecedores | `Standards/un_Entidades` (`teCLI`) | [T12](../telas/T12-clientes.md) | `/Clientes` | 3 | ✅ |
| Funcionários | `Standards/un_Entidades` (`teFUNC`) | [T13](../telas/T13-profissionais.md) | `/Profissionais` | 3 | ✅ |
| Tipos de Amostras | `un_CadTipoAmostra` | [T10-14](../telas/T10-14-tipos-amostra.md) | `/Auxiliares/AMOSTRAS_TIPO` | 2 | ✅ |
| Status das Amostras | `un_CadStatusAmostra` | [T10-15](../telas/T10-15-status-amostra.md) | `/Auxiliares/AMOSTRAS_STATUS` | 2 | ✅ |
| Tipos de Análises | `un_CadTipoAnalise` | [T10-16](../telas/T10-16-tipos-analise.md) | `/Auxiliares/ANALISES_TIPO` | 2 | ✅ |
| Status das Análises | `un_CadStatusAnalise` | [T10-17](../telas/T10-17-status-analise.md) | `/Auxiliares/ANALISES_STATUS` | 2 | ✅ |
| Métodos de Análise | `un_CadMetodoAnalise` | [T10-18](../telas/T10-18-metodos-analise.md) | `/Auxiliares/ANALISES_METODOS` | 2 | ✅ |
| Parâmetros para Análises | `un_ParametrosAnalises` | [T15](../telas/T15-parametros-analise.md) | `/ParametrosAnalise` | 3 | 🟨 |
| Status dos Boletins | `un_CadStatusBoletim` | [T10-19](../telas/T10-19-status-boletim.md) | `/Auxiliares/BOLETINS_STATUS` | 2 | ✅ |
| Status das Propostas | `un_CadStatusPropostas` | [T10-20](../telas/T10-20-status-proposta.md) | `/Auxiliares/LAB_PROPOSTAS_STATUS` | 2 | ✅ |
| Produtos | `Standards/un_Produtos` | [T14](../telas/T14-produtos.md) | `/Produtos` | 3 | ✅ |
| Empresas Usuárias do Sistema | `Standards/un_Entidades` (`teEMP`) | [T03](../telas/T03-empresa-usuaria.md) | `/Empresas` (menu), `/Empresa/Dados` | 1 | ✅ |
| Parâmetros do Sistema | `Standards/un_Parametros` | [T05](../telas/T05-parametros-empresa.md) | `/Empresa/Parametros` | 1 | ✅ |

## Movimentações
| Menu Delphi | Unit Delphi | MD v2 | Rota v2 | Fase | Situação |
|---|---|---|---|---|---|
| Propostas | `un_Propostas` + `undm_Propostas` | [T20](../telas/T20-propostas.md) | `/Propostas` | 4 (essencial) / 6 | 🟨 |
| (botão Ped.Venda da proposta) | `un_Propostas.Btn_PedVendaClick` | [T21](../telas/T21-pedidos-venda.md) | `/PedidosVenda` | 6 | ⬜ (tela nova) |
| Histórico de Amostras | `un_MovHistAmostras` + `undm_HistAmostras` | [T22](../telas/T22-recebimento-amostras.md) | `/Amostras` | 4 | 🟨 |
| (pesquisa de amostras) | `un_Lab_Hist_Amostras_Pesq` | [T29](../telas/T29-pesquisa-amostras.md) | componente | 4 | ⬜ |
| Movimentação de Amostras | `un_MovimentacaoAmostras` | [T23](../telas/T23-movimentacao-amostras.md) | `/Movimentacao` | 7 | 🟨 |
| Localização das Amostras | `un_LocalAmostra` + `unDM_LocalAmostra` | [T24](../telas/T24-localizacao-amostras.md) | `/Localizacao` | 7 | 🟨 |
| Resultados | `un_Resultados` + `unDM_Resultados` | [T26](../telas/T26-resultados.md) | `/Resultados` | 5 | 🟨 |
| Boletim | `un_CadBoletim` + `undm_Boletim` (`un_Boletim` antigo) | [T27](../telas/T27-boletim.md) | `/Boletins` | 5 | ⬜ |
| Relatório de Ensaio | `un_Relatorio_Ensaio` | [T28](../telas/T28-relatorio-ensaio.md) | `/RelatorioEnsaio` | 8 (confirmar) | ⬜ |

## Relatórios
| Menu Delphi | Unit Delphi | MD v2 | Rota v2 | Fase | Situação |
|---|---|---|---|---|---|
| Faturamento | `un_RelFaturamento` | [T30-01](../telas/T30-01-rel-faturamento.md) | `/Relatorios/Faturamento` | 8 | 🟨 |
| Nr. de Amostras por período | `un_RelAmostrasPeriodo` | [T30-02](../telas/T30-02-rel-amostras-periodo.md) | `/Relatorios/AmostrasPeriodo` | 8 | 🟨 |
| Nr. Amostras por cliente | `un_RelAmostrasCliente` | [T30-03](../telas/T30-03-rel-amostras-cliente.md) | `/Relatorios/AmostrasCliente` | 8 | ⬜ |
| Nr. Parâmetros analisados | `un_RelParamAnalisados` | [T30-04](../telas/T30-04-rel-parametros-analisados.md) | `/Relatorios/ParametrosAnalisados` | 8 | ⬜ |
| Tempo de duração das Análises | `un_RelTempoAnalises` | [T30-05](../telas/T30-05-rel-tempo-analises.md) | `/Relatorios/TempoAnalises` | 8 | ⬜ |
| Produção por período | `un_RelProducao` | [T30-06](../telas/T30-06-rel-producao.md) | `/Relatorios/Producao` | 8 | ⬜ |
| Etiqueta de Amostras | `un_EtqAmostra` + `un_EtqAmostraQtde` | [T25](../telas/T25-etiquetas-amostras.md) | `/Etiquetas` | 4 | ⬜ |

## Utilitários e sistema
| Menu Delphi | Unit Delphi | MD v2 | Rota v2 | Fase | Situação |
|---|---|---|---|---|---|
| (lançador externo MAIS_Acesso) | `uAcesso` (validação dos parâmetros) | [T00](../telas/T00-login-empresa.md) | `/Conta/Login` | 1 | ✅ |
| Ativar Empresa | `un_Menu` | [T00](../telas/T00-login-empresa.md) (trocar empresa) | `/Conta/Empresa` | 1 | ✅ |
| Atualizar Direitos de Acesso | `un_DireitoAcesso01`, `un_DirAcesso01` | [T02](../telas/T02-usuarios.md) + [T04](../telas/T04-perfis-permissoes.md) | `/Usuarios`, `/Perfis` | 1 | ✅ |
| Atualizar Componentes | `un_Menu` / `SYS_COMPONENTES` | [T04](../telas/T04-perfis-permissoes.md) (catálogo pelo seed) | — | — | ⛔ |
| Lado a Lado / Cascata | MDI | — | — | — | ⛔ |
| Sobre o Sistema | `un_About` | [T01](../telas/T01-layout-menu.md) (versão no menu) | — | 1 | ✅ |
| Sair | `un_Menu` | [T00](../telas/T00-login-empresa.md) | `/Conta/Sair` | 1 | ✅ |
| (novo) Auditoria | — | [T06](../telas/T06-auditoria.md) | `/Auditoria` | 8 | ⬜ |
| (novo) Migração | — | [T07](../telas/T07-migracao.md) | `/Migracao` | 3–5, 9 | 🟨 (errada) |
| (novo) Painel | — | [T01](../telas/T01-layout-menu.md) | `/` | 1 | ✅ |

## Formulários de base (sem tela própria — viram padrões)
| Unit Delphi | Padrão v2 |
|---|---|
| `Standards/un_CadastroBasico` | [T10-00 CRUD genérico](../telas/T10-00-crud-auxiliares.md) + layout Index/Criar/Editar (doc [01 §6](../01-ARQUITETURA-V2.md)) |
| `Standards/un_RelatorioBasico` | [T30-00 Padrão dos relatórios](../telas/T30-00-relatorios-padrao.md) |
| `Standards/un_MovimentacaoBasico`, `un_PesquisaBasico` | componentes de consulta/pesquisa ([T29](../telas/T29-pesquisa-amostras.md)) |
| `Library/unClassAcessos` | atributo `[Permissao]` + tag helper ([P02](../processos/P02-autenticacao-permissoes.md)) |
| `uAtencao` | mensagens/TempData ([P22](../processos/P22-erros-transacoes.md)) |
| `Rotinas.pas`, `Library/Funcoes.pas` | utilitários C# (`Documento.ValidarCpfCnpj`, `DataParcial`, formatação) |
