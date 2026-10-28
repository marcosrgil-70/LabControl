// T23 — modal "Nova movimentação": parâmetros e justificativa obrigatória só na saída; aviso de saída maior que o saldo
// (o servidor e o banco recusam); recusa da gravação reabre o modal com o que foi digitado.
(function () {
    const modal = document.getElementById('modalMovimento');
    if (!modal) return;
    const saida = document.getElementById('movSaida');
    const qtde = document.getElementById('movQtde');
    const aviso = document.getElementById('avisoSaldo');
    const parametros = document.getElementById('parametrosSaida');
    const justificativa = document.getElementById('movJustificativa');
    const saldo = parseFloat(document.getElementById('saldoAtual').dataset.saldo);

    // "1.000,5" → 1000.5 (pt-BR)
    const numero = t => parseFloat(t.trim().replace(/\.(?=\d{3}(\D|$))/g, '').replace(',', '.'));

    function atualizar() {
        const ehSaida = saida.checked;
        parametros.classList.toggle('d-none', !ehSaida);
        // na entrada os parâmetros não vão (o servidor recusa)
        if (!ehSaida) parametros.querySelectorAll('input[type=checkbox]').forEach(c => c.checked = false);
        justificativa.required = ehSaida;
        modal.querySelectorAll('[data-so-saida]').forEach(e => e.classList.toggle('d-none', !ehSaida));
        const q = numero(qtde.value);
        aviso.classList.toggle('d-none', !(ehSaida && q > saldo));
    }

    modal.querySelectorAll('input[name=Tipo]').forEach(r => r.addEventListener('change', atualizar));
    qtde.addEventListener('input', atualizar);
    atualizar();
    if (modal.dataset.abrir === 'true') bootstrap.Modal.getOrCreateInstance(modal).show();
})();
