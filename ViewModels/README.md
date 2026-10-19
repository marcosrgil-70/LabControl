# ViewModels — modelos das telas

Classes que a View recebe e o formulário devolve (`namespace LabControl.ViewModels`, já importado em `Views/_ViewImports.cshtml`). Entidades (`Models/`) **não** vão direto para o formulário: evita gravar colunas que a tela não mostra (`ID_EMPRESAS`, número do documento, colunas preenchidas pelo banco).

- Um arquivo por tela ou por grupo pequeno: `AmostraVM` (formulário), `AmostraFiltroVM` (consulta), `AmostraListaItemVM` (linha da grade).
- Validações de tela por Data Annotations com mensagens em português; regras que dependem do banco ficam no serviço/trigger.
- Datas `DateOnly`/`TimeOnly`, como nas entidades.
- Combos: `IEnumerable<SelectListItem>` ou carregados por AJAX (Tom Select — [arquitetura §6](../docs/01-ARQUITETURA-V2.md#6-padrões-de-ui-iguais-em-todas-as-telas)).
