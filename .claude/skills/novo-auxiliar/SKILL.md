---
name: novo-auxiliar
description: Inclui um cadastro auxiliar do LabControl (T10-01…T10-20 — tipos, status, unidades, prazos, moedas…) no CRUD genérico aprovado na 2.1 com Idiomas — uma entrada no CatalogoAuxiliares, regras específicas nos ganchos, mensagem de duplicidade, testes e docs. Use na tarefa 2.2 ou sempre que pedirem "cadastro de <auxiliar>", "tela de tipos de …", "replicar auxiliar", ou /novo-auxiliar <TABELA>.
---

# Novo cadastro auxiliar (padrão T10-00)

Responda em português do Brasil. Argumento: a tabela (ex.: `UNIDADES`) ou a tela (ex.: `T10-08`).
O controller (`Controllers/AuxiliaresController.cs`), o serviço (`Services/Auxiliares/AuxiliarService.cs`) e as views (`Views/Auxiliares/`) são **os mesmos para todos** — não crie controller nem view por auxiliar.

## 1. Ler antes
1. MD da tela `docs/telas/T10-xx-*.md` (campos, regras específicas, seed, onde é usado).
2. MD da tabela `docs/banco/objetos/tabelas/<TABELA>.md` (colunas, `UNIQUE`, FKs).
3. Entidade gerada `Models/<Entidade>.g.cs` (nomes das propriedades; **não editar `*.g.cs`**).

## 2. Entrada no catálogo (`Services/Auxiliares/AuxiliarDefinicao.cs` › `CatalogoAuxiliares.Todos`)
```csharp
new AuxiliarDefinicao<Unidade>
{
    Tabela = "UNIDADES", Titulo = "Unidades de medida", Singular = "unidade", Feminino = true, Icone = "bi-rulers",
    Ajuda = "<uma linha: para que serve / onde é usado>",
    Campos = [Descricao, new("Sigla", "Sigla", Busca: true, Maiusculas: true)],
},
```
- `Tabela` = nome da tabela (rota `/Auxiliares/UNIDADES`); `Singular` minúsculo; `Feminino` acerta "gravada/excluída".
- `Campos` na ordem do formulário; o 1º recebe o erro de duplicidade. Tamanho e obrigatoriedade **vêm do modelo do EF** — não repita.
- Tipos: `Texto` (padrão), `Inteiro` (`Minimo`/`Maximo`), `Logico`, `Cor` (`#RRGGBB`, badge na lista), `Opcoes` (enum da propriedade; rótulo = nome do enum em frase).
- `Busca: true` nos textos pesquisáveis; `NaLista: false` para não virar coluna; `CampoTexto` se o texto do combo não for `Descricao`.
- Ícone: Bootstrap Icons (`bi-…`).

## 3. Regras específicas (ganchos)
- `ValidarAntesDeGravar = async (db, e) => …` → `Resultado.Erro("mensagem", "Propriedade")` (também roda ao inativar/reativar). Ex.: T10-15 "ao menos um status com permite resultado".
- `PodeAlterarCampo = async (db, e, campo) => …` → `false` bloqueia o campo no formulário (cadeado) e na gravação. Ex.: T10-14/T10-16 código 1–99 imutável depois de usado (`EXISTS` na amostra).
- Regra que o banco já garante (trigger 45000, `UNIQUE`, FK) **não** se repete no C#.
- Grade de filhos (T10-11 parcelas) não cabe no genérico: tela própria seguindo o mesmo visual.

## 4. Mensagem de duplicidade
`Infra/ErrosMySql.cs`: a `UNIQUE` (ID_EMPRESAS, DESCRICAO) já está em `UnicosDescricao`; uma `UNIQUE` de código/sigla precisa de texto próprio em `Unicos` (ex.: `UQ_UNIDADE` → "Já existe uma unidade com esta sigla."). Confira o nome da constraint no MD da tabela.

## 5. Testes (`tests/LabControl.Tests/AuxiliaresTests.cs`)
O padrão já está coberto com Idiomas (incluir/alterar/inativar/excluir, duplicado, em uso, busca/paginação, empresas, tela, permissões, cadastro rápido). Para cada auxiliar novo, acrescente um `[Theory]`/`[Fact]` só com o **específico**: campo extra gravado e listado, mensagem de duplicidade do código/sigla, e cada gancho (positivo e negativo, com a mensagem exata).

## 6. Conferir e documentar
- `dotnet build` sem avisos; `dotnet test tests/LabControl.Tests --filter AuxiliaresTests`.
- Abrir `/Auxiliares` (o cartão aparece sozinho) e `/Auxiliares/<TABELA>`.
- MD da tela: critérios `[x]`, `**Status:** ✅`; matriz `docs/plano/MATRIZ-DELPHI-CSHARP.md` ⬜ → ✅.
- Ao terminar a tarefa do plano, siga a skill `concluir-tarefa`.
