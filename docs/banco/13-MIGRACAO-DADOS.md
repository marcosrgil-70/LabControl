# 13 — Migração de Dados Firebird → MySQL v2

> Origem: `C:\Project\Laboratorio\DBMAIS.FDB` (Firebird 2.5, servidor local `localhost:3050`, SYSDBA).
> Destino: `labcontrol_v2` criado pelos scripts `Scripts/v2/01..05`.
> Implementação: reescrever `Controllers/MigracaoController.cs` como **serviço** (`Services/Migracao/MigracaoService.cs`) + tela `/Migracao` (só ADMIN_GLOBAL). A migração da v1 (doc `_obsoleto_v1/18-migracao.md`) está **errada** (achata produtos da proposta, não migra resultados/boletins, endereços incompletos) e será descartada.

## 1. Regras gerais

1. Executar numa conexão com `SET @LC_MIGRACAO = 1;` (desliga numeração automática e validações; os números originais são preservados).
2. Todo registro migrado vai para a **empresa 1** (`COD_EMPRESA = '001'`), a única do legado.
3. Guardar o ID antigo em `ID_LEGADO` e manter dicionários em memória `idAntigo → idNovo` para cada tabela.
4. Uma transação por **etapa** (não uma única transação gigante) com log de etapa; a migração pode ser repetida do zero (`DROP DATABASE labcontrol_v2` + scripts).
5. Ao final: `SET @LC_MIGRACAO = 0;`, inicializar `SEQUENCIAS_DOCUMENTOS` com os máximos por ano e rodar as **conferências** (seção 4).
6. Strings: Firebird ISO8859_1 → UTF-8 (FirebirdSql.Data.FirebirdClient com `Charset=ISO8859_1`), aplicar `Trim()`.
7. Booleanos `'S'/'N'` → 1/0 (nulo → 0).

## 2. Ordem das etapas

