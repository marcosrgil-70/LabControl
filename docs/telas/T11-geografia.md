# T11 — Países, Estados e Cidades

**Fase:** 2 · **Rota:** `/Geografia` · **Permissão:** `GEOGRAFIA.*` · **Tabelas globais:** `PAISES`, `ESTADOS`, `CIDADES`
**Legado:** `un_Paises`, `un_Estados`, `un_Cidades`, `un_Bairros`, `un_Logradouros` (Standards). Bairros e logradouros eram cadastros obrigatórios antes do endereço — **descontinuados** (decisão D-07: endereço com logradouro/bairro em texto).

## Regras
- São as **únicas tabelas de cadastro globais** (dados IBGE/BACEN iguais para todas as empresas). Só ADMIN_GLOBAL inclui/altera; demais usuários consultam.
- Carga inicial: seed com Brasil + 27 UFs; importar a lista IBGE de municípios (CSV) por um botão "Importar IBGE".
- Cidade: Estado*, Nome*, Código IBGE (7, único).
- Componente reutilizável **Endereço** (usado em Clientes, Empresa): CEP → busca ViaCEP (servidor) → preenche logradouro, bairro, cidade (pelo IBGE) e UF; usuário completa número/complemento. Funciona também sem internet (digitação manual + combo de cidade por UF).

**Status:** ⬜
