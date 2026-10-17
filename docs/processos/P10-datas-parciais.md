# P10 — Datas Parciais (Fabricação e Validade)

**Criticidade:** 🟠 (particularidade do legado mantida) · **Fase:** 4
**Telas:** [T22 Recebimento](../telas/T22-recebimento-amostras.md), [T25 Etiquetas](../telas/T25-etiquetas-amostras.md), [T27 Boletim](../telas/T27-boletim.md), [T23 Movimentação](../telas/T23-movimentacao-amostras.md)
**Objetos:** [`LAB_HIST_AMOSTRAS`](../banco/objetos/tabelas/LAB_HIST_AMOSTRAS.md) (`FABRICACAO_DIA/MES/ANO`, `VALIDADE_DIA/MES/ANO`, `DT_FABRICACAO`, `DT_VALIDADE`), [`FN_DATA_PARCIAL`](../banco/objetos/rotinas/FN_DATA_PARCIAL.md), [`TG_AMOSTRAS_BI`](../banco/objetos/triggers/TG_AMOSTRAS_BI.md), [`TG_AMOSTRAS_BU`](../banco/objetos/triggers/TG_AMOSTRAS_BU.md)
**Regras:** [RN-05](../regras/RN-05-amostras.md)

## Por que existe
Rótulos de produtos trazem muitas vezes só o ano ou mês/ano de fabricação/validade. O Delphi tinha **três combos independentes** (dia 1–31, mês por extenso, ano 2100…1990) e gravava dia, mês e ano em colunas separadas (0 = não informado).

## Combinações válidas
| Informado | Exemplo gravado | Exibição/impressão | `DT_*` |
|---|---|---|---|
| nada | todos NULL | vazio (etiqueta: `---` na validade) | NULL |
| só ano | ano = 2027 | `2027` | NULL |
| mês + ano | mês = 5, ano = 2027 | `05/2027` | NULL |
| dia + mês + ano | 15/05/2027 | `15/05/2027` | 2027-05-15 |
| dia sem mês ou sem ano | — | **recusado**: "Validade: informe mês e ano quando informar o dia." | — |
| mês sem ano | — | **recusado**: "… informe o ano quando informar o mês." | — |
| dia inexistente (31/02) | — | **recusado**: "Data de validade inválida." | — |

## Onde cada regra fica
- **Tela**: três selects (dia vazio/1–31, mês vazio/Janeiro…Dezembro, ano vazio/ano atual+10…1990) com validação imediata; ao trocar mês/ano, recalcular os dias possíveis.
- **Banco** (`TG_AMOSTRAS_BI/BU`): 0 → NULL; mesmas validações; `DT_FABRICACAO`/`DT_VALIDADE` só quando completa; `CHECK` de faixa (dia 1–31, mês 1–12).
- **Exibição**: sempre por `FN_DATA_PARCIAL` (views) ou função C# equivalente (`DataParcial.Formatar`) — corrige o legado, que gerava `"  /  2026"` com espaços.

## Implementação no C#
```csharp
public readonly record struct DataParcial(byte? Dia, byte? Mes, short? Ano) {
    public string? Validar(string nome) { /* mesmas mensagens do banco */ }
    public override string ToString() => Ano is null ? "" : Dia is not null ? $"{Dia:00}/{Mes:00}/{Ano}" : Mes is not null ? $"{Mes:00}/{Ano}" : $"{Ano}";
}
```

## Cenários de teste
- Só mês/ano de validade → grava e imprime `05/2027` (roteiro doc 12 §7 item 5).
- 31/02/2027 → recusado; 29/02/2028 → aceito.
- Revisão da amostra copia as seis colunas.
