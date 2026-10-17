# T03 — Empresas e Parâmetros da Empresa

**Fase:** 1 · **Rotas:** `/Empresas` (lista, só ADMIN_GLOBAL), `/Empresa/Dados`, `/Empresa/Parametros` · **Permissões:** `EMPRESAS.*`, `PARAMETROS_EMPRESA.*`
**Legado:** "Empresa Usuária" era o cadastro de Entidades com `TIPO_EMPRESA_USUARIA` (`un_Entidades`, `teEMP`); "Parâmetros do Sistema" (`un_Parametros.pas`) mostrava os últimos números (`ULT_COD_PROPOSTA`, `ULT_COD_AMOSTRA`, `ULT_PED_VENDA`, `ULT_COD_BOLETIM`, `ULT_COD_RE`).
**Tabelas:** `EMPRESAS`, `EMPRESAS_PARAMETROS`, `SEQUENCIAS_DOCUMENTOS`.

## Dados da empresa (`EMPRESAS`)
Código (3, único, imutável), Razão social*, Nome fantasia, CNPJ (validação de dígito, único), IE, IM, endereço (CEP com busca ViaCEP → logradouro, bairro, cidade IBGE), DDD/fone, e-mail, site, **logo** (PNG/JPG até 1 MB — usada no boletim/proposta/pedido), cabeçalho e rodapé do laudo, **nº de acreditação** (aparece no boletim acreditado), ativa.

## Nova empresa (ADMIN_GLOBAL)
1. Grava `EMPRESAS` → `CALL SP_INICIALIZAR_EMPRESA(id)` (status, idiomas, moedas, tipos, perfil ADMINISTRADOR, parâmetros).
2. Opção "Copiar cadastros de": empresa origem + checkboxes (Auxiliares, Métodos, Parâmetros de análise, Produtos, Condições de pagamento, Prazos, Tipos de resultado). **Clientes não são copiados.**
3. Vincular o usuário atual à nova empresa com perfil ADMINISTRADOR.

## Parâmetros (`EMPRESAS_PARAMETROS`)
| Campo | Uso |
|---|---|
| Status inicial da proposta | nova proposta e nova revisão |
| Status "Enviado ao cliente" | grava `DT_ENVIO_CLIENTE` (regra do Delphi) |
| Status inicial da amostra | recebimento |
| Status que permite resultados | filtro da tela de Resultados (legado `ID_AMOSTRAS_STATUS_RESULTADOS`) |
| Status inicial / emitido do boletim | revisão / emissão |
| Dias retroativos para data de entrega (padrão 7) | proteção contra ano digitado errado |
| Validade padrão da proposta (dias, padrão 30) | sugere `DT_VALIDADE` |
| Período padrão de pesquisa (dias, padrão 365) | filtros das telas (Delphi: `Data_Servidor - 365`) |
| Exige 2º responsável no boletim | `SP_EMITIR_BOLETIM` |
| Legenda padrão do boletim / Contrato padrão da proposta | textos sugeridos |

## Numeração (somente leitura + ajuste com auditoria)
Grid de `SEQUENCIAS_DOCUMENTOS` da empresa (tipo, ano, último número). Ajuste manual só por ADMIN_GLOBAL, nunca para um número **menor** que o máximo já usado (validar com `MAX(COD_*)`), com motivo gravado em `AUDITORIA`.

**Status:** ⬜
