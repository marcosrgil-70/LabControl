// Gera as entidades EF Core (Models/*.g.cs), as configurações Fluent
// (Data/Configurations/*.g.cs) e os DbSets do LabDbContext a partir de
// Scripts/v2/*.sql (tabelas, FKs, triggers) + views.json (colunas das views).
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadAll, setByTriggerDetail } from '../gerador-docs/parse.mjs';

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const DIR_MODELS = path.join(ROOT, 'Models');
const DIR_CONFIG = path.join(ROOT, 'Data/Configurations');
const m = loadAll(path.join(ROOT, 'Scripts/v2'));
const viewsJson = JSON.parse(fs.readFileSync(path.join(path.dirname(fileURLToPath(import.meta.url)), 'views.json'), 'utf8'));

// ---------------------------------------------------------------------
// Nome da classe (singular) por tabela/view. DbSet = tabela em PascalCase.
// ---------------------------------------------------------------------
const CLASSES = {
  PAISES: 'Pais', ESTADOS: 'Estado', CIDADES: 'Cidade', EMPRESAS: 'Empresa',
  SEQUENCIAS_DOCUMENTOS: 'SequenciaDocumento', USUARIOS: 'Usuario', USUARIOS_EMPRESAS: 'UsuarioEmpresa',
  PERMISSOES: 'Permissao', PERFIS: 'Perfil', PERFIS_PERMISSOES: 'PerfilPermissao', USUARIOS_PERFIS: 'UsuarioPerfil',
  AUDITORIA: 'Auditoria', EMPRESAS_PARAMETROS: 'EmpresaParametros', TIPOS_LOGRADOURO: 'TipoLogradouro',
  ENDERECOS_TIPOS: 'EnderecoTipo', FONES_TIPOS: 'FoneTipo', EMAILS_TIPOS: 'EmailTipo',
  TIPOS_REG_PROFISSIONAL: 'TipoRegProfissional', CARGOS_FUNCIONARIOS: 'CargoFuncionario', IDIOMAS: 'Idioma',
  UNIDADES: 'Unidade', EMBALAGENS_TIPOS: 'EmbalagemTipo', LAB_TIPOS_RESULTADOS: 'LabTipoResultado',
  CONDICOES_PAGTOS: 'CondicaoPagto', CONDICOES_PAGTOS_PARC: 'CondicaoPagtoParc', PRAZOS: 'Prazo', MOEDAS: 'Moeda',
  AMOSTRAS_TIPO: 'AmostraTipo', AMOSTRAS_STATUS: 'AmostraStatus', ANALISES_TIPO: 'AnaliseTipo',
  ANALISES_STATUS: 'AnaliseStatus', ANALISES_METODOS: 'AnaliseMetodo', BOLETINS_STATUS: 'BoletimStatus',
  LAB_PROPOSTAS_STATUS: 'LabPropostaStatus', ENTIDADES: 'Entidade', ENTIDADES_ENDERECOS: 'EntidadeEndereco',
  ENTIDADES_FONES: 'EntidadeFone', ENTIDADES_EMAILS: 'EntidadeEmail', ENTIDADES_FUNCIONARIOS: 'EntidadeFuncionario',
  PRODUTOS: 'Produto', LAB_PARAMETROS_ANALISES: 'LabParametroAnalise', LAB_PROPOSTAS: 'LabProposta',
  LAB_PROPOSTAS_TEXTOS: 'LabPropostaTextos', LAB_PROPOSTAS_PRODUTOS: 'LabPropostaProduto',
  LAB_PROPOSTAS_PRODANALISES: 'LabPropostaProdAnalise', PEDIDOS_VENDA: 'PedidoVenda', PEDIDOS_VENDA_ITENS: 'PedidoVendaItem',
  LAB_HIST_AMOSTRAS: 'LabHistAmostra', LAB_HIST_AMOSTRAS_TESTES: 'LabHistAmostraTeste', LAB_HIST_AMOSTRAS_LOG: 'LabHistAmostraLog',
  LAB_MOV_AMOSTRAS: 'LabMovAmostra', LAB_MOV_AMOSTRAS_PARAM: 'LabMovAmostraParam', LAB_HIST_AMOSTRAS_SALDO: 'LabHistAmostraSaldo',
  LAB_LOCAL_AMOSTRAS: 'LabLocalAmostra', LAB_RESULTADOS_ANALISES: 'LabResultadoAnalise', LAB_RESULTADOS_PARAM: 'LabResultadoParam',
  LAB_BOLETIM_ANALISES: 'LabBoletimAnalise', LAB_BOLETIM_PARAMETROS: 'LabBoletimParametro', LAB_RELATORIO_ENSAIO: 'LabRelatorioEnsaio',
  VW_CLIENTES: 'VwCliente', VW_ENTIDADES_ENDERECOS: 'VwEntidadeEndereco', VW_PROFISSIONAIS: 'VwProfissional',
  VW_LAB_PROPOSTAS: 'VwLabProposta', VW_LAB_PROPOSTAS_ANALISES: 'VwLabPropostaAnalise', VW_LAB_AMOSTRAS: 'VwLabAmostra',
  VW_LAB_TESTES_PENDENTES: 'VwLabTestePendente', VW_LAB_RESULTADOS: 'VwLabResultado', VW_LAB_BOLETINS: 'VwLabBoletim',
  VW_PEDIDOS_VENDA_SALDO: 'VwPedidoVendaSaldo',
};

