# T11-04 — Bairros e Logradouros (descontinuados)

**Fase:** — (não será implementada) · **Criticidade:** ⚪
**Legado:** `Standards/un_Bairros.pas` (*Cadastros Básicos → Bairros*: Cidade, Descrição — tabela `CIDADES_BAIRROS`, 485 linhas) e `Standards/un_Logradouros.pas` (*Cadastros Básicos → Logradouros*: Cidade, Bairro, Tipo, Nome — tabela `LOGRADOUROS`, 552 linhas).
**Objetos do banco (destino dos dados):** [`ENTIDADES_ENDERECOS`](../banco/objetos/tabelas/ENTIDADES_ENDERECOS.md) (colunas `LOGRADOURO`, `BAIRRO`, `ID_TIPOS_LOGRADOURO`, `ID_CIDADES`)
**Decisão:** D-07 de [banco/11-MODELO-MYSQL.md](../banco/11-MODELO-MYSQL.md)

## Por que não existe no v2
- No Delphi o endereço da entidade apontava para `ID_LOGRADOUROS`; para cadastrar um endereço era preciso cadastrar antes o bairro e o logradouro (botões "+" `Btn_incBairro`/`Btn_incLogra` na tela de entidades).
- No v2 o endereço guarda logradouro e bairro **em texto** e a cidade pelo IBGE, preenchidos pela busca de CEP (componente da [T11-03](T11-03-cidades.md)).

## Migração (M-07)
`ENTIDADES_ENDERECOS ⨝ LOGRADOUROS ⨝ CIDADES_BAIRROS ⨝ CIDADES` → `LOGRADOURO = NOME_LOGRADOURO`, `BAIRRO = descrição do bairro`, `ID_TIPOS_LOGRADOURO` pelo tipo do logradouro, `ID_CIDADES` pelo código IBGE.

## Critério de aceite
- [ ] Os 538 endereços migrados com logradouro, bairro e cidade preenchidos (listar os que ficarem sem cidade).
