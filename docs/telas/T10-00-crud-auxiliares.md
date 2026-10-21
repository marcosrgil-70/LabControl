# T10-00 — Padrão dos Cadastros Auxiliares (CRUD genérico)

**Fase:** 2 · **Rota:** `/Auxiliares/{tabela}` · **Permissão:** `AUXILIARES.CONSULTAR/INCLUIR/ALTERAR/EXCLUIR` · **Criticidade:** ⚪ (mas pré-requisito de todas as outras telas)
**Legado:** forms herdados de `Standards/un_CadastroBasico.pas` (abas **Dados** e **Consultar**, botões Incluir/Alterar/Excluir/Gravar/Cancelar/Consultar, grid com colunas configuráveis gravadas por usuário).
**Processos:** [P01 Multiempresa](../processos/P01-multiempresa.md) · **Regras:** [RN-03 Cadastros](../regras/RN-03-cadastros.md)

## Objetivo
Um único controller/serviço genérico atende os 20 cadastros auxiliares, porque todos têm a mesma mecânica (descrição + 0 a 3 campos) e são **lookups** das telas principais. Implementar e aprovar com **um** auxiliar (Idiomas) e depois replicar por configuração.

## Comportamento herdado do Delphi (`un_CadastroBasico`)
| Delphi | Web v2 |
|---|---|
| Aba "Consultar" com grid e busca | `Index`: busca por descrição (contém), filtro "mostrar inativos", tabela paginada |
| Aba "Dados" (Incluir/Alterar) | modal com o formulário |
| "Deseja realmente excluir este registro?" | confirmação; **excluir só sem uso**, senão oferecer "Inativar" |
| Fechar com registro em edição → "As alterações não foram salvas. Deseja cancelar…?" | aviso ao fechar o modal com alterações |
| Configuração de colunas do grid por usuário (`ConfigColunaGrid`) | não reproduzir (colunas fixas) |

## Implementação
- `AuxiliarDefinicao` (por tabela): nome, tabela, chave, colunas (rótulo, tipo, tamanho, obrigatório, único), regras específicas (ganchos `ValidarAntesDeGravar`, `PodeAlterarCampo`).
- `AuxiliaresController` → `AuxiliarService<T>` → EF com filtro global de empresa.
- Todos têm `ID_EMPRESAS` (preenchido no `SaveChanges`) e `ATIVO`.
- **Unicidade** pelas `UNIQUE (ID_EMPRESAS, …)` do banco: tratar `MySqlException` 1062 como "Já existe um registro com esta descrição/código".
- **Cor**: `<input type=color>`, gravado `#RRGGBB`, exibido como badge.
- **Cadastro rápido "+"**: endpoint `POST /Auxiliares/{tabela}/Rapido` devolvendo `{id, texto}` para o combo que abriu o modal (proposta: produto, método, idioma, prazo; resultados: tipo de resultado).
- Ao trocar de empresa, cada empresa vê só os seus registros.