// ENUM do MySQL (lista de valores) -> enum C# de Models/Enums.cs
const ENUMS = {
  "'F','J'": 'Categoria',
  "'M','F'": 'Sexo',
  "'S','C','D','P','V'": 'EstadoCivil',
  "'BOLETIM','RELATORIO'": 'TipoDocumento',
  "'ABERTO','EM_EXECUCAO','CONCLUIDO','CANCELADO'": 'SituacaoPedido',
  "'E','S'": 'EntradaSaida',
  "'ARQUIVADA','DESCARTADA'": 'SituacaoArquivo',
  "'GERAL','NOTA_FISCAL','CORRESPONDENCIA','ENTREGA','COBRANCA'": 'FinalidadeEndereco',
};
// valor gravado no banco -> membro do enum C#
const MEMBROS = {
  Categoria: { F: 'Fisica', J: 'Juridica' },
  Sexo: { M: 'Masculino', F: 'Feminino' },
  EstadoCivil: { S: 'Solteiro', C: 'Casado', D: 'Divorciado', P: 'Separado', V: 'Viuvo' },
  TipoDocumento: { BOLETIM: 'Boletim', RELATORIO: 'Relatorio' },
  SituacaoPedido: { ABERTO: 'Aberto', EM_EXECUCAO: 'EmExecucao', CONCLUIDO: 'Concluido', CANCELADO: 'Cancelado' },
  EntradaSaida: { E: 'Entrada', S: 'Saida' },
  SituacaoArquivo: { ARQUIVADA: 'Arquivada', DESCARTADA: 'Descartada' },
  FinalidadeEndereco: { GERAL: 'Geral', NOTA_FISCAL: 'NotaFiscal', CORRESPONDENCIA: 'Correspondencia', ENTREGA: 'Entrega', COBRANCA: 'Cobranca' },
};

// ---------------------------------------------------------------------
// Colunas que as triggers (BEFORE) preenchem. Ver "Preenchidas pelo banco"
// nos MDs de docs/banco/objetos/tabelas.
//   calc  = sempre calculada pelo banco: o EF nunca envia e relê após INSERT/UPDATE
//   add   = o banco preenche se vier vazia: o EF omite quando vazia e relê após o INSERT
//   fixo  = número/ano do documento: o EF nunca envia (nem no INSERT) e relê após incluir;
//           assim a aplicação não consegue impor um número (o trigger guardaria um valor <> 0)
// Colunas set pela trigger que NÃO estão aqui são normais (a aplicação envia):
// ID_EMPRESAS herdado, normalização 0 -> NULL, limpeza de campos incompatíveis.
// ---------------------------------------------------------------------
const GERADAS = {
  PRODUTOS: { CODIGO: 'add' },
  LAB_PROPOSTAS: { COD_PROPOSTA: 'fixo', ANO_PROPOSTA: 'fixo', NR_PROPOSTA: 'calc', DT_SOLICITACAO: 'add',
    VR_SOMA_VR_ANALIS: 'calc', VR_SOMA_DESC_ANALIS: 'calc', VR_SOMA_VR_TOT_ANALIS: 'calc', VR_TOTAL_PROPOSTA: 'calc' },
  LAB_PROPOSTAS_PRODUTOS: { ITEM: 'add' },
  LAB_PROPOSTAS_PRODANALISES: { VR_SUBTOTAL: 'calc', VR_TOTAL: 'calc' },
  PEDIDOS_VENDA: { NR_PEDIDO: 'fixo' },
  LAB_HIST_AMOSTRAS: { ANO_AMOSTRA: 'fixo', COD_AMOSTRA: 'fixo', NR_AMOSTRA: 'calc', ID_PRODUTOS: 'add',
    ID_PEDIDOS_VENDA: 'add', DT_VALIDADE: 'calc', DT_FABRICACAO: 'calc' },
  LAB_HIST_AMOSTRAS_TESTES: { DT_PREVISTA: 'add' },
  LAB_RESULTADOS_PARAM: { RESULTADO_FORMATADO: 'calc', ESPECIF_FORMATADA: 'calc' },
  LAB_BOLETIM_ANALISES: { COD_BOLETIM: 'add', DT_BOLETIM: 'add' },
  LAB_RELATORIO_ENSAIO: { NR_RELATORIO: 'fixo', ANO_RELATORIO: 'fixo' },
};
// Têm ID_EMPRESAS mas NÃO recebem o filtro da empresa ativa (docs/processos/P01-multiempresa.md):
// EMPRESAS é global; os vínculos do usuário (global) são lidos no login/admin para todas as empresas.
const SEM_FILTRO_EMPRESA = new Set(['EMPRESAS', 'USUARIOS_EMPRESAS', 'USUARIOS_PERFIS']);

