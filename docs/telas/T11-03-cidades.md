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
- [ ] CEP válido preenche o endereço e a cidade correta (pelo IBGE).
- [ ] Sem internet o endereço pode ser digitado.
- [ ] Importação IBGE idempotente.

**Status:** ⬜