## Lista dos auxiliares
| Tela | Tabela | Delphi |
|---|---|---|
| [T10-01 Tipos de Logradouro](T10-01-tipos-logradouro.md) | [`TIPOS_LOGRADOURO`](../banco/objetos/tabelas/TIPOS_LOGRADOURO.md) | Standards/un_Logradouros_Tipos.pas |
| [T10-02 Tipos de Endereço](T10-02-tipos-endereco.md) | [`ENDERECOS_TIPOS`](../banco/objetos/tabelas/ENDERECOS_TIPOS.md) | Standards/un_Enderecos_Tipos.pas |
| [T10-03 Tipos de Telefone](T10-03-tipos-telefone.md) | [`FONES_TIPOS`](../banco/objetos/tabelas/FONES_TIPOS.md) | Standards/un_CadTipoFone.pas |
| [T10-04 Tipos de E-mail](T10-04-tipos-email.md) | [`EMAILS_TIPOS`](../banco/objetos/tabelas/EMAILS_TIPOS.md) | Standards/un_cadEmails_Tipos.pas |
| [T10-05 Registros Profissionais](T10-05-registros-profissionais.md) | [`TIPOS_REG_PROFISSIONAL`](../banco/objetos/tabelas/TIPOS_REG_PROFISSIONAL.md) | LabControl/un_CadRegProfissional.pas |
| [T10-06 Cargos de Funcionários](T10-06-cargos-funcionarios.md) | [`CARGOS_FUNCIONARIOS`](../banco/objetos/tabelas/CARGOS_FUNCIONARIOS.md) | LabControl/un_Cargo_Funcionarios.pas |
| [T10-07 Idiomas](T10-07-idiomas.md) | [`IDIOMAS`](../banco/objetos/tabelas/IDIOMAS.md) | LabControl/un_CadIdiomas.pas |
| [T10-08 Unidades de Medida](T10-08-unidades.md) | [`UNIDADES`](../banco/objetos/tabelas/UNIDADES.md) | LabControl/un_CadUnidades.pas |
| [T10-09 Tipos de Embalagem](T10-09-tipos-embalagem.md) | [`EMBALAGENS_TIPOS`](../banco/objetos/tabelas/EMBALAGENS_TIPOS.md) | LabControl/un_CadTipoEmbalagem.pas |
| [T10-10 Tipos de Resultado (descritivos)](T10-10-tipos-resultado.md) | [`LAB_TIPOS_RESULTADOS`](../banco/objetos/tabelas/LAB_TIPOS_RESULTADOS.md) | LabControl/un_TipoResultado.pas |
| [T10-11 Condições de Pagamento](T10-11-condicoes-pagamento.md) | [`CONDICOES_PAGTOS`](../banco/objetos/tabelas/CONDICOES_PAGTOS.md) | Standards/un_Condicoes_Pagtos.pas |
| [T10-12 Prazos](T10-12-prazos.md) | [`PRAZOS`](../banco/objetos/tabelas/PRAZOS.md) | LabControl/un_CadPrazos.pas |
| [T10-13 Moedas](T10-13-moedas.md) | [`MOEDAS`](../banco/objetos/tabelas/MOEDAS.md) | — (sem tela no Delphi; 2 registros cadastrados direto no banco) |
| [T10-14 Tipos de Amostra](T10-14-tipos-amostra.md) | [`AMOSTRAS_TIPO`](../banco/objetos/tabelas/AMOSTRAS_TIPO.md) | LabControl/un_CadTipoAmostra.pas |
| [T10-15 Status das Amostras](T10-15-status-amostra.md) | [`AMOSTRAS_STATUS`](../banco/objetos/tabelas/AMOSTRAS_STATUS.md) | LabControl/un_CadStatusAmostra.pas |
| [T10-16 Tipos de Análise](T10-16-tipos-analise.md) | [`ANALISES_TIPO`](../banco/objetos/tabelas/ANALISES_TIPO.md) | LabControl/un_CadTipoAnalise.pas |
| [T10-17 Status das Análises](T10-17-status-analise.md) | [`ANALISES_STATUS`](../banco/objetos/tabelas/ANALISES_STATUS.md) | LabControl/un_CadStatusAnalise.pas |
| [T10-18 Métodos de Análise](T10-18-metodos-analise.md) | [`ANALISES_METODOS`](../banco/objetos/tabelas/ANALISES_METODOS.md) | LabControl/un_CadMetodoAnalise.pas |
| [T10-19 Status dos Boletins](T10-19-status-boletim.md) | [`BOLETINS_STATUS`](../banco/objetos/tabelas/BOLETINS_STATUS.md) | LabControl/un_CadStatusBoletim.pas |
| [T10-20 Status das Propostas](T10-20-status-proposta.md) | [`LAB_PROPOSTAS_STATUS`](../banco/objetos/tabelas/LAB_PROPOSTAS_STATUS.md) | LabControl/un_CadStatusPropostas.pas |

## Critérios de aceite (valem para todos)
- [x] Incluir, alterar, inativar e excluir (sem uso) com busca e paginação.
- [x] Inativos não aparecem nos combos de inclusão, mas aparecem nos registros antigos.
- [x] Descrição/código duplicado na mesma empresa é recusado com mensagem clara.
- [x] Cada empresa vê só os seus registros.
- [x] Cadastro rápido devolve o item ao combo de origem — 2.4: `CadastroRapidoTests` + navegador `CrudNavegadorTelasTests.Cadastro_rapido_mais_inclui_e_seleciona_no_combo`.

