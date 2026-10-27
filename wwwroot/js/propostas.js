// T20 — Proposta (tarefas 4.2 e 6.1). Produtos × análises editados na página e gravados junto com o cabeçalho (campo
// ItensJson, uma transação no servidor). Contatos/endereços do cliente e da empresa de cobrança vêm por AJAX
// (/Propostas/Entidade/{id}) com as sugestões da T20. Valores recalculados aqui só para exibir: ao gravar, o banco
// recalcula (triggers + SP_RECALCULAR_PROPOSTA, P06).
(() => {
    const form = document.getElementById('formProposta');
    if (!form) return;
    const $ = id => document.getElementById(id);
    const podeGravar = form.dataset.podeGravar === '1';
    const dados = JSON.parse($('dadosProposta').textContent);
    const campoItens = $('ItensJson');
    let itens = JSON.parse(campoItens.value || '[]');
    let selecionado = itens.length ? 0 : -1;

    const moeda = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    const fmt = v => moeda.format(v);
    const r2 = v => Math.round((v + Number.EPSILON) * 100) / 100;
    // "1.234,56", "1.000" (milhar) ou "12.5" — a mesma regra do Textos.Decimal do servidor
    const num = s => {
        s = String(s ?? '').trim();
        if (!s) return 0;
        if (s.includes(',') || /^\d{1,3}(\.\d{3})+$/.test(s)) s = s.replaceAll('.', '').replace(',', '.');
        const n = Number(s);
        return Number.isFinite(n) && n >= 0 ? n : NaN;
    };
    const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    const textoDe = (select, id) => [...select.options].find(o => o.value === String(id))?.text ?? '';
    const produtos = new Map(dados.produtos.map(p => [p.id, p]));
    const parametros = new Map(dados.parametros.map(p => [p.id, p]));

    // ------------------------------------------------------------ valores (P06)
    const valores = (qtde, a) => {
        const sub = r2(qtde * a.vrUnitario);
        const desc = a.porcDesconto > 0 ? r2(sub * a.porcDesconto / 100) : a.vrDesconto;
        return { sub, desc, total: r2(sub - desc) };
    };
    const atualizarTotais = () => {
        let sub = 0, desc = 0, tot = 0;
        for (const i of itens) for (const a of i.analises) { const v = valores(i.qtdeAmostras, a); sub += v.sub; desc += v.desc; tot += v.total; }
        const porc = num($('PorcDesconto').value);
        let geral = num($('VrDesconto').value);
        if (porc > 0) { geral = r2(r2(tot) * porc / 100); $('VrDesconto').value = fmt(geral); }
        $('VrDesconto').readOnly = porc > 0;
        $('totSubtotal').textContent = fmt(r2(sub));
        $('totDescAnalises').textContent = fmt(r2(desc));
        $('totAnalises').textContent = fmt(r2(tot));
        $('totProposta').textContent = fmt(r2(tot - (Number.isNaN(geral) ? 0 : geral)));
        $('contagemAnalises').textContent = itens.reduce((n, i) => n + i.analises.length, 0);
    };
    $('PorcDesconto').addEventListener('input', atualizarTotais);
    $('VrDesconto').addEventListener('input', atualizarTotais);

    // ------------------------------------------------------------ tabelas
    const botoes = (alterar, excluir, k) => podeGravar
        ? `<button type="button" class="btn btn-outline-secondary btn-sm" data-acao="${alterar}" data-k="${k}"><i class="bi bi-pencil me-1"></i>Alterar</button>
           <button type="button" class="btn btn-outline-danger btn-sm" data-acao="${excluir}" data-k="${k}"><i class="bi bi-trash me-1"></i>Excluir</button>` : '';

    const desenhar = () => {
        $('tabelaProdutos').tBodies[0].innerHTML = itens.map((i, k) => {
            const p = produtos.get(i.idProdutos);
            const total = i.analises.reduce((s, a) => s + valores(i.qtdeAmostras, a).total, 0);
            return `<tr class="${k === selecionado ? 'table-active' : ''}" data-selecionar="${k}" style="cursor: pointer">
                <td>${i.item || '<span class="text-body-secondary small">novo</span>'}</td>
                <td class="fw-semibold">${esc(p?.nome ?? textoDe($('produtoId'), i.idProdutos))}</td>
                <td class="small">${esc(p?.extra?.unidade)}</td>
                <td class="text-end">${i.qtdeAmostras}</td>
                <td class="text-end">${i.analises.length}</td>
                <td class="text-end">${fmt(r2(total))}</td>
                <td class="text-end text-nowrap">${botoes('alterar-produto', 'excluir-produto', k)}</td></tr>`;
        }).join('') || '<tr><td colspan="7" class="text-center text-body-secondary small py-2">Nenhum produto. Use “Incluir produto”.</td></tr>';

        const item = itens[selecionado];
        $('produtoSelecionado').textContent = item ? `— item ${item.item || 'novo'}: ${produtos.get(item.idProdutos)?.nome ?? ''}` : '';
        $('tabelaAnalises').tBodies[0].innerHTML = !item
            ? '<tr><td colspan="13" class="text-center text-body-secondary small py-2">Selecione um produto para ver as análises.</td></tr>'
            : item.analises.map((a, k) => {
                const v = valores(item.qtdeAmostras, a);
                const p = parametros.get(a.idParametro);
                return `<tr>
                    <td>${esc(p?.nome ?? textoDe($('analiseParametro'), a.idParametro))}${a.stRevAnterior ? ' <span class="badge text-bg-light border">rev. anterior</span>' : ''}</td>
                    <td class="small">${esc(p?.extra?.tipo)}</td>
                    <td class="small">${esc(textoDe($('analiseMetodo'), a.idMetodo))}</td>
                    <td class="text-end">${item.qtdeAmostras}</td>
                    <td class="text-end">${fmt(a.vrUnitario)}</td>
                    <td class="text-end">${fmt(v.sub)}</td>
                    <td class="text-end">${a.porcDesconto ? a.porcDesconto.toLocaleString('pt-BR') : ''}</td>
                    <td class="text-end">${v.desc ? fmt(v.desc) : ''}</td>
                    <td class="text-end fw-semibold">${fmt(v.total)}</td>
                    <td class="small">${esc(textoDe($('analisePrazo'), a.idPrazo))}</td>
                    <td class="small">${esc(textoDe($('analiseIdioma'), a.idIdioma))}</td>
                    <td class="small">${a.tipoDocumento === 'RELATORIO' ? 'Relatório' : 'Boletim'}</td>
                    <td class="text-end text-nowrap">${botoes('alterar-analise', 'excluir-analise', k)}</td></tr>`;
            }).join('') || '<tr><td colspan="13" class="text-center text-body-secondary small py-2">Produto sem análises. Use “Incluir análise”.</td></tr>';
        atualizarTotais();
    };

    // ------------------------------------------------------------ modal do produto
    const modalProduto = bootstrap.Modal.getOrCreateInstance($('modalProduto'));
    let produtoEmEdicao = null;
    const erroModal = (modal, msg) => { const e = modal.querySelector('[data-erro]'); e.textContent = msg ?? ''; e.classList.toggle('d-none', !msg); };

    const abrirProduto = k => {
        produtoEmEdicao = k;
        const i = k === null ? null : itens[k];
        $('produtoId').value = i ? String(i.idProdutos) : '';
        $('produtoQtde').value = i ? i.qtdeAmostras : 1;
        $('novoProduto').classList.add('d-none');
        erroModal($('modalProduto'), null);
        $('tituloModalProduto').textContent = i ? 'Alterar produto' : 'Incluir produto';
        modalProduto.show();
    };
    $('modalProduto').addEventListener('shown.bs.modal', () => $('produtoId').focus());
    $('formProduto').addEventListener('submit', e => {
        e.preventDefault();
        const id = Number($('produtoId').value);
        const qtde = Number($('produtoQtde').value);
        if (!id) return erroModal($('modalProduto'), 'Escolha o produto.');
        if (!Number.isInteger(qtde) || qtde < 1) return erroModal($('modalProduto'), 'A quantidade de amostras deve ser um número inteiro maior que zero.');
        if (produtoEmEdicao === null) {
            itens.push({ id: 0, item: 0, idProdutos: id, qtdeAmostras: qtde, analises: [] });
            selecionado = itens.length - 1;
        } else {
            Object.assign(itens[produtoEmEdicao], { idProdutos: id, qtdeAmostras: qtde });
            selecionado = produtoEmEdicao;
        }
        modalProduto.hide();
        desenhar();
    });

    // "+" do produto: POST /Produtos/Rapido (T14) com descrição e unidade
    form.ownerDocument.querySelector('[data-acao="novo-produto"]')?.addEventListener('click', () => {
        $('novoProduto').classList.toggle('d-none');
        $('novoProdutoDescricao').focus();
    });
    document.querySelector('[data-acao="gravar-novo-produto"]')?.addEventListener('click', async () => {
        const corpo = new FormData();
        corpo.append('Descricao', $('novoProdutoDescricao').value);
        corpo.append('IdUnidades', $('novoProdutoUnidade').value);
        corpo.append('__RequestVerificationToken', form.querySelector('[name=__RequestVerificationToken]').value);
        const r = await fetch('/Produtos/Rapido', { method: 'POST', body: corpo, headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        if (r.status === 422) return erroModal($('modalProduto'), (await r.json()).erro);
        if (!r.ok) return erroModal($('modalProduto'), r.status === 403 ? 'Você não tem permissão para incluir produtos.' : 'Não foi possível incluir o produto.');
        const { id, texto } = await r.json();
        const unidade = textoDe($('novoProdutoUnidade'), $('novoProdutoUnidade').value);
        produtos.set(id, { id, nome: texto, extra: { unidade } });
        $('produtoId').add(new Option(texto, id));
        $('produtoId').value = String(id);
        $('novoProduto').classList.add('d-none');
        $('novoProdutoDescricao').value = '';
        erroModal($('modalProduto'), null);
    });

    // ------------------------------------------------------------ modal da análise
    const modalAnalise = bootstrap.Modal.getOrCreateInstance($('modalAnalise'));
    let analiseEmEdicao = null;
    const documento = () => document.querySelector('[name=analiseDocumento]:checked')?.value ?? 'BOLETIM';
    const lerAnalise = () => ({
        idParametro: Number($('analiseParametro').value), idMetodo: Number($('analiseMetodo').value),
        idIdioma: Number($('analiseIdioma').value), idPrazo: Number($('analisePrazo').value), tipoDocumento: documento(),
        vrUnitario: num($('analiseUnitario').value), porcDesconto: num($('analisePorc').value), vrDesconto: num($('analiseDesconto').value),
    });
    const mostrarValoresAnalise = () => {
        const a = lerAnalise();
        const qtde = itens[selecionado]?.qtdeAmostras ?? 0;
        if ([a.vrUnitario, a.porcDesconto, a.vrDesconto].some(Number.isNaN)) { $('analiseSubtotal').textContent = $('analiseTotal').textContent = '—'; return; }
        const v = valores(qtde, a);
        if (a.porcDesconto > 0) $('analiseDesconto').value = fmt(v.desc);
        $('analiseDesconto').readOnly = a.porcDesconto > 0;
        $('analiseSubtotal').textContent = fmt(v.sub);
        $('analiseTotal').textContent = fmt(v.total);
    };
    ['analiseUnitario', 'analisePorc', 'analiseDesconto'].forEach(id => $(id).addEventListener('input', mostrarValoresAnalise));
    // parâmetro: mostra o tipo, sugere o valor (se a linha estiver com 0) e o método padrão (T20, regra do Delphi)
    $('analiseParametro').addEventListener('change', () => {
        const p = parametros.get(Number($('analiseParametro').value));
        $('analiseTipo').textContent = p ? `Tipo de análise: ${p.extra.tipo}` : '';
        if (p && !num($('analiseUnitario').value)) $('analiseUnitario').value = fmt(p.extra.unitario);
        if (p?.extra.metodo && [...$('analiseMetodo').options].some(o => o.value === String(p.extra.metodo))) $('analiseMetodo').value = String(p.extra.metodo);
        mostrarValoresAnalise();
    });

    const abrirAnalise = k => {
        const item = itens[selecionado];
        if (!item) { alert('Inclua ou selecione um produto antes.'); return; }
        analiseEmEdicao = k;
        // nova linha copia método/idioma/prazo/documento da anterior do produto (como a seleção múltipla do legado)
        const base = k === null ? item.analises.at(-1) : item.analises[k];
        $('analiseParametro').value = k === null ? '' : String(base.idParametro);
        $('analiseMetodo').value = base ? String(base.idMetodo) : '';
        $('analiseIdioma').value = base ? String(base.idIdioma) : '';
        $('analisePrazo').value = base ? String(base.idPrazo) : '';
        (document.querySelector(`[name=analiseDocumento][value="${base?.tipoDocumento ?? 'BOLETIM'}"]`) ?? $('analiseBoletim')).checked = true;
        $('analiseQtde').value = item.qtdeAmostras;
        $('analiseUnitario').value = k === null ? '' : fmt(base.vrUnitario);
        $('analisePorc').value = k === null || !base.porcDesconto ? '' : base.porcDesconto.toLocaleString('pt-BR');
        $('analiseDesconto').value = k === null || !base.vrDesconto ? '' : fmt(base.vrDesconto);
        $('analiseTipo').textContent = k === null ? '' : `Tipo de análise: ${parametros.get(base.idParametro)?.extra.tipo ?? ''}`;
        erroModal($('modalAnalise'), null);
        $('tituloModalAnalise').textContent = k === null ? 'Incluir análise' : 'Alterar análise';
        mostrarValoresAnalise();
        modalAnalise.show();
    };
    $('modalAnalise').addEventListener('shown.bs.modal', () => $('analiseParametro').focus());
    $('formAnalise').addEventListener('submit', e => {
        e.preventDefault();
        const a = lerAnalise();
        const falta = !a.idParametro ? 'Escolha o parâmetro.' : !a.idMetodo ? 'Escolha o método.' : !a.idIdioma ? 'Escolha o idioma.' : !a.idPrazo ? 'Escolha o prazo.' : null;
        if (falta) return erroModal($('modalAnalise'), falta);
        if ([a.vrUnitario, a.porcDesconto, a.vrDesconto].some(Number.isNaN) || a.porcDesconto > 100)
            return erroModal($('modalAnalise'), 'Valores inválidos: unitário e desconto maiores ou iguais a zero; % de 0 a 100.');
        const item = itens[selecionado];
        const v = valores(item.qtdeAmostras, a);
        if (v.desc > v.sub) return erroModal($('modalAnalise'), 'Desconto maior que o subtotal da análise.');
        if (a.porcDesconto > 0) a.vrDesconto = 0;
        if (analiseEmEdicao === null) item.analises.push({ id: 0, stRevAnterior: false, ...a });
        else Object.assign(item.analises[analiseEmEdicao], a);
        modalAnalise.hide();
        desenhar();
    });

    // ------------------------------------------------------------ cliques nas tabelas e botões
    document.addEventListener('click', e => {
        const alvo = e.target.closest('[data-acao], [data-selecionar]');
        if (!alvo || !form.parentElement.contains(alvo)) return;
        const k = alvo.dataset.k === undefined ? null : Number(alvo.dataset.k);
        switch (alvo.dataset.acao) {
            case 'incluir-produto': abrirProduto(null); break;
            case 'alterar-produto': abrirProduto(k); break;
            case 'excluir-produto':
                if (confirm(`Excluir o produto "${produtos.get(itens[k].idProdutos)?.nome ?? ''}" e as ${itens[k].analises.length} análise(s) dele?`)) {
                    itens.splice(k, 1);
                    selecionado = Math.min(selecionado, itens.length - 1);
                    desenhar();
                }
                break;
            case 'incluir-analise': abrirAnalise(null); break;
            case 'selecionar-parametros': abrirSelecao(); break;
            case 'alterar-analise': abrirAnalise(k); break;
            case 'excluir-analise':
                if (confirm('Excluir esta análise?')) { itens[selecionado].analises.splice(k, 1); desenhar(); }
                break;
            default:
                if (alvo.dataset.selecionar !== undefined && !e.target.closest('button')) { selecionado = Number(alvo.dataset.selecionar); desenhar(); }
        }
    });

    // ------------------------------------------------------------ cliente e cobrança (contatos, endereços, sugestões)
    const preencher = (select, opcoes, escolhido) => {
        const atual = escolhido ?? select.value;
        select.replaceChildren(new Option('—', ''), ...opcoes.map(o => new Option(o.texto, o.id)));
        select.value = opcoes.some(o => String(o.id) === String(atual)) ? String(atual) : '';
    };
    const detalhar = () => {
        for (const d of document.querySelectorAll('[data-detalhe-de]')) {
            const select = $(d.dataset.detalheDe);
            const dono = dados[select.dataset.enderecos];
            d.textContent = dono?.enderecos.find(x => String(x.id) === select.value)?.detalhe ?? '';
        }
        const c = dados.cliente;
        const contato = c?.contatos.find(x => String(x.id) === $('IdEntContato').value);
        $('infoCliente').textContent = c ? [c.cpfCnpj && `CPF/CNPJ ${c.cpfCnpj}`, c.fone && `Fone ${c.fone}`,
            contato && `Contato: ${contato.email}${contato.departamento ? ' — ' + contato.departamento : ''}`].filter(Boolean).join(' · ') : '';
    };
    // aplica a entidade nos combos dela; sugerir = trocar pelas sugestões (escolha nova do usuário), senão mantém os valores
    const aplicar = (qual, sugerir) => {
        const e = dados[qual];
        for (const s of document.querySelectorAll(`[data-contatos="${qual}"], [data-enderecos="${qual}"]`)) {
            const lista = e ? (s.dataset.contatos ? e.contatos : e.enderecos) : [];
            const chave = s.id[0].toLowerCase() + s.id.slice(1);
            preencher(s, lista, sugerir ? (s.dataset.sugestao ? e?.[s.dataset.sugestao] : '') : dados.valores?.[chave]);
        }
        detalhar();
    };
    const carregar = async (qual, id) => {
        if (!id) { dados[qual] = null; return; }
        const r = await fetch(`/Propostas/Entidade/${id}`, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        dados[qual] = r.ok ? await r.json() : null;
    };
    let clienteAnterior = $('IdEntidades').value;
    $('IdEntidades').addEventListener('change', async () => {
        const id = $('IdEntidades').value;
        await carregar('cliente', id);
        aplicar('cliente', true);
        // cobrança = o próprio cliente, a não ser que já fosse outra empresa escolhida de propósito
        if (!$('IdEntCobranca').value || $('IdEntCobranca').value === clienteAnterior) {
            $('IdEntCobranca').value = id;
            dados.cobranca = dados.cliente;
            aplicar('cobranca', true);
        }
        clienteAnterior = id;
    });
    $('IdEntCobranca').addEventListener('change', async () => {
        await carregar('cobranca', $('IdEntCobranca').value);
        aplicar('cobranca', true);
    });
    document.querySelectorAll('[data-enderecos], #IdEntContato').forEach(s => s.addEventListener('change', detalhar));

    // ------------------------------------------------------------ textos ricos (HTML simples; o servidor limpa)
    for (const editor of document.querySelectorAll('[data-texto]')) editor.innerHTML = $(editor.dataset.texto).value;
    document.querySelectorAll('[data-barra] [data-comando]').forEach(b => b.addEventListener('click', () => {
        b.closest('.border').querySelector('[data-texto]').focus();
        document.execCommand(b.dataset.comando);
    }));

    // ------------------------------------------------------------ seleção múltipla de parâmetros (6.1; Delphi Btn_SelParam)
    // marca vários parâmetros e inclui todos de uma vez no produto escolhido, com idioma/prazo/documento comuns e o
    // método padrão de cada parâmetro (ou o escolhido); valor unitário = o do cadastro do parâmetro
    const modalSelecao = bootstrap.Modal.getOrCreateInstance($('modalSelecao'));
    const visiveis = () => [...$('tabelaSelecao').tBodies[0].rows].filter(r => !r.hidden);
    const contarSelecao = () => {
        const n = $('tabelaSelecao').querySelectorAll('tbody input:checked').length;
        $('selContagem').textContent = n ? `${n} marcado(s)` : '';
    };
    const abrirSelecao = () => {
        const item = itens[selecionado];
        if (!item) { alert('Inclua ou selecione um produto antes.'); return; }
        const ja = new Set(item.analises.map(a => a.idParametro));
        $('tabelaSelecao').tBodies[0].innerHTML = dados.parametros.map(p => `
            <tr data-busca="${esc((p.nome + ' ' + (p.extra.tipo ?? '')).toLowerCase())}">
                <td><input type="checkbox" class="form-check-input" value="${p.id}" id="sel_${p.id}" ${ja.has(p.id) ? 'disabled' : ''} /></td>
                <td><label for="sel_${p.id}" class="mb-0">${esc(p.nome)}</label>${ja.has(p.id) ? ' <span class="badge text-bg-light border">já no produto</span>' : ''}</td>
                <td class="small">${esc(p.extra.tipo)}</td>
                <td class="text-end small">${fmt(p.extra.unitario ?? 0)}</td></tr>`).join('');
        // padrões = a última análise do produto (como o legado copiava da linha base)
        const base = item.analises.at(-1);
        $('selMetodo').value = base ? String(base.idMetodo) : '';
        $('selIdioma').value = base ? String(base.idIdioma) : ($('selIdioma').options.length === 2 ? $('selIdioma').options[1].value : '');
        $('selPrazo').value = base ? String(base.idPrazo) : '';
        (document.querySelector(`[name=selDocumento][value="${base?.tipoDocumento ?? 'BOLETIM'}"]`) ?? $('selBoletim')).checked = true;
        $('selFiltro').value = '';
        $('selecaoProduto').textContent = `— item ${item.item || 'novo'}: ${produtos.get(item.idProdutos)?.nome ?? ''}`;
        erroModal($('modalSelecao'), null);
        contarSelecao();
        modalSelecao.show();
    };
    $('modalSelecao').addEventListener('shown.bs.modal', () => $('selFiltro').focus());
    $('selFiltro').addEventListener('input', () => {
        const termo = $('selFiltro').value.trim().toLowerCase();
        for (const r of $('tabelaSelecao').tBodies[0].rows) r.hidden = termo !== '' && !r.dataset.busca.includes(termo);
    });
    $('tabelaSelecao').addEventListener('change', contarSelecao);
    $('modalSelecao').querySelector('[data-acao="marcar-visiveis"]').addEventListener('click', () => {
        for (const r of visiveis()) { const c = r.querySelector('input'); if (!c.disabled) c.checked = true; }
        contarSelecao();
    });
    $('formSelecao').addEventListener('submit', e => {
        e.preventDefault();
        const item = itens[selecionado];
        const marcados = [...$('tabelaSelecao').querySelectorAll('tbody input:checked')].map(c => parametros.get(Number(c.value)));
        if (!marcados.length) return erroModal($('modalSelecao'), 'Nenhum parâmetro foi selecionado.');
        const idIdioma = Number($('selIdioma').value), idPrazo = Number($('selPrazo').value);
        if (!idIdioma) return erroModal($('modalSelecao'), 'Escolha o idioma.');
        if (!idPrazo) return erroModal($('modalSelecao'), 'Escolha o prazo.');
        const metodoComum = Number($('selMetodo').value);
        const metodoDe = p => ($('selMetodoPadrao').checked && p.extra.metodo) || metodoComum;
        const semMetodo = marcados.filter(p => !metodoDe(p));
        if (semMetodo.length) return erroModal($('modalSelecao'), `Escolha o método: ${semMetodo.map(p => p.nome).join(', ')} não tem método padrão.`);
        const tipoDocumento = document.querySelector('[name=selDocumento]:checked')?.value ?? 'BOLETIM';
        for (const p of marcados)
            item.analises.push({ id: 0, stRevAnterior: false, idParametro: p.id, idMetodo: metodoDe(p), idIdioma, idPrazo, tipoDocumento,
                vrUnitario: p.extra.unitario ?? 0, porcDesconto: 0, vrDesconto: 0 });
        modalSelecao.hide();
        desenhar();
    });

    // ------------------------------------------------------------ contrato (6.1): contrato padrão e importar arquivo
    const editorContrato = document.querySelector('[data-texto="TxContrato"]');
    const avisoContrato = msg => { $('avisoContrato').textContent = msg ?? ''; $('avisoContrato').classList.toggle('d-none', !msg); };
    const porNoContrato = html => {
        if (editorContrato.textContent.trim() && !confirm('Substituir o texto atual do contrato?')) return false;
        editorContrato.innerHTML = html ?? '';
        return true;
    };
    document.querySelector('[data-acao="contrato-padrao"]')?.addEventListener('click', async () => {
        const r = await fetch('/Propostas/ContratoPadrao', { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        if (!r.ok) return avisoContrato('Não foi possível buscar o contrato padrão.');
        if (porNoContrato((await r.json()).html)) avisoContrato('Contrato padrão da empresa aplicado: confira e grave a proposta.');
    });
    $('arquivoContrato')?.addEventListener('change', async e => {
        const arquivo = e.target.files[0];
        e.target.value = '';
        if (!arquivo) return;
        const corpo = new FormData();
        corpo.append('arquivo', arquivo);
        corpo.append('__RequestVerificationToken', form.querySelector('[name=__RequestVerificationToken]').value);
        const r = await fetch('/Propostas/ImportarContrato', { method: 'POST', body: corpo, headers: { 'X-Requested-With': 'XMLHttpRequest' } });
        if (r.status === 422) return avisoContrato((await r.json()).erro);
        if (!r.ok) return avisoContrato(r.status === 413 ? 'Arquivo maior que 2 MB.' : 'Não foi possível importar o arquivo.');
        const { html, aviso } = await r.json();
        if (porNoContrato(html)) avisoContrato(`Arquivo "${arquivo.name}" importado${aviso ? ' — ' + aviso : ''}. Confira e grave a proposta.`);
    });

    // ------------------------------------------------------------ gravar
    form.addEventListener('submit', () => {
        campoItens.value = JSON.stringify(itens);
        for (const editor of document.querySelectorAll('[data-texto]')) $(editor.dataset.texto).value = editor.innerHTML;
    });

    aplicar('cliente', false);
    aplicar('cobranca', false);
    desenhar();
    if (form.querySelector('[data-erro-itens]')) bootstrap.Tab.getOrCreateInstance($('botaoAbaAnalises')).show();
})();