| # | Etapa | Origem → Destino | Regras específicas |
|---|---|---|---|
| M-01 | Empresa | `EMPRESAS` + `ENTIDADES`/`PJ`/endereço da entidade 4 → `EMPRESAS` | Identidade da empresa usuária vai para `EMPRESAS`; a entidade 4 migra como entidade da empresa (`TIPO_EMPRESA_USUARIA`, D-19) e `EMPRESAS.ID_ENTIDADES` aponta para ela (contatos em M-06/M-07). Chamar `SP_INICIALIZAR_EMPRESA(1)` **depois** de migrar os auxiliares (para não duplicar status) |
| M-02 | Geografia | `PAISES`, `ESTADOS`, `CIDADES` → idem | Casar UF por sigla (o seed já tem as 27); cidades: o seed `06_cidades.sql` já traz os 5.571 municípios do IBGE; as 190 do legado (sem código IBGE) são casadas por **nome + UF** e as não encontradas vão para a conferência |
| M-03 | Auxiliares | `LOGRADOUROS_TIPOS`, `ENDERECOS_TIPOS` (+FINALIDADE por ID 1..4), `FONES_TIPOS`, `EMAILS_TIPOS`, `TIPOS_REG_PROFISSIONAL`, `CARGO_FUNCIONARIOS`, `IDIOMAS`, `UNIDADES`, `EMBALAGENS_TIPOS`, `LAB_TIPOS_RESULTADOS`, `CONDICOES_PAGTOS` (`INATIVO` 0/1 → `ATIVO`), `PRAZOS` (`QTDE`→`QTDE_DIAS`), `MOEDAS`, `AMOSTRAS_STATUS` (`PERMITE_RESULTADO = ID = EMPRESAS_PARAMETROS.ID_AMOSTRAS_STATUS_RESULTADOS`), `ANALISES_STATUS`, `BOLETINS_STATUS`, `LAB_PROPOSTAS_STATUS`, `ANALISES_METODOS` | Cores TColor → `#RRGGBB` (doc 11 §4). **`AMOSTRAS_TIPO.CODIGO = ID antigo`** e **`ANALISES_TIPO.CODIGO = ID antigo`** (o número da amostra continua igual). Idioma "---" migra |
| M-04 | Parâmetros da empresa | `EMPRESAS_PARAMETROS` → idem | Mapear os IDs de status; `ULT_*` **não** migram (vão para `SEQUENCIAS_DOCUMENTOS` em M-16) |
| M-05 | Entidades | `ENTIDADES` + `ENTIDADES_PF` + `ENTIDADES_PJ` + `ENTIDADES_OBSERVACOES` → `ENTIDADES` | `CPF_CNPJ` = CPF ou CNPJ; documentos duplicados na mesma empresa → sufixo no log e `CPF_CNPJ` NULL no segundo (listar); `ESTADO_CIVIL` 0..4 → S,C,D,P,V |
| M-06 | Contatos | `ENTIDADES_FONES`, `ENTIDADES_EMAILS` | Mantendo o mapa de IDs (os e-mails são FKs de contato nas propostas) |
| M-07 | Endereços | `ENTIDADES_ENDERECOS` ⨝ `LOGRADOUROS` ⨝ `CIDADES_BAIRROS` ⨝ `CIDADES` | `LOGRADOURO = NOME_LOGRADOURO`, `BAIRRO = descrição do bairro`, `ID_TIPOS_LOGRADOURO` pelo tipo do logradouro, `ID_CIDADES` pelo IBGE |
| M-08 | Profissionais | `ENTIDADES_FUNCIONARIOS` (+`ENTIDADES_FUNC_ASSINATURAS`) | `ASSINA_BOLETIM = 1` para quem já aparece como RESP/RESP2 em boletins; `EXECUTA_ANALISE = 1` |
| M-09 | Usuários | `USUARIO` → `USUARIOS` + `USUARIOS_EMPRESAS` (empresa 1, `ID_ENTIDADES_FUNC` mapeado) + `USUARIOS_PERFIS` | `LOGIN = USUNOM`; `SENHA_HASH` = PBKDF2 de senha provisória (exibida 1 vez no relatório da migração) e `TROCAR_SENHA = 1`; `USUADM = 1` → perfil ADMINISTRADOR; demais → perfil "OPERADOR" (a ajustar) ; `USUCOD = 1` (ADMINISTRADOR) → `ADMIN_GLOBAL = 1` |
| M-10 | Produtos e parâmetros | `PRODUTOS`, `LAB_PARAMETROS_ANALISES` | `DESC_REDUZIDA` vazia → `DESCRICAO`; `SEQUENCIAS_DOCUMENTOS('PRODUTO',0) = SYS_TABELAS_ID.ID` |
| M-11 | Propostas | `LAB_PROPOSTAS` + 5 tabelas de texto | `ANO = 2000 + ANO`; `ID_ENT_END_COBRANCA = COALESCE(ID_END_ENT_COBRANCA, ID_ENT_END_NF)` (validar que o ID é endereço da entidade de cobrança); `NR_PROPOSTA` calculado; por (empresa, ano, cod): `ID_LAB_PROPOSTAS_ORIGEM` = revisão anterior e `ULTIMA_REVISAO = 1` só na maior. Campos NOT NULL ausentes no legado (`ID_ENT_END_LAUDO`, `ID_ENT_CONTATO_RESULTADO`, `ID_ENT_COBRANCA`) → usar o primeiro endereço/contato do cliente e listar no log |
| M-12 | Produtos/análises da proposta | `LAB_PROPOSTAS_PRODUTOS` (ITEM = antigo `ID_LAB_PROPOSTAS_PRODUTOS`), `LAB_PROPOSTAS_PRODANALISES` | `TIPO_DOCUMENTO` em maiúsculas (BOLETIM/RELATORIO); valores copiados (triggers desligadas) |
| M-13 | Pedidos de venda | `DISTINCT (empresa, COD, ANO, ID_PEDIDOS_VENDAS)` das propostas | 1 pedido por proposta: `NR_PEDIDO = ID_PEDIDOS_VENDAS`, `ID_LAB_PROPOSTAS` = maior revisão que tem o número, `DT_PEDIDO = DT_AUTORIZACAO`, `SITUACAO = 'CONCLUIDO'` se todas as amostras têm boletim, senão `'EM_EXECUCAO'`; itens = produtos dessa revisão |
| M-14 | Amostras | `LAB_HIST_AMOSTRAS` + `_OBSERVACAO` | `ANO = 2000 + ANO` (0 → ano da entrega); `ID_LAB_PROPOSTAS_PRODUTOS` por (proposta, item); `ID_PEDIDOS_VENDA` pelo número; `NR_AMOSTRA` = `FN_NR_AMOSTRA_ID`; por (empresa, ano, cod): `ID_AMOSTRA_RAIZ` = revisão 0, `ID_AMOSTRA_ORIGEM` = revisão anterior, `ULTIMA_REVISAO` na maior; revisões sem `COLETOR`/`DATAHORACOLETA` copiam da raiz |
| M-15 | Testes, estoque e arquivo | `LAB_HIST_AMOSTRAS_TESTES` (`ID_TESTE_ORIGEM` = teste equivalente da revisão anterior por (parâmetro, método), quando único), `LAB_MOV_AMOSTRAS` (`AMOSTRA_COMPLEMENTAR='A'` → `AUTOMATICA=1`; movimentos de revisões > 0 **descartados**: eram entradas duplicadas), `LAB_MOV_AMOSTRAS_PARAM`, `LAB_LOCAL_AMOSTRAS` (`STATUS 0→ARQUIVADA, 1→DESCARTADA`; aponta para a raiz; se houver 2, fica o mais recente) | Depois: `CALL PR_ATUALIZA_SALDO(id)` para cada raiz; `LAB_HIST_AMOSTRAS_LOG` migra como JSON (`ACAO` I/A/E) — opcional, 10.509 linhas |
| M-16 | Resultados | `LAB_RESULTADOS_ANALISES`, `LAB_RESULTADOS_PARAM` | `REVISAO` renumerada 0..n por amostra raiz (ordem: revisão antiga, ID); `ULTIMA_REVISAO` na maior; `ID_RESULTADO_ORIGEM` = anterior; `ID_PARAM_ORIGEM` por teste equivalente; `VR_EXP_*` → inteiro; recalcular `RESULTADO_FORMATADO`/`ESPECIF_FORMATADA` com as funções (UPDATE em lote após a carga) |
| M-17 | Boletins | `LAB_BOLETIM_ANALISES` + `LAB_BOLETIM_OBS` + `LAB_BOLETIM_PARAMETROS` | `COD_BOLETIM` = número base da amostra (sem " Rnn"); `REVISAO` renumerada 0..n por código (ordem `REVISAO`, ID); `ULTIMA_REVISAO` na maior; `DH_GERACAO` nulo → `DT_BOLETIM`; **`DH_EMISSAO = DH_GERACAO`** (todo o histórico fica congelado — D-11); `ID_BOLETINS_STATUS` = "Enviado ao cliente"; `RESULTADO_CONCAT` mantido como estava impresso |
| M-18 | Sequências | — | `SEQUENCIAS_DOCUMENTOS`: `AMOSTRA` por ano = `MAX(COD_AMOSTRA)`; `PROPOSTA` por ano = `MAX(COD_PROPOSTA)` (ignorar os 12 com COD = ID); `PEDIDO_VENDA` = `MAX(NR_PEDIDO)`; `PRODUTO` = `SYS_TABELAS_ID` |

