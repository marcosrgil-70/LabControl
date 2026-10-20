# T11-03 — Cidades (+ componente de endereço com CEP)

**Fase:** 2 · **Rota:** `/Geografia/Cidades` · **Permissão:** `GEOGRAFIA.*` (inclusão/alteração/importação só ADMIN_GLOBAL) · **Criticidade:** 🟠 (base de todos os endereços)
**Legado:** `Standards/un_Cidades.pas` (menu *Cadastros Básicos → Cidades*) — País, Estado, Nome, Código IBGE. 190 registros.
**Objetos do banco:** [`CIDADES`](../banco/objetos/tabelas/CIDADES.md), [`ESTADOS`](../banco/objetos/tabelas/ESTADOS.md), [`VW_ENTIDADES_ENDERECOS`](../banco/objetos/views/VW_ENTIDADES_ENDERECOS.md) · **Regras:** [RN-03](../regras/RN-03-cadastros.md)

## Campos
| Campo | Coluna | Regra |
|---|---|---|
| Estado* | `ID_ESTADOS` | combo (filtra por país) |
| Nome* | `NOME` | até 150 |
| Código IBGE | `CODIGO_IBGE` | 7 dígitos, **único** |

## Importar IBGE
Botão "Importar municípios IBGE" (CSV oficial: código, nome, UF): insere os que faltam e atualiza nomes pelo código; relatório de incluídos/atualizados. Recomendado **antes** da migração (M-02).

## Componente reutilizável "Endereço" (usado em T12 Clientes e T03 Empresa)
1. CEP com máscara → botão/blur chama **ViaCEP no servidor** (`/Geografia/Cep/{cep}`, com cache e timeout de 3 s).
2. Preenche logradouro, bairro, tipo de logradouro (pela primeira palavra, se existir em `TIPOS_LOGRADOURO`), cidade (pelo `CODIGO_IBGE` devolvido) e UF.
3. Usuário completa número/complemento.
4. Sem internet ou CEP não encontrado: digitação manual + combo de cidade filtrado pela UF.
- Substitui o fluxo do legado, que exigia cadastrar **bairro e logradouro antes** do endereço (T11-04).

## Critérios de aceite
- [x] CEP válido preenche o endereço e a cidade correta (pelo IBGE) — `GET /Geografia/Cep/{cep}` (ViaCEP no servidor, cache 1 dia, separa o tipo de logradouro), usado no componente de endereço ([P25](../processos/P25-cadastro-entidades-contatos.md)).
- [x] Sem internet o endereço pode ser digitado (mensagem "Serviço de CEP indisponível…" e preenchimento manual).
- [x] Importação IBGE idempotente — feita como **seed** `Scripts/v2/06_cidades.sql` (5.571 municípios, `INSERT IGNORE` pelo código IBGE; gerador `tools/gerador-cidades`). A tela de manutenção de países/estados/cidades continua na 2.3.

> **20/10/2026 (relato do usuário: "cadastro de cidades não funciona"):** a tela ainda não existe (2.3), mas o item *Cidades* aparecia no menu porque o JSON do combo se chamava `/Geografia/Cidades` e o menu o tomava pela tela. O JSON passou a `/Geografia/CidadesDaUf`; o item volta ao menu quando a tela for feita. Teste: `PermissoesMenuPainelTests.Menu_real_sem_tela_inexistente_e_com_uma_entrada_de_empresa`.

**Status:** 🟨 cidades IBGE, busca de CEP e componente de endereço prontos em 20/10/2026 (P25); falta a tela de manutenção T11-01…03 (2.3).
