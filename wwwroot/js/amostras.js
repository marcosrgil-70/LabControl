// T22 — Recebimento de amostras (tarefa 4.3). Cascata cliente → proposta (última revisão, status que permite amostra)
// → item → tipo de análise (só os das análises do item), por /Amostras/Cliente/{id} e /Amostras/Proposta/{id};
// prévia do número TT-?????-AA/YY (o número de verdade é dado pelo banco ao gravar, com o ano do servidor);
// avisos da data de entrega (P04): antiga demais pede confirmação explícita; ano diferente do atual avisa.
(() => {
    const form = document.getElementById('formAmostra');
    if (!form) return;
    const $ = id => document.getElementById(id);
    const dados = JSON.parse($('dadosAmostra').textContent);
    const ajax = { headers: { 'X-Requested-With': 'XMLHttpRequest' } };
    const hoje = new Date(dados.hoje + 'T00:00:00');
    const entregaOriginal = $('DtEntrega').value;

    const preencher = (select, opcoes, escolhido) => {
        const atual = escolhido ?? select.value;
        select.replaceChildren(new Option('—', ''), ...opcoes.map(o => new Option(o.nome, o.id)));
        select.value = opcoes.some(o => String(o.id) === String(atual)) ? String(atual) : '';
    };

    // ------------------------------------------------------------ cascata
    const mostrarCliente = () => {
        const c = dados.cliente;
        $('infoCliente').textContent = c ? [c.cpfCnpj && `CPF/CNPJ ${c.cpfCnpj}`, c.endereco, c.fone && `Fone ${c.fone}`].filter(Boolean).join(' · ') : '';
        preencher($('IdLabPropostas'), c?.propostas ?? [], dados.valores.idLabPropostas);
        if (c && !c.propostas.length) $('infoCliente').textContent += ' · Nenhuma proposta (última revisão, status que permite amostra) para este cliente.';
    };
    const mostrarProposta = () => {
        const p = dados.proposta;
        $('nrPedido').textContent = p?.nrPedido ?? '';
        preencher($('IdLabPropostasProdutos'), (p?.itens ?? []).map(i => ({ id: i.id, nome: `${i.item} — ${i.produto}` })), dados.valores.idLabPropostasProdutos);
        mostrarItem();
    };
    const mostrarItem = () => {
        const item = dados.proposta?.itens.find(i => String(i.id) === $('IdLabPropostasProdutos').value);
        const tipos = dados.tiposAnalise.filter(t => item?.tiposAnalise.includes(t.id)).map(t => ({ id: t.id, nome: `${String(t.codigo).padStart(2, '0')} — ${t.nome}` }));
        preencher($('IdAnalisesTipo'), tipos, dados.valores.idAnalisesTipo);
        if (tipos.length === 1) $('IdAnalisesTipo').value = String(tipos[0].id);
        previa();
    };
    const buscar = async (url) => { const r = await fetch(url, ajax); return r.ok ? r.json() : null; };

    $('IdEntidades').addEventListener('change', async () => {
        dados.valores = {};
        const id = $('IdEntidades').value;
        dados.cliente = id ? await buscar(`/Amostras/Cliente/${id}`) : null;
        dados.proposta = null;
        mostrarCliente();
        mostrarProposta();
    });
    $('IdLabPropostas').addEventListener('change', async () => {
        dados.valores = {};
        const id = $('IdLabPropostas').value;
        dados.proposta = id ? await buscar(`/Amostras/Proposta/${id}`) : null;
        mostrarProposta();
    });
    $('IdLabPropostasProdutos').addEventListener('change', () => { dados.valores = {}; mostrarItem(); });

    // ------------------------------------------------------------ prévia do número (TT-?????-AA/YY)
    const previa = () => {
        if ($('nrAmostra').dataset.gravado) return;
        const codigo = (lista, id) => lista.find(t => String(t.id) === id)?.codigo;
        const tt = codigo(dados.tiposAmostra, $('IdAmostrasTipo').value), aa = codigo(dados.tiposAnalise, $('IdAnalisesTipo').value);
        const yy = String(hoje.getFullYear() % 100).padStart(2, '0');
        $('nrAmostra').textContent = tt && aa ? `${String(tt).padStart(2, '0')}-?????-${String(aa).padStart(2, '0')}/${yy} (INCLUSÃO)` : 'INCLUSÃO';
    };
    $('IdAmostrasTipo').addEventListener('change', previa);
    $('IdAnalisesTipo').addEventListener('change', previa);

    // ------------------------------------------------------------ data de entrega (P04)
    const diasAtras = () => $('DtEntrega').value ? Math.round((hoje - new Date($('DtEntrega').value + 'T00:00:00')) / 86400000) : 0;
    const retroativa = () => diasAtras() > dados.diasRetroativos && $('DtEntrega').value !== entregaOriginal;
    const avisarData = () => {
        const aviso = $('avisoData');
        const dias = diasAtras();
        const ano = $('DtEntrega').value ? Number($('DtEntrega').value.slice(0, 4)) : hoje.getFullYear();
        aviso.className = 'small mt-md-4';
        aviso.textContent = '';
        if (dias < 0) { aviso.textContent = 'Data de entrega maior que a data de hoje.'; aviso.classList.add('text-danger'); }
        else if (retroativa()) {
            aviso.textContent = `Entrega de ${dias} dias atrás (limite ${dados.diasRetroativos}): será pedida confirmação` + (dados.podeRetroativa ? '.' : ' e você não tem a permissão para gravar.');
            aviso.classList.add('text-danger');
        } else if (ano !== hoje.getFullYear()) {
            aviso.textContent = `Ano da entrega (${ano}) diferente do ano atual: o número será de ${hoje.getFullYear()}.`;
            aviso.classList.add('text-warning-emphasis', 'bg-warning-subtle', 'px-2', 'rounded');
        }
    };
    $('DtEntrega').addEventListener('input', avisarData);

    // ------------------------------------------------------------ observação (texto rico; o servidor limpa o HTML)
    const editor = document.querySelector('[data-texto]');
    editor.innerHTML = $('TxObservacao').value;
    document.querySelectorAll('[data-comando]').forEach(b => b.addEventListener('click', () => { editor.focus(); document.execCommand(b.dataset.comando); }));

    // ------------------------------------------------------------ gravar
    document.querySelector('[data-confirmar-retroativa]')?.addEventListener('click', () => $('ConfirmouRetroativa').value = 'true');
    form.addEventListener('submit', e => {
        $('TxObservacao').value = editor.innerHTML;
        if (e.submitter?.hasAttribute('data-confirmar-retroativa') || $('ConfirmouRetroativa').value.toLowerCase() === 'true' || !retroativa()) return;
        const data = $('DtEntrega').value.split('-').reverse().join('/');
        if (confirm(`A data de entrega ${data} é de ${diasAtras()} dias atrás. Confirme que o ANO está correto.`)) $('ConfirmouRetroativa').value = 'true';
        else e.preventDefault();
    });

    mostrarCliente();
    if (dados.proposta) mostrarProposta(); else previa();
    avisarData();
})();
