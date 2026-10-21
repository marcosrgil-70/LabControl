# Fase 2 — Cadastros Básicos (auxiliares e geografia)

**Objetivo:** todos os lookups prontos (20 auxiliares + geografia + componente de endereço + cadastro rápido), pré-requisito de clientes, propostas e amostras.
**Modelo/modo:** 🟢 Sonnet para 2.1/2.3/2.4 · ⚪ Haiku + ⚡ accept edits para 2.2 (replicação) · ✋ para o primeiro auxiliar.
**Ler antes:** [T10-00](../telas/T10-00-crud-auxiliares.md) e os MDs [T10-01 … T10-20](../telas/T10-00-crud-auxiliares.md#lista-dos-auxiliares) · [T11-01…04](../telas/T11-03-cidades.md) · [RN-03](../regras/RN-03-cadastros.md)
**Depende de:** Fase 1.

---

### 2.1 CRUD genérico com um auxiliar (Idiomas)
> ✅ **Concluída em 20/10/2026** — visual aprovado pelo usuário; ver [T10-00 › Implementação](../telas/T10-00-crud-auxiliares.md#implementação-tarefa-21); aceite em `AuxiliaresTests`; skill `/novo-auxiliar` criada.
- **Passos:** `AuxiliarDefinicao`, `AuxiliaresController`, `AuxiliarService<T>`, views Index (busca, inativos, paginação) e modal; tradução de duplicidade (1062) e de uso (1451 → "inativar"); cadastro rápido (`/Auxiliares/{tabela}/Rapido`).
- **Aceite:** [aceite do padrão T10-00](../telas/T10-00-crud-auxiliares.md#critérios-de-aceite-valem-para-todos) para Idiomas; aprovação do usuário do visual.
- **Ao concluir:** criar a skill `.claude/skills/novo-auxiliar/SKILL.md` a partir do CRUD de Idiomas aprovado (definição, permissões, menu, views, teste, docs), para a 2.2 replicar os 19 auxiliares por ela.
- **Esforço:** M

### 2.2 Replicar para os outros 19 auxiliares ⚪⚡
> ✅ **Concluída em 21/10/2026** — T10-01…T10-06 em 20/10/2026 ([P25](../processos/P25-cadastro-entidades-contatos.md)) e T10-08…T10-20 em 21/10/2026: ver [T10-11 › Implementação](../telas/T10-11-condicoes-pagamento.md#implementação-tarefa-22) (parcelas em tela própria), [T10-14](../telas/T10-14-tipos-amostra.md#implementação-tarefa-22)/[T10-16](../telas/T10-16-tipos-analise.md#implementação-tarefa-22) (código travado) e [T10-15](../telas/T10-15-status-amostra.md#implementação-tarefa-22); aceite em `AuxiliaresEspecificosTests` + navegador `CrudNavegadorTelasTests`. Os 20 auxiliares aparecem como cartões em `/Auxiliares` (item único "Auxiliares" no menu).
- **Passos:** uma `AuxiliarDefinicao` por tabela, conforme o MD de cada uma (campos, únicos, regras específicas). Atenção às regras especiais:
  - [T10-14](../telas/T10-14-tipos-amostra.md)/[T10-16](../telas/T10-16-tipos-analise.md): **código 1–99 imutável após uso** (validar `EXISTS` antes de permitir edição);
  - [T10-15](../telas/T10-15-status-amostra.md): ao menos um "permite resultado";
  - [T10-02](../telas/T10-02-tipos-endereco.md): finalidade;
  - [T10-11](../telas/T10-11-condicoes-pagamento.md): código obrigatório + grid de parcelas (Σ = 100%);
  - [T10-12](../telas/T10-12-prazos.md): quantidade de dias ≥ 0;
  - status: seletor de cor `#RRGGBB`.
- **Aceite:** checklist de cada MD T10-xx marcado; menu "Auxiliares" com os 20 itens.
- **Esforço:** M · **Depende de:** 2.1

### 2.3 Geografia + importação IBGE + componente de endereço
> ✅ **Concluída em 21/10/2026** — ver [T11-03 › Implementação](../telas/T11-03-cidades.md#implementação-tarefa-23) (e [T11-01](../telas/T11-01-paises.md#implementação-tarefa-23)/[T11-02](../telas/T11-02-estados.md#implementação-tarefa-23)); aceite em `GeografiaTests` + navegador `CrudNavegadorTelasTests.Geografia_pais_estado_cidade_e_importacao_ibge`.
> 🟨 **Parte feita em 20/10/2026:** 5.571 municípios IBGE no seed `06_cidades.sql`, `/Geografia/CidadesDaUf` (combo; o nome `/Geografia/Cidades` fica para a tela T11-03), `/Geografia/Cep/{cep}` (ViaCEP no servidor) e o componente de endereço/contatos (`_ContatosAbas`/`_ContatosModais`/`contatos.js`). Falta a tela de manutenção T11-01…03.
> 🔧 **20/10/2026:** o item *Cidades* aparecia no menu sem a tela existir (era o JSON do combo) — saiu do menu; volta quando a T11-03 for feita nesta tarefa. Ver [T11-03](../telas/T11-03-cidades.md).
- **Passos:** T11-01/02/03 (só ADMIN_GLOBAL altera); importação do CSV do IBGE; endpoint `/Geografia/Cep/{cep}` (ViaCEP no servidor, cache, timeout); componente Razor/JS "Endereço" reutilizável (usado depois em T12 e T03).
- **Aceite:** [aceite da T11-03](../telas/T11-03-cidades.md#critérios-de-aceite).
- **Esforço:** M

### 2.4 Componente "+" (cadastro rápido) nos lookups
> ✅ **Concluída em 21/10/2026** — ver [T10-00 › Implementação (2.4)](../telas/T10-00-crud-auxiliares.md#implementação-tarefa-24--cadastro-rápido-); aceite em `CadastroRapidoTests` + navegador `Cadastro_rapido_mais_inclui_e_seleciona_no_combo`. Sem Tom Select (o projeto usa `<select>` simples): o "+" é o atributo `lc-rapido` no combo. O aceite "idioma pelo +" foi provado com cargo e tipo de endereço — ainda não há combo de idioma (vem com a proposta, 4.2); mesmo componente.
- **Passos:** modal genérico que abre o formulário do auxiliar e devolve `{id, texto}` ao Tom Select de origem; usado na proposta (produto, método, idioma, prazo) e nos resultados (tipo de resultado).
- **Aceite:** incluir um idioma pelo "+" já deixa o item selecionado no combo.
- **Esforço:** S · **Depende de:** 2.1

### 2.5 Revisão
`/code-review`; atualizar diário.
> ✅ **Concluída em 21/10/2026** — `/code-review medium` de `f0c1ee7^..HEAD` (2.1–2.4 + T03/T12/T13/contatos antecipados): **10 achados**, verificados no código.

| # | Achado | Decisão |
|---|---|---|
| 1 | Contatos pela tela de clientes aceitavam qualquer entidade não-empresa (um profissional podia ter endereços/fones/e-mails alterados com só `CLIENTES.ALTERAR`) | ✅ corrigido: `ContatosService.PertenceAsync` usa o mesmo critério da lista da T12 (algum tipo comercial) — `Contatos_da_tela_de_clientes_nao_mexem_em_profissional` |
| 2 | A entidade da empresa usuária copia o CNPJ e ocupa o `UQ_ENT_DOC`: cliente com o CNPJ do próprio laboratório é recusado sem registro visível, e trocar o CNPJ da empresa para o de um cliente dá 1062 na trigger | ⏳ pendência 15 (exige `/mudanca-banco` e decisão do usuário) |
| 3 | Qualquer falha ao excluir oferecia "Inativar" ("está em uso") — inclusive "não encontrado" e regra do auxiliar | ✅ corrigido: só com `ErrosMySql.MensagemEmUso` (1451) — `Excluir_so_oferece_inativar_quando_o_registro_esta_em_uso` |
| 4 | CEP sem código IBGE virava `CODIGO_IBGE IS NULL` e pegava uma cidade digitada qualquer | ✅ corrigido em `ConsultaCepService` — caso novo em `Cep_liga_a_cidade_pelo_ibge…` |
| 5 | GUID da instância derruba todas as sessões em reciclagem do IIS e entre instâncias balanceadas | ⏳ pendência 16: comportamento **pedido pelo usuário** (2ª correção pós-teste); rever antes da virada (9.3) com o tipo de hospedagem |
| 6 | Regra do T10-15 bloqueava qualquer edição de status numa empresa sem status que permite resultado | ✅ corrigido: só barra quando a operação **tira** um que permitia (valores originais do EF) — caso novo em `Sempre_resta_um_status…` |
| 7 | Aviso "Este unidade está em uso" sem gênero | ✅ corrigido (`Feminino`) |
| 8 | Lista de empresas mostrava a cidade do 1º endereço, de qualquer tipo | ✅ corrigido: prefere o endereço de nota fiscal |
| 9 | Cidades da UF e combo de UF repetidos (controller + 3 serviços) | ⏳ `/simplify` (pendência 5) |
| 10 | Paginação/contagem de inativos e normalização de texto copiadas em 4 serviços | ⏳ `/simplify` (pendência 5) |

## Definição de pronto
- [x] 20 auxiliares + geografia por empresa (exceto geografia global), com cadastro rápido (2.2–2.4).
- [x] Códigos de tipo de amostra/análise protegidos (2.2).
- [x] CRUD de cada tela provado no navegador (P26) — `CrudNavegadorTests`/`CrudNavegadorTelasTests`: Idiomas, Tipos de endereço, tipos de amostra/status, parcelas, geografia, cadastro rápido (os demais auxiliares usam as mesmas views do genérico).