## Implementação (tarefa 2.1)
- `Services/Auxiliares/AuxiliarDefinicao.cs` (`AuxiliarDefinicao<T>` + `CatalogoAuxiliares`), `Services/Auxiliares/AuxiliarService.cs`, `Controllers/AuxiliaresController.cs`, `Views/Auxiliares/{Index,Lista,_Form}.cshtml`, `ViewModels/AuxiliarVM.cs`. Incluir um auxiliar = uma entrada no catálogo (skill `/novo-auxiliar`).
- Rotas: `/Auxiliares` (cartões), `/Auxiliares/{tabela}` (lista: busca, "mostrar inativos", 20 por página), `GET {tabela}/Incluir|Editar/{id}` (formulário do modal), `POST {tabela}/Incluir|Editar/{id}|Excluir/{id}|Ativo/{id}`, `POST {tabela}/Rapido` → `{ id, texto }` (422 `{ erro, campo }`), `GET {tabela}/Opcoes?atual=` (ativos + o já escolhido; basta estar logado).
- Tamanho e obrigatoriedade vêm do modelo do EF (gerado de `Scripts/v2`); texto aparado; duplicidade pela `UNIQUE` do banco (collation sem maiúsculas/acentos: "português" = "Português"); exclusão por `DELETE` direto filtrado pela empresa — em uso o banco recusa (1451) e a lista oferece **Inativar**. Erro ao gravar reabre o modal com a mensagem no campo; fechar com alterações pede confirmação (como o legado).
- Ganchos para a 2.2: `ValidarAntesDeGravar`, `PodeAlterarCampo` (campo bloqueado com cadeado). Mensagens com gênero (`Feminino`).

## Implementação (tarefa 2.2)
- Ganchos novos: `ValidarAntesDeExcluir` (T10-15) e `Acao` (botão extra na linha para uma tela própria — T10-11 parcelas). Duplicidade cai no campo citado na mensagem (sigla, código), não mais sempre no 1º. Campo `short` (código 1–99) corrigido no conversor.

## Implementação (tarefa 2.4) — cadastro rápido "+"
- No combo: `<select … lc-rapido="TABELA">` (`Infra/CadastroRapidoTagHelper.cs`): com AUXILIARES.INCLUIR emite `data-rapido`/`data-rapido-nome`; sem a permissão o combo fica como está. Tabela fora do catálogo = erro de programação (exceção ao renderizar).
- `wwwroot/js/site.js` (todas as telas): envolve o combo num `input-group` com o botão **+** (título "Incluir &lt;singular&gt;"), abre o `_Form` do auxiliar (`GET {tabela}/Incluir`) num modal sem o campo Ativo, grava por `POST {tabela}/Rapido`, insere o item em ordem alfabética em **todos** os combos da mesma tabela na página e o seleciona (dispara `change`); 422 mostra o erro no campo, com o modal aberto.
- Combo dentro de outro modal (contatos): o modal do "+" é criado **dentro** do `.modal` de baixo, sem backdrop próprio (fundo escurecido por CSS `.modal-rapido-sobre`) — o foco preso pelo Bootstrap no modal de baixo não briga com o de cima e o Esc fecha só o de cima.
- Aplicado em: T13 (cargo, tipo de registro) e nos modais de contatos de T03/T12/T13 (tipo de endereço, tipo de logradouro, tipo de telefone, tipo de e-mail). Proposta (método, idioma, prazo) e resultados (tipo de resultado) usam o mesmo atributo quando as telas existirem (4.2, 5.2); produto não é auxiliar: tem o próprio `POST /Produtos/Rapido` (T14, 3.3).

**Status:** ✅ concluída — padrão (2.1), os 20 auxiliares (2.2) e o cadastro rápido "+" (2.4, 21/10/2026).

## Ajustes de 20/10/2026 (teste do usuário)
- Botões da lista com **ícone + texto** (Alterar, Inativar/Reativar, Excluir); **Inativar pede confirmação** ("sai da lista e dos combos de inclusão — marque Mostrar inativos para vê-lo") e a mensagem depois de inativar repete onde ele foi parar.
- A pesquisa avisa no rodapé quando há **inativos que também correspondem** ("N inativo(s) também correspondem à pesquisa — mostrar").
- Testes no navegador ([P26](../processos/P26-testes-ponta-a-ponta.md)): `Auxiliar_idiomas_crud_completo`, `Auxiliar_com_opcoes_tipos_de_endereco`.
