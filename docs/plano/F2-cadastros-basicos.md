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
> 🟨 **Parte feita em 20/10/2026:** 5.571 municípios IBGE no seed `06_cidades.sql`, `/Geografia/CidadesDaUf` (combo; o nome `/Geografia/Cidades` fica para a tela T11-03), `/Geografia/Cep/{cep}` (ViaCEP no servidor) e o componente de endereço/contatos (`_ContatosAbas`/`_ContatosModais`/`contatos.js`). Falta a tela de manutenção T11-01…03.
> 🔧 **20/10/2026:** o item *Cidades* aparecia no menu sem a tela existir (era o JSON do combo) — saiu do menu; volta quando a T11-03 for feita nesta tarefa. Ver [T11-03](../telas/T11-03-cidades.md).
- **Passos:** T11-01/02/03 (só ADMIN_GLOBAL altera); importação do CSV do IBGE; endpoint `/Geografia/Cep/{cep}` (ViaCEP no servidor, cache, timeout); componente Razor/JS "Endereço" reutilizável (usado depois em T12 e T03).
- **Aceite:** [aceite da T11-03](../telas/T11-03-cidades.md#critérios-de-aceite).
- **Esforço:** M

### 2.4 Componente "+" (cadastro rápido) nos lookups
- **Passos:** modal genérico que abre o formulário do auxiliar e devolve `{id, texto}` ao Tom Select de origem; usado na proposta (produto, método, idioma, prazo) e nos resultados (tipo de resultado).
- **Aceite:** incluir um idioma pelo "+" já deixa o item selecionado no combo.
- **Esforço:** S · **Depende de:** 2.1

### 2.5 Revisão
`/code-review`; atualizar diário.

## Definição de pronto
- [ ] 20 auxiliares + geografia por empresa (exceto geografia global), com cadastro rápido.
- [ ] Códigos de tipo de amostra/análise protegidos.
- [ ] CRUD de cada tela provado no navegador (P26).
