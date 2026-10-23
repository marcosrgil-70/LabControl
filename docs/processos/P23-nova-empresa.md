# P23 — Nova Empresa e Cópia de Cadastros

**Criticidade:** 🟠 · **Fase:** 1
**Telas:** [T03 Empresa usuária](../telas/T03-empresa-usuaria.md), [T05 Parâmetros](../telas/T05-parametros-empresa.md), [T04 Perfis](../telas/T04-perfis-permissoes.md)
**Objetos:** [`EMPRESAS`](../banco/objetos/tabelas/EMPRESAS.md), [`SP_INICIALIZAR_EMPRESA`](../banco/objetos/rotinas/SP_INICIALIZAR_EMPRESA.md), [`EMPRESAS_PARAMETROS`](../banco/objetos/tabelas/EMPRESAS_PARAMETROS.md), [`PERFIS`](../banco/objetos/tabelas/PERFIS.md), todas as tabelas auxiliares
**Regras:** [RN-01](../regras/RN-01-gerais-multiempresa.md)

## Fluxo
1. ADMIN_GLOBAL preenche os dados da empresa (T03) → INSERT `EMPRESAS`.
2. `CALL SP_INICIALIZAR_EMPRESA(id)` (idempotente): status de amostra/análise/boletim/proposta, idiomas, moedas, tipos de endereço (com finalidade), fone, e-mail, registro profissional, tipos de logradouro, `EMPRESAS_PARAMETROS` com os status padrão e perfil ADMINISTRADOR com todas as permissões.
3. **Copiar cadastros de outra empresa** (opcional; rotina nova em `EmpresaService.CopiarCadastros`, numa transação):
   | Grupo | Tabelas | Observação |
   |---|---|---|
   | Auxiliares | todos de [T10](../telas/T10-00-crud-auxiliares.md) | casar por descrição/código; não duplicar o que o `SP_INICIALIZAR_EMPRESA` já criou |
   | Métodos | `ANALISES_METODOS` | — |
   | Parâmetros de análise | `LAB_PARAMETROS_ANALISES` | remapear tipo de análise e método pelos novos IDs |
   | Produtos | `PRODUTOS` | remapear unidade e embalagem; manter o código |
   | Condições de pagamento / Prazos / Tipos de resultado | idem | — |
   - **Nunca** copiar entidades (clientes, profissionais), propostas, amostras, resultados, boletins nem sequências.
   - `AMOSTRAS_TIPO.CODIGO` e `ANALISES_TIPO.CODIGO` copiados iguais (a nova empresa numera as próprias amostras a partir de 1).
4. Vincula o usuário criador (`USUARIOS_EMPRESAS` + perfil ADMINISTRADOR).
5. `AUDITORIA` com o resumo da cópia (quantidades por tabela).

## Implementação (tarefa 1.5)
Ver [T03 › Implementação](../telas/T03-empresa-usuaria.md#implementação-tarefa-15). A cópia é `Services/CopiaCadastros.cs` (em vez de `EmpresaService.CopiarCadastros`): grupos AUXILIARES, METODOS, PARAMETROS, PRODUTOS, CONDICOES, PRAZOS, TIPOS_RESULTADO; casa por descrição/código/sigla (as chaves únicas do banco; `ANALISES_METODOS` pela descrição); o resumo vai para `AUDITORIA` (ação I em `EMPRESAS`, `COPIA_CADASTROS`). Testes em `EmpresaServiceTests`.

## Cenários de teste
- Empresa nova sem cópia: consegue cadastrar cliente/proposta/amostra (status e parâmetros existem).
- Com cópia: parâmetros apontam para tipos/métodos **da nova empresa** (nunca para IDs da origem).
- Primeira amostra da nova empresa = `…-00001-…`.

> **Pendência 18 (21/10/2026; diagnóstico em 22/10/2026, correção na 3.6):** no banco de testes o `SP_INICIALIZAR_EMPRESA` chegou a **8,6 s em média** (2.001 chamadas, `performance_schema`), mas **isolada leva 0,04 s** (785 empresas, 152 mil linhas em `PERFIS_PERMISSOES`) — não cresce com o número de empresas. A demora é **espera de lock**: com a suíte inteira e o `MigracaoParte1Tests` em paralelo, 16 testes estouraram 30 s entre os minutos 14 e 18; o monitor (`sys.innodb_lock_waits`) viu esperas no `SEQUENCIAS_DOCUMENTOS` (`SP_PROXIMO_NUMERO`) e `commit` lento, com a máquina sem memória livre. Rodando a migração à parte, a suíte passa 175/175. A fazer: isolar o teste da migração (coleção xUnit sem paralelismo) e medir de novo a suíte inteira; se ainda houver espera, investigar quem segura o lock em `SEQUENCIAS_DOCUMENTOS`.

