// T27 — Boletim (tarefa 5.3): "Marcar todos" no grid de resultados da amostra (o resto são formulários comuns).
(() => {
    document.addEventListener('click', e => {
        const botao = e.target.closest('[data-marcar-todos]');
        if (!botao) return;
        const caixas = [...document.querySelectorAll(`${botao.dataset.marcarTodos} input[type=checkbox]`)];
        const marcar = caixas.some(c => !c.checked);
        caixas.forEach(c => c.checked = marcar);
    });
})();
