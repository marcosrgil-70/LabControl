# T10 — Cadastros Auxiliares (CRUD genérico)

**Fase:** 2 · **Rota:** `/Auxiliares/{tabela}` · **Permissão:** `AUXILIARES.*`
**Legado:** cada um era um form herdado de `un_CadastroBasico` (abas Dados/Consultar, botões Incluir/Alterar/Excluir/Gravar/Cancelar). Todos simples, mas **usados como lookup em outras telas** — por isso precisam existir antes de Clientes, Propostas e Amostras.

## Implementação
Um único `AuxiliaresController` + `AuxiliarDefinicao` (nome, tabela, colunas, validações) → lista com busca + modal de inclusão/edição. Todos têm `ID_EMPRESAS` (filtro global) e `ATIVO` (inativos somem dos combos, mas continuam nos registros antigos).
**Excluir** só se não houver uso (FK); caso contrário oferecer "Inativar".
Cada lookup usado em outra tela tem botão **"+" (cadastro rápido)** que abre o modal do auxiliar e devolve o item criado (Delphi: botões `Btn_IncCad*` na proposta e tecla Insert no tipo de resultado).

## Tabelas

| # | Auxiliar (menu Delphi) | Tabela | Campos (além de ATIVO) | Regras específicas | Usado em |
|---|---|---|---|---|---|
| 1 | Tipos de Logradouros | `TIPOS_LOGRADOURO` | Descrição*, Abreviatura* (única) | — | Endereços |
| 2 | Tipos de Endereço | `ENDERECOS_TIPOS` | Descrição* (única), **Finalidade** (Geral/NF/Correspondência/Entrega/Cobrança) | Finalidade sugere o endereço na proposta | Endereços, Proposta |
| 3 | Tipos de Telefone | `FONES_TIPOS` | Descrição* | — | Fones |
| 4 | Tipos de E-mail | `EMAILS_TIPOS` | Descrição* | — | E-mails/contatos |
| 5 | Registros Profissionais | `TIPOS_REG_PROFISSIONAL` | Descrição* (15) ex.: CRF, CRQ | — | Profissionais, boletim |
| 6 | Cargos de Funcionários | `CARGOS_FUNCIONARIOS` | Descrição* (50) | — | Profissionais, boletim (cargo do responsável) |
| 7 | Idiomas | `IDIOMAS` | Descrição* | — | Proposta (análise), testes |
| 8 | Unidades | `UNIDADES` | Descrição*, Sigla* (única, 15) | Sigla vai para o boletim | Produtos, Resultados |
| 9 | Tipos de Embalagens | `EMBALAGENS_TIPOS` | Descrição* | — | Produtos, Amostras |
| 10 | Tipos de Resultados | `LAB_TIPOS_RESULTADOS` | Descrição* (80) ex.: "Ausência em 25 g" | Texto impresso no boletim | Resultados |
| 11 | Condições de Pagamento | `CONDICOES_PAGTOS` (+`_PARC`) | Código* (10, único), Descrição* (250); grid opcional de parcelas (nº, dias, %) — Σ% = 100 | Lista só ativas na proposta (Delphi: `INATIVO = 0`) | Proposta, Pedido |
| 12 | Prazos | `PRAZOS` | Descrição*, **Qtde. de dias*** (≥0), dias úteis (s/n) | Calcula `DT_PREVISTA` dos testes | Proposta, testes |
| 13 | Moedas | `MOEDAS` | Descrição*, Sigla* | — | Proposta |
| 14 | **Tipos de Amostras** | `AMOSTRAS_TIPO` | **Código* (1–99, único)**, Descrição* | **O código compõe o nº da amostra (TT)**: não pode ser alterado depois de usado em amostra | Amostras |
| 15 | Status das Amostras | `AMOSTRAS_STATUS` | Descrição*, Cor, **Permite resultado** | Ao menos 1 status com "permite resultado" | Amostras, Resultados |
| 16 | **Tipos de Análises** | `ANALISES_TIPO` | **Código* (1–99, único)**, Descrição* | **Compõe o nº da amostra (AA)**: imutável após uso | Parâmetros, Amostras |
| 17 | Status das Análises | `ANALISES_STATUS` | Descrição*, Cor | — | Testes |
| 18 | Métodos de Análise | `ANALISES_METODOS` | Descrição* (250) | Vai para "Metodologias" do boletim | Parâmetros, Proposta, testes |
| 19 | Status dos Boletins | `BOLETINS_STATUS` | Descrição*, Cor | — | Boletim |
| 20 | Status das Propostas | `LAB_PROPOSTAS_STATUS` | Descrição*, Cor, **Permite receber amostra** | — | Proposta, Amostras |

Geografia (T11) e Parâmetros de Análise (T15) têm telas próprias.

### Cor
Seletor de cor (`<input type=color>`), gravado `#RRGGBB`; exibido como badge nas listas.

## Aceite
- [ ] Os 20 auxiliares com incluir/alterar/inativar/excluir (sem uso) e busca.
- [ ] Código de tipo de amostra/análise bloqueado após uso.
- [ ] Inativos não aparecem nos combos de inclusão, mas aparecem em registros antigos.
- [ ] Cada empresa vê só os seus.

**Status:** ⬜ (v1 tem `TabelasAuxiliares` parcial e sem empresa — refazer genérico)