// set pela trigger, mas deliberadamente normais (conferido à mão)
const NORMAIS = new Set(['ID_EMPRESAS', 'ID_LAB_PROPOSTAS', 'ID_AMOSTRA_RAIZ', 'ID_AMOSTRA_ORIGEM', 'DT_ENVIO_CLIENTE',
  'VR_DESCONTO', 'ID_LAB_HIST_AMOSTRAS', 'VALIDADE_DIA', 'VALIDADE_MES', 'VALIDADE_ANO', 'FABRICACAO_DIA',
  'FABRICACAO_MES', 'FABRICACAO_ANO', 'ID_UNIDADES', 'ID_UNIDADES_AMOSTRA', 'ID_UNIDADES_ESPECIF',
  'ID_LAB_TIPOS_RESULTADOS', 'ID_LAB_TIPO_RESULTADOS_ESP', 'VR_RESULTADO', 'VR_EXP_RESULTADO', 'SIMBOLO_GRANDEZA',
  'VR_RESULTADO_ESPECIF', 'VR_EXP_ESPECIF', 'SIMBOLO_GR_ESPECIF',
  // contatos: a trigger só normaliza o que a aplicação já envia (e-mail em minúsculas, inativo não é principal)
  'PRINCIPAL', 'EMAIL']);

// ---------------------------------------------------------------------
const pascal = s => s.toLowerCase().split('_').filter(Boolean).map(p => p[0].toUpperCase() + p.slice(1)).join('');
const xml = s => s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
const erros = [];
const avisos = [];

function tipo(sqlType, ctx) {
  const t = sqlType.trim();
  const U = t.toUpperCase();
  let mm;
  if ((mm = t.match(/^enum\((.*)\)$/i))) {
    const e = ENUMS[mm[1]];
    if (!e) erros.push(`${ctx}: ENUM sem enum C#: ${mm[1]}`);
    return { cs: e, enumName: e, value: true };
  }
  if (/^TINYINT\(1\)$/.test(U)) return { cs: 'bool', value: true };
  if (/^TINYINT\b/.test(U)) return { cs: 'byte', value: true, colType: 'tinyint' };
  if (/^SMALLINT\b/.test(U)) return { cs: 'short', value: true };
  if (/^BIGINT\b/.test(U)) return { cs: 'long', value: true };
  if (/^INT\b/.test(U)) return { cs: 'int', value: true };
  if ((mm = U.match(/^DECIMAL\((\d+),(\d+)\)/))) return { cs: 'decimal', value: true, precision: [+mm[1], +mm[2]] };
  if ((mm = U.match(/^VARCHAR\((\d+)\)/))) return { cs: 'string', maxLength: +mm[1] };
  if ((mm = U.match(/^CHAR\((\d+)\)/))) return { cs: 'string', maxLength: +mm[1], fixed: true };
  if (/^(TINY|MEDIUM|LONG)?TEXT$/.test(U)) return { cs: 'string', colType: t.toLowerCase() };
  if (/^(TINY|MEDIUM|LONG)?BLOB$/.test(U)) return { cs: 'byte[]', colType: t.toLowerCase() };
  if (U === 'JSON') return { cs: 'string', colType: 'json' };
  if (U === 'DATE') return { cs: 'DateOnly', value: true };
  if (U === 'TIME') return { cs: 'TimeOnly', value: true };
  if (/^DATETIME(\(\d\))?$/.test(U)) return { cs: 'DateTime', value: true, colType: t.toLowerCase() };
  erros.push(`${ctx}: tipo não mapeado: ${t}`);
  return { cs: 'object' };
}

