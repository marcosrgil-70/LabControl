# Services — regras de negócio

Um serviço por assunto (`AmostraService`, `PropostaService`, `BoletimService`…), `namespace LabControl.Services`, registrado como `AddScoped` no `Program.cs`. Controllers são finos: validam o formulário, chamam o serviço e devolvem View/JSON.

- Recebem por construtor `LabDbContext`, `IEmpresaContexto`, `Procedures` e `IAuditoria` conforme a necessidade.
- Retornam `Resultado` / `Resultado<T>` (`Infra/Resultado.cs`); o controller aplica com `this.AplicarNoFormulario(r)` ou `this.AplicarNoTempData(r)` (`Infra/ResultadoMvc.cs`).
- Erros do MySQL de negócio (45000, 1062, 1451, 1452) viram `Resultado.Erro` por `ErrosMySql.MensagemDeNegocio` / `CapturarAsync` — padrão completo em [P22](../docs/processos/P22-erros-transacoes.md#padrão-de-serviço-c).
- Procedures só por `Infra/Procedures.cs`; as de revisão/emissão (transação própria) **fora** de `BeginTransaction`.
- Nunca calcular número de documento nem ano (`MAX+1`): o banco gera e o EF relê.
- Não filtrar empresa à mão nem usar `IgnoreQueryFilters()`: o filtro global já faz ([P01](../docs/processos/P01-multiempresa.md)).
- Testes em `tests/LabControl.Tests` (use `Cenario` para montar os dados).
