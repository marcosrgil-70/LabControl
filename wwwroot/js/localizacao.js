// T24 — localização da amostra: o grupo Descarte só aparece com a situação "Descartada"; ao escolher, a data/hora
// do descarte vem preenchida com agora (o servidor confere: obrigatória, não futura, não antes do arquivo).
(function () {
    const form = document.getElementById('formLocalizacao');
    if (!form) return;
    const descartada = document.getElementById('stDescartada');
    const data = document.getElementById('DtHrDescarte');
    const agora = () => {
        const d = new Date();
        d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
        return d.toISOString().slice(0, 16);
    };

    function atualizar(escolheu) {
        const ehDescarte = descartada.checked;
        form.querySelectorAll('[data-so-descartada]').forEach(e => e.classList.toggle('d-none', !ehDescarte));
        form.querySelectorAll('[data-so-arquivada]').forEach(e => e.classList.toggle('d-none', ehDescarte));
        data.required = ehDescarte;
        if (escolheu && ehDescarte && !data.value) data.value = agora();
    }

    form.querySelectorAll('input[name=Status]').forEach(r => r.addEventListener('change', () => atualizar(true)));
    atualizar(false);
})();