## 3. Conferências obrigatórias (relatório final da tela)

| Conferência | Esperado (legado) |
|---|---|
| Entidades / e-mails / fones / endereços | 527 / 664 / 695 / 538 |
| Produtos / parâmetros / métodos | 1.915 / 462 / 176 |
| Propostas / produtos / análises | 2.358 / 5.761 / 19.252 |
| Pedidos de venda | nº distintos de `ID_PEDIDOS_VENDAS` por proposta (≈ 1.435 = 1.454 − repetições) |
| Amostras / testes | 5.770 / 20.551 |
| Resultados / parâmetros | 5.345 / 19.528 |
| Boletins / parâmetros | 5.312 / 19.615 |
| Soma `VR_TOTAL_PROPOSTA` por ano | igual ao legado (centavos) |
| Amostras por ano (2013…2018) | 62 / 611 / 511 / 663 / 2.482 / 1.440 (+1 com ano 0) |
| `SELECT NR_AMOSTRA` de 20 amostras sorteadas | idêntico ao `VW_LAB_HIST_AMOSTRA.NR_AMOSTRA` do legado |
| Amostras com `ULTIMA_REVISAO = 1` por (empresa, ano, cod) | exatamente 1 |
| Boletins com `ULTIMA_REVISAO = 1` por código | exatamente 1 |
| Saldo das raízes | igual a `LAB_HIST_AMOSTRAS_SALDO` do legado |
| Anomalias listadas (doc 10 §8) | todas presentes no log |

## 4. Após a migração

1. `SET @LC_MIGRACAO = 0`.
2. Testar login com um usuário migrado (troca de senha obrigatória).
3. Rodar o roteiro de teste do doc 12 §7 numa **cópia** do banco migrado.
4. Backup `mysqldump --routines --triggers labcontrol_v2`.