function inicializador(c, tp, geracao) {
  if (geracao || c.ai) return tp.cs === 'string' && c.notNull ? ' = null!;' : '';
  const d = c.def;
  if (d && !/CURRENT_(TIMESTAMP|DATE)/i.test(d)) {
    const lit = d.replace(/^'(.*)'$/, '$1');
    if (tp.enumName) return ` = ${tp.enumName}.${MEMBROS[tp.enumName][lit]};`;
    if (tp.cs === 'bool') return lit === '1' ? ' = true;' : '';
    if (tp.cs === 'string') return ` = "${lit}";`;
    if (lit === '0') return '';
    return ` = ${lit}${tp.cs === 'decimal' ? 'm' : ''};`;
  }
  if (tp.cs === 'string' && c.notNull) return ' = string.Empty;';
  if (tp.cs === 'byte[]' && c.notNull) return ' = [];';
  return '';
}

const CAB = '// <auto-generated>\n//   Gerado por tools/gerador-csharp/gerar.mjs a partir de Scripts/v2/*.sql — NÃO EDITAR.\n//   Ajustes manuais: arquivo parcial sem ".g" (ex.: Models/LabProposta.cs, Data/Configurations/LabPropostaConfiguration.cs).\n// </auto-generated>\n#nullable enable\n';

// ---------------------------------------------------------------------
// Modelo intermediário
// ---------------------------------------------------------------------
const pkDe = {};
for (const t of Object.values(m.tables)) pkDe[t.name] = t.pk;

// triggers BEFORE por tabela -> colunas que elas atribuem
const setPorTabela = {};
for (const g of Object.values(m.triggers)) {
  if (g.timing !== 'BEFORE' || g.event === 'DELETE') continue;
  for (const c of Object.keys(setByTriggerDetail(g.body))) (setPorTabela[g.table] ||= new Set()).add(c);
}

const entidades = [];
for (const t of Object.values(m.tables)) {
  const classe = CLASSES[t.name];
  if (!classe) { erros.push(`tabela sem nome de classe: ${t.name}`); continue; }
  const ger = GERADAS[t.name] || {};
  for (const c of setPorTabela[t.name] || []) {
    if (!ger[c] && !NORMAIS.has(c)) avisos.push(`${t.name}.${c} é atribuída por trigger e não está em GERADAS nem NORMAIS`);
  }
  for (const c of Object.keys(ger)) if (!t.columns.some(x => x.name === c)) erros.push(`GERADAS: ${t.name}.${c} não existe`);
  const props = t.columns.map(c => {
    const tp = tipo(c.type, `${t.name}.${c.name}`);
    let geracao = ger[c.name] || null;
    if (!geracao && /CURRENT_(TIMESTAMP|DATE)/i.test(c.def)) geracao = c.onUpd ? 'calc' : (/CURRENT_DATE/i.test(c.def) ? 'add' : 'criacao');
    if (!geracao && c.onUpd) geracao = 'calc';
    const pk = t.pk.includes(c.name);
    return { col: c.name, prop: pascal(c.name), tp, nullable: !c.notNull, geracao, ai: c.ai, pk, comment: c.comment, init: inicializador(c, tp, geracao) };
  });
  const fks = t.fks.map(f => {
    const alvo = CLASSES[f.ref];
    const pkAlvo = pkDe[f.ref] || [];
    const igualPk = f.refCols.join() === pkAlvo.join() && f.cols.join() === f.refCols.join();
    const ps = f.cols.map(c => props.find(x => x.col === c));
    const nav = igualPk ? alvo : pascal(f.cols[0]).replace(/^Id/, '');
    const umParaUm = t.pk.join() === f.cols.join();
    const principal = f.refCols.join() !== pkAlvo.join() ? f.refCols.map(pascal) : null;
    return { name: f.name, props: ps.map(p => p.prop), nullable: ps.some(p => p.nullable), alvo, nav, umParaUm, principal, onDelete: f.onDelete };
  });
  entidades.push({ tabela: t.name, classe, dbset: pascal(t.name), props, fks, pk: t.pk.map(pascal), comment: t.comment, view: false });
}

