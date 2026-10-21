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

> **Pendência 18 (21/10/2026, a investigar na 3.6):** no banco de testes (630 empresas) o `SP_INICIALIZAR_EMPRESA` levou **8,6 s em média** em 2.001 chamadas (`performance_schema`). Verificar se é o `PR_CRIAR_PERFIL`/permissões dos perfis crescendo com o número de empresas ou só a máquina saturada (CPU em 100% no dia). É o que deixa a suíte em 11–12 min.

