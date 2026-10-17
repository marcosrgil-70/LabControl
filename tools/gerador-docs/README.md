# Gerador da documentação dos objetos do banco

Gera `docs/banco/objetos/**` (um MD por tabela, view, rotina e trigger) a partir dos scripts `Scripts/v2/*.sql` e dos metadados de negócio.

## Uso
```bash
node tools/gerador-docs/gerar.mjs        # regenera docs/banco/objetos (sobrescreve)
node tools/gerador-docs/checar_links.mjs # valida links e âncoras de docs/**/*.md
```
Requer Node 18+ (sem dependências).

## Quando rodar
Sempre que um script de `Scripts/v2/` mudar (coluna, constraint, trigger, procedure, view). Depois, rode o verificador de links.

## Onde editar
| Arquivo | O que contém |
|---|---|
| `parse.mjs` | parser dos scripts SQL (tabelas, FKs, rotinas, triggers, views, mensagens 45000) |
| `meta_tabelas.mjs` | por tabela: finalidade, origem no legado, volume, etapa de migração, telas, processos, criticidade, regras de negócio, notas para o C# |
| `meta_outros.mjs` | o mesmo para views, rotinas e triggers |
| `nomes.mjs` | nomes das telas e processos usados nos links |
| `gerar.mjs` | montagem dos MDs e do índice `docs/banco/objetos/README.md` |

**Não edite à mão** os arquivos de `docs/banco/objetos/`: a próxima geração sobrescreve. Regras de negócio novas vão para `meta_*.mjs`.
As telas, processos, regras e o plano (`docs/telas`, `docs/processos`, `docs/regras`, `docs/plano`) são mantidos à mão.