const colsView = {};
for (const c of viewsJson) (colsView[c.view.toUpperCase()] ||= []).push(c);
for (const v of Object.keys(m.views)) {
  const classe = CLASSES[v];
  if (!classe) { erros.push(`view sem nome de classe: ${v}`); continue; }
  const cols = (colsView[v] || []).sort((a, b) => a.pos - b.pos);
  if (!cols.length) erros.push(`view ${v} sem colunas em views.json (rode colunas_views.sql)`);
  const props = cols.map(c => {
    const tp = tipo(c.type, `${v}.${c.col}`);
    return { col: c.col, prop: pascal(c.col), tp, nullable: c.nullable, geracao: null, pk: false, comment: '', init: tp.cs === 'string' && !c.nullable ? ' = string.Empty;' : '' };
  });
  entidades.push({ tabela: v, classe, dbset: pascal(v), props, fks: [], pk: [], comment: m.views[v].comment || '', view: true });
}
for (const v of Object.keys(colsView)) if (!m.views[v]) erros.push(`views.json tem ${v}, que não existe em 04_views.sql`);

// nomes únicos por classe
for (const e of entidades) {
  const nomes = [...e.props.map(p => p.prop), ...e.fks.map(f => f.nav)];
  const dup = nomes.filter((n, i) => nomes.indexOf(n) !== i);
  if (dup.length) erros.push(`${e.classe}: membros duplicados ${[...new Set(dup)].join(', ')}`);
  if (nomes.includes(e.classe)) erros.push(`${e.classe}: membro com o mesmo nome da classe`);
}

if (erros.length) { console.error('ERROS:\n  ' + erros.join('\n  ')); process.exit(1); }

// ---------------------------------------------------------------------
// Escrita
// ---------------------------------------------------------------------
function tipoCs(p) {
  const base = p.tp.cs;
  if (base === 'string' || base === 'byte[]') return p.nullable ? `${base}?` : base;
  return p.nullable ? `${base}?` : base;
}

function classeCs(e) {
  const L = [CAB, 'namespace LabControl.Models;', ''];
  L.push('/// <summary>');
  L.push(`/// ${e.view ? 'View' : 'Tabela'} <c>${e.tabela}</c>${e.comment ? ' — ' + xml(e.comment) : ''}`);
  L.push('/// </summary>');
  const emp = e.props.find(p => p.col === 'ID_EMPRESAS');
  const iface = !emp || SEM_FILTRO_EMPRESA.has(e.tabela) ? ''
    : !e.view && !emp.nullable ? ' : IPorEmpresa' : ' : IFiltradoPorEmpresa';
  L.push(`public partial class ${e.classe}${iface}`);
  L.push('{');
  for (const p of e.props) {
    const notas = [];
    if (p.comment) notas.push(xml(p.comment));
    if (p.geracao === 'calc') notas.push('Calculada pelo banco (trigger/default): não é enviada; relida após gravar.');
    if (p.geracao === 'add') notas.push('Preenchida pelo banco quando vazia: relida após incluir.');
    if (p.geracao === 'fixo') notas.push('Número/ano do documento, sempre gerado pelo banco: não é enviado; relido após incluir.');
    if (p.geracao === 'criacao') notas.push('Preenchida pelo banco na inclusão.');
    if (notas.length) L.push(`    /// <summary><c>${p.col}</c> — ${notas.join(' · ')}</summary>`);
    L.push(`    public ${tipoCs(p)} ${p.prop} { get; set; }${p.init}`);
  }
  if (e.fks.length) L.push('');
  for (const f of e.fks) L.push(`    public ${f.alvo}${f.nullable ? '?' : ''} ${f.nav} { get; set; }${f.nullable ? '' : ' = null!;'}`);
  L.push('}', '');
  return L.join('\n');
}

