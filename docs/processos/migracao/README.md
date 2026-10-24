# Migração — Etapas M-01 a M-18

> Visão geral e princípios: [P24 Migração](../P24-migracao.md) · Documento base: [banco/13-MIGRACAO-DADOS.md](../../banco/13-MIGRACAO-DADOS.md) · Tela: [T07](../../telas/T07-migracao.md)

> **Situação (24/10/2026):** M-01…M-15 implementadas e conferidas contra o `DBMAIS.FDB` real (3.5 e 4.1), numa **execução só** pela T07 (~3 min; decisão do usuário). M-16…M-18 (resultados, boletins, sequências) na **5.1**. Cada MD traz a seção "Implementação" com os números reais e as anomalias.

| Etapa | Parte (fase) | Depende de | Origem → destino |
|---|---|---|---|
| [M-01 Empresa](M-01-empresa.md) | 1 (3.5) | — | EMPRESAS, ENTIDADES (TIPO_EMPRESA_USUARIA, entidade 4), ENTIDADES_PJ… → EMPRESAS |
| [M-02 Geografia (países, estados, cidades)](M-02-geografia.md) | 1 (3.5) | — | PAISES, ESTADOS, CIDADES → PAISES, ESTADOS, CIDADES |
| [M-03 Cadastros auxiliares](M-03-auxiliares.md) | 1 (3.5) | M-01 | LOGRADOUROS_TIPOS, ENDERECOS_TIPOS, FONES_TIPOS… → TIPOS_LOGRADOURO, ENDERECOS_TIPOS, FONES_TIPOS… |
| [M-04 Parâmetros da empresa](M-04-parametros-empresa.md) | 1 (3.5) | M-03 | EMPRESAS_PARAMETROS → EMPRESAS_PARAMETROS |
| [M-05 Entidades (clientes, fornecedores, funcionários)](M-05-entidades.md) | 1 (3.5) | M-01, M-03 | ENTIDADES, ENTIDADES_PF, ENTIDADES_PJ… → ENTIDADES |
| [M-06 Contatos (telefones e e-mails)](M-06-contatos.md) | 1 (3.5) | M-05 | ENTIDADES_FONES, ENTIDADES_EMAILS → ENTIDADES_FONES, ENTIDADES_EMAILS |
| [M-07 Endereços](M-07-enderecos.md) | 1 (3.5) | M-02, M-05 | ENTIDADES_ENDERECOS, LOGRADOUROS, CIDADES_BAIRROS → ENTIDADES_ENDERECOS |
| [M-08 Profissionais](M-08-profissionais.md) | 1 (3.5) | M-05, M-03 | ENTIDADES_FUNCIONARIOS, ENTIDADES_FUNC_ASSINATURAS → ENTIDADES_FUNCIONARIOS |
| [M-09 Usuários, empresas e perfis](M-09-usuarios.md) | 1 (3.5) | M-08, M-04 | USUARIO, SYS_*_ACESSOS → USUARIOS, USUARIOS_EMPRESAS, USUARIOS_PERFIS… |
| [M-10 Produtos e parâmetros de análise](M-10-produtos-parametros.md) | 1 (3.5) | M-03 | PRODUTOS, LAB_PARAMETROS_ANALISES, SYS_TABELAS_ID → PRODUTOS, LAB_PARAMETROS_ANALISES |
| [M-11 Propostas](M-11-propostas.md) | 2 (4.1) | M-06, M-07, M-08, M-03 | LAB_PROPOSTAS, LAB_PROPOSTAS_CONTRATOS, LAB_PROPOSTAS_OBSERVACAO… → LAB_PROPOSTAS, LAB_PROPOSTAS_TEXTOS |
| [M-12 Produtos e análises da proposta](M-12-produtos-analises-proposta.md) | 2 (4.1) | M-11, M-10 | LAB_PROPOSTAS_PRODUTOS, LAB_PROPOSTAS_PRODANALISES → LAB_PROPOSTAS_PRODUTOS, LAB_PROPOSTAS_PRODANALISES |
| [M-13 Pedidos de venda](M-13-pedidos-venda.md) | 2 (4.1) | M-12 | LAB_PROPOSTAS.ID_PEDIDOS_VENDAS → PEDIDOS_VENDA, PEDIDOS_VENDA_ITENS |
| [M-14 Amostras](M-14-amostras.md) | 2 (4.1) | M-12, M-13, M-08 | LAB_HIST_AMOSTRAS, LAB_HIST_AMOSTRAS_OBSERVACAO → LAB_HIST_AMOSTRAS |
| [M-15 Testes, estoque, arquivo e log](M-15-testes-estoque-arquivo.md) | 2 (4.1) | M-14 | LAB_HIST_AMOSTRAS_TESTES, LAB_MOV_AMOSTRAS, LAB_MOV_AMOSTRAS_PARAM… → LAB_HIST_AMOSTRAS_TESTES, LAB_MOV_AMOSTRAS, LAB_MOV_AMOSTRAS_PARAM… |
| [M-16 Resultados](M-16-resultados.md) | 3 (5.1) | M-15 | LAB_RESULTADOS_ANALISES, LAB_RESULTADOS_PARAM → LAB_RESULTADOS_ANALISES, LAB_RESULTADOS_PARAM |
| [M-17 Boletins](M-17-boletins.md) | 3 (5.1) | M-16 | LAB_BOLETIM_ANALISES, LAB_BOLETIM_OBS, LAB_BOLETIM_PARAMETROS → LAB_BOLETIM_ANALISES, LAB_BOLETIM_PARAMETROS |
| [M-18 Sequências de numeração](M-18-sequencias.md) | 3 (5.1) | M-11, M-13, M-14, M-10 | (máximos dos documentos migrados), SYS_TABELAS_ID → SEQUENCIAS_DOCUMENTOS |

## Conferências finais
Ver [banco/13 §3](../../banco/13-MIGRACAO-DADOS.md#3-conferências-obrigatórias-relatório-final-da-tela).
