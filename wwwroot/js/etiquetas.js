// T25 — Etiquetas (tarefa 4.5): marcar/desmarcar todas e lembrar a última impressora usada neste navegador
// (a impressão em si é no servidor: PPLB em RAW para a impressora escolhida).
(() => {
    const form = document.getElementById('formEtiquetas');
    if (!form) return;
    const impressora = document.getElementById('Impressora');
    const chave = 'labcontrol.etiquetas.impressora';
    try {
        const ultima = localStorage.getItem(chave);
        if (ultima && [...impressora.options].some(o => o.value === ultima)) impressora.value = ultima;
    } catch { /* sem armazenamento local: escolhe de novo */ }
    form.addEventListener('submit', () => { try { localStorage.setItem(chave, impressora.value); } catch { } });

    const marcas = () => form.querySelectorAll('tbody input[type=checkbox]');
    document.getElementById('marcarTodas').addEventListener('change', e => marcas().forEach(c => c.checked = e.target.checked));
})();