const DEL = { CASCADE: 'Cascade', 'SET NULL': 'SetNull' };
function configCs(e) {
  const L = [CAB, 'using LabControl.Models;', 'using Microsoft.EntityFrameworkCore;', 'using Microsoft.EntityFrameworkCore.Metadata.Builders;', '', 'namespace LabControl.Data.Configurations;', ''];
  L.push(`public partial class ${e.classe}Configuration : IEntityTypeConfiguration<${e.classe}>`);
  L.push('{');
  L.push(`    public void Configure(EntityTypeBuilder<${e.classe}> b)`);
  L.push('    {');
  if (e.view) {
    L.push(`        b.ToView("${e.tabela}");`);
    L.push('        b.HasNoKey();');
  } else {
    L.push(`        b.ToTable("${e.tabela}");`);
    L.push(e.pk.length === 1 ? `        b.HasKey(e => e.${e.pk[0]});` : `        b.HasKey(e => new { ${e.pk.map(k => 'e.' + k).join(', ')} });`);
  }
  for (const p of e.props) {
    let s = `        b.Property(e => e.${p.prop}).HasColumnName("${p.col}")`;
    if (p.tp.maxLength) s += `.HasMaxLength(${p.tp.maxLength})`;
    if (p.tp.fixed) s += '.IsFixedLength()';
    if (p.tp.precision) s += `.HasPrecision(${p.tp.precision.join(', ')})`;
    if (p.tp.colType) s += `.HasColumnType("${p.tp.colType}")`;
    if (p.tp.enumName) s += `.HasConversion(EnumBanco<${p.tp.enumName}>.Conversor)`;
    if (!p.nullable && (p.tp.cs === 'string' || p.tp.cs === 'byte[]')) s += '.IsRequired()';
    if (p.pk && !p.ai && e.pk.length === 1 && p.tp.cs === 'int') s += '.ValueGeneratedNever()';
    if (p.geracao === 'calc') s += '.CalculadaPeloBanco()';
    if (p.geracao === 'add') s += '.PreenchidaPeloBancoNaInclusao()';
    if (p.geracao === 'fixo') s += '.PreenchidaPeloBancoNaInclusao(alteravel: false, enviar: false)';
    if (p.geracao === 'criacao') s += '.PreenchidaPeloBancoNaInclusao(alteravel: false, enviar: false)';
    L.push(s + ';');
  }
  const lambda = (v, ps) => ps.length === 1 ? `${v} => ${v}.${ps[0]}` : `${v} => new { ${ps.map(x => `${v}.${x}`).join(', ')} }`;
  for (const f of e.fks) {
    let s = f.umParaUm
      ? `        b.HasOne(e => e.${f.nav}).WithOne().HasForeignKey<${e.classe}>(${lambda('e', f.props)})`
      : `        b.HasOne(e => e.${f.nav}).WithMany().HasForeignKey(${lambda('e', f.props)})`;
    if (f.principal) s += f.umParaUm ? `.HasPrincipalKey<${f.alvo}>(${lambda('p', f.principal)})` : `.HasPrincipalKey(${lambda('p', f.principal)})`;
    s += `.OnDelete(DeleteBehavior.${DEL[f.onDelete] || 'Restrict'}).HasConstraintName("${f.name}")`;
    L.push(s + ';');
  }
  L.push('        Ajustar(b);');
  L.push('    }');
  L.push('');
  L.push(`    partial void Ajustar(EntityTypeBuilder<${e.classe}> b);`);
  L.push('}', '');
  return L.join('\n');
}

function dbsetsCs() {
  const L = [CAB, 'using LabControl.Models;', 'using Microsoft.EntityFrameworkCore;', '', 'namespace LabControl.Data;', '', 'public partial class LabDbContext', '{'];
  for (const e of entidades) L.push(`    public DbSet<${e.classe}> ${e.dbset} => Set<${e.classe}>();`);
  L.push('}', '');
  return L.join('\n');
}

function limpar(dir) {
  fs.mkdirSync(dir, { recursive: true });
  for (const f of fs.readdirSync(dir)) if (f.endsWith('.g.cs')) fs.unlinkSync(path.join(dir, f));
}
const escrever = (arq, txt) => fs.writeFileSync(arq, txt.replace(/\n/g, '\r\n'), 'utf8');

limpar(DIR_MODELS);
limpar(DIR_CONFIG);
for (const e of entidades) {
  escrever(path.join(DIR_MODELS, `${e.classe}.g.cs`), classeCs(e));
  escrever(path.join(DIR_CONFIG, `${e.classe}Configuration.g.cs`), configCs(e));
}
escrever(path.join(ROOT, 'Data/LabDbContext.DbSets.g.cs'), dbsetsCs());

const nT = entidades.filter(e => !e.view).length;
console.log(`${nT} tabelas + ${entidades.length - nT} views -> ${entidades.length} entidades e configurações.`);
if (avisos.length) console.log('AVISOS:\n  ' + avisos.join('\n  '));
