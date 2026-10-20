# Gerador de `Scripts/v2/06_cidades.sql`

Municípios do Brasil (código IBGE de 7 dígitos, nomes em maiúsculas como no legado), ligados às UFs de
`05_seed.sql` pelos 2 primeiros dígitos do código (`ESTADOS.CODIGO_IBGE`).

```bash
curl -s -o municipios.json https://servicodados.ibge.gov.br/api/v1/localidades/municipios
node tools/gerador-cidades/gerar.mjs municipios.json Scripts/v2/06_cidades.sql
```

Depois: atualizar a contagem `CIDADES` em `Scripts/v2/recriar.ps1` (`$esperado`) e seguir a skill `/mudanca-banco`.
O arquivo baixado não vai para o repositório (só o SQL gerado).
